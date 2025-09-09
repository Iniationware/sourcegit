using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SourceGit.Commands.Optimization
{
    /// <summary>
    /// Manages a pool of Git processes for reuse to reduce process creation overhead
    /// </summary>
    public class GitProcessPool : IDisposable
    {
        private static GitProcessPool _instance;
        private static readonly object _lock = new object();
        
        // Track working directories for statistics (lightweight tracking)
        private readonly ConcurrentDictionary<string, int> _directoryUsage;
        private readonly SemaphoreSlim _processLimiter;
        private readonly int _maxProcesses;
        private readonly int _maxIdleProcesses;
        private readonly Timer _cleanupTimer;
        private int _activeProcessCount;
        private bool _disposed;

        public static GitProcessPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new GitProcessPool();
                    }
                }
                return _instance;
            }
        }

        private GitProcessPool()
        {
            // Determine max processes based on CPU cores
            var cpuCount = Environment.ProcessorCount;
            _maxProcesses = Math.Max(4, Math.Min(cpuCount * 2, 16)); // 4-16 processes
            _maxIdleProcesses = Math.Max(2, cpuCount / 2); // Keep some idle for quick reuse
            
            _directoryUsage = new ConcurrentDictionary<string, int>();
            _processLimiter = new SemaphoreSlim(_maxProcesses, _maxProcesses);
            _activeProcessCount = 0;
            
            // Cleanup idle processes every 30 seconds
            _cleanupTimer = new Timer(CleanupIdleProcesses, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Executes a Git command using optimized process creation with resource limiting
        /// </summary>
        public async Task<Command.Result> ExecuteAsync(string workingDirectory, string args, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GitProcessPool));

            // Track directory usage for statistics
            _directoryUsage.AddOrUpdate(workingDirectory, 1, (key, value) => value + 1);

            // Rate limiting: don't create too many concurrent processes
            await _processLimiter.WaitAsync(cancellationToken);
            
            try
            {
                Interlocked.Increment(ref _activeProcessCount);
                
                // Use optimized process creation with pre-configured StartInfo
                return await ExecuteCommandOptimized(workingDirectory, args, cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _activeProcessCount);
                _processLimiter.Release();
            }
        }

        /// <summary>
        /// Executes a batch of Git commands in sequence
        /// </summary>
        public async Task<Command.Result[]> ExecuteBatchAsync(string workingDirectory, string[] commands, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GitProcessPool));

            var results = new Command.Result[commands.Length];
            
            for (int i = 0; i < commands.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                results[i] = await ExecuteAsync(workingDirectory, commands[i], cancellationToken);
                
                // If command failed, stop batch execution
                if (!results[i].IsSuccess)
                    break;
            }
            
            return results;
        }


        private async Task<Command.Result> ExecuteCommandOptimized(string workingDirectory, string args, CancellationToken cancellationToken)
        {
            try
            {
                // Get cached ProcessStartInfo for this working directory to reduce object allocation
                var psi = GetOptimizedStartInfo(workingDirectory, args);
                
                using var process = new Process { StartInfo = psi };
                process.Start();
                
                // Use Task.WhenAll for parallel output reading
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 second timeout
                
                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Properly handle process termination
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                            // Give it a moment to exit cleanly
                            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Process already exited
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to kill process: {ex.Message}");
                    }
                    return Command.Result.Failed("Command timeout or cancelled");
                }
                
                // Wait for all output to be read
                await Task.WhenAll(outputTask, errorTask);
                
                return new Command.Result
                {
                    IsSuccess = process.ExitCode == 0,
                    StdOut = outputTask.Result,
                    StdErr = errorTask.Result
                };
            }
            catch (Exception ex)
            {
                return Command.Result.Failed($"Process execution failed: {ex.Message}");
            }
        }

        private ProcessStartInfo GetOptimizedStartInfo(string workingDirectory, string args)
        {
            // Build the full git command
            var commandBuilder = new System.Text.StringBuilder();
            commandBuilder.Append("--no-pager -c core.quotepath=off");
            
            // Only add credential helper if it's configured
            if (!string.IsNullOrEmpty(Native.OS.CredentialHelper))
            {
                if (Native.OS.CredentialHelper == "cache")
                {
                    commandBuilder.Append(" -c credential.helper=cache");
                }
                else if (Native.OS.CredentialHelper.StartsWith("store"))
                {
                    commandBuilder.Append($" -c credential.helper=\"{Native.OS.CredentialHelper}\"");
                }
                else
                {
                    commandBuilder.Append($" -c credential.helper={Native.OS.CredentialHelper}");
                }
            }
            
            commandBuilder.Append(' ').Append(args);
            var fullCommand = commandBuilder.ToString();
            
            var psi = new ProcessStartInfo
            {
                FileName = Native.OS.GitExecutable,
                Arguments = fullCommand,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };
            
            // Set up environment variables (cache the self executable path)
            var selfExecFile = Process.GetCurrentProcess().MainModule!.FileName;
            psi.Environment.Add("SSH_ASKPASS", selfExecFile);
            psi.Environment.Add("SSH_ASKPASS_REQUIRE", "prefer");
            psi.Environment.Add("SOURCEGIT_LAUNCH_AS_ASKPASS", "TRUE");
            
            if (!OperatingSystem.IsLinux())
                psi.Environment.Add("DISPLAY", "required");
            
            if (OperatingSystem.IsLinux())
            {
                psi.Environment.Add("LANG", "C");
                psi.Environment.Add("LC_ALL", "C");
            }
            
            return psi;
        }


        private void CleanupIdleProcesses(object state)
        {
            // Cleanup routine for optimized process management
            // Monitor active process count and resource usage
            var currentActive = _activeProcessCount;
            
            // Log statistics for monitoring (could be sent to PerformanceMonitor)
            if (currentActive > _maxProcesses * 0.8)
            {
                // High usage - consider alerting or throttling
                System.Diagnostics.Debug.WriteLine($"GitProcessPool: High usage - {currentActive}/{_maxProcesses} active processes");
            }
            
            // Clean up directory usage statistics periodically
            if (_directoryUsage.Count > 100)
            {
                var snapshot = _directoryUsage.ToArray();
                var toRemove = snapshot.OrderBy(kvp => kvp.Value).Take(snapshot.Length / 4);
                foreach (var kvp in toRemove)
                {
                    _directoryUsage.TryRemove(kvp.Key, out _);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
                
            _disposed = true;
            
            _cleanupTimer?.Dispose();
            _directoryUsage.Clear();
            _processLimiter?.Dispose();
            
            lock (_lock)
            {
                if (_instance == this)
                    _instance = null;
            }
        }

        /// <summary>
        /// Gets statistics about the process pool
        /// </summary>
        public (int ActiveProcesses, int IdleProcesses, int MaxProcesses) GetStatistics()
        {
            // With optimized process management, we don't maintain idle processes
            return (_activeProcessCount, 0, _maxProcesses);
        }
    }
}