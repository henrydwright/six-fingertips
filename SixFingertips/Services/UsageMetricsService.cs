using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure;
using Microsoft.Extensions.Logging;

namespace SixFingertips.Services;

public class UsageMetricsService
{
    private readonly MetricsQueryClient _metricsClient;
    private readonly string _foundryResourceId;
    private readonly ILogger<UsageMetricsService> _logger;
    // costs accurate as of 22/06/2025 from https://azure.microsoft.com/en-us/pricing/details/cognitive-services/openai-service/?cdn=disable
    private const double COST_PER_1M_INPUT_TOKENS = 0.82;
    private const double COST_PER_1M_OUTPUT_TOKENS = 3.27;
    private const double PROJECT_BUDGET = 20.00;

    public UsageMetricsService(IConfiguration configuration, ILogger<UsageMetricsService> logger)
    {
        _logger = logger;
        _foundryResourceId = configuration["AzureAI:ResourceID"]
            ?? throw new ArgumentNullException("AzureAI:ResourceID configuration is missing");
        var azureCredential = new DefaultAzureCredential();
        _metricsClient = new MetricsQueryClient(azureCredential);
    }

    public async Task<LifetimeUsage> GetLifetimeTokenUsageAsync()
    {
        string[] metricNames = new[] { "ProcessedPromptTokens", "GeneratedTokens" };
        try
        {
            Response<MetricsQueryResult> results = await _metricsClient.QueryResourceAsync(
                _foundryResourceId,
                metricNames,
                new MetricsQueryOptions
                {
                    Aggregations = { MetricAggregationType.Total },
                    TimeRange = new QueryTimeRange(new TimeSpan(60, 0, 0, 0)),
                    Granularity = new TimeSpan(1, 0, 0, 0)
                });

            MetricTimeSeriesElement promptTokensResult = results.Value.GetMetricByName("ProcessedPromptTokens").TimeSeries[0];
            MetricTimeSeriesElement completionTokenResult = results.Value.GetMetricByName("GeneratedTokens").TimeSeries[0];
            int promptTokens = 0;
            int completionTokens = 0;
            for (int i = 0; i < promptTokensResult.Values.Count; i++)
            {
                promptTokens += (int)(promptTokensResult.Values[i].Total ?? 0.0);
                completionTokens += (int)(completionTokenResult.Values[i].Total ?? 0.0);
            }
            return new LifetimeUsage(promptTokens, completionTokens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lifetime token usage");
            throw new Exception("Error retrieving lifetime token usage", ex);
        }
    }

    public class LifetimeUsage
    {
        public LifetimeUsage(int promptTokens, int completionTokens)
        {
            TotalPromptTokens = promptTokens;
            TotalCompletionTokens = completionTokens;
            double totalCost = ((double)promptTokens / 1000000 * COST_PER_1M_INPUT_TOKENS) + ((double)completionTokens / 1000000 * COST_PER_1M_OUTPUT_TOKENS);
            PercentProjectBudgetUsed = totalCost / PROJECT_BUDGET;
        }
        public int TotalPromptTokens { get; }
        public int TotalCompletionTokens { get; }
        public double PercentProjectBudgetUsed { get; }
    }
}
