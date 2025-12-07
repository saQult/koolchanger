using KoolChanger.Backend.Hubs;
using KoolChanger.Backend.Services;
using NLog;
using LogLevel = NLog.LogLevel;

namespace KoolChanger.Backend;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var urls = builder.Configuration["HostUrls"]?.Split(";");
        builder.WebHost.UseKestrelHttpsConfiguration().UseUrls(urls ?? ["http://0.0.0.0:5000"]);

        var config = new NLog.Config.LoggingConfiguration();

        // Targets where to log to: File and Console
        var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

        // Rules for mapping loggers to targets
        config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);

        // Apply config
        NLog.LogManager.Configuration = config;

        builder.Services.AddLogging();
        builder.Services.AddSignalR();
        builder.Services.AddSingleton<ILobbyService, InMemoryLobbyService>();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        var app = builder.Build();

        app.UseCors();

        app.MapHub<LobbyHub>("/lobbyhub");
        app.MapGet("/", () => "hello");

        app.Run();
    }
}