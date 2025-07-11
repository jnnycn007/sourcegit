using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace SourceGit.Views
{
    public partial class RepositoryToolbar : UserControl
    {
        public RepositoryToolbar()
        {
            InitializeComponent();
        }

        private void OpenWithExternalTools(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && DataContext is ViewModels.Repository repo)
            {
                var menu = repo.CreateContextMenuForExternalTools();
                menu?.Open(button);
                e.Handled = true;
            }
        }

        private async void OpenStatistics(object _, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo)
            {
                await App.ShowDialog(new ViewModels.Statistics(repo.FullPath));
                e.Handled = true;
            }
        }

        private async void OpenConfigure(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo)
            {
                await App.ShowDialog(new ViewModels.RepositoryConfigure(repo));
                e.Handled = true;
            }
        }

        private void Fetch(object _, RoutedEventArgs e)
        {
            var launcher = this.FindAncestorOfType<Launcher>();
            if (launcher is not null && DataContext is ViewModels.Repository repo)
            {
                var startDirectly = launcher.HasKeyModifier(KeyModifiers.Control);
                launcher.ClearKeyModifier();
                repo.Fetch(startDirectly);
                e.Handled = true;
            }
        }

        private void Pull(object _, RoutedEventArgs e)
        {
            var launcher = this.FindAncestorOfType<Launcher>();
            if (launcher is not null && DataContext is ViewModels.Repository repo)
            {
                if (repo.IsBare)
                {
                    App.RaiseException(repo.FullPath, "Can't run `git pull` in bare repository!");
                    return;
                }

                var startDirectly = launcher.HasKeyModifier(KeyModifiers.Control);
                launcher.ClearKeyModifier();
                repo.Pull(startDirectly);
                e.Handled = true;
            }
        }

        private void Push(object _, RoutedEventArgs e)
        {
            var launcher = this.FindAncestorOfType<Launcher>();
            if (launcher is not null && DataContext is ViewModels.Repository repo)
            {
                var startDirectly = launcher.HasKeyModifier(KeyModifiers.Control);
                launcher.ClearKeyModifier();
                repo.Push(startDirectly);
                e.Handled = true;
            }
        }

        private void StashAll(object _, RoutedEventArgs e)
        {
            var launcher = this.FindAncestorOfType<Launcher>();
            if (launcher is not null && DataContext is ViewModels.Repository repo)
            {
                var startDirectly = launcher.HasKeyModifier(KeyModifiers.Control);
                launcher.ClearKeyModifier();
                repo.StashAll(startDirectly);
                e.Handled = true;
            }
        }

        private void OpenGitFlowMenu(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo && sender is Control control)
            {
                var menu = repo.CreateContextMenuForGitFlow();
                menu?.Open(control);
            }

            e.Handled = true;
        }

        private void OpenGitLFSMenu(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo && sender is Control control)
            {
                var menu = repo.CreateContextMenuForGitLFS();
                menu?.Open(control);
            }

            e.Handled = true;
        }

        private async void StartBisect(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository { IsBisectCommandRunning: false, InProgressContext: null } repo &&
                repo.CanCreatePopup())
            {
                if (repo.LocalChangesCount > 0)
                    App.RaiseException(repo.FullPath, "You have un-committed local changes. Please discard or stash them first.");
                else if (repo.IsBisectCommandRunning || repo.BisectState != Models.BisectState.None)
                    App.RaiseException(repo.FullPath, "Bisect is running! Please abort it before starting a new one.");
                else
                    await repo.ExecBisectCommandAsync("start");
            }

            e.Handled = true;
        }

        private void OpenCustomActionMenu(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo && sender is Control control)
            {
                var menu = repo.CreateContextMenuForCustomAction();
                menu?.Open(control);
            }

            e.Handled = true;
        }

        private async void OpenGitLogs(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo)
            {
                await App.ShowDialog(new ViewModels.ViewLogs(repo));
                e.Handled = true;
            }
        }

        private void NavigateToHead(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository { CurrentBranch: not null } repo)
            {
                var repoView = TopLevel.GetTopLevel(this)?.FindDescendantOfType<Repository>();
                repoView?.LocalBranchTree?.Select(repo.CurrentBranch);

                repo.NavigateToCommit(repo.CurrentBranch.Head);
                e.Handled = true;
            }
        }
    }
}
