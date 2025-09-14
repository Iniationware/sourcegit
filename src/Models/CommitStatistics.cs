using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.Models
{
    /// <summary>
    /// Commit statistics model with real-time updates
    /// </summary>
    public class CommitStatistics : ObservableObject
    {
        private int _todayCommits;
        private int _weekCommits;
        private int _monthCommits;
        private string _topAuthorToday;
        private string _topAuthorWeek;
        private string _topAuthorMonth;
        private int _peakHour;
        private string _mostActiveBranch;
        private DateTime _lastUpdated;
        private string _errorMessage;

        public int TodayCommits
        {
            get => _todayCommits;
            set => SetProperty(ref _todayCommits, value);
        }

        public int WeekCommits
        {
            get => _weekCommits;
            set => SetProperty(ref _weekCommits, value);
        }

        public int MonthCommits
        {
            get => _monthCommits;
            set => SetProperty(ref _monthCommits, value);
        }

        public string TopAuthorToday
        {
            get => _topAuthorToday;
            set => SetProperty(ref _topAuthorToday, value);
        }

        public string TopAuthorWeek
        {
            get => _topAuthorWeek;
            set => SetProperty(ref _topAuthorWeek, value);
        }

        public string TopAuthorMonth
        {
            get => _topAuthorMonth;
            set => SetProperty(ref _topAuthorMonth, value);
        }

        public int PeakHour
        {
            get => _peakHour;
            set => SetProperty(ref _peakHour, value);
        }

        public string MostActiveBranch
        {
            get => _mostActiveBranch;
            set => SetProperty(ref _mostActiveBranch, value);
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// Update statistics from repository
        /// </summary>
        public void UpdateFromRepository(string repoPath)
        {
            try
            {
                ErrorMessage = null;

                // Get today's stats
                var todayStats = Commands.QueryCommitStats.GetTodayStats(repoPath);
                if (todayStats != null)
                {
                    TodayCommits = todayStats.TotalCommits;
                    TopAuthorToday = GetTopAuthor(todayStats.CommitsByAuthor);

                    // Find peak hour from today's data
                    if (todayStats.CommitsByHour.Any())
                    {
                        PeakHour = todayStats.CommitsByHour
                            .OrderByDescending(kvp => kvp.Value)
                            .First().Key;
                    }
                }

                // Get week stats
                var weekStats = Commands.QueryCommitStats.GetWeekStats(repoPath);
                if (weekStats != null)
                {
                    WeekCommits = weekStats.TotalCommits;
                    TopAuthorWeek = GetTopAuthor(weekStats.CommitsByAuthor);

                    // Get most active branch from week data
                    if (weekStats.CommitsByBranch.Any())
                    {
                        MostActiveBranch = weekStats.CommitsByBranch
                            .OrderByDescending(kvp => kvp.Value)
                            .First().Key;
                    }
                }

                // Get month stats
                var monthStats = Commands.QueryCommitStats.GetMonthStats(repoPath);
                if (monthStats != null)
                {
                    MonthCommits = monthStats.TotalCommits;
                    TopAuthorMonth = GetTopAuthor(monthStats.CommitsByAuthor);
                }

                LastUpdated = DateTime.Now;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error updating statistics: {ex.Message}";
                ResetStats();
            }
        }

        private string GetTopAuthor(Dictionary<string, int> commitsByAuthor)
        {
            if (commitsByAuthor == null || !commitsByAuthor.Any())
                return "No commits";

            var topAuthor = commitsByAuthor
                .OrderByDescending(kvp => kvp.Value)
                .First();

            return $"{topAuthor.Key} ({topAuthor.Value})";
        }

        private void ResetStats()
        {
            TodayCommits = 0;
            WeekCommits = 0;
            MonthCommits = 0;
            TopAuthorToday = "N/A";
            TopAuthorWeek = "N/A";
            TopAuthorMonth = "N/A";
            PeakHour = 0;
            MostActiveBranch = "N/A";
        }

        /// <summary>
        /// Get a formatted summary of statistics
        /// </summary>
        public string GetSummary()
        {
            if (HasError)
                return ErrorMessage;

            return $"Today: {TodayCommits} | Week: {WeekCommits} | Month: {MonthCommits}";
        }

        /// <summary>
        /// Get activity level based on today's commits
        /// </summary>
        public string GetActivityLevel()
        {
            return TodayCommits switch
            {
                0 => "Quiet",
                <= 5 => "Normal",
                <= 15 => "Active",
                <= 30 => "Very Active",
                _ => "On Fire! 🔥"
            };
        }
    }
}
