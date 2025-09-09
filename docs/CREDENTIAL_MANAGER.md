# Git Credential Manager Documentation

## Overview

SourceGit now includes comprehensive Git credential management to solve the common problem on Linux (and other platforms) where users have to repeatedly enter their credentials for each Git operation.

## Features

### Automatic Detection
- **Auto-discovery**: Automatically detects available credential helpers on your system
- **Smart Selection**: Chooses the most secure available option by default
- **Cross-platform**: Works on Windows, macOS, and Linux with platform-specific optimizations

### Supported Credential Helpers

#### 1. **None**
- No credential storage
- Will prompt for credentials each time
- Most secure but least convenient

#### 2. **Git Credential Manager (GCM)**
- Cross-platform credential manager from Microsoft
- Supports multi-factor authentication
- Works with GitHub, Azure DevOps, Bitbucket, and more
- **Recommended for most users**

#### 3. **Cache**
- Stores credentials in memory temporarily
- Default timeout: 1 hour
- Good balance of security and convenience
- No persistent storage on disk

#### 4. **Store**
- Saves credentials to a file on disk
- **Warning**: Stores passwords in plain text
- Only use on secure, single-user systems
- File location: `~/.sourcegit/data/.git-credentials`

#### 5. **Platform-Specific Options**

##### Linux: Libsecret
- Integrates with GNOME Keyring or KWallet
- Secure encrypted storage
- Automatically unlocked when you log in
- **Recommended for Linux desktop users**

##### macOS: Keychain
- Uses the native macOS Keychain
- Secure, encrypted storage
- Integrated with system security
- **Recommended for macOS users**

##### Windows: Windows Credential Store
- Uses Windows Credential Manager
- Secure, encrypted storage
- Integrated with Windows security
- **Recommended for Windows users**

#### 6. **Custom**
- Specify your own credential helper command
- For advanced users with specific requirements
- Supports any git-credential-* helper

## Configuration

### Via UI

1. Open **Preferences** (Ctrl/Cmd + ,)
2. Navigate to the **Git** tab
3. Find the **Credential Helper** dropdown
4. Select your preferred credential helper
5. For custom helpers, enter the command in the text field that appears

### Automatic Configuration

SourceGit automatically:
1. Detects available credential helpers on startup
2. Selects the most secure available option
3. Applies the configuration to all Git operations

### Priority Order

When multiple helpers are available, SourceGit selects based on this priority:
1. Git Credential Manager Core
2. Git Credential Manager
3. Libsecret (Linux)
4. macOS Keychain (macOS)
5. Windows Credential Store (Windows)
6. Cache (memory-based)
7. Store (file-based)
8. None

## Linux-Specific Setup

### Installing Credential Helpers

#### Ubuntu/Debian
```bash
# For Git Credential Manager
curl -sSL https://aka.ms/gcm/linux-install-source.sh | sh
git-credential-manager configure

# For Libsecret
sudo apt-get install libsecret-1-0 libsecret-1-dev
sudo apt-get install git-credential-libsecret
```

#### Fedora/RHEL
```bash
# For Git Credential Manager
curl -sSL https://aka.ms/gcm/linux-install-source.sh | sh
git-credential-manager configure

# For Libsecret
sudo dnf install libsecret libsecret-devel
sudo dnf install git-credential-libsecret
```

#### Arch Linux
```bash
# For Git Credential Manager
yay -S git-credential-manager-core-bin

# For Libsecret
sudo pacman -S libsecret
sudo pacman -S git-credential-libsecret
```

### Troubleshooting Linux Credential Issues

#### Problem: Credentials not persisting
**Solution**: 
1. Check if a keyring daemon is running:
   ```bash
   ps aux | grep -E 'gnome-keyring|kwallet'
   ```
2. If not, start it:
   ```bash
   # For GNOME
   gnome-keyring-daemon --start --daemonize
   
   # For KDE
   kwalletd5 &
   ```

#### Problem: "No credential helper is configured"
**Solution**:
1. Open SourceGit Preferences
2. Select a credential helper from the dropdown
3. Restart SourceGit

#### Problem: Libsecret not working
**Solution**:
1. Ensure libsecret is installed correctly
2. Check if the keyring is unlocked
3. Try using the Cache helper as a fallback

## Security Recommendations

### Best Practices

1. **Use platform-specific secure storage** when available:
   - Linux: Libsecret
   - macOS: Keychain
   - Windows: Credential Store

2. **Avoid the Store helper** unless:
   - You're on a secure, single-user system
   - The repository contains only public data
   - You understand the security implications

3. **Use Cache for temporary access**:
   - Good for shared systems
   - Credentials expire after timeout
   - No persistent storage

4. **Configure timeout for Cache**:
   - Default: 3600 seconds (1 hour)
   - Adjust based on your security needs

### Personal Access Tokens

For enhanced security with services like GitHub:
1. Use Personal Access Tokens instead of passwords
2. Generate tokens with minimal required permissions
3. Set expiration dates on tokens
4. Store tokens securely using credential helpers

## Implementation Details

### How It Works

1. **Detection Phase**: On startup, SourceGit checks for available credential helpers
2. **Selection Phase**: Chooses the best available helper based on security and platform
3. **Configuration Phase**: Sets the credential helper for all Git operations
4. **Runtime Phase**: Git uses the configured helper for all remote operations

### Technical Architecture

The credential manager system consists of:
- `Models/CredentialManager.cs`: Core credential manager logic
- `Views/Preferences.axaml`: UI for credential helper selection
- `Native/OS.cs`: Platform-specific credential helper integration
- `Commands/Command.cs`: Git command execution with credential helper

### Git Command Integration

All Git commands executed by SourceGit include:
```bash
git -c credential.helper=[selected-helper] [command]
```

This ensures consistent credential handling across all operations.

## Frequently Asked Questions

### Q: Why do I still get prompted for credentials?
**A**: This can happen if:
- No credential helper is configured
- The credential helper is not installed properly
- Your stored credentials have expired
- You're accessing a new repository/remote

### Q: Is it safe to use the Store helper?
**A**: The Store helper saves passwords in plain text. Only use it on:
- Secure, single-user systems
- Systems with encrypted disk storage
- For repositories without sensitive data

### Q: Can I use multiple credential helpers?
**A**: Git supports credential helper chaining, but SourceGit currently configures one helper at a time for simplicity and predictability.

### Q: How do I clear stored credentials?
**A**: Depends on the helper:
- **Cache**: Restart SourceGit or wait for timeout
- **Store**: Delete `~/.git-credentials` or the configured file
- **Libsecret/Keychain**: Use system tools to manage stored passwords
- **GCM**: Run `git credential-manager erase`

## Support

If you encounter issues with credential management:
1. Check the credential helper dropdown in Preferences
2. Verify the helper is installed correctly
3. Try a different credential helper
4. Report issues at: https://github.com/sourcegit-scm/sourcegit/issues