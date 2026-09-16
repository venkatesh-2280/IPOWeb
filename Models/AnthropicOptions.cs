namespace IPOWeb.Models
{
    public class AnthropicOptions
    {
        // Populated only via Program.cs PostConfigure from the ANTHROPIC_API_KEY environment variable.
        // Never store this in appsettings.json.
        public string ApiKey { get; set; }
        public string WorkspaceId { get; set; }
        public string Model { get; set; } = "claude-sonnet-5";
        public int MaxTokens { get; set; } = 2048;
        public int MaxToolRoundtrips { get; set; } = 6;
    }
}
