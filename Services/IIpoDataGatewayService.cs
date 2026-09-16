using System.Collections.Generic;
using System.Threading.Tasks;
using IPOWeb.Models;
using Newtonsoft.Json.Linq;

namespace IPOWeb.Services
{
    // Calls the backend IPO API on behalf of the currently logged-in user (same
    // APItoken-{name}_{role} cookie every controller already uses), so it can be
    // invoked from services/service-only code (e.g. the chat assistant) that has
    // no MVC ControllerContext of its own.
    public interface IIpoDataGatewayService
    {
        Task<MomRequest> GetMomReportAsync(string offerCode);
        Task<List<Dictionary<string, object>>> GetOffersListAsync(string userCode, string roleCode);
        Task<List<Dictionary<string, object>>> GetEmailListAsync(string offerCode, string in_action);
        Task<List<Dictionary<string, object>>> GetRejectionDetailAsync(string offerCode, string ruleCode);
        Task<JToken> GetBidBankAsync(string offerCode, string category, string reconType);
        Task<JToken> GetBidUpiAsync(string offerCode);
        Task<JToken> GetRightsEntitlementAsync(string offerCode);
    }
}
