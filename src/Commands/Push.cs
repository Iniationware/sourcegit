using System.Threading.Tasks;

namespace SourceGit.Commands
{
    public class Push : Command
    {
        public Push(string repo, string local, string remote, string remoteBranch, bool withTags, bool checkSubmodules, bool track, bool force)
        {
            _remote = remote;

            WorkingDirectory = repo;
            Context = repo;
            Args = "push --progress --verbose ";

            if (withTags)
                Args += "--tags ";
            if (checkSubmodules)
                Args += "--recurse-submodules=check ";
            if (track)
                Args += "-u ";
            if (force)
                Args += "--force-with-lease ";

            Args += $"{remote} {local}:{remoteBranch}";
        }

        public Push(string repo, string remote, string refname, bool isDelete)
        {
            _remote = remote;

            WorkingDirectory = repo;
            Context = repo;
            Args = "push ";

            if (isDelete)
                Args += "--delete ";

            Args += $"{remote} {refname}";
        }

        public async Task<bool> RunAsync()
        {
            // Check if remote URL is a public repository
            var remoteUrl = await new Config(WorkingDirectory).GetAsync($"remote.{_remote}.url").ConfigureAwait(false);
            
            // Push ALWAYS requires authentication, even for public repos
            // This is by design - you can read from public repos without auth,
            // but writing always requires proper credentials
            if (!string.IsNullOrEmpty(remoteUrl) && remoteUrl.StartsWith("https://"))
            {
                // Check if user is trying to push to a public host without proper setup
                if (remoteUrl.Contains("github.com") || remoteUrl.Contains("gitlab.com"))
                {
                    // Note: We still need credentials for push, but we can provide a better error message
                    RaiseError = true;
                    
                    // Check if we have stored credentials or SSH key
                    var hasCredentials = !string.IsNullOrEmpty(Native.OS.CredentialHelper);
                    var sshKey = await new Config(WorkingDirectory).GetAsync($"remote.{_remote}.sshkey").ConfigureAwait(false);
                    
                    if (!hasCredentials && string.IsNullOrEmpty(sshKey))
                    {
                        // Provide helpful message about push requirements
                        App.RaiseException(Context, 
                            $"Push to {remoteUrl} requires authentication.\n\n" +
                            "Even though the repository is public, pushing changes always requires proper credentials.\n\n" +
                            "Options:\n" +
                            "1. Set up a Personal Access Token (recommended for HTTPS)\n" +
                            "2. Configure SSH keys (recommended for SSH URLs)\n" +
                            "3. Use Git Credential Manager\n\n" +
                            "For GitHub: Create a token at Settings → Developer settings → Personal access tokens\n" +
                            "For GitLab: Create a token at Settings → Access Tokens");
                        return false;
                    }
                }
            }
            
            SSHKey = await new Config(WorkingDirectory).GetAsync($"remote.{_remote}.sshkey").ConfigureAwait(false);
            
            // Don't skip credentials for push - it always needs auth
            SkipCredentials = false;
            
            return await ExecAsync().ConfigureAwait(false);
        }

        private readonly string _remote;
    }
}
