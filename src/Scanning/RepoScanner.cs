using GitManagerApp.Cache;
using GitManagerApp.Configuration;
using GitManagerApp.Git;

namespace GitManagerApp.Scanning;

public sealed class RepoScanner
{
    private const string CacheKey = "repos";

    private readonly ICacheService _cache;
    private readonly ManagerConfig _config;

    public RepoScanner(ManagerConfig config, ICacheService cache)
    {
        _config = config;
        _cache = cache;
    }

    public List<RepoConfig> Scan()
    {
        var cachedRepos = _cache.Get(CacheKey);
        if (cachedRepos is not null)
        {
            return cachedRepos
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(path => new RepoConfig(path))
                .ToList();
        }

        var repos = new List<RepoConfig>();

        ScanDirectory(_config.RootDir, repos);
        _cache.Set(CacheKey, string.Join(';', repos.Select(repo => repo.Path)));

        return repos;
    }

    private void ScanDirectory(string path, List<RepoConfig> repos)
    {
        var directoryName = System.IO.Path.GetFileName(path) ?? string.Empty;
        if (ShouldSkipDirectory(directoryName))
        {
            return;
        }

        if (GitService.IsGitRepo(path))
        {
            repos.Add(CreateRepoConfig(path, directoryName));
            LocalizeNestedRepositories(path);
            return;
        }

        foreach (var directory in Directory.GetDirectories(path))
        {
            ScanDirectory(directory, repos);
        }
    }

    private RepoConfig CreateRepoConfig(string path, string directoryName)
    {
        return new RepoConfig(
            Path: path,
            ExcludeFromChecks: _config.CheckExcludes.Contains(directoryName));
    }

    private void LocalizeNestedRepositories(string path)
    {
        foreach (var directory in Directory.GetDirectories(path).Where(GitService.IsGitRepo))
        {
            GitService.LocalizeAsSubmodule(path, directory);
        }
    }

    private bool ShouldSkipDirectory(string directoryName)
    {
        return _config.ScanExcludes.Any(exclude => directoryName.Contains(exclude));
    }
}
