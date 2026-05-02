using System.Text.Json;
using ClaudeUsesDetails.Models;

namespace ClaudeUsesDetails.Services;

public class ClaudeDataService
{
    public string ClaudeDir => _claudeDir;
    private readonly string _claudeDir;
    private readonly PricingRates _rates;

    public ClaudeDataService(IConfiguration config)
    {
        var cfgDir = config["ClaudeDir"];
        _claudeDir = string.IsNullOrWhiteSpace(cfgDir)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude")
            : cfgDir;

        _rates = new PricingRates
        {
            Input = double.Parse(config["Rates:Input"] ?? "5.0") / 1_000_000,
            Output = double.Parse(config["Rates:Output"] ?? "25.0") / 1_000_000,
            CacheRead = double.Parse(config["Rates:CacheRead"] ?? "0.5") / 1_000_000,
            CacheCreate = double.Parse(config["Rates:CacheCreate"] ?? "6.25") / 1_000_000,
        };
    }

    public bool IsAvailable() => Directory.Exists(_claudeDir);

    // ── History ──────────────────────────────────────────────────────────────

    public List<HistoryEntry> GetHistory()
    {
        var entries = new List<HistoryEntry>();
        foreach (var (file, _) in GetAllProjectJsonlFilesWithProject())
        {
            foreach (var line in SafeReadLines(file))
            {
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;
                    if (GetString(root, "type") != "user") continue;
                    if (!root.TryGetProperty("message", out var msg)) continue;

                    // Extract first text content block
                    var display = "";
                    if (msg.TryGetProperty("content", out var content))
                    {
                        if (content.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in content.EnumerateArray())
                            {
                                if (GetString(item, "type") == "text")
                                {
                                    display = GetString(item, "text");
                                    break;
                                }
                            }
                        }
                        else if (content.ValueKind == JsonValueKind.String)
                        {
                            display = content.GetString() ?? "";
                        }
                    }

                    if (string.IsNullOrWhiteSpace(display)) continue;

                    entries.Add(new HistoryEntry
                    {
                        Display = display.Length > 300 ? display[..300] + "…" : display,
                        Timestamp = GetString(root, "timestamp"),
                        Project = GetString(root, "cwd"),
                        SessionId = GetString(root, "sessionId"),
                    });
                }
                catch (JsonException) { }
            }
        }
        entries.Sort((a, b) => string.Compare(a.Timestamp, b.Timestamp, StringComparison.Ordinal));
        return entries;
    }

    // ── Projects ─────────────────────────────────────────────────────────────

    public List<ProjectSummary> GetProjects()
    {
        var history = GetHistory();
        var map = new Dictionary<string, (int msgs, HashSet<string> sessions, string? first, string? last)>();

        foreach (var e in history)
        {
            var proj = string.IsNullOrEmpty(e.Project) ? "unknown" : e.Project;
            if (!map.ContainsKey(proj))
                map[proj] = (0, new HashSet<string>(), null, null);

            var (msgs, sessions, first, last) = map[proj];
            sessions.Add(e.SessionId);
            var ts = e.Timestamp;
            map[proj] = (
                msgs + 1,
                sessions,
                first == null || ts.CompareTo(first) < 0 ? ts : first,
                last == null || ts.CompareTo(last) > 0 ? ts : last
            );
        }

        return map
            .Select(kv => new ProjectSummary
            {
                Name = kv.Key,
                ShortName = kv.Key.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? kv.Key,
                Messages = kv.Value.msgs,
                Sessions = kv.Value.sessions.Count,
                FirstSeen = kv.Value.first,
                LastSeen = kv.Value.last,
            })
            .OrderByDescending(p => p.Messages)
            .ToList();
    }

    // ── Daily Costs ───────────────────────────────────────────────────────────

    public DailyCostsResult GetDailyCosts()
    {
        var daily = new Dictionary<string, DailyCostAccum>();

        foreach (var file in GetAllProjectJsonlFiles())
            ParseDailyCosts(file, daily);

        var days = daily.Keys.OrderBy(d => d).Select(date =>
        {
            var d = daily[date];
            var cost = d.Input * _rates.Input + d.Output * _rates.Output
                     + d.CacheRead * _rates.CacheRead + d.CacheCreate * _rates.CacheCreate;
            return new DailyCostEntry
            {
                Date = date,
                Messages = d.Messages,
                ToolCalls = d.ToolCalls,
                Sessions = d.Sessions.Count,
                Input = d.Input,
                Output = d.Output,
                CacheRead = d.CacheRead,
                CacheCreate = d.CacheCreate,
                Cost = Math.Round(cost, 4),
                Models = d.Models,
            };
        }).ToList();

        var totals = new DailyCostEntry
        {
            Date = "All Time",
            Messages = days.Sum(d => d.Messages),
            ToolCalls = days.Sum(d => d.ToolCalls),
            Sessions = days.Sum(d => d.Sessions),
            Input = days.Sum(d => d.Input),
            Output = days.Sum(d => d.Output),
            CacheRead = days.Sum(d => d.CacheRead),
            CacheCreate = days.Sum(d => d.CacheCreate),
            Cost = Math.Round(days.Sum(d => d.Cost), 4),
        };
        foreach (var day in days)
            foreach (var (model, count) in day.Models)
                totals.Models[model] = totals.Models.GetValueOrDefault(model) + count;

        return new DailyCostsResult { Days = days, Totals = totals, Rates = _rates };
    }

    // ── Stats (derived from daily costs) ─────────────────────────────────────

    public StatsData GetStats()
    {
        var costs = GetDailyCosts();
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var todayEntry = costs.Days.FirstOrDefault(d => d.Date == today);

        return new StatsData
        {
            Today = ToSummaryStats(todayEntry),
            AllTime = ToSummaryStats(costs.Totals),
        };
    }

    private SummaryStats ToSummaryStats(DailyCostEntry? e)
    {
        if (e == null) return new SummaryStats();
        var totalReadable = e.Input + e.CacheRead;
        var cacheHitRate = totalReadable > 0 ? (double)e.CacheRead / totalReadable * 100 : 0;
        var savings = e.CacheRead * (_rates.Input - _rates.CacheRead);
        return new SummaryStats
        {
            Sessions = e.Sessions,
            Messages = e.Messages,
            ToolCalls = e.ToolCalls,
            InputTokens = e.Input,
            OutputTokens = e.Output,
            CacheReadTokens = e.CacheRead,
            CacheCreateTokens = e.CacheCreate,
            Cost = e.Cost,
            CacheHitRate = Math.Round(cacheHitRate, 1),
            CacheSavings = Math.Round(savings, 4),
        };
    }

    // ── Tool Calls ────────────────────────────────────────────────────────────

    public ToolCallsResult GetToolCalls()
    {
        var counts = new Dictionary<string, int>();
        var byProject = new Dictionary<string, Dictionary<string, int>>();

        foreach (var (file, proj) in GetAllProjectJsonlFilesWithProject())
            ParseToolCalls(file, proj, counts, byProject);

        var sorted = counts
            .OrderByDescending(kv => kv.Value)
            .Select(kv => new ToolCallItem { Tool = kv.Key, Count = kv.Value })
            .ToList();

        return new ToolCallsResult { Tools = sorted, ByProject = byProject };
    }

    public List<ToolDetail> GetToolDetails(string toolName)
    {
        var calls = new List<ToolDetail>();
        foreach (var (file, proj) in GetAllProjectJsonlFilesWithProject())
            ParseToolDetails(file, proj, toolName, calls);

        calls.Sort((a, b) => string.Compare(b.Timestamp, a.Timestamp, StringComparison.Ordinal));
        return calls;
    }

    // ── Analytics ─────────────────────────────────────────────────────────────

    public List<ModelUsageItem> GetModelUsage()
    {
        var costs = GetDailyCosts();
        var modelTotals = new Dictionary<string, int>();
        foreach (var day in costs.Days)
            foreach (var (model, count) in day.Models)
                modelTotals[model] = modelTotals.GetValueOrDefault(model) + count;

        var total = modelTotals.Values.Sum();
        return modelTotals
            .OrderByDescending(kv => kv.Value)
            .Select(kv => new ModelUsageItem
            {
                Model = kv.Key,
                ShortName = ShortenModel(kv.Key),
                Count = kv.Value,
                Percentage = total > 0 ? Math.Round((double)kv.Value / total * 100, 1) : 0,
            })
            .ToList();
    }

    public List<ActivityDay> GetActivityHeatmap()
    {
        var history = GetHistory();
        var counts = new Dictionary<string, int>();
        foreach (var e in history)
        {
            if (e.Timestamp.Length >= 10)
                counts[e.Timestamp[..10]] = counts.GetValueOrDefault(e.Timestamp[..10]) + 1;
        }

        // Build last 52 weeks (364 days + today = 365 days)
        var result = new List<ActivityDay>();
        var end = DateTime.UtcNow.Date;
        var start = end.AddDays(-364);

        for (var d = start; d <= end; d = d.AddDays(1))
        {
            var key = d.ToString("yyyy-MM-dd");
            var count = counts.GetValueOrDefault(key);
            result.Add(new ActivityDay
            {
                Date = key,
                Count = count,
                Level = count == 0 ? 0 : count < 3 ? 1 : count < 8 ? 2 : count < 15 ? 3 : 4,
            });
        }
        return result;
    }

    public List<HourlyUsage> GetHourlyUsage()
    {
        var history = GetHistory();
        var counts = new int[24];
        foreach (var e in history)
        {
            if (DateTime.TryParse(e.Timestamp, out var dt))
                counts[dt.ToLocalTime().Hour]++;
        }
        return Enumerable.Range(0, 24)
            .Select(h => new HourlyUsage { Hour = h, Count = counts[h] })
            .ToList();
    }

    public List<TopCommand> GetTopBashCommands(int top = 20)
    {
        var details = GetToolDetails("Bash");
        return details
            .Where(d => !string.IsNullOrWhiteSpace(d.Command))
            .GroupBy(d => (d.Command ?? "").Split('\n')[0].Trim())
            .OrderByDescending(g => g.Count())
            .Take(top)
            .Select(g => new TopCommand
            {
                Command = g.Key.Length > 120 ? g.Key[..120] + "…" : g.Key,
                Count = g.Count(),
                Project = g.First().Project,
            })
            .ToList();
    }

    // ── File helpers ──────────────────────────────────────────────────────────

    private IEnumerable<string> GetAllProjectJsonlFiles()
    {
        var projectsDir = Path.Combine(_claudeDir, "projects");
        if (!Directory.Exists(projectsDir)) yield break;
        foreach (var file in Directory.EnumerateFiles(projectsDir, "*.jsonl", SearchOption.AllDirectories))
            yield return file;
    }

    private IEnumerable<(string file, string project)> GetAllProjectJsonlFilesWithProject()
    {
        var projectsDir = Path.Combine(_claudeDir, "projects");
        if (!Directory.Exists(projectsDir)) yield break;
        foreach (var dir in Directory.EnumerateDirectories(projectsDir))
        {
            var projName = Path.GetFileName(dir);
            foreach (var file in Directory.EnumerateFiles(dir, "*.jsonl", SearchOption.AllDirectories))
                yield return (file, projName);
        }
    }

    // ── JSONL parsers ─────────────────────────────────────────────────────────

    private void ParseDailyCosts(string filePath, Dictionary<string, DailyCostAccum> daily)
    {
        foreach (var line in SafeReadLines(filePath))
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                var ts = GetString(root, "timestamp");
                if (ts.Length < 10) continue;
                var day = ts[..10];

                if (!daily.ContainsKey(day))
                    daily[day] = new DailyCostAccum();
                var d = daily[day];

                var type = GetString(root, "type");
                var sid = GetString(root, "sessionId");

                if (type == "user")
                {
                    d.Messages++;
                    d.Sessions.Add(sid);
                }

                if (type == "assistant" && root.TryGetProperty("message", out var msg))
                {
                    if (msg.TryGetProperty("usage", out var usage))
                    {
                        d.Input += GetLong(usage, "input_tokens");
                        d.Output += GetLong(usage, "output_tokens");
                        d.CacheRead += GetLong(usage, "cache_read_input_tokens");
                        d.CacheCreate += GetLong(usage, "cache_creation_input_tokens");
                    }

                    var model = GetString(msg, "model");
                    if (!string.IsNullOrEmpty(model))
                        d.Models[model] = d.Models.GetValueOrDefault(model) + 1;

                    if (msg.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
                        foreach (var item in content.EnumerateArray())
                            if (GetString(item, "type") == "tool_use") d.ToolCalls++;
                }
            }
            catch (JsonException) { }
        }
    }

    private void ParseToolCalls(string filePath, string projDir,
        Dictionary<string, int> counts, Dictionary<string, Dictionary<string, int>> byProject)
    {
        foreach (var line in SafeReadLines(filePath))
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                if (GetString(root, "type") != "assistant") continue;
                if (!root.TryGetProperty("message", out var msg)) continue;
                if (!msg.TryGetProperty("content", out var content)) continue;
                if (content.ValueKind != JsonValueKind.Array) continue;

                foreach (var item in content.EnumerateArray())
                {
                    if (GetString(item, "type") != "tool_use") continue;
                    var tool = GetString(item, "name");
                    counts[tool] = counts.GetValueOrDefault(tool) + 1;
                    if (!byProject.ContainsKey(projDir)) byProject[projDir] = new();
                    byProject[projDir][tool] = byProject[projDir].GetValueOrDefault(tool) + 1;
                }
            }
            catch (JsonException) { }
        }
    }

    private void ParseToolDetails(string filePath, string projDir, string toolName, List<ToolDetail> calls)
    {
        foreach (var line in SafeReadLines(filePath))
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                if (GetString(root, "type") != "assistant") continue;
                if (!root.TryGetProperty("message", out var msg)) continue;
                if (!msg.TryGetProperty("content", out var content)) continue;
                if (content.ValueKind != JsonValueKind.Array) continue;

                foreach (var item in content.EnumerateArray())
                {
                    if (GetString(item, "type") != "tool_use") continue;
                    if (GetString(item, "name") != toolName) continue;

                    var input = item.TryGetProperty("input", out var inp) ? inp : default;
                    var detail = new ToolDetail
                    {
                        Project = projDir,
                        Timestamp = GetString(root, "timestamp"),
                    };

                    switch (toolName)
                    {
                        case "Bash":
                            detail.Command = GetString(input, "command");
                            detail.Description = GetString(input, "description");
                            break;
                        case "Read":
                        case "Edit":
                        case "Write":
                            detail.FilePath = GetString(input, "file_path");
                            break;
                        case "Grep":
                            detail.Pattern = GetString(input, "pattern");
                            detail.Path = GetString(input, "path");
                            detail.Glob = GetString(input, "glob");
                            break;
                        case "Glob":
                            detail.Pattern = GetString(input, "pattern");
                            detail.Path = GetString(input, "path");
                            break;
                        case "Agent":
                            detail.Description = GetString(input, "description");
                            detail.SubagentType = GetString(input, "subagent_type");
                            break;
                        default:
                            if (input.ValueKind != JsonValueKind.Undefined)
                                detail.Input = input.GetRawText()[..Math.Min(200, input.GetRawText().Length)];
                            break;
                    }
                    calls.Add(detail);
                }
            }
            catch (JsonException) { }
        }
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    private static IEnumerable<string> SafeReadLines(string path)
    {
        if (!File.Exists(path)) yield break;
        foreach (var line in File.ReadLines(path))
            if (!string.IsNullOrWhiteSpace(line)) yield return line;
    }

    private static string GetString(JsonElement el, string prop)
    {
        if (el.ValueKind == JsonValueKind.Undefined) return "";
        return el.TryGetProperty(prop, out var v) ? v.GetString() ?? "" : "";
    }

    private static long GetLong(JsonElement el, string prop)
    {
        if (el.ValueKind == JsonValueKind.Undefined) return 0;
        return el.TryGetProperty(prop, out var v) && v.TryGetInt64(out var i) ? i : 0;
    }

    private static string ShortenModel(string model)
    {
        if (model.Contains("opus")) return "Opus";
        if (model.Contains("sonnet")) return "Sonnet";
        if (model.Contains("haiku")) return "Haiku";
        return model.Split('-').FirstOrDefault() ?? model;
    }

    // ── Accumulator (private) ─────────────────────────────────────────────────

    private class DailyCostAccum
    {
        public long Input, Output, CacheRead, CacheCreate;
        public int Messages, ToolCalls;
        public HashSet<string> Sessions = new();
        public Dictionary<string, int> Models = new();
    }
}
