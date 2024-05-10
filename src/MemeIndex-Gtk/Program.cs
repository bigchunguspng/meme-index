using System.Globalization;
using System.Text;
using MemeIndex_Core;
using MemeIndex_Core.Controllers;
using MemeIndex_Core.Data;
using MemeIndex_Core.Data.Entities;
using MemeIndex_Core.Services.Data;
using MemeIndex_Core.Services.Data.Contracts;
using MemeIndex_Core.Services.ImageToText;
using MemeIndex_Core.Services.ImageToText.ColorTag;
using MemeIndex_Core.Services.ImageToText.OCR;
using MemeIndex_Core.Services.Indexing;
using MemeIndex_Core.Services.Search;
using MemeIndex_Core.Utils;
using MemeIndex_Gtk.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemeIndex_Gtk
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Logger.Log(ConsoleColor.Magenta, "[Start]");

            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            if (Environment.OSVersion.Platform is PlatformID.Win32NT)
            {
                Console.InputEncoding = Encoding.Unicode;
            }

            var settings = new HostApplicationBuilderSettings
            {
                DisableDefaults = true,
                ApplicationName = "Meme-Index™"
            };
            var builder = new HostApplicationBuilder(settings); // 0.13-0.25 --> 0.03 sec

            builder.Logging.SetMinimumLevel(LogLevel.Warning);

            // DATABASE
            builder.Services.AddDbContext<MemeDbContext>(ServiceLifetime.Singleton, ServiceLifetime.Singleton);

            // LVL 1 SERVICES
            builder.Services.AddSingleton<IMonitoringService, MonitoringService>();
            builder.Services.AddSingleton<IDirectoryService, DirectoryService>();
            builder.Services.AddSingleton<IFileService, FileService>();
            builder.Services.AddSingleton<TagService>();

            // LVL 2 SERVICES
            builder.Services.AddSingleton<FileWatchService>();
            builder.Services.AddSingleton<OvertakingService>();
            builder.Services.AddSingleton<IndexingService>();
            builder.Services.AddSingleton<SearchService>();

            // CONTROLLERS
            builder.Services.AddSingleton<IndexController>();
            builder.Services.AddSingleton<SearchController>();

            // TAG HELPERS
            builder.Services.AddSingleton<ColorSearchProfile>();
            builder.Services.AddTransient<ImageGroupingService>();

            // TAG METHODS
            builder.Services.AddTransient<ColorTagService>();
            builder.Services.AddTransient<OnlineOcrService>();
            builder.Services.AddTransient<ImageToTextServiceResolver>(provider => key => key switch
            {
                Mean.RGB_CODE => provider.GetRequiredService<ColorTagService>(),
                Mean.ENG_CODE => provider.GetRequiredService<OnlineOcrService>(),
                _ => throw new ArgumentOutOfRangeException(nameof(key))
            });

            // CONFIG
            builder.Services.AddSingleton<IConfigProvider<Config>, ConfigProvider<ConfigGtk>>();
            builder.Services.AddSingleton<IConfigProvider<ConfigGtk>, ConfigProvider<ConfigGtk>>();

            // APP SPECIFIC
            builder.Services.AddSingleton<App>();
            builder.Services.AddTransient<CustomCss>();

            // LAZY
            builder.Services.AddLazy<IndexController>();
            builder.Services.AddLazy<SearchController>();
            builder.Services.AddLazy<MemeDbContext>();

            using var host = builder.Build(); // 0.13 --> 0.08 sec
            using var app = host.Services.GetRequiredService<App>(); // 0.20 --> 0.01 sec

            app.Start();
        }

        private static void AddLazy<T>(this IServiceCollection services) where T : notnull
        {
            services.AddTransient<Lazy<T>>(provider => new(provider.GetRequiredService<T>));
        }
    }
}