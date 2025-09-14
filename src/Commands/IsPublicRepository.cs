using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SourceGit.Commands
{
    /// <summary>
    /// Checks if a remote repository is publicly accessible without authentication
    /// </summary>
    public class IsPublicRepository
    {
        public static async Task<bool> CheckAsync(string remoteUrl)
        {
            if (string.IsNullOrEmpty(remoteUrl))
                return false;

            try
            {
                // Parse the URL to determine the repository type
                var uri = new Uri(remoteUrl);

                // Handle GitHub repositories
                if (uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.Equals("www.github.com", StringComparison.OrdinalIgnoreCase))
                {
                    return await CheckGitHubRepository(uri);
                }

                // Handle GitLab repositories
                if (uri.Host.Equals("gitlab.com", StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.Equals("www.gitlab.com", StringComparison.OrdinalIgnoreCase))
                {
                    return await CheckGitLabRepository(uri);
                }

                // For other Git hosts, try a simple HEAD request
                return await CheckGenericRepository(remoteUrl);
            }
            catch
            {
                // If we can't determine, assume it needs authentication
                return false;
            }
        }

        private static async Task<bool> CheckGitHubRepository(Uri uri)
        {
            try
            {
                // Extract owner and repo from path
                var pathParts = uri.AbsolutePath.Trim('/').Split('/');
                if (pathParts.Length < 2)
                    return false;

                var owner = pathParts[0];
                var repo = pathParts[1].Replace(".git", "");

                // Use GitHub API to check if repo is public
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "SourceGit");

                var apiUrl = $"https://api.github.com/repos/{owner}/{repo}";
                var response = await client.GetAsync(apiUrl);

                // If we get a 200, it's public
                // If we get a 404 or 403, it might be private or doesn't exist
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<bool> CheckGitLabRepository(Uri uri)
        {
            try
            {
                // Extract project path from URL
                var pathParts = uri.AbsolutePath.Trim('/').Replace(".git", "");

                // Use GitLab API to check if project is public
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "SourceGit");

                var apiUrl = $"https://gitlab.com/api/v4/projects/{Uri.EscapeDataString(pathParts)}";
                var response = await client.GetAsync(apiUrl);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<bool> CheckGenericRepository(string remoteUrl)
        {
            try
            {
                // Try to access the info/refs endpoint which should be available for public repos
                var infoRefsUrl = remoteUrl.Replace(".git", "") + "/info/refs";

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var response = await client.GetAsync(infoRefsUrl);

                // If we can access it without auth, it's likely public
                return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;
            }
            catch
            {
                return false;
            }
        }
    }
}
