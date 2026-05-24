namespace SmartKnowledgeDesk.Models
{
    public class AutomationSettings
    {
        public EmailIngestionAutomationSettings EmailIngestion { get; set; } = new();

        public TicketTriageAutomationSettings TicketTriage { get; set; } = new();

        public StaleTicketEscalationAutomationSettings StaleTicketEscalation { get; set; } = new();
    }

    public class EmailIngestionAutomationSettings
    {
        public bool Enabled { get; set; }

        public int IntervalMinutes { get; set; } = 15;

        public int MaxEmailsPerRun { get; set; } = 5;

        public string AgentName { get; set; } = "enterprise support AI agent";

        public string Host { get; set; } = "imap.gmail.com";

        public int Port { get; set; } = 993;

        public bool UseSsl { get; set; } = true;

        public bool PreferIpv4 { get; set; } = true;
    }

    public class TicketTriageAutomationSettings
    {
        public bool Enabled { get; set; } = true;

        public int IntervalMinutes { get; set; } = 30;

        public int BatchSize { get; set; } = 10;

        public string AgentName { get; set; } = "enterprise support AI agent";
    }

    public class StaleTicketEscalationAutomationSettings
    {
        public bool Enabled { get; set; } = true;

        public int IntervalMinutes { get; set; } = 60;

        public int StaleAfterHours { get; set; } = 24;
    }
}
