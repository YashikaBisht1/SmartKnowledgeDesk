using Microsoft.AspNetCore.Mvc;
using SmartKnowledgeDesk.Data;
using SmartKnowledgeDesk.Services;
using Microsoft.EntityFrameworkCore;

namespace SmartKnowledgeDesk.Controllers
{
    public class AIController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AIService _aiService;
        private readonly GmailService _gmailService;

        public AIController(ApplicationDbContext context, AIService aiService, GmailService gmailService)
        {
            _context = context;
            _aiService = aiService;
            _gmailService = gmailService;
        }

        public async Task<IActionResult> Summarize(int id, string agent = "enterprise support AI agent")
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            var summary = await _aiService.SummarizeTicket(id, ticket.Description, agent);

            ViewBag.Summary = summary;
            ViewBag.Title = ticket.Title;

            return View();
        }

        public async Task<IActionResult> RunAgent(int id, string agent = "enterprise support AI agent")
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            var summary = await _aiService.SummarizeTicket(id, ticket.Description, agent);

            ViewBag.Summary = summary;
            ViewBag.Title = ticket.Title;

            return View("Summarize");
        }

        public IActionResult ReadEmails()
        {
            var emails = _gmailService.ReadEmails();
            return View(emails);
        }
    }
}


