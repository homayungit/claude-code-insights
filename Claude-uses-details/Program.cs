using ClaudeUsesDetails.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddSingleton<ClaudeDataService>();
builder.Services.AddSingleton<ChangeNotifier>();

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

// Start file watcher
var notifier = app.Services.GetRequiredService<ChangeNotifier>();
var claudeDir = app.Services.GetRequiredService<ClaudeDataService>().ClaudeDir;
notifier.StartWatching(claudeDir);

// ── API endpoints ────────────────────────────────────────────────────────────

app.MapGet("/api/stats", (ClaudeDataService svc) => svc.GetStats());

app.MapGet("/api/history", (ClaudeDataService svc) => svc.GetHistory());

app.MapGet("/api/tool-calls", (ClaudeDataService svc) => svc.GetToolCalls());

app.MapGet("/api/tool-details/{toolName}", (string toolName, ClaudeDataService svc) =>
    svc.GetToolDetails(toolName));

app.MapGet("/api/projects", (ClaudeDataService svc) => svc.GetProjects());

app.MapGet("/api/daily-costs", (ClaudeDataService svc) => svc.GetDailyCosts());

app.MapGet("/api/model-usage", (ClaudeDataService svc) => svc.GetModelUsage());

app.MapGet("/api/activity-heatmap", (ClaudeDataService svc) => svc.GetActivityHeatmap());

app.MapGet("/api/hourly-usage", (ClaudeDataService svc) => svc.GetHourlyUsage());

app.MapGet("/api/top-commands", (ClaudeDataService svc) => svc.GetTopBashCommands());

// ── SSE endpoint ─────────────────────────────────────────────────────────────

app.MapGet("/api/events", async (ChangeNotifier notifier, HttpContext ctx, CancellationToken ct) =>
{
    ctx.Response.Headers["Content-Type"] = "text/event-stream";
    ctx.Response.Headers["Cache-Control"] = "no-cache";
    ctx.Response.Headers["X-Accel-Buffering"] = "no";
    ctx.Response.Headers["Connection"] = "keep-alive";

    var ch = notifier.Subscribe();
    try
    {
        await ctx.Response.WriteAsync("data: connected\n\n", ct);
        await ctx.Response.Body.FlushAsync(ct);

        await foreach (var msg in ch.Reader.ReadAllAsync(ct))
        {
            await ctx.Response.WriteAsync($"data: {msg}\n\n", ct);
            await ctx.Response.Body.FlushAsync(ct);
        }
    }
    catch (OperationCanceledException) { }
    finally
    {
        notifier.Unsubscribe(ch);
    }
});

app.Run();
