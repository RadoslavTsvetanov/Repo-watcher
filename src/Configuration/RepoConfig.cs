namespace GitManagerApp.Configuration;

public sealed record RepoConfig(
    string Path,
    bool ExcludeFromChecks = false,
    string? AlternativeRemote = null);
