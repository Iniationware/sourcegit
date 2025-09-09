# SourceGit Iniationware Edition - Development TODO

## Overview
This document tracks planned improvements and features for the SourceGit Iniationware Edition. Items are organized by priority and estimated effort.

---

## 🔴 Priority 1 - Critical Fixes (Immediate)

### Platform-Specific Issues

#### Linux
- [ ] **Fix Desktop File MimeType** [`build/resources/_common/applications/sourcegit.desktop`]
  - Remove incorrect `MimeType=inode/directory` causing unwanted app launches
  - Issue: #1650
  - Effort: 15 minutes

#### macOS
- [ ] **Fix ARM64 ZIP Release Build** [`build-macos-signed.sh`]
  - ARM64 ZIP releases potentially broken
  - Issue: #1723
  - Test on Apple Silicon hardware
  - Effort: 2 hours

### Authentication & Security

- [ ] **GPG Signing Support Enhancement** [`src/Commands/Commit.cs`]
  - Add GPG_TTY environment variable detection
  - Provide clear error messages when GPG signing fails
  - Issue: #1645
  - Effort: 1 hour

- [ ] **SSH Agent Authentication Fix** [`src/Commands/Command.cs`]
  - Linux SSH agent broken since v2025.28
  - Review SSH_AUTH_SOCK handling
  - Issue: #1670
  - Effort: 2 hours

---

## 🟡 Priority 2 - High Impact Features (Next Sprint)

### Merge Conflict Resolution

- [ ] **Built-in 3-Way Merge Editor**
  - Create new view: `src/Views/MergeConflictResolver.axaml`
  - Implement conflict parser: `src/Models/ConflictParser.cs`
  - Add inline editing capabilities
  - Reference: Similar to GitKraken's conflict resolution
  - Effort: 2 weeks

### Enhanced Diff Viewer

- [ ] **Minimap Navigation** [`src/Views/DiffView.axaml.cs`]
  - Add scrollable minimap showing file overview
  - Highlight changed regions in minimap
  - Click-to-navigate functionality
  - Effort: 3 days

- [ ] **Word-Level Diff Highlighting** [`src/Views/TextDiffView.axaml.cs`]
  - Implement word-by-word comparison algorithm
  - Add inline highlighting for changed words
  - Effort: 2 days

- [ ] **Auto-Jump to First Change** [`src/Views/DiffView.axaml.cs`]
  - When "Show full file" is enabled, auto-scroll to first diff
  - Issue: #1655
  - Effort: 4 hours

### Advanced Search System

- [ ] **Global Repository Search**
  - New ViewModel: `src/ViewModels/GlobalSearch.cs`
  - Search across all registered repositories
  - Index commit messages, file names, and content
  - Effort: 1 week

- [ ] **Full-Text Commit Search** [`src/Commands/QueryCommits.cs`]
  - Implement commit message indexing
  - Add search filters (author, date, branch)
  - Effort: 3 days

---

## 🟢 Priority 3 - Performance Optimizations

### Memory Management

- [ ] **LRU Cache for Commit Graph** [`src/ViewModels/Repository.cs:2429-2432`]
  - Replace basic caching with LRU implementation
  - Add memory pressure detection
  - Implement size limits and eviction policies
  - Effort: 2 days

- [ ] **Performance Logging to File** [`src/Models/PerformanceMonitor.cs:203-204`]
  - Implement optional file-based logging
  - Add log rotation and size limits
  - Create performance analysis tools
  - Effort: 1 day

### macOS Optimizations

- [ ] **FSEvents Integration** [`src/Native/MacOS.cs`]
  - Replace FileSystemWatcher with native FSEvents
  - Better performance for large repositories
  - Create new class: `src/Native/MacOSFileWatcher.cs`
  - Effort: 1 week

- [ ] **Apple Silicon Detection & Optimization**
  ```csharp
  // Add to src/Native/MacOS.cs
  public bool IsAppleSilicon() => 
      RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
  ```
  - Optimize Git path detection for ARM64
  - Add ARM64-specific process optimizations
  - Effort: 2 days

### Async Operations

- [ ] **Convert Blocking Operations to Async** [`src/ViewModels/`]
  - Audit all ViewModels for blocking operations
  - Convert to async/await with ConfigureAwait(false)
  - Priority files:
    - `Repository.cs`
    - `WorkingCopy.cs`
    - `Histories.cs`
  - Effort: 3 days

---

## 🔵 Priority 4 - UI/UX Enhancements

### Drag & Drop Support

