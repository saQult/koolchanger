#region

using KoolChanger.Client.Interfaces;
using KoolChanger.Client.Services;
using KoolChanger.Client.ViewModels;
using KoolChanger.Client.ViewModels.Dialogs;
using KoolChanger.Client.Views.Dialogs;
using KoolChanger.Client.Views.Pages;
using KoolChanger.Client.Views.Windows;
using KoolChanger.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

#endregion


namespace KoolChanger.Client;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        IServiceCollection services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<IDataInitializationService, DataInitializationService>();
        services.AddSingleton<IFilesystemService, FilesystemService>();
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IFolderBrowserService, FolderBrowserService>();
        services.AddSingleton<IToolServiceFactory, ToolServiceFactory>();
        services.AddSingleton<ICustomSkinServiceFactory, CustomSkinServiceFactory>();
        services.AddSingleton<IPartyServiceFactory, PartyServiceFactory>();

        services.AddSingleton<SkinService>();
        services.AddSingleton<ChampionService>();
        services.AddSingleton<UpdateService>();
        services.AddSingleton<LCUService>();

        services.AddSingleton<MainViewModel>();
        services.AddTransient<CustomMessageBoxViewModel>();
        services.AddTransient<CustomSkinsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<TestViewModel>();
        services.AddTransient<SkinsPageViewModel>();

        services.AddTransient<MainWindow>();
        services.AddTransient<CustomMessageBox>();
        services.AddTransient<CustomSkinsForm>();
        services.AddTransient<SettingsForm>();
        services.AddTransient<TestWindow>();

        services.AddTransient<SettingsPage>();
        services.AddTransient<SkinsPage>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var navigationService = _serviceProvider.GetRequiredService<INavigationService>();

        navigationService.Register<MainViewModel, MainWindow>();
        navigationService.Register<CustomMessageBoxViewModel, CustomMessageBox>();
        navigationService.Register<SettingsViewModel, SettingsForm>();
        navigationService.Register<CustomSkinsViewModel, CustomSkinsForm>();
        navigationService.Register<TestViewModel, TestWindow>();

        navigationService.RegisterPage<SettingsViewModel, SettingsPage>();
        navigationService.RegisterPage<SkinsPageViewModel, SkinsPage>();

        navigationService.NavigateTo<TestViewModel>();

        base.OnStartup(e);
    }
}
