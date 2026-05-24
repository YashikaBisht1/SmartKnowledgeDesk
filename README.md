# SmartKnowledgeDesk

SmartKnowledgeDesk is a local ASP.NET Core ticketing automation platform that reads incoming Gmail messages, creates support tickets, applies AI-powered triage, logs automation events, and escalates stale issues.

## Project Journey

This repository was built from an interactive development session with the following workflow:

1. **Initial analysis**
   - Reviewed the application structure and identified automation-related files.
   - Confirmed there were three background services: email ingestion, AI ticket triage, and stale ticket escalation.
   - Discovered the UI only displayed automation settings, not execution history or AI recommendations.

2. **Feature enhancement**
   - Added persistent storage for AI results: `SuggestedSolution`, `NextAction`, and `AssignedTeam` on `Ticket`.
   - Added `AutomationEvent` logging for each automation action.
   - Implemented `IAutomationRunRecorder` and `FileAutomationRunRecorder` to record automation run summaries in `App_Data/automation-results.jsonl`.
   - Updated the automations dashboard to show metrics, recent run history, and event feed.
   - Extended ticket details to display AI-recommended solutions and next actions.

3. **Bug and deployment preparation**
   - Validated the API route in `TicketApiController` and ensured `create-from-email` is explicit.
   - Confirmed the app is configured to start correctly and identified port conflicts with launch settings.
   - Laid the groundwork for future deployment by creating a Git repository for GitHub/Vercel connection.

## Local run

To run the project locally:

```powershell
cd c:\Users\bisht\knowledge_base
dotnet run --project SmartKnowledgeDesk\SmartKnowledgeDesk.csproj --no-launch-profile --urls http://127.0.0.1:5300
```

Then open:

```
http://127.0.0.1:5300
```

## Notes for GitHub / Vercel

- This repository is now initialized locally with a clean `.gitignore` for C# and ASP.NET projects.
- A GitHub remote has not been configured from this environment because GitHub CLI is not installed and there is no access token.
- Once you have a GitHub repository URL, add it as a remote and push the code:

```powershell
git remote add origin https://github.com/<your-username>/<repo-name>.git
git push -u origin main
```

## Project structure highlights

- `SmartKnowledgeDesk/Program.cs` — app startup and hosted service registration
- `SmartKnowledgeDesk/Services/EmailTicketIngestionService.cs` — reads Gmail and creates tickets
- `SmartKnowledgeDesk/Services/TicketTriageAutomationService.cs` — fills missing AI ticket metadata
- `SmartKnowledgeDesk/Services/StaleTicketEscalationService.cs` — escalates old unresolved tickets
- `SmartKnowledgeDesk/Controllers/AutomationsController.cs` — builds automation dashboard data
- `SmartKnowledgeDesk/Views/Automations/Index.cshtml` — displays automation activity and metrics
- `SmartKnowledgeDesk/Models/Ticket.cs` — contains AI fields and workflow metadata

## Deployment

For Vercel or GitHub integration, push this repo to GitHub first. Then connect the GitHub repo to Vercel for deployment tracking.
