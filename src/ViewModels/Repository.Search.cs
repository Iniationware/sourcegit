using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace SourceGit.ViewModels
{
    /// <summary>
    /// Repository search operations - handles all search and filtering functionality
    /// </summary>
    public partial class Repository
    {
        #region Search Properties and State

        /// <summary>
        /// Clears the current filter
        /// </summary>
        public void ClearFilter()
        {
            Filter = string.Empty;
        }

        /// <summary>
        /// Clears the search commit filter
        /// </summary>
        public void ClearSearchCommitFilter()
        {
            SearchCommitFilter = string.Empty;
        }

        /// <summary>
        /// Clears matched files for searching
        /// </summary>
        public void ClearMatchedFilesForSearching()
        {
            MatchedFilesForSearching = null;
        }

        #endregion

        #region Commit Search Operations

        /// <summary>
        /// Starts searching for commits based on current filter settings
        /// </summary>
        public void StartSearchCommits()
        {
            if (_histories == null)
                return;

            IsSearchLoadingVisible = true;
            SelectedSearchedCommit = null;
            MatchedFilesForSearching = null;

            Task.Run(async () =>
            {
                var visible = new List<Models.Commit>();
                var method = (Models.CommitSearchMethod)_searchCommitFilterType;

                if (method == Models.CommitSearchMethod.BySHA)
                {
                    var isCommitSHA = await new Commands.IsCommitSHA(_fullpath, _searchCommitFilter)
                        .GetResultAsync()
                        .ConfigureAwait(false);

                    if (isCommitSHA)
                    {
                        var commit = await new Commands.QuerySingleCommit(_fullpath, _searchCommitFilter)
                            .GetResultAsync()
                            .ConfigureAwait(false);
                        visible.Add(commit);
                    }
                }
                else
                {
                    visible = await new Commands.QueryCommits(_fullpath, _searchCommitFilter, method, _onlySearchCommitsInCurrentBranch)
                        .GetResultAsync()
                        .ConfigureAwait(false);
                }

                PostToUIThread(() =>
                {
                    SearchedCommits = visible;
                    IsSearchLoadingVisible = false;
                });
            });
        }

        #endregion

        #region File Path Search Operations

        /// <summary>
        /// Checks if currently searching commits by file path
        /// </summary>
        /// <returns>True if searching by file path</returns>
        private bool IsSearchingCommitsByFilePath()
        {
            return _isSearching && _searchCommitFilterType == (int)Models.CommitSearchMethod.ByPath;
        }

        /// <summary>
        /// Calculates worktree files for searching
        /// </summary>
        private void CalcWorktreeFilesForSearching()
        {
            if (!IsSearchingCommitsByFilePath())
            {
                _requestingWorktreeFiles = false;
                _worktreeFiles = null;
                MatchedFilesForSearching = null;
                GC.Collect();
                return;
            }

            if (_requestingWorktreeFiles)
                return;

            _requestingWorktreeFiles = true;

            Task.Run(async () =>
            {
                _worktreeFiles = await new Commands.QueryRevisionFileNames(_fullpath, "HEAD")
                    .GetResultAsync()
                    .ConfigureAwait(false);

                PostToUIThread(() =>
                {
                    if (IsSearchingCommitsByFilePath() && _requestingWorktreeFiles)
                        CalcMatchedFilesForSearching();

                    _requestingWorktreeFiles = false;
                });
            });
        }

        /// <summary>
        /// Calculates matched files for searching based on current filter
        /// </summary>
        private void CalcMatchedFilesForSearching()
        {
            if (_worktreeFiles == null || _worktreeFiles.Count == 0 || _searchCommitFilter.Length < 3)
            {
                MatchedFilesForSearching = null;
                return;
            }

            var matched = new List<string>();
            foreach (var file in _worktreeFiles)
            {
                if (file.Contains(_searchCommitFilter, StringComparison.OrdinalIgnoreCase) && file.Length != _searchCommitFilter.Length)
                {
                    matched.Add(file);
                    if (matched.Count > 100)
                        break;
                }
            }

            MatchedFilesForSearching = matched;
        }

        #endregion

        #region History Filter Operations

        /// <summary>
        /// Clears all history filters
        /// </summary>
        public void ClearHistoriesFilter()
        {
            _settings.HistoriesFilters.Clear();
            HistoriesFilterMode = Models.FilterMode.None;

            ResetBranchTreeFilterMode(LocalBranchTrees);
            ResetBranchTreeFilterMode(RemoteBranchTrees);
            ResetTagFilterMode();
            RefreshCommits();
        }

        /// <summary>
        /// Removes a specific history filter
        /// </summary>
        /// <param name="filter">The filter to remove</param>
        public void RemoveHistoriesFilter(Models.Filter filter)
        {
            if (_settings.HistoriesFilters.Remove(filter))
            {
                HistoriesFilterMode = _settings.HistoriesFilters.Count > 0 ? _settings.HistoriesFilters[0].Mode : Models.FilterMode.None;
                RefreshHistoriesFilters(true);
            }
        }

        /// <summary>
        /// Sets the tag filter mode
        /// </summary>
        /// <param name="tag">The tag to filter</param>
        /// <param name="mode">The filter mode</param>
        public void SetTagFilterMode(Models.Tag tag, Models.FilterMode mode)
        {
            var changed = _settings.UpdateHistoriesFilter(tag.Name, Models.FilterType.Tag, mode);
            if (changed)
                RefreshHistoriesFilters(true);
        }

        /// <summary>
        /// Sets the branch filter mode
        /// </summary>
        /// <param name="branch">The branch to filter</param>
        /// <param name="mode">The filter mode</param>
        /// <param name="clearExists">Whether to clear existing filters</param>
        /// <param name="refresh">Whether to refresh after setting</param>
        public void SetBranchFilterMode(Models.Branch branch, Models.FilterMode mode, bool clearExists, bool refresh)
        {
            var node = FindBranchNode(branch.IsLocal ? _localBranchTrees : _remoteBranchTrees, branch.FullName);
            if (node != null)
                SetBranchFilterMode(node, mode, clearExists, refresh);
        }

        /// <summary>
        /// Sets the branch filter mode for a specific node
        /// </summary>
        /// <param name="node">The branch tree node</param>
        /// <param name="mode">The filter mode</param>
        /// <param name="clearExists">Whether to clear existing filters</param>
        /// <param name="refresh">Whether to refresh after setting</param>
        public void SetBranchFilterMode(BranchTreeNode node, Models.FilterMode mode, bool clearExists, bool refresh)
        {
            var isLocal = node.Path.StartsWith("refs/heads/", StringComparison.Ordinal);
            var tree = isLocal ? _localBranchTrees : _remoteBranchTrees;

            if (clearExists)
            {
                _settings.HistoriesFilters.Clear();
                HistoriesFilterMode = Models.FilterMode.None;
            }

            if (node.Backend is Models.Branch branch)
            {
                var type = isLocal ? Models.FilterType.LocalBranch : Models.FilterType.RemoteBranch;
                var changed = _settings.UpdateHistoriesFilter(node.Path, type, mode);
                if (!changed)
                    return;

                if (isLocal && !string.IsNullOrEmpty(branch.Upstream) && !branch.IsUpstreamGone)
                    _settings.UpdateHistoriesFilter(branch.Upstream, Models.FilterType.RemoteBranch, mode);
            }
            else
            {
                var type = isLocal ? Models.FilterType.LocalBranchFolder : Models.FilterType.RemoteBranchFolder;
                var changed = _settings.UpdateHistoriesFilter(node.Path, type, mode);
                if (!changed)
                    return;

                _settings.RemoveChildrenBranchFilters(node.Path);
            }

            UpdateParentBranchFilters(node, tree, isLocal);
            RefreshHistoriesFilters(refresh);
        }

        /// <summary>
        /// Toggles a history show flag
        /// </summary>
        /// <param name="flag">The flag to toggle</param>
        public void ToggleHistoryShowFlag(Models.HistoryShowFlags flag)
        {
            if (_settings.HistoryShowFlags.HasFlag(flag))
                HistoryShowFlags -= flag;
            else
                HistoryShowFlags |= flag;
        }

        #endregion

        #region Navigation Operations

        /// <summary>
        /// Navigates to a specific commit
        /// </summary>
        /// <param name="sha">The commit SHA to navigate to</param>
        /// <param name="isDelayMode">Whether to delay the navigation</param>
        public void NavigateToCommit(string sha, bool isDelayMode = false)
        {
            if (isDelayMode)
            {
                _navigateToCommitDelayed = sha;
            }
            else if (_histories != null)
            {
                SelectedViewIndex = 0;
                _histories.NavigateTo(sha);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Refreshes history filters
        /// </summary>
        /// <param name="refresh">Whether to refresh commits</param>
        private void RefreshHistoriesFilters(bool refresh)
        {
            if (_settings.HistoriesFilters.Count > 0)
                HistoriesFilterMode = _settings.HistoriesFilters[0].Mode;
            else
                HistoriesFilterMode = Models.FilterMode.None;

            if (!refresh)
                return;

            var filters = _settings.CollectHistoriesFilters();
            UpdateBranchTreeFilterMode(LocalBranchTrees, filters);
            UpdateBranchTreeFilterMode(RemoteBranchTrees, filters);
            UpdateTagFilterMode(filters);
            RefreshCommits();
        }

        /// <summary>
        /// Updates branch tree filter mode
        /// </summary>
        /// <param name="nodes">The branch tree nodes</param>
        /// <param name="filters">The filters to apply</param>
        private void UpdateBranchTreeFilterMode(List<BranchTreeNode> nodes, Dictionary<string, Models.FilterMode> filters)
        {
            foreach (var node in nodes)
            {
                node.FilterMode = filters.GetValueOrDefault(node.Path, Models.FilterMode.None);

                if (!node.IsBranch)
                    UpdateBranchTreeFilterMode(node.Children, filters);
            }
        }

        /// <summary>
        /// Updates tag filter mode
        /// </summary>
        /// <param name="filters">The filters to apply</param>
        private void UpdateTagFilterMode(Dictionary<string, Models.FilterMode> filters)
        {
            if (VisibleTags is TagCollectionAsTree tree)
            {
                foreach (var node in tree.Tree)
                    node.UpdateFilterMode(filters);
            }
            else if (VisibleTags is TagCollectionAsList list)
            {
                foreach (var item in list.TagItems)
                    item.FilterMode = filters.GetValueOrDefault(item.Tag.Name, Models.FilterMode.None);
            }
        }

        /// <summary>
        /// Resets branch tree filter mode
        /// </summary>
        /// <param name="nodes">The branch tree nodes</param>
        private void ResetBranchTreeFilterMode(List<BranchTreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                node.FilterMode = Models.FilterMode.None;
                if (!node.IsBranch)
                    ResetBranchTreeFilterMode(node.Children);
            }
        }

        /// <summary>
        /// Resets tag filter mode
        /// </summary>
        private void ResetTagFilterMode()
        {
            if (VisibleTags is TagCollectionAsTree tree)
            {
                var filters = new Dictionary<string, Models.FilterMode>();
                foreach (var node in tree.Tree)
                    node.UpdateFilterMode(filters);
            }
            else if (VisibleTags is TagCollectionAsList list)
            {
                foreach (var item in list.TagItems)
                    item.FilterMode = Models.FilterMode.None;
            }
        }

        /// <summary>
        /// Finds a branch node by path
        /// </summary>
        /// <param name="nodes">The nodes to search</param>
        /// <param name="path">The path to find</param>
        /// <returns>The found node or null</returns>
        private BranchTreeNode FindBranchNode(List<BranchTreeNode> nodes, string path)
        {
            foreach (var node in nodes)
            {
                if (node.Path.Equals(path, StringComparison.Ordinal))
                    return node;

                if (path.StartsWith(node.Path, StringComparison.Ordinal))
                {
                    var founded = FindBranchNode(node.Children, path);
                    if (founded != null)
                        return founded;
                }
            }

            return null;
        }

        /// <summary>
        /// Updates parent branch filters
        /// </summary>
        /// <param name="node">The starting node</param>
        /// <param name="tree">The branch tree</param>
        /// <param name="isLocal">Whether dealing with local branches</param>
        private void UpdateParentBranchFilters(BranchTreeNode node, List<BranchTreeNode> tree, bool isLocal)
        {
            var parentType = isLocal ? Models.FilterType.LocalBranchFolder : Models.FilterType.RemoteBranchFolder;
            var cur = node;
            do
            {
                var lastSepIdx = cur.Path.LastIndexOf('/');
                if (lastSepIdx <= 0)
                    break;

                var parentPath = cur.Path.Substring(0, lastSepIdx);
                var parent = FindBranchNode(tree, parentPath);
                if (parent == null)
                    break;

                _settings.UpdateHistoriesFilter(parent.Path, parentType, Models.FilterMode.None);
                cur = parent;
            } while (true);
        }

        #endregion
    }
}