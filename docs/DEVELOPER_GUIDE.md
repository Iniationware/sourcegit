# SourceGit Developer Guide

## Table of Contents
1. [Getting Started](#getting-started)
2. [Development Environment](#development-environment)
3. [Building the Project](#building-the-project)
4. [Development Workflow](#development-workflow)
5. [Adding New Features](#adding-new-features)
6. [Testing](#testing)
7. [Debugging](#debugging)
8. [Performance Optimization](#performance-optimization)
9. [Common Tasks](#common-tasks)
10. [Troubleshooting](#troubleshooting)

## Getting Started

### Prerequisites

1. **Required Software**
   - [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
   - [Git](https://git-scm.com/) (2.25+)
   - [Git LFS](https://git-lfs.github.com/)
   - IDE: [Visual Studio 2022](https://visualstudio.microsoft.com/), [JetBrains Rider](https://www.jetbrains.com/rider/), or [VS Code](https://code.visualstudio.com/)

2. **Recommended Tools**
   - [Avalonia for Visual Studio](https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaVS)
   - [Avalonia for VS Code](https://marketplace.visualstudio.com/items?itemName=AvaloniaUI.AvaloniaVSCode)
   - [dotnet-format](https://github.com/dotnet/format) for code formatting

### Clone and Setup

```bash
# Clone the repository
git clone https://github.com/Iniationware/sourcegit.git
cd sourcegit

# Install Git LFS files
git lfs pull

# Restore NuGet packages
dotnet restore

# Build the project
dotnet build -c Debug

# Run the application
dotnet run --project src/SourceGit.csproj
```

## Development Environment

### IDE Configuration

#### Visual Studio 2022
1. Open `SourceGit.sln` in Visual Studio
2. Install Avalonia extension from Extensions → Manage Extensions
3. Set `SourceGit` as startup project
4. Press F5 to run with debugging

#### JetBrains Rider
1. Open `SourceGit.sln` in Rider
2. Install Avalonia plugin from Settings → Plugins
3. Configure run configuration for `SourceGit` project
4. Use Shift+F10 to run

#### VS Code
1. Open folder in VS Code
2. Install C# and Avalonia extensions
3. Configure `launch.json`:
```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Launch SourceGit",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/src/bin/Debug/net9.0/SourceGit.dll",
            "args": [],
            "cwd": "${workspaceFolder}/src",
            "console": "internalConsole"
        }
    ]
}
```

### Project Structure

```
sourcegit/
├── src/                        # Main application source
│   ├── Commands/              # Git command wrappers
│   ├── Models/                # Data models
│   ├── ViewModels/            # MVVM ViewModels
│   ├── Views/                 # Avalonia XAML views
│   ├── Native/                # Platform-specific code
│   ├── Resources/             # Resources and assets
│   │   ├── Icons.axaml       # Icon resources
│   │   ├── Themes.axaml      # Theme definitions
│   │   └── Locales/          # Localization files
│   └── SourceGit.csproj      # Project file
├── build/                     # Build scripts
├── docs/                      # Documentation
└── tests/                     # Unit tests (planned)
```

## Building the Project

### Debug Build
```bash
# Build for debugging
dotnet build -c Debug

# Run with debugging symbols
dotnet run --project src/SourceGit.csproj -c Debug
```

### Release Build
```bash
# Build optimized release
dotnet build -c Release

# Create platform-specific builds
dotnet publish src/SourceGit.csproj -c Release -r win-x64
dotnet publish src/SourceGit.csproj -c Release -r osx-arm64
dotnet publish src/SourceGit.csproj -c Release -r linux-x64
```

### Build Options
```bash
# Self-contained (includes .NET runtime)
dotnet publish -c Release -r win-x64 --self-contained

# Framework-dependent (requires .NET runtime)
dotnet publish -c Release -r win-x64 --self-contained false

# Single file executable
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true

# With ReadyToRun (faster startup)
dotnet publish -c Release -r win-x64 -p:PublishReadyToRun=true
```

## Development Workflow

### Git Workflow

1. **Create Feature Branch**
```bash
git checkout -b feature/your-feature-name
```

2. **Make Changes**
   - Follow coding standards
   - Write clear commit messages
   - Test your changes

3. **Commit Changes**
```bash
git add .
git commit -m "feat: add new feature description"
```

4. **Push and Create PR**
```bash
git push origin feature/your-feature-name
# Create pull request on GitHub
```

### Coding Standards

#### C# Style Guide
```csharp
// File header
using System;
using System.Collections.Generic;

namespace SourceGit.Commands
{
    // Class documentation
    /// <summary>
    /// Executes git add command
    /// </summary>
    public class Add : Command
    {
        // Private fields with underscore prefix
        private readonly string _repo;
        private List<string> _files;

        // Properties in PascalCase
        public bool StageAll { get; set; }

        // Constructor
        public Add(string repo, List<string> files = null)
        {
            _repo = repo;
            _files = files ?? new List<string>();
        }

        // Methods in PascalCase
        public override void Execute()
        {
            // Local variables in camelCase
            var args = BuildArguments();

            // Use var for obvious types
            var result = RunCommand(args);

            // Explicit type for clarity
            Dictionary<string, object> data = ParseResult(result);
        }

        // Private methods
        private string BuildArguments()
        {
            // Implementation
        }
    }
}
```

#### XAML Style Guide
```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:SourceGit.ViewModels"
             x:Class="SourceGit.Views.Repository">

    <!-- Group related properties -->
    <Grid RowDefinitions="Auto,*,Auto"
          ColumnDefinitions="200,*">

        <!-- Use meaningful names -->
        <TextBlock x:Name="TitleText"
                   Grid.Row="0"
                   Text="{Binding Title}"
                   FontSize="18"
                   FontWeight="Bold"/>

        <!-- Consistent spacing -->
        <ListBox Grid.Row="1"
                 Margin="0,8,0,8"
                 Items="{Binding Items}">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <!-- Template content -->
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
    </Grid>
</UserControl>
```

## Adding New Features

### Step 1: Adding a New Git Command

Create command wrapper in `src/Commands/`:

```csharp
// src/Commands/MyNewCommand.cs
namespace SourceGit.Commands
{
    public class MyNewCommand : Command
    {
        public MyNewCommand(string repo, string param)
        {
            WorkingDirectory = repo;
            Args = $"my-command {param}";
        }

        protected override void ParseResult(List<string> lines)
        {
            // Parse command output
            foreach (var line in lines)
            {
                // Process each line
            }
        }
    }
}
```

### Step 2: Create ViewModel

Add ViewModel in `src/ViewModels/`:

```csharp
// src/ViewModels/MyFeature.cs
namespace SourceGit.ViewModels
{
    public class MyFeature : ObservableObject
    {
        private string _status;

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public RelayCommand ExecuteCommand { get; }

        public MyFeature(Repository repo)
        {
            ExecuteCommand = new RelayCommand(Execute);
        }

        private async void Execute()
        {
            Status = "Executing...";

            var cmd = new MyNewCommand(_repo.FullPath, "param");
            var success = await cmd.ExecAsync();

            Status = success ? "Success" : "Failed";
        }
    }
}
```

### Step 3: Create View

Add View in `src/Views/`:

```xml
<!-- src/Views/MyFeature.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="SourceGit.Views.MyFeature">

    <Grid RowDefinitions="Auto,*,Auto">
        <TextBlock Text="My New Feature"
                   FontSize="18"
                   FontWeight="Bold"/>

        <TextBlock Grid.Row="1"
                   Text="{Binding Status}"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center"/>

        <Button Grid.Row="2"
                Content="Execute"
                Command="{Binding ExecuteCommand}"
                HorizontalAlignment="Right"/>
    </Grid>
</UserControl>
```

Code-behind:
```csharp
// src/Views/MyFeature.axaml.cs
namespace SourceGit.Views
{
    public partial class MyFeature : UserControl
    {
        public MyFeature()
        {
            InitializeComponent();
        }
    }
}
```

### Step 4: Wire Up Feature

Register in appropriate location:
```csharp
// In Repository.cs or appropriate location
public void ShowMyFeature()
{
    var vm = new ViewModels.MyFeature(this);
    var dialog = new Views.MyFeature { DataContext = vm };
    await dialog.ShowDialog(MainWindow);
}
```

## Testing

### Unit Testing (Planned)

```csharp
// tests/Commands/AddTests.cs
[TestClass]
public class AddTests
{
    [TestMethod]
    public void Add_StagesFiles()
    {
        // Arrange
        var repo = TestRepository.Create();
        var files = new List<string> { "file1.txt", "file2.txt" };

        // Act
        var cmd = new Add(repo.Path, files);
        cmd.Exec();

        // Assert
        var status = new QueryStatus(repo.Path);
        status.Exec();
        Assert.AreEqual(2, status.StagedFiles.Count);
    }
}
```

### Manual Testing Checklist

- [ ] Feature works on Windows
- [ ] Feature works on macOS
- [ ] Feature works on Linux
- [ ] UI is responsive during operations
- [ ] Errors are handled gracefully
- [ ] Cancellation works properly
- [ ] Memory usage is reasonable
- [ ] No UI freezes occur

## Debugging

### Debug Output

```csharp
// Use debug output
System.Diagnostics.Debug.WriteLine("Debug info");

// Conditional compilation
#if DEBUG
    Console.WriteLine("Debug mode");
#endif

// Logging
App.Log($"Operation started: {operation}");
```

### Performance Profiling

```csharp
// Measure execution time
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
// ... operation ...
stopwatch.Stop();
App.Log($"Operation took: {stopwatch.ElapsedMilliseconds}ms");
```

### Memory Profiling

```csharp
// Check memory usage
var before = GC.GetTotalMemory(false);
// ... operation ...
var after = GC.GetTotalMemory(false);
App.Log($"Memory used: {(after - before) / 1024}KB");
```

## Performance Optimization

### 1. Async Operations

```csharp
// Bad - blocks UI
public void LoadCommits()
{
    var commits = QueryCommits.Load(repo); // Blocking
    Commits = commits;
}

// Good - keeps UI responsive
public async Task LoadCommitsAsync()
{
    Commits = await Task.Run(() => QueryCommits.Load(repo));
}
```

### 2. Collection Virtualization

```xml
<!-- Use VirtualizingStackPanel for large lists -->
<ListBox Items="{Binding LargeCollection}">
    <ListBox.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel />
        </ItemsPanelTemplate>
    </ListBox.ItemsPanel>
</ListBox>
```

### 3. Lazy Loading

```csharp
private Lazy<List<Commit>> _commits = new Lazy<List<Commit>>(() =>
{
    return QueryCommits.Load(repo);
});

public List<Commit> Commits => _commits.Value;
```

### 4. Caching

```csharp
private static readonly Dictionary<string, object> _cache = new();

public T GetCached<T>(string key, Func<T> factory)
{
    if (!_cache.TryGetValue(key, out var value))
    {
        value = factory();
        _cache[key] = value;
    }
    return (T)value;
}
```

## Common Tasks

### Adding a New Locale

1. Create locale file in `src/Resources/Locales/`:
```xml
<!-- src/Resources/Locales/es_ES.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui">
    <sys:String x:Key="Text.Welcome">Bienvenido</sys:String>
    <sys:String x:Key="Text.Repository">Repositorio</sys:String>
    <!-- Add all translations -->
</ResourceDictionary>
```

2. Register in `Models/Locales.cs`:
```csharp
public static readonly List<Locale> Supported = new()
{
    new Locale("English", "en_US"),
    new Locale("Español", "es_ES"), // New locale
    // ...
};
```

### Adding a Context Menu Item

```xml
<ContextMenu>
    <MenuItem Header="{DynamicResource Text.MyAction}"
              Command="{Binding MyActionCommand}"
              CommandParameter="{Binding}">
        <MenuItem.Icon>
            <Path Width="12" Height="12"
                  Data="{StaticResource Icons.MyIcon}"/>
        </MenuItem.Icon>
    </MenuItem>
</ContextMenu>
```

### Adding a Keyboard Shortcut

```xml
<UserControl.KeyBindings>
    <KeyBinding Gesture="Ctrl+Shift+N"
                Command="{Binding NewFeatureCommand}"/>
</UserControl.KeyBindings>
```

## Troubleshooting

### Common Issues

#### 1. Build Errors

**Problem**: NuGet packages not found
```bash
# Solution
dotnet restore --force
dotnet clean
dotnet build
```

**Problem**: Platform-specific build fails
```bash
# Ensure correct runtime identifier
dotnet publish -c Release -r win-x64 --self-contained
```

#### 2. Runtime Issues

**Problem**: Application crashes on startup
- Check .NET 9 runtime is installed
- Verify all dependencies are present
- Check for missing resource files

**Problem**: Git commands fail
- Ensure Git is in PATH
- Check Git version (2.25+ required)
- Verify repository permissions

#### 3. UI Issues

**Problem**: UI not updating
```csharp
// Ensure UI updates on main thread
Dispatcher.UIThread.Post(() =>
{
    // Update UI properties
});
```

**Problem**: Memory leaks
- Unsubscribe from events
- Dispose resources properly
- Use weak references where appropriate

### Debug Tips

1. **Enable verbose logging**:
```csharp
App.Settings.LogLevel = LogLevel.Verbose;
```

2. **Check command output**:
```csharp
command.TraceOutput = true; // See Git command output
```

3. **Use debugger visualizers**:
   - Set breakpoints in ParseResult methods
   - Inspect command arguments
   - Check ViewModel properties

## Best Practices

### 1. Follow MVVM Pattern
- Keep Views simple (XAML only)
- Business logic in ViewModels
- Data structures in Models
- No code-behind unless necessary

### 2. Handle Errors Gracefully
```csharp
try
{
    await operation();
}
catch (GitException ex)
{
    App.ShowError($"Git operation failed: {ex.Message}");
}
catch (Exception ex)
{
    App.LogError(ex);
    App.ShowError("An unexpected error occurred");
}
```

### 3. Use Resources
```xml
<!-- Bad -->
<TextBlock Text="Click here"/>

<!-- Good -->
<TextBlock Text="{DynamicResource Text.ClickHere}"/>
```

### 4. Test Cross-Platform
Always test changes on:
- Windows 10/11
- macOS 11+
- Ubuntu 20.04+

### 5. Document Your Code
```csharp
/// <summary>
/// Executes a Git command and returns the result
/// </summary>
/// <param name="args">Command arguments</param>
/// <returns>Command output</returns>
public async Task<string> ExecuteAsync(string args)
```

## Resources

### Documentation
- [Architecture Guide](./ARCHITECTURE.md)
- [API Documentation](./API.md)
- [Build Documentation](./BUILD.md)

### External Resources
- [Avalonia Documentation](https://docs.avaloniaui.net/)
- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [Git Documentation](https://git-scm.com/doc)

### Community
- [GitHub Issues](https://github.com/Iniationware/sourcegit/issues)
- [GitHub Discussions](https://github.com/Iniationware/sourcegit/discussions)

## Contributing

Please read our [Contributing Guide](../CONTRIBUTING.md) before submitting pull requests.

## License

This project is licensed under the MIT License - see [LICENSE](../LICENSE) file for details.