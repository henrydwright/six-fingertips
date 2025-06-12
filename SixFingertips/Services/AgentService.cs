using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Azure;

namespace SixFingertips.Services;

public class AgentService
{
    private readonly PersistentAgentsClient _client;
    private readonly string _modelDeploymentName;

    public AgentService(IConfiguration configuration)
    {
        var endpoint = configuration["AzureAI:Endpoint"] 
            ?? throw new ArgumentNullException("AzureAI:Endpoint configuration is missing");
        _modelDeploymentName = configuration["AzureAI:ModelDeploymentName"] 
            ?? throw new ArgumentNullException("AzureAI:ModelDeploymentName configuration is missing");

        _client = new PersistentAgentsClient(endpoint, new DefaultAzureCredential());
    }

    public async Task<string> ProcessUserInputAsync(string userInput)
    {
        try
        {
            // Create a new agent and store the ID
            var agentResponse = await _client.Administration.CreateAgentAsync(
                _modelDeploymentName);
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
            
            // Get the last message (which should be the assistant's response)
            var lastMessage = messages.First();
            if (lastMessage.ContentItems.First().GetType() == typeof(MessageTextContent))
            {
                var msgTextContent = lastMessage.ContentItems.First() as MessageTextContent;
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