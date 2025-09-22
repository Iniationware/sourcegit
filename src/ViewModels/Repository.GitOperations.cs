using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SourceGit.ViewModels
{
    /// <summary>
    /// Repository Git operations - handles core Git commands
    /// </summary>
    public partial class Repository
    {
        #region Fetch Operations

        /// <summary>
        /// Initiates a fetch operation
        /// </summary>
        /// <param name="autoStart">Whether to start the operation automatically</param>
        public async Task FetchAsync(bool autoStart)
        {
            if (!CanCreatePopup())
                return;

            if (_remotes.Count == 0)
            {
                App.RaiseException(_fullpath, "No remotes added to this repository!!!");
                return;
            }

            if (autoStart)
                await ShowAndStartPopupAsync(new Fetch(this));
            else
                ShowPopup(new Fetch(this));
        }

        #endregion

        #region Pull Operations

        /// <summary>
        /// Initiates a pull operation
        /// </summary>
        /// <param name="autoStart">Whether to start the operation automatically</param>
        public async Task PullAsync(bool autoStart)
        {
            if (IsBare || !CanCreatePopup())
                return;

            if (_remotes.Count == 0)
            {
                App.RaiseException(_fullpath, "No remotes added to this repository!!!");
                return;
            }

            if (_currentBranch == null)
            {
                App.RaiseException(_fullpath, "Can NOT find current branch!!!");
                return;
            }

            var pull = new Pull(this, null);
            if (autoStart && pull.SelectedBranch != null)
                await ShowAndStartPopupAsync(pull);
            else
                ShowPopup(pull);
        }

        #endregion

        #region Push Operations

        /// <summary>
        /// Initiates a push operation
        /// </summary>
        /// <param name="autoStart">Whether to start the operation automatically</param>
        public async Task PushAsync(bool autoStart)
        {
            if (!CanCreatePopup())
                return;

            if (_remotes.Count == 0)
            {
                App.RaiseException(_fullpath, "No remotes added to this repository!!!");
                return;
            }

            if (_currentBranch == null)
            {
                App.RaiseException(_fullpath, "Can NOT find current branch!!!");
                return;
            }

            if (autoStart)
                await ShowAndStartPopupAsync(new Push(this, null));
            else
                ShowPopup(new Push(this, null));
        }

        #endregion

        #region Branch Operations

        /// <summary>
        /// Creates a new branch
        /// </summary>
        public void CreateNewBranch()
        {
            if (_currentBranch == null)
            {
                App.RaiseException(_fullpath, "Git cannot create a branch before your first commit.");
                return;
            }

            if (CanCreatePopup())
                ShowPopup(new CreateBranch(this, _currentBranch));
        }

        /// <summary>
        /// Checks out a branch asynchronously
        /// </summary>
        /// <param name="branch">The branch to checkout</param>
        public async Task CheckoutBranchAsync(Models.Branch branch)
        {
            if (branch.IsLocal)
            {
                var worktree = _worktrees.Find(x => x.Branch.Equals(branch.FullName, StringComparison.Ordinal));
                if (worktree != null)
                {
                    OpenWorktree(worktree);
                    return;
                }
            }

            if (IsBare || !CanCreatePopup())
                return;

            if (branch.IsLocal)
            {
                if (_localChangesCount > 0 || _submodules.Count > 0)
                    ShowPopup(new Checkout(this, branch.Name));
                else
                    await ShowAndStartPopupAsync(new Checkout(this, branch.Name));
            }
            else
            {
                await HandleRemoteBranchCheckout(branch);
            }
        }

        /// <summary>
        /// Deletes a branch
        /// </summary>
        /// <param name="branch">The branch to delete</param>
        public void DeleteBranch(Models.Branch branch)
        {
            if (CanCreatePopup())
                ShowPopup(new DeleteBranch(this, branch));
        }

        /// <summary>
        /// Deletes multiple branches
        /// </summary>
        /// <param name="branches">The branches to delete</param>
        /// <param name="isLocal">Whether the branches are local</param>
        public void DeleteMultipleBranches(List<Models.Branch> branches, bool isLocal)
        {
            if (CanCreatePopup())
                ShowPopup(new DeleteMultipleBranches(this, branches, isLocal));
        }

        /// <summary>
        /// Merges multiple branches
        /// </summary>
        /// <param name="branches">The branches to merge</param>
        public void MergeMultipleBranches(List<Models.Branch> branches)
        {
            if (CanCreatePopup())
                ShowPopup(new MergeMultiple(this, branches));
        }

        /// <summary>
        /// Compares a branch with the worktree
        /// </summary>
        /// <param name="branch">The branch to compare</param>
        public async Task CompareBranchWithWorktreeAsync(Models.Branch branch)
        {
            if (_histories != null)
            {
                SelectedSearchedCommit = null;

                var target = await new Commands.QuerySingleCommit(_fullpath, branch.Head).GetResultAsync().ConfigureAwait(false);
                _histories.AutoSelectedCommit = null;
                _histories.DetailContext = new RevisionCompare(_fullpath, target, null);
            }
        }

        #endregion

        #region Tag Operations

        /// <summary>
        /// Creates a new tag
        /// </summary>
        public void CreateNewTag()
        {
            if (_currentBranch == null)
            {
                App.RaiseException(_fullpath, "Git cannot create a tag before your first commit.");
                return;
            }

            if (CanCreatePopup())
                ShowPopup(new CreateTag(this, _currentBranch));
        }

        /// <summary>
        /// Checks out a tag asynchronously
        /// </summary>
        /// <param name="tag">The tag to checkout</param>
        public async Task CheckoutTagAsync(Models.Tag tag)
        {
            var c = await new Commands.QuerySingleCommit(_fullpath, tag.SHA).GetResultAsync().ConfigureAwait(false);
            if (c != null)
                await _histories?.CheckoutBranchByCommitAsync(c);
        }

        /// <summary>
        /// Deletes a tag
        /// </summary>
        /// <param name="tag">The tag to delete</param>
        public void DeleteTag(Models.Tag tag)
        {
            if (CanCreatePopup())
                ShowPopup(new DeleteTag(this, tag));
        }

        #endregion

        #region Stash Operations

        /// <summary>
        /// Stashes all changes
        /// </summary>
        /// <param name="autoStart">Whether to start the operation automatically</param>
        public async Task StashAllAsync(bool autoStart)
        {
            await _workingCopy?.StashAllAsync(autoStart);
        }

        /// <summary>
        /// Clears all stashes
        /// </summary>
        public void ClearStashes()
        {
            if (CanCreatePopup())
                ShowPopup(new ClearStashes(this));
        }

        #endregion

        #region Merge Operations

        /// <summary>
        /// Skips the current merge
        /// </summary>
        public async Task SkipMergeAsync()
        {
            await _workingCopy?.SkipMergeAsync();
        }

        /// <summary>
        /// Aborts the current merge
        /// </summary>
        public async Task AbortMergeAsync()
        {
            await _workingCopy?.AbortMergeAsync();
        }

        #endregion

        #region Bisect Operations

        /// <summary>
        /// Executes a bisect command
        /// </summary>
        /// <param name="subcmd">The bisect subcommand</param>
        public async Task ExecBisectCommandAsync(string subcmd)
        {
            IsBisectCommandRunning = true;
            SetWatcherEnabled(false);

            var log = CreateLog($"Bisect({subcmd})");

            var succ = await new Commands.Bisect(_fullpath, subcmd).Use(log).ExecAsync();
            log.Complete();

            var head = await new Commands.QueryRevisionByRefName(_fullpath, "HEAD").GetResultAsync();
            if (!succ)
                App.RaiseException(_fullpath, log.Content.Substring(log.Content.IndexOf('\n')).Trim());
            else if (log.Content.Contains("is the first bad commit"))
                App.SendNotification(_fullpath, log.Content.Substring(log.Content.IndexOf('\n')).Trim());

            MarkBranchesDirtyManually();
            NavigateToCommit(head, true);
            SetWatcherEnabled(true);
            IsBisectCommandRunning = false;
        }

        #endregion

        #region Patch Operations

        /// <summary>
        /// Applies a patch
        /// </summary>
        public void ApplyPatch()
        {
            if (CanCreatePopup())
                ShowPopup(new Apply(this));
        }

        /// <summary>
        /// Saves a commit as a patch file
        /// </summary>
        /// <param name="commit">The commit to save</param>
        /// <param name="folder">The folder to save to</param>
        /// <param name="index">The index for filename generation</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SaveCommitAsPatchAsync(Models.Commit commit, string folder, int index = 0)
        {
            var ignore_chars = new HashSet<char> { '/', '\\', ':', ',', '*', '?', '\"', '<', '>', '|', '`', '$', '^', '%', '[', ']', '+', '-' };
            var builder = new System.Text.StringBuilder();
            builder.Append(index.ToString("D4"));
            builder.Append('-');

            var chars = commit.Subject.ToCharArray();
            var len = 0;
            foreach (var c in chars)
            {
                if (!ignore_chars.Contains(c))
                {
                    if (c == ' ' || c == '\t')
                        builder.Append('-');
                    else
                        builder.Append(c);

                    len++;

                    if (len >= 48)
                        break;
                }
            }
            builder.Append(".patch");

            var saveTo = System.IO.Path.Combine(folder, builder.ToString());
            var log = CreateLog("Save Commit as Patch");
            var succ = await new Commands.FormatPatch(_fullpath, commit.SHA, saveTo).Use(log).ExecAsync();
            log.Complete();
            return succ;
        }

        #endregion

        #region Cleanup Operations

        /// <summary>
        /// Cleans up the repository
        /// </summary>
        public async Task CleanupAsync()
        {
            if (CanCreatePopup())
                await ShowAndStartPopupAsync(new Cleanup(this));
        }

        /// <summary>
        /// Discards all changes
        /// </summary>
        public void DiscardAllChanges()
        {
            if (CanCreatePopup())
                ShowPopup(new Discard(this));
        }

        #endregion

        #region Custom Actions

        /// <summary>
        /// Executes a custom action
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="scopeTarget">The scope target for the action</param>
        public async Task ExecCustomActionAsync(Models.CustomAction action, object scopeTarget)
        {
            if (!CanCreatePopup())
                return;

            var popup = new ExecuteCustomAction(this, action, scopeTarget);
            if (action.Controls.Count == 0)
                await ShowAndStartPopupAsync(popup);
            else
                ShowPopup(popup);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Handles checkout of remote branches
        /// </summary>
        /// <param name="branch">The remote branch to checkout</param>
        private async Task HandleRemoteBranchCheckout(Models.Branch branch)
        {
            foreach (var b in _branches)
            {
                if (b.IsLocal &&
                    b.Upstream.Equals(branch.FullName, StringComparison.Ordinal) &&
                    b.TrackStatus.Ahead.Count == 0)
                {
                    if (b.TrackStatus.Behind.Count > 0)
                        ShowPopup(new CheckoutAndFastForward(this, b, branch));
                    else if (!b.IsCurrent)
                        await CheckoutBranchAsync(b);

                    return;
                }
            }

            ShowPopup(new CreateBranch(this, branch));
        }

        #endregion
    }
}
