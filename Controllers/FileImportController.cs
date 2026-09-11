using DocumentFormat.OpenXml.Bibliography;
using IPOWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace IPOWeb.Controllers
{
    public class FileImportController : Controller
    {
        public IActionResult FileImport()
        {           
            return View();
        }

        private IConfiguration _configuration;
        public FileImportController(IConfiguration configuration)
        {

            _configuration = configuration;
        }
        string urlstring = "";
        string APIcookieName = "";

        [HttpGet]
        public async Task<JsonResult> GetDatasetList()
        {
            urlstring = _configuration.GetSection("Appsettings")["connector_api"] + "Pipeline/GetDataset_list";

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value + "_" + User.FindFirst(ClaimTypes.Role)?.Value;
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var response = await client.GetAsync(urlstring);
                    var responseString = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<FileImportModel>>(responseString);
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

        [HttpPost]
        public async Task<JsonResult> GetPipelinetList()
        {
            //urlstring = _configuration.GetSection("Appsettings")["connector_api"]
            //            + "Pipeline/getPipelinelistData?dataset=" + dataset;

            urlstring = _configuration.GetSection("Appsettings")["connector_api"]
                        + "Pipeline/getpipelines";

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value + "_" + User.FindFirst(ClaimTypes.Role)?.Value;
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var response = await client.GetAsync(urlstring);
                    var responseString = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<PipelineModel>>(responseString);
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

        [HttpPost]
        public async Task<JsonResult> ImportData(IFormFile file, string pipeline_code, string initiated_by, string dataset_code, string ref_bank_data)
        {
            try
            {
                if (file == null)
                {
                    return Json(new { success = false, message = "File not received" });
                }

                var refBankList = JsonConvert.DeserializeObject<List<RefBankModel>>(ref_bank_data);
                // Example: get first item
                var reference_no = refBankList[0].reference_no;
                var bank_code = refBankList[0].bank_code;
                var asba_flag = refBankList[0].asba_flag;

                string parameterJson = JsonConvert.SerializeObject(refBankList);

                string urlstring = _configuration.GetSection("Appsettings")["connector_api"]
                    + "Pipeline/NewScheduler?pipeline_code=" + pipeline_code
                    + "&initiated_by=" + initiated_by
                    + "&dataset_code=" + dataset_code
                    + "&parameter=" + Uri.EscapeDataString(parameterJson);

                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;

                    APIcookieName = "APItoken-" + User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                                  + "_" + User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    using (var content = new MultipartFormDataContent())
                    {
                        using (var stream = file.OpenReadStream())
                        {
                            var fileContent = new StreamContent(stream);
                            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                            content.Add(fileContent, "file", file.FileName);

                            var response = await client.PostAsync(urlstring, content);

                            var responseString = await response.Content.ReadAsStringAsync();

                            return Json(new
                            {
                                success = response.IsSuccessStatusCode,
                                data = responseString
                            });
                        }
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

        [HttpPost]
        public async Task<JsonResult> BankDetails()
        {
            string urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "BankDetails";

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
        public async Task<JsonResult> datasethistory([FromBody] FileInfoRequest req)
        {
            DataSet ds = new DataSet();
            string urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "fileinfo";

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

                    // 🔹 Send request
                    var json = JsonConvert.SerializeObject(req);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(urlstring, content);

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Response.Cookies.Delete(APIcookieName);
                        return Json(new { success = false, authExpired = true });
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "API call failed: " + response.StatusCode
                        });
                    }

                    // 🔹 Read API response
                    var resultMessage = await response.Content.ReadAsStringAsync();

                    if (string.IsNullOrWhiteSpace(resultMessage))
                    {
                        return Json(new { success = false, message = "Empty response from API" });
                    }

                    try
                    {
                        var actualJson = JsonConvert.DeserializeObject<string>(resultMessage);
                        ds = JsonConvert.DeserializeObject<DataSet>(actualJson);
                    }
                    catch (Exception ex)
                    {
                        return Json(new { success = false, message = "JSON Parse Error: " + ex.Message });
                    }

                    var list = ds.Tables.Count > 0 ? ConvertToDatasetJobList(ds.Tables[0]) : new List<DatasetJob>();
                    return Json(new
                    {
                        success = true,
                        data = list
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, data = "" });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetDatasettList(string Pipeline_code)
        {
            string urlstring =
                Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"])
                + "getdatasetPipeline?Pipeline_code=" + Pipeline_code;

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;

                    APIcookieName = "APItoken-" +
                                    User.FindFirst(ClaimTypes.Name)?.Value + "_" +
                                    User.FindFirst(ClaimTypes.Role)?.Value;

                    string token = Request.Cookies[APIcookieName];

                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);

                    var response = await client.GetAsync(urlstring);

                    response.EnsureSuccessStatusCode();

                    var responseString = await response.Content.ReadAsStringAsync();

                    // ✅ Correct deserialization
                    var result =
                        JsonConvert.DeserializeObject<DatasetResponse>(responseString);

                    return Json(new
                    {
                        success = true,
                        data = result.Table
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


        [HttpGet]
        public async Task<JsonResult> GetTotal(string ipocodes)
        {
            string urlstring =
                Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"])
                + "gettotal?ipocodes=" + ipocodes;

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;

                    APIcookieName = "APItoken-" +
                                    User.FindFirst(ClaimTypes.Name)?.Value + "_" +
                                    User.FindFirst(ClaimTypes.Role)?.Value;

                    string token = Request.Cookies[APIcookieName];

                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);

                    var response = await client.GetAsync(urlstring);

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
                        string resultMessage = await response.Content.ReadAsStringAsync();

                        Console.WriteLine("GetTotal API Response: " + resultMessage);

                        return Json(new
                        {
                            success = true,
                            data = resultMessage
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            success = false,
                            message = "API call failed: " + response.StatusCode
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

        public static List<DatasetJob> ConvertToDatasetJobList(DataTable dt)
        {
            var list = new List<DatasetJob>();

            foreach (DataRow dr in dt.Rows)
            {
                list.Add(new DatasetJob
                {
                    dataset_name = dr["dataset_name"]?.ToString(),
                    job_status = dr["job_status"]?.ToString(),
                    job_remark = dr["job_remark"] != DBNull.Value ? Convert.ToInt32(dr["job_remark"]) : 0,
                    job_initiated_by = dr["job_initiated_by"]?.ToString(),
                    start_date = dr["start_date"] != DBNull.Value ? Convert.ToDateTime(dr["start_date"]) : DateTime.MinValue,
                    //reference_no = dr["reference_no"]?.ToString()
                });
            }

            return list;
        }

        public class FileInfoRequest
        {
            public string ipo_code { get; set; }
            public string dataset_code { get; set; }
        }
        public class DatasetJob
        {
            public string dataset_name { get; set; }
            public string job_status { get; set; }
            public int job_remark { get; set; }
            public string job_initiated_by { get; set; }
            public DateTime start_date { get; set; }
            // public string reference_no { get; set; }
        }

        public class DatasetResponse
        {
            public List<FileImportModel> Table { get; set; }
        }
    }
}
