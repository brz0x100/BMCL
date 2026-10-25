using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BMCL.Desktop.Views.Dialogs;

public partial class InstanceSettingsWindow : Window
{
    public InstanceSettingsWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
