# SourceGit API Documentation

## Table of Contents
1. [Command API](#command-api)
2. [Repository API](#repository-api)
3. [Model API](#model-api)
4. [ViewModel API](#viewmodel-api)
5. [Native API](#native-api)
6. [Extension Points](#extension-points)

## Command API

### Base Command Class

```csharp
namespace SourceGit.Commands
{
    public class Command
    {
        // Properties
        public string WorkingDirectory { get; set; }
        public string Args { get; set; }
        public bool TraceOutput { get; set; }

        // Core Methods
        public void Exec()
        public Task<bool> ExecAsync(CancellationToken cancellationToken = default)
        public string ReadToEnd()
        public Task<string> ReadToEndAsync()

        // Virtual Methods for Override
        protected virtual void ParseResult(List<string> lines)
        protected virtual void OnProgress(string message)
    }
}
```

### Common Command Examples

#### Add Files
```csharp
public class Add : Command
{
    public Add(string repo, List<string> files = null)

    // Usage
    var add = new Add(repoPath, new List<string> { "file1.txt", "file2.cs" });
    add.Exec();
```

#### Commit Changes
```csharp
public class Commit : Command
{
    public Commit(string repo, string message, bool amend = false, bool allowEmpty = false)

    // Usage
    var commit = new Commit(repoPath, "feat: add new feature", amend: false);
    await commit.ExecAsync();
```

#### Query Commits
```csharp
public class QueryCommits : Command
{
    public QueryCommits(string repo, string ref, int limit = 0, string since = null)
    public List<Commit> Result { get; }

    // Usage
    var query = new QueryCommits(repoPath, "HEAD", limit: 100);
    query.Exec();
    var commits = query.Result;
```

#### Fetch Remote
```csharp
public class Fetch : Command
{
    public Fetch(string repo, string remote, bool prune = false, bool tags = true)

    // Usage with progress
    var fetch = new Fetch(repoPath, "origin", prune: true);
    fetch.OnProgress += (msg) => Console.WriteLine($"Progress: {msg}");
    await fetch.ExecAsync();
```

## Repository API

### IRepository Interface

```csharp
namespace SourceGit.Models
{
    public interface IRepository
    {
        // Properties
        string FullPath { get; }
        string GitDir { get; }

        // State Properties
        List<Branch> Branches { get; }
        List<Remote> Remotes { get; }
        List<Tag> Tags { get; }
        WorkingCopy WorkingCopy { get; }

        // Methods
        void RefreshBranches()
        void RefreshCommits()
        void RefreshWorkingCopy()
        void RefreshSubmodules()

        // Settings
        RepositorySettings Settings { get; }
        void SaveSettings()
    }
}
```

### Repository ViewModel

```csharp
namespace SourceGit.ViewModels
{
    public class Repository : ObservableObject, IRepository
    {
        // Core Operations
        public void Fetch(Remote remote, bool prune = false)
        public void Pull(Branch branch, bool rebase = false)
        public void Push(Branch branch, bool force = false)

        // Branch Operations
        public void CreateBranch(string name, string basedOn)
        public void DeleteBranch(Branch branch, bool force = false)
        public void CheckoutBranch(Branch branch)
        public void MergeBranch(Branch from, Branch to, string strategy = null)

        // Commit Operations
        public void StageChanges(List<Change> changes)
        public void UnstageChanges(List<Change> changes)
        public void CommitChanges(string message, bool amend = false)
        public void RevertCommit(Commit commit)

        // Working Copy
        public void DiscardChanges(List<Change> changes)
        public void StashChanges(string message = null)
        public void PopStash(Stash stash)

        // Async Operations with Progress
        public async Task<bool> FetchAsync(Remote remote, IProgress<string> progress)
        public async Task<bool> CloneAsync(string url, string path, IProgress<string> progress)
    }
}
```

## Model API

### Core Models

#### Branch Model
```csharp
public class Branch
{
    public string Name { get; set; }
    public string FullName { get; set; }
    public bool IsLocal { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsDetached { get; set; }
    public string Remote { get; set; }
    public string UpstreamTrackingBranch { get; set; }
    public int AheadCount { get; set; }
    public int BehindCount { get; set; }
    public string Head { get; set; }
    public string HeadSubject { get; set; }
}
```

#### Commit Model
```csharp
public class Commit
{
    public string SHA { get; set; }
    public string ShortSHA { get; }
    public List<string> Parents { get; set; }
    public User Author { get; set; }
    public User Committer { get; set; }
    public string Subject { get; set; }
    public string Message { get; set; }
    public DateTime AuthorTime { get; set; }
    public DateTime CommitTime { get; set; }
    public List<Decorator> Decorators { get; set; }
    public bool HasSignature { get; set; }
}
```

#### Change Model
```csharp
public class Change
{
    public enum Status
    {
        None, Modified, Added, Deleted, Renamed, Copied, Unmerged, Untracked
    }

    public string Path { get; set; }
    public string OriginalPath { get; set; }
    public Status Index { get; set; }
    public Status WorkingTree { get; set; }

    public bool IsConflit { get; }
    public bool IsUntracked { get; }
}
```

#### Remote Model
```csharp
public class Remote
{
    public string Name { get; set; }
    public string URL { get; set; }
    public string PushURL { get; set; }

    public void Fetch(bool prune = false)
    public void Push(string localBranch, string remoteBranch)
    public void AddBranch(string branch)
    public void RemoveBranch(string branch)
}
```

## ViewModel API

### Base ViewModel Classes

#### ObservableObject Base
```csharp
public abstract class ObservableObject : INotifyPropertyChanged
{
    protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
}
```

#### Popup Base
```csharp
public abstract class Popup : ObservableObject
{
    public string Title { get; set; }

    public virtual Task<bool> Sure()
    public static Task<bool> InvokeAsync(Popup popup)
}
```

### Common ViewModels

#### Welcome ViewModel
```csharp
public class Welcome : ObservableObject
{
    public ObservableCollection<RepositoryNode> Repositories { get; }

    public void InitRepository(string path)
    public void CloneRepository(string url, string path)
    public void OpenRepository(string path)
    public void AddRepository(string path)
    public void RemoveRepository(RepositoryNode node)
}
```

#### Histories ViewModel
```csharp
public class Histories : ObservableObject
{
    public ObservableCollection<Commit> Commits { get; }
    public Commit SelectedCommit { get; set; }

    public void NavigateToCommit(string sha)
    public void SearchCommits(string query)
    public void FilterByBranch(Branch branch)
    public void ShowGraph(bool show)
}
```

#### WorkingCopy ViewModel
```csharp
public class WorkingCopy : ObservableObject
{
    public ObservableCollection<Change> Changes { get; }
    public ObservableCollection<Change> Staged { get; }
    public ObservableCollection<Change> Unstaged { get; }

    public void Stage(List<Change> changes)
    public void Unstage(List<Change> changes)
    public void Discard(List<Change> changes)
    public void OpenDiff(Change change)
    public void Commit(string message)
}
```

## Native API

### Platform Interface
```csharp
namespace SourceGit.Native
{
    public interface IOperatingSystem
    {
        string Name { get; }

        // File Operations
        void OpenInFileManager(string path)
        void OpenWithDefaultEditor(string path)
        void OpenTerminal(string path)

        // External Tools
        void OpenInVSCode(string path)
        void OpenInSublime(string path)

        // System Integration
        void SetupFileAssociations()
        void RegisterContextMenu()

        // Shell/Terminal
        ShellOrTerminal DetectShell()
        void LaunchShell(string workingDirectory, string command = null)
    }
}
```

### Platform-Specific Implementations

#### Windows
```csharp
public class Windows : IOperatingSystem
{
    public void OpenInFileManager(string path)
    {
        Process.Start("explorer.exe", $"/select,\"{path}\"");
    }

    public void RegisterContextMenu()
    {
        // Registry operations for context menu
    }
}
```

#### macOS
```csharp
public class MacOS : IOperatingSystem
{
    public void OpenInFileManager(string path)
    {
        Process.Start("open", $"-R \"{path}\"");
    }

    public void LaunchShell(string workingDirectory, string command = null)
    {
        var terminal = DetectTerminal();
        // AppleScript to launch terminal
    }
}
```

#### Linux
```csharp
public class Linux : IOperatingSystem
{
    public void OpenInFileManager(string path)
    {
        var fileManager = DetectFileManager();
        Process.Start(fileManager, $"\"{path}\"");
    }

    private string DetectFileManager()
    {
        // Detect nautilus, dolphin, thunar, etc.
    }
}
```

## Extension Points

### Custom External Tools

```csharp
public class ExternalTool
{
    public string Name { get; set; }
    public string Executable { get; set; }
    public string Arguments { get; set; }
    public string Icon { get; set; }

    public void Launch(string repoPath, params string[] args)
    {
        var processArgs = string.Format(Arguments, args);
        Process.Start(Executable, processArgs);
    }
}

// Registration
App.RegisterExternalTool(new ExternalTool
{
    Name = "Custom IDE",
    Executable = "/path/to/ide",
    Arguments = "--open {0}",
    Icon = "ide.png"
});
```

### Custom Diff/Merge Tools

```csharp
public class ExternalMerger
{
    public string Name { get; set; }
    public string Type { get; set; } // "diff" or "merge"
    public string Executable { get; set; }
    public string ArgumentsFormat { get; set; }

    public void Launch(string[] files)
    {
        // Launch external tool with files
    }
}

// Usage in settings
App.Settings.ExternalMergeTools.Add(new ExternalMerger
{
    Name = "Beyond Compare",
    Type = "both",
    Executable = "bcomp",
    ArgumentsFormat = "{0} {1} {2} {3}"
});
```

### Theme Customization

```csharp
public class ThemeOverrides
{
    public Dictionary<string, string> BasicColors { get; set; }
    public Dictionary<string, string> GraphColors { get; set; }

    public static ThemeOverrides Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ThemeOverrides>(json);
    }

    public void Apply()
    {
        // Apply theme overrides to application
    }
}
```

### AI Integration

```csharp
public interface IAIProvider
{
    Task<string> GenerateCommitMessage(List<Change> changes)
    Task<string> SuggestBranchName(string description)
    Task<string> ReviewCode(string diff)
}

public class OpenAIProvider : IAIProvider
{
    private readonly string _apiKey;

    public async Task<string> GenerateCommitMessage(List<Change> changes)
    {
        // Use Azure.AI.OpenAI to generate message
    }
}

// Register provider
App.RegisterAIProvider(new OpenAIProvider(apiKey));
```

### Command Hooks

```csharp
public interface ICommandHook
{
    void PreExecute(Command command)
    void PostExecute(Command command, bool success)
}

public class LoggingHook : ICommandHook
{
    public void PreExecute(Command command)
    {
        Log.Info($"Executing: {command.Args}");
    }

    public void PostExecute(Command command, bool success)
    {
        Log.Info($"Completed: {command.Args} - Success: {success}");
    }
}

// Register hook
Command.RegisterHook(new LoggingHook());
```

## Error Handling

### Command Errors

```csharp
public class CommandException : Exception
{
    public int ExitCode { get; }
    public string ErrorOutput { get; }

    public CommandException(string message, int exitCode, string errorOutput)
        : base(message)
    {
        ExitCode = exitCode;
        ErrorOutput = errorOutput;
    }
}

// Usage
try
{
    var cmd = new Commit(repo, message);
    await cmd.ExecAsync();
}
catch (CommandException ex)
{
    Log.Error($"Git command failed: {ex.Message}");
    Log.Error($"Exit code: {ex.ExitCode}");
    Log.Error($"Error output: {ex.ErrorOutput}");
}
```

### Repository Errors

```csharp
public class RepositoryException : Exception
{
    public enum ErrorType
    {
        NotARepository,
        LockedRepository,
        CorruptedRepository,
        NetworkError,
        AuthenticationError
    }

    public ErrorType Type { get; }

    public RepositoryException(ErrorType type, string message)
        : base(message)
    {
        Type = type;
    }
}
```

## Best Practices

### 1. Command Usage
- Always use `ExecAsync` for long-running operations
- Provide cancellation tokens for user-cancelable operations
- Handle progress callbacks for user feedback
- Check exit codes and parse error output

### 2. Repository Operations
- Use the Repository class methods instead of direct commands
- Subscribe to repository events for UI updates
- Handle offline/network error scenarios
- Validate user input before operations

### 3. Performance
- Use async/await for all I/O operations
- Cache expensive query results
- Batch multiple operations when possible
- Dispose resources properly (especially Process objects)

### 4. Error Handling
- Catch specific exceptions when possible
- Provide meaningful error messages to users
- Log errors for debugging
- Implement retry logic for transient failures

## Examples

### Complete Workflow Example

```csharp
// Initialize repository
var repo = new Repository(repoPath);

// Check status
await repo.RefreshWorkingCopyAsync();

// Stage changes
var changes = repo.WorkingCopy.Unstaged.ToList();
repo.StageChanges(changes);

// Generate commit message with AI
var aiProvider = App.GetAIProvider();
var message = await aiProvider.GenerateCommitMessage(changes);

// Commit
await repo.CommitChangesAsync(message);

// Push to remote
var currentBranch = repo.Branches.FirstOrDefault(b => b.IsCurrent);
await repo.PushAsync(currentBranch, progress: new Progress<string>(msg =>
{
    Console.WriteLine($"Push progress: {msg}");
}));

// Create tag
var tag = new Tag
{
    Name = "v1.0.0",
    Message = "Release version 1.0.0"
};
await repo.CreateTagAsync(tag);
```

### Custom Command Example

```csharp
public class CustomGitCommand : Command
{
    private List<string> _results = new List<string>();

    public CustomGitCommand(string repo) : base()
    {
        WorkingDirectory = repo;
        Args = "custom-command --with-options";
    }

    protected override void ParseResult(List<string> lines)
    {
        foreach (var line in lines)
        {
            if (line.StartsWith("RESULT:"))
            {
                _results.Add(line.Substring(7));
            }
        }
    }

    public List<string> GetResults() => _results;
}

// Usage
var cmd = new CustomGitCommand(repoPath);
await cmd.ExecAsync();
var results = cmd.GetResults();
```

## API Versioning

The SourceGit API follows semantic versioning:
- **Major**: Breaking changes to public APIs
- **Minor**: New features, backward compatible
- **Patch**: Bug fixes, no API changes

Current API Version: **2025.34.11**

## Further Reading

- [Architecture Documentation](./ARCHITECTURE.md)
- [Developer Guide](./DEVELOPER_GUIDE.md)
- [Contributing Guide](../CONTRIBUTING.md)
- [Plugin Development](./PLUGINS.md)