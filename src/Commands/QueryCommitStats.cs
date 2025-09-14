using System;
using System.Collections.Generic;

namespace SourceGit.Commands
{
    /// <summary>
    /// Query commit statistics for different time periods
    /// </summary>
    public class QueryCommitStats : Command
    {
        public class CommitStats
        {
            public int TotalCommits { get; set; }
            public Dictionary<string, int> CommitsByAuthor { get; set; } = new Dictionary<string, int>();
            public Dictionary<int, int> CommitsByHour { get; set; } = new Dictionary<int, int>();
            public Dictionary<string, int> CommitsByBranch { get; set; } = new Dictionary<string, int>();
            public DateTime Since { get; set; }
            public DateTime Until { get; set; }
        }

        private readonly DateTime _since;
        private readonly DateTime _until;

        public QueryCommitStats(string repo, DateTime since, DateTime until = default)
        {
            WorkingDirectory = repo;
            Context = repo;
            _since = since;
            _until = until == default ? DateTime.Now : until;

            // Query commits with author and date
            Args = $"log --since=\"{_since:yyyy-MM-dd HH:mm:ss}\" --until=\"{_until:yyyy-MM-dd HH:mm:ss}\" --format=\"%aN|%aI\" --all";
        }

        public new CommitStats Result()
        {
            var stats = new CommitStats
            {
                Since = _since,
                Until = _until
            };

            var output = ReadToEnd();
            if (output.IsSuccess && !string.IsNullOrWhiteSpace(output.StdOut))
            {
                var lines = output.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                stats.TotalCommits = lines.Length;

                // Initialize hour buckets
                for (int i = 0; i < 24; i++)
                {
                    stats.CommitsByHour[i] = 0;
                }

                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 2)
                    {
                        // Count by author
                        var author = parts[0].Trim();
                        if (!string.IsNullOrEmpty(author))
                        {
                            if (!stats.CommitsByAuthor.ContainsKey(author))
                                stats.CommitsByAuthor[author] = 0;
                            stats.CommitsByAuthor[author]++;
                        }

                        // Count by hour
                        if (DateTime.TryParse(parts[1], out var commitDate))
                        {
                            stats.CommitsByHour[commitDate.Hour]++;
                        }
                    }
                }
            }

            // Get branch statistics separately
            QueryBranchStats(stats);

            return stats;
        }

        private void QueryBranchStats(CommitStats stats)
        {
            // Query commits per branch
            var branchCmd = new QueryCommitStats(WorkingDirectory, _since, _until)
            {
                Args = $"log --since=\"{_since:yyyy-MM-dd HH:mm:ss}\" --until=\"{_until:yyyy-MM-dd HH:mm:ss}\" --format=\"%H\" --all"
            };

            var branchOutput = branchCmd.ReadToEnd();
            if (branchOutput.IsSuccess && !string.IsNullOrWhiteSpace(branchOutput.StdOut))
            {
                var commits = branchOutput.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var commit in commits)
                {
                    // Find which branches contain this commit
                    var containsCmd = new QueryCommitStats(WorkingDirectory, _since, _until)
                    {
                        Args = $"branch --contains {commit.Trim()} --format=\"%(refname:short)\""
                    };

                    var containsOutput = containsCmd.ReadToEnd();
                    if (containsOutput.IsSuccess && !string.IsNullOrWhiteSpace(containsOutput.StdOut))
                    {
                        var branches = containsOutput.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var branch in branches)
                        {
                            var branchName = branch.Trim();
                            if (!string.IsNullOrEmpty(branchName))
                            {
                                if (!stats.CommitsByBranch.ContainsKey(branchName))
                                    stats.CommitsByBranch[branchName] = 0;
                                stats.CommitsByBranch[branchName]++;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get stats for today
        /// </summary>
        public static CommitStats GetTodayStats(string repo)
        {
            var today = DateTime.Today;
            var cmd = new QueryCommitStats(repo, today);
            return cmd.Result();
        }

        /// <summary>
        /// Get stats for this week
        /// </summary>
        public static CommitStats GetWeekStats(string repo)
        {
            var today = DateTime.Today;
            var dayOfWeek = (int)today.DayOfWeek;
            var weekStart = today.AddDays(-dayOfWeek);
            var cmd = new QueryCommitStats(repo, weekStart);
            return cmd.Result();
        }

        /// <summary>
        /// Get stats for this month
        /// </summary>
        public static CommitStats GetMonthStats(string repo)
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var cmd = new QueryCommitStats(repo, monthStart);
            return cmd.Result();
        }
    }
}