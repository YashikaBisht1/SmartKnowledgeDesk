using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SmartKnowledgeDesk.Models;
using SmartKnowledgeDesk.Plugins;
using SmartKnowledgeDesk.Data;
using Microsoft.EntityFrameworkCore;
using SmartKnowledgeDesk.Services;

namespace SmartKnowledgeDesk.Controllers
{
    public class AutomationsController : Controller
    {
        private readonly AutomationSettings _settings;
        private readonly IEnumerable<ITicketAutomationPlugin> _plugins;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AutomationsController> _logger;
        private readonly IAutomationRunRecorder _runRecorder;

        public AutomationsController(
            IOptions<AutomationSettings> settings,
            IEnumerable<ITicketAutomationPlugin> plugins,
            ApplicationDbContext context,
            ILogger<AutomationsController> logger,
            IAutomationRunRecorder runRecorder)
        {
            _settings = settings.Value;
            _plugins = plugins;
            _context = context;
            _logger = logger;
            _runRecorder = runRecorder;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Plugins = _plugins.ToList();
            ViewBag.CodexAutomations = GetCodexAutomations();
            var automationRunResults = await _runRecorder.ReadRecentAsync(25);
            ViewBag.AutomationRunResults = automationRunResults;
            ViewBag.LatestEmailIngestionResult = automationRunResults
                .FirstOrDefault(result => result.AutomationName == "Email Ingestion");

            var eventStats = new Dictionary<string, int>();
            var recentEvents = new List<AutomationEvent>();

            try
            {
                recentEvents = await _context.AutomationEvents
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(50)
                    .ToListAsync();

                eventStats["Total Events"] = await _context.AutomationEvents.CountAsync();
                eventStats["Today"] = await _context.AutomationEvents
                    .Where(e => e.CreatedAt.Date == DateTime.Now.Date)
                    .CountAsync();
                eventStats["Email Imported"] = await _context.AutomationEvents
                    .Where(e => e.EventType == "Email Imported")
                    .CountAsync();
                eventStats["Triaged"] = await _context.AutomationEvents
                    .Where(e => e.EventType == "Ticket Triaged")
                    .CountAsync();
                eventStats["Escalated"] = await _context.AutomationEvents
                    .Where(e => e.EventType == "Ticket Escalated")
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Automation event data is unavailable.");
                ViewBag.EventDataUnavailable = true;
            }
            
            ViewBag.EventStats = eventStats;
            ViewBag.RecentEvents = recentEvents;
            
            return View(_settings);
        }

        private static List<CodexAutomationInfo> GetCodexAutomations()
        {
            return new List<CodexAutomationInfo>
            {
                new()
                {
                    Name = "Check Mail And Create Tickets",
                    AutomationId = "check-mail-and-create-tickets",
                    Schedule = "Every hour",
                    Purpose = "Checks the mail-to-ticket workflow and reports how many tickets were created."
                },
                new()
                {
                    Name = "Triage Backlog Watch",
                    AutomationId = "triage-backlog-watch",
                    Schedule = "Every 2 hours",
                    Purpose = "Finds tickets missing AI category, priority, suggested solution, next action, or team assignment."
                },
                new()
                {
                    Name = "Stale Ticket Escalation Watch",
                    AutomationId = "stale-ticket-escalation-watch",
                    Schedule = "Every 3 hours",
                    Purpose = "Checks old or high-priority open tickets and confirms escalation behavior."
                },
                new()
                {
                    Name = "Automation Health Snapshot",
                    AutomationId = "automation-health-snapshot",
                    Schedule = "Every 6 hours",
                    Purpose = "Verifies the app, Automations page, ticket API, and recent automation activity."
                },
                new()
                {
                    Name = "Ticket Quality Audit",
                    AutomationId = "ticket-quality-audit",
                    Schedule = "Every 12 hours",
                    Purpose = "Audits recent tickets for incomplete AI output, vague descriptions, and inconsistent values."
                }
            };
        }
    }
}
