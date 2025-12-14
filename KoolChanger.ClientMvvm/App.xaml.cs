#region

using KoolChanger.ClientMvvm.Interfaces;
using KoolChanger.ClientMvvm.Services;
using KoolChanger.ClientMvvm.ViewModels;
using KoolChanger.ClientMvvm.ViewModels.Dialogs;
using KoolChanger.ClientMvvm.Views.Dialogs;
using KoolChanger.ClientMvvm.Views.Windows;
using KoolChanger.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Animation;



#endregion


namespace KoolChanger.ClientMvvm;

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
       
        services.AddTransient<MainWindow>();
        services.AddTransient<CustomMessageBox>();
        services.AddTransient<CustomSkinsForm>();
        services.AddTransient<SettingsForm>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var navigationService = _serviceProvider.GetRequiredService<INavigationService>();

        navigationService.Register<MainViewModel, MainWindow>();
        navigationService.Register<CustomMessageBoxViewModel, CustomMessageBox>();
        navigationService.Register<SettingsViewModel, SettingsForm>();
        navigationService.Register<CustomSkinsViewModel, CustomSkinsForm>();

        navigationService.NavigateTo<MainViewModel>();

        base.OnStartup(e);
         
      
    }
}
