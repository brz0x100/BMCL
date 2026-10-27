using Avalonia.Controls;
using Avalonia.Interactivity;
using BMCL.Core.Instances;
using BMCL.Desktop.ViewModels;
using BMCL.Desktop.Views.Dialogs;

namespace BMCL.Desktop.Views.Pages;

public partial class InstancesView : UserControl
{
    public InstancesView()
    {
        InitializeComponent();
    }

    private async void OnInstanceSettingsClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: Instance instance } ||
            DataContext is not InstancesViewModel viewModel)
        {
            return;
        }

        if (TopLevel.GetTopLevel(this) is not Window owner)
        {
            return;
        }

        var shell = owner.DataContext as MainWindowViewModel;
        if (shell?.IsDialogOpen == true)
        {
            return;
        }

        viewModel.SelectedInstance = instance;
        if (shell is not null)
        {
            shell.IsDialogOpen = true;
        }

        try
        {
            var window = new InstanceSettingsWindow
            {
                DataContext = viewModel.CreateSettingsViewModel(instance),
            };
            DialogSizing.ApplySettings(window, owner);
            await window.ShowDialog(owner);
            await viewModel.LoadAsync();
            viewModel.SelectInstance(instance.Id);
        }
        catch (Exception exception)
        {
            viewModel.Status = $"Failed to open instance settings: {exception.Message}";
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
