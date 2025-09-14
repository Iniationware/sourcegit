using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Threading;

namespace SourceGit.ViewModels
{
    /// <summary>
    /// Shared state for Repository partial classes
    /// Contains all fields that need to be accessed across partial class boundaries
    /// </summary>
    public partial class Repository
    {
        #region Core Repository State
        protected internal string _fullpath = string.Empty;
        protected internal string _gitDir = string.Empty;
        protected internal string _gitCommonDir = string.Empty;
        protected internal bool _isWorktree = false;
        protected internal Models.RepositorySettings _settings = null;
        #endregion

        #region Collections and Data
        protected internal List<Models.Remote> _remotes = new List<Models.Remote>();
        protected internal List<Models.Branch> _branches = new List<Models.Branch>();
        protected internal Models.Branch _currentBranch = null;
        protected internal List<BranchTreeNode> _localBranchTrees = new List<BranchTreeNode>();
        protected internal List<BranchTreeNode> _remoteBranchTrees = new List<BranchTreeNode>();
        protected internal List<Models.Worktree> _worktrees = new List<Models.Worktree>();
        protected internal List<Models.Tag> _tags = new List<Models.Tag>();
        protected internal object _visibleTags = null;
        protected internal List<Models.Submodule> _submodules = new List<Models.Submodule>();
        protected internal object _visibleSubmodules = null;
        protected internal List<Models.GitFlowBranchGroup> _gitFlowGroups = new List<Models.GitFlowBranchGroup>();
        protected internal List<Models.Branch> _gitFlowBranches = new List<Models.Branch>();
        #endregion

        #region View Models and UI State
        protected internal Models.Watcher _watcher = null;
        protected internal Histories _histories = null;
        protected internal WorkingCopy _workingCopy = null;
        protected internal StashesPage _stashesPage = null;
        protected internal int _selectedViewIndex = 0;
        protected internal object _selectedView = null;
        #endregion

        #region Search State
        protected internal bool _isSearching = false;
        protected internal bool _isSearchLoadingVisible = false;
        protected internal int _searchCommitFilterType = (int)Models.CommitSearchMethod.ByMessage;
        protected internal bool _onlySearchCommitsInCurrentBranch = false;
        protected internal string _searchCommitFilter = string.Empty;
        protected internal List<Models.Commit> _searchedCommits = new List<Models.Commit>();
        protected internal Models.Commit _selectedSearchedCommit = null;
        protected internal bool _requestingWorktreeFiles = false;
        protected internal List<string> _worktreeFiles = null;
        protected internal List<string> _matchedFilesForSearching = null;
        #endregion

        #region Performance and Background Operations
        protected internal bool _isAutoFetching = false;
        protected internal Timer _autoFetchTimer = null;
        protected internal DateTime _lastFetchTime = DateTime.MinValue;
        protected internal MemoryMetrics _memoryMetrics = null;
        protected internal Models.BranchCounter _branchCounter = null;
        protected internal Models.CommitStatistics _commitStats = null;
        protected internal CancellationTokenSource _operationsCancellationTokenSource = null;
        #endregion

        #region Filter and Display State
        protected internal string _filter = string.Empty;
        protected internal Models.FilterMode _historiesFilterMode = Models.FilterMode.None;
        protected internal bool _hasAllowedSignersFile = false;
        protected internal int _localBranchesCount = 0;
        protected internal int _localChangesCount = 0;
        protected internal int _stashesCount = 0;
        #endregion

        #region Bisect and Navigation State
        protected internal Models.BisectState _bisectState = Models.BisectState.None;
        protected internal bool _isBisectCommandRunning = false;
        protected internal string _navigateToCommitDelayed = string.Empty;
        #endregion

        #region Cache and Performance
        protected internal static readonly Models.LRUCache<string, Models.CommitGraph> _graphCache =
            new Models.LRUCache<string, Models.CommitGraph>(
                maxCapacity: 50,
                maxMemoryMB: 200,
                sizeCalculator: graph =>
                {
                    if (graph == null)
                        return 0;
                    long size = 0;
                    size += (graph.Paths?.Count ?? 0) * 250;
                    size += (graph.Links?.Count ?? 0) * 150;
                    size += (graph.Dots?.Count ?? 0) * 100;
                    size += 1024;
                    return size;
                });
        #endregion

        #region Helper Methods for State Management
        /// <summary>
        /// Thread-safe method to access UI dispatcher
        /// </summary>
        protected internal static void ExecuteOnUIThread(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
                action();
            else
                Dispatcher.UIThread.Invoke(action);
        }

        /// <summary>
        /// Thread-safe method to post to UI dispatcher
        /// </summary>
        protected internal static void PostToUIThread(Action action)
        {
            Dispatcher.UIThread.Post(action);
        }
        #endregion
    }
}
