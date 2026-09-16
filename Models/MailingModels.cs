using System.Collections.Generic;

namespace IPOWeb.Models
{
    public class MailRecipient
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public Dictionary<string, object> Params { get; set; } = new();
    }

    public class BulkMailRequest
    {
        public string OfferCode { get; set; }
        public string in_action { get; set; }
        public string MailType { get; set; }       // free-text label for logging only, e.g. "RightsIssue", "AllotmentIntimation"
        public string Subject { get; set; }        // may contain {{params.x}} placeholders
        public string HtmlContent { get; set; }    // may contain {{params.x}} placeholders
    }

    public class BulkMailResult
    {
        public bool Success { get; set; }
        public int TotalRecipients { get; set; }
        public int SentCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
