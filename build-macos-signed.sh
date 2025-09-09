#!/bin/bash

# SourceGit macOS Build Script with Proper Signing
# Compatible with macOS 15.6 (Sequoia) and higher
# 
# This script builds and signs SourceGit for distribution on modern macOS
# including proper entitlements, hardened runtime, and notarization preparation

set -e  # Exit on error

# Configuration
VERSION=$(cat VERSION)
APP_NAME="SourceGit"
BUNDLE_ID="com.sourcegit-scm.sourcegit"
BUILD_DIR="publish-mac-signed"
APP_BUNDLE="${APP_NAME}.app"
DMG_NAME="${APP_NAME}-${VERSION}-arm64.dmg"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Signing configuration (update these for your certificate)
# To find your identity: security find-identity -v -p codesigning
SIGNING_IDENTITY="${SIGNING_IDENTITY:-}"  # Use env var or set to your "Developer ID Application: Name (TEAMID)"
NOTARIZATION_TEAM_ID="${NOTARIZATION_TEAM_ID:-}"  # Your Apple Developer Team ID

# Function to print colored output
print_status() {
    echo -e "${BLUE}==>${NC} $1"
}

print_success() {
    echo -e "${GREEN}✓${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    print_status "Checking prerequisites..."
    
    # Check for .NET SDK
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK is not installed"
        echo "Install from: https://dotnet.microsoft.com/download/dotnet/9.0"
        exit 1
    fi
    
    # Check for Xcode command line tools
    if ! command -v codesign &> /dev/null; then
        print_error "Xcode command line tools are not installed"
        echo "Run: xcode-select --install"
        exit 1
    fi
    
    # Check .NET version
    DOTNET_VERSION=$(dotnet --version)
    print_success ".NET SDK ${DOTNET_VERSION} found"
    
    # Check if running on Apple Silicon
    ARCH=$(uname -m)
    if [[ "$ARCH" == "arm64" ]]; then
        print_success "Running on Apple Silicon (${ARCH})"
    else
        print_warning "Running on Intel Mac (${ARCH}), building for Apple Silicon"
    fi
    
    # Check for signing identity
    if [[ -n "$SIGNING_IDENTITY" ]]; then
        if security find-identity -v -p codesigning | grep -q "$SIGNING_IDENTITY"; then
            print_success "Signing identity found: ${SIGNING_IDENTITY}"
        else
            print_warning "Signing identity not found, will use ad-hoc signing"
            SIGNING_IDENTITY=""
        fi
    else
        print_warning "No signing identity specified, will use ad-hoc signing"
        echo "For distribution, set SIGNING_IDENTITY environment variable"
    fi
}

# Clean previous builds
clean_build() {
    print_status "Cleaning previous builds..."
    rm -rf "$BUILD_DIR"
    rm -rf "$APP_BUNDLE"
    rm -f *.dmg
    rm -f *.zip
    print_success "Cleaned previous builds"
}

# Build the application
build_app() {
    print_status "Building SourceGit ${VERSION} for macOS ARM64..."
    
    # Restore dependencies
    print_status "Restoring NuGet packages..."
    dotnet restore
    
    # Build with Release configuration and AOT
    print_status "Building application (this may take a few minutes)..."
    dotnet publish src/SourceGit.csproj \
        -c Release \
        -r osx-arm64 \
        -o "$BUILD_DIR" \
        --self-contained \
        -p:PublishAot=true \
        -p:PublishTrimmed=true \
        -p:TrimMode=link \
        -p:DebugType=none \
        -p:DebugSymbols=false
    
    print_success "Build completed"
}

