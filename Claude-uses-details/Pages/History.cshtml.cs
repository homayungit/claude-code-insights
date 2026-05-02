using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaudeUsesDetails.Pages;

public class HistoryModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "History";
        ViewData["Active"] = "History";
        ViewData["Subtitle"] = "Full prompt audit log";
    }
}
