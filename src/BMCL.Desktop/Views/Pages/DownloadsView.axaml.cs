using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BMCL.Core.Minecraft;
using BMCL.Desktop.ViewModels;
using BMCL.Desktop.Views.Dialogs;

namespace BMCL.Desktop.Views.Pages;

public partial class DownloadsView : UserControl
{
    public DownloadsView()
    {
        InitializeComponent();
    }

    private async void OnVersionDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: VersionSummary version } &&
            DataContext is DownloadsViewModel viewModel)
        {
            await ShowVersionWindowAsync(viewModel, version);
        }
    }

    private async void OnVersionArrowClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: VersionSummary version } &&
            DataContext is DownloadsViewModel viewModel)
        {
            viewModel.SelectedVersion = version;
            await ShowVersionWindowAsync(viewModel, version);
        }
    }

    private async Task ShowVersionWindowAsync(DownloadsViewModel viewModel, VersionSummary version)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
        {
            return;
        }

        var shell = owner.DataContext as MainWindowViewModel;
        if (shell?.IsDialogOpen == true)
        {
            return;
        }

        if (shell is not null)
        {
            shell.IsDialogOpen = true;
        }

        try
        {
            var window = new VersionInstallWindow
            {
                DataContext = viewModel.CreateInstallViewModel(version),
            };
            DialogSizing.ApplySettings(window, owner);
            await window.ShowDialog(owner);
        }
        catch (Exception exception)
        {
            viewModel.Status = $"Failed to open the install dialog: {exception.Message}";
        }
        finally
        {
            if (shell is not null)
            {
                shell.IsDialogOpen = false;
            }
        }
    }
}
