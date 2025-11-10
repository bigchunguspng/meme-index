using System.Globalization;
using System.Text;
using MemeIndex_Core;
using MemeIndex_Core.Controllers;
using MemeIndex_Core.Data;
using MemeIndex_Core.Data.Entities;
using MemeIndex_Core.Services.Data;
using MemeIndex_Core.Services.ImageAnalysis;
using MemeIndex_Core.Services.ImageAnalysis.Color;
using MemeIndex_Core.Services.ImageAnalysis.OCR;
using MemeIndex_Core.Services.Indexing;
using MemeIndex_Core.Services.Search;
using MemeIndex_Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemeIndex_Console;

internal static class Program
{
    public static void Main(string[] args)
    {
        Logger.Log("[Start]", ConsoleColor.Magenta);

        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        if (Environment.OSVersion.Platform is PlatformID.Win32NT)
        {
            Console.InputEncoding = Encoding.Unicode;
        }

        Logger.StatusChanged += (_, message) => Logger.Log(message ?? string.Empty);

        args.ForEachTry(Demo.ColorTagService_GetTextRepresentation);
        Console.ReadKey();
        return;

        var builder = Host.CreateApplicationBuilder(args);

        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        builder.Services.AddHostedService<ConsoleLoopUI>();

        builder.Services.AddDbContext<MemeDbContext>(ServiceLifetime.Singleton, ServiceLifetime.Singleton);

        builder.Services.AddSingleton<MonitoringService>();
        builder.Services.AddSingleton<DirectoryService>();
        builder.Services.AddSingleton<FileService>();
        builder.Services.AddSingleton<TagService>();

        builder.Services.AddSingleton<FileWatchService>();
        builder.Services.AddSingleton<OvertakingService>();
        builder.Services.AddSingleton<IndexingService>();
        builder.Services.AddSingleton<SearchService>();

        builder.Services.AddSingleton<IndexController>();
        builder.Services.AddSingleton<SearchController>();

        builder.Services.AddSingleton<ColorSearchProfile>();
        builder.Services.AddTransient<ImageGroupingService>();

        builder.Services.AddTransient<ColorTagService>();
        builder.Services.AddTransient<OnlineOcrService>();
        builder.Services.AddTransient<ImageToTextServiceResolver>(provider => key => key switch
        {
            Mean.RGB_CODE => provider.GetRequiredService<ColorTagService>(),
            Mean.ENG_CODE => provider.GetRequiredService<OnlineOcrService>(),
            _ => throw new ArgumentOutOfRangeException(nameof(key))
        });

        builder.Services.AddSingleton<IConfigProvider<Config>, ConfigProvider<Config>>();

        using var host = builder.Build();

        host.Services.GetRequiredService<MemeDbContext>().EnsureCreated();

        host.Run();

        Logger.Log("[Fin]", ConsoleColor.Magenta);
    }
}