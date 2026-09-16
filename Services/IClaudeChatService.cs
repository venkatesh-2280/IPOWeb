using System.Collections.Generic;
using System.Threading.Tasks;
using IPOWeb.Models;

namespace IPOWeb.Services
{
    public interface IClaudeChatService
    {
        Task<ChatAskResult> AskAsync(string userMessage, List<ChatTurn> priorTurns, string userCode, string roleCode);
    }
}
