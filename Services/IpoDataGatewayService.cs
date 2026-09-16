using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using IPOWeb.Models;
using IPOWeb.Services.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IPOWeb.Services
{
    // Backend calls follow the exact same shape every controller already uses
    // (new HttpClient() + APItoken-{name}_{role} bearer cookie), extracted here
    // so service-only code (the chat assistant) can call the backend without an
    // MVC ControllerContext. Registered Scoped, so the per-instance _momCache
    // lives only for the duration of one HTTP request.
    public class IpoDataGatewayService : IIpoDataGatewayService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<IpoDataGatewayService> _logger;
        private readonly string _apiBaseUrl;

        private readonly Dictionary<string, MomRequest> _momCache = new(StringComparer.OrdinalIgnoreCase);

        public IpoDataGatewayService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<IpoDataGatewayService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger;
            _apiBaseUrl = _configuration.GetSection("Appsettings")["apiurl"] ?? string.Empty;
        }

        private (HttpContext httpContext, string cookieName, string token) ResolveAuth()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                throw new IpoApiAuthExpiredException();

            var userName = httpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            var cookieName = "APItoken-" + userName + "_" + role;

            if (!httpContext.Request.Cookies.TryGetValue(cookieName, out var token) || string.IsNullOrWhiteSpace(token))
                throw new IpoApiAuthExpiredException();

            return (httpContext, cookieName, token);
        }

        private async Task<string> SendAsync(string relativeUrl)
        {
            var (httpContext, cookieName, token) = ResolveAuth();

            var client = _httpClientFactory.CreateClient();
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var url = _apiBaseUrl.TrimEnd('/') + "/" + relativeUrl;
            var response = await client.GetAsync(url).ConfigureAwait(false);

            ApiTokenRefreshMiddleware.TokenUpdate(httpContext, response, cookieName);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                httpContext.Response.Cookies.Delete(cookieName);
                throw new IpoApiAuthExpiredException();
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("Backend API call {Url} failed with {Status}", relativeUrl, response.StatusCode);
                throw new Exception("Backend API call failed: " + response.StatusCode + " (" + relativeUrl + ")");
            }

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public async Task<MomRequest> GetMomReportAsync(string offerCode)
        {
            if (_momCache.TryGetValue(offerCode, out var cached))
                return cached;

            var resultMessage = await SendAsync("getMomReports?offer_code=" + Uri.EscapeDataString(offerCode)).ConfigureAwait(false);

            dynamic temp = JsonConvert.DeserializeObject<dynamic>(resultMessage);
            if (temp is string)
            {
                temp = JsonConvert.DeserializeObject<dynamic>(temp.ToString());
            }

            var result = new MomRequest
            {
                summary = temp.Table != null && temp.Table.Count > 0
                    ? ((JObject)temp.Table[0]).ToObject<SummaryData>() : new SummaryData(),
                bankData = temp.Table1 != null
                    ? ((IEnumerable<dynamic>)temp.Table1).Select(x => ((JObject)x).ToObject<BankData>()).ToList() : new List<BankData>(),
                nonasbabankData = temp.Table2 != null
                    ? ((IEnumerable<dynamic>)temp.Table2).Select(x => ((JObject)x).ToObject<BankData>()).ToList() : new List<BankData>(),
                rejectionData = temp.Table3 != null
                    ? ((IEnumerable<dynamic>)temp.Table3).Select(x => ((JObject)x).ToObject<RejectionData>()).ToList() : new List<RejectionData>(),
                categoryData = temp.Table4 != null
                    ? ((IEnumerable<dynamic>)temp.Table4).Select(x => ((JObject)x).ToObject<CategoryData>()).ToList() : new List<CategoryData>(),
                categoryINDData = temp.Table5 != null
                    ? ((IEnumerable<dynamic>)temp.Table5).Select(x => ((JObject)x).ToObject<CategoryINDData>()).ToList() : new List<CategoryINDData>(),
                categoryCo = temp.Table6 != null
                    ? ((IEnumerable<dynamic>)temp.Table6).Select(x => ((JObject)x).ToObject<CategoryCo>()).ToList() : new List<CategoryCo>(),
                categoryNRA10L = temp.Table7 != null
                    ? ((IEnumerable<dynamic>)temp.Table7).Select(x => ((JObject)x).ToObject<CategoryNRA10L>()).ToList() : new List<CategoryNRA10L>(),
                categoryNRB10L = temp.Table8 != null
                    ? ((IEnumerable<dynamic>)temp.Table8).Select(x => ((JObject)x).ToObject<CategoryNRB10L>()).ToList() : new List<CategoryNRB10L>(),
                bankUPIData = temp.Table9 != null
                    ? ((IEnumerable<dynamic>)temp.Table9).Select(x => ((JObject)x).ToObject<BankUPIData>()).ToList() : new List<BankUPIData>(),
                bidApplRcd = temp.Table10 != null && temp.Table10.Count > 0
                    ? ((JObject)temp.Table10[0]).ToObject<bidApplRcdData>() : new bidApplRcdData(),
                validAppln = temp.Table11 != null
                    ? ((IEnumerable<dynamic>)temp.Table11).Select(x => ((JObject)x).ToObject<ValidAppln>()).ToList() : new List<ValidAppln>(),
                allotmentSummary = temp.Table12 != null
                    ? ((IEnumerable<dynamic>)temp.Table12).Select(x => ((JObject)x).ToObject<AllotmentSummary>()).ToList() : new List<AllotmentSummary>(),
                bankMaster = temp.Table13 != null
                    ? ((IEnumerable<dynamic>)temp.Table13).Select(x => ((JObject)x).ToObject<BankMaster>()).ToList() : new List<BankMaster>(),
                categoryQIB = temp.Table14 != null
                    ? ((IEnumerable<dynamic>)temp.Table14).Select(x => ((JObject)x).ToObject<CategoryQIB>()).ToList() : new List<CategoryQIB>(),
                categoryMM = temp.Table15 != null
                    ? ((IEnumerable<dynamic>)temp.Table15).Select(x => ((JObject)x).ToObject<CategoryMM>()).ToList() : new List<CategoryMM>(),
                categoryEMP = temp.Table16 != null
                    ? ((IEnumerable<dynamic>)temp.Table16).Select(x => ((JObject)x).ToObject<CategoryEMP>()).ToList() : new List<CategoryEMP>(),
                categoryNIIC = temp.Table17 != null
                    ? ((IEnumerable<dynamic>)temp.Table17).Select(x => ((JObject)x).ToObject<CategoryNIIC>()).ToList() : new List<CategoryNIIC>(),
                categorySOA = temp.Table18 != null
                    ? ((IEnumerable<dynamic>)temp.Table18).Select(x => ((JObject)x).ToObject<CategorySOA>()).ToList() : new List<CategorySOA>(),
                categoryEXMMSOA = temp.Table19 != null
                    ? ((IEnumerable<dynamic>)temp.Table19).Select(x => ((JObject)x).ToObject<CategoryEXMMSOA>()).ToList() : new List<CategoryEXMMSOA>(),
                categoryMARMAK = temp.Table20 != null
                    ? ((IEnumerable<dynamic>)temp.Table20).Select(x => ((JObject)x).ToObject<CategoryMARMAK>()).ToList() : new List<CategoryMARMAK>(),
                categoryTechRej = temp.Table21 != null
                    ? ((IEnumerable<dynamic>)temp.Table21).Select(x => ((JObject)x).ToObject<CategoryTechRej>()).ToList() : new List<CategoryTechRej>(),
                categoryUPISummary = temp.Table22 != null && temp.Table22.Count > 0
                    ? ((JObject)temp.Table22[0]).ToObject<categoryUPISummary>() : new categoryUPISummary(),
                categoryCNIIC = temp.Table23 != null && temp.Table23.Count > 0
                    ? ((JObject)temp.Table23[0]).ToObject<CategoryCNIIC>() : new CategoryCNIIC(),
                categoryCQIB = temp.Table24 != null && temp.Table24.Count > 0
                    ? ((JObject)temp.Table24[0]).ToObject<CategoryCQIB>() : new CategoryCQIB(),
                categoryCMMS = temp.Table25 != null && temp.Table25.Count > 0
                    ? ((JObject)temp.Table25[0]).ToObject<CategoryCMMS>() : new CategoryCMMS(),
                categoryCRNR = temp.Table26 != null && temp.Table26.Count > 0
                    ? ((JObject)temp.Table26[0]).ToObject<CategoryCRNR>() : new CategoryCRNR(),
                categoryCOVERSUBS = temp.Table27 != null && temp.Table27.Count > 0
                    ? ((JObject)temp.Table27[0]).ToObject<CategoryCOVERSUBS>() : new CategoryCOVERSUBS(),
                categoryCANCH = temp.Table28 != null && temp.Table28.Count > 0
                    ? ((JObject)temp.Table28[0]).ToObject<CategoryCANCH>() : new CategoryCANCH(),
                categoryCSTK = temp.Table29 != null
                    ? ((IEnumerable<dynamic>)temp.Table29).Select(x => ((JObject)x).ToObject<CategoryCSTK>()).ToList() : new List<CategoryCSTK>(),
                bankNAMaster = temp.Table30 != null
                    ? ((IEnumerable<dynamic>)temp.Table30).Select(x => ((JObject)x).ToObject<BankNAMaster>()).ToList() : new List<BankNAMaster>(),
            };

            _momCache[offerCode] = result;
            return result;
        }

        public async Task<List<Dictionary<string, object>>> GetOffersListAsync(string userCode, string roleCode)
        {
            var resultMessage = await SendAsync(
                "GetOfferlist?in_user_code=" + Uri.EscapeDataString(userCode ?? string.Empty) +
                "&in_role_code=" + Uri.EscapeDataString(roleCode ?? string.Empty)).ConfigureAwait(false);

            var jsonString = JsonConvert.DeserializeObject<string>(resultMessage);
            var dataSet = JsonConvert.DeserializeObject<DataSet>(jsonString);
            var table = dataSet?.Tables.Count > 0 ? dataSet.Tables[0] : null;
            return DataTableToList(table);
        }

        public async Task<List<Dictionary<string, object>>> GetEmailListAsync(string offerCode, string in_action)
        {
            var url = "getemailList?offer_code="
        + Uri.EscapeDataString(offerCode)
        + "&in_action="
        + Uri.EscapeDataString(in_action);
            var resultMessage = await SendAsync(url).ConfigureAwait(false);

            //var resultMessage = await SendAsync("getemailList?offer_code=" + Uri.EscapeDataString(offerCode)).ConfigureAwait(false);
            
            var jsonString = JsonConvert.DeserializeObject<string>(resultMessage);
            var dataSet = JsonConvert.DeserializeObject<DataSet>(jsonString);
            var table = dataSet?.Tables.Count > 0 ? dataSet.Tables[0] : null;
            return DataTableToList(table);
        }

        public async Task<List<Dictionary<string, object>>> GetRejectionDetailAsync(string offerCode, string ruleCode)
        {
            var resultMessage = await SendAsync(
                "GetRejectiondetail?offer_code=" + Uri.EscapeDataString(offerCode) +
                "&rule_code=" + Uri.EscapeDataString(ruleCode ?? string.Empty)).ConfigureAwait(false);

            var jsonString = JsonConvert.DeserializeObject<string>(resultMessage);
            var dataTable = JsonConvert.DeserializeObject<DataTable>(jsonString);
            return DataTableToList(dataTable);
        }

        public async Task<JToken> GetBidBankAsync(string offerCode, string category, string reconType)
        {
            var resultMessage = await SendAsync(
                "GetbidBank?offer_code=" + Uri.EscapeDataString(offerCode) +
                "&category=" + Uri.EscapeDataString(category ?? string.Empty) +
                "&recontype=" + Uri.EscapeDataString(reconType ?? string.Empty)).ConfigureAwait(false);

            return JToken.Parse(resultMessage);
        }

        public async Task<JToken> GetBidUpiAsync(string offerCode)
        {
            var resultMessage = await SendAsync("getBidUpi?offer_code=" + Uri.EscapeDataString(offerCode)).ConfigureAwait(false);
            return JToken.Parse(resultMessage);
        }

        private static List<Dictionary<string, object>> DataTableToList(DataTable table)
        {
            var rows = new List<Dictionary<string, object>>();
            if (table == null) return rows;

            foreach (DataRow row in table.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in table.Columns)
                {
                    dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                }
                rows.Add(dict);
            }
            return rows;
        }
    }
}
