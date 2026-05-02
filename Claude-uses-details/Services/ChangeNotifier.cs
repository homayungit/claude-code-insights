using System.Threading.Channels;

namespace ClaudeUsesDetails.Services;

public class ChangeNotifier : IDisposable
{
    private readonly List<Channel<string>> _clients = new();
    private readonly object _lock = new();
    private FileSystemWatcher? _watcher;
    private Timer? _debounce;

    public void StartWatching(string claudeDir)
    {
        var projectsDir = Path.Combine(claudeDir, "projects");
        if (!Directory.Exists(projectsDir)) return;

        _watcher = new FileSystemWatcher(projectsDir)
        {
            IncludeSubdirectories = true,
            Filter = "*.jsonl",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };

        _watcher.Changed += OnFileEvent;
        _watcher.Created += OnFileEvent;
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        // Debounce: wait 1s after last event before broadcasting
        _debounce?.Dispose();
        _debounce = new Timer(_ => Broadcast("data-changed"), null, 1000, Timeout.Infinite);
    }

    private void Broadcast(string message)
    {
        List<Channel<string>> snapshot;
        lock (_lock) snapshot = _clients.ToList();
        foreach (var ch in snapshot)
            ch.Writer.TryWrite(message);
    }

    public Channel<string> Subscribe()
    {
        var ch = Channel.CreateBounded<string>(new BoundedChannelOptions(10)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
        });
        lock (_lock) _clients.Add(ch);
        return ch;
    }

    public void Unsubscribe(Channel<string> ch)
    {
        lock (_lock) _clients.Remove(ch);
        ch.Writer.TryComplete();
    }

    public void Dispose()
    {
        _debounce?.Dispose();
        _watcher?.Dispose();
    }
}
