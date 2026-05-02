namespace ClaudeUsesDetails.Models;

public class HistoryEntry
{
    public string Display { get; set; } = "";
    public string Timestamp { get; set; } = "";
    public string Project { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string ShortProject => Project.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? Project;
}
