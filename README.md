# Git Manager

Git Manager scans a root directory for Git repositories, monitors them for local changes, generates a commit message, commits the changes, and pushes them.

The current AI implementation is a placeholder (`DummyAIService`), so commit messages are fixed until a real AI service is added.

## Project Structure

```text
src/
  AI/              AI summary interface and dummy implementation
  Cache/           File-based cache for discovered repositories
  Configuration/   Manager and repository configuration records
  Git/             Git command wrapper
  Monitoring/      Periodic repository change checks
  Scanning/        Recursive repository discovery
  GitManager.cs    Main orchestration class
  Program.cs       Application entry point
```

## Requirements

- .NET 8 SDK or newer
- Git installed and available in your terminal

Check your .NET installation with:

```bash
dotnet --version
```

## How Repositories Are Monitored

The app monitors repositories by scanning a root folder.

By default, the root folder is the directory where you run the app:

```csharp
var config = ManagerConfig.Default(Directory.GetCurrentDirectory());
```

Any child folder that contains a `.git` directory is treated as a repository and added to monitoring.

For example, if you run Git Manager from:

```text
/Users/me/projects
```

and your folders look like this:

```text
/Users/me/projects/
  app-one/.git/
  app-two/.git/
  notes/
```

then `app-one` and `app-two` will be monitored. `notes` will be ignored unless it is also a Git repository.

## How To Add A Repo For Monitoring

Place the repository inside the root directory that Git Manager scans.

Example:

```bash
cd /Users/me/projects
git clone git@github.com:example/my-repo.git
dotnet run
```

`my-repo` will be discovered automatically during the scan.

The scan result is cached in:

```text
.git_manager_cache.txt
```

If you add or remove repositories after the first run, delete this cache file so Git Manager rescans the folder:

```bash
rm .git_manager_cache.txt
dotnet run
```

## Ignored Folders

The default scan skips folders whose names include:

- `node_modules`
- `.git`
- `.venv`

These defaults live in:

```text
src/Configuration/ManagerConfig.cs
```

## Excluding A Repo From Checks

Repositories can be marked as excluded with `CheckExcludes` in `ManagerConfig`.

For example:

```csharp
var config = ManagerConfig.Default("/Users/me/projects") with
{
    CheckExcludes = new HashSet<string> { "experimental-repo" }
};
```

The repo can still be discovered, but it will not be committed or pushed by the monitor.

## Running

From the project root:

```bash
dotnet run
```

The app starts immediately and checks repositories every 30 minutes.

```text
Git Manager running. Press Ctrl+C to exit.
```

## Notes

- The app currently commits all local changes with `git add -A`.
- The app pushes with `git push` unless an alternative remote is configured.
- Nested Git repositories inside another repository are localized as submodules.
- Make sure you only run this in folders where automatic commit and push behavior is desired.
