using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SixFingertips.Services;
using Markdig;

namespace SixFingertips.Pages;

[ValidateAntiForgeryToken]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IAgentService _agentService;
    private readonly Dictionary<string, string> _friendlyFingertipsFunctionNames;

    // take an argument name and get a pair of friendly name for agument and lookup for friendly names for values it could hold
    private readonly Dictionary<string, KeyValuePair<string, Dictionary<string, string>>> _friendlyFingertipsArgumentNamesAndValues;

    [BindProperty]
    public string InputText { get; set; } = string.Empty;

    [BindProperty]
    public string? AgentResponse { get; set; }

    [BindProperty]
    public string? AgentResponseHtml { get; set; }

    [BindProperty]
    public string? PercentBudgetUsed { get; set; }

    [BindProperty]
    public string? TotalTokensUsed { get; set; }

    public List<AgentService.ToolFunctionCall>? ApiCalls { get; set; }

    public IndexModel(ILogger<IndexModel> logger, IAgentService agentService)
    {
        _logger = logger;
        _agentService = agentService;

        // should all this be hard coded? no! but it will do for a side project...
        _friendlyFingertipsFunctionNames = new Dictionary<string, string>
        {
            ["fingertips_api_reduced_Areas_SearchForAreaByNameAndType"] = "Area: Search for an area code",
            ["fingertips_api_reduced_Areas_GetParentAreas"] = "Area: Lookup parent(s)",
            ["fingertips_api_reduced_Areas_GetAreasByAreaCode"] = "Area: Lookup basic information",
            ["fingertips_api_reduced_Areas_GetChildAreas"] = "Area: Lookup children",
            ["fingertips_api_reduced_Areas_GetAreaAddress"] = "Area: Lookup address",
            ["fingertips_api_reduced_Data_GetQuinaryPopulation"] = "Data: Get GP population breakdown by age bands and sex",
            ["fingertips_api_reduced_Data_GetGPPracticeSummaryInformation"] = "Data: Get GP practice summary information",
            ["fingertips_api_reduced_Data_ListIndicatorDefinitionsByProfile"] = "Data: Lookup indicators by profile (collection) and area type",
            ["fingertips_api_reduced_Data_GetDataForSpecificAreaAndIndicator"] = "Data: Get indicator value for area",
            ["code_interpreter"] = "Processing: Execute Python code"
        };

        Dictionary<string, string> areaTypeIdLookup = new Dictionary<string, string>
        {
            ["7"] = "GP practice",
            ["502"] = "County council / Unitary authority",
            ["66"] = "CCG / sub-ICB location",
            ["167"] = "CCG / sub-ICB location",
            ["204"] = "Primary Care Network (PCN)",
            ["15"] = "Whole of England"
        };

        Dictionary<string, string> profileIdLookup = new Dictionary<string, string>
        {
            ["18"] = "Smoking",
            ["32"] = "Obesity, Physical Activity and Nutrition",
            ["84"] = "Dementia",
            ["87"] = "Alcohol",
            ["139"] = "Diabetes"
        };

        _friendlyFingertipsArgumentNamesAndValues = new Dictionary<string, KeyValuePair<string, Dictionary<string, string>>>
        {
            ["search_text"] = new KeyValuePair<string, Dictionary<string, string>>("Search Term", new Dictionary<string, string>()),
            ["area_type_ids"] = new KeyValuePair<string, Dictionary<string, string>>("ID(s) for type of area", areaTypeIdLookup),
            ["child_area_code"] = new KeyValuePair<string, Dictionary<string, string>>("Area code for child", new Dictionary<string, string>()),
            ["parent_area_type_ids"] = new KeyValuePair<string, Dictionary<string, string>>("ID for type of parent area to retrieve", areaTypeIdLookup),
            ["area_codes"] = new KeyValuePair<string, Dictionary<string, string>>("Area code(s)", new Dictionary<string, string>()),
            ["area_type_id"] = new KeyValuePair<string, Dictionary<string, string>>("ID for type of area", areaTypeIdLookup),
            ["parent_area_code"] = new KeyValuePair<string, Dictionary<string, string>>("Area code for parent", new Dictionary<string, string>()),
            ["profile_id"] = new KeyValuePair<string, Dictionary<string, string>>("ID for profile (curated selection of indicators)", profileIdLookup),
            ["area_code"] = new KeyValuePair<string, Dictionary<string, string>>("Area code", new Dictionary<string, string>()),
            ["data_point_offset"] = new KeyValuePair<string, Dictionary<string, string>>("Number of years back from latest available year to get data for", new Dictionary<string, string>()),
            ["indicator_ids"] = new KeyValuePair<string, Dictionary<string, string>>("ID for indicator", new Dictionary<string, string>())
        };

    }

    public void OnGet()
    {
    }

    public string GetFingertipsFunctionFriendlyName(string api_call_name)
    {
        return _friendlyFingertipsFunctionNames.GetValueOrDefault(api_call_name, api_call_name);
    }

    public string GetFingertipsArgumentNameFriendlyName(string argument_name)
    {
        return _friendlyFingertipsArgumentNamesAndValues.GetValueOrDefault(argument_name, new KeyValuePair<string, Dictionary<string, string>>(argument_name, new Dictionary<string, string>())).Key;
    }

    public string GetFingertipsArgumentValueFriendlyName(string argument_name, string argument_value)
    {
        Dictionary<string, string> argumentDictionary = _friendlyFingertipsArgumentNamesAndValues.GetValueOrDefault(argument_name, new KeyValuePair<string, Dictionary<string, string>>(argument_name, new Dictionary<string, string>())).Value;
        if (!argumentDictionary.ContainsKey(argument_value))
        {
            return argument_value;
        }
        else
        {
            return argument_value + " (" + argumentDictionary[argument_value] + ")";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!string.IsNullOrEmpty(InputText))
        {
            try
            {
                _logger.LogInformation("Processing user input: {Text}", InputText);
                var encapsulatedAgentResponse = await _agentService.ProcessUserInputAsync(InputText);
                AgentResponse = encapsulatedAgentResponse.AgentResponseText;
                PercentBudgetUsed = string.Format("{0:0.00}", encapsulatedAgentResponse.AgentLifetimeResourceUsage.PercentProjectBudgetUsed);
                TotalTokensUsed = (encapsulatedAgentResponse.AgentLifetimeResourceUsage.TotalCompletionTokens + encapsulatedAgentResponse.AgentLifetimeResourceUsage.TotalPromptTokens).ToString("N0");
                ApiCalls = encapsulatedAgentResponse.AgentToolCalls;
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
                ModelState.AddModelError(string.Empty, ex.StackTrace ?? "Detailed error information could not be retrieved.");
            }
        }
        return Page();
    }
}
