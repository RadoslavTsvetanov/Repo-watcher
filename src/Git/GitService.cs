using System.Diagnostics;

namespace GitManagerApp.Git;

public static class GitService
{
    public static bool IsGitRepo(string path) => Directory.Exists(System.IO.Path.Combine(path, ".git"));

    public static bool HasChanges(string path) => !string.IsNullOrWhiteSpace(Run(path, "status", "--porcelain"));

    public static string Diff(string path) => Run(path, "diff");

    public static void Commit(string path, string message)
    {
        Run(path, "add", "-A");
        Run(path, "commit", "-m", message.Replace("\"", "'"));
    }

    public static void Push(string path, string? remote)
    {
        string[] args = string.IsNullOrWhiteSpace(remote)
            ? new[] { "push" }
            : new[] { "push" }.Concat(remote.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToArray();

        Run(path, args);
    }

    public static void LocalizeAsSubmodule(string parent, string repo)
    {
        Run(parent, "submodule", "add", repo);
    }

    private static string Run(string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start git process.");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Git command failed: git {string.Join(' ', arguments)}{Environment.NewLine}{error}");
        }

        return output;
    }
}
