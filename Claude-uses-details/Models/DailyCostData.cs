namespace ClaudeUsesDetails.Models;

public class DailyCostEntry
{
    public string Date { get; set; } = "";
    public int Messages { get; set; }
    public int ToolCalls { get; set; }
    public int Sessions { get; set; }
    public long Input { get; set; }
    public long Output { get; set; }
    public long CacheRead { get; set; }
    public long CacheCreate { get; set; }
    public double Cost { get; set; }
    public Dictionary<string, int> Models { get; set; } = new();
}

public class DailyCostsResult
{
    public List<DailyCostEntry> Days { get; set; } = new();
    public DailyCostEntry Totals { get; set; } = new();
    public PricingRates Rates { get; set; } = new();
}

public class PricingRates
{
    public double Input { get; set; }
    public double Output { get; set; }
    public double CacheRead { get; set; }
    public double CacheCreate { get; set; }
}

public class SummaryStats
{
    public int Sessions { get; set; }
    public int Messages { get; set; }
    public int ToolCalls { get; set; }
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long CacheReadTokens { get; set; }
    public long CacheCreateTokens { get; set; }
    public double Cost { get; set; }
    public double CacheHitRate { get; set; }
    public double CacheSavings { get; set; }
}

public class StatsData
{
    public SummaryStats Today { get; set; } = new();
    public SummaryStats AllTime { get; set; } = new();
}