- [ ] **Commit Cherry-Pick via Drag** [`src/Views/Histories.axaml.cs`]
  - Enable dragging commits between branches
  - Visual feedback during drag operation
  - Effort: 3 days

- [ ] **File Operations Drag & Drop** [`src/Views/WorkingCopy.axaml.cs`]
  - Drag files to stage/unstage
  - Create patches via drag to desktop
  - Effort: 2 days

### Interactive Git Graph

- [ ] **Graph Filtering** [`src/Views/Histories.axaml`]
  - Filter by author
  - Filter by date range
  - Filter by branch
  - Effort: 3 days

- [ ] **Inline Commit Search** [`src/Views/Histories.axaml`]
  - Add search box directly in graph view
  - Real-time filtering as you type
  - Effort: 2 days

### Theme System

- [ ] **High Contrast Mode**
  - Create new theme: `src/Resources/Themes/HighContrast.axaml`
  - WCAG 2.1 AA compliance
  - Effort: 2 days

- [ ] **Custom Theme Editor**
  - New view: `src/Views/ThemeEditor.axaml`
  - Live preview of theme changes
  - Export/import theme files
  - Effort: 1 week

---

## 🟣 Priority 5 - Advanced Features

### Repository Templates

- [ ] **Template System** 
  - New model: `src/Models/RepositoryTemplate.cs`
  - Predefined templates for common project types
  - Custom template creation
  - Effort: 1 week

- [ ] **Pre-commit Hook Management**
  - UI for managing Git hooks
  - Template library for common hooks
  - New view: `src/Views/GitHooksManager.axaml`
  - Effort: 3 days

### Team Collaboration

- [ ] **Pull Request Preview** (Read-Only)
  - Fetch PR data from GitHub/GitLab APIs
  - Display PR status and reviews
  - New ViewModel: `src/ViewModels/PullRequestViewer.cs`
  - Effort: 2 weeks

- [ ] **Enhanced Issue Tracker Integration**
  - Auto-link issues in commit messages
  - Show issue status inline
  - Effort: 1 week

### Git Advanced Features

- [ ] **Git Worktree Enhanced UI**
  - Dedicated worktree management view
  - Visual worktree switching
  - New view: `src/Views/WorktreeManager.axaml`
  - Effort: 1 week

- [ ] **Partial Clone Support**
  - UI for configuring partial clones
  - Visual indicators for partial repositories
  - Effort: 3 days

- [ ] **Git LFS Improvements**
  - Better visual indicators for LFS files
  - LFS file size warnings
  - Batch LFS operations
  - Effort: 3 days

---

## 🟤 Priority 6 - Architecture & Long-term

### Plugin System

- [ ] **Plugin Architecture Foundation**
  - Design plugin API
  - Create plugin loader: `src/Plugins/PluginManager.cs`
  - Plugin manifest format
  - Effort: 3 weeks

- [ ] **Example Plugins**
  - GitLab integration plugin
  - Custom theme plugin
  - External tool integration plugin
  - Effort: 2 weeks

### Code Quality

- [ ] **Error Handling Standardization**
  - Create centralized error handler
  - Structured logging system
  - User-friendly error messages
  - Effort: 1 week

- [ ] **Unit Test Coverage**
  - Add tests for critical components
  - Target 80% coverage for new code
  - Set up CI/CD test automation
  - Effort: Ongoing

---

## Implementation Schedule

### Sprint 1 (Week 1-2)
- All Priority 1 fixes
- Start Built-in Merge Editor design
- Begin Enhanced Diff Viewer work

### Sprint 2 (Week 3-4)
- Complete Enhanced Diff Viewer
- Implement Advanced Search foundation
- Start Memory Management improvements

### Sprint 3 (Week 5-6)
- Complete Merge Conflict Resolution
- macOS optimizations
- Async operations conversion

### Sprint 4 (Week 7-8)
- UI/UX enhancements
- Drag & Drop support
- Theme system improvements

### Future Sprints
- Advanced features
- Plugin system
- Team collaboration features

---

## Success Metrics

- **Performance**: 30% reduction in memory usage for large repos
- **Stability**: 90% reduction in crash reports
- **Features**: Feature parity with GitKraken for core functionality
- **User Satisfaction**: 4.5+ star rating on package managers

---

## Notes

- Items marked with issue numbers reference upstream SourceGit issues
- Effort estimates assume single developer working full-time
- Priority can be adjusted based on user feedback and telemetry
- This list will be updated as items are completed or new issues discovered

---

Last Updated: 2025-09-09