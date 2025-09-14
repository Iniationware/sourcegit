using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SourceGit.Models
{
    public class Watcher : IDisposable
    {
        private readonly Channel<FileSystemEventArgs> _eventChannel;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task _eventProcessorTask;

        public Watcher(IRepository repo, string fullpath, string gitDir)
        {
            _repo = repo;
            _cancellationTokenSource = new CancellationTokenSource();

            // Create a bounded channel to prevent memory issues with rapid events
            _eventChannel = Channel.CreateBounded<FileSystemEventArgs>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

            // Start the event processor task
            _eventProcessorTask = ProcessEventsAsync(_cancellationTokenSource.Token);

            var testGitDir = new DirectoryInfo(Path.Combine(fullpath, ".git")).FullName;
            var desiredDir = new DirectoryInfo(gitDir).FullName;
            if (testGitDir.Equals(desiredDir, StringComparison.Ordinal))
            {
                var combined = new FileSystemWatcher();
                combined.Path = fullpath;
                combined.Filter = "*";
                combined.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime;
                combined.IncludeSubdirectories = true;
                combined.Created += OnRepositoryChanged;
                combined.Renamed += OnRepositoryChanged;
                combined.Changed += OnRepositoryChanged;
                combined.Deleted += OnRepositoryChanged;
                combined.EnableRaisingEvents = true;

                _watchers.Add(combined);
            }
            else
            {
                var wc = new FileSystemWatcher();
                wc.Path = fullpath;
                wc.Filter = "*";
                wc.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime;
                wc.IncludeSubdirectories = true;
                wc.Created += OnWorkingCopyChanged;
                wc.Renamed += OnWorkingCopyChanged;
                wc.Changed += OnWorkingCopyChanged;
                wc.Deleted += OnWorkingCopyChanged;
                wc.EnableRaisingEvents = true;

                var git = new FileSystemWatcher();
                git.Path = gitDir;
                git.Filter = "*";
                git.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.DirectoryName | NotifyFilters.FileName;
                git.IncludeSubdirectories = true;
                git.Created += OnGitDirChanged;
                git.Renamed += OnGitDirChanged;
                git.Changed += OnGitDirChanged;
                git.Deleted += OnGitDirChanged;
                git.EnableRaisingEvents = true;

                _watchers.Add(wc);
                _watchers.Add(git);
            }

            _timer = new Timer(Tick, null, 100, 100);
        }

        public void SetEnabled(bool enabled)
        {
            if (enabled)
            {
                Interlocked.Decrement(ref _lockCount);
                if (_lockCount < 0)
                    Interlocked.Exchange(ref _lockCount, 0);
            }
            else
            {
                Interlocked.Increment(ref _lockCount);
            }
        }

        public void SetSubmodules(List<Submodule> submodules)
        {
            var newSet = new HashSet<string>();
            foreach (var submodule in submodules)
                newSet.Add(submodule.Path);
            _submodules = newSet;
        }

        public void MarkBranchDirtyManually()
        {
            Interlocked.Exchange(ref _updateBranch, DateTime.Now.ToFileTime() - 1);
        }

        public void MarkTagDirtyManually()
        {
            Interlocked.Exchange(ref _updateTags, DateTime.Now.ToFileTime() - 1);
        }

        public void MarkWorkingCopyDirtyManually()
        {
            Interlocked.Exchange(ref _updateWC, DateTime.Now.ToFileTime() - 1);
        }

        public void Dispose()
        {
            // Stop accepting new events
            _eventChannel.Writer.TryComplete();

            // Signal cancellation
            _cancellationTokenSource?.Cancel();

            // Dispose watchers
            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            _watchers.Clear();

            // Dispose timer
            _timer?.Dispose();
            _timer = null;

            // Wait for event processor to complete (max 1 second)
            try
            {
                _eventProcessorTask?.Wait(1000);
            }
            catch { }

            // Dispose cancellation token
            _cancellationTokenSource?.Dispose();
        }

        private void Tick(object sender)
        {
            if (Interlocked.Read(ref _lockCount) > 0)
                return;

            var now = DateTime.Now.ToFileTime();

            // Collect all pending updates that have passed their debounce delay
            var pendingUpdates = new List<Action>();
            bool needsCommitRefresh = false;

            var updateBranch = Interlocked.Read(ref _updateBranch);
            if (updateBranch > 0 && now > updateBranch)
            {
                Interlocked.Exchange(ref _updateBranch, 0);
                Interlocked.Exchange(ref _updateWC, 0);  // Branch changes often affect working copy

                pendingUpdates.Add(() => _repo.RefreshBranches());
                pendingUpdates.Add(() => _repo.RefreshWorktrees());
                pendingUpdates.Add(() => _repo.RefreshWorkingCopyChanges());
                needsCommitRefresh = true;

                var updateTags = Interlocked.Read(ref _updateTags);
                if (updateTags > 0)
                {
                    Interlocked.Exchange(ref _updateTags, 0);
                    pendingUpdates.Add(() => _repo.RefreshTags());
                }

                var updateSubmodules = Interlocked.Read(ref _updateSubmodules);
                if (updateSubmodules > 0 || _repo.MayHaveSubmodules())
                {
                    Interlocked.Exchange(ref _updateSubmodules, 0);
                    pendingUpdates.Add(() => _repo.RefreshSubmodules());
                }
            }
            else
            {
                // Handle individual updates if no branch update is pending
                var updateWC = Interlocked.Read(ref _updateWC);
                if (updateWC > 0 && now > updateWC)
                {
                    Interlocked.Exchange(ref _updateWC, 0);
                    pendingUpdates.Add(() => _repo.RefreshWorkingCopyChanges());
                }

                var updateSubmodules = Interlocked.Read(ref _updateSubmodules);
                if (updateSubmodules > 0 && now > updateSubmodules)
                {
                    Interlocked.Exchange(ref _updateSubmodules, 0);
                    pendingUpdates.Add(() => _repo.RefreshSubmodules());
                }

                var updateStashes = Interlocked.Read(ref _updateStashes);
                if (updateStashes > 0 && now > updateStashes)
                {
                    Interlocked.Exchange(ref _updateStashes, 0);
                    pendingUpdates.Add(() => _repo.RefreshStashes());
                }

                var updateTags = Interlocked.Read(ref _updateTags);
                if (updateTags > 0 && now > updateTags)
                {
                    Interlocked.Exchange(ref _updateTags, 0);
                    pendingUpdates.Add(() => _repo.RefreshTags());
                    needsCommitRefresh = true;
                }
            }

            // Execute all pending updates in parallel for better multi-core utilization
            if (pendingUpdates.Count > 0)
            {
                Task.Run(() =>
                {
                    // Run all independent refresh operations in parallel
                    Parallel.Invoke(pendingUpdates.ToArray());

                    // Refresh commits last as it may depend on other data
                    if (needsCommitRefresh)
                        _repo.RefreshCommits();
                });
            }
        }

        private void OnRepositoryChanged(object o, FileSystemEventArgs e)
        {
            // Queue the event for async processing
            if (!_eventChannel.Writer.TryWrite(e))
            {
                // Channel is full, drop the event (will be caught by next periodic scan)
            }
        }

        private void OnGitDirChanged(object o, FileSystemEventArgs e)
        {
            // Queue the event for async processing
            if (!_eventChannel.Writer.TryWrite(e))
            {
                // Channel is full, drop the event
            }
        }

        private void OnWorkingCopyChanged(object o, FileSystemEventArgs e)
        {
            // Queue the event for async processing
            if (!_eventChannel.Writer.TryWrite(e))
            {
                // Channel is full, drop the event
            }
        }

        private async Task ProcessEventsAsync(CancellationToken cancellationToken)
        {
            var eventBatch = new Dictionary<string, FileSystemEventArgs>();
            var lastProcessTime = DateTime.UtcNow;
            var debounceDelay = TimeSpan.FromMilliseconds(200);

            try
            {
                while (await _eventChannel.Reader.WaitToReadAsync(cancellationToken))
                {
                    // Collect events for debouncing
                    while (_eventChannel.Reader.TryRead(out var e))
                    {
                        if (string.IsNullOrEmpty(e.Name))
                            continue;

                        // Use the full path as key to deduplicate events
                        eventBatch[e.FullPath] = e;

                        // Process batch if it gets too large
                        if (eventBatch.Count > 100)
                        {
                            ProcessEventBatch(eventBatch);
                            eventBatch.Clear();
                            lastProcessTime = DateTime.UtcNow;
                        }
                    }

                    // Process batch after debounce delay
                    if (eventBatch.Count > 0 && DateTime.UtcNow - lastProcessTime > debounceDelay)
                    {
                        ProcessEventBatch(eventBatch);
                        eventBatch.Clear();
                        lastProcessTime = DateTime.UtcNow;
                    }

                    // Small delay to prevent tight loop
                    await Task.Delay(50, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
        }

        private void ProcessEventBatch(Dictionary<string, FileSystemEventArgs> batch)
        {
            foreach (var kvp in batch)
            {
                var e = kvp.Value;
                if (string.IsNullOrEmpty(e.Name))
                    continue;

                var name = e.Name.Replace('\\', '/').TrimEnd('/');

                // Process repository-wide changes
                if (name.Equals(".git", StringComparison.Ordinal))
                    continue;

                if (name.EndsWith("/.git", StringComparison.Ordinal))
                    continue;

                // Route to appropriate handler
                if (name.StartsWith(".git/", StringComparison.Ordinal))
                {
                    HandleGitDirFileChanged(name.Substring(5));
                }
                else if (!name.StartsWith(".git/", StringComparison.Ordinal) &&
                         !name.EndsWith("/.git", StringComparison.Ordinal))
                {
                    HandleWorkingCopyFileChanged(name);
                }
            }
        }

        private void HandleGitDirFileChanged(string name)
        {
            if (name.Contains("fsmonitor--daemon/", StringComparison.Ordinal) ||
                name.EndsWith(".lock", StringComparison.Ordinal) ||
                name.StartsWith("lfs/", StringComparison.Ordinal))
                return;

            if (name.StartsWith("modules", StringComparison.Ordinal))
            {
                if (name.EndsWith("/HEAD", StringComparison.Ordinal) ||
                    name.EndsWith("/ORIG_HEAD", StringComparison.Ordinal))
                {
                    Interlocked.Exchange(ref _updateSubmodules, DateTime.Now.AddSeconds(1).ToFileTime());
                    Interlocked.Exchange(ref _updateWC, DateTime.Now.AddSeconds(1).ToFileTime());
                }
            }
            else if (name.Equals("MERGE_HEAD", StringComparison.Ordinal) ||
                name.Equals("AUTO_MERGE", StringComparison.Ordinal))
            {
                if (_repo.MayHaveSubmodules())
                    Interlocked.Exchange(ref _updateSubmodules, DateTime.Now.AddSeconds(1).ToFileTime());
            }
            else if (name.StartsWith("refs/tags", StringComparison.Ordinal))
            {
                Interlocked.Exchange(ref _updateTags, DateTime.Now.AddSeconds(.5).ToFileTime());
            }
            else if (name.StartsWith("refs/stash", StringComparison.Ordinal))
            {
                Interlocked.Exchange(ref _updateStashes, DateTime.Now.AddSeconds(.5).ToFileTime());
            }
            else if (name.Equals("HEAD", StringComparison.Ordinal) ||
                name.Equals("BISECT_START", StringComparison.Ordinal) ||
                name.StartsWith("refs/heads/", StringComparison.Ordinal) ||
                name.StartsWith("refs/remotes/", StringComparison.Ordinal) ||
                (name.StartsWith("worktrees/", StringComparison.Ordinal) && name.EndsWith("/HEAD", StringComparison.Ordinal)))
            {
                Interlocked.Exchange(ref _updateBranch, DateTime.Now.AddSeconds(.5).ToFileTime());
            }
            else if (name.StartsWith("objects/", StringComparison.Ordinal) || name.Equals("index", StringComparison.Ordinal))
            {
                Interlocked.Exchange(ref _updateWC, DateTime.Now.AddSeconds(1).ToFileTime());
            }
        }

        private void HandleWorkingCopyFileChanged(string name)
        {
            if (name.StartsWith(".vs/", StringComparison.Ordinal))
                return;

            if (name.Equals(".gitmodules", StringComparison.Ordinal))
            {
                Interlocked.Exchange(ref _updateSubmodules, DateTime.Now.AddSeconds(1).ToFileTime());
                Interlocked.Exchange(ref _updateWC, DateTime.Now.AddSeconds(1).ToFileTime());
                return;
            }

            var submodules = _submodules; // Thread-safe snapshot
            if (submodules != null)
            {
                foreach (var submodule in submodules)
                {
                    if (name.StartsWith(submodule, StringComparison.Ordinal))
                    {
                        Interlocked.Exchange(ref _updateSubmodules, DateTime.Now.AddSeconds(1).ToFileTime());
                        return;
                    }
                }
            }

            Interlocked.Exchange(ref _updateWC, DateTime.Now.AddSeconds(1).ToFileTime());
        }

        private readonly IRepository _repo = null;
        private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
        private Timer _timer = null;
        private long _lockCount = 0;  // Changed to long for Interlocked operations
        private long _updateWC = 0;
        private long _updateBranch = 0;
        private long _updateSubmodules = 0;
        private long _updateStashes = 0;
        private long _updateTags = 0;

        // Thread-safe submodules collection
        private volatile HashSet<string> _submodules = new HashSet<string>();
    }
}
