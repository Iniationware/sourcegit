using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SourceGit.Commands.Optimization
{
    /// <summary>
    /// Executes multiple Git queries in optimized batches to reduce process overhead
    /// </summary>
    public class BatchQueryExecutor
    {
        private readonly GitProcessPool _processPool;
        private readonly GitCommandCache _cache;
        
        // Common query combinations that can be batched
        private static readonly Dictionary<string, string[]> _batchTemplates = new()
        {
            { "repository-state", new[] { "status --porcelain", "branch -a", "tag -l", "remote -v" } },
            { "branch-info", new[] { "branch -vv", "branch -r", "branch --merged", "branch --no-merged" } },
            { "commit-info", new[] { "log --oneline -20", "log --graph --oneline -10", "reflog -10" } },
            { "remote-state", new[] { "remote -v", "ls-remote --heads", "ls-remote --tags" } },
            { "config-all", new[] { "config --list", "config --get-regexp \"^user\\.\"", "config --get-regexp \"^gitflow\\.\"" } }
        };

        public BatchQueryExecutor()
        {
            _processPool = GitProcessPool.Instance;
            _cache = GitCommandCache.Instance;
        }

        /// <summary>
        /// Execute a batch of queries with intelligent caching and parallelization
        /// </summary>
        public async Task<BatchQueryResult> ExecuteBatchAsync(
            string repository, 
            string[] queries, 
            BatchExecutionOptions options = null)
        {
            options ??= new BatchExecutionOptions();
            var result = new BatchQueryResult();
            var stopwatch = Stopwatch.StartNew();
            
            // Group queries by cache type
            var queryGroups = GroupQueriesByCacheType(queries);
            
            // Execute based on strategy
            if (options.UseParallel && queries.Length > 3)
            {
                await ExecuteParallelBatches(repository, queryGroups, result, options);
            }
            else
            {
                await ExecuteSequentialBatches(repository, queryGroups, result, options);
            }
            
            stopwatch.Stop();
            result.TotalExecutionTime = stopwatch.ElapsedMilliseconds;
            result.Success = result.FailedQueries.Count == 0;
            
            // Log performance metrics
            if (options.TrackPerformance)
            {
                Models.PerformanceMonitor.StartTimer($"BatchQuery_{queries.Length}");
                Models.PerformanceMonitor.StopTimer($"BatchQuery_{queries.Length}");
            }
            
            return result;
        }

        /// <summary>
        /// Execute a predefined batch template
        /// </summary>
        public async Task<BatchQueryResult> ExecuteTemplateAsync(
            string repository, 
            string templateName, 
            BatchExecutionOptions options = null)
        {
            if (!_batchTemplates.TryGetValue(templateName, out var queries))
            {
                throw new ArgumentException($"Unknown batch template: {templateName}");
            }
            
            return await ExecuteBatchAsync(repository, queries, options);
        }

        /// <summary>
        /// Execute queries that can be combined into a single Git process
        /// </summary>
        public async Task<string> ExecuteCombinedQueryAsync(string repository, string[] queries)
        {
            // Some Git commands can be combined with semicolons or &&
            // For safety, we'll only combine read-only queries
            var safeQueries = queries.Where(IsReadOnlyQuery).ToArray();
            
            if (safeQueries.Length == 0)
                return string.Empty;
            
            if (safeQueries.Length == 1)
            {
                var result = await _processPool.ExecuteAsync(repository, safeQueries[0]);
                return result.IsSuccess ? result.StdOut : string.Empty;
            }
            
            // For multiple queries, execute them in sequence and combine results
            var combinedResults = new List<string>();
            
            foreach (var query in safeQueries)
            {
                var cacheType = DetermineCacheType(query);
                var result = await _cache.GetOrExecuteAsync(
                    repository,
                    query,
                    cacheType,
                    async () =>
                    {
                        var execResult = await _processPool.ExecuteAsync(repository, query);
                        return execResult.IsSuccess ? execResult.StdOut : string.Empty;
                    });
                
                if (!string.IsNullOrEmpty(result))
                {
                    combinedResults.Add($"=== {query} ===");
                    combinedResults.Add(result);
                }
            }
            
            return string.Join("\n", combinedResults);
        }

        /// <summary>
        /// Prefetch common queries for better responsiveness
        /// </summary>
        public async Task PrefetchCommonQueriesAsync(string repository)
        {
            var commonQueries = new[]
            {
                "status --porcelain",
                "branch -a",
                "remote -v",
                "config --get-regexp \"^gitflow\\.\"",
                "log --oneline -1"
            };
            
            var tasks = commonQueries.Select(query => Task.Run(async () =>
            {
                var cacheType = DetermineCacheType(query);
                await _cache.GetOrExecuteAsync(
                    repository,
                    query,
                    cacheType,
                    async () =>
                    {
                        var result = await _processPool.ExecuteAsync(repository, query);
                        return result.IsSuccess ? result.StdOut : string.Empty;
                    });
            }));
            
            await Task.WhenAll(tasks);
        }

        private async Task ExecuteParallelBatches(
            string repository,
            Dictionary<GitCommandCache.CacheType, List<string>> queryGroups,
            BatchQueryResult result,
            BatchExecutionOptions options)
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Min(options.MaxParallelism, Environment.ProcessorCount)
            };
            
            var tasks = new List<Task>();
            
            foreach (var group in queryGroups)
            {
                foreach (var query in group.Value)
                {
                    tasks.Add(ExecuteSingleQueryAsync(repository, query, group.Key, result, options));
                }
            }
            
            await Task.WhenAll(tasks);
        }

        private async Task ExecuteSequentialBatches(
            string repository,
            Dictionary<GitCommandCache.CacheType, List<string>> queryGroups,
            BatchQueryResult result,
            BatchExecutionOptions options)
        {
            foreach (var group in queryGroups)
            {
                foreach (var query in group.Value)
                {
                    await ExecuteSingleQueryAsync(repository, query, group.Key, result, options);
                }
            }
        }

        private async Task ExecuteSingleQueryAsync(
            string repository,
            string query,
            GitCommandCache.CacheType cacheType,
            BatchQueryResult result,
            BatchExecutionOptions options)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                string output;
                if (options.UseCache)
                {
                    output = await _cache.GetOrExecuteAsync(
                        repository,
                        query,
                        cacheType,
                        async () =>
                        {
                            var execResult = await _processPool.ExecuteAsync(repository, query);
                            return execResult.IsSuccess ? execResult.StdOut : string.Empty;
                        });
                }
                else
                {
                    var execResult = await _processPool.ExecuteAsync(repository, query);
                    output = execResult.IsSuccess ? execResult.StdOut : string.Empty;
                }
                
                stopwatch.Stop();
                
                result.Results[query] = new QueryResult
                {
                    Query = query,
                    Output = output,
                    ExecutionTime = stopwatch.ElapsedMilliseconds,
                    FromCache = options.UseCache && _cache.GetStatistics().TotalHits > 0
                };
            }
            catch (Exception ex)
            {
                result.FailedQueries[query] = ex.Message;
            }
        }

        private Dictionary<GitCommandCache.CacheType, List<string>> GroupQueriesByCacheType(string[] queries)
        {
            var groups = new Dictionary<GitCommandCache.CacheType, List<string>>();
            
            foreach (var query in queries)
            {
                var cacheType = DetermineCacheType(query);
                
                if (!groups.ContainsKey(cacheType))
                    groups[cacheType] = new List<string>();
                
                groups[cacheType].Add(query);
            }
            
            return groups;
        }

        private GitCommandCache.CacheType DetermineCacheType(string query)
        {
            if (query.Contains("status"))
                return GitCommandCache.CacheType.Status;
            if (query.Contains("branch"))
                return GitCommandCache.CacheType.Branches;
            if (query.Contains("tag"))
                return GitCommandCache.CacheType.Tags;
            if (query.Contains("remote"))
                return GitCommandCache.CacheType.Remotes;
            if (query.Contains("config") && query.Contains("gitflow"))
                return GitCommandCache.CacheType.GitFlowConfig;
            if (query.Contains("config"))
                return GitCommandCache.CacheType.Config;
            
            // Default to Status (shortest cache time)
            return GitCommandCache.CacheType.Status;
        }

        private bool IsReadOnlyQuery(string query)
        {
            // List of safe read-only Git commands
            var readOnlyCommands = new[]
            {
                "status", "log", "branch", "tag", "remote", "config --get",
                "config --list", "show", "diff", "ls-files", "ls-tree",
                "rev-parse", "describe", "reflog", "ls-remote"
            };
            
            return readOnlyCommands.Any(cmd => query.StartsWith(cmd));
        }

        public class BatchQueryResult
        {
            public Dictionary<string, QueryResult> Results { get; } = new();
            public Dictionary<string, string> FailedQueries { get; } = new();
            public long TotalExecutionTime { get; set; }
            public bool Success { get; set; }
            
            public int CacheHits => Results.Values.Count(r => r.FromCache);
            public int TotalQueries => Results.Count + FailedQueries.Count;
        }

        public class QueryResult
        {
            public string Query { get; set; }
            public string Output { get; set; }
            public long ExecutionTime { get; set; }
            public bool FromCache { get; set; }
        }

        public class BatchExecutionOptions
        {
            public bool UseCache { get; set; } = true;
            public bool UseParallel { get; set; } = true;
            public int MaxParallelism { get; set; } = 4;
            public bool TrackPerformance { get; set; } = true;
            public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        }
    }
}