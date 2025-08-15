using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace SourceGit.Views
{
    public partial class GitFlowView : UserControl
    {
        public GitFlowView()
        {
            InitializeComponent();
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

        private void OnBranchDoubleTapped(object sender, TappedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo && sender is ListBox listBox)
            {
                var selected = listBox.SelectedItem as Models.Branch;
                if (selected != null && !selected.IsCurrent)
                {
                    repo.CheckoutBranch(selected);
                }
            }
        }
    }
}