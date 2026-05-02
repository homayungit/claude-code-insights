using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaudeUsesDetails.Pages;

public class ProjectsModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "Projects";
        ViewData["Active"] = "Projects";
        ViewData["Subtitle"] = "Claude usage grouped by project";
    }
}
