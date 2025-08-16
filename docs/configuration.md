# Configuration Guide

This guide covers all configuration options available in SourceGit, from basic settings to advanced customization.

## 📍 Configuration Locations

SourceGit stores configuration in platform-specific directories:

| Platform | Configuration Path |
|----------|-------------------|
| **Windows** | `%APPDATA%\SourceGit\` |
| **macOS** | `~/Library/Application Support/SourceGit/` |
| **Linux** | `~/.config/SourceGit/` or `~/.sourcegit/` |

### Configuration Files

- `preference.json` - User preferences and settings
- `external_editors.json` - External tool configuration
- `avatars/` - Cached user avatars
- `logs/` - Application logs and crash reports
- `PATH` - Custom PATH environment (macOS/Linux)

## 🎨 Appearance Settings

### Themes

#### Built-in Themes
- **Light Theme** - Clean, bright interface
- **Dark Theme** - Easy on the eyes for long coding sessions

#### Custom Themes
1. Download themes from [sourcegit-theme](https://github.com/sourcegit-scm/sourcegit-theme)
2. Place `.json` theme file in configuration directory
3. Select in **Preferences** → **Appearance** → **Theme**

#### Creating Custom Themes
```json
{
  "Name": "My Custom Theme",
  "ColorMode": "Dark",
  "Colors": {
    "Window.Background": "#1e1e1e",
    "Window.Foreground": "#cccccc",
    "Button.Background": "#2d2d30",
    "Button.Foreground": "#ffffff",
    // ... more color definitions
  }
}
```

### Font Settings
- **Default Font**: System default monospace
- **Font Size**: 10-24pt
- **Font Family**: Any installed monospace font

### Language
Supported languages:
- English
- 简体中文 (Simplified Chinese)
- 繁體中文 (Traditional Chinese)
- Deutsch (German)
- Español (Spanish)
- Français (French)
- Italiano (Italian)
- 日本語 (Japanese)
- Português (Portuguese)
- Русский (Russian)
- Українська (Ukrainian)
- தமிழ் (Tamil)

## ⚙️ General Settings

### Repository Management

#### Default Clone Directory
Set where new repositories are cloned by default:
```json
{
  "DefaultCloneDir": "/home/user/projects"
}
```

#### Auto-Fetch
Automatically fetch from remotes:
- **Interval**: 10, 30, 60 minutes, or disabled
- **On Focus**: Fetch when window gains focus

#### File Watcher
Monitor repository for external changes:
- **Enable/Disable**: Toggle file system watching
- **Refresh Interval**: 1-10 seconds

### Commit Settings

#### Commit Message
- **Template**: Default commit message format
- **Sign Commits**: Enable GPG signing
- **Auto-Stage**: Automatically stage modified files

#### Author Information
Override global Git config per repository:
```json
{
  "User.Name": "Your Name",
  "User.Email": "your.email@example.com"
}
```

## 🤖 AI Integration

### OpenAI Configuration

Configure AI-powered commit message generation:

```json
{
  "OpenAI": {
    "Server": "https://api.openai.com/v1",
    "ApiKey": "sk-...",
    "Model": "gpt-3.5-turbo",
    "Temperature": 0.7
  }
}
```

### Alternative AI Services

#### Ollama (Local)
```json
{
  "OpenAI": {
    "Server": "http://localhost:11434/v1",
    "Model": "llama2",
    "ApiKey": ""  // Optional for local services
  }
}
```

#### Azure OpenAI
```json
{
  "OpenAI": {
    "Server": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key",
    "Model": "gpt-35-turbo",
    "ApiVersion": "2023-05-15"
  }
}
```

## 🔧 Advanced Settings

### Git Configuration

#### Global Git Settings
Configure Git behavior within SourceGit:

```json
{
  "Git": {
    "Path": "/usr/local/bin/git",  // Custom Git path
    "DefaultBranch": "main",        // Default branch name
    "AutoCRLF": "input",           // Line ending handling
    "CredentialHelper": "manager"  // Credential storage
  }
}
```

#### SSH Configuration
Per-repository SSH key:
```json
{
  "Repositories": {
    "/path/to/repo": {
      "SSHKey": "~/.ssh/id_rsa_work"
    }
  }
}
```

### Performance Settings

#### Cache Configuration
```json
{
  "Performance": {
    "EnableCache": true,
    "CacheTimeout": 120,        // Seconds
    "MaxCacheSize": 100,        // MB
    "MaxProcesses": 8           // Concurrent Git processes
  }
}
```

#### Memory Management
```json
{
  "Memory": {
    "MaxCommitLoad": 10000,     // Maximum commits to load
    "EnableVirtualization": true // Virtual scrolling for large lists
  }
}
```

### Display Settings

#### Diff Viewer
```json
{
  "Diff": {
    "Mode": "SideBySide",       // or "Inline"
    "ShowWhitespace": true,
    "ContextLines": 3,
    "SyntaxHighlighting": true
  }
}
```

#### Commit Graph
```json
{
  "Graph": {
    "ShowRemotes": true,
    "ColorScheme": "default",   // or "colorblind"
    "MaxColumns": 10
  }
}
```

## 🔒 Security Settings

### Credential Storage

#### Windows
Uses Windows Credential Manager by default

#### macOS
```bash
git config --global credential.helper osxkeychain
```

#### Linux
```bash
# Using libsecret
git config --global credential.helper libsecret

