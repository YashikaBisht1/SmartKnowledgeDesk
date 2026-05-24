using Microsoft.EntityFrameworkCore;
using SmartKnowledgeDesk.Models;

namespace SmartKnowledgeDesk.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Ticket> Tickets { get; set; }

        public DbSet<AutomationEvent> AutomationEvents { get; set; }
    }
}