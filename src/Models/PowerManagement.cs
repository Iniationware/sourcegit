using System;
using System.Runtime.InteropServices;

namespace SourceGit.Models
{
    public static class PowerManagement
    {
        public enum PowerMode
        {
            HighPerformance,
            Balanced,
            PowerSaver
        }

        private static PowerMode _currentMode = PowerMode.Balanced;
        private static bool _isOnBattery = false;
        private static DateTime _lastBatteryCheck = DateTime.MinValue;

        public static PowerMode CurrentMode => _currentMode;
        public static bool IsOnBattery => _isOnBattery;

        public static void Initialize()
        {
            UpdatePowerState();
        }

        public static void UpdatePowerState()
        {
            // Check at most once per minute
            if (DateTime.Now - _lastBatteryCheck < TimeSpan.FromMinutes(1))
                return;

            _lastBatteryCheck = DateTime.Now;
            _isOnBattery = CheckIfOnBattery();

            // Automatically switch to power saver mode when on battery
            if (_isOnBattery)
            {
                SetPowerMode(PowerMode.PowerSaver);
            }
            else
            {
                SetPowerMode(PowerMode.Balanced);
            }
        }

        public static void SetPowerMode(PowerMode mode)
        {
            if (_currentMode == mode)
                return;

            _currentMode = mode;
            ApplyPowerSettings();
        }

        private static void ApplyPowerSettings()
        {
            switch (_currentMode)
            {
                case PowerMode.PowerSaver:
                    // Reduce refresh rates and parallel operations
                    RefreshIntervals.FileWatcherInterval = 1000; // 1 second
                    RefreshIntervals.CommitTimeUpdateInterval = 300; // 5 minutes
                    RefreshIntervals.EventDebounceDelay = 1000; // 1 second
                    RefreshIntervals.MaxParallelOperations = 1;
                    RefreshIntervals.EnableBackgroundRefresh = false;
                    break;

                case PowerMode.Balanced:
                    // Balanced settings
                    RefreshIntervals.FileWatcherInterval = 500; // 500ms
                    RefreshIntervals.CommitTimeUpdateInterval = 60; // 1 minute
                    RefreshIntervals.EventDebounceDelay = 500; // 500ms
                    RefreshIntervals.MaxParallelOperations = Math.Max(2, Environment.ProcessorCount / 2);
                    RefreshIntervals.EnableBackgroundRefresh = true;
                    break;

                case PowerMode.HighPerformance:
                    // Maximum performance (original settings)
                    RefreshIntervals.FileWatcherInterval = 100; // 100ms
                    RefreshIntervals.CommitTimeUpdateInterval = 10; // 10 seconds
                    RefreshIntervals.EventDebounceDelay = 200; // 200ms
                    RefreshIntervals.MaxParallelOperations = Environment.ProcessorCount;
                    RefreshIntervals.EnableBackgroundRefresh = true;
                    break;
            }
        }

        private static bool CheckIfOnBattery()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return CheckWindowsBattery();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return CheckMacOSBattery();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return CheckLinuxBattery();
            }

            return false; // Default to AC power if unknown
        }

        private static bool CheckWindowsBattery()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "wmic",
                    Arguments = "path Win32_Battery get BatteryStatus /value",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(psi);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // BatteryStatus=2 means AC powered, 1 means battery
                    return output.Contains("BatteryStatus=1");
                }
            }
            catch { }

            return false;
        }

        private static bool CheckMacOSBattery()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "pmset",
                    Arguments = "-g ps",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(psi);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Check if running on battery
                    return output.Contains("Battery Power") ||
                           (!output.Contains("AC Power") && output.Contains("discharging"));
                }
            }
            catch { }

            return false;
        }

        private static bool CheckLinuxBattery()
        {
            try
            {
                var batteryPath = "/sys/class/power_supply/BAT0/status";
                if (System.IO.File.Exists(batteryPath))
                {
                    var status = System.IO.File.ReadAllText(batteryPath).Trim();
                    return status == "Discharging";
                }

                // Try alternative path
                batteryPath = "/sys/class/power_supply/BAT1/status";
                if (System.IO.File.Exists(batteryPath))
                {
                    var status = System.IO.File.ReadAllText(batteryPath).Trim();
                    return status == "Discharging";
                }
            }
            catch { }

            return false;
        }

        public static class RefreshIntervals
        {
            public static int FileWatcherInterval { get; set; } = 500;
            public static int CommitTimeUpdateInterval { get; set; } = 60;
            public static int EventDebounceDelay { get; set; } = 500;
            public static int MaxParallelOperations { get; set; } = 2;
            public static bool EnableBackgroundRefresh { get; set; } = true;
        }
    }
}
