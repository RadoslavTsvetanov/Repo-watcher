// Git Manager (C#) // ================= // // Scans a parent directory, detects Git repositories, localizes nested repos via submodules, // caches scan results, periodically checks for changes, summarizes them via AI, and commits + pushes. // // .NET 8+ compatible, clean architecture, extensible.

using System; using System.Collections.Generic; using System.Diagnostics; using System.IO; using System.Linq; using System.Text; using System.Threading; using System.Threading.Tasks;

// ----------------------------- // Configuration Models // -----------------------------

public record RepoConfig( string Path, bool ExcludeFromChecks = false, string? AlternativeRemote = null );

public record ManagerConfig( string RootDir, HashSet<string> ScanExcludes, HashSet<string> CheckExcludes, TimeSpan CheckInterval, string CacheFile ) { public static ManagerConfig Default(string rootDir) => new( RootDir: rootDir, ScanExcludes: new HashSet<string> { "node_modules", ".git", ".venv" }, CheckExcludes: new HashSet<string>(), CheckInterval: TimeSpan.FromMinutes(30), CacheFile: ".git_manager_cache.txt" ); }

// ----------------------------- // Cache Service (Abstracted) // -----------------------------

public interface ICacheService { string? Get(string key); void Set(string key, string value); void DeleteEntry(string key); void DeleteAll(); }

public sealed class FileCacheService : ICacheService { private readonly string _file; private readonly Dictionary<string, string> _cache = new();

public FileCacheService(string file)
{
    _file = file;
    Load();
}

private void Load()
{
    if (!File.Exists(_file)) return;
    foreach (var line in File.ReadAllLines(_file))
    {
        var parts = line.Split('=', 2);
        if (parts.Length == 2)
            _cache[parts[0]] = parts[1];
    }
}

private void Save()
{
    var sb = new StringBuilder();
    foreach (var kv in _cache)
        sb.AppendLine($"{kv.Key}={kv.Value}");
    File.WriteAllText(_file, sb.ToString());
}

public string? Get(string key) => _cache.TryGetValue(key, out var v) ? v : null;

public void Set(string key, string value)
{
    _cache[key] = value;
    Save();
}

public void DeleteEntry(string key)
{
    _cache.Remove(key);
    Save();
}

public void DeleteAll()
{
    _cache.Clear();
    Save();
}

}

// ----------------------------- // Git Utilities // -----------------------------

public static class GitService { public static bool IsGitRepo(string path) => Directory.Exists(System.IO.Path.Combine(path, ".git"));

public static bool HasChanges(string path)
    => !string.IsNullOrWhiteSpace(Run(path, "git status --porcelain"));

public static string Diff(string path) => Run(path, "git diff");

public static void Commit(string path, string message)
{
    Run(path, "git add -A");
    Run(path, $"git commit -m \"{message.Replace("\"", "'")}\"");
}

public static void Push(string path, string? remote)
{
    Run(path, remote == null ? "git push" : $"git push {remote}");
}

public static void LocalizeAsSubmodule(string parent, string repo)
{
    Run(parent, $"git submodule add {repo}");
}

private static string Run(string cwd, string cmd)
{
    var psi = new ProcessStartInfo
    {
        FileName = "bash",
        Arguments = $"-c \"{cmd}\"",
        WorkingDirectory = cwd,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    using var p = Process.Start(psi)!;
    var output = p.StandardOutput.ReadToEnd();
    p.WaitForExit();
    return output;
}

}

// ----------------------------- // AI Summarization Service (Stub) // -----------------------------

public interface IAIService { string Summarize(string diff); }

public sealed class DummyAIService : IAIService { public string Summarize(string diff) => "Automated update: summarized changes"; }

// ----------------------------- // Repository Scanner // -----------------------------

public sealed class RepoScanner { private readonly ManagerConfig _config; private readonly ICacheService _cache;

public RepoScanner(ManagerConfig config, ICacheService cache)
{
    _config = config;
    _cache = cache;
}

public List<RepoConfig> Scan()
{
    var cached = _cache.Get("repos");
    if (cached != null)
        return cached.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => new RepoConfig(p))
            .ToList();

    var repos = new List<RepoConfig>();
    ScanDir(_config.RootDir, repos);
    _cache.Set("repos", string.Join(';', repos.Select(r => r.Path)));
    return repos;
}

private void ScanDir(string path, List<RepoConfig> repos)
{
    var name = System.IO.Path.GetFileName(path);
    if (_config.ScanExcludes.Any(e => name.Contains(e))) return;

    if (GitService.IsGitRepo(path))
    {
        repos.Add(new RepoConfig(
            path,
            _config.CheckExcludes.Contains(name)
        ));

        foreach (var dir in Directory.GetDirectories(path))
            if (GitService.IsGitRepo(dir))
                GitService.LocalizeAsSubmodule(path, dir);
        return;
    }

    foreach (var dir in Directory.GetDirectories(path))
        ScanDir(dir, repos);
}

}

// ----------------------------- // Change Monitor // -----------------------------

public sealed class RepoMonitor : IDisposable { private readonly ManagerConfig _config; private readonly List<RepoConfig> _repos; private readonly IAIService _ai; private readonly Timer _timer;

public RepoMonitor(ManagerConfig config, List<RepoConfig> repos, IAIService ai)
{
    _config = config;
    _repos = repos;
    _ai = ai;
    _timer = new Timer(_ => Check(), null, TimeSpan.Zero, _config.CheckInterval);
}

private void Check()
{
    foreach (var repo in _repos)
    {
        if (repo.ExcludeFromChecks) continue;
        if (!GitService.HasChanges(repo.Path)) continue;

        var diff = GitService.Diff(repo.Path);
        var msg = _ai.Summarize(diff);
        GitService.Commit(repo.Path, msg);
        GitService.Push(repo.Path, repo.AlternativeRemote);
    }
}

public void Dispose() => _timer.Dispose();

}

// ----------------------------- // Main Manager // -----------------------------

public sealed class GitManager : IDisposable { private readonly RepoScanner _scanner; private readonly IAIService _ai; private RepoMonitor? _monitor;

public GitManager(ManagerConfig config, IAIService ai)
{
    var cache = new FileCacheService(config.CacheFile);
    _scanner = new RepoScanner(config, cache);
    _ai = ai;
}

public void Start()
{
    var repos = _scanner.Scan();
    _monitor = new RepoMonitor(
        ManagerConfig.Default(Directory.GetCurrentDirectory()),
        repos,
        _ai
    );
}

public void Dispose() => _monitor?.Dispose();

}

// ----------------------------- // Entry Point // -----------------------------

public static class Program { public static void Main() { var config = ManagerConfig.Default(Directory.GetCurrentDirectory()); using var manager = new GitManager(config, new DummyAIService()); manager.Start();

Console.WriteLine("Git Manager running. Press Ctrl+C to exit.");
    Thread.Sleep(Timeout.Infinite);
}

}
