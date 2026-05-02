# claude-code-insights

> A real-time web dashboard for [Claude Code](https://claude.ai/code) — track your daily costs, cache savings, tool usage, project activity, and full prompt history. Updates live as you work, no browser reload needed.

![Dashboard Screenshot](docs/images/dashboard.png)

---

## What is this?

When you use **Claude Code**, every session, message, tool call, and token is quietly logged to your local `~/.claude` directory. This dashboard reads that data and turns it into something actually useful — so you can understand how much you're spending, what Claude is doing for you, and how your usage evolves over time.

Built with **ASP.NET Core 10** and runs entirely on your machine. Your data never leaves your computer.

---

## Features

### Dashboard
- **Today vs All-Time summary** — sessions, messages, tool calls, and estimated cost side by side
- **Cost trend chart** — line chart of your spend over the last 30 days
- **Model usage donut chart** — see your Opus / Sonnet / Haiku split
- **Peak usage hours** — bar chart of which hours of the day you use Claude most
- **Activity heatmap** — GitHub-style contribution graph of the last 52 weeks
- **Cache performance** — hit rate and estimated dollar savings vs no-cache baseline

### Daily Costs
- Per-day breakdown of input tokens, output tokens, cache reads, cache writes
- Cache hit rate per day
- Estimated cost per day with totals row
- Bar chart of daily spend

### Tools
- Bar chart of every Claude tool ranked by call count (Bash, Read, Edit, Write, Grep, Glob, Agent, and more)
- **Click any tool** to drill down into its full call history — see the exact commands, file paths, grep patterns, or agent descriptions
- Top bash commands you've run most often

### Projects
- All your projects ranked by message count
- Per-project session count, first seen, and last active dates
- Live search to filter by project name

### History
- Full prompt audit log — every message you've sent to Claude
- Paginated (100 per page) with **live search**
- Shows timestamp, project, and full prompt text

### Live Updates
- Dashboard **auto-refreshes** the moment Claude Code writes new data
- No browser reload needed — powered by Server-Sent Events (SSE) + `FileSystemWatcher`
- Live indicator in the top bar shows connection status in real time

---

## Screenshots

| Dashboard | Daily Costs |
|-----------|-------------|
| ![Dashboard](docs/images/dashboard.png) | ![Daily Costs](docs/images/daily-costs.png) |

| Tools | History |
|-------|---------|
| ![Tools](docs/images/tools.png) | ![History](docs/images/history.png) |

---

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- [Claude Code](https://claude.ai/code) installed and used at least once (data lives in `~/.claude`)

---

## Quick Start

```bash
git clone https://github.com/YOUR_USERNAME/claude-code-insights.git
cd claude-code-insights
dotnet run --urls http://localhost:5100
```

Then open [http://localhost:5100](http://localhost:5100) in your browser.

That's it. No database, no Docker, no configuration needed — it reads directly from your `~/.claude` directory.

---

## Configuration

All settings live in `appsettings.json`. You only need to change anything if your Claude data is in a non-standard location or you want to use different pricing rates.

```json
{
  "ClaudeDir": "",
  "Rates": {
    "Input":       "5.0",
    "Output":      "25.0",
    "CacheRead":   "0.5",
    "CacheCreate": "6.25"
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `ClaudeDir` | `~/.claude` | Path to your Claude data directory. Leave empty to use the default. |
| `Rates:Input` | `5.0` | Input token price (USD per 1M tokens) |
| `Rates:Output` | `25.0` | Output token price (USD per 1M tokens) |
| `Rates:CacheRead` | `0.5` | Cache read price (USD per 1M tokens) |
| `Rates:CacheCreate` | `6.25` | Cache write price (USD per 1M tokens) |

Default rates match **AWS Bedrock (ap-southeast-2)** pricing.

**For Anthropic API pricing**, use these values:

```json
"Rates": {
  "Input":       "15.0",
  "Output":      "75.0",
  "CacheRead":   "1.5",
  "CacheCreate": "18.75"
}
```

---

## How Live Updates Work

```
You send a message in Claude Code
        ↓
Claude Code writes to ~/.claude/projects/**/*.jsonl
        ↓
FileSystemWatcher detects the file change
        ↓
1-second debounce (batches rapid writes into one event)
        ↓
Server pushes "data-changed" event via SSE (/api/events)
        ↓
Browser receives the event and re-fetches the data
        ↓
Charts and tables update instantly — no page reload
```

The live indicator in the top-right corner tells you the current state:

| Indicator | Meaning |
|-----------|---------|
| 🟢 **Live** | Connected, watching for changes |
| 🟡 **Updating…** | New data detected, refreshing |
| 🔴 **Reconnecting…** | Connection lost, retrying in 5s |

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| Backend | ASP.NET Core 10 Minimal API + Razor Pages |
| Data parsing | C# with `System.Text.Json` (reads `.jsonl` files directly) |
| Live updates | Server-Sent Events (SSE) + `FileSystemWatcher` |
| Charts | [Chart.js 4](https://www.chartjs.org/) |
| Styling | Custom CSS (no framework) — dark purple/indigo theme |
| Data source | `~/.claude` local files — read-only, nothing is sent anywhere |

---

## Data Privacy

This tool is **100% local**:
- Reads only from your `~/.claude` directory
- Never sends any data to external servers
- No analytics, no telemetry
- Binds to `127.0.0.1` (localhost only) — not accessible from other devices on your network

---

## API Endpoints

The dashboard exposes these JSON endpoints if you want to use the data elsewhere:

| Endpoint | Description |
|----------|-------------|
| `GET /api/stats` | Today vs all-time summary stats |
| `GET /api/daily-costs` | Per-day token and cost breakdown |
| `GET /api/tool-calls` | Tool usage counts ranked by frequency |
| `GET /api/tool-details/{toolName}` | Detailed call log for a specific tool |
| `GET /api/projects` | Projects ranked by message count |
| `GET /api/history` | Full prompt history |
| `GET /api/model-usage` | Breakdown by Claude model |
| `GET /api/activity-heatmap` | Daily activity for the last 52 weeks |
| `GET /api/hourly-usage` | Activity grouped by hour of day |
| `GET /api/top-commands` | Most frequently run bash commands |
| `GET /api/events` | SSE stream for live updates |

---

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you'd like to change.

---

## License

MIT
