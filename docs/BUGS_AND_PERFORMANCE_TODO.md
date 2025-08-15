# Bug and Performance Issues TODO List

Based on open GitHub issues as of August 2025. Priority sorted by severity and impact.

## 🚨 Critical Issues (Crashes & Data Loss Risk)

### [ ] #1718 - Folder Creation Crash
- **Problem**: App crashes when creating a folder while looking for a repository
- **Impact**: Application stability, potential data loss
- **Priority**: HIGH
- **Affected**: All platforms

### [ ] #1715 - Branch Refresh Failure
- **Problem**: Failed to refresh branches functionality
- **Impact**: Core functionality broken, unable to see branch updates
- **Priority**: HIGH
- **Affected**: Unknown platforms

## ⚡ Performance Issues

### [ ] #1720 - macOS Performance & Stability
- **Problem**: Slow performance, crashes, and git problems on macOS
- **Impact**: Overall user experience degraded on macOS
- **Priority**: HIGH
- **Affected**: macOS (likely both Intel and ARM)
- **Notes**: May be related to file watching, memory management, or native API calls

## 🔧 Platform-Specific Bugs

### macOS ARM64
### [ ] #1723 - M1 Mac ARM ZIP Release Issue
- **Problem**: ZIP releases from tags potentially broken for M1 Macs
- **Impact**: Distribution and installation problems
- **Priority**: MEDIUM
- **Affected**: macOS ARM64 (M1/M2/M3)
- **Notes**: Check build pipeline and packaging scripts

### Linux
### [ ] #1670 - SSH Agent Authentication Broken (since v2025.28)
- **Problem**: SSH agent based authentication not working on Linux
- **Impact**: Unable to authenticate with remote repositories
- **Priority**: HIGH
- **Affected**: Linux
- **Notes**: Regression in version 2025.28

### [ ] #1650 - Desktop File Mime Type Conflict
- **Problem**: Incorrect MimeType causes SourceGit to open when other apps open folders
- **Impact**: Unwanted application launches, user annoyance
- **Priority**: MEDIUM
- **Affected**: Linux
- **Fix**: Update `sourcegit.desktop` MimeType configuration

## 🔐 Security & Authentication

### [ ] #1645 - GPG Commit Signing Failure
- **Problem**: "gpg failed to sign the data" during commits
- **Impact**: Unable to create signed commits
- **Priority**: MEDIUM
- **Affected**: All platforms with GPG signing enabled
- **Notes**: Check GPG integration, environment variables, and key access

## 🎨 UI/UX Issues

### [ ] #1655 - Diff View Navigation
- **Problem**: When "Show full file" is enabled, clicking file diff should auto-jump to first change
- **Impact**: Poor navigation experience in diff view
- **Priority**: LOW
- **Affected**: All platforms
- **Notes**: Enhancement to improve usability

## Investigation Notes

### Performance Optimization Areas
1. **File Watching**: Review `Models/Watcher.cs` for efficiency
2. **Memory Management**: Check for memory leaks in long-running sessions
3. **Git Command Execution**: Optimize `Commands/Command.cs` for parallel operations
4. **UI Rendering**: Review Avalonia UI performance, especially for large repos

### macOS Specific Checks
1. **Native API Calls**: Review `Native/MacOS.cs` for potential issues
2. **Code Signing**: Verify proper signing for ARM64 builds
3. **File System Events**: Check FSEvents integration
4. **Memory Pressure**: Monitor memory usage patterns

### Testing Priorities
1. Large repository performance (>10k commits, >1k branches)
2. SSH authentication on all platforms
3. GPG signing with different key types
4. Cross-platform file watching reliability
5. Memory usage during extended sessions

## Quick Fixes

These can be addressed immediately:

1. **#1650**: Update `build/resources/_common/applications/sourcegit.desktop`
   - Remove or correct `MimeType=inode/directory`
   
2. **#1655**: Implement auto-scroll in `Views/DiffView.axaml.cs`
   - Add logic to scroll to first change when full file view is enabled

## Long-term Improvements

1. **Performance Monitoring**: Add telemetry for performance metrics
2. **Error Reporting**: Improve crash reporting with stack traces
3. **Automated Testing**: Implement E2E tests for critical workflows
4. **Memory Profiling**: Regular profiling to catch leaks early

## Resources

- GitHub Issues: https://github.com/sourcegit-scm/sourcegit/issues
- Pull Requests: https://github.com/sourcegit-scm/sourcegit/pulls
- Discussions: https://github.com/sourcegit-scm/sourcegit/discussions