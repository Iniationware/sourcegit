using System.Collections.Generic;

namespace SourceGit.Commands
{
    /// <summary>
    /// Count branches in the repository
    /// </summary>
    public class CountBranches : Command
    {
        public class BranchCount
        {
            public int Total { get; set; }
            public int Local { get; set; }
            public int Remote { get; set; }
            public Dictionary<string, int> ByType { get; set; } = new Dictionary<string, int>();
        }

        public CountBranches(string repo)
        {
            WorkingDirectory = repo;
            Context = repo;
            Args = "branch -a --no-column";
        }

        public new BranchCount Result()
        {
            var output = ReadToEnd();
            if (output.IsSuccess)
            {
                var count = new BranchCount();
                var lines = output.StdOut.Split('\n');

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var branch = line.Trim();
                    if (branch.StartsWith("*"))
                        branch = branch.Substring(1).Trim();

                    if (branch.StartsWith("remotes/"))
                    {
                        count.Remote++;

                        // Count by remote name
                        var parts = branch.Split('/');
                        if (parts.Length > 1)
                        {
                            var remoteName = parts[1];
                            if (!count.ByType.ContainsKey($"remote/{remoteName}"))
                                count.ByType[$"remote/{remoteName}"] = 0;
                            count.ByType[$"remote/{remoteName}"]++;
                        }
                    }
                    else
                    {
                        count.Local++;

                        // Detect GitFlow branch types
                        if (branch.StartsWith("feature/"))
                        {
                            if (!count.ByType.ContainsKey("feature"))
                                count.ByType["feature"] = 0;
                            count.ByType["feature"]++;
                        }
                        else if (branch.StartsWith("release/"))
                        {
                            if (!count.ByType.ContainsKey("release"))
                                count.ByType["release"] = 0;
                            count.ByType["release"]++;
                        }
                        else if (branch.StartsWith("hotfix/"))
                        {
                            if (!count.ByType.ContainsKey("hotfix"))
                                count.ByType["hotfix"] = 0;
                            count.ByType["hotfix"]++;
                        }
                        else if (branch == "develop" || branch == "master" || branch == "main")
                        {
                            if (!count.ByType.ContainsKey("main"))
                                count.ByType["main"] = 0;
                            count.ByType["main"]++;
                        }
                        else
                        {
                            if (!count.ByType.ContainsKey("other"))
                                count.ByType["other"] = 0;
                            count.ByType["other"]++;
                        }
                    }

                    count.Total++;
                }

                return count;
            }

            return new BranchCount();
        }
    }
}