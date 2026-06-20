using GitManagerApp.AI;
using GitManagerApp.Configuration;
using GitManagerApp.Git;

namespace GitManagerApp.Monitoring;

public sealed class RepoMonitor : IDisposable
{
    private readonly IAIService _ai;
    private readonly ManagerConfig _config;
    private readonly IReadOnlyList<RepoConfig> _repos;
    private readonly Timer _timer;

    public RepoMonitor(ManagerConfig config, IReadOnlyList<RepoConfig> repos, IAIService ai)
    {
        _config = config;
        _repos = repos;
        _ai = ai;
        _timer = new Timer(_ => Check(), null, TimeSpan.Zero, _config.CheckInterval);
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    private void Check()
    {
        foreach (var repo in _repos)
        {
            if (repo.ExcludeFromChecks || !GitService.HasChanges(repo.Path))
            {
                continue;
            }

            var diff = GitService.Diff(repo.Path);
            var message = _ai.Summarize(diff);

            GitService.Commit(repo.Path, message);
            GitService.Push(repo.Path, repo.AlternativeRemote);
        }
    }
}
