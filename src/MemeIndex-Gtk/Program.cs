using System.Globalization;
using System.Text;
using MemeIndex_Core.Controllers;
using MemeIndex_Core.Data;
using MemeIndex_Core.Services.Data;
using MemeIndex_Core.Services.Indexing;
using MemeIndex_Core.Services.OCR;
using MemeIndex_Core.Services.Search;
using MemeIndex_Gtk.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MemeIndex_Gtk
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            if (Environment.OSVersion.Platform is PlatformID.Win32NT)
            {
                Console.InputEncoding = Encoding.Unicode;
            }

            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddDbContext<MemeDbContext>(ServiceLifetime.Singleton, ServiceLifetime.Singleton);

            builder.Services.AddSingleton<IMonitoringService, MonitoringService>();
            builder.Services.AddSingleton<IDirectoryService, DirectoryService>();
            builder.Services.AddSingleton<IFileService, FileService>();

            builder.Services.AddSingleton<FileWatchService>();
            builder.Services.AddSingleton<OvertakingService>();
            builder.Services.AddSingleton<IndexingService>();
            builder.Services.AddSingleton<SearchService>();

            builder.Services.AddSingleton<IndexController>();
            builder.Services.AddSingleton<SearchController>();

            builder.Services.AddSingleton<ColorTagService>();
            builder.Services.AddSingleton<OnlineOcrService>();
            builder.Services.AddTransient<OcrServiceResolver>(provider => key => key switch
            {
                DatabaseInitializer.RGB_CODE => provider.GetRequiredService<ColorTagService>(),
                DatabaseInitializer.ENG_CODE => provider.GetRequiredService<OnlineOcrService>(),
                _ => throw new ArgumentOutOfRangeException(nameof(key))
            });

            builder.Services.AddSingleton<App>();
            builder.Services.AddTransient<CustomCss>();

            using var host = builder.Build();
            using var app = host.Services.GetRequiredService<App>();

            app.Start();
        }
    }
}