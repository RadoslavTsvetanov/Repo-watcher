using GitManagerApp.AI;
using GitManagerApp.Cache;
using GitManagerApp.Configuration;
using GitManagerApp.Monitoring;
using GitManagerApp.Scanning;

namespace GitManagerApp;

public sealed class GitManager : IDisposable
{
    private readonly IAIService _ai;
    private readonly ManagerConfig _config;
    private readonly RepoScanner _scanner;
    private RepoMonitor? _monitor;

    public GitManager(ManagerConfig config, IAIService ai)
    {
        _config = config;
        _ai = ai;
        _scanner = new RepoScanner(config, new FileCacheService(config.CacheFile));
    }

    public void Start()
    {
        var repos = _scanner.Scan();
        _monitor = new RepoMonitor(_config, repos, _ai);
    }

    public void Dispose()
    {
        _monitor?.Dispose();
    }
}
