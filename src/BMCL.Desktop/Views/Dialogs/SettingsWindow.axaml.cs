using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BMCL.Desktop.Views.Dialogs;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
