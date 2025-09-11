using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Collections;

namespace SourceGit.ViewModels
{
    /// <summary>
    /// Interface defining core repository services and operations
    /// </summary>
    public interface IRepositoryService
    {
        // Core Properties
        string FullPath { get; }
        string GitDir { get; }
        bool IsBare { get; }
        Models.RepositorySettings Settings { get; }
        
        // Branch Management
        Models.Branch CurrentBranch { get; }
        AvaloniaList<Models.Branch> Branches { get; }
        AvaloniaList<BranchTreeNode> LocalBranchTrees { get; }
        AvaloniaList<BranchTreeNode> RemoteBranchTrees { get; }
        
        // Remote Management
        AvaloniaList<Models.Remote> Remotes { get; }
        
        // Tag Management
        AvaloniaList<Models.Tag> Tags { get; }
        AvaloniaList<Models.Tag> VisibleTags { get; }
        
        // Working Copy
        WorkingCopy WorkingCopy { get; }
        
        // Stash Management
        List<Models.Stash> Stashes { get; }
        
        // Submodule Management
        List<Models.Submodule> Submodules { get; }
        
        // Core Operations
        void Close();
        void OpenInFileManager();
        void OpenInTerminal();
        
        // Refresh Operations
        Task RefreshBranches();
        Task RefreshTags();
        Task RefreshCommits();
        Task RefreshWorkingCopy();
        Task RefreshStashes();
        Task RefreshRemotes();
        Task RefreshSubmodules();
        Task RefreshAfterOperation(Models.RefreshOptions options);
        
        // Git Operations
        bool Fetch(Models.Remote remote, bool prune, bool noTags, Action<string> onProgress);
        bool Pull(Models.Branch branch, Action<string> onProgress);
        bool Push(Models.Branch branch, bool force, bool withTags, Action<string> onProgress);
        bool Checkout(string target);
        bool CreateBranch(string name, string basedOn);
        bool DeleteBranch(Models.Branch branch, bool force);
        bool CreateTag(string name, string basedOn, string message);
        bool DeleteTag(Models.Tag tag);
        
        // Stash Operations
        bool Stash(bool includeUntracked, string message);
        bool ApplyStash(Models.Stash stash);
        bool DropStash(Models.Stash stash);
        
        // Submodule Operations
        bool AddSubmodule(string url, string path);
        bool UpdateSubmodule(string path);
        bool RemoveSubmodule(string path);
        
        // History Navigation
        void NavigateToCommit(string sha);
        void NavigateToBranch(Models.Branch branch);
        void NavigateToTag(Models.Tag tag);
        
        // Search Operations
        void SearchCommits(string query);
        void ClearSearchFilter();
        
        // UI State Management
        void SetWatcherEnabled(bool enabled);
        void MarkBranchesDirty();
        void MarkTagsDirty();
        void MarkWorkingCopyDirty();
    }
}