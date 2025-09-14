using System;
using System.Threading.Tasks;

namespace SourceGit.Commands
{
    /// <summary>
    /// Fetch command that removes authentication from URLs for public repositories
    /// </summary>
    public class FetchWithoutAuth : Command
    {
        public FetchWithoutAuth(string repo, string remote, bool noTags = false, bool force = false)
        {
            _remote = remote;

            WorkingDirectory = repo;
            Context = repo;

            // Build basic fetch command
            Args = "fetch --progress --verbose ";

            if (noTags)
                Args += "--no-tags ";
            else
                Args += "--tags ";

            if (force)
                Args += "--force ";

            // We'll add the remote URL directly instead of using the remote name
            _needsUrlRewrite = true;
        }

        public async Task<bool> RunAsync()
        {
            if (_needsUrlRewrite)
            {
                // Get the remote URL
                var remoteUrl = await new Config(WorkingDirectory).GetAsync($"remote.{_remote}.url").ConfigureAwait(false);

                if (!string.IsNullOrEmpty(remoteUrl))
                {
                    // Check if it's a public repository URL that we can fetch without auth
                    if (IsPublicRepoUrl(remoteUrl))
                    {
                        // For HTTPS URLs to public hosts, ensure no credentials are embedded
                        var cleanUrl = CleanUrl(remoteUrl);

                        // Use the clean URL directly instead of the remote name
                        // This bypasses any stored credentials
                        Args += cleanUrl;

                        // Ensure no credentials are used
                        SkipCredentials = true;
                        SSHKey = string.Empty;
                    }
                    else
                    {
                        // Use normal remote name for non-public repos
                        Args += _remote;
                        SSHKey = await new Config(WorkingDirectory).GetAsync($"remote.{_remote}.sshkey").ConfigureAwait(false);
                    }
                }
                else
                {
                    // Fallback to remote name if we can't get URL
                    Args += _remote;
                }
            }

            return await ExecAsync().ConfigureAwait(false);
        }

        private bool IsPublicRepoUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            // Check for HTTPS URLs to known public hosts
            if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return url.Contains("github.com", StringComparison.OrdinalIgnoreCase) ||
                       url.Contains("gitlab.com", StringComparison.OrdinalIgnoreCase) ||
                       url.Contains("bitbucket.org", StringComparison.OrdinalIgnoreCase) ||
                       url.Contains("gitee.com", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private string CleanUrl(string url)
        {
            // Remove any embedded credentials from the URL
            // Format: https://username:password@github.com/... -> https://github.com/...

            if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return url;

            try
            {
                var uri = new Uri(url);

                // If there's user info in the URL, remove it
                if (!string.IsNullOrEmpty(uri.UserInfo))
                {
                    // Reconstruct URL without credentials
                    var cleanUri = new UriBuilder(uri)
                    {
                        UserName = string.Empty,
                        Password = string.Empty
                    };

                    return cleanUri.Uri.ToString();
                }

                return url;
            }
            catch
            {
                // If parsing fails, try manual cleaning
                var atIndex = url.IndexOf('@');
                var protocolEnd = url.IndexOf("://") + 3;

                if (atIndex > protocolEnd && atIndex < url.Length - 1)
                {
                    // Remove everything between :// and @
                    return url.Substring(0, protocolEnd) + url.Substring(atIndex + 1);
                }

                return url;
            }
        }

        private readonly string _remote;
        private readonly bool _needsUrlRewrite;
    }
}
