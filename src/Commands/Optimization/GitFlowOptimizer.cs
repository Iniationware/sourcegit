using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SourceGit.Commands.Optimization
{
    /// <summary>
    /// Optimizes Git-Flow operations by batching related commands and intelligent caching
    /// </summary>
    public class GitFlowOptimizer
    {
        private readonly GitCommandCache _cache;
        private readonly GitProcessPool _processPool;
        
        // Git-Flow branch patterns
        private static readonly Dictionary<string, string> _branchPrefixes = new()
        {
            { "feature", "feature/" },
            { "release", "release/" },
            { "hotfix", "hotfix/" },
            { "support", "support/" },
            { "bugfix", "bugfix/" }
        };

        public GitFlowOptimizer()
        {
            _cache = GitCommandCache.Instance;
            _processPool = GitProcessPool.Instance;
        }

        /// <summary>
        /// Optimize Git-Flow init operation
        /// </summary>
        public async Task<bool> OptimizeGitFlowInit(string repository)
        {
            // Check if Git-Flow is already initialized using cached config
            var configData = await _cache.GetOrExecuteAsync(
                repository,
                "config --get-regexp \"^gitflow\\.\"",
                GitCommandCache.CacheType.GitFlowConfig,
                async () =>
                {
                    var result = await _processPool.ExecuteAsync(repository, "config --get-regexp \"^gitflow\\.\"");
                    return result.IsSuccess ? result.StdOut : string.Empty;
                });
            
            return !string.IsNullOrEmpty(configData) && configData.Contains("gitflow.branch");
        }

        /// <summary>
        /// Optimize Git-Flow start operation with intelligent prefetching
        /// </summary>
        public async Task<GitFlowStartResult> OptimizeGitFlowStart(string repository, string branchType, string branchName, string baseBranch = null)
        {
            var result = new GitFlowStartResult();
            
            // Prefetch related data in parallel
            var tasks = new List<Task>();
            
            // Get current branches to check for conflicts
            tasks.Add(Task.Run(async () =>
            {
                result.ExistingBranches = await GetBranchesOfType(repository, branchType);
            }));
            
            // Get base branch info if not specified
            if (string.IsNullOrEmpty(baseBranch))
            {
                tasks.Add(Task.Run(async () =>
                {
                    baseBranch = await GetDefaultBaseBranch(repository, branchType);
                    result.BaseBranch = baseBranch;
                }));
            }
            
            // Get Git-Flow configuration
            tasks.Add(Task.Run(async () =>
            {
                result.GitFlowConfig = await GetGitFlowConfig(repository);
            }));
            
            await Task.WhenAll(tasks);
            
            // Check if branch already exists
            var fullBranchName = _branchPrefixes.GetValueOrDefault(branchType, branchType + "/") + branchName;
            if (result.ExistingBranches?.Contains(fullBranchName) == true)
            {
                result.Success = false;
                result.ErrorMessage = $"Branch '{fullBranchName}' already exists";
                return result;
            }
            
            // Invalidate caches that will be affected
            _cache.InvalidateByOperation(repository, GitOperation.GitFlowStart);
            
            result.Success = true;
            result.FullBranchName = fullBranchName;
            return result;
        }

        /// <summary>
        /// Optimize Git-Flow finish operation with batch cleanup
        /// </summary>
        public async Task<GitFlowFinishResult> OptimizeGitFlowFinish(string repository, string branchType, string branchName, bool deleteLocal = true, bool deleteRemote = false)
        {
            var result = new GitFlowFinishResult();
            var fullBranchName = _branchPrefixes.GetValueOrDefault(branchType, branchType + "/") + branchName;
            
            // Prefetch all necessary data
            var tasks = new List<Task>();
            
            // Get merge target branch
            tasks.Add(Task.Run(async () =>
            {
                result.TargetBranch = await GetMergeTargetBranch(repository, branchType);
            }));
            
            // Check for uncommitted changes
            tasks.Add(Task.Run(async () =>
            {
                result.HasUncommittedChanges = await HasUncommittedChanges(repository);
            }));
            
            // Get branch tracking info
            tasks.Add(Task.Run(async () =>
            {
                result.TrackingInfo = await GetBranchTrackingInfo(repository, fullBranchName);
            }));
            
            await Task.WhenAll(tasks);
            
            if (result.HasUncommittedChanges)
            {
                result.Success = false;
                result.ErrorMessage = "Cannot finish with uncommitted changes";
                return result;
            }
            
            // Prepare batch commands for cleanup with proper escaping
            var cleanupCommands = new List<string>();
            
            if (deleteLocal)
            {
                cleanupCommands.Add($"branch -d {EscapeGitArgument(fullBranchName)}");
            }
            
            if (deleteRemote && !string.IsNullOrEmpty(result.TrackingInfo))
            {
                var remoteBranch = ExtractRemoteBranch(result.TrackingInfo);
                if (!string.IsNullOrEmpty(remoteBranch))
                {
                    cleanupCommands.Add($"push origin --delete {EscapeGitArgument(remoteBranch)}");
                }
            }
            
            result.CleanupCommands = cleanupCommands;
            
            // Invalidate affected caches
            _cache.InvalidateByOperation(repository, GitOperation.GitFlowFinish);
            
            result.Success = true;
            return result;
        }

        /// <summary>
        /// Get all branches of a specific Git-Flow type
        /// </summary>
        private async Task<List<string>> GetBranchesOfType(string repository, string branchType)
        {
            var prefix = _branchPrefixes.GetValueOrDefault(branchType, branchType + "/");
            
            var branchData = await _cache.GetOrExecuteAsync(
                repository,
                "branch -a",
                GitCommandCache.CacheType.Branches,
                async () =>
                {
                    var result = await _processPool.ExecuteAsync(repository, "branch -a");
                    return result.IsSuccess ? result.StdOut : string.Empty;
                });
            
            if (string.IsNullOrEmpty(branchData))
                return new List<string>();
            
            var branches = new List<string>();
            var lines = branchData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var branch = line.Trim().TrimStart('*').Trim();
                if (branch.StartsWith(prefix) || branch.Contains("/" + prefix))
                {
                    // Extract local branch name
                    var lastSlash = branch.LastIndexOf('/');
                    if (lastSlash > 0 && branch.StartsWith("remotes/"))
                    {
                        branch = branch.Substring(branch.IndexOf('/', 8) + 1);
                    }
                    branches.Add(branch);
                }
            }
            
            return branches.Distinct().ToList();
        }

        /// <summary>
        /// Get default base branch for Git-Flow branch type
        /// </summary>
        private async Task<string> GetDefaultBaseBranch(string repository, string branchType)
        {
            var config = await GetGitFlowConfig(repository);
            
            return branchType switch
            {
                "feature" => config.GetValueOrDefault("gitflow.branch.develop", "develop"),
                "release" => config.GetValueOrDefault("gitflow.branch.develop", "develop"),
                "hotfix" => config.GetValueOrDefault("gitflow.branch.master", "master"),
                "support" => config.GetValueOrDefault("gitflow.branch.master", "master"),
                "bugfix" => config.GetValueOrDefault("gitflow.branch.develop", "develop"),
                _ => "develop"
            };
        }

        /// <summary>
        /// Get merge target branch for Git-Flow branch type
        /// </summary>
        private async Task<string> GetMergeTargetBranch(string repository, string branchType)
        {
            var config = await GetGitFlowConfig(repository);
            
            return branchType switch
            {
                "feature" => config.GetValueOrDefault("gitflow.branch.develop", "develop"),
                "release" => config.GetValueOrDefault("gitflow.branch.master", "master"),
                "hotfix" => config.GetValueOrDefault("gitflow.branch.master", "master"),
                "support" => config.GetValueOrDefault("gitflow.branch.master", "master"),
                "bugfix" => config.GetValueOrDefault("gitflow.branch.develop", "develop"),
                _ => "develop"
            };
        }

        /// <summary>
        /// Get Git-Flow configuration
        /// </summary>
        private async Task<Dictionary<string, string>> GetGitFlowConfig(string repository)
        {
            var configData = await _cache.GetOrExecuteAsync(
                repository,
                "config --get-regexp \"^gitflow\\.\"",
                GitCommandCache.CacheType.GitFlowConfig,
                async () =>
                {
                    var result = await _processPool.ExecuteAsync(repository, "config --get-regexp \"^gitflow\\.\"");
                    return result.IsSuccess ? result.StdOut : string.Empty;
                });
            
            var config = new Dictionary<string, string>();
            
            if (!string.IsNullOrEmpty(configData))
            {
                var lines = configData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var parts = line.Split(' ', 2);
                    if (parts.Length == 2)
                    {
                        config[parts[0]] = parts[1];
                    }
                }
            }
            
            return config;
        }

        /// <summary>
        /// Check for uncommitted changes
        /// </summary>
        private async Task<bool> HasUncommittedChanges(string repository)
        {
            var statusData = await _cache.GetOrExecuteAsync(
                repository,
                "status --porcelain",
                GitCommandCache.CacheType.Status,
                async () =>
                {
                    var result = await _processPool.ExecuteAsync(repository, "status --porcelain");
                    return result.IsSuccess ? result.StdOut : string.Empty;
                });
            
            return !string.IsNullOrWhiteSpace(statusData);
        }

        /// <summary>
        /// Get branch tracking information
        /// </summary>
        private async Task<string> GetBranchTrackingInfo(string repository, string branchName)
        {
            var result = await _processPool.ExecuteAsync(repository, $"branch -vv --list {branchName}");
            return result.IsSuccess ? result.StdOut : string.Empty;
        }

        /// <summary>
        /// Extract remote branch name from tracking info
        /// </summary>
        private string ExtractRemoteBranch(string trackingInfo)
        {
            // Pattern: [origin/branch-name]
            var match = Regex.Match(trackingInfo, @"\[([^/]+)/([^\]]+)\]");
            return match.Success ? match.Groups[2].Value : string.Empty;
        }

        /// <summary>
        /// Escape Git arguments to prevent command injection
        /// </summary>
        private static string EscapeGitArgument(string arg)
        {
            if (string.IsNullOrEmpty(arg))
                return arg;
            
            // Escape special characters that could be interpreted by shell
            if (arg.Contains(' ') || arg.Contains('"') || arg.Contains('\'') || 
                arg.Contains('$') || arg.Contains('`') || arg.Contains('\\') ||
                arg.Contains('!') || arg.Contains('&') || arg.Contains('|') ||
                arg.Contains(';') || arg.Contains('<') || arg.Contains('>'))
            {
                // Use double quotes and escape internal quotes
                return $"\"{arg.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
            }
            
            return arg;
        }

        public class GitFlowStartResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public string FullBranchName { get; set; }
            public string BaseBranch { get; set; }
            public List<string> ExistingBranches { get; set; }
            public Dictionary<string, string> GitFlowConfig { get; set; }
        }

        public class GitFlowFinishResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public string TargetBranch { get; set; }
            public bool HasUncommittedChanges { get; set; }
            public string TrackingInfo { get; set; }
            public List<string> CleanupCommands { get; set; }
        }
    }
}