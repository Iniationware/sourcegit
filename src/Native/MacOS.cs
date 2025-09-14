using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace SourceGit.Native
{
    [SupportedOSPlatform("macOS")]
    internal class MacOS : OS.IBackend
    {
        /// <summary>
        /// Detects if running on Apple Silicon (ARM64) Mac
        /// </summary>
        public static bool IsAppleSilicon() =>
            RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

        /// <summary>
        /// Gets the current processor architecture
        /// </summary>
        public static Architecture ProcessorArchitecture =>
            RuntimeInformation.ProcessArchitecture;
        public void SetupApp(AppBuilder builder)
        {
            builder.With(new MacOSPlatformOptions()
            {
                DisableDefaultApplicationMenuItems = true,
            });

            // Fix `PATH` env on macOS with architecture-aware paths
            var path = Environment.GetEnvironmentVariable("PATH");
            if (IsAppleSilicon())
            {
                // On Apple Silicon, prioritize /opt/homebrew (ARM64 native paths)
                if (string.IsNullOrEmpty(path))
                    path = "/opt/homebrew/bin:/opt/homebrew/sbin:/usr/local/bin:/usr/bin:/bin:/usr/sbin:/sbin";
                else if (!path.Contains("/opt/homebrew/", StringComparison.Ordinal))
                    path = "/opt/homebrew/bin:/opt/homebrew/sbin:" + path;
            }
            else
            {
                // On Intel Macs, prioritize /usr/local (x86_64 paths)
                if (string.IsNullOrEmpty(path))
                    path = "/usr/local/bin:/usr/bin:/bin:/usr/sbin:/sbin:/opt/homebrew/bin:/opt/homebrew/sbin";
                else if (!path.Contains("/usr/local/bin", StringComparison.Ordinal))
                    path = "/usr/local/bin:" + path;
            }

            var customPathFile = Path.Combine(OS.DataDir, "PATH");
            if (File.Exists(customPathFile))
            {
                var env = File.ReadAllText(customPathFile).Trim();
                if (!string.IsNullOrEmpty(env))
                    path = env;
            }

            Environment.SetEnvironmentVariable("PATH", path);
        }

        public void SetupWindow(Window window)
        {
            window.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.SystemChrome;
            window.ExtendClientAreaToDecorationsHint = true;
        }

        public string FindGitExecutable()
        {
            // Optimize path search order based on architecture
            var gitPathVariants = IsAppleSilicon()
                ? new List<string>() {
                    // Apple Silicon: Prioritize ARM64 native paths
                    "/opt/homebrew/bin/git",           // Homebrew ARM64
                    "/opt/homebrew/opt/git/bin/git",   // Homebrew git formula
                    "/usr/bin/git",                    // System git (universal binary)
                    "/usr/local/bin/git"               // Legacy Intel location
                }
                : new List<string>() {
                    // Intel Mac: Prioritize x86_64 paths
                    "/usr/local/bin/git",              // Homebrew Intel
                    "/usr/bin/git",                    // System git
                    "/opt/homebrew/bin/git",           // ARM64 Homebrew (Rosetta 2)
                    "/opt/homebrew/opt/git/bin/git"   // ARM64 git formula (Rosetta 2)
                };

            // Check each path and return the first valid one
            foreach (var path in gitPathVariants)
            {
                if (File.Exists(path))
                {
                    // Verify it's executable and the correct architecture if possible
                    try
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = path,
                            Arguments = "--version",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };

                        using var process = Process.Start(startInfo);
                        if (process != null)
                        {
                            process.WaitForExit(1000); // 1 second timeout
                            if (process.ExitCode == 0)
                                return path;
                        }
                    }
                    catch
                    {
                        // If we can't verify, still return it as a fallback
                        return path;
                    }
                }
            }

            return string.Empty;
        }

        public string FindTerminal(Models.ShellOrTerminal shell)
        {
            return shell.Type switch
            {
                "mac-terminal" => "Terminal",
                "iterm2" => "iTerm",
                "warp" => "Warp",
                "ghostty" => "Ghostty",
                "kitty" => "kitty",
                _ => string.Empty,
            };
        }

        public List<Models.ExternalTool> FindExternalTools()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var finder = new Models.ExternalToolsFinder();
            finder.VSCode(() => "/Applications/Visual Studio Code.app/Contents/Resources/app/bin/code");
            finder.VSCodeInsiders(() => "/Applications/Visual Studio Code - Insiders.app/Contents/Resources/app/bin/code");
            finder.VSCodium(() => "/Applications/VSCodium.app/Contents/Resources/app/bin/codium");
            finder.Cursor(() => "/Applications/Cursor.app/Contents/Resources/app/bin/cursor");
            finder.Fleet(() => Path.Combine(home, "Applications/Fleet.app/Contents/MacOS/Fleet"));
            finder.FindJetBrainsFromToolbox(() => Path.Combine(home, "Library/Application Support/JetBrains/Toolbox"));
            finder.SublimeText(() => "/Applications/Sublime Text.app/Contents/SharedSupport/bin/subl");
            finder.Zed(() => File.Exists("/usr/local/bin/zed") ? "/usr/local/bin/zed" : "/Applications/Zed.app/Contents/MacOS/cli");
            return finder.Tools;
        }

        public void OpenBrowser(string url)
        {
            Process.Start("open", url);
        }

        public void OpenInFileManager(string path, bool select)
        {
            if (Directory.Exists(path))
                Process.Start("open", path.Quoted());
            else if (File.Exists(path))
                Process.Start("open", $"{path.Quoted()} -R");
        }

        public void OpenTerminal(string workdir)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var dir = string.IsNullOrEmpty(workdir) ? home : workdir;
            Process.Start("open", $"-a {OS.ShellOrTerminal} {dir.Quoted()}");
        }

        public void OpenWithDefaultEditor(string file)
        {
            Process.Start("open", file.Quoted());
        }
    }
}
