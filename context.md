# SmartKnowledgeDesk Project Context

## Project Purpose
SmartKnowledgeDesk is an ASP.NET Core ticketing and automation platform that ingests support emails, creates tickets automatically, applies AI-driven triage, and escalates stale issues.

## Key Components

### Background Automations
- `Services/EmailTicketIngestionService.cs`
  - Reads Gmail messages on app start and every configured interval.
  - Uses `GmailService` to fetch email subject/body.
  - Calls `AIService.RunTicketAgent(...)` to assign category, priority, suggested solution, and next action.
  - Creates `Ticket` records with status `New` and logs `AutomationEvent` entries.
  - Records run results through `IAutomationRunRecorder`.

- `Services/TicketTriageAutomationService.cs`
  - Runs on start and then at configured intervals.
  - Finds tickets missing category or priority.
  - Re-processes ticket descriptions with AI and updates `SuggestedSolution` and `NextAction`.
  - Creates `AutomationEvent` entries and records run results.

- `Services/StaleTicketEscalationService.cs`
  - Runs on start and then at configured intervals.
  - Finds tickets older than the stale threshold that are not closed or escalated.
  - Sets status to `Escalated` and logs events.
  - Records run results via `IAutomationRunRecorder`.

### Automation Tracking
- `Models/AutomationEvent.cs`
  - Stores automation event records in the database.
  - Includes event type, description, timestamp, status, and related ticket ID.

- `Services/IAutomationRunRecorder.cs` and `Services/FileAutomationRunRecorder.cs`
  - Persist run summaries in `App_Data/automation-results.jsonl`.
  - Provide recent run history for the UI.

### API and UI
- `Controllers/TicketApiController.cs`
  - Exposes `POST /api/TicketApi/create-from-email`.
  - Creates tickets from email payloads and stores AI-suggested solution fields.

- `Controllers/AutomationsController.cs`
  - Builds dashboard data for automation settings, event counts, run history, and codex automation descriptions.

- `Views/Automations/Index.cshtml`
  - Displays automation configuration cards, metrics, recorded run results, codex automation table, plugin list, and recent automation events.

### Data Model Enhancements
- `Models/Ticket.cs`
  - Added AI fields: `SuggestedSolution`, `NextAction`, `AssignedTeam`.
  - Supports richer ticket details and automated routing.

- `Data/ApplicationDbContext.cs`
  - Added `DbSet<AutomationEvent>`.

## Runtime Setup
- `Program.cs`
  - Registers hosted services for email ingestion, ticket triage, and stale escalation.
  - Registers `FileAutomationRunRecorder` as singleton.
  - Uses SQL Server database via `DefaultConnection`.
  - Configures background service exception behavior to ignore exceptions and continue.

## Important Behavior Notes
- Email ingestion and other automations run immediately on app startup, not only after the first timer tick.
- The app uses file-based run result persistence and database event logging together.
- Gmail credentials are read from `appsettings.json` under `Gmail:Email` and `Gmail:Password`.
- `TicketApiController` explicitly defines the `create-from-email` POST route to avoid route parameter conflicts.

## Current Feature Coverage
- Fully automated email-to-ticket creation
- AI-based ticket triage with stored recommendations
- Stale ticket escalation
- Automation dashboard with event history
- Manual API-based ticket creation with AI analysis
