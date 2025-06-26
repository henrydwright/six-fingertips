using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure;

namespace SixFingertips.Services;

public class AgentService
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly MetricsQueryClient _metricsClient;
    private readonly string _modelDeploymentName;
    private readonly string _foundryResourceId;
    private readonly IWebHostEnvironment _webHostEnvironment;
    // costs accurate as of 22/06/2025 from https://azure.microsoft.com/en-us/pricing/details/cognitive-services/openai-service/?cdn=disable
    private const double COST_PER_1M_INPUT_TOKENS = 0.82;
    private const double COST_PER_1M_OUTPUT_TOKENS = 3.27;
    private const double PROJECT_BUDGET = 20.00;

    public AgentService(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;

        var endpoint = configuration["AzureAI:Endpoint"] 
            ?? throw new ArgumentNullException("AzureAI:Endpoint configuration is missing");
        _modelDeploymentName = configuration["AzureAI:ModelDeploymentName"] 
            ?? throw new ArgumentNullException("AzureAI:ModelDeploymentName configuration is missing");
        _foundryResourceId = configuration["AzureAI:ResourceID"]
            ?? throw new ArgumentNullException("AzureAI:ResourceID configuration is missing");

        DefaultAzureCredential credential = new DefaultAzureCredential();

        _metricsClient = new MetricsQueryClient(credential);
        _agentsClient = new PersistentAgentsClient(endpoint, credential);
    }

    public async Task<LifetimeUsage> GetLifetimeTokenUsageAsync()
    {
        string [] metricNames = new[] {"ProcessedPromptTokens", "GeneratedTokens"};
        try {
            Response<MetricsQueryResult> results = await _metricsClient.QueryResourceAsync(
                _foundryResourceId,
                metricNames,
                new MetricsQueryOptions {
                    Aggregations = {MetricAggregationType.Total},
                    TimeRange = new QueryTimeRange(new TimeSpan(60,0,0,0)),
                    Granularity = new TimeSpan(1,0,0,0)
                } );

            MetricTimeSeriesElement promptTokensResult = results.Value.GetMetricByName("ProcessedPromptTokens").TimeSeries[0];
            MetricTimeSeriesElement completionTokenResult = results.Value.GetMetricByName("GeneratedTokens").TimeSeries[0];
            int promptTokens = 0;
            int completionTokens = 0;
            for (int i = 0; i < promptTokensResult.Values.Count; i++) {
                promptTokens += (int)(promptTokensResult.Values[i].Total ?? 0.0);
                completionTokens += (int)(completionTokenResult.Values[i].Total ?? 0.0);
            }
            return new LifetimeUsage(promptTokens, completionTokens);

        } catch (Exception ex) {
            throw new Exception("Error retrieving lifetime token usage", ex);
        }
    }

    public class LifetimeUsage 
    {
            public LifetimeUsage(int promptTokens, int completionTokens)
            {
                TotalPromptTokens = promptTokens;
                TotalCompletionTokens = completionTokens;
                double TotalCost = ((double)promptTokens / 1000000 * COST_PER_1M_INPUT_TOKENS) + ((double)completionTokens / 1000000 * COST_PER_1M_OUTPUT_TOKENS);
                PercentProjectBudgetUsed = TotalCost / PROJECT_BUDGET;
            }
            public int TotalPromptTokens { get; }
            public int TotalCompletionTokens { get; }
            public double PercentProjectBudgetUsed { get; }
    }

    public class AgentResponse
    {
        public AgentResponse(string agentResponseText, LifetimeUsage agentResourceUsage) {
            AgentResponseText = agentResponseText;
            AgentLifetimeResourceUsage = agentResourceUsage;
        }
        public string AgentResponseText {get;}
        public LifetimeUsage AgentLifetimeResourceUsage {get;}
    }

    public async Task<AgentResponse> ProcessUserInputAsync(string userInput)
    {
        try
        {
            // Check current usage levels
            var lifetimeUsage = await GetLifetimeTokenUsageAsync();
            if (lifetimeUsage.PercentProjectBudgetUsed > 1.0) {
                throw new Exception("Total AI token spend on this free project has been exceeded.");
            }

            // Read the OpenAPI spec file
            var specPath = Path.Combine(_webHostEnvironment.WebRootPath, "fingertips_api_spec_subset.json");
            var openApiSpec = await File.ReadAllBytesAsync(specPath);

            // Create OpenAPI tool definition
            var openApiTool = new OpenApiToolDefinition(
                name: "fingertips_api_reduced",
                description: "Access the Fingertips public health dataset API to retrieve health indicators and data",
                spec: BinaryData.FromBytes(openApiSpec),
                openApiAuthentication: new OpenApiAnonymousAuthDetails()
            );

            var codeInterpreterTool = new CodeInterpreterToolDefinition();

            // Create a new agent with the OpenAPI tool
            var agentResponse = await _agentsClient.Administration.CreateAgentAsync(
                model: _modelDeploymentName,
                name: "Fingertips Health Data Assistant (v3.1)",
                instructions: "You are a helpful assistant that can access and analyze public health data from the Fingertips dataset. Use the fingertips_api_reduced tool to retrieve data when needed. "+
                "The area type IDs available are: 7 - GP Practice; 170, 173 and 180 - Council; 54, 56, 58, 60 and 63 - CCG or sub-ICB location; Primary Care Network (PCN) - 204",
                tools: new List<ToolDefinition> { openApiTool, codeInterpreterTool }
            );
            var agent = agentResponse.Value;

            // Create a new thread
            var threadResponse = await _agentsClient.Threads.CreateThreadAsync();
            var thread = threadResponse.Value;

            // Create a message with the user's input
            await _agentsClient.Messages.CreateMessageAsync(
                thread.Id,
                MessageRole.User,
                userInput);

            // Create and execute a run
            var run = await _agentsClient.Runs.CreateRunAsync(
                thread.Id,
                agent.Id);

            // Poll until the run is complete
            do
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                var runResponse = await _agentsClient.Runs.GetRunAsync(thread.Id, run.Value.Id);
                run = runResponse;
            }
            while (run.Value.Status == RunStatus.Queued || run.Value.Status == RunStatus.InProgress);

            if (run.Value.Status != RunStatus.Completed)
            {
                throw new Exception($"Run failed with status {run.Value.Status}: {run.Value.LastError?.Message}");
            }

            // Get the messages from the thread
            var messages = _agentsClient.Messages.GetMessages(thread.Id);

            // This is where the tool use would be returned, if the API actually worked

            // var runSteps = _agentsClient.Runs.GetRunSteps(run.Value);
            // foreach (RunStep rs in runSteps.Reverse()) {
            //     var stepDetails = rs.StepDetails;
            //     Console.WriteLine(stepDetails.ToString());
            //     if (stepDetails is RunStepToolCallDetails) {
            //         RunStepToolCallDetails toolCallDetails = (RunStepToolCallDetails)stepDetails;
            //         foreach (RunStepToolCall toolCall in toolCallDetails.ToolCalls) {
            //             if (toolCall is RunStepOpenAPIToolCall) {
            //                 RunStepOpenAPIToolCall openApiToolCall = (RunStepOpenAPIToolCall)toolCall;
            //                 Console.WriteLine(openApiToolCall.OpenAPI.Keys.ToString());
            //             }
            //         }
            //     }
            // }
            
            // Get the first message (which should be the assistant's response)
            var firstMessage = messages.First();

            // Tidy up the agents by deleting this one
            await _agentsClient.Administration.DeleteAgentAsync(agent.Id);

            if (firstMessage.ContentItems.First().GetType() == typeof(MessageTextContent))
            {
                var msgTextContent = firstMessage.ContentItems.First() as MessageTextContent;
                return new AgentResponse(msgTextContent?.Text ?? "No response received", lifetimeUsage);
            }
            else
            {
                return new AgentResponse("No response received", lifetimeUsage);
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error getting agent response for user input", ex);
        }
    }
} 