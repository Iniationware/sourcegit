using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SourceGit.Commands.Optimization
{
    /// <summary>
    /// Intelligent caching layer for Git command results with Git-Flow awareness
    /// </summary>
    public class GitCommandCache : IDisposable
    {
        private static GitCommandCache _instance;
        private static readonly object _lock = new object();
        
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly Timer _cleanupTimer;
        private bool _disposed;

        public class CacheEntry
        {
            private int _hitCount;
            
            public string Data { get; set; }
            public DateTime ExpiresAt { get; set; }
            public CacheType Type { get; set; }
            
            public int HitCount 
            { 
                get => _hitCount;
                set => _hitCount = value;
            }
            
            public void IncrementHitCount()
            {
                Interlocked.Increment(ref _hitCount);
            }
        }

        public enum CacheType
        {
            Config,          // 15 minutes - rarely changes
            Remotes,         // 10 minutes - relatively stable
            Branches,        // 2 minutes - moderate volatility
            Tags,            // 5 minutes - low volatility
            Status,          // 30 seconds - high volatility
            GitFlowConfig,   // 15 minutes - Git-Flow specific
            BranchRelations  // 30 seconds - very volatile during Git-Flow ops
        }

        public static GitCommandCache Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new GitCommandCache();
                    }
                }
                return _instance;
            }
        }

        private GitCommandCache()
        {
            _cache = new ConcurrentDictionary<string, CacheEntry>();
            
            // Cleanup expired entries every minute
            _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Get cached result or execute command and cache
        /// </summary>
        public async Task<string> GetOrExecuteAsync(string repository, string command, CacheType type, Func<Task<string>> executor)
        {
            var cacheKey = GetCacheKey(repository, command);
            
            // Check if we have a valid cached entry
            if (_cache.TryGetValue(cacheKey, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
            {
                entry.IncrementHitCount();
                Models.PerformanceMonitor.StartTimer($"CacheHit_{type}");
                Models.PerformanceMonitor.StopTimer($"CacheHit_{type}");
                return entry.Data;
            }
            
            // Execute the command
            Models.PerformanceMonitor.StartTimer($"CacheMiss_{type}");
            var result = await executor();
            Models.PerformanceMonitor.StopTimer($"CacheMiss_{type}");
            
            // Cache the result
            if (!string.IsNullOrEmpty(result))
            {
                var expiration = GetExpiration(type);
                _cache[cacheKey] = new CacheEntry
                {
                    Data = result,
                    ExpiresAt = DateTime.UtcNow.Add(expiration),
                    Type = type,
                    HitCount = 0
                };
            }
            
            return result;
        }

        /// <summary>
        /// Invalidate cache entries based on operation type
        /// </summary>
        public void InvalidateByOperation(string repository, GitOperation operation)
        {
            var keysToRemove = new List<string>();
            
            // Create snapshot to avoid enumeration during modification
            var snapshot = _cache.ToArray();
            
            foreach (var kvp in snapshot)
            {
                if (!kvp.Key.StartsWith(repository))
                    continue;
                    
                var shouldInvalidate = ShouldInvalidate(kvp.Value.Type, operation);
                if (shouldInvalidate)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Invalidate all cache for a repository
        /// </summary>
        public void InvalidateRepository(string repository)
        {
            var keysToRemove = new List<string>();
            
            // Create snapshot to avoid enumeration during modification
            var snapshot = _cache.Keys.ToArray();
            
            foreach (var key in snapshot)
            {
                if (key.StartsWith(repository))
                {
                    keysToRemove.Add(key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Invalidate specific cache type for a repository
        /// </summary>
        public void InvalidateCacheType(string repository, CacheType type)
        {
            var keysToRemove = new List<string>();
            
            // Create snapshot to avoid enumeration during modification
            var snapshot = _cache.ToArray();
            
            foreach (var kvp in snapshot)
            {
                if (kvp.Key.StartsWith(repository) && kvp.Value.Type == type)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            var stats = new CacheStatistics
            {
                TotalEntries = _cache.Count,
                EntriesByType = new Dictionary<CacheType, int>(),
                TotalHits = 0,
                EstimatedMemoryMB = 0
            };
            
            // Create snapshot to avoid enumeration during modification
            var snapshot = _cache.ToArray();
            
            foreach (var kvp in snapshot)
            {
                var entry = kvp.Value;
                
                if (!stats.EntriesByType.ContainsKey(entry.Type))
                    stats.EntriesByType[entry.Type] = 0;
                    
                stats.EntriesByType[entry.Type]++;
                stats.TotalHits += entry.HitCount;
                
                // Rough memory estimation (2 bytes per char)
                stats.EstimatedMemoryMB += (entry.Data?.Length ?? 0) * 2.0 / (1024 * 1024);
            }
            
            return stats;
        }

        private string GetCacheKey(string repository, string command)
        {
            // Create a unique key for this repository and command
            return $"{repository}|{command}";
        }

        private TimeSpan GetExpiration(CacheType type)
        {
            return type switch
            {
                CacheType.Config => TimeSpan.FromMinutes(15),
                CacheType.GitFlowConfig => TimeSpan.FromMinutes(15),
                CacheType.Remotes => TimeSpan.FromMinutes(10),
                CacheType.Tags => TimeSpan.FromMinutes(5),
                CacheType.Branches => TimeSpan.FromMinutes(2),
                CacheType.Status => TimeSpan.FromSeconds(30),
                CacheType.BranchRelations => TimeSpan.FromSeconds(30),
                _ => TimeSpan.FromMinutes(1)
            };
        }

        private bool ShouldInvalidate(CacheType cacheType, GitOperation operation)
        {
            return operation switch
            {
                GitOperation.Checkout => cacheType is CacheType.Status or CacheType.BranchRelations,
                GitOperation.BranchCreate => cacheType is CacheType.Branches or CacheType.BranchRelations,
                GitOperation.BranchDelete => cacheType is CacheType.Branches or CacheType.BranchRelations,
                GitOperation.Merge => cacheType is CacheType.Status or CacheType.Branches or CacheType.BranchRelations,
                GitOperation.Rebase => cacheType is CacheType.Status or CacheType.Branches or CacheType.BranchRelations,
                GitOperation.Commit => cacheType is CacheType.Status,
                GitOperation.Push => cacheType is CacheType.Branches or CacheType.BranchRelations,
                GitOperation.Pull => cacheType is CacheType.Branches or CacheType.Status or CacheType.BranchRelations,
                GitOperation.Fetch => cacheType is CacheType.Branches or CacheType.Tags or CacheType.BranchRelations,
                GitOperation.ConfigChange => cacheType is CacheType.Config or CacheType.GitFlowConfig,
                GitOperation.GitFlowStart => cacheType is CacheType.Branches or CacheType.GitFlowConfig or CacheType.BranchRelations,
                GitOperation.GitFlowFinish => cacheType is CacheType.Branches or CacheType.GitFlowConfig or CacheType.BranchRelations,
                GitOperation.TagCreate => cacheType is CacheType.Tags,
                GitOperation.TagDelete => cacheType is CacheType.Tags,
                _ => false
            };
        }

        private void CleanupExpiredEntries(object state)
        {
            var now = DateTime.UtcNow;
            var keysToRemove = new List<string>();
            
            // Create snapshot to avoid enumeration during modification
            var snapshot = _cache.ToArray();
            
            foreach (var kvp in snapshot)
            {
                if (kvp.Value.ExpiresAt <= now)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }
            
            // Also cleanup if cache is too large (>100MB estimated)
            var stats = GetStatistics();
            if (stats.EstimatedMemoryMB > 100)
            {
                // Remove least recently used entries using snapshot
                var sortedEntries = snapshot.ToList();
                sortedEntries.Sort((a, b) => a.Value.HitCount.CompareTo(b.Value.HitCount));
                
                // Remove bottom 25% by hit count
                var removeCount = sortedEntries.Count / 4;
                for (int i = 0; i < removeCount; i++)
                {
                    _cache.TryRemove(sortedEntries[i].Key, out _);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
                
            _disposed = true;
            _cleanupTimer?.Dispose();
            _cache.Clear();
            
            lock (_lock)
            {
                if (_instance == this)
                    _instance = null;
            }
        }

        public class CacheStatistics
        {
            public int TotalEntries { get; set; }
            public Dictionary<CacheType, int> EntriesByType { get; set; }
            public int TotalHits { get; set; }
            public double EstimatedMemoryMB { get; set; }
        }
    }

    public enum GitOperation
    {
        Checkout,
        BranchCreate,
        BranchDelete,
        Merge,
        Rebase,
        Commit,
        Push,
        Pull,
        Fetch,
        ConfigChange,
        GitFlowStart,
        GitFlowFinish,
        TagCreate,
        TagDelete
    }
}