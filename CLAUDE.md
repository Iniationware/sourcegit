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

## Lessons Learned

### Development Methodology
**Divide and Conquer (Teile und Herrsche)**: When implementing new features, follow these principles:
1. **Start Small**: Implement ONE small, working feature completely before expanding
2. **Use Real Data**: Never use simulated/fake data - always work with actual Git commands
3. **Backend First**: Build the Git command wrapper and data model before any UI
4. **Test Early**: Verify functionality with real repositories before adding complexity
5. **Incremental Enhancement**: Add features one at a time, testing each addition

### Common Pitfalls to Avoid
- **Script-Kiddy Approach**: Don't try to implement everything at once with simulated data
- **Missing Validation**: Always check if methods/properties exist before using them
- **Protection Levels**: Respect access modifiers - don't try to access internal/protected members
- **Converter Dependencies**: Verify all converters exist before referencing them in XAML
- **Namespace Conflicts**: Use fully qualified names when there are ambiguous references

### Shutdown Performance
When dealing with background operations and UI updates:
- Use `CancellationTokenSource` for all long-running operations
- Implement `_isUnloading` flag to prevent dispatcher operations during shutdown
- Clean up event handlers properly in `OnUnloaded()`
- Cancel pending operations before disposal to prevent 30+ second hangs

### Git Command Integration
- All Git operations must inherit from `Command` class
- Use `Command.Exec()` for fire-and-forget, `Command.ExecAsync()` for awaitable operations
- Parse command output in `ParseResult()` override
- Log all commands through `ICommandLog` interface
- Handle errors gracefully with proper exception handling

## Versioning and Release Strategy

### Iniationware Version Pattern
**Format**: `v{YEAR}.{WEEK}-IW.{INCREMENT}`
- **Base Version**: Always use the latest official SourceGit release tag (e.g., v2025.34)
- **IW Suffix**: Add `-IW.X` to indicate Iniationware custom features
- **Increment**: Increase the number after IW for each new Iniationware release

**Examples**:
- `v2025.34-IW.1` - First Iniationware release based on SourceGit v2025.34
- `v2025.34-IW.5` - Fifth Iniationware release based on same base version

### Creating a New Release
```bash
# 1. Find the latest official SourceGit version (without IW)
git tag --list | grep -v "IW" | grep "v2025" | sort -V | tail -1

# 2. Find the latest IW version for that base
git tag --list | grep "v2025.34-IW" | sort -V | tail -1

# 3. Create new tag with incremented IW number
git tag -a v2025.34-IW.6 -m "Release description..."

# 4. Push tag to trigger GitHub Actions workflow
git push origin v2025.34-IW.6
```

### GitHub Actions Workflow
The `.github/workflows/release.yml` automatically:
1. Triggers on new version tags (v*)
2. Builds the application for all platforms
3. Creates packages (Windows, macOS, Linux)
4. Publishes GitHub release with assets

### Important Notes
- **Stay on base version**: Don't change the base version (e.g., v2025.34) unless updating to newer SourceGit
- **Only increment IW number**: For Iniationware features, only increase the number after -IW
- **Tag triggers workflow**: Pushing a tag automatically starts the release build process
- **Semantic messages**: Use clear, descriptive release notes focusing on added features