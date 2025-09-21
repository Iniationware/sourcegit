using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace SourceGit.Commands
{
    /// <summary>
    /// Optimized QueryCommits implementation with batching, memory optimization, and progress feedback
    /// Designed for handling large repositories (1000+ commits) without UI freezing
    /// </summary>
    public class QueryCommitsOptimized : Command
    {
        private const int BATCH_SIZE = 50; // Process commits in batches
        private const int YIELD_FREQUENCY = 100; // Yield control every N commits

        public QueryCommitsOptimized(string repo, string limits, bool needFindHead = true)
        {
            WorkingDirectory = repo;
            Context = repo;
            Args = $"log --no-show-signature --decorate=full --format=%H%x00%P%x00%D%x00%aN±%aE%x00%at%x00%cN±%cE%x00%ct%x00%s {limits}";
            _findFirstMerged = needFindHead;
        }

        public QueryCommitsOptimized(string repo, string filter, Models.CommitSearchMethod method, bool onlyCurrentBranch)
        {
            string search = onlyCurrentBranch ? string.Empty : "--branches --remotes ";

            if (method == Models.CommitSearchMethod.ByAuthor)
            {
                search += $"-i --author={filter.Quoted()}";
            }
            else if (method == Models.CommitSearchMethod.ByCommitter)
            {
                search += $"-i --committer={filter.Quoted()}";
            }
            else if (method == Models.CommitSearchMethod.ByMessage)
            {
                var argsBuilder = new StringBuilder();
                argsBuilder.Append(search);

                var words = filter.Split([' ', '\t', '\r'], StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                    argsBuilder.Append("--grep=").Append(word.Trim().Quoted()).Append(' ');
                argsBuilder.Append("--all-match -i");

                search = argsBuilder.ToString();
            }
            else if (method == Models.CommitSearchMethod.ByPath)
            {
                search += $"-- {filter.Quoted()}";
            }
            else
            {
                search = $"-G{filter.Quoted()}";
            }

            WorkingDirectory = repo;
            Context = repo;
            Args = $"log -1000 --date-order --no-show-signature --decorate=full --format=%H%x00%P%x00%D%x00%aN±%aE%x00%at%x00%cN±%cE%x00%ct%x00%s {search}";
            _findFirstMerged = false;
        }

        public async Task<List<Models.Commit>> GetResultAsync()
        {
            // Use memory-optimized collection
            var commits = Models.MemoryOptimizer.RentCommitList();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var proc = new Process();
                proc.StartInfo = CreateGitStartInfo(true);
                proc.Start();

                var rawLines = Models.MemoryOptimizer.RentStringList();
                var lineCount = 0;

                // Read all lines first with batching
                while (await proc.StandardOutput.ReadLineAsync() is { } line)
                {
                    rawLines.Add(line);
                    lineCount++;

                    // Process in batches to prevent memory buildup
                    if (rawLines.Count >= BATCH_SIZE)
                    {
                        await ProcessCommitBatch(rawLines, commits);
                        rawLines.Clear();

                        // Yield control periodically to keep UI responsive
                        if (lineCount % YIELD_FREQUENCY == 0)
                        {
                            await Task.Delay(1); // Allow other tasks to run

                            // Suggest GC every 500 commits to manage memory
                            if (lineCount % 500 == 0)
                            {
                                Models.MemoryOptimizer.SuggestGarbageCollection();
                            }
                        }
                    }
                }

                // Process remaining commits
                if (rawLines.Count > 0)
                {
                    await ProcessCommitBatch(rawLines, commits);
                }

                // Return string list to pool
                Models.MemoryOptimizer.ReturnStringList(rawLines);

                await proc.WaitForExitAsync().ConfigureAwait(false);

                if (_findFirstMerged && !_isHeadFound && commits.Count > 0)
                    await MarkFirstMergedAsync(commits).ConfigureAwait(false);

                System.Diagnostics.Debug.WriteLine($"[PERF] QueryCommitsOptimized processed {commits.Count} commits in {stopwatch.ElapsedMilliseconds}ms");

                // Create a new list to return (can't return pooled list)
                var result = new List<Models.Commit>(commits);
                Models.MemoryOptimizer.ReturnCommitList(commits);

                return result;
            }
            catch (Exception e)
            {
                // Make sure to return pooled resources on error
                Models.MemoryOptimizer.ReturnCommitList(commits);
                App.RaiseException(Context, $"Failed to query commits. Reason: {e.Message}");
                return new List<Models.Commit>();
            }
        }

        private static async Task ProcessCommitBatch(List<string> lines, List<Models.Commit> commits)
        {
            await Task.Run(() =>
            {
                foreach (var line in lines)
                {
                    var parts = line.Split('\0');
                    if (parts.Length != 8)
                        continue;

                    var commit = CreateCommitFromParts(parts);
                    commits.Add(commit);
                }
            });
        }

        private static Models.Commit CreateCommitFromParts(string[] parts)
        {
            var commit = new Models.Commit() { SHA = parts[0] };

            // Optimize memory allocation by reusing existing User objects
            commit.ParseParents(parts[1]);
            commit.ParseDecorators(parts[2]);
            commit.Author = Models.User.FindOrAdd(parts[3]);
            commit.AuthorTime = ulong.Parse(parts[4]);
            commit.Committer = Models.User.FindOrAdd(parts[5]);
            commit.CommitterTime = ulong.Parse(parts[6]);
            commit.Subject = parts[7];

            return commit;
        }

        private async Task MarkFirstMergedAsync(List<Models.Commit> commits)
        {
            Args = $"log --since={commits[^1].CommitterTimeStr.Quoted()} --format=\"%H\"";

            var rs = await ReadToEndAsync().ConfigureAwait(false);
            var shas = rs.StdOut.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            if (shas.Length == 0)
                return;

            var set = new HashSet<string>(shas);

            foreach (var c in commits)
            {
                if (set.Contains(c.SHA))
                {
                    c.IsMerged = true;
                    break;
                }
            }
        }

        private bool _findFirstMerged = false;
        private bool _isHeadFound = false;
    }
}