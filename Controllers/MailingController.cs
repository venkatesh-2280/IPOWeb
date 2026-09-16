using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IPOWeb.Models;
using IPOWeb.Services;
using IPOWeb.Services.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IPOWeb.Controllers
{
    // Generic bulk-mail trigger: proxies recipients from the existing getemailList
    // backend endpoint into the Brevo plugin. Not specific to any one mail type -
    // "Rights Issue Notice", "Allotment Intimation", etc. are just a subject/body
    // template and a MailType label supplied by the caller at send time.
    [Authorize]
    public class MailingController : Controller
    {
        private readonly IIpoDataGatewayService _gateway;
        private readonly IBrevoMailService _brevoMailService;

        public MailingController(IIpoDataGatewayService gateway, IBrevoMailService brevoMailService)
        {
            _gateway = gateway;
            _brevoMailService = brevoMailService;
        }

        public IActionResult Mailing()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetRecipients(string offer_code, string in_action)
        {
            if (string.IsNullOrWhiteSpace(offer_code))
                return Json(new { success = false, message = "offer_code is required." });

            try
            {
                var rows = await _gateway.GetEmailListAsync(offer_code, in_action);
                var recipients = MapToRecipients(rows);
                var sampleColumns = recipients.Count > 0
                    ? recipients[0].Params.Keys.OrderBy(k => k).ToList()
                    : new List<string>();

                return Json(new
                {
                    success = true,
                    count = recipients.Count,
                    sampleColumns
                });
            }
            catch (IpoApiAuthExpiredException)
            {
                return Json(new { success = false, authExpired = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> SendBulkMail([FromBody] BulkMailRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.OfferCode))
                return Json(new { success = false, message = "offer_code is required." });
            if (string.IsNullOrWhiteSpace(model.Subject) || string.IsNullOrWhiteSpace(model.HtmlContent))
                return Json(new { success = false, message = "Subject and message body are required." });

            try
            {
                var rows = await _gateway.GetEmailListAsync(model.OfferCode, model.in_action);
                var recipients = MapToRecipients(rows);

                if (recipients.Count == 0)
                    return Json(new { success = false, message = "No recipients found for this offer code." });

                var result = await _brevoMailService.SendBulkAsync(model.Subject, model.HtmlContent, recipients);

                return Json(new
                {
                    success = result.Success,
                    result.TotalRecipients,
                    result.SentCount,
                    result.FailedCount,
                    result.Errors
                });
            }
            catch (IpoApiAuthExpiredException)
            {
                return Json(new { success = false, authExpired = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private static readonly string[] EmailColumnCandidates = { "email", "email_id", "emailid", "email_address" };
        private static readonly string[] NameColumnCandidates = { "name", "investor_name", "applicant_name", "full_name" };

        private static List<MailRecipient> MapToRecipients(List<Dictionary<string, object>> rows)
        {
            var recipients = new List<MailRecipient>();
            if (rows == null) return recipients;

            foreach (var row in rows)
            {
                var emailKey = row.Keys.FirstOrDefault(k => EmailColumnCandidates.Contains(k, StringComparer.OrdinalIgnoreCase));
                if (emailKey == null) continue;

                var email = row[emailKey]?.ToString();
                if (string.IsNullOrWhiteSpace(email)) continue;

                var nameKey = row.Keys.FirstOrDefault(k => NameColumnCandidates.Contains(k, StringComparer.OrdinalIgnoreCase));
                var name = nameKey != null ? row[nameKey]?.ToString() : null;

                var recipientParams = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in row)
                {
                    if (kvp.Key == emailKey || kvp.Key == nameKey) continue;
                    recipientParams[kvp.Key] = kvp.Value;
                }

                recipients.Add(new MailRecipient { Email = email, Name = name, Params = recipientParams });
            }

            return recipients;
        }
    }
}
