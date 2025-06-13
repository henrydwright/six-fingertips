using Azure.AI.Agents.Persistent;
using Azure.Identity;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace SixFingertips.Services;

public class AgentService
{
    private readonly PersistentAgentsClient _client;
    private readonly string _modelDeploymentName;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AgentService(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
    {
        var endpoint = configuration["AzureAI:Endpoint"] 
            ?? throw new ArgumentNullException("AzureAI:Endpoint configuration is missing");
        _modelDeploymentName = configuration["AzureAI:ModelDeploymentName"] 
            ?? throw new ArgumentNullException("AzureAI:ModelDeploymentName configuration is missing");
        _webHostEnvironment = webHostEnvironment;

        _client = new PersistentAgentsClient(endpoint, new DefaultAzureCredential());
    }

    public async Task<string> ProcessUserInputAsync(string userInput)
    {
        try
        {
            // Read the OpenAPI spec file
            var specPath = Path.Combine(_webHostEnvironment.WebRootPath, "fingertips_api_spec.json");
            var openApiSpec = await File.ReadAllBytesAsync(specPath);

            // Create OpenAPI tool definition
            var openApiTool = new OpenApiToolDefinition(
                name: "fingertips_api",
                description: "Access the Fingertips public health dataset API to retrieve health indicators and data",
                spec: BinaryData.FromBytes(openApiSpec),
                openApiAuthentication: new OpenApiAnonymousAuthDetails()
            );

            // Create a new agent with the OpenAPI tool
            var agentResponse = await _client.Administration.CreateAgentAsync(
                model: _modelDeploymentName,
                name: "Fingertips Health Data Assistant",
                instructions: "You are a helpful assistant that can access and analyze public health data from the Fingertips dataset. Use the fingertips_api tool to retrieve data when needed.",
                tools: new List<ToolDefinition> { openApiTool }
            );
            var agent = agentResponse.Value;

            // Create a new thread
            var threadResponse = await _client.Threads.CreateThreadAsync();
            var thread = threadResponse.Value;

            // Create a message with the user's input
            await _client.Messages.CreateMessageAsync(
                thread.Id,
                MessageRole.User,
                userInput);

            // Create and execute a run
            var run = await _client.Runs.CreateRunAsync(
                thread.Id,
                agent.Id);

            // Poll until the run is complete
            do
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                var runResponse = await _client.Runs.GetRunAsync(thread.Id, run.Value.Id);
                run = runResponse;
            }
            while (run.Value.Status == RunStatus.Queued || run.Value.Status == RunStatus.InProgress);

            if (run.Value.Status != RunStatus.Completed)
            {
                throw new Exception($"Run failed with status {run.Value.Status}: {run.Value.LastError?.Message}");
            }

            // Get the messages from the thread
            var messages = _client.Messages.GetMessages(thread.Id);
            
            // Get the first message (which should be the assistant's response)
            var firstMessage = messages.First();
            if (firstMessage.ContentItems.First().GetType() == typeof(MessageTextContent))
            {
                var msgTextContent = firstMessage.ContentItems.First() as MessageTextContent;
                return msgTextContent?.Text ?? "No response received";
            }
            else
            {
                return "No response received";
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error processing user input", ex);
        }
    }
} 