# SourceGit Documentation

Welcome to the SourceGit documentation! This guide will help you get the most out of SourceGit, the fast and free Git GUI client.

## 📚 Documentation Structure

### Getting Started
- [**Installation Guide**](./installation.md) - Platform-specific installation instructions
- [**Configuration**](./configuration.md) - Settings and customization options
- [**External Tools**](./external-tools.md) - IDE and editor integration

### Development
- [**Contributing Guide**](./contributing.md) - How to contribute to SourceGit
- [**Architecture Overview**](../CLAUDE.md) - Technical architecture and development notes
- [**Bug & Performance Tracking**](./BUGS_AND_PERFORMANCE_TODO.md) - Current issues and improvements

### Reference
- [**Translation Status**](../TRANSLATION.md) - Localization progress
- [**Third-Party Licenses**](../THIRD-PARTY-LICENSES.md) - Dependencies and licenses

## 🎯 Quick Links

### For Users
- [Download Latest Release](https://github.com/sourcegit-scm/sourcegit/releases/latest)
- [Report a Bug](https://github.com/sourcegit-scm/sourcegit/issues/new)
- [Request a Feature](https://github.com/sourcegit-scm/sourcegit/issues/new)
- [Join Discussions](https://github.com/sourcegit-scm/sourcegit/discussions)

### For Developers
- [GitHub Repository](https://github.com/sourcegit-scm/sourcegit)
- [Build Instructions](./contributing.md#building-from-source)
- [API Documentation](./api/index.md) *(coming soon)*

## 🌟 Key Features

### Git Operations
- **Core Git Commands**: Clone, fetch, pull, push, merge, rebase
- **Advanced Features**: Interactive rebase, cherry-pick, bisect
- **Git-Flow Support**: Full workflow automation with performance optimization
- **Visual Tools**: Commit graph, blame view, file history

### User Interface
- **Themes**: Built-in light/dark themes with custom theme support
- **Languages**: 12+ languages with active translation community
- **Diff Viewer**: Side-by-side, inline, and image diff support
- **Search**: Fast commit and file searching

### Productivity
- **AI Integration**: Generate commit messages with OpenAI
- **External Tools**: Launch repositories in VS Code, JetBrains IDEs, and more
- **Performance**: 60-80% faster operations with intelligent caching
- **Workspaces**: Manage multiple repositories efficiently

## 📊 Performance Highlights

Recent optimizations have achieved:
- **60-80%** reduction in repeated Git queries
- **40-60%** faster Git-Flow operations
- **Up to 4x** speedup for batch operations
- **Intelligent caching** with automatic invalidation
- **Resource management** preventing exhaustion

## 🔍 Common Tasks

### Basic Workflow
1. **Clone a Repository**: File → Clone Repository
2. **Make Changes**: Edit files in your preferred editor
3. **Stage Changes**: Select files and stage them
4. **Commit**: Write message and commit
5. **Push**: Push changes to remote

### Git-Flow Workflow
1. **Initialize Git-Flow**: Repository → Git-Flow → Initialize
2. **Start Feature**: Git-Flow → Start Feature
3. **Develop Feature**: Make commits as needed
4. **Finish Feature**: Git-Flow → Finish Feature
5. **Create Release**: Git-Flow → Start Release

### Resolving Conflicts
1. **Identify Conflicts**: Look for conflict markers
2. **Open Merge Tool**: Right-click → Resolve Conflicts
3. **Choose Resolution**: Select changes to keep
4. **Mark Resolved**: Stage the resolved file
5. **Complete Merge**: Commit the resolution

## 🛠️ Troubleshooting

### Common Issues

**Git not detected**
- Ensure Git ≥ 2.25.1 is installed
- Check PATH environment variable
- Restart SourceGit after installing Git

**Cannot authenticate**
- Install Git Credential Manager
- Configure SSH keys if using SSH
- Check repository remote URLs

**Performance issues**
- Enable caching in preferences
- Adjust process pool size
- Check available system resources

**Display problems (Linux)**
- Set `AVALONIA_SCREEN_SCALE_FACTORS`
- Configure `AVALONIA_IM_MODULE=none` for input issues

## 💬 Getting Help

### Support Channels
- **GitHub Issues**: [Bug reports and feature requests](https://github.com/sourcegit-scm/sourcegit/issues)
- **Discussions**: [Community forum](https://github.com/sourcegit-scm/sourcegit/discussions)
- **Wiki**: [Additional guides and tips](https://github.com/sourcegit-scm/sourcegit/wiki)

### Reporting Issues
When reporting issues, please include:
- SourceGit version
- Operating system and version
- Git version (`git --version`)
- Steps to reproduce
- Error messages or logs

## 🤝 Contributing

We welcome contributions! See our [Contributing Guide](./contributing.md) for:
- Code contribution guidelines
- Translation help
- Documentation improvements
- Bug reporting best practices

## 📜 License

SourceGit is open source software licensed under the [MIT License](../LICENSE).

---

<div align="center">
  <b>Thank you for using SourceGit!</b>
  
  ⭐ Star us on [GitHub](https://github.com/sourcegit-scm/sourcegit) if you find it helpful!
</div>