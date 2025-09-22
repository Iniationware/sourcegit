using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace SourceGit.ViewModels
{
    /// <summary>
    /// Repository refresh operations - handles all data refresh functionality
    /// </summary>
    public partial class Repository
    {
        #region Public Refresh Methods

        /// <summary>
        /// Refreshes repository data based on the provided options.
        /// This method reduces code duplication across Push, Pull, Fetch, and other operations.
        /// </summary>
        /// <param name="options">Options specifying which parts to refresh</param>
        public async Task RefreshAfterOperation(Models.RefreshOptions options)
        {
            if (options == null)
                return;

            // Run background refreshes in parallel
            var tasks = new List<Task>();

            if (options.RefreshBranches)
            {
                tasks.Add(Task.Run(() =>
                {
                    Models.PerformanceMonitor.StartTimer("RefreshBranches");
                    RefreshBranches();
                    Models.PerformanceMonitor.StopTimer("RefreshBranches");
                }));
            }

            if (options.RefreshTags)
            {
                tasks.Add(Task.Run(() =>
                {
                    Models.PerformanceMonitor.StartTimer("RefreshTags");
                    RefreshTags();
                    Models.PerformanceMonitor.StopTimer("RefreshTags");
                }));
            }

            if (options.RefreshWorkingCopy)
            {
                tasks.Add(Task.Run(() =>
                {
                    Models.PerformanceMonitor.StartTimer("RefreshWorkingCopyChanges");
                    RefreshWorkingCopyChanges();
                    Models.PerformanceMonitor.StopTimer("RefreshWorkingCopyChanges");
                }));
            }

            if (options.RefreshStashes)
            {
                tasks.Add(Task.Run(() =>
                {
                    Models.PerformanceMonitor.StartTimer("RefreshStashes");
                    RefreshStashes();
                    Models.PerformanceMonitor.StopTimer("RefreshStashes");
                }));
            }

            if (options.RefreshSubmodules)
            {
                tasks.Add(Task.Run(() =>
                {
                    Models.PerformanceMonitor.StartTimer("RefreshSubmodules");
                    RefreshSubmodules();
                    Models.PerformanceMonitor.StopTimer("RefreshSubmodules");
                }));
            }

            // Wait for all parallel tasks to complete
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }

            // RefreshCommits should run after other refreshes as it may depend on their data
            if (options.RefreshCommits)
            {
                Models.PerformanceMonitor.StartTimer("RefreshCommits");
                RefreshCommits();
                Models.PerformanceMonitor.StopTimer("RefreshCommits");
            }
        }

        /// <summary>
        /// Refreshes all repository data
        /// </summary>
        public void RefreshAll()
        {
            Models.PerformanceMonitor.StartTimer("RefreshAll");

            // RefreshCommits must run first as other operations may depend on commit data
            Models.PerformanceMonitor.StartTimer("RefreshCommits");
            RefreshCommits();
            Models.PerformanceMonitor.StopTimer("RefreshCommits");

            // Run independent read-only operations in parallel for better multi-core utilization
            var parallelTasks = new List<Task>
            {
                Task.Run(() => {
                    Models.PerformanceMonitor.StartTimer("RefreshBranches");
                    RefreshBranches();
                    Models.PerformanceMonitor.StopTimer("RefreshBranches");
                }),
                Task.Run(() => {
                    Models.PerformanceMonitor.StartTimer("RefreshTags");
                    RefreshTags();
                    Models.PerformanceMonitor.StopTimer("RefreshTags");
                }),
                Task.Run(() => {
                    Models.PerformanceMonitor.StartTimer("RefreshSubmodules");
                    RefreshSubmodules();
                    Models.PerformanceMonitor.StopTimer("RefreshSubmodules");
                }),
                Task.Run(() => {
                    Models.PerformanceMonitor.StartTimer("RefreshWorktrees");
                    RefreshWorktrees();
                    Models.PerformanceMonitor.StopTimer("RefreshWorktrees");
                }),
                Task.Run(() => {
                    Models.PerformanceMonitor.StartTimer("RefreshWorkingCopyChanges");
                    RefreshWorkingCopyChanges();
                    Models.PerformanceMonitor.StopTimer("RefreshWorkingCopyChanges");
                }),
                Task.Run(() => {
                    Models.PerformanceMonitor.StartTimer("RefreshStashes");
                    RefreshStashes();
                    Models.PerformanceMonitor.StopTimer("RefreshStashes");
                })
            };

            // Fire and forget - these will complete asynchronously
            Task.WhenAll(parallelTasks).ContinueWith(async t =>
            {
                if (t.IsFaulted)
                {
                    // Log any exceptions from parallel tasks
                    foreach (var task in parallelTasks)
                    {
                        if (task.IsFaulted)
                            App.LogException(task.Exception?.GetBaseException());
                    }
                }
                else
                {
                    var totalTime = Models.PerformanceMonitor.StopTimer("RefreshAll");
                    System.Diagnostics.Debug.WriteLine($"[PERF] RefreshAll completed in {totalTime}ms");

                    // Log summary periodically
                    if (totalTime > 0 && Models.PerformanceMonitor.GetAverageTime("RefreshAll") > 0)
                    {
                        var summary = Models.PerformanceMonitor.GetPerformanceSummary();
                        System.Diagnostics.Debug.WriteLine(summary);
                    }

                    // Load GitFlow configuration after branches are loaded
                    await LoadGitFlowConfigAsync();

                    // Update GitFlow branches if enabled
                    if (_settings != null && _settings.ShowGitFlowInSidebar && _branches != null)
                    {
                        var localBranches = _branches.Where(b => b.IsLocal && !b.IsDetachedHead).ToList();
                        ExecuteOnUIThread(() =>
                        {
                            UpdateGitFlowBranches(localBranches);
                        });
                    }
                }
            });

            Task.Run(async () =>
            {
                await LoadIssueTrackersAsync();
            });
        }

        #endregion

        #region Individual Refresh Methods

        /// <summary>
        /// Refreshes branch and remote information
        /// </summary>
        public void RefreshBranches()
        {
            Task.Run(async () =>
            {
                try
                {
                    // Use optimized query with caching and batch execution
                    var branches = await new Commands.QueryBranchesOptimized(_fullpath).GetResultAsync().ConfigureAwait(false);
                    var remotes = await new Commands.QueryRemotes(_fullpath).GetResultAsync().ConfigureAwait(false);

                    // Validate results before proceeding
                    if (branches == null)
                    {
                        App.RaiseException(_fullpath, "QueryBranchesOptimized returned null, using empty list");
                        branches = new List<Models.Branch>();
                    }
                    if (remotes == null)
                    {
                        App.RaiseException(_fullpath, "QueryRemotes returned null, using empty list");
                        remotes = new List<Models.Remote>();
                    }

                    var builder = BuildBranchTree(branches, remotes);

                    ExecuteOnUIThread(() =>
                    {
                        Remotes = remotes;
                        Branches = branches;
                        CurrentBranch = branches.Find(x => x.IsCurrent);
                        LocalBranchTrees = builder.Locals;
                        RemoteBranchTrees = builder.Remotes;

                        var localBranchesCount = 0;
                        var localBranches = new List<Models.Branch>();
                        foreach (var b in branches)
                        {
                            if (b.IsLocal && !b.IsDetachedHead)
                            {
                                localBranchesCount++;
                                localBranches.Add(b);
                            }
                        }
                        LocalBranchesCount = localBranchesCount;

                        // Update branch counter
                        if (_branchCounter != null)
                        {
                            _branchCounter.UpdateFromRepository(_fullpath);
                        }

                        // Update commit statistics
                        if (_commitStats != null)
                        {
                            _commitStats.UpdateFromRepository(_fullpath);
                        }

                        // Check and auto-configure GitFlow if structure is detected
                        CheckAndAutoConfigureGitFlow(localBranches);
                    });

                    // Load GitFlow configuration after auto-configuration
                    await LoadGitFlowConfigAsync().ConfigureAwait(false);

                    ExecuteOnUIThread(() =>
                    {
                        // Get local branches again for GitFlow update
                        var localBranches = new List<Models.Branch>();
                        foreach (var b in branches)
                        {
                            if (b.IsLocal && !b.IsDetachedHead)
                            {
                                localBranches.Add(b);
                            }
                        }

                        // Update GitFlow branches only if GitFlow display is enabled
                        if (_settings != null && _settings.ShowGitFlowInSidebar)
                        {
                            UpdateGitFlowBranches(localBranches);
                        }
                        UpdateWorkingCopyRemotesInfo(remotes);
                        UpdatePendingPullPushState();
                    });
                }
                catch (Exception ex)
                {
                    // Log the detailed error to help diagnose the issue
                    App.RaiseException(_fullpath, $"Failed to refresh branches: {ex.Message}\n\nStack trace:\n{ex.StackTrace}");

                    ExecuteOnUIThread(() =>
                    {
                        // Keep existing data if refresh fails
                        if (Branches == null)
                            Branches = new List<Models.Branch>();
                        if (Remotes == null)
                            Remotes = new List<Models.Remote>();
                    });
                }
            });
        }

        /// <summary>
        /// Refreshes worktree information
        /// </summary>
        public void RefreshWorktrees()
        {
            Task.Run(async () =>
            {
                var worktrees = await new Commands.Worktree(_fullpath).ReadAllAsync().ConfigureAwait(false);
                if (worktrees.Count > 0)
                {
                    var cleaned = new List<Models.Worktree>();
                    var normalizedGitDir = _gitDir.Replace('\\', '/');

                    foreach (var worktree in worktrees)
                    {
                        if (worktree.FullPath.Equals(_fullpath, StringComparison.Ordinal) ||
                            worktree.FullPath.Equals(normalizedGitDir, StringComparison.Ordinal))
                            continue;

                        cleaned.Add(worktree);
                    }

                    ExecuteOnUIThread(() => Worktrees = cleaned);
                }
                else
                {
                    ExecuteOnUIThread(() => Worktrees = worktrees);
                }
            });
        }

        /// <summary>
        /// Refreshes tag information
        /// </summary>
        public void RefreshTags()
        {
            Task.Run(async () =>
            {
                var tags = await new Commands.QueryTags(_fullpath).GetResultAsync().ConfigureAwait(false);
                ExecuteOnUIThread(() =>
                {
                    Tags = tags;
                    VisibleTags = BuildVisibleTags();
                });
            });
        }

        /// <summary>
        /// Refreshes commit history with optimized async patterns and progress feedback
        /// </summary>
        public void RefreshCommits()
        {
            Task.Run(async () =>
            {
                try
                {
                    await Dispatcher.UIThread.InvokeAsync(() => _histories.IsLoading = true);

                    var builder = new StringBuilder();
                    builder.Append($"-{Preferences.Instance.MaxHistoryCommits} ");

                    if (_settings.EnableTopoOrderInHistories)
                        builder.Append("--topo-order ");
                    else
                        builder.Append("--date-order ");

                    if (_settings.HistoryShowFlags.HasFlag(Models.HistoryShowFlags.Reflog))
                        builder.Append("--reflog ");

                    if (_settings.HistoryShowFlags.HasFlag(Models.HistoryShowFlags.FirstParentOnly))
                        builder.Append("--first-parent ");

                    if (_settings.HistoryShowFlags.HasFlag(Models.HistoryShowFlags.SimplifyByDecoration))
                        builder.Append("--simplify-by-decoration ");

                    var filters = _settings.BuildHistoriesFilter();
                    if (string.IsNullOrEmpty(filters))
                        builder.Append("--branches --remotes --tags HEAD");
                    else
                        builder.Append(filters);

                    // Use the optimized QueryCommits with streaming and batching
                    var commits = await new Commands.QueryCommitsOptimized(_fullpath, builder.ToString()).GetResultAsync().ConfigureAwait(false);

                    // Create graph with enhanced progress feedback
                    var graph = await GetOrCreateCommitGraphOptimized(commits);

                    ExecuteOnUIThread(() =>
                    {
                        if (_histories != null)
                        {
                            _histories.IsLoading = false;
                            _histories.Commits = commits;
                            _histories.Graph = graph;

                            BisectState = _histories.UpdateBisectInfo();

                            if (!string.IsNullOrEmpty(_navigateToCommitDelayed))
                                NavigateToCommit(_navigateToCommitDelayed);
                        }

                        _navigateToCommitDelayed = string.Empty;
                    });
                }
                catch (Exception ex)
                {
                    App.RaiseException(_fullpath, $"Failed to refresh commits: {ex.Message}");
                    ExecuteOnUIThread(() =>
                    {
                        if (_histories != null)
                            _histories.IsLoading = false;
                    });
                }
            });
        }

        /// <summary>
        /// Refreshes submodule information
        /// </summary>
        public void RefreshSubmodules()
        {
            if (!MayHaveSubmodules())
            {
                if (_submodules.Count > 0)
                {
                    ExecuteOnUIThread(() =>
                    {
                        Submodules = new List<Models.Submodule>();
                        VisibleSubmodules = BuildVisibleSubmodules();
                    });
                }
                return;
            }

            Task.Run(async () =>
            {
                var submodules = await new Commands.QuerySubmodules(_fullpath).GetResultAsync().ConfigureAwait(false);
                _watcher?.SetSubmodules(submodules);

                ExecuteOnUIThread(() =>
                {
                    bool hasChanged = HasSubmodulesChanged(submodules);
                    if (hasChanged)
                    {
                        Submodules = submodules;
                        VisibleSubmodules = BuildVisibleSubmodules();
                    }
                });
            });
        }

        /// <summary>
        /// Refreshes working copy changes
        /// </summary>
        public void RefreshWorkingCopyChanges()
        {
            if (IsBare)
                return;

            Task.Run(async () =>
            {
                var changes = await new Commands.QueryLocalChanges(_fullpath, _settings.IncludeUntrackedInLocalChanges)
                    .GetResultAsync()
                    .ConfigureAwait(false);

                if (_workingCopy == null)
                    return;

                changes.Sort((l, r) => Models.NumericSort.Compare(l.Path, r.Path));
                _workingCopy.SetData(changes);

                ExecuteOnUIThread(() =>
                {
                    LocalChangesCount = changes.Count;
                    OnPropertyChanged(nameof(InProgressContext));
                    GetOwnerPage()?.ChangeDirtyState(Models.DirtyState.HasLocalChanges, changes.Count == 0);
                });
            });
        }

        /// <summary>
        /// Refreshes stash information
        /// </summary>
        public void RefreshStashes()
        {
            if (IsBare)
                return;

            Task.Run(async () =>
            {
                var stashes = await new Commands.QueryStashes(_fullpath).GetResultAsync().ConfigureAwait(false);
                ExecuteOnUIThread(() =>
                {
                    if (_stashesPage != null)
                        _stashesPage.Stashes = stashes;

                    StashesCount = stashes.Count;
                });
            });
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets or creates commit graph with caching and chunked loading for large repositories
        /// </summary>
        private async Task<Models.CommitGraph> GetOrCreateCommitGraph(List<Models.Commit> commits)
        {
            return await GetOrCreateCommitGraphOptimized(commits);
        }

        /// <summary>
        /// Optimized commit graph creation with enhanced performance monitoring and memory management
        /// </summary>
        private async Task<Models.CommitGraph> GetOrCreateCommitGraphOptimized(List<Models.Commit> commits)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Generate cache key based on repository state and settings
            var cacheKey = commits.Count > 0
                ? $"{_fullpath}_{commits[0].SHA}_{commits.Count}_{_settings.HistoryShowFlags}_{_currentBranch}"
                : null;

            Models.CommitGraph graph = null;
            if (cacheKey != null)
            {
                // Try to get from LRU cache
                graph = _graphCache.Get(cacheKey);
                if (graph != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[PERF] CommitGraph cache hit for {commits.Count} commits");
                    return graph;
                }
            }

            // Enhanced thresholds for better performance
            const int CHUNK_SIZE = 50;
            const int LARGE_REPO_THRESHOLD = 300;
            const int YIELD_FREQUENCY = 100;

            if (commits.Count > LARGE_REPO_THRESHOLD)
            {
                // Process large repos with progress feedback and memory optimization
                graph = await Task.Run(async () =>
                {
                    System.Diagnostics.Debug.WriteLine($"[PERF] Processing large repository with {commits.Count} commits");

                    // Use the optimized async CommitGraph.ParseAsync method
                    var tempGraph = await Models.CommitGraph.ParseAsync(commits,
                        _settings.HistoryShowFlags.HasFlag(Models.HistoryShowFlags.FirstParentOnly),
                        CHUNK_SIZE, YIELD_FREQUENCY);

                    return tempGraph;
                });
            }
            else
            {
                // Small repositories: use optimized sync method
                graph = await Task.Run(() =>
                    Models.CommitGraph.Parse(commits, _settings.HistoryShowFlags.HasFlag(Models.HistoryShowFlags.FirstParentOnly)));
            }

            if (cacheKey != null && graph != null)
            {
                _graphCache.Set(cacheKey, graph);

                // Memory management: trim cache if growing too large
                if (_graphCache.Count > 20) // Reduced from 30 for better memory usage
                {
                    _graphCache.TrimExcess();
                }
            }

            stopwatch.Stop();
            System.Diagnostics.Debug.WriteLine($"[PERF] CommitGraph creation took {stopwatch.ElapsedMilliseconds}ms for {commits.Count} commits");

            return graph;
        }

        /// <summary>
        /// Updates GitFlow branch groupings
        /// </summary>
        private void UpdateGitFlowBranches(List<Models.Branch> localBranches)
        {
            try
            {
                // Check if we should update GitFlow branches
                // Don't require IsGitFlowEnabled() here as it checks for branch existence which might not be loaded yet
                if (GitFlow != null && GitFlow.IsValid && _settings != null && _settings.ShowGitFlowInSidebar)
                {
                    var groups = new List<Models.GitFlowBranchGroup>();
                    var gitFlowBranches = new List<Models.Branch>();

                    var featureGroup = new Models.GitFlowBranchGroup { Type = Models.GitFlowBranchType.Feature, Name = "Features" };
                    var releaseGroup = new Models.GitFlowBranchGroup { Type = Models.GitFlowBranchType.Release, Name = "Releases" };
                    var hotfixGroup = new Models.GitFlowBranchGroup { Type = Models.GitFlowBranchType.Hotfix, Name = "Hotfixes" };
                    var supportGroup = new Models.GitFlowBranchGroup { Type = Models.GitFlowBranchType.Support, Name = "Support" };

                    foreach (var branch in localBranches)
                    {
                        var type = GetGitFlowTypeForBranch(branch);
                        switch (type)
                        {
                            case Models.GitFlowBranchType.Feature:
                                featureGroup.Branches.Add(branch);
                                gitFlowBranches.Add(branch);
                                break;
                            case Models.GitFlowBranchType.Release:
                                releaseGroup.Branches.Add(branch);
                                gitFlowBranches.Add(branch);
                                break;
                            case Models.GitFlowBranchType.Hotfix:
                                hotfixGroup.Branches.Add(branch);
                                gitFlowBranches.Add(branch);
                                break;
                            case Models.GitFlowBranchType.Support:
                                supportGroup.Branches.Add(branch);
                                gitFlowBranches.Add(branch);
                                break;
                        }
                    }

                    if (featureGroup.Branches.Count > 0)
                        groups.Add(featureGroup);
                    if (releaseGroup.Branches.Count > 0)
                        groups.Add(releaseGroup);
                    if (hotfixGroup.Branches.Count > 0)
                        groups.Add(hotfixGroup);
                    if (supportGroup.Branches.Count > 0)
                        groups.Add(supportGroup);

                    GitFlowBranchGroups = groups;
                    GitFlowBranches = gitFlowBranches;
                }
                else
                {
                    GitFlowBranchGroups = new List<Models.GitFlowBranchGroup>();
                    GitFlowBranches = new List<Models.Branch>();
                }
            }
            catch
            {
                // Ignore GitFlow errors to prevent app crashes
                GitFlowBranchGroups = new List<Models.GitFlowBranchGroup>();
                GitFlowBranches = new List<Models.Branch>();
            }
        }

        private Models.GitFlowBranchType GetGitFlowTypeForBranch(Models.Branch b)
        {
            if (GitFlow == null || !GitFlow.IsValid)
                return Models.GitFlowBranchType.None;

            var name = b.Name;
            if (name.StartsWith(GitFlow.FeaturePrefix, StringComparison.Ordinal))
                return Models.GitFlowBranchType.Feature;
            if (name.StartsWith(GitFlow.ReleasePrefix, StringComparison.Ordinal))
                return Models.GitFlowBranchType.Release;
            if (name.StartsWith(GitFlow.HotfixPrefix, StringComparison.Ordinal))
                return Models.GitFlowBranchType.Hotfix;
            if (!string.IsNullOrEmpty(GitFlow.SupportPrefix) && name.StartsWith(GitFlow.SupportPrefix, StringComparison.Ordinal))
                return Models.GitFlowBranchType.Support;

            return Models.GitFlowBranchType.None;
        }

        /// <summary>
        /// Updates working copy remotes information
        /// </summary>
        private void UpdateWorkingCopyRemotesInfo(List<Models.Remote> remotes)
        {
            if (_workingCopy != null)
                _workingCopy.HasRemotes = remotes.Count > 0;
        }

        /// <summary>
        /// Updates pending pull/push state
        /// </summary>
        private void UpdatePendingPullPushState()
        {
            var hasPendingPullOrPush = CurrentBranch?.TrackStatus.IsVisible ?? false;
            GetOwnerPage()?.ChangeDirtyState(Models.DirtyState.HasPendingPullOrPush, !hasPendingPullOrPush);
        }

        /// <summary>
        /// Checks if submodules have changed
        /// </summary>
        private bool HasSubmodulesChanged(List<Models.Submodule> newSubmodules)
        {
            bool hasChanged = _submodules.Count != newSubmodules.Count;
            if (!hasChanged)
            {
                var old = new Dictionary<string, Models.Submodule>();
                foreach (var module in _submodules)
                    old.Add(module.Path, module);

                foreach (var module in newSubmodules)
                {
                    if (!old.TryGetValue(module.Path, out var exist))
                    {
                        hasChanged = true;
                        break;
                    }

                    hasChanged = !exist.SHA.Equals(module.SHA, StringComparison.Ordinal) ||
                                 !exist.Branch.Equals(module.Branch, StringComparison.Ordinal) ||
                                 !exist.URL.Equals(module.URL, StringComparison.Ordinal) ||
                                 exist.Status != module.Status;

                    if (hasChanged)
                        break;
                }
            }
            return hasChanged;
        }

        /// <summary>
        /// Loads issue trackers asynchronously
        /// </summary>
        private async Task LoadIssueTrackersAsync()
        {
            var issuetrackers = new List<Models.IssueTracker>();
            await CreateIssueTrackerCommand(true).ReadAllAsync(issuetrackers, true).ConfigureAwait(false);
            await CreateIssueTrackerCommand(false).ReadAllAsync(issuetrackers, false).ConfigureAwait(false);
            PostToUIThread(() =>
            {
                IssueTrackers.Clear();
                IssueTrackers.AddRange(issuetrackers);
            });
        }

        /// <summary>
        /// Loads GitFlow configuration asynchronously
        /// </summary>
        private async Task LoadGitFlowConfigAsync()
        {
            var config = await new Commands.Config(_fullpath).ReadAllAsync().ConfigureAwait(false);
            _hasAllowedSignersFile = config.TryGetValue("gpg.ssh.allowedSignersFile", out var allowedSignersFile) && !string.IsNullOrEmpty(allowedSignersFile);

            // Check if GitFlow is configured explicitly
            bool hasGitFlowConfig = config.Keys.Any(k => k.StartsWith("gitflow."));

            if (hasGitFlowConfig)
            {
                // Load explicit GitFlow configuration
                if (config.TryGetValue("gitflow.branch.master", out var masterName))
                    GitFlow.Master = masterName;
                if (config.TryGetValue("gitflow.branch.develop", out var developName))
                    GitFlow.Develop = developName;
                if (config.TryGetValue("gitflow.prefix.feature", out var featurePrefix))
                    GitFlow.FeaturePrefix = featurePrefix;
                if (config.TryGetValue("gitflow.prefix.release", out var releasePrefix))
                    GitFlow.ReleasePrefix = releasePrefix;
                if (config.TryGetValue("gitflow.prefix.hotfix", out var hotfixPrefix))
                    GitFlow.HotfixPrefix = hotfixPrefix;
                if (config.TryGetValue("gitflow.prefix.support", out var supportPrefix))
                    GitFlow.SupportPrefix = supportPrefix;
                if (config.TryGetValue("gitflow.prefix.versiontag", out var versionTagPrefix))
                    GitFlow.VersionTagPrefix = versionTagPrefix;
            }
            else if (_branches != null && _branches.Count > 0)
            {
                // Auto-detect GitFlow based on branch structure when no explicit config exists
                // Check for master or main branch
                var masterBranch = _branches.Find(b => b.IsLocal && b.Name == "master");
                var mainBranch = _branches.Find(b => b.IsLocal && b.Name == "main");

                if (masterBranch != null || mainBranch != null)
                {
                    // Set master/main branch
                    GitFlow.Master = masterBranch != null ? "master" : "main";

                    // Check for develop branch
                    var developBranch = _branches.Find(b => b.IsLocal && b.Name == "develop");

                    if (developBranch != null)
                    {
                        GitFlow.Develop = "develop";

                        // Auto-detect common GitFlow prefixes based on existing branches
                        bool hasFeatureBranches = _branches.Any(b => b.IsLocal && b.Name.StartsWith("feature/"));
                        bool hasReleaseBranches = _branches.Any(b => b.IsLocal && b.Name.StartsWith("release/"));
                        bool hasHotfixBranches = _branches.Any(b => b.IsLocal && b.Name.StartsWith("hotfix/"));
                        bool hasSupportBranches = _branches.Any(b => b.IsLocal && b.Name.StartsWith("support/"));

                        // Set default GitFlow prefixes
                        GitFlow.FeaturePrefix = "feature/";
                        GitFlow.ReleasePrefix = "release/";
                        GitFlow.HotfixPrefix = "hotfix/";

                        if (hasSupportBranches)
                            GitFlow.SupportPrefix = "support/";

                        GitFlow.VersionTagPrefix = "";

                        // Auto-initialize GitFlow configuration for the repository
                        // This allows GitFlow commands to work without manual initialization
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                // Write GitFlow configuration synchronously to ensure it's set
                                var configCmd = new Commands.Config(_fullpath);
                                await configCmd.SetAsync("gitflow.branch.master", GitFlow.Master).ConfigureAwait(false);
                                await configCmd.SetAsync("gitflow.branch.develop", GitFlow.Develop).ConfigureAwait(false);
                                await configCmd.SetAsync("gitflow.prefix.feature", GitFlow.FeaturePrefix).ConfigureAwait(false);
                                await configCmd.SetAsync("gitflow.prefix.release", GitFlow.ReleasePrefix).ConfigureAwait(false);
                                await configCmd.SetAsync("gitflow.prefix.hotfix", GitFlow.HotfixPrefix).ConfigureAwait(false);
                                await configCmd.SetAsync("gitflow.prefix.bugfix", "bugfix/").ConfigureAwait(false);
                                await configCmd.SetAsync("gitflow.prefix.support", GitFlow.SupportPrefix).ConfigureAwait(false);
                                await configCmd.SetAsync("gitflow.prefix.versiontag", GitFlow.VersionTagPrefix ?? "", true).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                // Silently fail - auto-configuration is optional
                                App.LogException(ex);
                            }
                        });
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Checks for GitFlow structure and auto-configures if detected
        /// </summary>
        private void CheckAndAutoConfigureGitFlow(List<Models.Branch> localBranches)
        {
            // Check if GitFlow is already configured
            Task.Run(async () =>
            {
                var config = await new Commands.Config(_fullpath).ReadAllAsync().ConfigureAwait(false);
                bool hasGitFlowConfig = config.Keys.Any(k => k.StartsWith("gitflow."));

                if (!hasGitFlowConfig && localBranches != null && localBranches.Count > 0)
                {
                    // Check for master or main branch
                    var masterBranch = localBranches.Find(b => b.Name == "master");
                    var mainBranch = localBranches.Find(b => b.Name == "main");
                    var developBranch = localBranches.Find(b => b.Name == "develop");

                    if ((masterBranch != null || mainBranch != null) && developBranch != null)
                    {
                        var primaryBranch = masterBranch != null ? "master" : "main";

                        // Write GitFlow configuration
                        var configCmd = new Commands.Config(_fullpath);
                        await configCmd.SetAsync("gitflow.branch.master", primaryBranch).ConfigureAwait(false);
                        await configCmd.SetAsync("gitflow.branch.develop", "develop").ConfigureAwait(false);
                        await configCmd.SetAsync("gitflow.prefix.feature", "feature/").ConfigureAwait(false);
                        await configCmd.SetAsync("gitflow.prefix.release", "release/").ConfigureAwait(false);
                        await configCmd.SetAsync("gitflow.prefix.hotfix", "hotfix/").ConfigureAwait(false);
                        await configCmd.SetAsync("gitflow.prefix.bugfix", "bugfix/").ConfigureAwait(false);
                        await configCmd.SetAsync("gitflow.prefix.support", "support/").ConfigureAwait(false);
                        await configCmd.SetAsync("gitflow.prefix.versiontag", "", true).ConfigureAwait(false);

                        // Update the GitFlow object
                        ExecuteOnUIThread(() =>
                        {
                            GitFlow.Master = primaryBranch;
                            GitFlow.Develop = "develop";
                            GitFlow.FeaturePrefix = "feature/";
                            GitFlow.ReleasePrefix = "release/";
                            GitFlow.HotfixPrefix = "hotfix/";
                            GitFlow.SupportPrefix = "support/";
                            GitFlow.VersionTagPrefix = "";
                        });

                        System.Diagnostics.Debug.WriteLine($"[GitFlow] Auto-configured for {primaryBranch}/develop structure");
                    }
                }
            });
        }
    }
}
