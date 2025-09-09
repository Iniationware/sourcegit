using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SourceGit.Commands
{
    public partial class Command
    {
        public class Result
        {
            public bool IsSuccess { get; set; } = false;
            public string StdOut { get; set; } = string.Empty;
            public string StdErr { get; set; } = string.Empty;

            public static Result Failed(string reason) => new Result() { StdErr = reason };
        }

        public enum EditorType
        {
            None,
            CoreEditor,
            RebaseEditor,
        }

        public string Context { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = null;
        public EditorType Editor { get; set; } = EditorType.CoreEditor;
        public string SSHKey { get; set; } = string.Empty;
        public string Args { get; set; } = string.Empty;
        public bool SkipCredentials { get; set; } = false;

        // Only used in `ExecAsync` mode.
        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
        public bool RaiseError { get; set; } = true;
        public Models.ICommandLog Log { get; set; } = null;

        public async Task<bool> ExecAsync()
        {
            Log?.AppendLine($"$ git {Args}\n");

            var errs = new List<string>();

            using var proc = new Process();
            proc.StartInfo = CreateGitStartInfo(true);
            proc.OutputDataReceived += (_, e) => HandleOutput(e.Data, errs);
            proc.ErrorDataReceived += (_, e) => HandleOutput(e.Data, errs);

            Process dummy = null;
            var dummyProcLock = new object();
            try
            {
                proc.Start();

                // Not safe, please only use `CancellationToken` in readonly commands.
                if (CancellationToken.CanBeCanceled)
                {
                    dummy = proc;
                    CancellationToken.Register(() =>
                    {
                        lock (dummyProcLock)
                        {
                            if (dummy is { HasExited: false })
                                dummy.Kill();
                        }
                    });
                }
            }
            catch (Exception e)
            {
                if (RaiseError)
                    App.RaiseException(Context, e.Message);

                Log?.AppendLine(string.Empty);
                return false;
            }

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            try
            {
                await proc.WaitForExitAsync(CancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                HandleOutput(e.Message, errs);
            }

            if (dummy != null)
            {
                lock (dummyProcLock)
                {
                    dummy = null;
                }
            }

            Log?.AppendLine(string.Empty);

            if (!CancellationToken.IsCancellationRequested && proc.ExitCode != 0)
            {
                if (RaiseError)
                {
                    var errMsg = string.Join("\n", errs).Trim();
                    if (!string.IsNullOrEmpty(errMsg))
                        App.RaiseException(Context, errMsg);
                }

                return false;
            }

            return true;
        }

        protected Result ReadToEnd()
        {
            using var proc = new Process() { StartInfo = CreateGitStartInfo(true) };

            try
            {
                proc.Start();
            }
            catch (Exception e)
            {
                return Result.Failed(e.Message);
            }

            var rs = new Result() { IsSuccess = true };
            rs.StdOut = proc.StandardOutput.ReadToEnd();
            rs.StdErr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            rs.IsSuccess = proc.ExitCode == 0;
            return rs;
        }

        protected async Task<Result> ReadToEndAsync()
        {
            using var proc = new Process() { StartInfo = CreateGitStartInfo(true) };

            try
            {
                proc.Start();
            }
            catch (Exception e)
            {
                return Result.Failed(e.Message);
            }

            var rs = new Result() { IsSuccess = true };
            rs.StdOut = await proc.StandardOutput.ReadToEndAsync(CancellationToken).ConfigureAwait(false);
            rs.StdErr = await proc.StandardError.ReadToEndAsync(CancellationToken).ConfigureAwait(false);
            await proc.WaitForExitAsync(CancellationToken).ConfigureAwait(false);

            rs.IsSuccess = proc.ExitCode == 0;
            return rs;
        }

        protected ProcessStartInfo CreateGitStartInfo(bool redirect)
        {
            var start = new ProcessStartInfo();
            start.FileName = Native.OS.GitExecutable;
            start.UseShellExecute = false;
            start.CreateNoWindow = true;

            if (redirect)
            {
                start.RedirectStandardOutput = true;
                start.RedirectStandardError = true;
                start.StandardOutputEncoding = Encoding.UTF8;
                start.StandardErrorEncoding = Encoding.UTF8;
            }

            // Force using this app as SSH askpass program
            var selfExecFile = Process.GetCurrentProcess().MainModule!.FileName;
            start.Environment.Add("SSH_ASKPASS", selfExecFile); // Can not use parameter here, because it invoked by SSH with `exec`
            start.Environment.Add("SSH_ASKPASS_REQUIRE", "prefer");
            start.Environment.Add("SOURCEGIT_LAUNCH_AS_ASKPASS", "TRUE");
            if (!OperatingSystem.IsLinux())
                start.Environment.Add("DISPLAY", "required");
            
            // For public repositories, disable credential prompts via environment
            if (SkipCredentials || (Args != null && (Args.Contains("github.com") || Args.Contains("gitlab.com"))))
            {
                // These environment variables tell Git not to prompt for credentials
                start.Environment["GIT_TERMINAL_PROMPT"] = "0";
                start.Environment["GIT_ASKPASS"] = "echo";
                start.Environment["GCM_INTERACTIVE"] = "never";
                start.Environment["GIT_CREDENTIAL_HELPER"] = "";
            }

            // If an SSH private key was provided, sets the environment.
            if (!start.Environment.ContainsKey("GIT_SSH_COMMAND") && !string.IsNullOrEmpty(SSHKey))
                start.Environment.Add("GIT_SSH_COMMAND", $"ssh -i '{SSHKey}'");

            // Force using en_US.UTF-8 locale
            if (OperatingSystem.IsLinux())
            {
                start.Environment.Add("LANG", "C");
                start.Environment.Add("LC_ALL", "C");
            }

            var builder = new StringBuilder();
            builder.Append("--no-pager -c core.quotepath=off");
            
            // Check if we're working with a public repository (no credentials needed)
            var isPublicRepo = false;
            var needsCredentials = true;
            
            // Check for operations that typically don't need credentials on public repos
            if (Args != null)
            {
                // Check if this is a read-only operation on GitHub/GitLab
                var isReadOperation = Args.Contains("fetch") || Args.Contains("pull") || 
                                     Args.Contains("ls-remote") || Args.Contains("clone") ||
                                     Args.Contains("remote -v") || Args.Contains("remote get-url");
                
                // Check if URL contains public Git hosts
                var hasPublicHost = Args.Contains("github.com") || Args.Contains("gitlab.com") || 
                                   Args.Contains("bitbucket.org") || Args.Contains("gitee.com");
                
                // If it's a read operation on a public host, disable credentials
                if (isReadOperation && hasPublicHost)
                {
                    isPublicRepo = true;
                    needsCredentials = false;
                }
                
                // Also check for HTTPS URLs which are typically public
                if (Args.Contains("https://github.com") || Args.Contains("https://gitlab.com"))
                {
                    isPublicRepo = true;
                    needsCredentials = false;
                }
            }
            
            // ALWAYS explicitly set credential helper to avoid system defaults
            if (SkipCredentials || !needsCredentials || isPublicRepo)
            {
                // Explicitly disable ALL credential helpers including system ones
                // Use multiple methods to ensure no credentials are requested
                builder.Append(" -c credential.helper=");
                builder.Append(" -c credential.helper=''");
                builder.Append(" -c credential.helper=!");
                builder.Append(" -c core.askpass=");
                builder.Append(" -c core.askPass=''");
            }
            else if (!string.IsNullOrEmpty(Native.OS.CredentialHelper))
            {
                // For the cache helper with timeout, we need special handling
                if (Native.OS.CredentialHelper == "cache")
                {
                    // Set cache with default timeout of 900 seconds (15 minutes)
                    builder.Append(" -c credential.helper=cache -c credentialcache.ignoreSIGHUP=true");
                }
                else if (Native.OS.CredentialHelper.StartsWith("store"))
                {
                    // Store helper might have --file parameter
                    builder.Append($" -c credential.helper=\"{Native.OS.CredentialHelper}\"");
                }
                else
                {
                    // Other helpers just use the name
                    builder.Append($" -c credential.helper={Native.OS.CredentialHelper}");
                }
            }
            
            builder.Append(' ');

            switch (Editor)
            {
                case EditorType.CoreEditor:
                    builder.Append($"""-c core.editor="\"{selfExecFile}\" --core-editor" """);
                    break;
                case EditorType.RebaseEditor:
                    builder.Append($"""-c core.editor="\"{selfExecFile}\" --rebase-message-editor" -c sequence.editor="\"{selfExecFile}\" --rebase-todo-editor" -c rebase.abbreviateCommands=true """);
                    break;
                default:
                    builder.Append("-c core.editor=true ");
                    break;
            }

            builder.Append(Args);
            start.Arguments = builder.ToString();

            // Working directory
            if (!string.IsNullOrEmpty(WorkingDirectory))
                start.WorkingDirectory = WorkingDirectory;

            return start;
        }

        private void HandleOutput(string line, List<string> errs)
        {
            if (line == null)
                return;

            Log?.AppendLine(line);

            // Lines to hide in error message.
            if (line.Length > 0)
            {
                if (line.StartsWith("remote: Enumerating objects:", StringComparison.Ordinal) ||
                    line.StartsWith("remote: Counting objects:", StringComparison.Ordinal) ||
                    line.StartsWith("remote: Compressing objects:", StringComparison.Ordinal) ||
                    line.StartsWith("Filtering content:", StringComparison.Ordinal) ||
                    line.StartsWith("hint:", StringComparison.Ordinal))
                    return;

                if (REG_PROGRESS().IsMatch(line))
                    return;
            }

            errs.Add(line);
        }

        [GeneratedRegex(@"\d+%")]
        private static partial Regex REG_PROGRESS();
    }
}
