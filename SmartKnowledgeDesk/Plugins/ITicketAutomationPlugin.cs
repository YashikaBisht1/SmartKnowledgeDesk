using SmartKnowledgeDesk.Models;

namespace SmartKnowledgeDesk.Plugins
{
    public interface ITicketAutomationPlugin
    {
        string Name { get; }

        string Description { get; }

        Task TicketCreatedAsync(Ticket ticket, AgentResponse agentResponse, CancellationToken cancellationToken);

        Task TicketTriagedAsync(Ticket ticket, AgentResponse agentResponse, CancellationToken cancellationToken);

        Task TicketEscalatedAsync(Ticket ticket, CancellationToken cancellationToken);
    }
}
