using System.Threading.Tasks;

namespace SourceGit.Commands
{
    public class Fetch : Command
    {
        public Fetch(string repo, string remote, bool noTags, bool force)
        {
            _remoteKey = $"remote.{remote}.sshkey";
            _remote = remote;

            WorkingDirectory = repo;
            Context = repo;
            Args = "fetch --progress --verbose ";

            if (noTags)
                Args += "--no-tags ";
            else
                Args += "--tags ";

            if (force)
                Args += "--force ";

            Args += remote;
        }

        public Fetch(string repo, Models.Branch local, Models.Branch remote)
        {
            _remoteKey = $"remote.{remote.Remote}.sshkey";
            _remote = remote.Remote;

            WorkingDirectory = repo;
            Context = repo;
            Args = $"fetch --progress --verbose {remote.Remote} {remote.Name}:{local.Name}";
        }

        public async Task<bool> RunAsync()
        {
            // Check if remote URL is a public repository
            if (!string.IsNullOrEmpty(_remote))
            {
                var remoteUrl = await new Config(WorkingDirectory).GetAsync($"remote.{_remote}.url").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(remoteUrl) && remoteUrl.StartsWith("https://"))
                {
                    // For public HTTPS repos, we don't need SSH keys or credentials
                    if (remoteUrl.Contains("github.com") || remoteUrl.Contains("gitlab.com") || 
                        remoteUrl.Contains("bitbucket.org") || remoteUrl.Contains("gitee.com"))
                    {
                        // Skip SSH key and credentials for public repos
                        SSHKey = string.Empty;
                        SkipCredentials = true;
                    }
                    else
                    {
                        SSHKey = await new Config(WorkingDirectory).GetAsync(_remoteKey).ConfigureAwait(false);
                    }
                }
                else
                {
                    SSHKey = await new Config(WorkingDirectory).GetAsync(_remoteKey).ConfigureAwait(false);
                }
            }
            else
            {
                SSHKey = await new Config(WorkingDirectory).GetAsync(_remoteKey).ConfigureAwait(false);
            }
            
            return await ExecAsync().ConfigureAwait(false);
        }

        private readonly string _remoteKey;
        private readonly string _remote = string.Empty;
    }
}
