namespace SmartKnowledgeDesk.Models
{
    public class AutomationRunResult
    {
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string AutomationName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;
    }
}
