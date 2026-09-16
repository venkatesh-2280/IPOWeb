using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IPOWeb.Models;
using IPOWeb.Services.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IPOWeb.Services
{
    // Owns the Anthropic Messages API tool-calling loop. Each tool wraps one
    // existing backend report call (via IIpoDataGatewayService) so Claude can
    // only ever read data the current user's own APItoken already authorizes.
    public class ClaudeChatService : IClaudeChatService
    {
        private const string AnthropicVersion = "2023-06-01";
        private const int MaxRowsPerTool = 50;

        private const string SystemPrompt =
            "You are an assistant embedded in IPOWeb, an IPO allotment back-office system. " +
            "Users ask about IPO offers using terms like: offer code (e.g. CL00037-41), allottee/allotment " +
            "(how many applications received shares), subscription / times-subscribed / oversubscription, " +
            "rejection (technical rejection, PAN mismatch, invalid DP), and investor categories " +
            "IND/Retail, HNI/NII (individual and corporate), QIB, NRI (split into <=10L and >10L slabs), " +
            "Market Maker (MM), and Employee. " +
            "For Rights Offers (offer_type = Rights Offer), users may also ask for a specific investor's " +
            "Rights Issue entitlement (how many shares a PAN is entitled to buy) - use get_entitlement_by_pan " +
            "for these, which needs both the offer_code and the investor's PAN. " +
            "If the user names a company or IPO instead of giving an explicit offer code, call find_offer_code " +
            "first to resolve it before calling any other tool. " +
            "Always answer using the data returned by the tools - never guess numbers. " +
            "When presenting category breakdowns, use a short markdown table. Be concise.";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AnthropicOptions _options;
        private readonly IIpoDataGatewayService _gateway;
        private readonly ILogger<ClaudeChatService> _logger;

        public ClaudeChatService(
            IHttpClientFactory httpClientFactory,
            IOptions<AnthropicOptions> options,
            IIpoDataGatewayService gateway,
            ILogger<ClaudeChatService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _options = options?.Value ?? new AnthropicOptions();
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _logger = logger;
        }

        public async Task<ChatAskResult> AskAsync(string userMessage, List<ChatTurn> priorTurns, string userCode, string roleCode)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                return new ChatAskResult
                {
                    Success = true,
                    Reply = "The chat assistant isn't configured yet. Ask your administrator to set the ANTHROPIC_API_KEY environment variable."
                };
            }

            var messages = new JArray();
            var recentTurns = priorTurns != null && priorTurns.Count > 10
                ? priorTurns.Skip(priorTurns.Count - 10).ToList()
                : priorTurns ?? new List<ChatTurn>();

            foreach (var turn in recentTurns)
            {
                messages.Add(new JObject { ["role"] = turn.Role, ["content"] = turn.Content });
            }
            messages.Add(new JObject { ["role"] = "user", ["content"] = userMessage });

            var tools = BuildToolDefinitions();

            for (int roundtrip = 0; roundtrip < _options.MaxToolRoundtrips; roundtrip++)
            {
                JObject responseJson;
                try
                {
                    var requestBody = new JObject
                    {
                        ["model"] = _options.Model,
                        ["max_tokens"] = _options.MaxTokens,
                        ["system"] = SystemPrompt,
                        ["messages"] = messages,
                        ["tools"] = tools
                    };
                    responseJson = await CallAnthropicAsync(requestBody).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Anthropic API call failed");
                    return new ChatAskResult { Success = false, Reply = "Sorry, I couldn't reach the chat assistant service right now." };
                }

                var stopReason = (string)responseJson["stop_reason"];
                var contentBlocks = responseJson["content"] as JArray ?? new JArray();

                if (stopReason == "tool_use")
                {
                    messages.Add(new JObject { ["role"] = "assistant", ["content"] = contentBlocks });

                    var toolResults = new JArray();
                    foreach (var block in contentBlocks)
                    {
                        if ((string)block["type"] != "tool_use") continue;

                        var toolName = (string)block["name"];
                        var toolUseId = (string)block["id"];
                        var input = block["input"] as JObject ?? new JObject();

                        string resultText;
                        try
                        {
                            resultText = await ExecuteToolAsync(toolName, input, userCode, roleCode).ConfigureAwait(false);
                        }
                        catch (IpoApiAuthExpiredException)
                        {
                            return new ChatAskResult { Success = false, AuthExpired = true };
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Chat tool {Tool} failed", toolName);
                            resultText = JsonConvert.SerializeObject(new { error = ex.Message });
                        }

                        toolResults.Add(new JObject
                        {
                            ["type"] = "tool_result",
                            ["tool_use_id"] = toolUseId,
                            ["content"] = resultText
                        });
                    }

                    messages.Add(new JObject { ["role"] = "user", ["content"] = toolResults });
                    continue;
                }

                var replyText = string.Join("\n", contentBlocks
                    .Where(b => (string)b["type"] == "text")
                    .Select(b => (string)b["text"]));

                return new ChatAskResult
                {
                    Success = true,
                    Reply = string.IsNullOrWhiteSpace(replyText) ? "I couldn't find an answer to that." : replyText
                };
            }

            return new ChatAskResult
            {
                Success = true,
                Reply = "I couldn't fully answer that within the allotted steps - try narrowing your question (e.g. give a single offer code)."
            };
        }

        private async Task<JObject> CallAnthropicAsync(JObject requestBody)
        {
            var client = _httpClientFactory.CreateClient("Anthropic");
            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
            request.Headers.Add("x-api-key", _options.ApiKey);
            request.Headers.Add("anthropic-version", AnthropicVersion);
            if (!string.IsNullOrWhiteSpace(_options.WorkspaceId))
                request.Headers.Add("anthropic-workspace-id", _options.WorkspaceId);
            request.Content = new StringContent(requestBody.ToString(Formatting.None), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new Exception("Anthropic API returned " + response.StatusCode + ": " + body);

            return JObject.Parse(body);
        }

        private async Task<string> ExecuteToolAsync(string toolName, JObject input, string userCode, string roleCode)
        {
            switch (toolName)
            {
                case "find_offer_code":
                {
                    var query = (string)input["query"] ?? string.Empty;
                    var offers = await _gateway.GetOffersListAsync(userCode, roleCode).ConfigureAwait(false);
                    var matches = offers.Where(o => MatchesQuery(o, query)).Take(MaxRowsPerTool).ToList();
                    return JsonConvert.SerializeObject(new { matches });
                }
                case "get_allotment_summary":
                {
                    var offerCode = RequireString(input, "offer_code");
                    var mom = await _gateway.GetMomReportAsync(offerCode).ConfigureAwait(false);
                    return JsonConvert.SerializeObject(new
                    {
                        offer = mom.summary,
                        allotmentByCategory = mom.allotmentSummary,
                        applicationsByCategory = mom.validAppln
                    });
                }
                case "get_subscription_rates":
                {
                    var offerCode = RequireString(input, "offer_code");
                    var mom = await _gateway.GetMomReportAsync(offerCode).ConfigureAwait(false);
                    return JsonConvert.SerializeObject(new
                    {
                        overall = mom.categoryCOVERSUBS,
                        retailIndividual = mom.categoryINDData,
                        hniCorporate = mom.categoryCo,
                        nriUpTo10L = mom.categoryNRA10L,
                        nriAbove10L = mom.categoryNRB10L,
                        qib = mom.categoryCQIB,
                        marketMaker = mom.categoryMARMAK
                    });
                }
                case "get_rejection_summary":
                {
                    var offerCode = RequireString(input, "offer_code");
                    var ruleCode = (string)input["rule_code"];
                    if (!string.IsNullOrWhiteSpace(ruleCode))
                    {
                        var detail = await _gateway.GetRejectionDetailAsync(offerCode, ruleCode).ConfigureAwait(false);
                        return JsonConvert.SerializeObject(new { detail = detail.Take(MaxRowsPerTool) });
                    }
                    var mom = await _gateway.GetMomReportAsync(offerCode).ConfigureAwait(false);
                    return JsonConvert.SerializeObject(new
                    {
                        reasons = mom.rejectionData,
                        panAndDpMismatch = mom.categoryCRNR,
                        technicalRejection = mom.categoryTechRej
                    });
                }
                case "get_bank_wise_bid_summary":
                {
                    var offerCode = RequireString(input, "offer_code");
                    var category = (string)input["category"];
                    var reconType = (string)input["recontype"];
                    var data = await _gateway.GetBidBankAsync(offerCode, category, reconType).ConfigureAwait(false);
                    return TruncateJson(data);
                }
                case "get_upi_bid_summary":
                {
                    var offerCode = RequireString(input, "offer_code");
                    var data = await _gateway.GetBidUpiAsync(offerCode).ConfigureAwait(false);
                    return TruncateJson(data);
                }
                case "get_entitlement_by_pan":
                {
                    var offerCode = RequireString(input, "offer_code");
                    var pan = RequireString(input, "pan");
                    var data = await _gateway.GetRightsEntitlementAsync(offerCode).ConfigureAwait(false);
                    var details = data?["details"] as JArray ?? new JArray();

                    var match = details.FirstOrDefault(row =>
                        row is JArray arr && arr.Count > 1 &&
                        string.Equals((string)arr[1], pan, StringComparison.OrdinalIgnoreCase));

                    if (match is not JArray row2)
                    {
                        return JsonConvert.SerializeObject(new
                        {
                            found = false,
                            message = $"No Rights Issue entitlement record found for PAN {pan} under offer {offerCode}."
                        });
                    }

                    return JsonConvert.SerializeObject(new
                    {
                        found = true,
                        folioOrBoId = row2[0],
                        pan = row2[1],
                        investorName = row2[2],
                        depository = row2[3],
                        dpId = row2[4],
                        clientId = row2[5],
                        holdingQty = row2[6],
                        ratio = row2[7],
                        eligibleEntitlementQty = row2[8],
                        fractionalQty = row2[9],
                        additionalApplied = row2[10],
                        totalApplied = row2[11],
                        allotted = row2[12],
                        unallotted = row2[13],
                        status = row2[14]
                    });
                }
                default:
                    return JsonConvert.SerializeObject(new { error = "Unknown tool: " + toolName });
            }
        }

        private static bool MatchesQuery(Dictionary<string, object> row, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            return row.Values.Any(v => v != null && v.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string RequireString(JObject input, string key)
        {
            var value = (string)input[key];
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"Missing required parameter '{key}'.");
            return value;
        }

        private static string TruncateJson(JToken token)
        {
            if (token is JArray arr && arr.Count > MaxRowsPerTool)
            {
                var truncated = new JObject
                {
                    ["data"] = new JArray(arr.Take(MaxRowsPerTool)),
                    ["note"] = $"showing first {MaxRowsPerTool} of {arr.Count} rows"
                };
                return truncated.ToString(Formatting.None);
            }
            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties().ToList())
                {
                    if (prop.Value is JArray innerArr && innerArr.Count > MaxRowsPerTool)
                    {
                        obj[prop.Name] = new JArray(innerArr.Take(MaxRowsPerTool));
                    }
                }
            }
            return token?.ToString(Formatting.None) ?? "null";
        }

        private static JArray BuildToolDefinitions()
        {
            return new JArray
            {
                new JObject
                {
                    ["name"] = "find_offer_code",
                    ["description"] = "Resolve a company/client name or partial IPO name the user typed into the exact offer_code(s) needed by the other tools. Call this FIRST whenever the user names a company instead of giving an explicit offer code (offer codes look like CL00037-41).",
                    ["input_schema"] = new JObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JObject
                        {
                            ["query"] = new JObject
                            {
                                ["type"] = "string",
                                ["description"] = "Company name, partial name, or client code as typed by the user."
                            }
                        },
                        ["required"] = new JArray("query")
                    }
                },
                new JObject
                {
                    ["name"] = "get_allotment_summary",
                    ["description"] = "Category-wise allotment results for an IPO offer: number of allottees (applications) and shares allotted per investor category (Retail/IND, HNI/Corporate, NRI, QIB, Employee, Market Maker), plus gross/valid/rejected application counts. Use for 'how many allottees', 'shares allotted by category'.",
                    ["input_schema"] = new JObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JObject
                        {
                            ["offer_code"] = new JObject
                            {
                                ["type"] = "string",
                                ["description"] = "IPO offer code, e.g. CL00037-41. If the user gave a company name instead, call find_offer_code first."
                            }
                        },
                        ["required"] = new JArray("offer_code")
                    }
                },
                new JObject
                {
                    ["name"] = "get_subscription_rates",
                    ["description"] = "Subscription (times-subscribed / over-subscription) rate for an IPO offer, overall and per investor category (Retail/IND, HNI/Corporate, NRI, QIB, Market Maker). Use for 'what is the subscription rate', 'how many times subscribed', 'over-subscription for category X'.",
                    ["input_schema"] = new JObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JObject
                        {
                            ["offer_code"] = new JObject { ["type"] = "string", ["description"] = "IPO offer code, e.g. CL00037-41." }
                        },
                        ["required"] = new JArray("offer_code")
                    }
                },
                new JObject
                {
                    ["name"] = "get_rejection_summary",
                    ["description"] = "Rejection reasons/counts for an IPO offer: PAN-mismatch, invalid-DP, multi-PAN counts, and technical rejection totals. Pass rule_code only if the user asks about a specific rejection rule.",
                    ["input_schema"] = new JObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JObject
                        {
                            ["offer_code"] = new JObject { ["type"] = "string", ["description"] = "IPO offer code, e.g. CL00037-41." },
                            ["rule_code"] = new JObject { ["type"] = "string", ["description"] = "Optional specific technical-rejection rule code." }
                        },
                        ["required"] = new JArray("offer_code")
                    }
                },
                new JObject
                {
                    ["name"] = "get_bank_wise_bid_summary",
                    ["description"] = "Bank-wise bid application count/quantity/amount for an IPO offer, optionally filtered by investor category and reconciliation type (e.g. ASBA/NONASBA).",
                    ["input_schema"] = new JObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JObject
                        {
                            ["offer_code"] = new JObject { ["type"] = "string", ["description"] = "IPO offer code, e.g. CL00037-41." },
                            ["category"] = new JObject { ["type"] = "string", ["description"] = "Optional investor category filter (IND, HNI, QIB, MM, EMP)." },
                            ["recontype"] = new JObject { ["type"] = "string", ["description"] = "Optional reconciliation type filter (e.g. ASBA, NONASBA)." }
                        },
                        ["required"] = new JArray("offer_code")
                    }
                },
                new JObject
                {
                    ["name"] = "get_upi_bid_summary",
                    ["description"] = "Bank-wise UPI-mode bid counts, shares, and amounts for an IPO offer.",
                    ["input_schema"] = new JObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JObject
                        {
                            ["offer_code"] = new JObject { ["type"] = "string", ["description"] = "IPO offer code, e.g. CL00037-41." }
                        },
                        ["required"] = new JArray("offer_code")
                    }
                },
                new JObject
                {
                    ["name"] = "get_entitlement_by_pan",
                    ["description"] = "Rights Issue entitlement details for one investor, looked up by PAN, for a given Rights Offer. " +
                        "Returns holding quantity, rights ratio, eligible entitlement quantity, fractional quantity, additional applied, " +
                        "total applied, allotted, unallotted, and status. Use for 'how many shares is PAN X entitled to' or similar " +
                        "for a Rights Offer offer_code. Entitlement must already have been run for the offer for data to exist.",
                    ["input_schema"] = new JObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JObject
                        {
                            ["offer_code"] = new JObject { ["type"] = "string", ["description"] = "Rights Offer code, e.g. CL00037-41." },
                            ["pan"] = new JObject { ["type"] = "string", ["description"] = "Investor's PAN number, e.g. ABCDE1234F." }
                        },
                        ["required"] = new JArray("offer_code", "pan")
                    }
                }
            };
        }
    }
}
