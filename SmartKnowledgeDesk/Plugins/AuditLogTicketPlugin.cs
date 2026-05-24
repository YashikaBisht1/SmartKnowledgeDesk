using SmartKnowledgeDesk.Models;

namespace SmartKnowledgeDesk.Plugins
{
    public class AuditLogTicketPlugin : ITicketAutomationPlugin
    {
        private readonly ILogger<AuditLogTicketPlugin> _logger;

        public AuditLogTicketPlugin(ILogger<AuditLogTicketPlugin> logger)
        {
            _logger = logger;
        }

        public string Name => "Automation audit log";

        public string Description => "Writes scheduled ticket automation activity to application logs.";

        public Task TicketCreatedAsync(Ticket ticket, AgentResponse agentResponse, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Automation created ticket {TicketId} with category {Category} and priority {Priority}.",
                ticket.Id,
                agentResponse.Category,
                agentResponse.Priority);

            return Task.CompletedTask;
        }

        public Task TicketTriagedAsync(Ticket ticket, AgentResponse agentResponse, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Automation triaged ticket {TicketId} with category {Category} and priority {Priority}.",
                ticket.Id,
                agentResponse.Category,
                agentResponse.Priority);

            return Task.CompletedTask;
        }

        public Task TicketEscalatedAsync(Ticket ticket, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Automation escalated stale ticket {TicketId}.", ticket.Id);
            return Task.CompletedTask;
        }
    }
}
