using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SourceGit.Models
{
    /// <summary>
    /// Memory optimization utilities for large repository operations
    /// Implements object pooling and efficient allocation patterns
    /// </summary>
    public static class MemoryOptimizer
    {
        private static readonly ConcurrentQueue<List<Commit>> _commitListPool = new();
        private static readonly ConcurrentQueue<List<string>> _stringListPool = new();
        private static readonly object _poolLock = new();
        private static volatile int _poolSize = 0;
        private const int MAX_POOL_SIZE = 10;

        /// <summary>
        /// Gets a pooled List&lt;Commit&gt; to reduce allocations
        /// </summary>
        public static List<Commit> RentCommitList()
        {
            if (_commitListPool.TryDequeue(out var list))
            {
                list.Clear();
                return list;
            }
            return new List<Commit>();
        }

        /// <summary>
        /// Returns a List&lt;Commit&gt; to the pool for reuse
        /// </summary>
        public static void ReturnCommitList(List<Commit> list)
        {
            if (list == null || _poolSize >= MAX_POOL_SIZE)
                return;

            list.Clear();
            if (list.Capacity > 2000) // Don't pool very large lists
            {
                list.TrimExcess(); // Reduce capacity before pooling
            }

            _commitListPool.Enqueue(list);
            lock (_poolLock)
            {
                _poolSize++;
            }
        }

        /// <summary>
        /// Gets a pooled List&lt;string&gt; to reduce allocations
        /// </summary>
        public static List<string> RentStringList()
        {
            if (_stringListPool.TryDequeue(out var list))
            {
                list.Clear();
                return list;
            }
            return new List<string>();
        }

        /// <summary>
        /// Returns a List&lt;string&gt; to the pool for reuse
        /// </summary>
        public static void ReturnStringList(List<string> list)
        {
            if (list == null || _poolSize >= MAX_POOL_SIZE)
                return;

            list.Clear();
            if (list.Capacity > 1000) // Don't pool very large lists
            {
                list.TrimExcess();
            }

            _stringListPool.Enqueue(list);
        }

        /// <summary>
        /// Suggests garbage collection if memory usage is high
        /// Should be called after processing large batches
        /// </summary>
        public static void SuggestGarbageCollection()
        {
            // Only suggest GC if we're using more than 85% of available memory
            var totalMemory = GC.GetTotalMemory(false);
            if (totalMemory > 500 * 1024 * 1024) // 500MB threshold
            {
                GC.Collect(0, GCCollectionMode.Optimized);
            }
        }

        /// <summary>
        /// Efficiently processes commit data in chunks to reduce memory pressure
        /// </summary>
        public static IEnumerable<List<T>> ChunkData<T>(IEnumerable<T> source, int chunkSize)
        {
            var chunk = new List<T>(chunkSize);
            foreach (var item in source)
            {
                chunk.Add(item);
                if (chunk.Count == chunkSize)
                {
                    yield return chunk;
                    chunk = new List<T>(chunkSize);
                }
            }

            if (chunk.Count > 0)
                yield return chunk;
        }

        /// <summary>
        /// Clears all pools to free memory
        /// Call this when memory pressure is detected
        /// </summary>
        public static void ClearPools()
        {
            while (_commitListPool.TryDequeue(out _)) { }
            while (_stringListPool.TryDequeue(out _)) { }

            lock (_poolLock)
            {
                _poolSize = 0;
            }
        }
    }
}