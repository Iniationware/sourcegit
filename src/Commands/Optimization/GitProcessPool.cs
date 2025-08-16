using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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
        
        private readonly ConcurrentQueue<Process> _availableProcesses;
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
            
            _availableProcesses = new ConcurrentQueue<Process>();
            _processLimiter = new SemaphoreSlim(_maxProcesses, _maxProcesses);
            _activeProcessCount = 0;
            
            // Cleanup idle processes every 30 seconds
            _cleanupTimer = new Timer(CleanupIdleProcesses, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Executes a Git command using a pooled process
        /// </summary>
        public async Task<Command.Result> ExecuteAsync(string workingDirectory, string args, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GitProcessPool));

            // Wait for available process slot
            await _processLimiter.WaitAsync(cancellationToken);
            
            try
            {
                Interlocked.Increment(ref _activeProcessCount);
                
                // Try to reuse an existing process for the same working directory
                Process process = null;
                if (!_availableProcesses.TryDequeue(out process) || process.HasExited)
                {
                    // Create new process if none available
                    process = CreateGitProcess(workingDirectory);
                }
                
                // Execute the command
                return await ExecuteCommand(process, workingDirectory, args, cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _activeProcessCount);
                _processLimiter.Release();
            }
        }

        /// <summary>
        /// Executes a batch of Git commands in sequence using the same process
        /// </summary>
        public async Task<Command.Result[]> ExecuteBatchAsync(string workingDirectory, string[] commands, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GitProcessPool));

            await _processLimiter.WaitAsync(cancellationToken);
            
            try
            {
                Interlocked.Increment(ref _activeProcessCount);
                
                Process process = null;
                if (!_availableProcesses.TryDequeue(out process) || process.HasExited)
                {
                    process = CreateGitProcess(workingDirectory);
                }
                
                var results = new Command.Result[commands.Length];
                for (int i = 0; i < commands.Length; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    results[i] = await ExecuteCommand(process, workingDirectory, commands[i], cancellationToken);
                    
                    // If command failed, stop batch execution
                    if (!results[i].IsSuccess)
                        break;
                }
                
                // Return process to pool if still valid
                if (!process.HasExited && _availableProcesses.Count < _maxIdleProcesses)
                {
                    _availableProcesses.Enqueue(process);
                }
                else
                {
                    process.Kill();
                    process.Dispose();
                }
                
                return results;
            }
            finally
            {
                Interlocked.Decrement(ref _activeProcessCount);
                _processLimiter.Release();
            }
        }

        private Process CreateGitProcess(string workingDirectory)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Native.OS.GitExecutable,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };
            
            // Set up environment variables
            var selfExecFile = Process.GetCurrentProcess().MainModule!.FileName;
            startInfo.Environment.Add("SSH_ASKPASS", selfExecFile);
            startInfo.Environment.Add("SSH_ASKPASS_REQUIRE", "prefer");
            startInfo.Environment.Add("SOURCEGIT_LAUNCH_AS_ASKPASS", "TRUE");
            
            if (!OperatingSystem.IsLinux())
                startInfo.Environment.Add("DISPLAY", "required");
            
            if (OperatingSystem.IsLinux())
            {
                startInfo.Environment.Add("LANG", "C");
                startInfo.Environment.Add("LC_ALL", "C");
            }
            
            var process = new Process { StartInfo = startInfo };
            process.Start();
            
            return process;
        }

        private async Task<Command.Result> ExecuteCommand(Process process, string workingDirectory, string args, CancellationToken cancellationToken)
        {
            try
            {
                // Change working directory if needed
                if (process.StartInfo.WorkingDirectory != workingDirectory)
                {
                    process.StartInfo.WorkingDirectory = workingDirectory;
                    // For persistent processes, we'd need to handle this differently
                    // For now, recreate the process
                    process.Kill();
                    process.Dispose();
                    process = CreateGitProcess(workingDirectory);
                }
                
                // Build the full git command
                var fullCommand = $"--no-pager -c core.quotepath=off -c credential.helper={Native.OS.CredentialHelper} {args}";
                
                // Execute the command
                process.StartInfo.Arguments = fullCommand;
                
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    cts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 second timeout
                    
                    try
                    {
                        await Task.WhenAll(outputTask, errorTask);
                    }
                    catch (OperationCanceledException)
                    {
                        process.Kill();
                        return Command.Result.Failed("Command timeout or cancelled");
                    }
                }
                
                return new Command.Result
                {
                    IsSuccess = process.ExitCode == 0,
                    StdOut = await outputTask,
                    StdErr = await errorTask
                };
            }
            catch (Exception ex)
            {
                return Command.Result.Failed($"Process execution failed: {ex.Message}");
            }
        }

        private void CleanupIdleProcesses(object state)
        {
            // Clean up excess idle processes
            while (_availableProcesses.Count > _maxIdleProcesses)
            {
                if (_availableProcesses.TryDequeue(out var process))
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill();
                        process.Dispose();
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
            
            // Clean up any dead processes
            var tempList = new System.Collections.Generic.List<Process>();
            while (_availableProcesses.TryDequeue(out var process))
            {
                if (!process.HasExited)
                {
                    tempList.Add(process);
                }
                else
                {
                    process.Dispose();
                }
            }
            
            foreach (var process in tempList)
            {
                _availableProcesses.Enqueue(process);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
                
            _disposed = true;
            
            _cleanupTimer?.Dispose();
            
            // Kill all pooled processes
            while (_availableProcesses.TryDequeue(out var process))
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill();
                    process.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
            
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
            return (_activeProcessCount, _availableProcesses.Count, _maxProcesses);
        }
    }
}