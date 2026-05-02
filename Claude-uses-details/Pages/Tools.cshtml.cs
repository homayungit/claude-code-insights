using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaudeUsesDetails.Pages;

public class ToolsModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "Tools";
        ViewData["Active"] = "Tools";
        ViewData["Subtitle"] = "Which Claude tools you use most";
    }
}
