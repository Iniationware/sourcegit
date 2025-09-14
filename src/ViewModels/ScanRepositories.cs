﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;

namespace SourceGit.ViewModels
{
    public class ScanRepositories : Popup
    {
        public bool UseCustomDir
        {
            get => _useCustomDir;
            set => SetProperty(ref _useCustomDir, value);
        }

        public string CustomDir
        {
            get => _customDir;
            set => SetProperty(ref _customDir, value);
        }

        public List<Models.ScanDir> ScanDirs
        {
            get;
        }

        [Required(ErrorMessage = "Scan directory is required!!!")]
        public Models.ScanDir Selected
        {
            get => _selected;
            set => SetProperty(ref _selected, value, true);
        }

        public ScanRepositories()
        {
            ScanDirs = new List<Models.ScanDir>();

            var workspace = Preferences.Instance.GetActiveWorkspace();
            if (!string.IsNullOrEmpty(workspace.DefaultCloneDir))
                ScanDirs.Add(new Models.ScanDir(workspace.DefaultCloneDir, "Workspace"));

            if (!string.IsNullOrEmpty(Preferences.Instance.GitDefaultCloneDir))
                ScanDirs.Add(new Models.ScanDir(Preferences.Instance.GitDefaultCloneDir, "Global"));

            if (ScanDirs.Count > 0)
                _selected = ScanDirs[0];
            else
                _useCustomDir = true;

            GetManagedRepositories(Preferences.Instance.RepositoryNodes, _managed);
        }

        public override async Task<bool> Sure()
        {
            var selectedDir = _useCustomDir ? _customDir : _selected?.Path;
            if (string.IsNullOrEmpty(selectedDir))
            {
                App.RaiseException(null, "Missing root directory to scan!");
                return false;
            }

            if (!Directory.Exists(selectedDir))
                return true;

            ProgressDescription = $"Scan repositories under '{selectedDir}' ...";

            var minDelay = Task.Delay(500);
            var rootDir = new DirectoryInfo(selectedDir);
            var found = new List<string>();

            // Check if directory exists and is accessible
            if (!rootDir.Exists)
            {
                App.RaiseException(rootDir.FullName, $"Directory does not exist: {rootDir.FullName}");
                return false;
            }

            try
            {
                await GetUnmanagedRepositoriesAsync(rootDir, found, new EnumerationOptions()
                {
                    AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = false, // We handle recursion manually
                });
            }
            catch (Exception ex)
            {
                App.RaiseException(rootDir.FullName, $"Failed to scan repositories: {ex.Message}");
                return false;
            }

            // Make sure this task takes at least 0.5s to avoid the popup panel disappearing too quickly.
            await minDelay;

            // Process results on UI thread to avoid concurrent collection modification
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                var normalizedRoot = rootDir.FullName.Replace('\\', '/').TrimEnd('/');
                foreach (var f in found)
                {
                    var parent = new DirectoryInfo(f).Parent!.FullName.Replace('\\', '/').TrimEnd('/');
                    if (parent.Equals(normalizedRoot, StringComparison.Ordinal))
                    {
                        Preferences.Instance.FindOrAddNodeByRepositoryPath(f, null, false, false);
                    }
                    else if (parent.StartsWith(normalizedRoot, StringComparison.Ordinal))
                    {
                        var relative = parent.Substring(normalizedRoot.Length).TrimStart('/');
                        var group = FindOrCreateGroupRecursive(Preferences.Instance.RepositoryNodes, relative);
                        Preferences.Instance.FindOrAddNodeByRepositoryPath(f, group, false, false);
                    }
                }

                Preferences.Instance.AutoRemoveInvalidNode();
                Preferences.Instance.Save();
                Welcome.Instance.Refresh();
            });

            return true;
        }

        private void GetManagedRepositories(List<RepositoryNode> group, HashSet<string> repos)
        {
            foreach (var node in group)
            {
                if (node.IsRepository)
                    repos.Add(node.Id);
                else
                    GetManagedRepositories(node.SubNodes, repos);
            }
        }

        private async Task GetUnmanagedRepositoriesAsync(DirectoryInfo dir, List<string> outs, EnumerationOptions opts, int depth = 0)
        {
            DirectoryInfo[] subdirs;
            try
            {
                subdirs = dir.GetDirectories("*", opts);
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we don't have permission to access
                return;
            }
            catch (DirectoryNotFoundException)
            {
                // Directory was deleted while scanning
                return;
            }
            catch (Exception ex)
            {
                // Log other exceptions but continue scanning
                App.RaiseException(dir.FullName, $"Error scanning directory: {ex.Message}");
                return;
            }

            foreach (var subdir in subdirs)
            {
                if (subdir.Name.StartsWith(".", StringComparison.Ordinal) ||
                    subdir.Name.Equals("node_modules", StringComparison.Ordinal))
                    continue;

                ProgressDescription = $"Scanning {subdir.FullName}...";

                var normalizedSelf = subdir.FullName.Replace('\\', '/').TrimEnd('/');
                if (_managed.Contains(normalizedSelf))
                    continue;

                try
                {
                    var gitDir = Path.Combine(subdir.FullName, ".git");
                    if (Directory.Exists(gitDir) || File.Exists(gitDir))
                    {
                        var test = await new Commands.QueryRepositoryRootPath(subdir.FullName).GetResultAsync().ConfigureAwait(false);
                        if (test.IsSuccess && !string.IsNullOrEmpty(test.StdOut))
                        {
                            var normalized = test.StdOut.Trim().Replace('\\', '/').TrimEnd('/');
                            if (!_managed.Contains(normalized))
                                outs.Add(normalized);
                        }

                        continue;
                    }

                    var isBare = await new Commands.IsBareRepository(subdir.FullName).GetResultAsync().ConfigureAwait(false);
                    if (isBare)
                    {
                        outs.Add(normalizedSelf);
                        continue;
                    }

                    if (depth < 5)
                        await GetUnmanagedRepositoriesAsync(subdir, outs, opts, depth + 1);
                }
                catch (Exception ex)
                {
                    // Log error but continue scanning other directories
                    App.RaiseException(subdir.FullName, $"Error processing directory: {ex.Message}");
                }
            }
        }

        private RepositoryNode FindOrCreateGroupRecursive(List<RepositoryNode> collection, string path)
        {
            RepositoryNode node = null;
            foreach (var name in path.Split('/'))
            {
                node = FindOrCreateGroup(collection, name);
                collection = node.SubNodes;
            }

            return node;
        }

        private RepositoryNode FindOrCreateGroup(List<RepositoryNode> collection, string name)
        {
            foreach (var node in collection)
            {
                if (node.Name.Equals(name, StringComparison.Ordinal))
                    return node;
            }

            var added = new RepositoryNode()
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                IsRepository = false,
                IsExpanded = true,
            };
            collection.Add(added);

            Preferences.Instance.SortNodes(collection);
            return added;
        }

        private HashSet<string> _managed = new();
        private bool _useCustomDir = false;
        private string _customDir = string.Empty;
        private Models.ScanDir _selected = null;
    }
}
