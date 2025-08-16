using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SourceGit.Commands
{
    public partial class QueryRemotes : Command
    {
        [GeneratedRegex(@"^([\w\.\-]+)\s*(\S+).*$")]
        private static partial Regex REG_REMOTE();

        public QueryRemotes(string repo)
        {
            WorkingDirectory = repo;
            Context = repo;
            Args = "remote -v";
        }

        public async Task<List<Models.Remote>> GetResultAsync()
        {
            var outs = new List<Models.Remote>();
            
            try
            {
                // Use retry wrapper to handle lock files
                var wrapper = new CommandWithRetry(this);
                var rs = await wrapper.ReadToEndWithRetryAsync().ConfigureAwait(false);
                if (!rs.IsSuccess)
                {
                    // Log the error for debugging
                    if (!string.IsNullOrEmpty(rs.StdErr))
                        App.RaiseException(WorkingDirectory, $"QueryRemotes failed: {rs.StdErr}");
                    return outs;
                }

            var lines = rs.StdOut.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var match = REG_REMOTE().Match(line);
                if (!match.Success)
                    continue;

                var remote = new Models.Remote()
                {
                    Name = match.Groups[1].Value,
                    URL = match.Groups[2].Value,
                };

                if (outs.Find(x => x.Name == remote.Name) != null)
                    continue;

                outs.Add(remote);
            }
            }
            catch (Exception ex)
            {
                App.RaiseException(WorkingDirectory, $"QueryRemotes exception: {ex.Message}");
            }

            return outs;
        }
    }
}
