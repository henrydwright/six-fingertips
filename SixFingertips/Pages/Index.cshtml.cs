using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SixFingertips.Services;

namespace SixFingertips.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly AgentService _agentService;

    [BindProperty]
    public string InputText { get; set; } = string.Empty;

    [BindProperty]
    public string? AgentResponse { get; set; }

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
