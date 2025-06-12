using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SixFingertips.Services;
using Markdig;

namespace SixFingertips.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly AgentService _agentService;

    [BindProperty]
    public string InputText { get; set; } = string.Empty;

    [BindProperty]
    public string? AgentResponse { get; set; }

    [BindProperty]
    public string? AgentResponseHtml { get; set; }

    public IndexModel(ILogger<IndexModel> logger, AgentService agentService)
    {
        _logger = logger;
        _agentService = agentService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!string.IsNullOrEmpty(InputText))
        {
            try
            {
                _logger.LogInformation("Processing user input: {Text}", InputText);
                AgentResponse = await _agentService.ProcessUserInputAsync(InputText);
                _logger.LogInformation("Received agent response: {Response}", AgentResponse);
                
                // Convert Markdown to HTML
                if (!string.IsNullOrEmpty(AgentResponse))
                {
                    var pipeline = new MarkdownPipelineBuilder()
                        .UseAdvancedExtensions()
                        .DisableHtml() // For security, disable raw HTML
                        .Build();
                    AgentResponseHtml = Markdown.ToHtml(AgentResponse, pipeline);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing user input");
                ModelState.AddModelError(string.Empty, "An error occurred while processing your request. Please try again.");
            }
        }
        return Page();
    }
}
