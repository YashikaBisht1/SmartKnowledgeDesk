namespace SmartKnowledgeDesk.Models
{
    public class EmailTicketRequest
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? AgentName { get; set; }
        public string? CreatedBy { get; set; }
    }
}
