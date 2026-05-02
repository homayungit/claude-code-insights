using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaudeUsesDetails.Pages;

public class IndexModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "Dashboard";
        ViewData["Active"] = "Dashboard";
        ViewData["Subtitle"] = "Overview of your Claude Code usage";
    }
}
