using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BMCL.Desktop.Views.Dialogs;

public partial class VersionInstallWindow : Window
{
    public VersionInstallWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
