using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SourceGit.Commands
{
    /// <summary>
    /// Command extension that handles Git lock files and implements retry logic
    /// </summary>
    public class CommandWithRetry : Command
    {
        private const int MAX_RETRIES = 5;
        private const int INITIAL_DELAY_MS = 100;
        private const int MAX_DELAY_MS = 5000;
        
        private readonly Command _innerCommand;
        
        public CommandWithRetry(Command command)
        {
            _innerCommand = command;
            // Copy properties from inner command
            this.Context = command.Context;
            this.WorkingDirectory = command.WorkingDirectory;
            this.Editor = command.Editor;
            this.SSHKey = command.SSHKey;
            this.Args = command.Args;
            this.CancellationToken = command.CancellationToken;
            this.RaiseError = command.RaiseError;
            this.Log = command.Log;
        }
        
        /// <summary>
        /// Executes the command with retry logic for lock file conflicts
        /// </summary>
        public new async Task<bool> ExecAsync()
        {
            int retryCount = 0;
            int delayMs = INITIAL_DELAY_MS;
            
            while (retryCount < MAX_RETRIES)
            {
                // Check for lock files before attempting operation
                if (HasLockFiles())
                {
                    await Task.Delay(delayMs, this.CancellationToken);
                    delayMs = Math.Min(delayMs * 2, MAX_DELAY_MS); // Exponential backoff
                    retryCount++;
                    continue;
                }
                
                try
                {
                    // Store original RaiseError setting
                    var originalRaiseError = this.RaiseError;
                    
                    // Suppress error for retry attempts
                    if (retryCount > 0)
                        this.RaiseError = false;
                    
                    var result = await base.ExecAsync();
                    
                    // Restore original setting
                    this.RaiseError = originalRaiseError;
                    
                    if (result)
                        return true;
                    
                    // Check if failure was due to lock file
                    if (IsLockFileError())
                    {
                        await Task.Delay(delayMs, this.CancellationToken);
                        delayMs = Math.Min(delayMs * 2, MAX_DELAY_MS);
                        retryCount++;
                        continue;
                    }
                    
                    // If not a lock file error, return the failure
                    return false;
                }
                catch (TaskCanceledException)
                {
                    throw; // Rethrow cancellation
                }
                catch (Exception ex)
                {
                    if (retryCount >= MAX_RETRIES - 1)
                        throw; // Rethrow on final attempt
                    
                    // Check if exception is lock-related
                    if (ex.Message.Contains("index.lock") || 
                        ex.Message.Contains("Another git process") ||
                        ex.Message.Contains("Unable to create") ||
                        ex.Message.Contains("locked"))
                    {
                        await Task.Delay(delayMs, this.CancellationToken);
                        delayMs = Math.Min(delayMs * 2, MAX_DELAY_MS);
                        retryCount++;
                        continue;
                    }
                    
                    throw; // Rethrow non-lock exceptions
                }
            }
            
            // Final attempt with error reporting enabled
            this.RaiseError = true;
            return await base.ExecAsync();
        }
        
        /// <summary>
        /// Executes the command with retry logic and returns the result
        /// </summary>
        public async Task<Command.Result> ReadToEndWithRetryAsync()
        {
            int retryCount = 0;
            int delayMs = INITIAL_DELAY_MS;
            
            while (retryCount < MAX_RETRIES)
            {
                // Check for lock files before attempting operation
                if (HasLockFiles())
                {
                    await Task.Delay(delayMs);
                    delayMs = Math.Min(delayMs * 2, MAX_DELAY_MS);
                    retryCount++;
                    continue;
                }
                
                try
                {
                    var result = await base.ReadToEndAsync();
                    
                    if (result.IsSuccess)
                        return result;
                    
                    // Check if failure was due to lock file
                    if (IsLockFileError(result.StdErr))
                    {
                        await Task.Delay(delayMs);
                        delayMs = Math.Min(delayMs * 2, MAX_DELAY_MS);
                        retryCount++;
                        continue;
                    }
                    
                    return result;
                }
                catch (Exception ex)
                {
                    if (retryCount >= MAX_RETRIES - 1)
                        return Command.Result.Failed(ex.Message);
                    
                    // Check if exception is lock-related
                    if (ex.Message.Contains("index.lock") || 
                        ex.Message.Contains("Another git process") ||
                        ex.Message.Contains("Unable to create") ||
                        ex.Message.Contains("locked"))
                    {
                        await Task.Delay(delayMs);
                        delayMs = Math.Min(delayMs * 2, MAX_DELAY_MS);
                        retryCount++;
                        continue;
                    }
                    
                    return Command.Result.Failed(ex.Message);
                }
            }
            
            // Final attempt
            return await base.ReadToEndAsync();
        }
        
        /// <summary>
        /// Checks if common Git lock files exist
        /// </summary>
        private bool HasLockFiles()
        {
            if (string.IsNullOrEmpty(WorkingDirectory))
                return false;
            
            var gitDir = Path.Combine(WorkingDirectory, ".git");
            if (!Directory.Exists(gitDir))
                return false;
            
            // Check for common lock files
            string[] lockFiles = 
            {
                Path.Combine(gitDir, "index.lock"),
                Path.Combine(gitDir, "HEAD.lock"),
                Path.Combine(gitDir, "config.lock"),
                Path.Combine(gitDir, "refs", "heads", "*.lock"),
                Path.Combine(gitDir, "refs", "remotes", "*.lock")
            };
            
            foreach (var lockFile in lockFiles)
            {
                if (lockFile.Contains('*'))
                {
                    // Handle wildcard patterns
                    var dir = Path.GetDirectoryName(lockFile);
                    var pattern = Path.GetFileName(lockFile);
                    
                    if (Directory.Exists(dir))
                    {
                        var files = Directory.GetFiles(dir, pattern);
                        if (files.Length > 0)
                            return true;
                    }
                }
                else if (File.Exists(lockFile))
                {
                    // Check if lock file is stale (older than 10 minutes)
                    var fileInfo = new FileInfo(lockFile);
                    if (DateTime.Now - fileInfo.LastWriteTime > TimeSpan.FromMinutes(10))
                    {
                        // Try to remove stale lock file
                        try
                        {
                            File.Delete(lockFile);
                        }
                        catch
                        {
                            // Can't delete, consider it active
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Checks if the command failed due to a lock file
        /// </summary>
        private bool IsLockFileError()
        {
            // This would need access to the error output from the command
            // For now, we'll rely on the external error checking
            return false;
        }
        
        /// <summary>
        /// Checks if error message indicates a lock file issue
        /// </summary>
        private bool IsLockFileError(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
                return false;
            
            string[] lockIndicators = 
            {
                "index.lock",
                "Another git process",
                "Unable to create",
                "locked",
                "Permission denied",
                "fatal: Unable to create",
                "already exists",
                "cannot lock ref"
            };
            
            foreach (var indicator in lockIndicators)
            {
                if (errorMessage.Contains(indicator, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            
            return false;
        }
    }
}