using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BMCL.Desktop.ViewModels;
using BMCL.Desktop.Views.Dialogs;
using Material.Icons;

namespace BMCL.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        PropertyChanged += OnWindowPropertyChanged;
        SetupResizeCursors();
        UpdateWindowStateVisuals();
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WindowStateProperty)
        {
            UpdateWindowStateVisuals();
        }
    }

    private void UpdateWindowStateVisuals()
    {
        var isMaximized = WindowState is WindowState.Maximized or WindowState.FullScreen;

        if (MaximizeIcon is not null)
        {
            MaximizeIcon.Kind = isMaximized
                ? MaterialIconKind.WindowRestore
                : MaterialIconKind.WindowMaximize;
        }

        if (RootBorder is not null)
        {
            RootBorder.Margin = isMaximized ? default : new Thickness(24);
        }

        if (TopBarBackground is not null)
        {
            TopBarBackground.CornerRadius = isMaximized
                ? default
                : new CornerRadius(8, 8, 0, 0);
        }

        if (ContentBackgroundBorder is not null)
        {
            ContentBackgroundBorder.CornerRadius = isMaximized
                ? default
                : new CornerRadius(0, 0, 8, 8);
        }

        if (SidebarBorder is not null)
        {
            SidebarBorder.CornerRadius = isMaximized
                ? default
                : new CornerRadius(0, 0, 0, 8);
        }

        if (RightPanelBorder is not null)
        {
            RightPanelBorder.CornerRadius = isMaximized
                ? default
                : new CornerRadius(0, 0, 8, 0);
        }

        if (ShadowBorder is not null)
        {
            ShadowBorder.IsVisible = !isMaximized;
        }

        if (EdgeBorder is not null)
        {
            EdgeBorder.IsVisible = !isMaximized;
        }

        if (ResizeGripLayer is not null)
        {
            ResizeGripLayer.IsVisible = !isMaximized;
        }
    }

    private void SetupResizeCursors()
    {
        SetResizeCursor("ResizeTopLeft", StandardCursorType.TopLeftCorner);
        SetResizeCursor("ResizeTop", StandardCursorType.TopSide);
        SetResizeCursor("ResizeTopRight", StandardCursorType.TopRightCorner);
        SetResizeCursor("ResizeLeft", StandardCursorType.LeftSide);
        SetResizeCursor("ResizeRight", StandardCursorType.RightSide);
        SetResizeCursor("ResizeBottomLeft", StandardCursorType.BottomLeftCorner);
        SetResizeCursor("ResizeBottom", StandardCursorType.BottomSide);
        SetResizeCursor("ResizeBottomRight", StandardCursorType.BottomRightCorner);
    }

    private void SetResizeCursor(string name, StandardCursorType cursorType)
    {
        if (this.FindControl<Control>(name) is { } control)
        {
            control.Cursor = new Cursor(cursorType);
        }
    }

    private void OnResizePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { Tag: string tag } control ||
            !e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            return;
        }

        WindowEdge? edge = tag switch
        {
            "TopLeft" => WindowEdge.NorthWest,
            "Top" => WindowEdge.North,
            "TopRight" => WindowEdge.NorthEast,
            "Left" => WindowEdge.West,
            "Right" => WindowEdge.East,
            "BottomLeft" => WindowEdge.SouthWest,
            "Bottom" => WindowEdge.South,
            "BottomRight" => WindowEdge.SouthEast,
            _ => null,
        };

        if (edge is { } value)
        {
            BeginResizeDrag(value, e);
        }
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        BeginMoveDrag(e);
    }

    private void OnMinimizeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private async void OnOpenSettingsClick(object? sender, RoutedEventArgs e)
    {
        await ShowSettingsDialogAsync();
    }

    private async void OnOpenAccountClick(object? sender, RoutedEventArgs e)
    {
        await ShowAccountDialogAsync();
    }

    private async Task ShowSettingsDialogAsync()
    {
        if (DataContext is not MainWindowViewModel viewModel || viewModel.IsDialogOpen)
        {
            return;
        }

        viewModel.IsDialogOpen = true;
        var window = new SettingsWindow { DataContext = viewModel.SettingsPage };
        DialogSizing.ApplySettings(window, this);
        try
        {
            await window.ShowDialog(this);
        }
        catch (Exception exception)
        {
            viewModel.Status = $"Failed to open settings: {exception.Message}";
        }
        finally
        {
            viewModel.IsDialogOpen = false;
        }
    }

    private async Task ShowAccountDialogAsync()
    {
        if (DataContext is not MainWindowViewModel viewModel || viewModel.IsDialogOpen)
        {
            return;
        }

        viewModel.IsDialogOpen = true;
        var window = new AccountWindow { DataContext = viewModel.AccountPage };
        DialogSizing.ApplyAccount(window, this);
        try
        {
            await window.ShowDialog(this);
        }
        catch (Exception exception)
        {
            viewModel.Status = $"Failed to open accounts: {exception.Message}";
        }
        finally
        {
            viewModel.IsDialogOpen = false;
        }
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}
