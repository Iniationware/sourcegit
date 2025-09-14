
namespace SourceGit.Models
{
    /// <summary>
    /// Options for controlling repository refresh operations after Git commands
    /// </summary>
    public class RefreshOptions
    {
        /// <summary>
        /// Refresh local and remote branches (updates ahead/behind counts)
        /// </summary>
        public bool RefreshBranches { get; set; } = true;

        /// <summary>
        /// Refresh tags from remotes
        /// </summary>
        public bool RefreshTags { get; set; } = false;

        /// <summary>
        /// Refresh working copy status (modified/staged files)
        /// </summary>
        public bool RefreshWorkingCopy { get; set; } = false;

        /// <summary>
        /// Refresh commit history and graph
        /// </summary>
        public bool RefreshCommits { get; set; } = false;

        /// <summary>
        /// Refresh stashes
        /// </summary>
        public bool RefreshStashes { get; set; } = false;

        /// <summary>
        /// Refresh submodules
        /// </summary>
        public bool RefreshSubmodules { get; set; } = false;

        /// <summary>
        /// Creates refresh options for a push operation
        /// </summary>
        public static RefreshOptions ForPush(bool pushedTags = false, bool isCurrentBranch = false)
        {
            return new RefreshOptions
            {
                RefreshBranches = true,
                RefreshTags = pushedTags,
                RefreshCommits = isCurrentBranch,
                RefreshWorkingCopy = false
            };
        }

        /// <summary>
        /// Creates refresh options for a fetch operation
        /// </summary>
        public static RefreshOptions ForFetch(bool fetchedTags = true)
        {
            return new RefreshOptions
            {
                RefreshBranches = true,
                RefreshTags = fetchedTags,
                RefreshCommits = true,
                RefreshWorkingCopy = false
            };
        }

        /// <summary>
        /// Creates refresh options for a pull operation
        /// </summary>
        public static RefreshOptions ForPull()
        {
            return new RefreshOptions
            {
                RefreshBranches = true,
                RefreshTags = true,
                RefreshCommits = true,
                RefreshWorkingCopy = true
            };
        }

        /// <summary>
        /// Creates refresh options for a commit operation
        /// </summary>
        public static RefreshOptions ForCommit()
        {
            return new RefreshOptions
            {
                RefreshBranches = true,
                RefreshCommits = true,
                RefreshWorkingCopy = true,
                RefreshStashes = false
            };
        }

        /// <summary>
        /// Creates refresh options for a stash operation
        /// </summary>
        public static RefreshOptions ForStash()
        {
            return new RefreshOptions
            {
                RefreshBranches = false,
                RefreshTags = false,
                RefreshWorkingCopy = true,
                RefreshStashes = true,
                RefreshCommits = false,
                RefreshSubmodules = false
            };
        }

        /// <summary>
        /// Creates options to refresh everything
        /// </summary>
        public static RefreshOptions All()
        {
            return new RefreshOptions
            {
                RefreshBranches = true,
                RefreshTags = true,
                RefreshCommits = true,
                RefreshWorkingCopy = true,
                RefreshStashes = true,
                RefreshSubmodules = true
            };
        }
    }
}