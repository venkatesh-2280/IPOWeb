using DocumentFormat.OpenXml.EMMA;
using IPOWeb.Models;
using iTextSharp.text.pdf.qrcode;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace IPOWeb.Controllers
{
    public class IssueSetupController : Controller
    {
        public IActionResult IssueSetup(string mode)
        {
            ViewBag.Mode = mode;
            ViewBag.HideIPO = true;
            ViewBag.IssueStatus = "Draft";                
            ViewBag.IssueCode = "<<Client Code>>"; 
            return View();
        }

        private IConfiguration _configuration;
        public IssueSetupController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        string urlstring = "";
        string APIcookieName = "";

        [HttpPost]
        public JsonResult OfferType([FromBody] Qcdgridread objgridread)
        {
            urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "OfferType";
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value.ToString() + "_" + User.FindFirst(ClaimTypes.Role)?.Value.ToString();
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var json = JsonConvert.SerializeObject(objgridread);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = client.PostAsync(urlstring, content).Result;
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

                        // Deserialize the JSON string into an object
                        var companyData = JsonConvert.DeserializeObject<object>(resultMessage);

                        // Return that object directly, not as a string
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
        public JsonResult OfferHeaderSave([FromBody] OfferHeaderModel objOfferHeader)
        {
            urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "SetOfferHeader";
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value.ToString() + "_" + User.FindFirst(ClaimTypes.Role)?.Value.ToString();
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var json = JsonConvert.SerializeObject(objOfferHeader);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = client.PostAsync(urlstring, content).Result;
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
        public JsonResult OfferFetch(string client_code, string offer_code)
        {
            urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "GetOfferFetch";
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value.ToString() + "_" + User.FindFirst(ClaimTypes.Role)?.Value.ToString();
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    string url = urlstring + "?client_code=" + client_code + "&offer_code=" + offer_code;
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
        public JsonResult OfferDetailsSave([FromBody] OfferDetailsModel objOfferDetails)
        {
            urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "SetOfferDetail";
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value.ToString() + "_" + User.FindFirst(ClaimTypes.Role)?.Value.ToString();
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var json = JsonConvert.SerializeObject(objOfferDetails);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = client.PostAsync(urlstring, content).Result;
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
        public JsonResult OfferBankersSave([FromBody] OfferBankerModel objOfferBanker)
        {
            urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "SetOfferBankers";
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value.ToString() + "_" + User.FindFirst(ClaimTypes.Role)?.Value.ToString();
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var json = JsonConvert.SerializeObject(objOfferBanker);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = client.PostAsync(urlstring, content).Result;
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
                    if (response.IsSuccessStatusCode) {
                        string resultMessage = response.Content.ReadAsStringAsync().Result;
                        var companyData = JsonConvert.DeserializeObject<object>(resultMessage);
                        return Json(new { success = true, data = companyData });
                    } else {
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
        public JsonResult OfferStackHolderSave([FromBody] OfferStackModel objOfferStackModel)
        {
            urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "SetOfferStack";
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value.ToString() + "_" + User.FindFirst(ClaimTypes.Role)?.Value.ToString();
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var json = JsonConvert.SerializeObject(objOfferStackModel);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = client.PostAsync(urlstring, content).Result;
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
        public JsonResult saveOfferMilstone([FromBody] MilestoneModel objMilestoneModel)
        {
            urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "SetOffermiles";
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value.ToString() + "_" + User.FindFirst(ClaimTypes.Role)?.Value.ToString();
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var json = JsonConvert.SerializeObject(objMilestoneModel);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = client.PostAsync(urlstring, content).Result;
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
        public JsonResult saveCategory([FromBody] CategoryModel objCategoryModel)
        {
            urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "SetOfferCategory";
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value.ToString() + "_" + User.FindFirst(ClaimTypes.Role)?.Value.ToString();
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var json = JsonConvert.SerializeObject(objCategoryModel);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = client.PostAsync(urlstring, content).Result;
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
        public JsonResult GetAuditTrail(string ipo_code)
        {
            urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "GetAuditTrail";
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

   
        [HttpGet]
        public JsonResult Getifscdetails(string ifsc_search)
        {
            urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "Getifscdetails";
            try
            {
                DataSet result = new DataSet();
                string post_data = "";
                using (var client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    APIcookieName = "APItoken-" + User.FindFirst(ClaimTypes.Name)?.Value.ToString() + "_" + User.FindFirst(ClaimTypes.Role)?.Value.ToString();
                    string token = Request.Cookies[APIcookieName];
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    string url = urlstring + "?&ifsc_search=" + ifsc_search;
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
                        Stream data = response.Content.ReadAsStreamAsync().Result;
                        StreamReader reader = new StreamReader(data);
                        post_data = reader.ReadToEnd();
                        string _data1 = JsonConvert.DeserializeObject<string>(post_data);
                        result = JsonConvert.DeserializeObject<DataSet>(_data1);
                        string _data = JsonConvert.SerializeObject(result.Tables[0]);
                        return Json(new { _data });
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


    }

}
