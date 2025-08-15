using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace SourceGit.Models
{
    /// <summary>
    /// Performance monitoring for measuring operation times and optimization effectiveness
    /// </summary>
    public static class PerformanceMonitor
    {
        private static readonly Dictionary<string, Stopwatch> _activeTimers = new();
        private static readonly Dictionary<string, List<long>> _measurements = new();
        private static readonly Lock _lock = new();
        private static bool _enabled = true;

        public static bool IsEnabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>
        /// Start timing an operation
        /// </summary>
        public static void StartTimer(string operation)
        {
            if (!_enabled) return;

            lock (_lock)
            {
                if (_activeTimers.TryGetValue(operation, out var existingTimer))
                {
                    existingTimer.Restart();
                }
                else
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    _activeTimers[operation] = stopwatch;
                }
            }
        }

        /// <summary>
        /// Stop timing an operation and record the measurement
        /// </summary>
        public static long StopTimer(string operation)
        {
            if (!_enabled) return 0;

            lock (_lock)
            {
                if (_activeTimers.TryGetValue(operation, out var stopwatch))
                {
                    stopwatch.Stop();
                    var elapsed = stopwatch.ElapsedMilliseconds;
                    
                    // Record measurement
                    if (!_measurements.ContainsKey(operation))
                        _measurements[operation] = new List<long>();
                    
                    _measurements[operation].Add(elapsed);
                    
                    // Keep only last 100 measurements per operation
                    if (_measurements[operation].Count > 100)
                        _measurements[operation].RemoveAt(0);
                    
                    _activeTimers.Remove(operation);
                    
                    // Log if operation took too long
                    if (elapsed > 1000)
                    {
                        LogPerformance($"[SLOW] {operation} took {elapsed}ms");
                    }
                    else if (elapsed > 500)
                    {
                        LogPerformance($"[WARN] {operation} took {elapsed}ms");
                    }
                    
                    return elapsed;
                }
            }
            
            return 0;
        }

        /// <summary>
        /// Get average time for an operation
        /// </summary>
        public static double GetAverageTime(string operation)
        {
            lock (_lock)
            {
                if (_measurements.TryGetValue(operation, out var times) && times.Count > 0)
                {
                    long sum = 0;
                    foreach (var time in times)
                        sum += time;
                    return (double)sum / times.Count;
                }
            }
            return 0;
        }

        /// <summary>
        /// Get performance summary for all operations
        /// </summary>
        public static string GetPerformanceSummary()
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("=== Performance Summary ===");
            
            lock (_lock)
            {
                foreach (var kvp in _measurements)
                {
                    if (kvp.Value.Count > 0)
                    {
                        var avg = GetAverageTime(kvp.Key);
                        var min = long.MaxValue;
                        var max = long.MinValue;
                        
                        foreach (var time in kvp.Value)
                        {
                            if (time < min) min = time;
                            if (time > max) max = time;
                        }
                        
                        summary.AppendLine($"{kvp.Key}: Avg={avg:F0}ms, Min={min}ms, Max={max}ms, Count={kvp.Value.Count}");
                    }
                }
            }
            
            return summary.ToString();
        }

        /// <summary>
        /// Clear all measurements
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _activeTimers.Clear();
                _measurements.Clear();
            }
        }
        
        /// <summary>
        /// Trim old measurements to prevent unbounded growth
        /// </summary>
        public static void TrimOldMeasurements()
        {
            lock (_lock)
            {
                foreach (var kvp in _measurements)
                {
                    // Keep only last 50 measurements instead of 100
                    while (kvp.Value.Count > 50)
                    {
                        kvp.Value.RemoveAt(0);
                    }
                }
                
                // Remove operations that haven't been used recently
                var toRemove = new List<string>();
                foreach (var kvp in _measurements)
                {
                    if (kvp.Value.Count == 0)
                        toRemove.Add(kvp.Key);
                }
                
                foreach (var key in toRemove)
                {
                    _measurements.Remove(key);
                }
            }
        }
        
        /// <summary>
        /// Get total measurement count for monitoring
        /// </summary>
        public static int GetMeasurementCount()
        {
            lock (_lock)
            {
                int total = 0;
                foreach (var kvp in _measurements)
                {
                    total += kvp.Value.Count;
                }
                return total;
            }
        }

        private static void LogPerformance(string message)
        {
            // Log to debug output and potentially to a file
            Debug.WriteLine($"[PERF] {DateTime.Now:HH:mm:ss.fff} {message}");
            
            // TODO: Could also log to a file for analysis
            // File.AppendAllText("performance.log", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}\n");
        }
    }
}