# Using store (less secure)
git config --global credential.helper store
```

### GPG Signing

Enable commit signing:
```json
{
  "GPG": {
    "Enable": true,
    "Key": "your-gpg-key-id",
    "Program": "gpg"  // or "gpg2"
  }
}
```

## 🚀 Workflow Customization

### Custom Actions

Define custom Git workflows:
```json
{
  "CustomActions": [
    {
      "Name": "Deploy to Production",
      "Command": "git push production main",
      "Icon": "🚀"
    },
    {
      "Name": "Run Tests",
      "Command": "./scripts/test.sh",
      "WorkingDirectory": "${REPO_ROOT}"
    }
  ]
}
```

### Issue Tracker Integration

Link commits to issues:
```json
{
  "IssueTracker": {
    "Pattern": "#(\\d+)",
    "URL": "https://github.com/owner/repo/issues/$1"
  }
}
```

### Git-Flow Configuration

Customize Git-Flow branches:
```json
{
  "GitFlow": {
    "Master": "main",
    "Develop": "develop",
    "FeaturePrefix": "feature/",
    "ReleasePrefix": "release/",
    "HotfixPrefix": "hotfix/",
    "VersionTagPrefix": "v"
  }
}
```

## 📝 Preferences File Example

Complete `preference.json` example:

```json
{
  "Theme": "Dark",
  "Language": "en_US",
  "FontFamily": "Cascadia Code",
  "FontSize": 13,
  "DefaultCloneDir": "~/projects",
  "AutoFetch": true,
  "AutoFetchInterval": 600,
  "ShowHiddenFiles": false,
  "OpenAI": {
    "Server": "https://api.openai.com/v1",
    "ApiKey": "sk-...",
    "Model": "gpt-3.5-turbo"
  },
  "Git": {
    "DefaultBranch": "main",
    "AutoCRLF": "input"
  },
  "Diff": {
    "Mode": "SideBySide",
    "ShowWhitespace": true
  },
  "Performance": {
    "EnableCache": true,
    "MaxProcesses": 8
  },
  "Repositories": {
    "/home/user/project1": {
      "SSHKey": "~/.ssh/id_rsa_work",
      "User.Name": "Work Name",
      "User.Email": "work@company.com"
    }
  }
}
```

## 🔄 Import/Export Settings

### Export Settings
1. Navigate to **Preferences** → **Advanced**
2. Click **Export Settings**
3. Save the `.json` file

### Import Settings
1. Navigate to **Preferences** → **Advanced**
2. Click **Import Settings**
3. Select previously exported `.json` file

### Sync Settings
Store your `preference.json` in a cloud service or Git repository for synchronization across devices.

## 🛠️ Environment Variables

### Custom PATH (macOS/Linux)
```bash
echo $PATH > ~/Library/Application\ Support/SourceGit/PATH
```

### Display Scaling (Linux)
```bash
export AVALONIA_SCREEN_SCALE_FACTORS="1.5"
```

### Input Method (Linux)
```bash
export AVALONIA_IM_MODULE=none
```

## 💡 Tips

1. **Backup Settings**: Regularly backup your configuration directory
2. **Repository-Specific Config**: Override global settings per repository
3. **Performance Tuning**: Adjust cache and process limits for large repositories
4. **Custom Themes**: Share your themes with the community
5. **Keyboard Shortcuts**: Customize shortcuts in preferences