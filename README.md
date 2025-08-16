<div align="center">
  <img src="./build/resources/_common/icons/logo.svg" width="92" height="92" alt="SourceGit Logo"/>
  
  # SourceGit

  **🚀 Fast, Free & Opensource Git GUI Client**

  [![GitHub Stars](https://img.shields.io/github/stars/sourcegit-scm/sourcegit?style=flat-square&logo=github)](https://github.com/sourcegit-scm/sourcegit/stargazers)
  [![Latest Release](https://img.shields.io/github/v/release/sourcegit-scm/sourcegit?style=flat-square&logo=github)](https://github.com/sourcegit-scm/sourcegit/releases/latest)
  [![License](https://img.shields.io/github/license/sourcegit-scm/sourcegit?style=flat-square)](LICENSE)
  [![Downloads](https://img.shields.io/github/downloads/sourcegit-scm/sourcegit/total?style=flat-square)](https://github.com/sourcegit-scm/sourcegit/releases)
  [![Build Status](https://img.shields.io/github/actions/workflow/status/sourcegit-scm/sourcegit/ci.yml?branch=develop&style=flat-square)](https://github.com/sourcegit-scm/sourcegit/actions)

  [**Download**](https://github.com/sourcegit-scm/sourcegit/releases/latest) • 
  [**Documentation**](./docs/README.md) • 
  [**Contributing**](#-contributing) • 
  [**Screenshots**](#-screenshots)

</div>

---

## ✨ Why SourceGit?

SourceGit is a powerful Git GUI client designed to make version control intuitive and efficient. Built with modern .NET and Avalonia UI, it delivers native performance across all major platforms while remaining completely free and open source.

### 🎯 Key Features

<table>
<tr>
<td width="50%">

**🖥️ Cross-Platform**
- Native support for Windows, macOS, and Linux
- Consistent experience across all platforms
- Portable mode available

</td>
<td width="50%">

**🎨 Beautiful Interface**
- Built-in light/dark themes
- Customizable themes
- Clean, modern design

</td>
</tr>
<tr>
<td width="50%">

**⚡ Lightning Fast**
- Optimized Git operations with intelligent caching
- Parallel command execution
- Native performance with .NET 9

</td>
<td width="50%">

**🌍 International**
- 12 languages supported
- Active translation community
- RTL language support

</td>
</tr>
<tr>
<td width="50%">

**🔧 Comprehensive Git Support**
- Full Git and Git-Flow workflows
- Interactive rebase & cherry-pick
- Submodules, worktrees, and stashes
- SSH key management per remote

</td>
<td width="50%">

**🤖 Smart Features**
- AI-powered commit messages
- Visual commit graph
- Advanced diff viewer with image support
- Issue tracker integration

</td>
</tr>
</table>

## 🚀 Quick Start

### Prerequisites
- **Git** ≥ 2.25.1 must be installed ([Download Git](https://git-scm.com/downloads))
- **Git Credential Manager** recommended for authentication

### Installation

<details>
<summary><b>🪟 Windows</b></summary>

#### Option 1: Winget (Recommended)
```powershell
winget install SourceGit
```

#### Option 2: Scoop
```powershell
scoop bucket add extras
scoop install sourcegit
```

#### Option 3: Direct Download
Download the latest `.msi` or `.zip` from [Releases](https://github.com/sourcegit-scm/sourcegit/releases/latest)

> **Note**: MSYS Git is not supported. Use official Git for Windows.

</details>

<details>
<summary><b>🍎 macOS</b></summary>

#### Option 1: Homebrew (Recommended)
```bash
brew tap ybeapps/homebrew-sourcegit
brew install --cask --no-quarantine sourcegit
```

#### Option 2: Direct Download
1. Download from [Releases](https://github.com/sourcegit-scm/sourcegit/releases/latest)
2. Run: `sudo xattr -cr /Applications/SourceGit.app`

</details>

<details>
<summary><b>🐧 Linux</b></summary>

#### Debian/Ubuntu
```bash
curl https://codeberg.org/api/packages/yataro/debian/repository.key | sudo tee /etc/apt/keyrings/sourcegit.asc
echo "deb [signed-by=/etc/apt/keyrings/sourcegit.asc] https://codeberg.org/api/packages/yataro/debian generic main" | sudo tee /etc/apt/sources.list.d/sourcegit.list
sudo apt update && sudo apt install sourcegit
```

#### Fedora/RHEL
```bash
sudo dnf config-manager --add-repo https://codeberg.org/api/packages/yataro/rpm.repo
sudo dnf install sourcegit
```

#### AppImage
Available on [AppImage Hub](https://appimage.github.io/SourceGit/)

</details>

> 📖 For detailed installation instructions, see [Installation Guide](./docs/installation.md)

## 🎨 Screenshots

<div align="center">
  <img src="./screenshots/theme_dark.png" alt="Dark Theme" width="49%"/>
  <img src="./screenshots/theme_light.png" alt="Light Theme" width="49%"/>
</div>

> 🎨 Custom themes available at [sourcegit-theme](https://github.com/sourcegit-scm/sourcegit-theme)

## 🛠️ Advanced Features

### Git Operations
- **Comprehensive Git support**: Clone, fetch, pull, push, merge, rebase, cherry-pick
- **Git-Flow workflows**: Full support with optimized performance
- **Visual tools**: Commit graph, blame view, file history
- **Advanced diff**: Side-by-side, inline, and image diff modes

### Productivity Tools
- **AI Commit Messages**: OpenAI-compatible API support
- **External IDE Integration**: VS Code, JetBrains, Sublime Text, and more
- **Custom Actions**: Define your own Git workflows
- **Issue Tracking**: Link commits to issues automatically

### Performance Optimizations
- **Intelligent Caching**: 60-80% faster repeated operations
- **Batch Processing**: Parallel execution for multiple queries
- **Resource Management**: Automatic process pooling and cleanup
- **Git-Flow Optimization**: 40-60% faster workflow operations

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](./docs/contributing.md) for details.

### Development Setup

```bash
# Clone the repository
git clone https://github.com/sourcegit-scm/sourcegit.git
cd sourcegit

# Restore dependencies
dotnet restore

# Build and run
dotnet build
dotnet run --project src/SourceGit.csproj
```

### Contributors

<a href="https://github.com/sourcegit-scm/sourcegit/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=sourcegit-scm/sourcegit&columns=12" />
</a>

## 📚 Documentation

- [**Installation Guide**](./docs/installation.md) - Detailed platform-specific instructions
- [**User Manual**](./docs/user-manual.md) - Complete feature documentation
- [**Configuration**](./docs/configuration.md) - Settings and customization
- [**External Tools**](./docs/external-tools.md) - IDE integration setup
- [**Troubleshooting**](./docs/troubleshooting.md) - Common issues and solutions
- [**Translation Status**](./TRANSLATION.md) - Help translate SourceGit

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [AvaloniaUI](https://avaloniaui.net/) for cross-platform UI
- AI commit messages inspired by [commitollama](https://github.com/anjerodev/commitollama)
- See [Third-Party Licenses](THIRD-PARTY-LICENSES.md) for all dependencies

---

<div align="center">
  <b>⭐ Star us on GitHub if you find SourceGit helpful!</b>
  
  [Report Bug](https://github.com/sourcegit-scm/sourcegit/issues) • 
  [Request Feature](https://github.com/sourcegit-scm/sourcegit/issues) • 
  [Discussions](https://github.com/sourcegit-scm/sourcegit/discussions)
</div>