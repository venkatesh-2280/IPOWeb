using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IPOWeb.Models;
using IPOWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace IPOWeb.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private const string SessionKey = "ChatHistory";
        private const int MaxHistoryEntries = 10;

        private readonly IClaudeChatService _claudeChatService;

        public ChatController(IClaudeChatService claudeChatService)
        {
            _claudeChatService = claudeChatService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Ask([FromBody] ChatAskModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Message))
                return Json(new { success = false, message = "Please type a question." });

            var userCode = User.FindFirst(ClaimTypes.Name)?.Value;
            var roleCode = User.FindFirst(ClaimTypes.Role)?.Value;

            var historyJson = HttpContext.Session.GetString(SessionKey);
            var history = string.IsNullOrEmpty(historyJson)
                ? new List<ChatTurn>()
                : JsonConvert.DeserializeObject<List<ChatTurn>>(historyJson);

            var result = await _claudeChatService.AskAsync(model.Message, history, userCode, roleCode);

            if (result.AuthExpired)
                return Json(new { success = false, authExpired = true });

            history.Add(new ChatTurn { Role = "user", Content = model.Message });
            history.Add(new ChatTurn { Role = "assistant", Content = result.Reply });
            if (history.Count > MaxHistoryEntries)
                history = history.Skip(history.Count - MaxHistoryEntries).ToList();
            HttpContext.Session.SetString(SessionKey, JsonConvert.SerializeObject(history));

            return Json(new { success = result.Success, reply = result.Reply });
        }

        [HttpPost]
        public IActionResult ResetChat()
        {
            HttpContext.Session.Remove(SessionKey);
            return Json(new { success = true });
        }
    }
}
