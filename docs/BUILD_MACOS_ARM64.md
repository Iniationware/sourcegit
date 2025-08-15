# Building SourceGit for macOS ARM64 (Apple Silicon)

This guide explains how to build SourceGit as a native macOS application for Apple Silicon (M1/M2/M3) processors.

## Prerequisites

### Required Software
- **macOS**: 11.0 (Big Sur) or later
- **.NET SDK**: Version 9.0 or later
- **Xcode Command Line Tools**: For code signing

### Install Prerequisites

1. **Install .NET SDK 9.0**:
   ```bash
   # Download from https://dotnet.microsoft.com/download/dotnet/9.0
   # Or use Homebrew:
   brew install dotnet
   ```

2. **Verify .NET installation**:
   ```bash
   dotnet --version
   # Should output: 9.0.x or later
   ```

3. **Install Xcode Command Line Tools** (if not already installed):
   ```bash
   xcode-select --install
   ```

## Build Instructions

### Step 1: Clone the Repository
```bash
git clone https://github.com/sourcegit-scm/sourcegit.git
cd sourcegit
```

### Step 2: Restore Dependencies
```bash
dotnet restore
```

### Step 3: Build for macOS ARM64
```bash
# Create a publish directory
dotnet publish src/SourceGit.csproj \
  -c Release \
  -r osx-arm64 \
  -o publish-mac \
  --self-contained
```

**Build Parameters Explained:**
- `-c Release`: Build in Release configuration (optimized)
- `-r osx-arm64`: Target runtime for Apple Silicon
- `-o publish-mac`: Output directory
- `--self-contained`: Include .NET runtime (no separate installation needed)

### Step 4: Create macOS App Bundle

1. **Create the app bundle structure**:
   ```bash
   mkdir -p SourceGit.app/Contents/MacOS
   mkdir -p SourceGit.app/Contents/Resources
   ```

2. **Copy the built files**:
   ```bash
   cp -r publish-mac/* SourceGit.app/Contents/MacOS/
   ```

3. **Add the app icon**:
   ```bash
   cp build/resources/app/App.icns SourceGit.app/Contents/Resources/
   ```

4. **Create Info.plist with version**:
   ```bash
   # Read version from VERSION file
   VERSION=$(cat VERSION)
   
   # Generate Info.plist
   sed "s/SOURCE_GIT_VERSION/$VERSION/g" \
     build/resources/app/App.plist > SourceGit.app/Contents/Info.plist
   ```

### Step 5: Set Permissions and Sign

1. **Make the executable runnable**:
   ```bash
   chmod +x SourceGit.app/Contents/MacOS/SourceGit
   ```

2. **Remove quarantine attributes**:
   ```bash
   xattr -cr SourceGit.app
   ```

3. **Apply ad-hoc code signing**:
   ```bash
   codesign --force --deep --sign - SourceGit.app
   ```

## Installation

### Option 1: Using Finder
Simply drag `SourceGit.app` to your `/Applications` folder.

### Option 2: Using Terminal
```bash
cp -r SourceGit.app /Applications/
```

## First Launch

When launching SourceGit for the first time:

1. **macOS Gatekeeper Warning**: You may see a security warning. Go to System Preferences → Security & Privacy and click "Open Anyway".

2. **Git Credential Manager**: Ensure you have git-credential-manager installed:
   ```bash
   brew install --cask git-credential-manager
   ```

3. **Custom PATH** (optional): If you need custom PATH variables:
   ```bash
   echo $PATH > ~/Library/Application\ Support/SourceGit/PATH
   ```

## Troubleshooting

### App Won't Open
If macOS refuses to open the app:
```bash
sudo xattr -cr /Applications/SourceGit.app
sudo codesign --force --deep --sign - /Applications/SourceGit.app
```

### Missing Git
SourceGit requires Git >= 2.25.1. Install via Homebrew:
```bash
brew install git
```

### Performance Issues
For optimal performance on Apple Silicon, ensure you built with `-r osx-arm64` and not `-r osx-x64`.

## Build Script

For convenience, here's a complete build script:

```bash
#!/bin/bash
set -e

# Configuration
VERSION=$(cat VERSION)
PUBLISH_DIR="publish-mac"
APP_NAME="SourceGit.app"

echo "Building SourceGit v$VERSION for macOS ARM64..."

# Clean previous builds
rm -rf $PUBLISH_DIR $APP_NAME

# Restore and build
dotnet restore
dotnet publish src/SourceGit.csproj \
  -c Release \
  -r osx-arm64 \
  -o $PUBLISH_DIR \
  --self-contained

# Create app bundle
mkdir -p $APP_NAME/Contents/{MacOS,Resources}
cp -r $PUBLISH_DIR/* $APP_NAME/Contents/MacOS/
cp build/resources/app/App.icns $APP_NAME/Contents/Resources/
sed "s/SOURCE_GIT_VERSION/$VERSION/g" \
  build/resources/app/App.plist > $APP_NAME/Contents/Info.plist

# Set permissions and sign
chmod +x $APP_NAME/Contents/MacOS/SourceGit
xattr -cr $APP_NAME
codesign --force --deep --sign - $APP_NAME

echo "✅ Build complete! App size: $(du -sh $APP_NAME | cut -f1)"
echo "📁 Location: $(pwd)/$APP_NAME"
echo "🚀 To install: cp -r $APP_NAME /Applications/"
```

Save this as `build-macos.sh`, make it executable (`chmod +x build-macos.sh`), and run with `./build-macos.sh`.

## Additional Notes

- **App Size**: The final app bundle is approximately 200-220 MB (self-contained with .NET runtime)
- **Architecture**: This build is optimized for Apple Silicon (ARM64)
- **Compatibility**: The app requires macOS 11.0 or later
- **Updates**: SourceGit includes a built-in update checker for new versions

## Support

For issues or questions:
- GitHub Issues: https://github.com/sourcegit-scm/sourcegit/issues
- Documentation: https://github.com/sourcegit-scm/sourcegit/wiki