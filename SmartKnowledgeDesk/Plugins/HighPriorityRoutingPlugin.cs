using SmartKnowledgeDesk.Models;

namespace SmartKnowledgeDesk.Plugins
{
    public class HighPriorityRoutingPlugin : ITicketAutomationPlugin
    {
        public string Name => "High priority routing";

        public string Description => "Moves urgent or critical AI-classified tickets into attention states.";

        public Task TicketCreatedAsync(Ticket ticket, AgentResponse agentResponse, CancellationToken cancellationToken)
        {
            ApplyRouting(ticket, agentResponse.Priority);
            return Task.CompletedTask;
        }

        public Task TicketTriagedAsync(Ticket ticket, AgentResponse agentResponse, CancellationToken cancellationToken)
        {
            ApplyRouting(ticket, agentResponse.Priority);
            return Task.CompletedTask;
        }

        public Task TicketEscalatedAsync(Ticket ticket, CancellationToken cancellationToken)
        {
            if (!string.Equals(ticket.Status, "Closed", StringComparison.OrdinalIgnoreCase))
            {
                ticket.Status = "Escalated";
            }

            return Task.CompletedTask;
        }

        private static void ApplyRouting(Ticket ticket, string priority)
        {
            if (string.Equals(ticket.Status, "Closed", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (priority.Contains("critical", StringComparison.OrdinalIgnoreCase))
            {
                ticket.Status = "Escalated";
            }
            else if (priority.Contains("high", StringComparison.OrdinalIgnoreCase)
                     || priority.Contains("urgent", StringComparison.OrdinalIgnoreCase))
            {
                ticket.Status = "Needs Attention";
            }
        }
    }
}
