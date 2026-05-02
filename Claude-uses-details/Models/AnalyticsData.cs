namespace ClaudeUsesDetails.Models;

public class ModelUsageItem
{
    public string Model { get; set; } = "";
    public string ShortName { get; set; } = "";
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class ActivityDay
{
    public string Date { get; set; } = "";
    public int Count { get; set; }
    public int Level { get; set; } // 0-4 heatmap intensity
}

public class HourlyUsage
{
    public int Hour { get; set; }
    public int Count { get; set; }
    public string Label => $"{Hour:D2}:00";
}

public class TopCommand
{
    public string Command { get; set; } = "";
    public int Count { get; set; }
    public string Project { get; set; } = "";
}
