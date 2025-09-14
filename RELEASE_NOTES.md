# Release Notes - v2025.34.10

## 🎉 Highlights

This release brings **professional macOS packaging** with code signing and notarization support, along with comprehensive documentation updates for the Iniationware Edition.

## ✨ New Features

### macOS DMG Packaging
- **Signed DMG Creation**: Automatic code signing when certificates are configured
- **Notarization Support**: Full Gatekeeper approval workflow
- **Dual Format**: Both ZIP and DMG formats available in releases
- **Fallback Mode**: Creates unsigned DMG when certificates not available

### Documentation Overhaul
- **Updated README**: Complete rewrite with Iniationware Edition features
- **CHANGELOG**: Full release history documentation
- **Signing Guide**: Step-by-step macOS signing setup instructions
- **Version Strategy**: Clear documentation of versioning approach

## 🔧 Technical Improvements

### Build System
- Added `package.osx-dmg.sh` script for DMG creation
- Integrated signing workflow into GitHub Actions
- Created entitlements.plist for proper app permissions
- Support for both signed and unsigned distribution

### Project Organization
- Consistent version format across all components
- Structured documentation in `/docs` directory
- Clear separation of Iniationware enhancements
- Professional release management

## 📦 Installation

### macOS Users
You now have two options:
1. **DMG (Recommended)**: Download, mount, drag to Applications
2. **ZIP**: Extract and right-click → Open on first launch

### Signing Status
- With configured secrets: Fully signed and notarized DMG
- Without secrets: Unsigned DMG (requires right-click → Open)

## 🔐 For Developers

### Setting Up Code Signing
See [docs/MACOS_SIGNING.md](docs/MACOS_SIGNING.md) for:
- Apple Developer account setup
- Certificate generation and export
- GitHub Secrets configuration
- Local testing instructions

### Required Secrets
```yaml
MACOS_CERTIFICATE      # Base64 .p12 certificate
MACOS_CERTIFICATE_PWD  # Certificate password
APPLE_ID              # For notarization
NOTARIZE_PASSWORD     # App-specific password
TEAM_ID               # Developer Team ID
```

## 📊 Statistics

- **Files Changed**: 8
- **Additions**: 500+ lines
- **Documentation**: 3 new guides
- **Platforms**: Full support for Windows, macOS, Linux (x64/ARM64)

## 🙏 Acknowledgments

Thanks to all contributors and users of the Iniationware Edition. Your feedback drives our continuous improvement.

## 📥 Download

Get the latest release from: [GitHub Releases](https://github.com/Iniationware/sourcegit/releases/latest)

---

*For the complete changelog, see [CHANGELOG.md](CHANGELOG.md)*