using System.Text.Json;
using SmartKnowledgeDesk.Models;

namespace SmartKnowledgeDesk.Services
{
    public class FileAutomationRunRecorder : IAutomationRunRecorder
    {
        private static readonly SemaphoreSlim FileLock = new(1, 1);
        private readonly string _filePath;

        public FileAutomationRunRecorder(IWebHostEnvironment environment)
        {
            var dataPath = Path.Combine(environment.ContentRootPath, "App_Data");
            Directory.CreateDirectory(dataPath);
            _filePath = Path.Combine(dataPath, "automation-results.jsonl");
        }

        public async Task RecordAsync(
            string automationName,
            string status,
            string summary,
            CancellationToken cancellationToken = default)
        {
            var result = new AutomationRunResult
            {
                AutomationName = automationName,
                Status = status,
                Summary = summary,
                CreatedAt = DateTime.Now
            };

            var line = JsonSerializer.Serialize(result) + Environment.NewLine;
            await FileLock.WaitAsync(cancellationToken);
            try
            {
                await File.AppendAllTextAsync(_filePath, line, cancellationToken);
            }
            finally
            {
                FileLock.Release();
            }
        }

        public async Task<List<AutomationRunResult>> ReadRecentAsync(
            int count,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(_filePath))
            {
                return new List<AutomationRunResult>();
            }

            await FileLock.WaitAsync(cancellationToken);
            try
            {
                var lines = await File.ReadAllLinesAsync(_filePath, cancellationToken);
                return lines
                    .Reverse()
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(TryDeserialize)
                    .Where(result => result != null)
                    .Take(Math.Max(1, count))
                    .Cast<AutomationRunResult>()
                    .ToList();
            }
            finally
            {
                FileLock.Release();
            }
        }

        private static AutomationRunResult? TryDeserialize(string line)
        {
            try
            {
                return JsonSerializer.Deserialize<AutomationRunResult>(line);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
