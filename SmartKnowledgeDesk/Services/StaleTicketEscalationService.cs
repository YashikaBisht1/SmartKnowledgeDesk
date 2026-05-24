using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartKnowledgeDesk.Data;
using SmartKnowledgeDesk.Models;
using SmartKnowledgeDesk.Plugins;

namespace SmartKnowledgeDesk.Services
{
    public class StaleTicketEscalationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StaleTicketEscalationService> _logger;
        private readonly AutomationSettings _settings;
        private readonly IAutomationRunRecorder _runRecorder;

        public StaleTicketEscalationService(
            IServiceScopeFactory scopeFactory,
            ILogger<StaleTicketEscalationService> logger,
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
            if (!_settings.StaleTicketEscalation.Enabled)
            {
                _logger.LogInformation("Stale ticket escalation automation is disabled.");
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
                var plugins = scope.ServiceProvider.GetServices<ITicketAutomationPlugin>();
                var staleCutoff = DateTime.Now.AddHours(-Math.Max(1, _settings.StaleTicketEscalation.StaleAfterHours));

                var tickets = await context.Tickets
                    .Where(ticket => ticket.CreatedDate <= staleCutoff
                                     && ticket.Status != "Closed"
                                     && ticket.Status != "Escalated")
                    .ToListAsync(cancellationToken);

                foreach (var ticket in tickets)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ticket.Status = "Escalated";

                    foreach (var plugin in plugins)
                    {
                        await plugin.TicketEscalatedAsync(ticket, cancellationToken);
                    }

                    context.AutomationEvents.Add(new AutomationEvent
                    {
                        EventType = "Ticket Escalated",
                        Description = $"Stale ticket escalated: {ticket.Title} (No activity for {_settings.StaleTicketEscalation.StaleAfterHours} hours)",
                        TicketId = ticket.Id,
                        AutomationName = "Stale Ticket Escalation",
                        CreatedAt = DateTime.Now,
                        Status = "Success"
                    });
                }

                await context.SaveChangesAsync(cancellationToken);
                await _runRecorder.RecordAsync(
                    "Stale Ticket Escalation",
                    "Success",
                    $"Escalated {tickets.Count} stale tickets.",
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stale ticket escalation automation failed.");
                await _runRecorder.RecordAsync(
                    "Stale Ticket Escalation",
                    "Failed",
                    ex.Message,
                    cancellationToken);
            }
        }

        private TimeSpan GetInterval()
        {
            var minutes = Math.Max(1, _settings.StaleTicketEscalation.IntervalMinutes);
            return TimeSpan.FromMinutes(minutes);
        }
    }
}
