using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaudeUsesDetails.Pages;

public class DailyCostsModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "Daily Costs";
        ViewData["Active"] = "DailyCosts";
        ViewData["Subtitle"] = "Token usage and cost breakdown by day";
    }
}
