using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace SourceGit.Views
{
    public partial class GitFlowView : UserControl
    {
        public GitFlowView()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            UpdateFilterIcons();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsVisibleProperty && IsVisible)
            {
                UpdateFilterIcons();
            }
        }

        private void UpdateFilterIcons()
        {
            if (DataContext is not ViewModels.Repository repo)
                return;

            // Find all filter buttons and update their icons
            var listBoxes = this.GetVisualDescendants().OfType<ListBox>();
            foreach (var listBox in listBoxes)
            {
                // Iterate through all items in the ListBox
                for (int i = 0; i < listBox.ItemCount; i++)
                {
                    var listBoxItem = listBox.ContainerFromIndex(i) as ListBoxItem;
                    if (listBoxItem != null)
                    {
                        var button = listBoxItem.GetVisualDescendants().OfType<Button>()
                            .FirstOrDefault(b => b.Name == "FilterButton");

                        if (button?.Tag is Models.Branch branch)
                        {
                            // GitFlow branches are local branches, ensure we use the correct path
                            var branchPath = branch.IsLocal ? branch.FullName : $"refs/heads/{branch.Name}";
                            var node = repo.LocalBranchTrees != null ?
                                FindBranchNode(repo.LocalBranchTrees, branchPath) : null;
                            var filterMode = node?.FilterMode ?? Models.FilterMode.None;

                            // Find all three icon paths
                            var iconNone = button.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>()
                                .FirstOrDefault(p => p.Name == "FilterIconNone");
                            var iconIncluded = button.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>()
                                .FirstOrDefault(p => p.Name == "FilterIconIncluded");
                            var iconExcluded = button.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>()
                                .FirstOrDefault(p => p.Name == "FilterIconExcluded");

                            // Update icon visibility based on filter mode
                            if (iconNone != null && iconIncluded != null && iconExcluded != null)
                            {
                                iconNone.IsVisible = filterMode == Models.FilterMode.None;
                                iconIncluded.IsVisible = filterMode == Models.FilterMode.Included;
                                iconExcluded.IsVisible = filterMode == Models.FilterMode.Excluded;

                                // Add or remove the filter-active class to control button visibility
                                if (filterMode != Models.FilterMode.None)
                                {
                                    if (!listBoxItem.Classes.Contains("filter-active"))
                                        listBoxItem.Classes.Add("filter-active");
                                }
                                else
                                {
                                    listBoxItem.Classes.Remove("filter-active");
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnStartGitFlowBranch(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo && sender is Button btn)
            {
                if (btn.Tag is Models.GitFlowBranchType type)
                {
                    repo.StartGitFlowBranch(type);
                }
            }
        }

        private void OnBranchContextRequested(object sender, ContextRequestedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo && sender is ListBox listBox)
            {
                var selected = listBox.SelectedItem as Models.Branch;
                if (selected != null)
                {
                    var menu = repo.CreateContextMenuForGitFlowBranch(selected);
                    if (menu != null)
                    {
                        menu.Open(listBox);
                    }
                }
            }
            e.Handled = true;
        }

        private async void OnBranchDoubleTapped(object sender, TappedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo && sender is ListBox listBox)
            {
                var selected = listBox.SelectedItem as Models.Branch;
                if (selected != null && !selected.IsCurrent)
                {
                    await repo.CheckoutBranchAsync(selected);
                }
            }
        }

        private void OnFilterButtonClicked(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo && sender is Button btn)
            {
                if (btn.Tag is Models.Branch branch)
                {
                    // GitFlow branches are local branches, ensure we use the correct path
                    var branchPath = branch.IsLocal ? branch.FullName : $"refs/heads/{branch.Name}";

                    // Find the corresponding BranchTreeNode to get current filter mode
                    var node = repo.LocalBranchTrees != null ?
                        FindBranchNode(repo.LocalBranchTrees, branchPath) : null;

                    var currentMode = node?.FilterMode ?? Models.FilterMode.None;

                    // Create context menu like in FilterModeSwitchButton
                    var menu = new ContextMenu();

                    // If filter is active, show "Unset" option first
                    if (currentMode != Models.FilterMode.None)
                    {
                        var unset = new MenuItem();
                        unset.Header = App.Text("Repository.FilterCommits.Default");
                        unset.Click += (_, _) =>
                        {
                            repo.SetBranchFilterMode(branch, Models.FilterMode.None, false, true);
                            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => UpdateFilterIcons());
                        };
                        menu.Items.Add(unset);
                        menu.Items.Add(new MenuItem() { Header = "-" }); // separator
                    }

                    // "Filter Commits" option (set to included)
                    var include = new MenuItem();
                    include.Icon = App.CreateMenuIcon("Icons.Filter");
                    include.Header = App.Text("Repository.FilterCommits.Include");
                    include.IsEnabled = currentMode != Models.FilterMode.Included;
                    include.Click += (_, _) =>
                    {
                        repo.SetBranchFilterMode(branch, Models.FilterMode.Included, false, true);
                        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => UpdateFilterIcons());
                    };
                    menu.Items.Add(include);

                    // "Hide Commits" option (set to excluded)
                    var exclude = new MenuItem();
                    exclude.Icon = App.CreateMenuIcon("Icons.EyeClose");
                    exclude.Header = App.Text("Repository.FilterCommits.Exclude");
                    exclude.IsEnabled = currentMode != Models.FilterMode.Excluded;
                    exclude.Click += (_, _) =>
                    {
                        repo.SetBranchFilterMode(branch, Models.FilterMode.Excluded, false, true);
                        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => UpdateFilterIcons());
                    };
                    menu.Items.Add(exclude);

                    menu.Open(btn);
                }
            }
            e.Handled = true;
        }

        private ViewModels.BranchTreeNode FindBranchNode(System.Collections.Generic.List<ViewModels.BranchTreeNode> nodes, string path)
        {
            foreach (var node in nodes)
            {
                if (node.Path == path)
                    return node;

                if (node.Children.Count > 0)
                {
                    var found = FindBranchNode(node.Children, path);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }
    }
}
