using System;
using System.ComponentModel.DataAnnotations;

namespace SmartKnowledgeDesk.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Priority { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string SuggestedSolution { get; set; } = string.Empty;

        public string NextAction { get; set; } = string.Empty;

        public string AssignedTeam { get; set; } = string.Empty;
    }
}