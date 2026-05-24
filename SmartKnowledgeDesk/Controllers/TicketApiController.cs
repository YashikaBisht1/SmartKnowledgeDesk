using Microsoft.AspNetCore.Mvc;
using SmartKnowledgeDesk.Data;
using SmartKnowledgeDesk.Models;
using SmartKnowledgeDesk.Services;
using System.Linq;

namespace SmartKnowledgeDesk.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly AIService _aiService;

    public TicketApiController(ApplicationDbContext context, AIService aiService)
    {
        _context = context;
        _aiService = aiService;
    }

    [HttpGet]
    public IActionResult GetTickets()
    {
        var tickets = _context.Tickets.ToList();
        return Ok(tickets);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicketById(int id)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        return ticket == null ? NotFound() : Ok(ticket);
    }

    [HttpPost("create-from-email")]
    public async Task<IActionResult> CreateFromEmail([FromBody] EmailTicketRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Body))
        {
            return BadRequest("Email body is required.");
        }

        var agentResponse = await _aiService.RunTicketAgent(request.Body, request.AgentName ?? "enterprise support AI agent");

        var ticket = new Ticket
        {
            Title = string.IsNullOrWhiteSpace(request.Subject) ? "Email Ticket" : request.Subject,
            Description = request.Body,
            Category = agentResponse.Category,
            Priority = agentResponse.Priority,
            SuggestedSolution = agentResponse.SuggestedSolution,
            NextAction = agentResponse.NextAction,
            Status = "New",
            CreatedBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "Email Bot" : request.CreatedBy,
            CreatedDate = DateTime.Now
        };

        _context.Tickets.Add(ticket);
        
        _context.AutomationEvents.Add(new AutomationEvent
        {
            EventType = "Ticket Created from API",
            Description = $"Manual email ticket created: {ticket.Title}",
            AutomationName = "Email API",
            CreatedAt = DateTime.Now,
            Status = "Success"
        });

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTicketById), new { id = ticket.Id }, new
        {
            ticket.Id,
            ticket.Title,
            ticket.Category,
            ticket.Priority,
            ticket.Status,
            Suggestion = agentResponse.SuggestedSolution,
            NextAction = agentResponse.NextAction
        });
    }
}