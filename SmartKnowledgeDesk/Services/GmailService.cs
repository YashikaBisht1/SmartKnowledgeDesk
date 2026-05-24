using System.Net;
using System.Net.Sockets;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SmartKnowledgeDesk.Models;

namespace SmartKnowledgeDesk.Services
{
    public class GmailService
    {
        private readonly IConfiguration _configuration;
        private readonly EmailIngestionAutomationSettings _settings;

        public GmailService(
            IConfiguration configuration,
            IOptions<AutomationSettings> automationSettings)
        {
            _configuration = configuration;
            _settings = automationSettings.Value.EmailIngestion;
        }

        public List<EmailTicket> ReadEmails(int maxMessages = 5)
        {
            var emails = new List<EmailTicket>();
            var emailAddress = _configuration["Gmail:Email"];
            var password = _configuration["Gmail:Password"];
            var host = string.IsNullOrWhiteSpace(_settings.Host) ? "imap.gmail.com" : _settings.Host;
            var port = _settings.Port > 0 ? _settings.Port : 993;
            var secureSocketOptions = _settings.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTlsWhenAvailable;

            if (string.IsNullOrWhiteSpace(emailAddress) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Gmail credentials are not configured.");
            }

            using (var client = new ImapClient())
            {
                Connect(client, host, port, secureSocketOptions);
                client.Authenticate(emailAddress, password);

                var inbox = client.Inbox;
                if (inbox == null)
                {
                    throw new InvalidOperationException("Gmail inbox could not be opened.");
                }

                inbox.Open(FolderAccess.ReadOnly);

                var safeMaxMessages = Math.Max(1, maxMessages);
                for (int i = inbox.Count - 1; i >= Math.Max(0, inbox.Count - safeMaxMessages); i--)
                {
                    var message = inbox.GetMessage(i);
                    var body = message.TextBody ?? message.HtmlBody ?? string.Empty;

                    emails.Add(new EmailTicket
                    {
                        Subject = message.Subject ?? "(no subject)",
                        Body = body,
                        Sender = message.From.ToString()
                    });
                }

                client.Disconnect(true);
            }

            return emails;
        }

        private void Connect(ImapClient client, string host, int port, SecureSocketOptions secureSocketOptions)
        {
            var addresses = Dns.GetHostAddresses(host)
                .OrderBy(address => _settings.PreferIpv4 && address.AddressFamily != AddressFamily.InterNetwork ? 1 : 0)
                .ThenBy(address => address.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)
                .ToList();

            if (addresses.Count == 0)
            {
                throw new InvalidOperationException($"No IP addresses were resolved for '{host}'.");
            }

            var connectionErrors = new List<string>();

            foreach (var address in addresses)
            {
                Socket? socket = null;

                try
                {
                    socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(new IPEndPoint(address, port));
                    client.Connect(socket, host, port, secureSocketOptions);
                    return;
                }
                catch (Exception ex) when (ex is SocketException || ex is IOException || ex is SslHandshakeException)
                {
                    socket?.Dispose();
                    connectionErrors.Add($"{address} ({address.AddressFamily}): {ex.Message}");
                }
            }

            throw new InvalidOperationException(
                $"Unable to connect to Gmail IMAP at {host}:{port}. Attempts: {string.Join(" | ", connectionErrors)}");
        }
    }
}
