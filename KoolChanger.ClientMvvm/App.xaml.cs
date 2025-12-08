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
        //string path = Path.Combine(new FileInfo(Environment.ProcessPath).Directory.FullName, "Wrapper.dll");
        //var t = new FileInfo(path);
        //Console.WriteLine(t.Exists);
        //Assembly sampleDLL = Assembly.LoadFile(path);
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

        services.AddSingleton<KoolService>();

        services.AddSingleton<ToolService>(serviceProvider =>
        {
            var mainVm = serviceProvider.GetRequiredService<MainViewModel>();
            var config = serviceProvider.GetRequiredService<IConfigService>().LoadConfig();
            return new ToolService(config.GamePath);
        });
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