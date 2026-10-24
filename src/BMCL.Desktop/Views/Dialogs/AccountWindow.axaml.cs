using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BMCL.Desktop.Views.Dialogs;

public partial class AccountWindow : Window
{
    public AccountWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