# Create app bundle
create_bundle() {
    print_status "Creating app bundle..."
    
    # Create bundle structure
    mkdir -p "${APP_BUNDLE}/Contents/MacOS"
    mkdir -p "${APP_BUNDLE}/Contents/Resources"
    
    # Copy application files
    cp -r "${BUILD_DIR}"/* "${APP_BUNDLE}/Contents/MacOS/"
    
    # Copy icon
    if [[ -f "build/resources/app/App.icns" ]]; then
        cp "build/resources/app/App.icns" "${APP_BUNDLE}/Contents/Resources/"
        print_success "Icon copied"
    else
        print_warning "Icon file not found"
    fi
    
    # Create Info.plist
    cat > "${APP_BUNDLE}/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleIconFile</key>
    <string>App.icns</string>
    <key>CFBundleIdentifier</key>
    <string>${BUNDLE_ID}</string>
    <key>CFBundleName</key>
    <string>${APP_NAME}</string>
    <key>CFBundleDisplayName</key>
    <string>${APP_NAME}</string>
    <key>CFBundleVersion</key>
    <string>${VERSION}</string>
    <key>CFBundleShortVersionString</key>
    <string>${VERSION}</string>
    <key>CFBundleExecutable</key>
    <string>SourceGit</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>LSMinimumSystemVersion</key>
    <string>11.0</string>
    <key>LSApplicationCategoryType</key>
    <string>public.app-category.developer-tools</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSSupportsAutomaticGraphicsSwitching</key>
    <true/>
    <key>NSRequiresAquaSystemAppearance</key>
    <false/>
    <key>NSPrincipalClass</key>
    <string>NSApplication</string>
    <key>NSAppTransportSecurity</key>
    <dict>
        <key>NSAllowsArbitraryLoads</key>
        <false/>
    </dict>
</dict>
</plist>
EOF
    
    # Create entitlements file for hardened runtime
    cat > "entitlements.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <!-- Required for Git operations -->
    <key>com.apple.security.cs.allow-unsigned-executable-memory</key>
    <true/>
    <key>com.apple.security.cs.allow-jit</key>
    <true/>
    <key>com.apple.security.cs.disable-library-validation</key>
    <true/>
    
    <!-- File system access -->
    <key>com.apple.security.files.user-selected.read-write</key>
    <true/>
    <key>com.apple.security.files.bookmarks.app-scope</key>
    <true/>
    
    <!-- Network access for Git operations -->
    <key>com.apple.security.network.client</key>
    <true/>
    <key>com.apple.security.network.server</key>
    <false/>
    
    <!-- Shell/Terminal access for Git -->
    <key>com.apple.security.inherit</key>
    <true/>
</dict>
</plist>
EOF
    
    # Make executable
    chmod +x "${APP_BUNDLE}/Contents/MacOS/SourceGit"
    
    print_success "App bundle created"
}

# Sign the application
sign_app() {
    print_status "Signing application..."
    
    # Remove quarantine attributes first
    xattr -cr "${APP_BUNDLE}"
    
    if [[ -n "$SIGNING_IDENTITY" ]]; then
        # Production signing with Developer ID
        print_status "Signing with Developer ID: ${SIGNING_IDENTITY}"
        
        # Sign all frameworks and dylibs first (deep signing doesn't always work)
        find "${APP_BUNDLE}" -type f \( -name "*.dylib" -o -name "*.so" \) -exec \
            codesign --force --timestamp --options runtime \
            --sign "${SIGNING_IDENTITY}" \
            --entitlements entitlements.plist {} \;
        
        # Sign the main executable
        codesign --force --timestamp --options runtime \
            --sign "${SIGNING_IDENTITY}" \
            --entitlements entitlements.plist \
            "${APP_BUNDLE}/Contents/MacOS/SourceGit"
        
        # Sign the entire bundle
        codesign --force --deep --timestamp --options runtime \
            --sign "${SIGNING_IDENTITY}" \
            --entitlements entitlements.plist \
            "${APP_BUNDLE}"
        
        print_success "Signed with Developer ID"
        
        # Verify the signature
        print_status "Verifying signature..."
        codesign --verify --deep --strict --verbose=2 "${APP_BUNDLE}"
        print_success "Signature verified"
        
        # Check for notarization readiness
        print_status "Checking notarization readiness..."
        spctl -a -t exec -vvv "${APP_BUNDLE}" 2>&1 | head -20
        
    else
        # Ad-hoc signing for local use
        print_status "Using ad-hoc signing (local use only)"
        
        # Sign with ad-hoc identity
        codesign --force --deep --sign - \
            --entitlements entitlements.plist \
            "${APP_BUNDLE}"
        
        print_success "Ad-hoc signing completed"
    fi
    
    # Clean up entitlements file
    rm -f entitlements.plist
}

# Create ZIP for distribution
create_zip() {
    print_status "Creating ZIP archive for distribution..."
    
    ZIP_NAME="${APP_NAME}-${VERSION}-arm64.zip"
    
    # Remove old ZIP if exists
    rm -f "${ZIP_NAME}"
    
    # Create ZIP archive with proper compression
    # Using ditto for better macOS compatibility (preserves resource forks, extended attributes)
    ditto -c -k --sequesterRsrc --keepParent "${APP_BUNDLE}" "${ZIP_NAME}"
    
    # Alternatively, use zip command for cross-platform compatibility
    # zip -r -y "${ZIP_NAME}" "${APP_BUNDLE}"
    
    print_success "ZIP created: ${ZIP_NAME}"
    
    # Verify the ZIP file
    print_status "Verifying ZIP archive..."
    if unzip -t "${ZIP_NAME}" > /dev/null 2>&1; then
        print_success "ZIP archive verified successfully"
    else
        print_error "ZIP archive verification failed"
        exit 1
    fi
}

# Create DMG for distribution
create_dmg() {
    print_status "Creating DMG for distribution..."
    
    # Create a temporary directory for DMG contents
    DMG_TEMP="dmg_temp"
    rm -rf "$DMG_TEMP"
    mkdir -p "$DMG_TEMP"
    
    # Copy app to DMG temp directory
    cp -r "${APP_BUNDLE}" "$DMG_TEMP/"
    
    # Create symbolic link to Applications
    ln -s /Applications "$DMG_TEMP/Applications"
    
    # Create DMG
    hdiutil create -volname "${APP_NAME}" \
        -srcfolder "$DMG_TEMP" \
        -ov -format UDZO \
        "${DMG_NAME}"
    
    # Clean up
    rm -rf "$DMG_TEMP"
    
    if [[ -n "$SIGNING_IDENTITY" ]]; then
        # Sign the DMG
        codesign --force --sign "${SIGNING_IDENTITY}" "${DMG_NAME}"
        print_success "DMG signed"
    fi
    
    print_success "DMG created: ${DMG_NAME}"
}

# Notarization instructions
print_notarization_instructions() {
    if [[ -n "$SIGNING_IDENTITY" ]] && [[ -n "$NOTARIZATION_TEAM_ID" ]]; then
        print_status "Notarization Instructions"
        echo ""
        echo "To notarize the app for distribution:"
        echo "1. Submit for notarization:"
        echo "   xcrun notarytool submit \"${DMG_NAME}\" --team-id \"${NOTARIZATION_TEAM_ID}\" --wait"
        echo ""
        echo "2. After successful notarization, staple the ticket:"
        echo "   xcrun stapler staple \"${APP_BUNDLE}\""
        echo "   xcrun stapler staple \"${DMG_NAME}\""
        echo ""
        echo "Note: You'll need to set up App Store Connect API credentials first:"
        echo "   xcrun notarytool store-credentials"
    else
        print_warning "Notarization requires Developer ID certificate and Team ID"
        echo "Set SIGNING_IDENTITY and NOTARIZATION_TEAM_ID environment variables"
    fi
}

# Main build process
main() {
    echo "========================================="
    echo "   SourceGit macOS Build Script"
    echo "   Version: ${VERSION}"
    echo "========================================="
    echo ""
    
    check_prerequisites
    clean_build
    build_app
    create_bundle
    sign_app
    create_zip
    create_dmg
    
    echo ""
    echo "========================================="
    print_success "Build completed successfully!"
    echo ""
    echo "Output files:"
    echo "  - App Bundle: ${APP_BUNDLE}"
    echo "  - ZIP Archive: ${APP_NAME}-${VERSION}-arm64.zip"
    echo "  - DMG: ${DMG_NAME}"
    echo ""
    
    if [[ -z "$SIGNING_IDENTITY" ]]; then
        print_warning "App is ad-hoc signed (local use only)"
        echo "For distribution, set SIGNING_IDENTITY environment variable"
    else
        print_success "App is signed with Developer ID"
        print_notarization_instructions
    fi
    
    echo ""
    echo "To install locally:"
    echo "  cp -r \"${APP_BUNDLE}\" /Applications/"
    echo ""
    echo "========================================="
}

# Run the build
main "$@"