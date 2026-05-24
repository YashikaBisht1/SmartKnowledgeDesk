using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartKnowledgeDesk.Data;
using SmartKnowledgeDesk.Models;
using SmartKnowledgeDesk.Plugins;

namespace SmartKnowledgeDesk.Services
{
    public class EmailTicketIngestionService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailTicketIngestionService> _logger;
        private readonly AutomationSettings _settings;
        private readonly IAutomationRunRecorder _runRecorder;

        public EmailTicketIngestionService(
            IServiceScopeFactory scopeFactory,
            ILogger<EmailTicketIngestionService> logger,
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
            if (!_settings.EmailIngestion.Enabled)
            {
                _logger.LogInformation("Email ticket ingestion automation is disabled.");
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
                var gmailService = scope.ServiceProvider.GetRequiredService<GmailService>();
                var aiService = scope.ServiceProvider.GetRequiredService<AIService>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var plugins = scope.ServiceProvider.GetServices<ITicketAutomationPlugin>();

                var maxEmails = Math.Clamp(_settings.EmailIngestion.MaxEmailsPerRun, 1, 25);
                var emails = gmailService.ReadEmails(maxEmails)
                    .Where(email => !string.IsNullOrWhiteSpace(email.Body))
                    .ToList();
                var createdCount = 0;
                var skippedCount = 0;
                var failedCount = 0;

                foreach (var email in emails)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var alreadyImported = await context.Tickets.AnyAsync(ticket =>
                            ticket.CreatedBy == "Email Bot"
                            && ticket.Title == email.Subject
                            && ticket.Description == email.Body,
                            cancellationToken);

                        if (alreadyImported)
                        {
                            skippedCount++;
                            continue;
                        }

                        var agentResponse = await aiService.RunTicketAgent(email.Body, _settings.EmailIngestion.AgentName);
                        var ticket = new Ticket
                        {
                            Title = string.IsNullOrWhiteSpace(email.Subject) ? "Email Ticket" : email.Subject,
                            Description = email.Body,
                            Category = agentResponse.Category,
                            Priority = agentResponse.Priority,
                            SuggestedSolution = agentResponse.SuggestedSolution,
                            NextAction = agentResponse.NextAction,
                            Status = "New",
                            CreatedBy = "Email Bot",
                            CreatedDate = DateTime.Now
                        };

                        foreach (var plugin in plugins)
                        {
                            await plugin.TicketCreatedAsync(ticket, agentResponse, cancellationToken);
                        }

                        context.Tickets.Add(ticket);
                        createdCount++;
                        
                        context.AutomationEvents.Add(new AutomationEvent
                        {
                            EventType = "Email Imported",
                            Description = $"Created ticket from email: {ticket.Title}",
                            AutomationName = "Email Ingestion",
                            CreatedAt = DateTime.Now,
                            Status = "Success"
                        });
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        _logger.LogWarning(ex, "Skipping email '{Subject}' during ingestion.", email.Subject);
                    }
                }

                await context.SaveChangesAsync(cancellationToken);
                await _runRecorder.RecordAsync(
                    "Email Ingestion",
                    "Success",
                    $"Read {emails.Count} emails, created {createdCount} tickets, skipped {skippedCount} duplicates, failed {failedCount}.",
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email ticket ingestion automation failed.");
                await _runRecorder.RecordAsync(
                    "Email Ingestion",
                    "Failed",
                    ex.Message,
                    cancellationToken);
            }
        }

        private TimeSpan GetInterval()
        {
            var minutes = Math.Max(1, _settings.EmailIngestion.IntervalMinutes);
            return TimeSpan.FromMinutes(minutes);
        }
    }
}
