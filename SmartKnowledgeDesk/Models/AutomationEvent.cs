using System;

namespace SmartKnowledgeDesk.Models
{
    public class AutomationEvent
    {
        public int Id { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int? TicketId { get; set; }

        public string AutomationName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Success";
    }
}
