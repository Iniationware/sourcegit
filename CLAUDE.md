# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Building and Running

### Build from Source
```bash
# Restore dependencies
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
dotnet restore

# Build the project
dotnet build -c Release

# Run the application
dotnet run --project src/SourceGit.csproj
```

### Publish for Specific Platforms
```bash
# Windows
dotnet publish src/SourceGit.csproj -c Release -r win-x64

# macOS Intel
dotnet publish src/SourceGit.csproj -c Release -r osx-x64

# macOS Apple Silicon  
dotnet publish src/SourceGit.csproj -c Release -r osx-arm64

# Linux
dotnet publish src/SourceGit.csproj -c Release -r linux-x64
```

## Architecture Overview

### Framework and Technology Stack
- **Framework**: .NET 9.0 with Avalonia UI (cross-platform desktop framework)
- **Language**: C# with MVVM pattern using CommunityToolkit.Mvvm
- **UI Framework**: Avalonia 11.3.3 with custom themes and TextMate syntax highlighting
- **Key Dependencies**: Azure.AI.OpenAI for commit message generation, LiveChartsCore for statistics visualization

### Core Architecture Patterns

#### Command Pattern (`src/Commands/`)
All Git operations are encapsulated as Command classes inheriting from `Command.cs`. Each command:
- Creates a Git process with proper environment setup (SSH keys, editors)
- Handles stdout/stderr asynchronously
- Supports cancellation tokens for long-running operations
- Logs commands through `ICommandLog` interface

#### Repository Management (`src/ViewModels/Repository.cs`)
The central `Repository` class implements `IRepository` and manages:
- Git repository state (branches, commits, working copy, stashes)
- Background watchers for filesystem changes
- Refresh operations triggered by watchers or user actions
- Settings persistence in `$APPDATA/SourceGit/` or portable mode

#### MVVM Architecture
- **ViewModels** (`src/ViewModels/`): Business logic, inheriting from `ObservableObject`
- **Views** (`src/Views/`): Avalonia XAML views with code-behind for UI logic
- **Models** (`src/Models/`): Data structures and interfaces
- **Commands**: Separate from ViewModels, handle Git process execution

#### Native Platform Integration (`src/Native/`)
Platform-specific implementations for Windows, macOS, and Linux handle:
- File/folder selection dialogs
- External tool launching (VS Code, terminals, etc.)
- System notifications
- Window management and theming

### Key Architectural Decisions

#### Asynchronous Git Operations
All Git commands run asynchronously to keep the UI responsive. The `Command` class provides both fire-and-forget (`Exec()`) and awaitable (`ExecAsync()`) patterns.

#### Watcher-Based Updates
File system watchers (`Models/Watcher.cs`) monitor `.git` directories and trigger targeted refreshes rather than polling, improving performance.

#### Localization System
Resources are stored in `src/Resources/Locales/*.axaml` as Avalonia resource dictionaries. The `Models.Locales` class manages language switching at runtime.

#### Theme System
Supports light/dark themes with custom overrides (`Models/ThemeOverrides.cs`). Themes are defined in `Resources/Themes.axaml` and can be extended with custom JSON theme files.

## Development Notes

### Adding New Git Commands
1. Create a new class in `src/Commands/` inheriting from `Command`
2. Override `ParseResult()` if the command produces output needing parsing
3. Create corresponding ViewModel in `src/ViewModels/` if UI interaction is needed
4. Add View in `src/Views/` with XAML and code-behind

### Working with Popups
Popups inherit from `ViewModels.Popup` and use a consistent pattern:
- ViewModel handles validation and command execution
- View binds to ViewModel properties
- `Popup.InvokeAsync()` shows the popup and returns result

### Testing Git Operations
The application supports portable mode by creating a `data` folder next to the executable. This allows testing without affecting the system-wide installation.