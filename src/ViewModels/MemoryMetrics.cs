using System;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels
{
    public class MemoryMetrics : ObservableObject, IDisposable
    {
        public long WorkingSetMB
        {
            get => _workingSetMB;
            private set => SetProperty(ref _workingSetMB, value);
        }

        public long ManagedMemoryMB
        {
            get => _managedMemoryMB;
            private set => SetProperty(ref _managedMemoryMB, value);
        }

        public int UserCacheCount
        {
            get => _userCacheCount;
            private set => SetProperty(ref _userCacheCount, value);
        }

        public int PerformanceMetricsCount
        {
            get => _performanceMetricsCount;
            private set => SetProperty(ref _performanceMetricsCount, value);
        }

        public string MemoryDisplay
        {
            get => _memoryDisplay;
            private set => SetProperty(ref _memoryDisplay, value);
        }

        public string CacheDisplay
        {
            get => _cacheDisplay;
            private set => SetProperty(ref _cacheDisplay, value);
        }

        public MemoryMetrics()
        {
            // Initial update
            UpdateMetrics();

            // Set up timer for periodic updates (every 5 seconds)
            _updateTimer = new Timer(OnTimerCallback, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        private void OnTimerCallback(object state)
        {
            UpdateMetrics();
        }

        private void UpdateMetrics()
        {
            try
            {
                // Get memory statistics
                var stats = Models.MemoryManager.GetStatistics();
                WorkingSetMB = stats.WorkingSetMB;
                ManagedMemoryMB = stats.ManagedMemoryMB;

                // Get cache counts
                UserCacheCount = Models.User.GetCacheSize();
                PerformanceMetricsCount = Models.PerformanceMonitor.GetMeasurementCount();

                // Update display strings
                MemoryDisplay = $"{WorkingSetMB}/{ManagedMemoryMB} MB";
                CacheDisplay = $"U:{UserCacheCount} P:{PerformanceMetricsCount}";
            }
            catch
            {
                // Silently ignore errors in metrics collection
                MemoryDisplay = "-- MB";
                CacheDisplay = "--";
            }
        }

        public void Dispose()
        {
            _updateTimer?.Dispose();
            _updateTimer = null;
        }

        private Timer _updateTimer;
        private long _workingSetMB;
        private long _managedMemoryMB;
        private int _userCacheCount;
        private int _performanceMetricsCount;
        private string _memoryDisplay = "-- MB";
        private string _cacheDisplay = "--";
    }
}
