using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartKnowledgeDesk.Data;
using SmartKnowledgeDesk.Models;
using SmartKnowledgeDesk.Plugins;

namespace SmartKnowledgeDesk.Services
{
    public class TicketTriageAutomationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TicketTriageAutomationService> _logger;
        private readonly AutomationSettings _settings;
        private readonly IAutomationRunRecorder _runRecorder;

        public TicketTriageAutomationService(
            IServiceScopeFactory scopeFactory,
            ILogger<TicketTriageAutomationService> logger,
            IOptions<AutomationSettings> settings,
            IAutomationRunRecorder runRecorder)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _settings = settings.Value;
            _runRecorder = runRecorder;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_settings.TicketTriage.Enabled)
            {
                _logger.LogInformation("Ticket triage automation is disabled.");
                return;
            }

            await RunOnceAsync(stoppingToken);

            using var timer = new PeriodicTimer(GetInterval());
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunOnceAsync(stoppingToken);
            }
        }

        private async Task RunOnceAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var aiService = scope.ServiceProvider.GetRequiredService<AIService>();
                var plugins = scope.ServiceProvider.GetServices<ITicketAutomationPlugin>();

                var batchSize = Math.Clamp(_settings.TicketTriage.BatchSize, 1, 50);
                var tickets = await context.Tickets
                    .Where(ticket => string.IsNullOrWhiteSpace(ticket.Category)
                                     || string.IsNullOrWhiteSpace(ticket.Priority))
                    .OrderBy(ticket => ticket.CreatedDate)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                foreach (var ticket in tickets)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var agentResponse = await aiService.RunTicketAgent(ticket.Description, _settings.TicketTriage.AgentName);
                    ticket.Category = agentResponse.Category;
                    ticket.Priority = agentResponse.Priority;
                    ticket.SuggestedSolution = agentResponse.SuggestedSolution;
                    ticket.NextAction = agentResponse.NextAction;

                    if (string.IsNullOrWhiteSpace(ticket.Status))
                    {
                        ticket.Status = "Triaged";
                    }

                    foreach (var plugin in plugins)
                    {
                        await plugin.TicketTriagedAsync(ticket, agentResponse, cancellationToken);
                    }

                    context.AutomationEvents.Add(new AutomationEvent
                    {
                        EventType = "Ticket Triaged",
                        Description = $"AI Triage completed for ticket: {ticket.Title}. Priority: {ticket.Priority}, Category: {ticket.Category}",
                        TicketId = ticket.Id,
                        AutomationName = "AI Triage",
                        CreatedAt = DateTime.Now,
                        Status = "Success"
                    });
                }

                await context.SaveChangesAsync(cancellationToken);
                await _runRecorder.RecordAsync(
                    "AI Triage",
                    "Success",
                    $"Triaged {tickets.Count} tickets missing category or priority.",
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ticket triage automation failed.");
                await _runRecorder.RecordAsync(
                    "AI Triage",
                    "Failed",
                    ex.Message,
                    cancellationToken);
            }
        }

        private TimeSpan GetInterval()
        {
            var minutes = Math.Max(1, _settings.TicketTriage.IntervalMinutes);
            return TimeSpan.FromMinutes(minutes);
        }
    }
}
