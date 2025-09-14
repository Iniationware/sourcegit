using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels
{
    public partial class Repository : ObservableObject, Models.IRepository
    {
        public bool IsBare
        {
            get;
        }

        public string FullPath
        {
            get => _fullpath;
            set
            {
                if (value != null)
                {
                    var normalized = value.Replace('\\', '/').TrimEnd('/');
                    SetProperty(ref _fullpath, normalized);
                }
                else
                {
                    SetProperty(ref _fullpath, null);
                }
            }
        }

        public string GitDir
        {
            get => _gitDir;
            set => SetProperty(ref _gitDir, value);
        }

        public Models.RepositorySettings Settings
        {
            get => _settings;
        }

        public Models.GitFlow GitFlow
        {
            get;
            set;
        } = new Models.GitFlow();

        public Models.FilterMode HistoriesFilterMode
        {
            get => _historiesFilterMode;
            private set => SetProperty(ref _historiesFilterMode, value);
        }

        public bool HasAllowedSignersFile
        {
            get => _hasAllowedSignersFile;
        }

        public int SelectedViewIndex
        {
            get => _selectedViewIndex;
            set
            {
                if (SetProperty(ref _selectedViewIndex, value))
                {
                    SelectedView = value switch
                    {
                        1 => _workingCopy,
                        2 => _stashesPage,
                        _ => _histories,
                    };
                }
            }
        }

        public object SelectedView
        {
            get => _selectedView;
            set => SetProperty(ref _selectedView, value);
        }

        public bool EnableTopoOrderInHistories
        {
            get => _settings.EnableTopoOrderInHistories;
            set
            {
                if (value != _settings.EnableTopoOrderInHistories)
                {
                    _settings.EnableTopoOrderInHistories = value;
                    RefreshCommits();
                }
            }
        }

        public Models.HistoryShowFlags HistoryShowFlags
        {
            get => _settings.HistoryShowFlags;
            set
            {
                if (value != _settings.HistoryShowFlags)
                {
                    _settings.HistoryShowFlags = value;
                    RefreshCommits();
                }
            }
        }

        public bool OnlyHighlightCurrentBranchInHistories
        {
            get => _settings.OnlyHighlightCurrentBranchInHistories;
            set
            {
                if (value != _settings.OnlyHighlightCurrentBranchInHistories)
                {
                    _settings.OnlyHighlightCurrentBranchInHistories = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Filter
        {
            get => _filter;
            set
            {
                if (SetProperty(ref _filter, value))
                {
                    var builder = BuildBranchTree(_branches, _remotes);
                    LocalBranchTrees = builder.Locals;
                    RemoteBranchTrees = builder.Remotes;
                    VisibleTags = BuildVisibleTags();
                    VisibleSubmodules = BuildVisibleSubmodules();
                }
            }
        }

        public List<Models.Remote> Remotes
        {
            get => _remotes;
            private set => SetProperty(ref _remotes, value);
        }

        public List<Models.Branch> Branches
        {
            get => _branches;
            private set => SetProperty(ref _branches, value);
        }

        public Models.Branch CurrentBranch
        {
            get => _currentBranch;
            private set
            {
                var oldHead = _currentBranch?.Head;
                if (SetProperty(ref _currentBranch, value) && value != null)
                {
                    if (oldHead != _currentBranch.Head && _workingCopy is { UseAmend: true })
                        _workingCopy.UseAmend = false;
                }
            }
        }

        public List<BranchTreeNode> LocalBranchTrees
        {
            get => _localBranchTrees;
            private set => SetProperty(ref _localBranchTrees, value);
        }

        public List<BranchTreeNode> RemoteBranchTrees
        {
            get => _remoteBranchTrees;
            private set => SetProperty(ref _remoteBranchTrees, value);
        }

        public List<Models.Worktree> Worktrees
        {
            get => _worktrees;
            private set => SetProperty(ref _worktrees, value);
        }

        public List<Models.Tag> Tags
        {
            get => _tags;
            private set => SetProperty(ref _tags, value);
        }

        public bool ShowTagsAsTree
        {
            get => Preferences.Instance.ShowTagsAsTree;
            set
            {
                if (value != Preferences.Instance.ShowTagsAsTree)
                {
                    Preferences.Instance.ShowTagsAsTree = value;
                    VisibleTags = BuildVisibleTags();
                    OnPropertyChanged();
                }
            }
        }

        public object VisibleTags
        {
            get => _visibleTags;
            private set => SetProperty(ref _visibleTags, value);
        }

        public List<Models.Submodule> Submodules
        {
            get => _submodules;
            private set => SetProperty(ref _submodules, value);
        }

        public bool ShowSubmodulesAsTree
        {
            get => Preferences.Instance.ShowSubmodulesAsTree;
            set
            {
                if (value != Preferences.Instance.ShowSubmodulesAsTree)
                {
                    Preferences.Instance.ShowSubmodulesAsTree = value;
                    VisibleSubmodules = BuildVisibleSubmodules();
                    OnPropertyChanged();
                }
            }
        }

        public object VisibleSubmodules
        {
            get => _visibleSubmodules;
            private set => SetProperty(ref _visibleSubmodules, value);
        }

        public int LocalChangesCount
        {
            get => _localChangesCount;
            private set => SetProperty(ref _localChangesCount, value);
        }

        public int StashesCount
        {
            get => _stashesCount;
            private set => SetProperty(ref _stashesCount, value);
        }

        public int LocalBranchesCount
        {
            get => _localBranchesCount;
            private set => SetProperty(ref _localBranchesCount, value);
        }

        public bool IncludeUntracked
        {
            get => _settings.IncludeUntrackedInLocalChanges;
            set
            {
                if (value != _settings.IncludeUntrackedInLocalChanges)
                {
                    _settings.IncludeUntrackedInLocalChanges = value;
                    OnPropertyChanged();
                    RefreshWorkingCopyChanges();
                }
            }
        }

        public MemoryMetrics MemoryMetrics
        {
            get => _memoryMetrics;
            private set => SetProperty(ref _memoryMetrics, value);
        }

        public Models.BranchCounter BranchCounter
        {
            get => _branchCounter;
            private set => SetProperty(ref _branchCounter, value);
        }

        public Models.CommitStatistics CommitStats
        {
            get => _commitStats;
            private set => SetProperty(ref _commitStats, value);
        }

        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                if (SetProperty(ref _isSearching, value))
                {
                    if (value)
                    {
                        SelectedViewIndex = 0;
                        CalcWorktreeFilesForSearching();
                    }
                    else
                    {
                        SearchedCommits = new List<Models.Commit>();
                        SelectedSearchedCommit = null;
                        SearchCommitFilter = string.Empty;
                        MatchedFilesForSearching = null;
                        _requestingWorktreeFiles = false;
                        _worktreeFiles = null;
                    }
                }
            }
        }

        public bool IsSearchLoadingVisible
        {
            get => _isSearchLoadingVisible;
            private set => SetProperty(ref _isSearchLoadingVisible, value);
        }

        public bool OnlySearchCommitsInCurrentBranch
        {
            get => _onlySearchCommitsInCurrentBranch;
            set
            {
                if (SetProperty(ref _onlySearchCommitsInCurrentBranch, value) && !string.IsNullOrEmpty(_searchCommitFilter))
                    StartSearchCommits();
            }
        }

        public int SearchCommitFilterType
        {
            get => _searchCommitFilterType;
            set
            {
                if (SetProperty(ref _searchCommitFilterType, value))
                {
                    CalcWorktreeFilesForSearching();
                    if (!string.IsNullOrEmpty(_searchCommitFilter))
                        StartSearchCommits();
                }
            }
        }

        public string SearchCommitFilter
        {
            get => _searchCommitFilter;
            set
            {
                if (SetProperty(ref _searchCommitFilter, value) && IsSearchingCommitsByFilePath())
                    CalcMatchedFilesForSearching();
            }
        }

        public List<string> MatchedFilesForSearching
        {
            get => _matchedFilesForSearching;
            private set => SetProperty(ref _matchedFilesForSearching, value);
        }

        public List<Models.Commit> SearchedCommits
        {
            get => _searchedCommits;
            set => SetProperty(ref _searchedCommits, value);
        }

        public Models.Commit SelectedSearchedCommit
        {
            get => _selectedSearchedCommit;
            set
            {
                if (SetProperty(ref _selectedSearchedCommit, value) && value != null)
                    NavigateToCommit(value.SHA);
            }
        }

        public bool IsLocalBranchGroupExpanded
        {
            get => _settings.IsLocalBranchesExpandedInSideBar;
            set
            {
                if (value != _settings.IsLocalBranchesExpandedInSideBar)
                {
                    _settings.IsLocalBranchesExpandedInSideBar = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRemoteGroupExpanded
        {
            get => _settings.IsRemotesExpandedInSideBar;
            set
            {
                if (value != _settings.IsRemotesExpandedInSideBar)
                {
                    _settings.IsRemotesExpandedInSideBar = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsTagGroupExpanded
        {
            get => _settings.IsTagsExpandedInSideBar;
            set
            {
                if (value != _settings.IsTagsExpandedInSideBar)
                {
                    _settings.IsTagsExpandedInSideBar = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSubmoduleGroupExpanded
        {
            get => _settings.IsSubmodulesExpandedInSideBar;
            set
            {
                if (value != _settings.IsSubmodulesExpandedInSideBar)
                {
                    _settings.IsSubmodulesExpandedInSideBar = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsWorktreeGroupExpanded
        {
            get => _settings.IsWorktreeExpandedInSideBar;
            set
            {
                if (value != _settings.IsWorktreeExpandedInSideBar)
                {
                    _settings.IsWorktreeExpandedInSideBar = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowGitFlowInSidebar
        {
            get => _settings.ShowGitFlowInSidebar;
            set
            {
                if (value != _settings.ShowGitFlowInSidebar)
                {
                    _settings.ShowGitFlowInSidebar = value;
                    OnPropertyChanged();

                    // When enabling GitFlow display, check configuration and update branches
                    if (value)
                    {
                        Task.Run(async () =>
                        {
                            // Load GitFlow configuration if not already loaded
                            await LoadGitFlowConfigAsync();

                            // Ensure we have the latest branches
                            if (_branches == null || _branches.Count == 0)
                            {
                                // Refresh branches if not loaded
                                var branches = await new Commands.QueryBranches(_fullpath).GetResultAsync();
                                if (branches != null)
                                {
                                    _branches = branches;
                                }
                            }

                            // Collect local branches
                            var localBranches = new List<Models.Branch>();
                            if (_branches != null)
                            {
                                foreach (var b in _branches)
                                {
                                    if (b.IsLocal && !b.IsDetachedHead)
                                        localBranches.Add(b);
                                }
                            }

                            // Update GitFlow branches if enabled
                            // Note: We need to update on UI thread with the correct branches
                            Dispatcher.UIThread.Invoke(() =>
                            {
                                // Re-check if GitFlow is enabled after loading config
                                if (GitFlow != null && GitFlow.IsValid)
                                {
                                    UpdateGitFlowBranches(localBranches);

                                    // Auto-expand GitFlow section if it has content
                                    if (GitFlowBranchGroups != null && GitFlowBranchGroups.Count > 0)
                                    {
                                        _settings.IsGitFlowExpandedInSideBar = true;
                                    }

                                    // Force UI to refresh GitFlow section completely
                                    OnPropertyChanged(nameof(GitFlowBranchGroups));
                                    OnPropertyChanged(nameof(GitFlowBranches));
                                    OnPropertyChanged(nameof(IsGitFlowGroupExpanded));

                                    // Also ensure the sidebar knows to show GitFlow
                                    OnPropertyChanged(nameof(ShowGitFlowInSidebar));
                                }
                            });
                        });
                    }
                    else
                    {
                        // Clear GitFlow branches when disabled
                        GitFlowBranchGroups = new List<Models.GitFlowBranchGroup>();
                        GitFlowBranches = new List<Models.Branch>();
                    }
                }
            }
        }

        public bool IsGitFlowGroupExpanded
        {
            get => _settings.IsGitFlowExpandedInSideBar;
            set
            {
                if (value != _settings.IsGitFlowExpandedInSideBar)
                {
                    _settings.IsGitFlowExpandedInSideBar = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<Models.GitFlowBranchGroup> GitFlowBranchGroups
        {
            get => _gitFlowGroups;
            set => SetProperty(ref _gitFlowGroups, value);
        }

        public List<Models.Branch> GitFlowBranches
        {
            get => _gitFlowBranches;
            set => SetProperty(ref _gitFlowBranches, value);
        }

        public bool IsSortingLocalBranchByName
        {
            get => _settings.LocalBranchSortMode == Models.BranchSortMode.Name;
            set
            {
                _settings.LocalBranchSortMode = value ? Models.BranchSortMode.Name : Models.BranchSortMode.CommitterDate;
                OnPropertyChanged();

                var builder = BuildBranchTree(_branches, _remotes);
                LocalBranchTrees = builder.Locals;
                RemoteBranchTrees = builder.Remotes;
            }
        }

        public bool IsSortingRemoteBranchByName
        {
            get => _settings.RemoteBranchSortMode == Models.BranchSortMode.Name;
            set
            {
                _settings.RemoteBranchSortMode = value ? Models.BranchSortMode.Name : Models.BranchSortMode.CommitterDate;
                OnPropertyChanged();

                var builder = BuildBranchTree(_branches, _remotes);
                LocalBranchTrees = builder.Locals;
                RemoteBranchTrees = builder.Remotes;
            }
        }

        public bool IsSortingTagsByName
        {
            get => _settings.TagSortMode == Models.TagSortMode.Name;
            set
            {
                _settings.TagSortMode = value ? Models.TagSortMode.Name : Models.TagSortMode.CreatorDate;
                OnPropertyChanged();
                VisibleTags = BuildVisibleTags();
            }
        }

        public InProgressContext InProgressContext
        {
            get => _workingCopy?.InProgressContext;
        }

        public Models.BisectState BisectState
        {
            get => _bisectState;
            private set => SetProperty(ref _bisectState, value);
        }

        public bool IsBisectCommandRunning
        {
            get => _isBisectCommandRunning;
            private set => SetProperty(ref _isBisectCommandRunning, value);
        }

        public bool IsAutoFetching
        {
            get => _isAutoFetching;
            private set => SetProperty(ref _isAutoFetching, value);
        }

        public int CommitDetailActivePageIndex
        {
            get;
            set;
        } = 0;

        public AvaloniaList<Models.IssueTracker> IssueTrackers
        {
            get;
            private set;
        } = new AvaloniaList<Models.IssueTracker>();

        public AvaloniaList<CommandLog> Logs
        {
            get;
            private set;
        } = new AvaloniaList<CommandLog>();

        public Repository(bool isBare, string path, string gitDir)
        {
            IsBare = isBare;
            FullPath = path;
            GitDir = gitDir;
            MemoryMetrics = new MemoryMetrics();
            BranchCounter = new Models.BranchCounter();
            CommitStats = new Models.CommitStatistics();

            var commonDirFile = Path.Combine(_gitDir, "commondir");
            _isWorktree = _gitDir.Replace('\\', '/').IndexOf("/worktrees/", StringComparison.Ordinal) > 0 &&
                File.Exists(commonDirFile);

            if (_isWorktree)
            {
                var commonDir = File.ReadAllText(commonDirFile).Trim();
                if (!Path.IsPathRooted(commonDir))
                    commonDir = new DirectoryInfo(Path.Combine(_gitDir, commonDir)).FullName;

                _gitCommonDir = commonDir;
            }
            else
            {
                _gitCommonDir = _gitDir;
            }
        }

        public void Open()
        {
            var settingsFile = Path.Combine(_gitCommonDir, "sourcegit.settings");
            if (File.Exists(settingsFile))
            {
                try
                {
                    using var stream = File.OpenRead(settingsFile);
                    _settings = JsonSerializer.Deserialize(stream, JsonCodeGen.Default.RepositorySettings);
                }
                catch
                {
                    _settings = new Models.RepositorySettings();
                }
            }
            else
            {
                _settings = new Models.RepositorySettings();
            }

            try
            {
                _watcher = new Models.Watcher(this, _fullpath, _gitCommonDir);
            }
            catch (Exception ex)
            {
                App.RaiseException(string.Empty, $"Failed to start watcher for repository: '{_fullpath}'. You may need to press 'F5' to refresh repository manually!\n\nReason: {ex.Message}");
            }

            if (_settings.HistoriesFilters.Count > 0)
                _historiesFilterMode = _settings.HistoriesFilters[0].Mode;
            else
                _historiesFilterMode = Models.FilterMode.None;

            _histories = new Histories(this);
            _workingCopy = new WorkingCopy(this);
            _stashesPage = new StashesPage(this);
            _selectedView = _histories;
            _selectedViewIndex = 0;

            _workingCopy.CommitMessage = _settings.LastCommitMessage;
            _lastFetchTime = DateTime.Now;
            _autoFetchTimer = new Timer(AutoFetchInBackground, null, 5000, 5000);
            RefreshAll();
        }

        /// <summary>
        /// Cancels all pending background operations
        /// </summary>
        public void CancelPendingOperations()
        {
            // Cancel all pending background operations
            _operationsCancellationTokenSource?.Cancel();
        }

        public void Close()
        {
            SelectedView = null; // Do NOT modify. Used to remove exists widgets for GC.Collect

            // Cancel any pending operations before cleanup
            CancelPendingOperations();

            // Dispose of MemoryMetrics
            _memoryMetrics?.Dispose();

            // Clear cache for this repository to free memory
            ClearGraphCacheForRepository();
            _memoryMetrics = null;

            // Clear all observable collections first to release references
            Logs.Clear();
            _branches?.Clear();
            _remotes?.Clear();
            _tags?.Clear();
            _submodules?.Clear();
            _worktrees?.Clear();
            _stashesPage?.Stashes?.Clear();
            _localBranchTrees?.Clear();
            _remoteBranchTrees?.Clear();

            if (!_isWorktree)
            {
                _settings.LastCommitMessage = _workingCopy?.CommitMessage ?? string.Empty;
                using var stream = File.Create(Path.Combine(_gitCommonDir, "sourcegit.settings"));
                JsonSerializer.Serialize(stream, _settings, JsonCodeGen.Default.RepositorySettings);
            }

            // Dispose timers and watchers first
            _autoFetchTimer?.Dispose();
            _autoFetchTimer = null;

            _watcher?.Dispose();
            _watcher = null;

            // Dispose cancellation token source
            _operationsCancellationTokenSource?.Dispose();
            _operationsCancellationTokenSource = null;

            _settings = null;
            _historiesFilterMode = Models.FilterMode.None;

            // Dispose view models
            _histories?.Dispose();
            _workingCopy?.Dispose();
            _stashesPage?.Dispose();

            _histories = null;
            _workingCopy = null;
            _stashesPage = null;

            _localChangesCount = 0;
            _stashesCount = 0;

            // Force garbage collection to clean up immediately
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            _remotes.Clear();
            _branches.Clear();
            _localBranchTrees.Clear();
            _remoteBranchTrees.Clear();
            _tags.Clear();
            _visibleTags = null;
            _submodules.Clear();
            _visibleSubmodules = null;
            _searchedCommits.Clear();
            _selectedSearchedCommit = null;

            _requestingWorktreeFiles = false;
            _worktreeFiles = null;
            _matchedFilesForSearching = null;
        }

        public bool CanCreatePopup()
        {
            var page = GetOwnerPage();
            if (page == null)
                return false;

            return !_isAutoFetching && page.CanCreatePopup();
        }

        public void ShowPopup(Popup popup)
        {
            var page = GetOwnerPage();
            if (page != null)
                page.Popup = popup;
        }

        public async Task ShowAndStartPopupAsync(Popup popup)
        {
            var page = GetOwnerPage();
            page.Popup = popup;

            if (popup.CanStartDirectly())
                await page.ProcessPopupAsync();
        }

        public bool IsGitFlowEnabled()
        {
            return GitFlow != null &&
                GitFlow.IsValid &&
                _branches != null &&
                _branches.Find(x => x.IsLocal && x.Name.Equals(GitFlow.Master, StringComparison.Ordinal)) != null &&
                _branches.Find(x => x.IsLocal && x.Name.Equals(GitFlow.Develop, StringComparison.Ordinal)) != null;
        }

        public Models.GitFlowBranchType GetGitFlowType(Models.Branch b)
        {
            if (!IsGitFlowEnabled())
                return Models.GitFlowBranchType.None;

            var name = b.Name;
            if (name.StartsWith(GitFlow.FeaturePrefix, StringComparison.Ordinal))
                return Models.GitFlowBranchType.Feature;
            if (name.StartsWith(GitFlow.ReleasePrefix, StringComparison.Ordinal))
                return Models.GitFlowBranchType.Release;
            if (name.StartsWith(GitFlow.HotfixPrefix, StringComparison.Ordinal))
                return Models.GitFlowBranchType.Hotfix;
            return Models.GitFlowBranchType.None;
        }

        public bool IsLFSEnabled()
        {
            var path = Path.Combine(_fullpath, ".git", "hooks", "pre-push");
            if (!File.Exists(path))
                return false;

            var content = File.ReadAllText(path);
            return content.Contains("git lfs pre-push");
        }

        // LFS operations moved to separate service class if needed
        public async Task InstallLFSAsync()
        {
            var log = CreateLog("Install LFS");
            var succ = await new Commands.LFS(_fullpath).Use(log).InstallAsync();
            if (succ)
                App.SendNotification(_fullpath, "LFS enabled successfully!");

            log.Complete();
        }

        public async Task<bool> TrackLFSFileAsync(string pattern, bool isFilenameMode)
        {
            var log = CreateLog("Track LFS");
            var succ = await new Commands.LFS(_fullpath)
                .Use(log)
                .TrackAsync(pattern, isFilenameMode);

            if (succ)
                App.SendNotification(_fullpath, $"Tracking successfully! Pattern: {pattern}");

            log.Complete();
            return succ;
        }

        public async Task<bool> LockLFSFileAsync(string remote, string path)
        {
            var log = CreateLog("Lock LFS File");
            var succ = await new Commands.LFS(_fullpath)
                .Use(log)
                .LockAsync(remote, path);

            if (succ)
                App.SendNotification(_fullpath, $"Lock file successfully! File: {path}");

            log.Complete();
            return succ;
        }

        public async Task<bool> UnlockLFSFileAsync(string remote, string path, bool force, bool notify)
        {
            var log = CreateLog("Unlock LFS File");
            var succ = await new Commands.LFS(_fullpath)
                .Use(log)
                .UnlockAsync(remote, path, force);

            if (succ && notify)
                App.SendNotification(_fullpath, $"Unlock file successfully! File: {path}");

            log.Complete();
            return succ;
        }

        public CommandLog CreateLog(string name)
        {
            var log = new CommandLog(name);
            Logs.Insert(0, log);
            return log;
        }

        public async Task<Models.IssueTracker> AddIssueTrackerAsync(string name, string regex, string url)
        {
            var rule = new Models.IssueTracker()
            {
                IsShared = false,
                Name = name,
                RegexString = regex,
                URLTemplate = url,
            };

            var succ = await CreateIssueTrackerCommand(false).AddAsync(rule);
            if (succ)
            {
                IssueTrackers.Add(rule);
                return rule;
            }

            return null;
        }

        public async Task RemoveIssueTrackerAsync(Models.IssueTracker rule)
        {
            var succ = await CreateIssueTrackerCommand(rule.IsShared).RemoveAsync(rule);
            if (succ)
                IssueTrackers.Remove(rule);
        }

        public async Task ChangeIssueTrackerShareModeAsync(Models.IssueTracker rule)
        {
            await CreateIssueTrackerCommand(!rule.IsShared).RemoveAsync(rule);
            await CreateIssueTrackerCommand(rule.IsShared).AddAsync(rule);
        }

        // All refresh methods moved to Repository.Refresh.cs partial class
        // All Git operations moved to Repository.GitOperations.cs partial class  
        // All search operations moved to Repository.Search.cs partial class

        public void SetWatcherEnabled(bool enabled)
        {
            _watcher?.SetEnabled(enabled);
        }

        public void MarkBranchesDirtyManually()
        {
            // Invalidate branch cache when branches are manually marked dirty (e.g., after create/delete operations)
            Commands.Optimization.GitCommandCache.Instance.InvalidateByOperation(_fullpath, Commands.Optimization.GitOperation.BranchCreate);

            if (_watcher == null)
            {
                RefreshBranches();
                RefreshCommits();
                RefreshWorkingCopyChanges();
                RefreshWorktrees();
            }
            else
            {
                _watcher.MarkBranchDirtyManually();
            }
        }

        public void MarkTagsDirtyManually()
        {
            if (_watcher == null)
            {
                RefreshTags();
                RefreshCommits();
            }
            else
            {
                _watcher.MarkTagDirtyManually();
            }
        }

        public void MarkWorkingCopyDirtyManually()
        {
            if (_watcher == null)
                RefreshWorkingCopyChanges();
            else
                _watcher.MarkWorkingCopyDirtyManually();
        }

        public void MarkFetched()
        {
            _lastFetchTime = DateTime.Now;
        }

        public void ClearCommitMessage()
        {
            if (_workingCopy is not null)
                _workingCopy.CommitMessage = string.Empty;
        }

        public Models.Commit GetSelectedCommitInHistory()
        {
            return (_histories?.DetailContext as CommitDetail)?.Commit;
        }

        public void UpdateBranchNodeIsExpanded(BranchTreeNode node)
        {
            if (_settings == null || !string.IsNullOrWhiteSpace(_filter))
                return;

            if (node.IsExpanded)
            {
                if (!_settings.ExpandedBranchNodesInSideBar.Contains(node.Path))
                    _settings.ExpandedBranchNodesInSideBar.Add(node.Path);
            }
            else
            {
                _settings.ExpandedBranchNodesInSideBar.Remove(node.Path);
            }
        }

        public List<(Models.CustomAction, CustomActionContextMenuLabel)> GetCustomActions(Models.CustomActionScope scope)
        {
            var actions = new List<(Models.CustomAction, CustomActionContextMenuLabel)>();

            foreach (var act in Preferences.Instance.CustomActions)
            {
                if (act.Scope == scope)
                    actions.Add((act, new CustomActionContextMenuLabel(act.Name, true)));
            }

            foreach (var act in _settings.CustomActions)
            {
                if (act.Scope == scope)
                    actions.Add((act, new CustomActionContextMenuLabel(act.Name, false)));
            }

            return actions;
        }

        public bool MayHaveSubmodules()
        {
            var modulesFile = Path.Combine(_fullpath, ".gitmodules");
            var info = new FileInfo(modulesFile);
            return info.Exists && info.Length > 20;
        }

        public void NotifySettingsChanged()
        {
            OnPropertyChanged(nameof(Settings));
        }

        // GitFlow UI methods
        public void StartGitFlowBranch(Models.GitFlowBranchType type)
        {
            if (!IsGitFlowEnabled())
            {
                if (CanCreatePopup())
                    ShowPopup(new InitGitFlow(this));
                return;
            }

            if (CanCreatePopup())
                ShowPopup(new GitFlowStart(this, type));
        }

        public void FinishGitFlowBranch(Models.Branch branch)
        {
            if (!IsGitFlowEnabled())
            {
                App.RaiseException(_fullpath, "Git-Flow is not configured for this repository");
                return;
            }

            var type = GetGitFlowType(branch);
            if (type == Models.GitFlowBranchType.None ||
                type == Models.GitFlowBranchType.Master ||
                type == Models.GitFlowBranchType.Develop)
            {
                App.RaiseException(_fullpath, "This branch cannot be finished using Git-Flow");
                return;
            }

            if (CanCreatePopup())
                ShowPopup(new GitFlowFinish(this, branch, type));
        }

        public void InitializeGitFlow()
        {
            if (CanCreatePopup())
                ShowPopup(new InitGitFlow(this));
        }

        public void StartGitFlowFeature()
        {
            StartGitFlowBranch(Models.GitFlowBranchType.Feature);
        }

        public void StartGitFlowRelease()
        {
            StartGitFlowBranch(Models.GitFlowBranchType.Release);
        }

        public void StartGitFlowHotfix()
        {
            StartGitFlowBranch(Models.GitFlowBranchType.Hotfix);
        }
        public ContextMenu CreateContextMenuForGitFlowBranch(Models.Branch branch)
        {
            if (branch == null)
                return null;

            var menu = new ContextMenu();

            // Checkout
            var checkout = new MenuItem();
            checkout.Header = App.Text("BranchCM.Checkout", branch.Name);
            checkout.Icon = App.CreateMenuIcon("Icons.Check");
            checkout.IsEnabled = !branch.IsCurrent;
            checkout.Click += async (_, e) =>
            {
                await CheckoutBranchAsync(branch);
                e.Handled = true;
            };
            menu.Items.Add(checkout);

            // Finish GitFlow branch
            var type = GetGitFlowType(branch);
            if (type != Models.GitFlowBranchType.None &&
                type != Models.GitFlowBranchType.Master &&
                type != Models.GitFlowBranchType.Develop)
            {
                var finish = new MenuItem();
                finish.Header = App.Text("GitFlow.FinishBranch");
                finish.Icon = App.CreateMenuIcon("Icons.GitFlow");
                finish.Click += (_, e) =>
                {
                    FinishGitFlowBranch(branch);
                    e.Handled = true;
                };
                menu.Items.Add(finish);
            }

            menu.Items.Add(new MenuItem() { Header = "-" });

            // Merge
            var merge = new MenuItem();
            merge.Header = App.Text("BranchCM.Merge", branch.Name, _currentBranch.Name);
            merge.Icon = App.CreateMenuIcon("Icons.Merge");
            merge.IsEnabled = !branch.IsCurrent;
            merge.Click += (_, e) =>
            {
                if (CanCreatePopup())
                    ShowPopup(new Merge(this, branch, _currentBranch.Name, false));
                e.Handled = true;
            };
            menu.Items.Add(merge);

            // Delete
            var delete = new MenuItem();
            delete.Header = App.Text("BranchCM.Delete", branch.Name);
            delete.Icon = App.CreateMenuIcon("Icons.Clear");
            delete.IsEnabled = !branch.IsCurrent;
            delete.Click += (_, e) =>
            {
                if (CanCreatePopup())
                    ShowPopup(new DeleteBranch(this, branch));
                e.Handled = true;
            };
            menu.Items.Add(delete);
            return menu;
        }

        // Remote management methods (could be moved to separate partial class if needed)
        public void AddRemote()
        {
            if (CanCreatePopup())
                ShowPopup(new AddRemote(this));
        }

        public void DeleteRemote(Models.Remote remote)
        {
            if (CanCreatePopup())
                ShowPopup(new DeleteRemote(this, remote));
        }

        // Submodule management methods (could be moved to separate partial class if needed)
        public void AddSubmodule()
        {
            if (CanCreatePopup())
                ShowPopup(new AddSubmodule(this));
        }

        public void UpdateSubmodules()
        {
            if (CanCreatePopup())
                ShowPopup(new UpdateSubmodules(this, null));
        }

        public void OpenSubmodule(string submodule)
        {
            var selfPage = GetOwnerPage();
            if (selfPage == null)
                return;

            var root = Path.GetFullPath(Path.Combine(_fullpath, submodule));
            var normalizedPath = root.Replace('\\', '/').TrimEnd('/');

            var node = Preferences.Instance.FindNode(normalizedPath) ??
                new RepositoryNode
                {
                    Id = normalizedPath,
                    Name = Path.GetFileName(normalizedPath),
                    Bookmark = selfPage.Node.Bookmark,
                    IsRepository = true,
                };

            App.GetLauncher().OpenRepositoryInTab(node, null);
        }

        // Worktree management methods (could be moved to separate partial class if needed)
        public void AddWorktree()
        {
            if (CanCreatePopup())
                ShowPopup(new AddWorktree(this));
        }

        public async Task PruneWorktreesAsync()
        {
            if (CanCreatePopup())
                await ShowAndStartPopupAsync(new PruneWorktrees(this));
        }

        public void OpenWorktree(Models.Worktree worktree)
        {
            var node = Preferences.Instance.FindNode(worktree.FullPath) ??
                new RepositoryNode
                {
                    Id = worktree.FullPath,
                    Name = Path.GetFileName(worktree.FullPath),
                    Bookmark = 0,
                    IsRepository = true,
                };

            App.GetLauncher().OpenRepositoryInTab(node, null);
        }

        public async Task LockWorktreeAsync(Models.Worktree worktree)
        {
            SetWatcherEnabled(false);
            var log = CreateLog("Lock Worktree");
            var succ = await new Commands.Worktree(_fullpath).Use(log).LockAsync(worktree.FullPath);
            if (succ)
                worktree.IsLocked = true;
            log.Complete();
            SetWatcherEnabled(true);
        }

        public async Task UnlockWorktreeAsync(Models.Worktree worktree)
        {
            SetWatcherEnabled(false);
            var log = CreateLog("Unlock Worktree");
            var succ = await new Commands.Worktree(_fullpath).Use(log).UnlockAsync(worktree.FullPath);
            if (succ)
                worktree.IsLocked = false;
            log.Complete();
            SetWatcherEnabled(true);
        }

        public List<Models.OpenAIService> GetPreferredOpenAIServices()
        {
            var services = Preferences.Instance.OpenAIServices;
            if (services == null || services.Count == 0)
                return [];

            if (services.Count == 1)
                return [services[0]];

            var preferred = _settings.PreferredOpenAIService;
            var all = new List<Models.OpenAIService>();
            foreach (var service in services)
            {
                if (service.Name.Equals(preferred, StringComparison.Ordinal))
                    return [service];

                all.Add(service);
            }

            return all;
        }

        #region Helper Methods

        private LauncherPage GetOwnerPage()
        {
            var launcher = App.GetLauncher();
            if (launcher == null)
                return null;

            foreach (var page in launcher.Pages)
            {
                if (page.Node.Id.Equals(_fullpath))
                    return page;
            }

            return null;
        }

        private Commands.IssueTracker CreateIssueTrackerCommand(bool shared)
        {
            return new Commands.IssueTracker(_fullpath, shared ? $"{_fullpath}/.issuetracker" : null);
        }

        private BranchTreeNode.Builder BuildBranchTree(List<Models.Branch> branches, List<Models.Remote> remotes)
        {
            var builder = new BranchTreeNode.Builder(_settings.LocalBranchSortMode, _settings.RemoteBranchSortMode);
            if (string.IsNullOrEmpty(_filter))
            {
                builder.SetExpandedNodes(_settings.ExpandedBranchNodesInSideBar);
                builder.Run(branches, remotes, false);

                foreach (var invalid in builder.InvalidExpandedNodes)
                    _settings.ExpandedBranchNodesInSideBar.Remove(invalid);
            }
            else
            {
                var visibles = new List<Models.Branch>();
                foreach (var b in branches)
                {
                    if (b.FullName.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                        visibles.Add(b);
                }

                builder.Run(visibles, remotes, true);
            }

            var historiesFilters = _settings.CollectHistoriesFilters();
            UpdateBranchTreeFilterMode(builder.Locals, historiesFilters);
            UpdateBranchTreeFilterMode(builder.Remotes, historiesFilters);
            return builder;
        }

        private object BuildVisibleTags()
        {
            switch (_settings.TagSortMode)
            {
                case Models.TagSortMode.CreatorDate:
                    _tags.Sort((l, r) => r.CreatorDate.CompareTo(l.CreatorDate));
                    break;
                default:
                    _tags.Sort((l, r) => Models.NumericSort.Compare(l.Name, r.Name));
                    break;
            }

            var visible = new List<Models.Tag>();
            if (string.IsNullOrEmpty(_filter))
            {
                visible.AddRange(_tags);
            }
            else
            {
                foreach (var t in _tags)
                {
                    if (t.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                        visible.Add(t);
                }
            }

            var historiesFilters = _settings.CollectHistoriesFilters();
            UpdateTagFilterMode(historiesFilters);

            if (Preferences.Instance.ShowTagsAsTree)
            {
                var tree = TagCollectionAsTree.Build(visible, _visibleTags as TagCollectionAsTree);
                foreach (var node in tree.Tree)
                    node.UpdateFilterMode(historiesFilters);
                return tree;
            }
            else
            {
                var list = new TagCollectionAsList(visible);
                foreach (var item in list.TagItems)
                    item.FilterMode = historiesFilters.GetValueOrDefault(item.Tag.Name, Models.FilterMode.None);
                return list;
            }
        }

        private object BuildVisibleSubmodules()
        {
            var visible = new List<Models.Submodule>();
            if (string.IsNullOrEmpty(_filter))
            {
                visible.AddRange(_submodules);
            }
            else
            {
                foreach (var s in _submodules)
                {
                    if (s.Path.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                        visible.Add(s);
                }
            }

            if (Preferences.Instance.ShowSubmodulesAsTree)
                return SubmoduleCollectionAsTree.Build(visible, _visibleSubmodules as SubmoduleCollectionAsTree);
            else
                return new SubmoduleCollectionAsList() { Submodules = visible };
        }

        // UpdateBranchTreeFilterMode moved to Repository.Search.cs partial class

        // UpdateTagFilterMode moved to Repository.Search.cs partial class

        private void ClearGraphCacheForRepository()
        {
            // Clear cache entries specific to this repository
            // Since we're using a static cache, we can't easily remove just this repo's entries
            // But we can trim excess to free memory
            _graphCache.TrimExcess();

            // If this is causing memory pressure, clear more aggressively
            var stats = _graphCache.GetStatistics();
            if (stats.MemoryUsagePercent > 80)
            {
                _graphCache.Clear();
            }
        }

        private void AutoFetchInBackground(object sender)
        {
            Dispatcher.UIThread.Post(async () =>
            {
                if (_settings == null || !_settings.EnableAutoFetch)
                    return;

                if (!CanCreatePopup())
                {
                    _lastFetchTime = DateTime.Now;
                    return;
                }

                var lockFile = Path.Combine(_gitDir, "index.lock");
                if (File.Exists(lockFile))
                    return;

                var now = DateTime.Now;
                var desire = _lastFetchTime.AddMinutes(_settings.AutoFetchInterval);
                if (desire > now)
                    return;

                IsAutoFetching = true;

                var remotes = new List<string>();
                foreach (var r in _remotes)
                    remotes.Add(r.Name);

                if (_settings.FetchAllRemotes)
                {
                    foreach (var remote in remotes)
                        await new Commands.Fetch(_fullpath, remote, false, false) { RaiseError = false }.RunAsync();
                }
                else if (remotes.Count > 0)
                {
                    var remote = string.IsNullOrEmpty(_settings.DefaultRemote) ?
                        remotes.Find(x => x.Equals(_settings.DefaultRemote, StringComparison.Ordinal)) :
                        remotes[0];

                    await new Commands.Fetch(_fullpath, remote, false, false) { RaiseError = false }.RunAsync();
                }

                _lastFetchTime = DateTime.Now;
                IsAutoFetching = false;
            });
        }

        #endregion

        // Private fields moved to Repository.State.cs partial class
        // Cache management methods moved to Repository.State.cs partial class
    }
}
