using System.Threading.Tasks;

namespace SixFingertips.Services
{
    public interface IUsageMetricsService
    {
        Task<UsageMetricsService.LifetimeUsage> GetLifetimeTokenUsageAsync();
    }
}
