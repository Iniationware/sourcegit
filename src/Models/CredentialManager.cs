using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SourceGit.Models
{
    public static class CredentialManager
    {
        public enum CredentialHelperType
        {
            None,           // No helper (will prompt every time)
            Manager,        // Git Credential Manager (cross-platform)
            ManagerCore,    // Git Credential Manager Core (newer version)
            Store,          // Plain text storage (simple but insecure)
            Cache,          // In-memory cache with timeout
            Libsecret,      // Linux libsecret (GNOME Keyring)
            OSXKeychain,    // macOS Keychain
            WinCred,        // Windows Credential Store
            Custom          // User-specified helper
        }

        public class CredentialHelper
        {
            public CredentialHelperType Type { get; set; }
            public string Name { get; set; }
            public string Command { get; set; }
            public string Description { get; set; }
            public bool IsAvailable { get; set; }
            public bool RequiresConfiguration { get; set; }
        }

        private static readonly List<CredentialHelper> _helpers = new();
        private static CredentialHelper _currentHelper;

        static CredentialManager()
        {
            InitializeHelpers();
            DetectAvailableHelpers();
            SelectBestHelper();
        }

        public static List<CredentialHelper> AvailableHelpers => _helpers.FindAll(h => h.IsAvailable);

        public static CredentialHelper CurrentHelper
        {
            get => _currentHelper;
            set
            {
                _currentHelper = value;
                if (value != null)
                {
                    Native.OS.CredentialHelper = GetHelperCommand(value);
                }
            }
        }

        private static void InitializeHelpers()
        {
            _helpers.Clear();

            // None - no credential helper
            _helpers.Add(new CredentialHelper
            {
                Type = CredentialHelperType.None,
                Name = "None",
                Command = "",
                Description = "No credential storage (will prompt for credentials each time)",
                IsAvailable = true,
                RequiresConfiguration = false
            });

            // Git Credential Manager (cross-platform)
            _helpers.Add(new CredentialHelper
            {
                Type = CredentialHelperType.Manager,
                Name = "Git Credential Manager",
                Command = "manager",
                Description = "Cross-platform credential manager with multi-factor authentication support",
                IsAvailable = false,
                RequiresConfiguration = false
            });

            // Git Credential Manager Core (newer version)
            _helpers.Add(new CredentialHelper
            {
                Type = CredentialHelperType.ManagerCore,
                Name = "Git Credential Manager Core",
                Command = "manager-core",
                Description = "Modern cross-platform credential manager (recommended)",
                IsAvailable = false,
                RequiresConfiguration = false
            });

            // Store - plain text (simple but insecure)
            _helpers.Add(new CredentialHelper
            {
                Type = CredentialHelperType.Store,
                Name = "Store (Plain Text)",
                Command = "store",
                Description = "Simple file-based storage (WARNING: stores passwords in plain text)",
                IsAvailable = true, // Always available but not recommended
                RequiresConfiguration = true
            });

            // Cache - in-memory with timeout
            _helpers.Add(new CredentialHelper
            {
                Type = CredentialHelperType.Cache,
                Name = "Cache (Memory)",
                Command = "cache",
                Description = "Temporary in-memory storage (expires after 15 minutes by default)",
                IsAvailable = true, // Always available
                RequiresConfiguration = false
            });

            if (OperatingSystem.IsLinux())
            {
                // Libsecret for Linux
                _helpers.Add(new CredentialHelper
                {
                    Type = CredentialHelperType.Libsecret,
                    Name = "Libsecret (GNOME Keyring)",
                    Command = "libsecret",
                    Description = "Secure storage using GNOME Keyring or KWallet",
                    IsAvailable = false,
                    RequiresConfiguration = false
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                // OSX Keychain
                _helpers.Add(new CredentialHelper
                {
                    Type = CredentialHelperType.OSXKeychain,
                    Name = "macOS Keychain",
                    Command = "osxkeychain",
                    Description = "Secure storage using macOS Keychain",
                    IsAvailable = false,
                    RequiresConfiguration = false
                });
            }
            else if (OperatingSystem.IsWindows())
            {
                // Windows Credential Store
                _helpers.Add(new CredentialHelper
                {
                    Type = CredentialHelperType.WinCred,
                    Name = "Windows Credential Store",
                    Command = "wincred",
                    Description = "Secure storage using Windows Credential Manager",
                    IsAvailable = false,
                    RequiresConfiguration = false
                });
            }

            // Custom helper
            _helpers.Add(new CredentialHelper
            {
                Type = CredentialHelperType.Custom,
                Name = "Custom",
                Command = "",
                Description = "User-specified credential helper command",
                IsAvailable = true,
                RequiresConfiguration = true
            });
        }

        private static void DetectAvailableHelpers()
        {
            var gitExec = Native.OS.GitExecutable;
            if (string.IsNullOrEmpty(gitExec))
                return;

            // Check for Git Credential Manager
            CheckHelperAvailability("credential-manager", CredentialHelperType.Manager);
            CheckHelperAvailability("credential-manager-core", CredentialHelperType.ManagerCore);

            if (OperatingSystem.IsLinux())
            {
                // Check for libsecret
                CheckHelperAvailability("git-credential-libsecret", CredentialHelperType.Libsecret);
                
                // Also check if libsecret is installed via package manager
                if (!GetHelper(CredentialHelperType.Libsecret).IsAvailable)
                {
                    var libsecretPath = "/usr/libexec/git-core/git-credential-libsecret";
                    if (File.Exists(libsecretPath))
                    {
                        var helper = GetHelper(CredentialHelperType.Libsecret);
                        helper.IsAvailable = true;
                        helper.Command = libsecretPath;
                    }
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                // macOS Keychain is usually built into Git
                CheckGitCredentialHelper("osxkeychain", CredentialHelperType.OSXKeychain);
            }
            else if (OperatingSystem.IsWindows())
            {
                // Windows Credential Store is usually built into Git for Windows
                CheckGitCredentialHelper("wincred", CredentialHelperType.WinCred);
                CheckHelperAvailability("git-credential-manager.exe", CredentialHelperType.Manager);
            }
        }

        private static void CheckHelperAvailability(string executable, CredentialHelperType type)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = "--version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    process.WaitForExit(1000);
                    if (process.ExitCode == 0 || process.ExitCode == 1)
                    {
                        GetHelper(type).IsAvailable = true;
                    }
                }
            }
            catch
            {
                // Helper not available
            }
        }

        private static void CheckGitCredentialHelper(string helperName, CredentialHelperType type)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Native.OS.GitExecutable,
                    Arguments = $"credential-{helperName} --help",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    process.WaitForExit(1000);
                    // Git credential helpers often return non-zero on --help, check if we got output
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    if (!string.IsNullOrEmpty(output) || error.Contains("usage", StringComparison.OrdinalIgnoreCase))
                    {
                        GetHelper(type).IsAvailable = true;
                    }
                }
            }
            catch
            {
                // Helper not available
            }
        }

        private static void SelectBestHelper()
        {
            // Priority order for automatic selection
            var priorityOrder = new[]
            {
                CredentialHelperType.ManagerCore,
                CredentialHelperType.Manager,
                CredentialHelperType.Libsecret,
                CredentialHelperType.OSXKeychain,
                CredentialHelperType.WinCred,
                CredentialHelperType.Cache,
                CredentialHelperType.Store,
                CredentialHelperType.None
            };

            foreach (var type in priorityOrder)
            {
                var helper = GetHelper(type);
                if (helper != null && helper.IsAvailable)
                {
                    _currentHelper = helper;
                    break;
                }
            }

            // Fallback to cache if nothing else is available
            if (_currentHelper == null)
            {
                _currentHelper = GetHelper(CredentialHelperType.Cache);
            }
        }

        private static CredentialHelper GetHelper(CredentialHelperType type)
        {
            return _helpers.Find(h => h.Type == type);
        }

        public static string GetHelperCommand(CredentialHelper helper)
        {
            if (helper == null || helper.Type == CredentialHelperType.None)
                return "";

            return helper.Command;
        }

        public static void ConfigureStoreHelper(string path = null)
        {
            var storePath = path ?? Path.Combine(Native.OS.DataDir, ".git-credentials");
            var helper = GetHelper(CredentialHelperType.Store);
            if (helper != null)
            {
                helper.Command = $"store --file={storePath}";
            }
        }

        public static void ConfigureCacheHelper(int timeoutSeconds = 900)
        {
            var helper = GetHelper(CredentialHelperType.Cache);
            if (helper != null)
            {
                // The cache helper doesn't take timeout as part of the command
                // It needs to be configured separately via git config credential.helper 'cache --timeout=X'
                // For now, we just use the basic 'cache' command
                helper.Command = "cache";
                
                // Note: To properly set timeout, you would need to run:
                // git config --global credential.helper 'cache --timeout=900'
                // But since we're passing this via -c flag in each command, 
                // we can't include the timeout parameter this way
            }
        }

        public static bool TestCredentialHelper(CredentialHelper helper)
        {
            if (helper == null || helper.Type == CredentialHelperType.None)
                return true;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Native.OS.GitExecutable,
                    Arguments = $"credential-{helper.Command.Split(' ')[0]} get",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    // Send test input
                    process.StandardInput.WriteLine("protocol=https");
                    process.StandardInput.WriteLine("host=github.com");
                    process.StandardInput.WriteLine();
                    process.StandardInput.Close();

                    process.WaitForExit(2000);
                    // We don't care about the result, just that it didn't crash
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}