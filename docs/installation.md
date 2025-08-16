# Installation Guide

This guide provides detailed installation instructions for SourceGit on all supported platforms.

## Prerequisites

### Required Software
- **Git** version 2.25.1 or higher ([Download Git](https://git-scm.com/downloads))
- **Git Credential Manager** (recommended for authentication)
  - [Download for all platforms](https://github.com/git-ecosystem/git-credential-manager/releases)

### System Requirements
- **Windows**: Windows 10 version 1809+ or Windows 11
- **macOS**: macOS 10.15 (Catalina) or later
- **Linux**: Debian 12, Ubuntu 20.04+, Fedora 38+, or compatible distributions

---

## 🪟 Windows Installation

### Method 1: Windows Package Manager (Winget)
```powershell
# Install latest stable version
winget install SourceGit

# First run from command line or Win+R
SourceGit
```

> **Note**: After first launch, you can pin SourceGit to taskbar for easy access.

### Method 2: Scoop Package Manager
```powershell
# Add extras bucket
scoop bucket add extras

# Install SourceGit
scoop install sourcegit
```

### Method 3: Direct Download
1. Visit [Latest Release](https://github.com/sourcegit-scm/sourcegit/releases/latest)
2. Download one of:
   - `SourceGit_x.x.x_win_x64.msi` - Installer (recommended)
   - `SourceGit_x.x.x_win_x64.zip` - Portable version

### Windows-Specific Notes
- ⚠️ **MSYS Git is NOT supported** - Use official [Git for Windows](https://git-scm.com/download/win)
- Portable mode: Create a `data` folder next to `SourceGit.exe` to store settings locally

---

## 🍎 macOS Installation

### Method 1: Homebrew (Recommended)
```bash
# Add SourceGit tap
brew tap ybeapps/homebrew-sourcegit

# Install SourceGit
brew install --cask --no-quarantine sourcegit
```

### Method 2: Manual Installation
1. Download `SourceGit_x.x.x_osx-x64.zip` or `SourceGit_x.x.x_osx-arm64.zip` from [Releases](https://github.com/sourcegit-scm/sourcegit/releases/latest)
2. Extract and move `SourceGit.app` to `/Applications`
3. **Important**: Remove quarantine attribute:
   ```bash
   sudo xattr -cr /Applications/SourceGit.app
   ```

### macOS-Specific Configuration

#### Setting up PATH Environment
If Git or other tools aren't detected, create a custom PATH file:
```bash
echo $PATH > ~/Library/Application\ Support/SourceGit/PATH
```

#### Git Credential Manager
Install git-credential-manager for authentication:
```bash
brew install --cask git-credential-manager
```

---

## 🐧 Linux Installation

### Debian/Ubuntu (APT)
```bash
# Add GPG key
curl https://codeberg.org/api/packages/yataro/debian/repository.key | sudo tee /etc/apt/keyrings/sourcegit.asc

# Add repository
echo "deb [signed-by=/etc/apt/keyrings/sourcegit.asc, arch=amd64,arm64] https://codeberg.org/api/packages/yataro/debian generic main" | \
  sudo tee /etc/apt/sources.list.d/sourcegit.list

# Update and install
sudo apt update
sudo apt install sourcegit
```

### Fedora/RHEL (DNF/YUM)
```bash
# Download repo configuration
curl https://codeberg.org/api/packages/yataro/rpm.repo | sed -e 's/gpgcheck=1/gpgcheck=0/' > sourcegit.repo

# Add repository (Fedora 41+)
sudo dnf config-manager addrepo --from-repofile=./sourcegit.repo

# For Fedora 40 and earlier:
# sudo dnf config-manager --add-repo ./sourcegit.repo

# Install
sudo dnf install sourcegit
```

### openSUSE (Zypper)
```bash
# Add repository
sudo zypper addrepo https://codeberg.org/api/packages/yataro/rpm.repo

# Install
sudo zypper install sourcegit
```

### AppImage (Universal)
1. Download from [AppImage Hub](https://appimage.github.io/SourceGit/)
2. Make executable:
   ```bash
   chmod +x SourceGit-*.AppImage
   ```
3. Run:
   ```bash
   ./SourceGit-*.AppImage
   ```

### Linux-Specific Configuration

#### Display Scaling
For HiDPI displays, set the scaling factor:
```bash
export AVALONIA_SCREEN_SCALE_FACTORS="1.5"  # Adjust value as needed
```

#### Input Method Issues
If you cannot type accented characters (é, ñ, etc.):
```bash
export AVALONIA_IM_MODULE=none
```

#### Git Credential Storage
Install one of:
- `git-credential-manager` (recommended)
- `git-credential-libsecret` (GNOME/KDE)

```bash
# For Ubuntu/Debian
sudo apt install git-credential-libsecret

# For Fedora
sudo dnf install git-credential-libsecret
```

---

## 📁 Data Storage Locations

SourceGit stores user settings, avatars, and logs in platform-specific locations:

| Platform | Location |
|----------|----------|
| Windows | `%APPDATA%\SourceGit` |
| macOS | `~/Library/Application Support/SourceGit` |
| Linux | `~/.config/SourceGit` or `~/.sourcegit` |

### Portable Mode (Windows Only)
Create a `data` folder in the same directory as `SourceGit.exe` to enable portable mode.

---

## 🔧 Post-Installation Setup

### 1. Verify Git Installation
```bash
git --version
```
Should output version 2.25.1 or higher.

### 2. Configure Git Credentials
```bash
# Set up credential manager
git config --global credential.helper manager

# For Linux with libsecret
git config --global credential.helper libsecret
```

### 3. Launch SourceGit
- **Windows**: Start Menu or Desktop shortcut
- **macOS**: Applications folder or Launchpad
- **Linux**: Application menu or terminal command `sourcegit`

### 4. Initial Configuration
On first launch, SourceGit will:
- Detect Git installation
- Create configuration directory
- Set up default preferences

---

## 🚨 Troubleshooting

### Common Issues

#### Git Not Found
- Ensure Git is installed and in PATH
- Restart SourceGit after installing Git
- On macOS/Linux, create custom PATH file (see platform-specific sections)

#### Permission Denied (macOS)
```bash
sudo xattr -cr /Applications/SourceGit.app
```

#### Cannot Type Special Characters (Linux)
```bash
export AVALONIA_IM_MODULE=none
```

#### High DPI Display Issues (Linux)
```bash
export AVALONIA_SCREEN_SCALE_FACTORS="2"  # For 200% scaling
```

### Getting Help
- Check [Troubleshooting Guide](./troubleshooting.md)
- Visit [GitHub Issues](https://github.com/sourcegit-scm/sourcegit/issues)
- Join [Discussions](https://github.com/sourcegit-scm/sourcegit/discussions)

---

## 📦 Building from Source

If you prefer to build SourceGit yourself:

```bash
# Clone repository
git clone https://github.com/sourcegit-scm/sourcegit.git
cd sourcegit

# Install .NET SDK (if not installed)
# Visit: https://dotnet.microsoft.com/download

# Restore dependencies
dotnet restore

# Build
dotnet build -c Release

# Run
dotnet run --project src/SourceGit.csproj
```

### Publishing Platform-Specific Builds
```bash
# Windows
dotnet publish src/SourceGit.csproj -c Release -r win-x64

# macOS (Intel)
dotnet publish src/SourceGit.csproj -c Release -r osx-x64

# macOS (Apple Silicon)
dotnet publish src/SourceGit.csproj -c Release -r osx-arm64

# Linux
dotnet publish src/SourceGit.csproj -c Release -r linux-x64
```