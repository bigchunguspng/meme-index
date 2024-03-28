using System.Globalization;
using MemeIndex_Core;
using MemeIndex_Core.Data;
using MemeIndex_Core.Services;
using MemeIndex_Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MemeIndex_Console;

internal static class Program
{
    public static void Main(string[] args)
    {
        Logger.Log("[Start]", ConsoleColor.Magenta);

        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddHostedService<ConsoleLoopUI>();

        builder.Services.AddSingleton<Config>(_ => ConfigRepository.GetConfig());
        builder.Services.AddDbContext<MemeDbContext>(ServiceLifetime.Singleton, ServiceLifetime.Singleton);
        builder.Services.AddSingleton<IDirectoryService, DirectoryService>();
        builder.Services.AddSingleton<IFileService, FileService>();
        builder.Services.AddSingleton<FileWatchService>();
        builder.Services.AddSingleton<IOcrService, OnlineOcrService>();

        using var host = builder.Build();

        DatabaseInitializer.EnsureCreated(host.Services.GetRequiredService<MemeDbContext>());

        host.Run();

        Logger.Log("[Fin]", ConsoleColor.Magenta);
    }
}