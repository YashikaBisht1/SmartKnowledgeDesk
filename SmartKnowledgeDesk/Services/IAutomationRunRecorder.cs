using SmartKnowledgeDesk.Models;

namespace SmartKnowledgeDesk.Services
{
    public interface IAutomationRunRecorder
    {
        Task RecordAsync(string automationName, string status, string summary, CancellationToken cancellationToken = default);

        Task<List<AutomationRunResult>> ReadRecentAsync(int count, CancellationToken cancellationToken = default);
    }
}
