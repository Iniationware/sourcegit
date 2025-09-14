#!/bin/bash
set -e

# DMG creation and signing script for macOS
# Requires: VERSION and RUNTIME environment variables
# Optional: MACOS_CERTIFICATE, MACOS_CERTIFICATE_PWD, APPLE_ID, NOTARIZE_PASSWORD, TEAM_ID

cd build

# Prepare the app bundle
echo "Preparing SourceGit.app..."
rm -rf SourceGit.app
mkdir -p SourceGit.app/Contents/{MacOS,Resources}
cp -r SourceGit/* SourceGit.app/Contents/MacOS/
mv SourceGit.app/Contents/MacOS/SourceGit SourceGit.app/Contents/MacOS/SourceGit
cp resources/app/App.icns SourceGit.app/Contents/Resources/App.icns
sed "s/SOURCE_GIT_VERSION/$VERSION/g" resources/app/App.plist > SourceGit.app/Contents/Info.plist
rm -rf SourceGit.app/Contents/MacOS/SourceGit.dsym

# Code signing (if certificate is available)
if [ -n "$MACOS_CERTIFICATE" ] && [ -n "$MACOS_CERTIFICATE_PWD" ]; then
    echo "Setting up code signing..."

    # Create temporary keychain
    KEYCHAIN_PATH=$RUNNER_TEMP/app-signing.keychain-db
    KEYCHAIN_PASSWORD=$(openssl rand -base64 32)

    # Import certificate
    echo "$MACOS_CERTIFICATE" | base64 --decode > certificate.p12

    # Create and configure keychain
    security create-keychain -p "$KEYCHAIN_PASSWORD" "$KEYCHAIN_PATH"
    security set-keychain-settings -lut 21600 "$KEYCHAIN_PATH"
    security unlock-keychain -p "$KEYCHAIN_PASSWORD" "$KEYCHAIN_PATH"

    # Import certificate to keychain
    security import certificate.p12 -P "$MACOS_CERTIFICATE_PWD" -A -t cert -f pkcs12 -k "$KEYCHAIN_PATH"
    security list-keychain -d user -s "$KEYCHAIN_PATH"

    # Sign the app
    echo "Signing SourceGit.app..."
    codesign --deep --force --verify --verbose \
        --sign "Developer ID Application" \
        --options runtime \
        --entitlements resources/app/entitlements.plist \
        --timestamp \
        SourceGit.app

    echo "Verifying signature..."
    codesign --verify --verbose SourceGit.app

    # Clean up certificate
    rm certificate.p12
else
    echo "No signing certificate found, creating unsigned DMG..."
fi

# Create DMG
echo "Creating DMG..."
DMG_NAME="sourcegit_${VERSION}.${RUNTIME}.dmg"
VOLUME_NAME="SourceGit ${VERSION}"

# Create a temporary directory for DMG contents
rm -rf dmg_temp
mkdir dmg_temp
cp -R SourceGit.app dmg_temp/

# Create Applications symlink
ln -s /Applications dmg_temp/Applications

# Create DMG using hdiutil (more reliable than create-dmg in CI)
hdiutil create -volname "$VOLUME_NAME" \
    -srcfolder dmg_temp \
    -ov -format UDZO \
    "$DMG_NAME"

# Sign the DMG itself (if certificate is available)
if [ -n "$MACOS_CERTIFICATE" ]; then
    echo "Signing DMG..."
    codesign --sign "Developer ID Application" \
        --timestamp \
        "$DMG_NAME"
fi

# Notarization (if credentials are available)
if [ -n "$APPLE_ID" ] && [ -n "$NOTARIZE_PASSWORD" ] && [ -n "$TEAM_ID" ]; then
    echo "Submitting for notarization..."

    # Submit for notarization
    xcrun notarytool submit "$DMG_NAME" \
        --apple-id "$APPLE_ID" \
        --password "$NOTARIZE_PASSWORD" \
        --team-id "$TEAM_ID" \
        --wait

    # Staple the notarization ticket
    echo "Stapling notarization..."
    xcrun stapler staple "$DMG_NAME"

    echo "Verifying notarization..."
    spctl -a -t open --context context:primary-signature -v "$DMG_NAME"
fi

# Clean up
rm -rf dmg_temp SourceGit.app

echo "DMG created: $DMG_NAME"
ls -lh "$DMG_NAME"