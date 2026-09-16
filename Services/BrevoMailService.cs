using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using brevo_csharp.Api;
using brevo_csharp.Client;
using brevo_csharp.Model;
using IPOWeb.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IPOWeb.Services
{
    public class BrevoMailService : IBrevoMailService
    {
        private readonly BrevoOptions _options;
        private readonly ILogger<BrevoMailService> _logger;

        public BrevoMailService(IOptions<BrevoOptions> options, ILogger<BrevoMailService> logger)
        {
            _options = options?.Value ?? new BrevoOptions();
            _logger = logger;
        }

        public async Task<BulkMailResult> SendBulkAsync(
            string subject,
            string htmlContent,
            List<MailRecipient> recipients,
            string senderName = null,
            string senderEmail = null)
        {
            var result = new BulkMailResult { TotalRecipients = recipients?.Count ?? 0 };

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                result.Success = false;
                result.Errors.Add("Brevo is not configured (BREVO_API_KEY environment variable is not set).");
                return result;
            }

            if (recipients == null || recipients.Count == 0)
            {
                result.Success = false;
                result.Errors.Add("No recipients supplied.");
                return result;
            }

            // Configuration.Default is process-global in this SDK; build a scoped
            // instance instead so concurrent requests never clobber each other's key.
            var configuration = new brevo_csharp.Client.Configuration();
            configuration.ApiKey["api-key"] = _options.ApiKey;
            var api = new TransactionalEmailsApi(configuration);

            var sender = new SendSmtpEmailSender(
                name: senderName ?? _options.SenderName,
                email: senderEmail ?? _options.SenderEmail);

            var batchSize = _options.BatchSize > 0 ? _options.BatchSize : 500;

            foreach (var chunk in recipients.Chunk(batchSize))
            {
                var messageVersions = chunk.Select(r => new SendSmtpEmailMessageVersions(
                    to: new List<SendSmtpEmailTo1> { new SendSmtpEmailTo1(email: r.Email, name: r.Name) },
                    _params: r.Params ?? new Dictionary<string, object>()
                )).ToList();

                var email = new SendSmtpEmail(
                    sender: sender,
                    subject: subject,
                    htmlContent: htmlContent,
                    messageVersions: messageVersions);

                try
                {
                    await api.SendTransacEmailAsync(email).ConfigureAwait(false);
                    result.SentCount += chunk.Length;
                }
                catch (ApiException ex)
                {
                    _logger?.LogWarning(ex, "Brevo batch send failed for {Count} recipients: {ErrorContent}", chunk.Length, (object)ex.ErrorContent);
                    result.FailedCount += chunk.Length;
                    result.Errors.Add($"Batch of {chunk.Length} recipients failed: {ex.Message}");
                }
            }

            result.Success = result.FailedCount == 0;
            return result;
        }
    }
}
