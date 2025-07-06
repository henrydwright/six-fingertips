using System.Collections.Generic;
using System.Threading.Tasks;

namespace SixFingertips.Services
{
    public interface IAgentService
    {
        Task<AgentService.AgentResponse> ProcessUserInputAsync(string userInput);
    }
}
