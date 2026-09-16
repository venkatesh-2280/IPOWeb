using System;

namespace IPOWeb.Services.Exceptions
{
    // Thrown by IpoDataGatewayService when the backend IPO API rejects the current
    // user's APItoken cookie with a 401, so callers with no ActionResult of their own
    // (e.g. ClaudeChatService) can signal auth expiry up to the controller.
    public class IpoApiAuthExpiredException : Exception
    {
        public IpoApiAuthExpiredException() : base("Authentication expired.") { }
    }
}
