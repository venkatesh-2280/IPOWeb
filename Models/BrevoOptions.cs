namespace IPOWeb.Models
{
    public class BrevoOptions
    {
        // Populated only via Program.cs PostConfigure from the BREVO_API_KEY environment variable.
        // Never store this in appsettings.json.
        public string ApiKey { get; set; }
        public string SenderName { get; set; } = "IPO Registrar";
        public string SenderEmail { get; set; }
        public int BatchSize { get; set; } = 500;
    }
}
