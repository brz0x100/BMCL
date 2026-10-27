using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BMCL.Auth;
using BMCL.Core.Configuration;
using BMCL.Core.Instances;
using BMCL.Desktop.Services;
using BMCL.Desktop.ViewModels;
using BMCL.Desktop.Views;
using BMCL.Download;
using BMCL.Install;
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
            Timeout = TimeSpan.FromMinutes(10),
        });
        services.AddSingleton<LauncherSettingsStore>();
        services.AddSingleton<LauncherSettingsService>();
        services.AddSingleton<IMirrorUrlMapper, LauncherMirrorUrlMapper>();
        services.AddSingleton<IDownloadService>(provider => new HttpDownloadService(
            provider.GetRequiredService<HttpClient>(),
            provider.GetRequiredService<IMirrorUrlMapper>(),
            () => provider.GetRequiredService<LauncherSettingsService>()
                .Settings.MaxConcurrentDownloads));
        services.AddSingleton<MinecraftVersionService>();
        services.AddSingleton<MinecraftInstaller>();
        services.AddSingleton<IJavaLocator, JavaLocator>();
        services.AddSingleton<LaunchCommandBuilder>();
        services.AddSingleton<GameLaunchService>();
        services.AddSingleton(new AccountStore(Path.Combine(paths.RootDirectory, "accounts.json")));
        services.AddSingleton<AccountManager>();
        services.AddSingleton<InstanceStore>();
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<InstancesViewModel>();
        services.AddSingleton<DownloadsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<AccountViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        _services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = _services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = viewModel };
            _ = viewModel.InitializeAsync();
            desktop.Exit += (_, _) => _services?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
