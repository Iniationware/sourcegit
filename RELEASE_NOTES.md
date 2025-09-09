# SourceGit Iniationware Edition - Release Notes

## Version 2025.34-IW.1

### About This Fork
This is the Iniationware edition of SourceGit, featuring significant performance improvements, enhanced stability, and additional features not yet available in the upstream version.

### Versioning Scheme
- Format: `YYYY.WW-IW.PATCH`
- Example: `2025.34-IW.1` (Based on upstream week 34 of 2025, Iniationware fork, patch 1)

---

## Major Improvements

### 🚀 Performance Optimizations

#### Git Command Optimization System
- **Intelligent Caching**: Reduces redundant Git operations by up to 70%
- **Batch Query Executor**: Combines multiple Git queries for improved efficiency
- **Process Pooling**: Reuses Git processes to reduce overhead
- **Parallel Execution**: Leverages multi-core processors for faster operations
- **Smart Cache Invalidation**: Automatically refreshes cache when needed

#### Memory Management
- **Bounded Caches**: Prevents memory leaks with size-limited caches
- **Automatic Cleanup**: Proper disposal of resources when repositories close
- **Memory Metrics Display**: Real-time memory usage monitoring in bottom-left corner
- **Optimized Data Structures**: Reduced memory footprint for large repositories

### 🛡️ Stability Improvements

#### Thread Safety Fixes
- **File Watcher Improvements**: Complete rewrite with thread-safe operations
- **Event Channel Architecture**: Prevents crashes from rapid file system events
- **Lock-Free Operations**: Using Interlocked operations for thread safety
- **Proper Resource Disposal**: Ensures clean shutdown and resource cleanup

#### Error Handling
- **Retry Logic**: Automatic retry for Git operations blocked by lock files
- **Graceful Degradation**: Continues operation even when some features fail
- **Comprehensive Logging**: Better error tracking for debugging

### ✨ New Features

#### GitFlow Integration
- **Optimized GitFlow Operations**: Faster branch creation and management
- **Smart Branch Detection**: Automatic GitFlow pattern recognition
- **Batch GitFlow Commands**: Execute multiple GitFlow operations efficiently

#### UI Enhancements
- **Resizable History Columns**: Customizable column widths in commit history
- **Branch/Tag Tooltips**: Improved tooltips with more information
- **Memory Usage Display**: Real-time metrics in the interface
- **F5 Refresh Support**: Quick refresh with keyboard shortcut

#### From Upstream (Cherry-picked)
- **Vue.js Syntax Highlighting**: Support for Vue.js files
- **Menu Icons**: Enhanced visual navigation with new icons

### 🐛 Bug Fixes

#### Critical Fixes
- **Repository Scanning Crash** (#1718): Fixed folder creation crashes
- **Branch Refresh Failures** (#1715): Comprehensive error handling
- **Memory Leaks**: Prevented with bounded caches and disposal
- **macOS Fullscreen**: Fixed graph resize issues
- **Thread Safety**: Resolved race conditions in file watching

#### Additional Fixes
- Missing locale keys resolved
- README logo display corrected
- Graph column overlap prevention
- Proper error recovery mechanisms

---

## Technical Details

### Architecture Improvements
- **Command Pattern**: Retry wrapper for all Git commands
- **Cache Layer**: Three-tier caching system (L1: Hot, L2: Warm, L3: Cold)
- **Event-Driven Updates**: Channel-based file system monitoring
- **Performance Monitoring**: Built-in metrics collection and reporting

### Dependencies
- Based on SourceGit upstream (up to v2025.31)
- .NET 9.0 runtime
- AvaloniaUI 11.2.x framework

---

## Migration from Upstream

If you're switching from the upstream SourceGit:
1. Your repositories and settings will be preserved
2. Performance improvements will be immediately noticeable
3. No configuration changes required

---

## Known Issues
- Some upstream v2025.32-34 features not yet integrated (will be added in future releases)
- Package upgrades (AvaloniaUI 11.3.5) pending compatibility testing

---

## Contributors
- Iniationware team for performance optimizations and stability improvements
- Original SourceGit developers for the excellent foundation
- Community contributors for bug reports and testing

---

## Future Roadmap
- [ ] Integration of remaining upstream v2025.34 features
- [ ] Further performance optimizations for repositories with 100k+ commits
- [ ] Advanced caching strategies for remote operations
- [ ] Enhanced GitFlow visualizations

---

## Support
For issues specific to the Iniationware edition, please report to:
https://github.com/Iniationware/sourcegit/issues

For upstream issues, please report to:
https://github.com/sourcegit-scm/sourcegit/issues