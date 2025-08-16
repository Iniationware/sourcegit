using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SourceGit.Commands.Optimization;

namespace SourceGit.Commands
{
    /// <summary>
    /// Optimized version of QueryBranches using caching and batch execution
    /// </summary>
    public class QueryBranchesOptimized : Command
    {
        private const string PREFIX_LOCAL = "refs/heads/";
        private const string PREFIX_REMOTE = "refs/remotes/";
        private const string PREFIX_DETACHED_AT = "(HEAD detached at";
        private const string PREFIX_DETACHED_FROM = "(HEAD detached from";

        private readonly GitCommandCache _cache;
        private readonly BatchQueryExecutor _batchExecutor;
        private readonly GitProcessPool _processPool;

        public QueryBranchesOptimized(string repo)
        {
            WorkingDirectory = repo;
            Context = repo;
            Args = "branch -l --all -v --format=\"%(refname)%00%(committerdate:unix)%00%(objectname)%00%(HEAD)%00%(upstream)%00%(upstream:trackshort)\"";
            
            _cache = GitCommandCache.Instance;
            _batchExecutor = new BatchQueryExecutor();
            _processPool = GitProcessPool.Instance;
        }

        public async Task<List<Models.Branch>> GetResultAsync()
        {
            var branches = new List<Models.Branch>();
            
            try
            {
                Models.PerformanceMonitor.StartTimer("QueryBranchesOptimized");
                
                // Try to get from cache first
                var branchData = await _cache.GetOrExecuteAsync(
                    WorkingDirectory,
                    Args,
                    GitCommandCache.CacheType.Branches,
                    async () =>
                    {
                        // Use optimized process pool for execution
                        var result = await _processPool.ExecuteAsync(WorkingDirectory, Args);
                        return result.IsSuccess ? result.StdOut : string.Empty;
                    });
                
                if (string.IsNullOrEmpty(branchData))
                {
                    Models.PerformanceMonitor.StopTimer("QueryBranchesOptimized");
                    return branches;
                }

                var lines = branchData.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                var remoteHeads = new Dictionary<string, string>();
                var localBranchesNeedingStatus = new List<Models.Branch>();
                
                // First pass: Parse all branches
                foreach (var line in lines)
                {
                    var b = ParseLine(line);
                    if (b != null)
                    {
                        branches.Add(b);
                        if (!b.IsLocal)
                        {
                            remoteHeads.Add(b.FullName, b.Head);
                        }
                        else if (!string.IsNullOrEmpty(b.Upstream))
                        {
                            localBranchesNeedingStatus.Add(b);
                        }
                    }
                }

                // Second pass: Batch query track status for local branches
                if (localBranchesNeedingStatus.Count > 0)
                {
                    await UpdateTrackStatusBatch(localBranchesNeedingStatus, remoteHeads);
                }
                
                Models.PerformanceMonitor.StopTimer("QueryBranchesOptimized");
            }
            catch (Exception ex)
            {
                Models.PerformanceMonitor.StopTimer("QueryBranchesOptimized");
                App.RaiseException(WorkingDirectory, $"QueryBranchesOptimized exception: {ex.Message}");
            }

            return branches;
        }

        private async Task UpdateTrackStatusBatch(List<Models.Branch> localBranches, Dictionary<string, string> remoteHeads)
        {
            // Prepare batch queries for track status
            var statusQueries = new List<string>();
            var branchToQueryMap = new Dictionary<string, (Models.Branch Branch, string UpstreamHead)>();
            
            foreach (var branch in localBranches)
            {
                if (remoteHeads.TryGetValue(branch.Upstream, out var upstreamHead))
                {
                    branch.IsUpstreamGone = false;
                    
                    // Create query for track status (without --count to get actual commits)
                    var query = $"rev-list --left-right {branch.Head}...{upstreamHead}";
                    statusQueries.Add(query);
                    branchToQueryMap[query] = (branch, upstreamHead);
                }
                else
                {
                    branch.IsUpstreamGone = true;
                    branch.TrackStatus ??= new Models.BranchTrackStatus();
                }
            }
            
            if (statusQueries.Count > 0)
            {
                // Execute batch queries
                var batchOptions = new BatchQueryExecutor.BatchExecutionOptions
                {
                    UseCache = true,
                    UseParallel = statusQueries.Count > 5,
                    MaxParallelism = Math.Min(4, statusQueries.Count)
                };
                
                var batchResult = await _batchExecutor.ExecuteBatchAsync(WorkingDirectory, statusQueries.ToArray(), batchOptions);
                
                // Process results
                foreach (var kvp in batchResult.Results)
                {
                    if (branchToQueryMap.TryGetValue(kvp.Key, out var branchInfo))
                    {
                        branchInfo.Branch.TrackStatus = ParseTrackStatus(kvp.Value.Output);
                    }
                }
                
                // Handle failed queries - fallback to individual queries
                foreach (var failedQuery in batchResult.FailedQueries)
                {
                    if (branchToQueryMap.TryGetValue(failedQuery.Key, out var branchInfo))
                    {
                        // Fallback to individual query
                        branchInfo.Branch.TrackStatus = await new QueryTrackStatus(WorkingDirectory, branchInfo.Branch.Head, branchInfo.UpstreamHead).GetResultAsync();
                    }
                }
            }
        }

