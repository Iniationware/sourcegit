using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SourceGit.Models
{
    /// <summary>
    /// Thread-safe LRU (Least Recently Used) cache implementation with memory pressure detection
    /// </summary>
    /// <typeparam name="TKey">The type of cache keys</typeparam>
    /// <typeparam name="TValue">The type of cached values</typeparam>
    public class LRUCache<TKey, TValue> where TValue : class
    {
        private class CacheItem
        {
            public TValue Value { get; set; }
            public LinkedListNode<TKey> Node { get; set; }
            public long Size { get; set; }
            public DateTime LastAccessed { get; set; }
        }

        private readonly Dictionary<TKey, CacheItem> _cache;
        private readonly LinkedList<TKey> _lruList;
        private readonly object _lock = new object();
        private readonly int _maxCapacity;
        private readonly long _maxMemoryBytes;
        private long _currentMemoryUsage;
        private readonly Func<TValue, long> _sizeCalculator;

        /// <summary>
        /// Creates a new LRU cache instance
        /// </summary>
        /// <param name="maxCapacity">Maximum number of items to cache</param>
        /// <param name="maxMemoryMB">Maximum memory usage in megabytes</param>
        /// <param name="sizeCalculator">Optional function to calculate item size in bytes</param>
        public LRUCache(int maxCapacity = 100, long maxMemoryMB = 100, Func<TValue, long> sizeCalculator = null)
        {
            _maxCapacity = maxCapacity;
            _maxMemoryBytes = maxMemoryMB * 1024 * 1024; // Convert MB to bytes
            _cache = new Dictionary<TKey, CacheItem>(maxCapacity);
            _lruList = new LinkedList<TKey>();
            _sizeCalculator = sizeCalculator ?? EstimateObjectSize;
            _currentMemoryUsage = 0;
        }

        /// <summary>
        /// Gets the current number of items in the cache
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _cache.Count;
                }
            }
        }

        /// <summary>
        /// Gets the current memory usage in bytes
        /// </summary>
        public long MemoryUsage
        {
            get
            {
                lock (_lock)
                {
                    return _currentMemoryUsage;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value in the cache
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue Get(TKey key)
        {
            if (key == null)
                return null;

            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var item))
                {
                    // Move to front (most recently used)
                    _lruList.Remove(item.Node);
                    _lruList.AddFirst(item.Node);
                    item.LastAccessed = DateTime.UtcNow;
                    return item.Value;
                }
                return null;
            }
        }

        /// <summary>
        /// Adds or updates a value in the cache
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(TKey key, TValue value)
        {
            if (key == null || value == null)
                return;

            lock (_lock)
            {
                var itemSize = _sizeCalculator(value);

                // Check memory pressure before adding
                if (ShouldEvictDueToMemoryPressure(itemSize))
                {
                    EvictLeastRecentlyUsed();
                }

                if (_cache.TryGetValue(key, out var existingItem))
                {
                    // Update existing item
                    _currentMemoryUsage -= existingItem.Size;
                    _currentMemoryUsage += itemSize;

                    existingItem.Value = value;
                    existingItem.Size = itemSize;
                    existingItem.LastAccessed = DateTime.UtcNow;

                    // Move to front
                    _lruList.Remove(existingItem.Node);
                    _lruList.AddFirst(existingItem.Node);
                }
                else
                {
                    // Add new item
                    while (_cache.Count >= _maxCapacity || _currentMemoryUsage + itemSize > _maxMemoryBytes)
                    {
                        EvictLeastRecentlyUsed();
                    }

                    var node = _lruList.AddFirst(key);
                    _cache[key] = new CacheItem
                    {
                        Value = value,
                        Node = node,
                        Size = itemSize,
                        LastAccessed = DateTime.UtcNow
                    };
                    _currentMemoryUsage += itemSize;
                }
            }
        }

        /// <summary>
        /// Removes a value from the cache
        /// </summary>
        public bool Remove(TKey key)
        {
            if (key == null)
                return false;

            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var item))
                {
                    _lruList.Remove(item.Node);
                    _cache.Remove(key);
                    _currentMemoryUsage -= item.Size;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Clears all items from the cache
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _cache.Clear();
                _lruList.Clear();
                _currentMemoryUsage = 0;
            }
        }

        /// <summary>
        /// Checks if the cache contains a key
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            if (key == null)
                return false;

            lock (_lock)
            {
                return _cache.ContainsKey(key);
            }
        }

        /// <summary>
        /// Trims the cache based on memory pressure
        /// </summary>
        public void TrimExcess()
        {
            lock (_lock)
            {
                var memoryInfo = GC.GetTotalMemory(false);
                var memoryPressure = memoryInfo > (_maxMemoryBytes * 2);

                if (memoryPressure)
                {
                    // Remove items until we're at 50% capacity
                    var targetCount = _maxCapacity / 2;
                    while (_cache.Count > targetCount)
                    {
                        EvictLeastRecentlyUsed();
                    }
                }
            }
        }

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            lock (_lock)
            {
                return new CacheStatistics
                {
                    ItemCount = _cache.Count,
                    MaxCapacity = _maxCapacity,
                    MemoryUsageBytes = _currentMemoryUsage,
                    MaxMemoryBytes = _maxMemoryBytes,
                    MemoryUsagePercent = (_currentMemoryUsage * 100.0) / _maxMemoryBytes
                };
            }
        }

        private void EvictLeastRecentlyUsed()
        {
            if (_lruList.Last != null)
            {
                var key = _lruList.Last.Value;
                if (_cache.TryGetValue(key, out var item))
                {
                    _currentMemoryUsage -= item.Size;
                    _cache.Remove(key);
                    _lruList.RemoveLast();
                }
            }
        }

        private bool ShouldEvictDueToMemoryPressure(long newItemSize)
        {
            // Check system memory pressure
            var totalMemory = GC.GetTotalMemory(false);
            var gen2Collections = GC.CollectionCount(2);

            // Evict if:
            // - Adding item would exceed max memory
            // - System is under memory pressure (frequent Gen2 GCs)
            // - Total process memory is too high
            return (_currentMemoryUsage + newItemSize > _maxMemoryBytes) ||
                   (gen2Collections > 10 && totalMemory > _maxMemoryBytes * 3) ||
                   (totalMemory > 500_000_000); // 500MB total process memory
        }

        private long EstimateObjectSize(TValue value)
        {
            // Basic estimation - override with sizeCalculator for more accuracy
            if (value is CommitGraph graph)
            {
                // Estimate based on graph components
                return (graph.Paths?.Count ?? 0) * 200 +    // ~200 bytes per path
                       (graph.Links?.Count ?? 0) * 100 +    // ~100 bytes per link
                       (graph.Dots?.Count ?? 0) * 50 +      // ~50 bytes per dot
                       1024;                                 // Base overhead
            }

            // Default estimate for unknown types
            return 1024; // 1KB default
        }

        public class CacheStatistics
        {
            public int ItemCount { get; set; }
            public int MaxCapacity { get; set; }
            public long MemoryUsageBytes { get; set; }
            public long MaxMemoryBytes { get; set; }
            public double MemoryUsagePercent { get; set; }
        }
    }
}
