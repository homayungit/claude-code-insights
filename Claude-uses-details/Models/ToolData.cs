namespace ClaudeUsesDetails.Models;

public class ToolCallItem
{
    public string Tool { get; set; } = "";
    public int Count { get; set; }
}

public class ToolCallsResult
{
    public List<ToolCallItem> Tools { get; set; } = new();
    public Dictionary<string, Dictionary<string, int>> ByProject { get; set; } = new();
}

public class ToolDetail
{
    public string Project { get; set; } = "";
    public string Timestamp { get; set; } = "";
    public string? Command { get; set; }
    public string? Description { get; set; }
    public string? FilePath { get; set; }
    public string? Pattern { get; set; }
    public string? Path { get; set; }
    public string? Glob { get; set; }
    public string? SubagentType { get; set; }
    public string? Input { get; set; }
}
