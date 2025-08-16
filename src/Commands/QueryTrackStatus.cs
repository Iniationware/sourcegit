﻿using System;
using System.Threading.Tasks;

namespace SourceGit.Commands
{
    public class QueryTrackStatus : Command
    {
        public QueryTrackStatus(string repo, string local, string upstream)
        {
            WorkingDirectory = repo;
            Context = repo;
            Args = $"rev-list --left-right {local}...{upstream}";
        }

        public async Task<Models.BranchTrackStatus> GetResultAsync()
        {
            var status = new Models.BranchTrackStatus();

            // Use retry wrapper to handle lock files
            var wrapper = new CommandWithRetry(this);
            var rs = await wrapper.ReadToEndWithRetryAsync().ConfigureAwait(false);
            if (!rs.IsSuccess)
                return status;

            var lines = rs.StdOut.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line[0] == '>')
                    status.Behind.Add(line.Substring(1));
                else
                    status.Ahead.Add(line.Substring(1));
            }

            return status;
        }
    }
}
