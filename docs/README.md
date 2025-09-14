# SourceGit Documentation - Iniationware Edition

Welcome to the comprehensive documentation for SourceGit Iniationware Edition.

## 📚 Documentation Structure

### Getting Started
- [Installation Guide](../README.md#-installation) - Platform-specific installation instructions
- [Building from Source](../README.md#-building-from-source) - Developer build instructions
- [System Requirements](../README.md#-system-requirements) - Minimum and recommended specifications

### Release Information
- [Release Notes](../RELEASE_NOTES.md) - Latest release features and improvements
- [Changelog](../CHANGELOG.md) - Complete version history
- [Versioning Strategy](../CLAUDE.md#versioning-and-release-strategy) - Version numbering system

### Developer Guides
- [macOS Code Signing](./MACOS_SIGNING.md) - Setting up signed DMG releases
- [Contributing Guide](../CLAUDE.md) - Development workflow and guidelines
- [Architecture Overview](../CLAUDE.md#architecture-overview) - Technical architecture details

### Features Documentation

#### Performance Enhancements
- **GitCommandCache**: 60-70% performance improvement through intelligent caching
- **GitProcessPool**: Efficient process management and resource pooling
- **BatchQueryExecutor**: Parallel Git operations for faster workflows
- **Memory Manager**: Automatic resource optimization and cleanup

#### Repository Management
- **Metrics Dashboard**: Real-time branch and commit statistics
- **Refresh Options**: Granular control over repository updates
- **Git-Flow Support**: Optimized branch operations and workflows
- **Credential Manager**: Secure storage and management

#### UI/UX Features
- **Memory Indicators**: Real-time resource usage display
- **Performance Metrics**: Operation timing and efficiency tracking
- **Theme Support**: Light/dark themes with customization
- **Localization**: Support for 11+ languages

## 🔧 Configuration

### Application Settings
- Settings stored in `%APPDATA%/SourceGit/` (Windows) or `~/.sourcegit/` (macOS/Linux)
- Portable mode: Create `data` folder next to executable
- Repository-specific settings in `.git/sourcegit.json`

### Environment Variables
```bash
# SSH Key Path
export GIT_SSH_COMMAND="ssh -i ~/.ssh/your_key"

# GPG Signing
export GPG_TTY=$(tty)

# Custom Git Path
export GIT_EXECUTABLE="/usr/local/bin/git"
```

## 🎯 Quick Reference

### Keyboard Shortcuts
| Action | Windows/Linux | macOS |
|--------|--------------|-------|
| New Tab | `Ctrl+T` | `Cmd+T` |
| Close Tab | `Ctrl+W` | `Cmd+W` |
| Search | `Ctrl+F` | `Cmd+F` |
| Refresh | `F5` | `Cmd+R` |
| Commit | `Ctrl+Enter` | `Cmd+Enter` |
| Push | `Ctrl+P` | `Cmd+P` |
| Pull | `Ctrl+Shift+P` | `Cmd+Shift+P` |
| Stash | `Ctrl+S` | `Cmd+S` |

### Command Line Arguments
```bash
# Open specific repository
sourcegit /path/to/repo

# Portable mode
sourcegit --portable

# Debug mode
sourcegit --debug

# Reset settings
sourcegit --reset
```

## 🐛 Troubleshooting

### Common Issues

#### macOS: "Application cannot be opened"
```bash
# Remove quarantine flag
xattr -cr /Applications/SourceGit.app

# Or right-click → Open on first launch
```

#### Linux: Missing dependencies
```bash
# Ubuntu/Debian
sudo apt-get install libicu66 libssl1.1

# Fedora/RHEL
sudo dnf install libicu openssl-libs
```

#### Windows: Git not found
1. Install Git for Windows from https://git-scm.com
2. Add Git to PATH or configure in SourceGit settings

### Performance Optimization

#### Large Repositories
- Enable lazy loading in settings
- Use refresh options to limit scope
- Configure cache size limits
- Disable automatic refresh for specific repos

#### Memory Usage
- Monitor with built-in performance metrics
- Adjust cache sizes in settings
- Enable automatic cleanup
- Use portable mode for isolated instances

## 📞 Support Channels

- **Bug Reports**: [GitHub Issues](https://github.com/Iniationware/sourcegit/issues)
- **Feature Requests**: [GitHub Discussions](https://github.com/Iniationware/sourcegit/discussions)
- **Security Issues**: Email security@iniationware.com
- **Community Chat**: [Discord Server](https://discord.gg/sourcegit)

## 🔗 Additional Resources

- [Original SourceGit Documentation](https://github.com/sourcegit-scm/sourcegit/docs)
- [Avalonia UI Documentation](https://docs.avaloniaui.net/)
- [Git Documentation](https://git-scm.com/doc)
- [.NET Documentation](https://docs.microsoft.com/dotnet/)

---

*Last updated: September 2025 - Version 2025.34.10*