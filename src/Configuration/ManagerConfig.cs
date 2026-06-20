namespace GitManagerApp.Configuration;

public sealed record ManagerConfig(
    string RootDir,
    HashSet<string> ScanExcludes,
    HashSet<string> CheckExcludes,
    TimeSpan CheckInterval,
    string CacheFile)
{
    public static ManagerConfig Default(string rootDir) => new(
        RootDir: rootDir,
        ScanExcludes: new HashSet<string> { "node_modules", ".git", ".venv" },
        CheckExcludes: new HashSet<string>(),
        CheckInterval: TimeSpan.FromMinutes(30),
        CacheFile: ".git_manager_cache.txt");
}
