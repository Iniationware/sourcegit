# macOS Code Signing and Notarization Setup

This guide explains how to set up code signing and notarization for macOS releases.

## Prerequisites

1. **Apple Developer Account** ($99/year)
   - Sign up at https://developer.apple.com
   - Enroll in the Apple Developer Program

2. **Developer ID Certificate**
   - In Xcode: Preferences → Accounts → Manage Certificates
   - Create a "Developer ID Application" certificate
   - Export as .p12 file with password

## GitHub Secrets Setup

Add these secrets to your GitHub repository (Settings → Secrets and variables → Actions):

### Required Secrets

1. **MACOS_CERTIFICATE**
   ```bash
   # Convert your .p12 certificate to base64
   base64 -i DeveloperID_Application.p12 | pbcopy
   ```
   Paste the base64 string as the secret value

2. **MACOS_CERTIFICATE_PWD**
   - The password you used when exporting the .p12 certificate

3. **APPLE_ID** (for notarization)
   - Your Apple ID email address

4. **NOTARIZE_PASSWORD** (for notarization)
   - Generate an app-specific password:
   - Go to https://appleid.apple.com/account/manage
   - Sign in → Security → App-Specific Passwords
   - Generate a password for "SourceGit Notarization"

5. **TEAM_ID** (for notarization)
   - Find your Team ID in Apple Developer account
   - Or run: `xcrun altool --list-providers -u "your@email.com" -p "app-specific-password"`

## Testing Locally

Test the signing process locally:

```bash
# Set environment variables
export VERSION="2025.34.10"
export RUNTIME="osx-arm64"
export MACOS_CERTIFICATE="base64_encoded_cert"
export MACOS_CERTIFICATE_PWD="your_password"
export APPLE_ID="your@email.com"
export NOTARIZE_PASSWORD="app-specific-password"
export TEAM_ID="YOURTEAMID"

# Run the DMG script
cd build
./scripts/package.osx-dmg.sh
```

## Verification

After downloading the DMG:

```bash
# Check signature
codesign -dv --verbose=4 /path/to/SourceGit.app

# Check notarization
spctl -a -t open --context context:primary-signature -v /path/to/sourcegit.dmg

# Verify Gatekeeper acceptance
spctl -a -vvv /path/to/SourceGit.app
```

## Workflow Behavior

The workflow creates both signed DMG and unsigned ZIP:

- **With secrets**: Creates signed and notarized DMG + unsigned ZIP
- **Without secrets**: Creates unsigned DMG + unsigned ZIP

Both are uploaded as release assets, giving users options.

## Troubleshooting

### "Developer ID Application" not found
- Ensure certificate is properly imported
- Check keychain access permissions

### Notarization fails
- Verify app-specific password is correct
- Check Team ID matches your developer account
- Ensure all entitlements are correct

### DMG won't open
- Check if Gatekeeper is enabled: `spctl --status`
- Try right-click → Open for first launch

## Cost Considerations

- Apple Developer Program: $99/year
- No per-notarization costs
- Unlimited app notarizations included

## Alternative: Ad-hoc Signing (Free)

For testing without Apple Developer account:
```bash
# Ad-hoc sign (no notarization possible)
codesign --deep --force -s - SourceGit.app
```

Note: Ad-hoc signed apps will still show warnings but can be opened with right-click → Open.