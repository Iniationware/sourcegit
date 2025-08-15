using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace SourceGit.Models
{
    /// <summary>
    /// Central memory management for preventing leaks and monitoring usage
    /// </summary>
    public static class MemoryManager
    {
        private static readonly Timer _cleanupTimer;
        private static readonly List<WeakReference> _trackedObjects = new();
        private static readonly object _lock = new();
        private static long _lastCleanupTime = 0;
        private static bool _isEnabled = true;

        static MemoryManager()
        {
            // Run cleanup every 5 minutes
            _cleanupTimer = new Timer(CleanupCallback, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
        }

        public static bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// Register an object for tracking
        /// </summary>
        public static void TrackObject(object obj)
        {
            if (!_isEnabled || obj == null) return;

            lock (_lock)
            {
                _trackedObjects.Add(new WeakReference(obj));
            }
        }

        /// <summary>
        /// Force a memory cleanup
        /// </summary>
        public static void ForceCleanup()
        {
            CleanupInternal();
            
            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        /// <summary>
        /// Get memory usage statistics
        /// </summary>
        public static MemoryStatistics GetStatistics()
        {
            var stats = new MemoryStatistics();
            
            // Get process memory info
            using (var process = Process.GetCurrentProcess())
            {
                stats.WorkingSetMB = process.WorkingSet64 / (1024 * 1024);
                stats.PrivateMemoryMB = process.PrivateMemorySize64 / (1024 * 1024);
                stats.ManagedMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
            }

            // Count tracked objects
            lock (_lock)
            {
                stats.TotalTrackedObjects = _trackedObjects.Count;
                stats.AliveTrackedObjects = 0;

                foreach (var weakRef in _trackedObjects)
                {
                    if (weakRef.IsAlive)
                        stats.AliveTrackedObjects++;
                }
            }

            stats.LastCleanupTime = new DateTime(_lastCleanupTime);
            
            return stats;
        }

        /// <summary>
        /// Check if memory usage is high
        /// </summary>
        public static bool IsMemoryPressureHigh()
        {
            var stats = GetStatistics();
            
            // Consider memory pressure high if:
            // - Working set > 500MB
            // - Or managed memory > 300MB
            return stats.WorkingSetMB > 500 || stats.ManagedMemoryMB > 300;
        }

        private static void CleanupCallback(object state)
        {
            if (!_isEnabled) return;
            CleanupInternal();
        }

        private static void CleanupInternal()
        {
            // Clean up tracked objects
            lock (_lock)
            {
                _trackedObjects.RemoveAll(wr => !wr.IsAlive);
            }

            // Clean up various caches
            CleanupUserCache();
            CleanupPerformanceMonitor();
            
            _lastCleanupTime = DateTime.Now.Ticks;

            // If memory pressure is high, force GC
            if (IsMemoryPressureHigh())
            {
                GC.Collect(2, GCCollectionMode.Aggressive);
            }
        }

        private static void CleanupUserCache()
        {
            // User cache cleanup will be handled in User.cs
            User.CleanupCache();
        }

        private static void CleanupPerformanceMonitor()
        {
            // Keep only recent performance data
            var oldCount = PerformanceMonitor.GetMeasurementCount();
            if (oldCount > 1000)
            {
                PerformanceMonitor.TrimOldMeasurements();
            }
        }

        public static void Shutdown()
        {
            _cleanupTimer?.Dispose();
            
            lock (_lock)
            {
                _trackedObjects.Clear();
            }
        }
    }

    public class MemoryStatistics
    {
        public long WorkingSetMB { get; set; }
        public long PrivateMemoryMB { get; set; }
        public long ManagedMemoryMB { get; set; }
        public int TotalTrackedObjects { get; set; }
        public int AliveTrackedObjects { get; set; }
        public DateTime LastCleanupTime { get; set; }

        public override string ToString()
        {
            return $"Memory: Working={WorkingSetMB}MB, Private={PrivateMemoryMB}MB, Managed={ManagedMemoryMB}MB, " +
                   $"Tracked={AliveTrackedObjects}/{TotalTrackedObjects}, LastCleanup={LastCleanupTime:HH:mm:ss}";
        }
    }
}