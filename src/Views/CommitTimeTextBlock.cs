using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;

namespace SourceGit.Views
{
    public class CommitTimeTextBlock : TextBlock
    {
        public static readonly StyledProperty<bool> ShowAsDateTimeProperty =
            AvaloniaProperty.Register<CommitTimeTextBlock, bool>(nameof(ShowAsDateTime), true);

        public bool ShowAsDateTime
        {
            get => GetValue(ShowAsDateTimeProperty);
            set => SetValue(ShowAsDateTimeProperty, value);
        }

        public static readonly StyledProperty<int> DateTimeFormatProperty =
            AvaloniaProperty.Register<CommitTimeTextBlock, int>(nameof(DateTimeFormat));

        public int DateTimeFormat
        {
            get => GetValue(DateTimeFormatProperty);
            set => SetValue(DateTimeFormatProperty, value);
        }

        public static readonly StyledProperty<bool> UseAuthorTimeProperty =
            AvaloniaProperty.Register<CommitTimeTextBlock, bool>(nameof(UseAuthorTime), true);

        public bool UseAuthorTime
        {
            get => GetValue(UseAuthorTimeProperty);
            set => SetValue(UseAuthorTimeProperty, value);
        }

        protected override Type StyleKeyOverride => typeof(TextBlock);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == UseAuthorTimeProperty)
            {
                SetCurrentValue(TextProperty, GetDisplayText());
            }
            else if (change.Property == ShowAsDateTimeProperty)
            {
                SetCurrentValue(TextProperty, GetDisplayText());

                if (ShowAsDateTime)
                {
                    StopTimer();
                    HorizontalAlignment = HorizontalAlignment.Left;
                }
                else
                {
                    StartTimer();
                    HorizontalAlignment = HorizontalAlignment.Center;
                }
            }
            else if (change.Property == DateTimeFormatProperty)
            {
                if (ShowAsDateTime)
                    SetCurrentValue(TextProperty, GetDisplayText());
            }
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            if (!ShowAsDateTime)
                StartTimer();
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            StopTimer();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            SetCurrentValue(TextProperty, GetDisplayText());
        }

        private void StartTimer()
        {
            if (_refreshTimer != null)
                return;

            // Use adaptive timer interval based on power mode
            var interval = CalculateAdaptiveInterval();

            _refreshTimer = DispatcherTimer.Run(() =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    var text = GetDisplayText();
                    if (!text.Equals(Text, StringComparison.Ordinal))
                        Text = text;
                });

                return true;
            }, interval);
        }

        private void StopTimer()
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Dispose();
                _refreshTimer = null;
            }
        }

        private string GetDisplayText()
        {
            if (DataContext is not Models.Commit commit)
                return string.Empty;

            if (ShowAsDateTime)
                return UseAuthorTime ? commit.AuthorTimeStr : commit.CommitterTimeStr;

            var timestamp = UseAuthorTime ? commit.AuthorTime : commit.CommitterTime;
            var now = DateTime.Now;
            var localTime = DateTime.UnixEpoch.AddSeconds(timestamp).ToLocalTime();
            var span = now - localTime;
            if (span.TotalMinutes < 1)
                return App.Text("Period.JustNow");

            if (span.TotalHours < 1)
            {
                var minutes = (int)span.TotalMinutes;
                // Group into 5-minute buckets to reduce update frequency
                var bucket = (minutes / 5) * 5;
                if (bucket == 0)
                    return App.Text("Period.JustNow");
                return App.Text("Period.MinutesAgo", bucket);
            }

            if (span.TotalDays < 1)
            {
                var hours = (int)span.TotalHours;
                return hours == 1 ? App.Text("Period.HourAgo") : App.Text("Period.HoursAgo", hours);
            }

            var lastDay = now.AddDays(-1).Date;
            if (localTime >= lastDay)
                return App.Text("Period.Yesterday");

            if ((localTime.Year == now.Year && localTime.Month == now.Month) || span.TotalDays < 28)
            {
                var diffDay = now.Date - localTime.Date;
                return App.Text("Period.DaysAgo", (int)diffDay.TotalDays);
            }

            var lastMonth = now.AddMonths(-1).Date;
            if (localTime.Year == lastMonth.Year && localTime.Month == lastMonth.Month)
                return App.Text("Period.LastMonth");

            if (localTime.Year == now.Year || localTime > now.AddMonths(-11))
            {
                var diffMonth = (12 + now.Month - localTime.Month) % 12;
                return App.Text("Period.MonthsAgo", diffMonth);
            }

            var diffYear = now.Year - localTime.Year;
            if (diffYear == 1)
                return App.Text("Period.LastYear");

            return App.Text("Period.YearsAgo", diffYear);
        }

        private TimeSpan CalculateAdaptiveInterval()
        {
            // If on battery, use longer intervals
            if (Models.PowerManagement.IsOnBattery)
                return TimeSpan.FromMinutes(5);

            if (DataContext is not Models.Commit commit)
                return TimeSpan.FromMinutes(2);

            var timestamp = UseAuthorTime ? commit.AuthorTime : commit.CommitterTime;
            var localTime = DateTime.UnixEpoch.AddSeconds(timestamp).ToLocalTime();
            var age = DateTime.Now - localTime;

            // Adaptive intervals based on commit age and power mode
            if (age.TotalHours < 1)
                return TimeSpan.FromSeconds(Models.PowerManagement.RefreshIntervals.CommitTimeUpdateInterval);
            if (age.TotalDays < 1)
                return TimeSpan.FromMinutes(2);
            if (age.TotalDays < 7)
                return TimeSpan.FromMinutes(10);

            return TimeSpan.FromMinutes(30); // Very old commits update rarely
        }

        private IDisposable _refreshTimer = null;
    }
}
