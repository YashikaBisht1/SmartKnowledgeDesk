namespace SmartKnowledgeDesk.Models
{
    public class AgentResponse
    {
        public string Category { get; set; } = string.Empty;

        public string Priority { get; set; } = string.Empty;

        public string SuggestedSolution { get; set; } = string.Empty;

        public string NextAction { get; set; } = string.Empty;

        public string RawText { get; set; } = string.Empty;
    }
}