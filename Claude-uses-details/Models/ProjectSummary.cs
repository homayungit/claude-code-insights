namespace ClaudeUsesDetails.Models;

public class ProjectSummary
{
    public string Name { get; set; } = "";
    public string ShortName { get; set; } = "";
    public int Messages { get; set; }
    public int Sessions { get; set; }
    public string? FirstSeen { get; set; }
    public string? LastSeen { get; set; }
}
