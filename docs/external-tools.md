# External Tools Integration

SourceGit seamlessly integrates with popular development tools and IDEs, allowing you to open repositories directly in your preferred editor.

## 📝 Supported Editors

### Cross-Platform Editors

| Editor | Windows | macOS | Linux | Auto-Detection |
|--------|---------|-------|-------|-----------------|
| **Visual Studio Code** | ✅ | ✅ | ✅ | Yes |
| **Visual Studio Code - Insiders** | ✅ | ✅ | ✅ | Yes |
| **VSCodium** | ✅ | ✅ | ✅ | Yes |
| **Cursor** | ✅ | ✅ | ✅ | Yes |
| **Fleet** | ✅ | ✅ | ✅ | Yes |
| **Sublime Text** | ✅ | ✅ | ✅ | Yes |
| **Zed** | ❌ | ✅ | ✅ | Yes |

### Platform-Specific Editors

| Editor | Platform | Auto-Detection |
|--------|----------|----------------|
| **Visual Studio** | Windows | Yes |
| **Xcode** | macOS | Yes |
| **TextEdit** | macOS | Yes |
| **gedit** | Linux | Yes |
| **Kate** | Linux | Yes |

### JetBrains IDEs

SourceGit supports all JetBrains IDEs when **JetBrains Toolbox** is installed:

- IntelliJ IDEA
- WebStorm
- PyCharm
- Rider
- GoLand
- PhpStorm
- RubyMine
- CLion
- DataGrip
- Android Studio

## 🔧 Configuration

### Automatic Detection

SourceGit automatically detects installed editors by checking common installation paths:

**Windows:**
- `C:\Program Files\`
- `C:\Program Files (x86)\`
- `%LOCALAPPDATA%\Programs\`
- Registry entries

**macOS:**
- `/Applications/`
- `/usr/local/bin/`
- `~/Applications/`

**Linux:**
- `/usr/bin/`
- `/usr/local/bin/`
- `/snap/bin/`
- `/var/lib/flatpak/app/`

### Manual Configuration

If your editor is not detected (e.g., portable versions), you can manually configure it.

#### Step 1: Locate Configuration Directory

| Platform | Path |
|----------|------|
| Windows | `%APPDATA%\SourceGit\` |
| macOS | `~/Library/Application Support/SourceGit/` |
| Linux | `~/.config/SourceGit/` |

#### Step 2: Create Configuration File

Create a file named `external_editors.json` in the configuration directory:

```json
{
  "tools": {
    "Visual Studio Code": "/custom/path/to/Code.exe",
    "Sublime Text": "/custom/path/to/sublime_text.exe",
    "Custom Editor": "/path/to/your/editor.exe"
  }
}
```

### Configuration Examples

#### Windows (Portable VS Code)
```json
{
  "tools": {
    "Visual Studio Code": "D:\\PortableApps\\VSCode\\Code.exe"
  }
}
```

#### macOS (Custom Installation)
```json
{
  "tools": {
    "Visual Studio Code": "/Users/username/Applications/Visual Studio Code.app/Contents/MacOS/Electron",
    "Sublime Text": "/Applications/Sublime Text.app/Contents/MacOS/sublime_text"
  }
}
```

#### Linux (Snap/Flatpak)
```json
{
  "tools": {
    "Visual Studio Code": "/snap/code/current/bin/code",
    "Sublime Text": "/var/lib/flatpak/app/com.sublimetext.three/current/active/files/bin/subl"
  }
}
```

## 🚀 Using External Tools

### Opening Repository in Editor

1. **From Repository View:**
   - Click the **"Open in External Tool"** button in the toolbar
   - Select your preferred editor from the dropdown

2. **From Context Menu:**
   - Right-click on a repository
   - Select **"Open in..."** → Choose editor

3. **Keyboard Shortcut:**
   - Press `Ctrl+Shift+E` (Windows/Linux) or `Cmd+Shift+E` (macOS)
   - Opens in the last used editor

### Opening Specific Files

1. Right-click on any file in the working copy
2. Select **"Open with..."** → Choose editor
3. The file opens at the specific location

## 🎯 Advanced Features

### Command Line Arguments

Some editors support additional arguments for enhanced integration:

```json
{
  "tools": {
    "Visual Studio Code": {
      "path": "/usr/local/bin/code",
      "args": "--new-window --goto ${file}:${line}"
    }
  }
}
```

**Supported Variables:**
- `${file}` - Full path to file
- `${line}` - Line number
- `${column}` - Column number
- `${repo}` - Repository path

### Terminal Integration

Configure terminal emulators for command-line access:

```json
{
  "terminals": {
    "Windows Terminal": "wt.exe -d \"${repo}\"",
    "iTerm2": "open -a iTerm \"${repo}\"",
    "GNOME Terminal": "gnome-terminal --working-directory=\"${repo}\""
  }
}
```

## 💡 Tips and Tricks

### VS Code Workspace
Create a `.code-workspace` file in your repository for better VS Code integration:

```json
{
  "folders": [
    {
      "path": "."
    }
  ],
  "settings": {
    "git.enabled": true,
    "git.autoFetch": true
  }
}
```

### JetBrains Project Detection
Place `.idea` directory in your repository root for automatic project detection in JetBrains IDEs.

### Multiple Editor Profiles
Create different profiles for different project types:

```json
{
  "profiles": {
    "web": "Visual Studio Code",
    "mobile": "Android Studio",
    "backend": "IntelliJ IDEA"
  }
}
```

## 🔍 Troubleshooting

### Editor Not Detected

1. **Check Installation Path:**
   - Ensure editor is installed in a standard location
   - Or configure manually in `external_editors.json`

2. **Verify Permissions:**
   - SourceGit needs read permissions for editor directories
   - On macOS, grant access in System Preferences → Security & Privacy

3. **Update PATH:**
   - Ensure editor executable is in system PATH
   - Or use full absolute path in configuration

### Editor Opens But No File

- Check if editor supports command-line file arguments
- Verify the args configuration is correct
- Some editors require specific flags to open files

### JetBrains IDEs Not Listed

1. Install [JetBrains Toolbox](https://www.jetbrains.com/toolbox-app/)
2. Restart SourceGit
3. IDEs managed by Toolbox will appear automatically

## 📚 Editor-Specific Notes

### Visual Studio Code
- Supports opening specific lines with `:line` syntax
- Can open multiple files simultaneously
- Workspace files (`.code-workspace`) are recognized

### Sublime Text
- Supports project files (`.sublime-project`)
- Can open with specific syntax highlighting

### JetBrains IDEs
- Best detection with JetBrains Toolbox
- Supports opening specific files and lines
- Project files (`.idea`) enable full IDE features

### Vim/Neovim
Configure terminal-based editors:
```json
{
  "tools": {
    "Neovim": "kitty -e nvim \"${file}\""
  }
}
```