using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartKnowledgeDesk.Models;

namespace SmartKnowledgeDesk.Services
{
    public class AIService
    {
        private const int MaxTicketDescriptionLength = 4000;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public AIService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<AgentResponse> RunTicketAgent(string description, string agentName = "enterprise support AI agent")
        {
            var apiKey = _configuration["Groq:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Groq API key is not configured.");
            }

            var normalizedDescription = NormalizeDescription(description);

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model = "llama-3.1-8b-instant",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = $@"You are an {agentName}.

Analyze this support ticket and return:

Category:
Priority:
SuggestedSolution:
NextAction:

Ticket:
{normalizedDescription}

Keep answer short and practical."
                    }
                },
                max_tokens = 250
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "https://api.groq.com/openai/v1/chat/completions",
                content);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var resultObject = JsonConvert.DeserializeObject<JObject>(responseString);
            if (resultObject == null)
            {
                throw new InvalidOperationException("AI response was empty.");
            }

            var aiText = resultObject["choices"]?[0]?["message"]?["content"]?.ToString()
                         ?? resultObject["choices"]?[0]?["text"]?.ToString()
                         ?? string.Empty;

            if (string.IsNullOrWhiteSpace(aiText))
            {
                throw new InvalidOperationException("AI response contained no usable text.");
            }

            var agentResponse = new AgentResponse
            {
                RawText = aiText.Trim()
            };

            var lines = aiText.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("Category:", StringComparison.OrdinalIgnoreCase))
                    agentResponse.Category = trimmed.Substring("Category:".Length).Trim();
                else if (trimmed.StartsWith("Priority:", StringComparison.OrdinalIgnoreCase))
                    agentResponse.Priority = trimmed.Substring("Priority:".Length).Trim();
                else if (trimmed.StartsWith("SuggestedSolution:", StringComparison.OrdinalIgnoreCase))
                    agentResponse.SuggestedSolution = trimmed.Substring("SuggestedSolution:".Length).Trim();
                else if (trimmed.StartsWith("Suggested Solution:", StringComparison.OrdinalIgnoreCase))
                    agentResponse.SuggestedSolution = trimmed.Substring("Suggested Solution:".Length).Trim();
                else if (trimmed.StartsWith("NextAction:", StringComparison.OrdinalIgnoreCase))
                    agentResponse.NextAction = trimmed.Substring("NextAction:".Length).Trim();
                else if (trimmed.StartsWith("Next Action:", StringComparison.OrdinalIgnoreCase))
                    agentResponse.NextAction = trimmed.Substring("Next Action:".Length).Trim();
            }

            return agentResponse;
        }

        private static string NormalizeDescription(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return "No ticket description provided.";
            }

            var normalized = description
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ");

            normalized = string.Join(
                " ",
                normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            if (normalized.Length <= MaxTicketDescriptionLength)
            {
                return normalized;
            }

            return normalized[..MaxTicketDescriptionLength] + " [truncated]";
        }

        public async Task<string> SummarizeTicket(string description)
        {
            var agentResponse = await RunTicketAgent(description);
            return FormatSummary(agentResponse);
        }

        public async Task<string> SummarizeTicket(int ticketId, string description, string agentName)
        {
            var agentResponse = await RunTicketAgent(description, agentName);
            return $"Ticket Id: {ticketId}\nAgent: {agentName}\n\n{FormatSummary(agentResponse)}";
        }

        private static string FormatSummary(AgentResponse agentResponse)
        {
            if (string.IsNullOrWhiteSpace(agentResponse.Category)
                && string.IsNullOrWhiteSpace(agentResponse.Priority)
                && string.IsNullOrWhiteSpace(agentResponse.SuggestedSolution)
                && string.IsNullOrWhiteSpace(agentResponse.NextAction))
            {
                return agentResponse.RawText;
            }

            return $"Category: {agentResponse.Category}\nPriority: {agentResponse.Priority}\nSuggested Solution: {agentResponse.SuggestedSolution}\nNext Action: {agentResponse.NextAction}";
        }
    }
}
