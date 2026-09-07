using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace IPOWeb.Controllers
{
    public class RIReportsSummaryController : Controller
    {
        public IActionResult RIReportsSummary()
        {
             return View();
        }

        private IConfiguration _configuration;
        public RIReportsSummaryController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        string urlstring = "";
        string APIcookieName = "";
        [HttpGet]
        public JsonResult Getbidfilecountsummary(string ipo_code)
        {
            urlstring = Convert.ToString(_configuration.GetSection("Appsettings")["apiurl"]) + "Getbidfilecountsummary";
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

    }
}
