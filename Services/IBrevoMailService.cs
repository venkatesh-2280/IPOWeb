using System.Collections.Generic;
using System.Threading.Tasks;
using IPOWeb.Models;

namespace IPOWeb.Services
{
    // Generic bulk-mail plugin on top of Brevo's transactional email API.
    // Callers supply the subject/HTML template (with {{params.x}} placeholders)
    // and a recipient list with per-recipient merge fields - this service has no
    // knowledge of "rights issue" vs "allotment intimation" vs anything else.
    public interface IBrevoMailService
    {
        Task<BulkMailResult> SendBulkAsync(
            string subject,
            string htmlContent,
            List<MailRecipient> recipients,
            string senderName = null,
            string senderEmail = null);
    }
}
