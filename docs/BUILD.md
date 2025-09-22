# SourceGit Build and Deployment Documentation

## Table of Contents
1. [Build Requirements](#build-requirements)
2. [Development Builds](#development-builds)
3. [Production Builds](#production-builds)
4. [Platform-Specific Builds](#platform-specific-builds)
5. [CI/CD Pipeline](#cicd-pipeline)
6. [Release Process](#release-process)
7. [Packaging](#packaging)
8. [Code Signing](#code-signing)
9. [Deployment](#deployment)
10. [Troubleshooting](#troubleshooting)

## Build Requirements

### Minimum Requirements
- **.NET 9 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Git**: Version 2.25 or higher
- **Git LFS**: For binary assets
- **OS Requirements**:
  - Windows 10 1903+ (for Windows builds)
  - macOS 11+ (for macOS builds)
  - Ubuntu 20.04+ (for Linux builds)

### Optional Tools
- **Docker**: For containerized builds
- **GitHub CLI**: For release automation
- **Code signing certificates**: For signed releases

## Development Builds

### Quick Start
```bash
# Clone and setup
git clone https://github.com/Iniationware/sourcegit.git
cd sourcegit

# Restore dependencies
dotnet restore

# Build debug version
dotnet build -c Debug

# Run application
dotnet run --project src/SourceGit.csproj
```

### Debug Configuration
```xml
<!-- Directory.Build.props for debug settings -->
<Project>
  <PropertyGroup Condition="'$(Configuration)' == 'Debug'">
    <DebugType>full</DebugType>
    <DebugSymbols>true</DebugSymbols>
    <Optimize>false</Optimize>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
  </PropertyGroup>
</Project>
```

### Development Build Script
```bash
#!/bin/bash
# build-dev.sh

echo "Building SourceGit Development Version..."

# Clean previous builds
dotnet clean -c Debug
rm -rf src/bin/Debug
rm -rf src/obj/Debug

# Restore packages
dotnet restore

# Build
dotnet build -c Debug --no-restore

# Run tests (when available)
# dotnet test -c Debug --no-build

echo "Development build complete!"
```

## Production Builds

### Release Configuration
```xml
<!-- Directory.Build.props for release settings -->
<Project>
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <DebugType>embedded</DebugType>
    <DebugSymbols>true</DebugSymbols>
    <Optimize>true</Optimize>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <PublishReadyToRun>true</PublishReadyToRun>
    <PublishTrimmed>false</PublishTrimmed>
  </PropertyGroup>
</Project>
```

### Optimization Settings
```bash
# Full optimization build
dotnet publish -c Release \
  -p:PublishReadyToRun=true \
  -p:TieredCompilation=true \
  -p:TieredCompilationQuickJit=false
```

## Platform-Specific Builds

### Windows Build

#### x64 Build
```bash
# Framework-dependent
dotnet publish src/SourceGit.csproj \
  -c Release \
  -r win-x64 \
  --self-contained false \
  -o artifacts/win-x64

# Self-contained
dotnet publish src/SourceGit.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o artifacts/win-x64-sc
```

#### Windows Packaging Script
```powershell
# build-windows.ps1
param(
    [string]$Version = "2025.34.11",
    [switch]$Sign = $false
)

Write-Host "Building SourceGit for Windows..."

# Build
dotnet publish src/SourceGit.csproj `
    -c Release `
    -r win-x64 `
    --self-contained `
    -p:PublishSingleFile=true `
    -o "artifacts/win-x64"

# Create ZIP
Compress-Archive -Path "artifacts/win-x64/*" `
    -DestinationPath "artifacts/SourceGit-$Version-win-x64.zip"

# Sign if requested
if ($Sign) {
    & signtool sign /f certificate.pfx /p $env:CERT_PASSWORD `
        /t http://timestamp.digicert.com `
        "artifacts/win-x64/SourceGit.exe"
}

Write-Host "Windows build complete!"
```

### macOS Build

#### Universal Binary Build
```bash
#!/bin/bash
# build-macos.sh

VERSION="2025.34.11"

echo "Building SourceGit for macOS..."

# Build for Intel
dotnet publish src/SourceGit.csproj \
  -c Release \
  -r osx-x64 \
  --self-contained \
  -o artifacts/osx-x64

# Build for Apple Silicon
dotnet publish src/SourceGit.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained \
  -o artifacts/osx-arm64

# Create universal binary (optional)
mkdir -p artifacts/osx-universal
lipo -create \
  artifacts/osx-x64/SourceGit \
  artifacts/osx-arm64/SourceGit \
  -output artifacts/osx-universal/SourceGit

echo "macOS build complete!"
```

#### macOS DMG Creation
```bash
#!/bin/bash
# create-dmg.sh

VERSION="2025.34.11"
APP_NAME="SourceGit"

# Create app bundle structure
mkdir -p "$APP_NAME.app/Contents/MacOS"
mkdir -p "$APP_NAME.app/Contents/Resources"

# Copy files
cp -r artifacts/osx-arm64/* "$APP_NAME.app/Contents/MacOS/"
cp build/resources/App.icns "$APP_NAME.app/Contents/Resources/"

# Create Info.plist
cat > "$APP_NAME.app/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>SourceGit</string>
    <key>CFBundleIdentifier</key>
    <string>com.iniationware.sourcegit</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundleName</key>
    <string>SourceGit</string>
    <key>CFBundleIconFile</key>
    <string>App.icns</string>
    <key>LSMinimumSystemVersion</key>
    <string>11.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
EOF

# Sign the app (requires Developer ID)
codesign --deep --force --verify --verbose \
  --sign "Developer ID Application: Your Name" \
  "$APP_NAME.app"

# Create DMG
create-dmg \
  --volname "$APP_NAME" \
  --window-pos 200 120 \
  --window-size 600 400 \
  --icon-size 100 \
  --icon "$APP_NAME.app" 150 185 \
  --hide-extension "$APP_NAME.app" \
  --app-drop-link 450 185 \
  "SourceGit-$VERSION.dmg" \
  "$APP_NAME.app"

# Notarize (requires Apple Developer account)
xcrun altool --notarize-app \
  --primary-bundle-id "com.iniationware.sourcegit" \
  --username "apple-id@example.com" \
  --password "@keychain:AC_PASSWORD" \
  --file "SourceGit-$VERSION.dmg"

echo "DMG created and notarized!"
```

### Linux Build

#### Multi-Distribution Build
```bash
#!/bin/bash
# build-linux.sh

VERSION="2025.34.11"

echo "Building SourceGit for Linux..."

# Build for x64
dotnet publish src/SourceGit.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained \
  -o artifacts/linux-x64

# Build for ARM64
dotnet publish src/SourceGit.csproj \
  -c Release \
  -r linux-arm64 \
  --self-contained \
  -o artifacts/linux-arm64

echo "Linux builds complete!"
```

#### AppImage Creation
```bash
#!/bin/bash
# create-appimage.sh

VERSION="2025.34.11"
ARCH="x86_64"

# Setup AppDir
mkdir -p AppDir/usr/bin
mkdir -p AppDir/usr/share/applications
mkdir -p AppDir/usr/share/icons

# Copy application
cp -r artifacts/linux-x64/* AppDir/usr/bin/

# Create desktop file
cat > AppDir/usr/share/applications/sourcegit.desktop << EOF
[Desktop Entry]
Type=Application
Name=SourceGit
Comment=Cross-platform Git GUI Client
Exec=sourcegit %F
Icon=sourcegit
Terminal=false
Categories=Development;
MimeType=x-scheme-handler/git;
EOF

# Copy icon
cp build/resources/logo.png AppDir/usr/share/icons/sourcegit.png

# Create AppRun
cat > AppDir/AppRun << 'EOF'
#!/bin/bash
SELF=$(readlink -f "$0")
HERE=${SELF%/*}
export PATH="${HERE}/usr/bin:${PATH}"
exec "${HERE}/usr/bin/SourceGit" "$@"
EOF

chmod +x AppDir/AppRun

# Download appimagetool
wget -c https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage
chmod +x appimagetool-x86_64.AppImage

# Create AppImage
./appimagetool-x86_64.AppImage AppDir SourceGit-$VERSION-$ARCH.AppImage

echo "AppImage created!"
```

#### DEB Package Creation
```bash
#!/bin/bash
# create-deb.sh

VERSION="2025.34.11"
ARCH="amd64"

# Create package structure
mkdir -p sourcegit_${VERSION}/DEBIAN
mkdir -p sourcegit_${VERSION}/usr/bin
mkdir -p sourcegit_${VERSION}/usr/share/applications
mkdir -p sourcegit_${VERSION}/usr/share/icons

# Copy files
cp -r artifacts/linux-x64/* sourcegit_${VERSION}/usr/bin/
cp build/resources/logo.png sourcegit_${VERSION}/usr/share/icons/sourcegit.png

# Create control file
cat > sourcegit_${VERSION}/DEBIAN/control << EOF
Package: sourcegit
Version: $VERSION
Section: devel
Priority: optional
Architecture: $ARCH
Maintainer: Iniationware <support@iniationware.com>
Description: Cross-platform Git GUI Client
 SourceGit is a powerful, cross-platform Git GUI client
 built with .NET and Avalonia UI.
Depends: git (>= 2.25)
EOF

# Create desktop file
cat > sourcegit_${VERSION}/usr/share/applications/sourcegit.desktop << EOF
[Desktop Entry]
Type=Application
Name=SourceGit
Comment=Cross-platform Git GUI Client
Exec=/usr/bin/sourcegit %F
Icon=/usr/share/icons/sourcegit.png
Terminal=false
Categories=Development;
EOF

# Build package
dpkg-deb --build sourcegit_${VERSION}
mv sourcegit_${VERSION}.deb sourcegit_${VERSION}_${ARCH}.deb

echo "DEB package created!"
```

## CI/CD Pipeline

### GitHub Actions Workflow
```yaml
# .github/workflows/release.yml
name: Release Build

on:
  push:
    tags:
      - 'v*'

jobs:
  build-windows:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
        with:
          lfs: true

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Build Windows
        run: |
          dotnet publish src/SourceGit.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o artifacts/win-x64

      - name: Create ZIP
        run: Compress-Archive -Path artifacts/win-x64/* -DestinationPath SourceGit-${{ github.ref_name }}-win-x64.zip

      - name: Upload artifact
        uses: actions/upload-artifact@v3
        with:
          name: windows-build
          path: SourceGit-*.zip

  build-macos:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v3
        with:
          lfs: true

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Build macOS
        run: |
          dotnet publish src/SourceGit.csproj -c Release -r osx-arm64 --self-contained -o artifacts/osx-arm64
          dotnet publish src/SourceGit.csproj -c Release -r osx-x64 --self-contained -o artifacts/osx-x64

      - name: Create Archives
        run: |
          tar -czf SourceGit-${{ github.ref_name }}-osx-arm64.tar.gz -C artifacts osx-arm64
          tar -czf SourceGit-${{ github.ref_name }}-osx-x64.tar.gz -C artifacts osx-x64

      - name: Upload artifacts
        uses: actions/upload-artifact@v3
        with:
          name: macos-builds
          path: SourceGit-*.tar.gz

  build-linux:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
        with:
          lfs: true

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Build Linux
        run: |
          dotnet publish src/SourceGit.csproj -c Release -r linux-x64 --self-contained -o artifacts/linux-x64

      - name: Create AppImage
        run: |
          # AppImage creation script here
          ./build/create-appimage.sh

      - name: Upload artifacts
        uses: actions/upload-artifact@v3
        with:
          name: linux-builds
          path: |
            *.AppImage
            *.deb
            *.rpm

  create-release:
    needs: [build-windows, build-macos, build-linux]
    runs-on: ubuntu-latest
    steps:
      - name: Download artifacts
        uses: actions/download-artifact@v3

      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: |
            windows-build/*.zip
            macos-builds/*.tar.gz
            linux-builds/*
          generate_release_notes: true
          prerelease: ${{ contains(github.ref_name, 'beta') || contains(github.ref_name, 'rc') }}
```

## Release Process

### Version Management

#### Iniationware Versioning Pattern
```
Format: v{YEAR}.{WEEK}.{IW_INCREMENT}
Example: v2025.34.11
```

#### Version Update Script
```bash
#!/bin/bash
# update-version.sh

NEW_VERSION=$1

if [ -z "$NEW_VERSION" ]; then
    echo "Usage: ./update-version.sh <version>"
    exit 1
fi

# Update VERSION file
echo "$NEW_VERSION" > VERSION

# Update project file
sed -i "s/<Version>.*<\/Version>/<Version>$NEW_VERSION<\/Version>/" src/SourceGit.csproj

# Commit changes
git add VERSION src/SourceGit.csproj
git commit -m "chore: bump version to $NEW_VERSION"

echo "Version updated to $NEW_VERSION"
```

### Pre-Release Checklist

```bash
#!/bin/bash
# check_before_tag.sh

echo "Pre-release checks..."

# Check for uncommitted changes
if [ -n "$(git status --porcelain)" ]; then
    echo "❌ Uncommitted changes detected!"
    exit 1
fi

# Format check
echo "Running code formatting..."
dotnet format --verify-no-changes
if [ $? -ne 0 ]; then
    echo "❌ Code formatting issues detected!"
    exit 1
fi

# Build check
echo "Building project..."
dotnet build -c Release
if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi

# Test check (when available)
# dotnet test -c Release
# if [ $? -ne 0 ]; then
#     echo "❌ Tests failed!"
#     exit 1
# fi

echo "✅ All checks passed! Ready to tag."
```

### Creating a Release

```bash
#!/bin/bash
# create-release.sh

VERSION="2025.34.11"

# Run pre-release checks
./check_before_tag.sh

# Update version
./update-version.sh $VERSION

# Create tag
git tag -a "v$VERSION" -m "Release v$VERSION

## Changes
- Feature 1
- Feature 2
- Bug fixes

## Contributors
- @contributor1
- @contributor2"

# Push tag
git push origin "v$VERSION"

echo "Release v$VERSION created!"
```

## Packaging

### Windows Installer (WiX)
```xml
<!-- Product.wxs -->
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="*"
           Name="SourceGit"
           Language="1033"
           Version="2025.34.11"
           Manufacturer="Iniationware"
           UpgradeCode="YOUR-GUID-HERE">

    <Package InstallerVersion="200"
             Compressed="yes"
             InstallScope="perMachine" />

    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFilesFolder">
        <Directory Id="INSTALLFOLDER" Name="SourceGit">
          <Component Id="MainExecutable">
            <File Id="SourceGit.exe" Source="artifacts/win-x64/SourceGit.exe" />
          </Component>
        </Directory>
      </Directory>
    </Directory>

    <Feature Id="ProductFeature" Title="SourceGit" Level="1">
      <ComponentRef Id="MainExecutable" />
    </Feature>
  </Product>
</Wix>
```

### Chocolatey Package
```powershell
# sourcegit.nuspec
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2015/06/nuspec.xsd">
  <metadata>
    <id>sourcegit</id>
    <version>2025.34.11</version>
    <title>SourceGit</title>
    <authors>Iniationware</authors>
    <projectUrl>https://github.com/Iniationware/sourcegit</projectUrl>
    <iconUrl>https://raw.githubusercontent.com/Iniationware/sourcegit/main/icon.png</iconUrl>
    <licenseUrl>https://github.com/Iniationware/sourcegit/blob/main/LICENSE</licenseUrl>
    <description>Cross-platform Git GUI Client</description>
    <tags>git gui vcs</tags>
  </metadata>
  <files>
    <file src="tools\**" target="tools" />
  </files>
</package>
```

## Code Signing

### Windows Code Signing
```powershell
# Sign executable
signtool sign /f certificate.pfx /p $env:CERT_PASSWORD `
    /t http://timestamp.digicert.com `
    /d "SourceGit" `
    SourceGit.exe

# Verify signature
signtool verify /pa SourceGit.exe
```

### macOS Code Signing
```bash
# Sign app bundle
codesign --deep --force --verify --verbose \
    --sign "Developer ID Application: Your Name (TEAM_ID)" \
    --options runtime \
    --entitlements entitlements.plist \
    SourceGit.app

# Verify signature
codesign --verify --deep --strict --verbose=2 SourceGit.app

# Notarize
xcrun altool --notarize-app \
    --primary-bundle-id "com.iniationware.sourcegit" \
    --username "apple-id@example.com" \
    --password "@keychain:AC_PASSWORD" \
    --file SourceGit.dmg

# Staple notarization
xcrun stapler staple SourceGit.dmg
```

## Deployment

### GitHub Releases
Automated via GitHub Actions on tag push.

### Package Managers

#### Homebrew (macOS)
```ruby
# Formula/sourcegit.rb
class Sourcegit < Formula
  desc "Cross-platform Git GUI Client"
  homepage "https://github.com/Iniationware/sourcegit"
  version "2025.34.11"

  if OS.mac?
    if Hardware::CPU.arm?
      url "https://github.com/Iniationware/sourcegit/releases/download/v2025.34.11/SourceGit-2025.34.11-osx-arm64.tar.gz"
    else
      url "https://github.com/Iniationware/sourcegit/releases/download/v2025.34.11/SourceGit-2025.34.11-osx-x64.tar.gz"
    end
  end

  def install
    bin.install "SourceGit"
  end
end
```

#### Scoop (Windows)
```json
{
    "version": "2025.34.11",
    "description": "Cross-platform Git GUI Client",
    "homepage": "https://github.com/Iniationware/sourcegit",
    "license": "MIT",
    "url": "https://github.com/Iniationware/sourcegit/releases/download/v2025.34.11/SourceGit-2025.34.11-win-x64.zip",
    "bin": "SourceGit.exe",
    "shortcuts": [
        [
            "SourceGit.exe",
            "SourceGit"
        ]
    ]
}
```

## Troubleshooting

### Build Issues

#### Missing Dependencies
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Force restore
dotnet restore --force
```

#### Platform-Specific Issues
```bash
# Linux: Missing libicu
sudo apt-get install libicu-dev

# macOS: Missing Xcode tools
xcode-select --install

# Windows: Long path issues
git config --system core.longpaths true
```

#### Performance Issues
```bash
# Enable tiered compilation
export DOTNET_TieredCompilation=1
export DOTNET_TC_QuickJit=0

# Use ReadyToRun
dotnet publish -p:PublishReadyToRun=true
```

### Release Issues

#### Tag Already Exists
```bash
# Delete local tag
git tag -d v2025.34.11

# Delete remote tag
git push --delete origin v2025.34.11

# Recreate tag
git tag -a v2025.34.11 -m "Release message"
git push origin v2025.34.11
```

#### Failed CI Build
Check logs in GitHub Actions for specific errors.

### Deployment Issues

#### Notarization Failures (macOS)
```bash
# Check notarization status
xcrun altool --notarization-info <RequestUUID> \
    --username "apple-id@example.com" \
    --password "@keychain:AC_PASSWORD"

# View detailed log
xcrun altool --notarization-info <RequestUUID> \
    --username "apple-id@example.com" \
    --password "@keychain:AC_PASSWORD" \
    --output-format xml
```

## Conclusion

This build and deployment guide covers the complete process from development builds to production releases. Follow the platform-specific instructions for your target environment and use the automation scripts to streamline the process.

For additional support:
- [GitHub Issues](https://github.com/Iniationware/sourcegit/issues)
- [Developer Guide](./DEVELOPER_GUIDE.md)
- [Architecture Documentation](./ARCHITECTURE.md)