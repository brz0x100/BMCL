using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BMCL.Core.Configuration;
using BMCL.Desktop.ViewModels;
using BMCL.Desktop.Views;
using BMCL.Download;
using BMCL.Launch;
using BMCL.Launch.Java;
using Microsoft.Extensions.DependencyInjection;

namespace BMCL.Desktop;

public partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var paths = LauncherPaths.CreateDefault();
        paths.EnsureCreated();

        var services = new ServiceCollection();
        services.AddSingleton(paths);
        services.AddSingleton(new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30),
        });
        services.AddSingleton<IMirrorUrlMapper, IdentityMirrorUrlMapper>();
        services.AddSingleton<IDownloadService>(provider => new HttpDownloadService(
            provider.GetRequiredService<HttpClient>(),
            provider.GetRequiredService<IMirrorUrlMapper>()));
        services.AddSingleton<MinecraftVersionService>();
        services.AddSingleton<IJavaLocator, JavaLocator>();
        services.AddSingleton<LaunchCommandBuilder>();
        services.AddSingleton<MainWindowViewModel>();

        _services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = _services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = viewModel };
            viewModel.RefreshCommand.Execute(null);
            desktop.Exit += (_, _) => _services?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
