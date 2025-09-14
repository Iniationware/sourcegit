using System.Threading.Tasks;

namespace SourceGit.Commands
{
    public class Pull : Command
    {
        public Pull(string repo, string remote, string branch, bool useRebase)
        {
            _remote = remote;

            WorkingDirectory = repo;
            Context = repo;
            Args = "pull --verbose --progress ";

            if (useRebase)
                Args += "--rebase=true ";

            Args += $"{remote} {branch}";
        }

        public async Task<bool> RunAsync()
        {
            // Check if remote URL is a public repository
            var remoteUrl = await new Config(WorkingDirectory).GetAsync($"remote.{_remote}.url").ConfigureAwait(false);
            if (!string.IsNullOrEmpty(remoteUrl) && remoteUrl.StartsWith("https://"))
            {
                // For public HTTPS repos, we don't need SSH keys or credentials
                if (remoteUrl.Contains("github.com") || remoteUrl.Contains("gitlab.com") ||
                    remoteUrl.Contains("bitbucket.org") || remoteUrl.Contains("gitee.com"))
                {
                    SSHKey = string.Empty;
                    SkipCredentials = true;
                }
                else
                {
                    SSHKey = await new Config(WorkingDirectory).GetAsync($"remote.{_remote}.sshkey").ConfigureAwait(false);
                }
            }
            else
            {
                SSHKey = await new Config(WorkingDirectory).GetAsync($"remote.{_remote}.sshkey").ConfigureAwait(false);
            }

            return await ExecAsync().ConfigureAwait(false);
        }

        private readonly string _remote;
    }
}
