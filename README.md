<div align="center">
  <img src="./build/resources/_common/icons/sourcegit.png" width="92" height="92" alt="SourceGit Logo"/>

  # SourceGit - Iniationware Edition

  **🚀 Enterprise-Ready Git GUI Client with Advanced Features**

  [![GitHub Stars](https://img.shields.io/github/stars/Iniationware/sourcegit?style=flat-square&logo=github)](https://github.com/Iniationware/sourcegit/stargazers)
  [![Latest Release](https://img.shields.io/github/v/release/Iniationware/sourcegit?style=flat-square&logo=github)](https://github.com/Iniationware/sourcegit/releases/latest)
  [![License](https://img.shields.io/github/license/Iniationware/sourcegit?style=flat-square)](LICENSE)
  [![Downloads](https://img.shields.io/github/downloads/Iniationware/sourcegit/total?style=flat-square)](https://github.com/Iniationware/sourcegit/releases)
  [![Build Status](https://img.shields.io/github/actions/workflow/status/Iniationware/sourcegit/ci.yml?branch=develop&style=flat-square)](https://github.com/Iniationware/sourcegit/actions)

  [**Download**](https://github.com/Iniationware/sourcegit/releases/latest) •
  [**Documentation**](./docs/README.md) •
  [**Changelog**](./CHANGELOG.md) •
  [**Contributing**](#-contributing)

</div>

---

## 🎯 About Iniationware Edition

The **Iniationware Edition** is an enterprise-enhanced fork of SourceGit, featuring advanced performance optimizations, comprehensive monitoring capabilities, and professional-grade tools for development teams.

### 🆕 What's Enhanced in v2025.34.10

- **⚡ 60-70% Performance Boost**: Intelligent caching and process pooling
- **📊 Repository Metrics Dashboard**: Real-time statistics and monitoring
- **🔧 Advanced Git Operations**: Optimized Git-Flow, batch operations
- **🛡️ Enterprise Security**: Secure credential management
- **📦 Professional Packaging**: Signed macOS DMGs, Linux AppImage/DEB/RPM
- **🌍 Full Localization**: Support for 11+ languages

## ✨ Key Features

### Core Capabilities
- ✅ **Cross-Platform**: Native support for Windows, macOS (Intel/ARM), Linux (x64/ARM64)
- ✅ **Beautiful UI**: Modern design with light/dark themes
- ✅ **Fast Performance**: Built with .NET 9 and Avalonia UI
- ✅ **Git-Flow Support**: Full Git-Flow workflow integration
- ✅ **SSH & GPG**: Built-in SSH and GPG key management
- ✅ **Diff Tools**: Advanced diff viewer with syntax highlighting

### Iniationware Enhancements
- ⚡ **Performance Monitor**: Real-time memory and CPU tracking
- ⚡ **Smart Caching**: Intelligent command result caching
- ⚡ **Batch Operations**: Parallel Git command execution
- ⚡ **Repository Metrics**: Branch counter, commit statistics
- ⚡ **Memory Manager**: Automatic resource optimization

## 📥 Installation

### Quick Install

#### Windows
Download the latest `.zip` from [Releases](https://github.com/Iniationware/sourcegit/releases/latest) and extract.

#### macOS
Download the `.dmg` (signed) or `.zip` from [Releases](https://github.com/Iniationware/sourcegit/releases/latest).
- **DMG**: Double-click to mount and drag to Applications
- **ZIP**: Extract and move to Applications, then right-click → Open

#### Linux
Available as AppImage, DEB, or RPM:
```bash
# AppImage
chmod +x sourcegit-*.AppImage
./sourcegit-*.AppImage

# Debian/Ubuntu
sudo dpkg -i sourcegit_*.deb

# Fedora/RHEL
sudo rpm -i sourcegit-*.rpm
```

## 🏗️ Building from Source

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Git 2.25+ with Git-LFS

### Build Commands
```bash
# Clone repository
git clone https://github.com/Iniationware/sourcegit.git
cd sourcegit

# Build
dotnet build -c Release

# Run
dotnet run --project src/SourceGit.csproj

# Publish for your platform
dotnet publish src/SourceGit.csproj -c Release -r win-x64
dotnet publish src/SourceGit.csproj -c Release -r osx-arm64
dotnet publish src/SourceGit.csproj -c Release -r linux-x64
```

## 📊 System Requirements

| Platform | Minimum | Recommended |
|----------|---------|-------------|
| **Windows** | Windows 10 1903+ | Windows 11 |
| **macOS** | macOS 11 Big Sur | macOS 14 Sonoma |
| **Linux** | Ubuntu 20.04 / Debian 11 | Ubuntu 22.04+ |
| **Memory** | 4GB RAM | 8GB+ RAM |
| **.NET** | .NET 9 Runtime | .NET 9 SDK |

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Development Workflow
1. Fork the repository
2. Create a feature branch (`feature/amazing-feature`)
3. Commit your changes
4. Push to your branch
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Original [SourceGit](https://github.com/sourcegit-scm/sourcegit) by love-hacker
- [Avalonia UI](https://avaloniaui.net/) for the cross-platform framework
- All contributors and community members

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/Iniationware/sourcegit/issues)
- **Discussions**: [GitHub Discussions](https://github.com/Iniationware/sourcegit/discussions)
- **Website**: [Iniationware](https://iniationware.com)

---

<div align="center">
  <sub>Built with ❤️ by <a href="https://github.com/Iniationware">Iniationware</a></sub>
</div>