        private Models.BranchTrackStatus ParseTrackStatus(string output)
        {
            var status = new Models.BranchTrackStatus();
            
            if (string.IsNullOrWhiteSpace(output))
                return status;
            
            var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Length > 0)
                {
                    if (line[0] == '>')
                        status.Behind.Add(line.Substring(1));
                    else if (line[0] == '<')
                        status.Ahead.Add(line.Substring(1));
                }
            }
            
            return status;
        }

        private Models.Branch ParseLine(string line)
        {
            var parts = line.Split('\0');
            if (parts.Length != 6)
                return null;

            var branch = new Models.Branch();
            var refName = parts[0];
            if (refName.EndsWith("/HEAD", StringComparison.Ordinal))
                return null;

            branch.IsDetachedHead = refName.StartsWith(PREFIX_DETACHED_AT, StringComparison.Ordinal) ||
                refName.StartsWith(PREFIX_DETACHED_FROM, StringComparison.Ordinal);

            if (refName.StartsWith(PREFIX_LOCAL, StringComparison.Ordinal))
            {
                branch.Name = refName.Substring(PREFIX_LOCAL.Length);
                branch.IsLocal = true;
            }
            else if (refName.StartsWith(PREFIX_REMOTE, StringComparison.Ordinal))
            {
                var name = refName.Substring(PREFIX_REMOTE.Length);
                var nameParts = name.Split('/', 2);
                if (nameParts.Length != 2)
                    return null;

                branch.Remote = nameParts[0];
                branch.Name = nameParts[1];
                branch.IsLocal = false;
            }
            else
            {
                branch.Name = refName;
                branch.IsLocal = true;
            }

            ulong committerDate = 0;
            ulong.TryParse(parts[1], out committerDate);

            branch.FullName = refName;
            branch.CommitterDate = committerDate;
            branch.Head = parts[2];
            branch.IsCurrent = parts[3] == "*";
            branch.Upstream = parts[4];
            branch.IsUpstreamGone = false;

            if (!branch.IsLocal ||
                string.IsNullOrEmpty(branch.Upstream) ||
                string.IsNullOrEmpty(parts[5]) ||
                parts[5].Equals("=", StringComparison.Ordinal))
                branch.TrackStatus = new Models.BranchTrackStatus();

            return branch;
        }

        /// <summary>
        /// Prefetch branch data for better responsiveness
        /// </summary>
        public static async Task PrefetchAsync(string repository)
        {
            var cache = GitCommandCache.Instance;
            var processPool = GitProcessPool.Instance;
            
            // Prefetch main branch query
            await cache.GetOrExecuteAsync(
                repository,
                "branch -l --all -v --format=\"%(refname)%00%(committerdate:unix)%00%(objectname)%00%(HEAD)%00%(upstream)%00%(upstream:trackshort)\"",
                GitCommandCache.CacheType.Branches,
                async () =>
                {
                    var result = await processPool.ExecuteAsync(repository, "branch -l --all -v --format=\"%(refname)%00%(committerdate:unix)%00%(objectname)%00%(HEAD)%00%(upstream)%00%(upstream:trackshort)\"");
                    return result.IsSuccess ? result.StdOut : string.Empty;
                });
        }
    }
}