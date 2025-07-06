using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure;
using Azure.Core;
using System.Text.Json;

namespace SixFingertips.Services;

public class AgentService
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly DefaultAzureCredential _azureCredential;
    private readonly string _agentsEndpoint;
    private readonly string _modelDeploymentName;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<AgentService> _logger;
    private readonly UsageMetricsService _usageMetricsService;

    public AgentService(IConfiguration configuration, IWebHostEnvironment webHostEnvironment, ILogger<AgentService> logger, UsageMetricsService usageMetricsService)
    {
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
        _usageMetricsService = usageMetricsService;

        _agentsEndpoint = configuration["AzureAI:Endpoint"]
            ?? throw new ArgumentNullException("AzureAI:Endpoint configuration is missing");
        _modelDeploymentName = configuration["AzureAI:ModelDeploymentName"]
            ?? throw new ArgumentNullException("AzureAI:ModelDeploymentName configuration is missing");

        _azureCredential = new DefaultAzureCredential();
        _agentsClient = new PersistentAgentsClient(_agentsEndpoint, _azureCredential);
    }

    // ...removed GetLifetimeTokenUsageAsync, now in UsageMetricsService...

    // ...LifetimeUsage class is now in UsageMetricsService...

    public class AgentResponse
    {
        public AgentResponse(string agentResponseText, UsageMetricsService.LifetimeUsage agentResourceUsage, List<ToolFunctionCall> agentApiCalls)
        {
            AgentResponseText = agentResponseText;
            AgentLifetimeResourceUsage = agentResourceUsage;
            AgentToolCalls = agentApiCalls;
        }
        public string AgentResponseText { get; }
        public UsageMetricsService.LifetimeUsage AgentLifetimeResourceUsage { get; }
        public List<ToolFunctionCall> AgentToolCalls { get; }
    }

    public class ToolFunctionCall
    {
        public ToolFunctionCall(string name, Dictionary<string, string> arguments, string output) {
            Name = name;
            Arguments = arguments;
            Output = output;
        }

        public string Name {get;}
        public Dictionary<string, string> Arguments {get;}
        public string Output {get;}

    }
    public async Task<AgentResponse> ProcessUserInputAsync(string userInput)
    {
        try
        {
            // Check current usage levels
            var lifetimeUsage = await _usageMetricsService.GetLifetimeTokenUsageAsync();
            if (lifetimeUsage.PercentProjectBudgetUsed > 1.0)
            {
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
            _logger.LogInformation("Creating agent");
            // Create a new agent with the OpenAPI tool
            var agentResponse = await _agentsClient.Administration.CreateAgentAsync(
                model: _modelDeploymentName,
                name: "Fingertips Health Data Assistant (v4.1c)",
                instructions: "You are a helpful assistant that can access and analyze public health data from the Fingertips dataset. Use the fingertips_api_reduced tool to retrieve data when needed. If provided with only an indicator name and you need the indicator ID, guess which profile ID it is most likely to fit into and see if it appears in the indicators for that profile. Use the code interpreter tool for any numerical calculations when needed (e.g. finding totals across age bands). CHECK THE SCHEMA for returned data so you interpret it correctly (e.g. for population summary array it is two genders, not two years of data). ALWAYS give your response in the following format: 'ANSWER:\n <answer to the user's question in natural language, using paragraphs for clarity if needed>\n\nWORKINGS: <list of tool calls made and chain of thought as bullet points in markdown>'\n" +
                "The area type IDs available are: GP Practice - 7; Council - 502; ICB - 221; CCG or sub-ICB location - 66, 167; Primary Care Network (PCN) - 204; Whole of England - 15. Profile IDs available are: 18 - Smoking; 32 - Obesity, Physical Activity and Nutrition; 87 - Alcohol; 84 - Dementia; 139 - Diabetes. Deprivation indexes are HIGHER for LESS deprived areas - 10 is least deprived, 1 is most deprived. DO NOT provide links to download files, images or graphs in your output.\n",
                tools: new List<ToolDefinition> { openApiTool, codeInterpreterTool }
            );
            var agent = agentResponse.Value;
            await Task.Delay(TimeSpan.FromMilliseconds(250));

            _logger.LogInformation("Creating thread");
            // Create a new thread
            var threadResponse = await _agentsClient.Threads.CreateThreadAsync();
            var thread = threadResponse.Value;

            _logger.LogInformation("Creating message and run");
            // Create a message with the user's input
            await _agentsClient.Messages.CreateMessageAsync(
                thread.Id,
                MessageRole.User,
                userInput);

            // Create and execute a run
            var run = await _agentsClient.Runs.CreateRunAsync(
                thread.Id,
                agent.Id);

            _logger.LogInformation("Waiting for run to complete");
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

            _logger.LogInformation("Run completed, getting messages");
            // Get the messages from the thread
            var messages = _agentsClient.Messages.GetMessages(thread.Id);

            // The SDK doesn't support looking into the function calls in the level of detail we need, so we're forced to use the REST API and
            //  look into the JSON by hand
            var apiCalls = new List<ToolFunctionCall>();

            HttpClient httpClient = new HttpClient();
            string token = _azureCredential.GetToken(new TokenRequestContext(scopes: new[] { "https://ai.azure.com/.default" })).Token;
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var runSteps = _agentsClient.Runs.GetRunSteps(run.Value);
            foreach (RunStep runStep in runSteps.Reverse())
            {
                try
                {
                    string getUrl = $"{_agentsEndpoint}/threads/{thread.Id}/runs/{run.Value.Id}/steps/{runStep.Id}?api-version=v1";
                    _logger.LogInformation("Getting run step directly from URL: {GetUrl}", getUrl);
                    HttpResponseMessage response = await httpClient.GetAsync(getUrl);
                    response.EnsureSuccessStatusCode();
                    JsonDocument jsonContent = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
                    JsonElement root = jsonContent.RootElement;
                    string stepType = root.GetProperty("type").ToString();
                    if (stepType == "tool_calls")
                    {
                        JsonElement.ArrayEnumerator toolCalls = root.GetProperty("step_details").GetProperty("tool_calls").EnumerateArray();
                        foreach (JsonElement toolCall in toolCalls)
                        {
                            string toolType = toolCall.GetProperty("type").ToString();
                            if (toolType == "openapi")
                            {
                                JsonElement functionDetails = toolCall.GetProperty("function");
                                string name = functionDetails.GetProperty("name").ToString();
                                JsonDocument argumentsDoc = JsonDocument.Parse(functionDetails.GetProperty("arguments").ToString());
                                Dictionary<string, string> arguments = new Dictionary<string, string>();
                                foreach (JsonProperty argument in argumentsDoc.RootElement.EnumerateObject())
                                {
                                    arguments.Add(argument.Name.ToString(), argument.Value.ToString());
                                }
                                string output = functionDetails.GetProperty("output").ToString();
                                apiCalls.Add(new ToolFunctionCall(name, arguments, output));
                            }
                            else if (toolType == "code_interpreter")
                            {
                                JsonElement codeInterpreterDetails = toolCall.GetProperty("code_interpreter");
                                Dictionary<string, string> arguments = new Dictionary<string, string>
                                {
                                    ["code"] = codeInterpreterDetails.GetProperty("input").ToString()
                                };
                                apiCalls.Add(new ToolFunctionCall("code_interpreter", arguments, ""));
                            }
                            else
                            {
                                throw new Exception("Unknown tool type encountered");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not parse API tool request: {Message}", ex.Message);
                }
            }

            // Get the first message (which should be the assistant's response)
            var firstMessage = messages.First();

            // Tidy up the agents by deleting this one
            _logger.LogInformation("Deleting agent");
            await _agentsClient.Administration.DeleteAgentAsync(agent.Id);

            var contentItems = firstMessage.ContentItems;
            string fullAgentResponse = "";

            foreach (MessageContent content in contentItems)
            {
                Type contentType = content.GetType();
                if (contentType == typeof(MessageTextContent) && content != null)
                {
                    MessageTextContent? textContent = content as MessageTextContent;
                    _logger.LogDebug("Processing text message");

                    fullAgentResponse += textContent?.Text ?? "";
                }
                else if (contentType == typeof(MessageImageFileContent) && content != null)
                {
                    MessageImageFileContent? imageContent = content as MessageImageFileContent;
                    _logger.LogDebug("Processing image message");
                    string fileId = imageContent?.FileId ?? "";
                    if (fileId == "")
                        continue;

                    string imageMarkdown = "";

                    PersistentAgentFileInfo imageFileDetails = (await _agentsClient.Files.GetFileAsync(fileId)).Value;
                    _logger.LogInformation("Downloading file {Filename} ({Size}b)", imageFileDetails.Filename, imageFileDetails.Size);
                    string fileType = imageFileDetails.Filename.Split(".")[1];
                    BinaryData image = (await _agentsClient.Files.GetFileContentAsync(fileId)).Value;
                    string imageBase64Contents = Convert.ToBase64String(image.ToArray());
                    if (fileType == "png")
                    {
                        imageMarkdown = $"![AI generated image](data:image/png;base64,{imageBase64Contents})";
                    }
                    else if (fileType == "jpg" || fileType == "jpeg")
                    {
                        imageMarkdown = $"![AI generated image](data:image/jpeg;base64,{imageBase64Contents})";
                    }
                    fullAgentResponse += "\r\n\r\n" + imageMarkdown + "\r\n\r\n";
                }
            }

            if (fullAgentResponse == "")
            {
                return new AgentResponse("No response recieved", lifetimeUsage, apiCalls);
            }
            else
            {
                return new AgentResponse(fullAgentResponse, lifetimeUsage, apiCalls);
            }

        }
        catch (Exception ex)
        {
            throw new Exception("Error getting agent response for user input", ex);
        }
    }
} 