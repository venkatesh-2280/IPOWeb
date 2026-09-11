using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using IPOWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Data;
using System.Data.SqlTypes;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Web;

namespace IPOWeb.Controllers
{
    public class RunEntitlementController : MenuBaseController
    {
        public IActionResult RunEntitlement()
        {
            return View();
        }

        private IConfiguration _configuration;
        public RunEntitlementController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        string urlstring = "";
        string APIcookieName = "";

        [HttpGet]
        public JsonResult getRejReason(string offer_code, bool runRule)
        {
            urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "getRejReason";
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value.ToString() + "_" + User.FindFirst(ClaimTypes.Role)?.Value.ToString();
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    string url = urlstring + "?offer_code=" + offer_code + "&runRule=" + runRule;
                    var response = client.GetAsync(url).Result;
                    ApiTokenRefreshMiddleware.TokenUpdate(HttpContext, response, APIcookieName);
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Response.Cookies.Delete(APIcookieName);
                        return Json(new
                        {
                            success = false,
                            authExpired = true
                        });
                    }
                    if (response.IsSuccessStatusCode)
                    {
                        string resultMessage = response.Content.ReadAsStringAsync().Result;
                        var companyData = JsonConvert.DeserializeObject<object>(resultMessage);
                        return Json(new { success = true, data = companyData });
                    }
                    else
                    {
                        return Json(new { success = false, message = "API call failed: " + response.StatusCode });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult getdetailRejectionSummary_old(string offer_code, string rule_code)
        {
            urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "GetRejectiondetail";
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value.ToString() + "_" + User.FindFirst(ClaimTypes.Role)?.Value.ToString();
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    string url = urlstring + "?offer_code=" + offer_code + "&rule_code=" + rule_code;
                    var response = client.GetAsync(url).Result;
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Response.Cookies.Delete(APIcookieName);
                        return Json(new
                        {
                            success = false,
                            authExpired = true
                        });
                    }
                    if (response.IsSuccessStatusCode)
                    {
                        string resultMessage = response.Content.ReadAsStringAsync().Result;
                        var jsonString = JsonConvert.DeserializeObject<string>(resultMessage);
                        var dataTable = JsonConvert.DeserializeObject<DataTable>(jsonString);

                        using (var workbook = new XLWorkbook())
                        {
                            workbook.Worksheets.Add(dataTable, "Rejection Report");

                            using (var stream = new MemoryStream())
                            {
                                workbook.SaveAs(stream);
                                var content = stream.ToArray();
                                return File(
                                    content,
                                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                    "Rejection_Report.xlsx"
                                );
                            }
                        }
                        // return Json(new { success = true, data = companyData });
                    }
                    else
                    {
                        return Json(new { success = false, message = "API call failed: " + response.StatusCode });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // runRejection
        [HttpGet]
        public JsonResult runRejection(string offer_code)
        {
            urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "runRejection";
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value.ToString() + "_" + User.FindFirst(ClaimTypes.Role)?.Value.ToString();
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    string url = urlstring + "?offer_code=" + offer_code;
                    var response = client.GetAsync(url).Result;
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Response.Cookies.Delete(APIcookieName);
                        return Json(new
                        {
                            success = false,
                            authExpired = true
                        });
                    }
                    if (response.IsSuccessStatusCode)
                    {
                        string resultMessage = response.Content.ReadAsStringAsync().Result;
                        var companyData = JsonConvert.DeserializeObject<object>(resultMessage);
                        return Json(new { success = true, data = companyData });
                    }
                    else
                    {
                        return Json(new { success = false, message = "API call failed: " + response.StatusCode });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public IActionResult getdetailRejectionSummary(string offer_code, string rule_code)
        {
            urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "GetRejectiondetail";
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value.ToString() + "_" + User.FindFirst(ClaimTypes.Role)?.Value.ToString();
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    string url = urlstring + "?offer_code=" + offer_code + "&rule_code=" + rule_code;
                    var response = client.GetAsync(url).Result;
                    ApiTokenRefreshMiddleware.TokenUpdate(HttpContext, response, APIcookieName);
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Response.Cookies.Delete(APIcookieName);
                        return Json(new
                        {
                            success = false,
                            authExpired = true
                        });
                    }
                    if (response.IsSuccessStatusCode)
                    {
                        string resultMessage = response.Content.ReadAsStringAsync().Result;
                        var jsonString = JsonConvert.DeserializeObject<string>(resultMessage);
                        var dataTable = JsonConvert.DeserializeObject<DataTable>(jsonString);

                        using (var workbook = new XLWorkbook())
                        {
                            workbook.Worksheets.Add(dataTable, "Rejection Report");

                            using (var stream = new MemoryStream())
                            {
                                workbook.SaveAs(stream);
                                var content = stream.ToArray();
                                return File(
                                    content,
                                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                    "Rejection_Report.xlsx"
                                );
                            }
                        }
                        // return Json(new { success = true, data = companyData });
                    }
                    else
                    {
                        return Json(new { success = false, message = "API call failed: " + response.StatusCode });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetAddRejList_old(string ipo_code)
        {
            urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "GetAddRejList";
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value.ToString() + "_" + User.FindFirst(ClaimTypes.Role)?.Value.ToString();
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    string url = urlstring + "?&ipo_code=" + ipo_code;
                    var response = client.GetAsync(url).Result;
                    ApiTokenRefreshMiddleware.TokenUpdate(HttpContext, response, APIcookieName);
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Response.Cookies.Delete(APIcookieName);
                        return Json(new
                        {
                            success = false,
                            authExpired = true
                        });
                    }
                    if (response.IsSuccessStatusCode)
                    {
                        string resultMessage = response.Content.ReadAsStringAsync().Result;
                        var companyData = JsonConvert.DeserializeObject<object>(resultMessage);
                        return Json(new { success = true, data = companyData });
                    }
                    else
                    {
                        return Json(new { success = false, message = "API call failed: " + response.StatusCode });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GetAddRejList(string ipo_code,
                                string appl_no,
                                string order_no,
                                string pan_no,
                                string flag)
        {
            urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "GetAddRejList";
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value.ToString() + "_" + User.FindFirst(ClaimTypes.Role)?.Value.ToString();
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var query = HttpUtility.ParseQueryString(string.Empty);
                    query["ipo_code"] = ipo_code;
                    query["appl_no"] = appl_no;
                    query["order_no"] = order_no;
                    query["pan_no"] = pan_no;
                    query["flag"] = flag;
                    string url = urlstring + "?" + query.ToString();
                    var response = client.GetAsync(url).Result;
                    ApiTokenRefreshMiddleware.TokenUpdate(HttpContext, response, APIcookieName);
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Response.Cookies.Delete(APIcookieName);
                        return Json(new
                        {
                            success = false,
                            authExpired = true
                        });
                    }
                    if (response.IsSuccessStatusCode)
                    {
                        string resultMessage = response.Content.ReadAsStringAsync().Result;
                        var companyData = JsonConvert.DeserializeObject<object>(resultMessage);
                        return Json(new { success = true, data = companyData });
                    }
                    else
                    {
                        return Json(new { success = false, message = "API call failed: " + response.StatusCode });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<JsonResult> saveAddRejDetails([FromBody] RejectionModel mymodel)
        {
            var urlstring = _configuration.GetSection("Appsettings")["apiurl"] + "saveAddRejDetails";
            DataSet result = new DataSet();
            string post_data = "";
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value.ToString() + "_" + User.FindFirst(ClaimTypes.Role)?.Value.ToString();
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    string userCode = User.FindFirst(ClaimTypes.Name)?.Value.ToString();
                    if (!string.IsNullOrEmpty(userCode))
                    {
                        client.DefaultRequestHeaders.Add("User_Code", userCode);
                    }
                    var json = JsonConvert.SerializeObject(mymodel);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(urlstring, content);
                    ApiTokenRefreshMiddleware.TokenUpdate(HttpContext, response, APIcookieName);
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Response.Cookies.Delete(APIcookieName);
                        return Json(new
                        {
                            success = false,
                            authExpired = true
                        });
                    }
                    Stream data = response.Content.ReadAsStreamAsync().Result;
                    StreamReader reader = new StreamReader(data);
                    post_data = reader.ReadToEnd();
                    string _data1 = JsonConvert.DeserializeObject<string>(post_data);
                    result = JsonConvert.DeserializeObject<DataSet>(_data1);
                    string _data = JsonConvert.SerializeObject(result.Tables[0]);
                    return Json(new { _data });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        [HttpPost]
        public async Task<JsonResult> Getrulecodesdf_()
        {
            urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"])
                + "Getrulecode";

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value + "_" + User.FindFirst(ClaimTypes.Role)?.Value;
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var response = await client.PostAsync(urlstring, null);
                    var responseString = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<RunRulecodeModel>>(responseString);
                    return Json(new
                    {
                        success = true,
                        data = result
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        public async Task<JsonResult> Getrulecode(string ipo_code)
        {
            string urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "Getrulecode";
            urlstring += "?ipo_code=" + Uri.EscapeDataString(ipo_code);
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    string APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value + "_" + User.FindFirst(ClaimTypes.Role)?.Value;
                    string token = Request.Cookies[APIcookieName];
                    if (string.IsNullOrEmpty(token))
                    {
                        return Json(new { success = false, authExpired = true });
                    }

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var content = new StringContent("", Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(urlstring, content);
                    ApiTokenRefreshMiddleware.TokenUpdate(HttpContext, response, APIcookieName);
                    // 🔐 Unauthorized
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Response.Cookies.Delete(APIcookieName);
                        return Json(new { success = false, authExpired = true });
                    }

                    // ✅ Success
                    if (response.IsSuccessStatusCode)
                    {
                        var resultMessage = await response.Content.ReadAsStringAsync();
                        var companyData = JsonConvert.DeserializeObject<object>(resultMessage);
                        return Json(new { success = true, data = companyData });
                    }

                    // ❌ Failure
                    return Json(new
                    {
                        success = false,
                        message = "API call failed: " + response.StatusCode
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public JsonResult InsRightsEntitlement(string offer_code)
        {
            urlstring = Convert.ToString(
                _configuration.GetSection("Appsettings")["apiurl"]
            ) + "InsRightsEntitlement";

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;

                    APIcookieName = "APItoken-" +
                        User.FindFirst(ClaimTypes.Name)?.Value.ToString() +
                        "_" +
                        User.FindFirst(ClaimTypes.Role)?.Value.ToString();

                    string token = Request.Cookies[APIcookieName];

                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);

                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json")
                    );

                    string url = urlstring +
                                 "?offer_code=" +
                                 Uri.EscapeDataString(offer_code);

                    var response = client.PostAsync(url,null).Result;

                    ApiTokenRefreshMiddleware.TokenUpdate(
                        HttpContext,
                        response,
                        APIcookieName
                    );

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Response.Cookies.Delete(APIcookieName);

                        return Json(new
                        {
                            success = false,
                            authExpired = true
                        });
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        string resultMessage =
                            response.Content.ReadAsStringAsync().Result;

                        var result =
                            JsonConvert.DeserializeObject<dynamic>(resultMessage);

                        return Json(new
                        {
                            success = true,
                            result = result.result.ToString()
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            success = false,
                            message = "API call failed: " +
                                      response.StatusCode
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        [HttpGet]
        public IActionResult Export_ri_allotment_bo(string offer_code)
        {
            string urlstring = Convert.ToString(
                _configuration.GetSection("Appsettings")["apiurl"]) + "Export_ri_allotment_bo";

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;

                    string APIcookieName =
                        "APItoken-" +
                        User.FindFirst(ClaimTypes.Name)?.Value + "_" +
                        User.FindFirst(ClaimTypes.Role)?.Value;

                    string token = Request.Cookies[APIcookieName];

                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);

                    var response = client.GetAsync(urlstring + "?offer_code=" + offer_code).Result;
                    ApiTokenRefreshMiddleware.TokenUpdate(HttpContext, response, APIcookieName);
                    if (!response.IsSuccessStatusCode)
                        return Problem("API call failed");

                    string resultMessage = response.Content.ReadAsStringAsync().Result;

                    var ds = JsonConvert.DeserializeObject<DataSet>(resultMessage);

                    var stream = new MemoryStream();

                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
                    {
                        // =========================
                        // 📄 FILE 1 → CDSL
                        // =========================
                        var entry1 = archive.CreateEntry("ri_output_CDSL.txt");

                        using (var writer = new StreamWriter(entry1.Open()))
                        {
                            writer.WriteLine(
                                "DP-ID".PadRight(10) +
                                "CLNT-ID".PadRight(10) +
                                "ALLOTED QUANTITY".PadRight(150)
                            );

                            foreach (DataRow row in ds.Tables[0].Rows)
                            {
                                writer.WriteLine(
                                    (row["dp_id"]?.ToString() ?? "").PadRight(10) +
                                    (row["client_id"]?.ToString() ?? "").PadRight(10) +
                                    (row["raw_entitlement_qty"]?.ToString() ?? "").PadRight(150)
                                );
                            }
                        }

                        // =========================
                        // 📄 FILE 2 → NSDL
                        // =========================
                        var entry2 = archive.CreateEntry("ri_output_NSDL.txt");

                        using (var writer = new StreamWriter(entry2.Open()))
                        {
                            writer.WriteLine(
                                "DP-ID".PadRight(10) +
                                "CLNT-ID".PadRight(10) +
                                "ALLOTED QUANTITY".PadRight(150)
                            );

                            foreach (DataRow row in ds.Tables[1].Rows)
                            {
                                writer.WriteLine(
                                    (row["dp_id"]?.ToString() ?? "").PadRight(10) +
                                    (row["client_id"]?.ToString() ?? "").PadRight(10) +
                                    (row["raw_entitlement_qty"]?.ToString() ?? "").PadRight(150)
                                );
                            }
                        }
                    }

                    stream.Position = 0;

                    return File(stream, "application/zip", "ri_allotment.zip");
                }
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetRightsEntitlement(string offer_code)
        {
            urlstring = Convert.ToString(
                _configuration.GetSection("Appsettings")["apiurl"]
            ) + "GetRightsEntitlement";

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;

                    APIcookieName = "APItoken-" +
                        User.FindFirst(ClaimTypes.Name)?.Value?.ToString() +
                        "_" +
                        User.FindFirst(ClaimTypes.Role)?.Value?.ToString();

                    string token = Request.Cookies[APIcookieName];

                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);

                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json")
                    );

                    string url = urlstring +
                                 "?offer_code=" +
                                 Uri.EscapeDataString(offer_code);

                    // API call - ASYNC
                    var response = await client.GetAsync(url);

                    ApiTokenRefreshMiddleware.TokenUpdate(
                        HttpContext,
                        response,
                        APIcookieName
                    );

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Response.Cookies.Delete(APIcookieName);

                        return Json(new
                        {
                            success = false,
                            authExpired = true
                        });
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "API call failed: " +
                                      response.StatusCode
                        });
                    }

                    // Read API response - ASYNC
                    string resultMessage =
                        await response.Content.ReadAsStringAsync();

                    // Deserialize API response
                    JObject apiResult =
                        JsonConvert.DeserializeObject<JObject>(
                            resultMessage
                        );

                    if (apiResult == null)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Invalid response received from API."
                        });
                    }

                    // Convert summary
                    var summary =
                        apiResult["summary"]?
                            .ToObject<List<List<object>>>()
                        ?? new List<List<object>>();

                    // Convert details
                    var details =
                        apiResult["details"]?
                            .ToObject<List<List<object>>>()
                        ?? new List<List<object>>();

                    // Convert details
                    var disable_flag =
                        apiResult["disable_flag"]?
                            .ToObject<List<List<object>>>()
                        ?? new List<List<object>>();

                    return Json(new
                    {
                        success = true,
                        data = new
                        {
                            summary = summary,
                            details = details,
                            disable_flag = disable_flag
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

    }
}



public class RunDatasetModel
        {
            public int Id { get; set; }
            public string bankName { get; set; }
            public string Category { get; set; }
            public string Status { get; set; }
            public string LastSyncDate { get; set; }
            public string LastSyncStatus { get; set; }
        }

public class RunRulecodeModel
{
    public int rule_code { get; set; }
    public string rule_name { get; set; }
}
