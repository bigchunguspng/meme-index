using System.Globalization;
using System.Text;
using MemeIndex_Core;
using MemeIndex_Core.Controllers;
using MemeIndex_Core.Data;
using MemeIndex_Core.Services;
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

            builder.Services.AddSingleton<Config>(_ => ConfigRepository.GetConfig());
            builder.Services.AddDbContext<MemeDbContext>(ServiceLifetime.Singleton, ServiceLifetime.Singleton);
            builder.Services.AddSingleton<IDirectoryService, DirectoryService>();
            builder.Services.AddSingleton<IFileService, FileService>();
            builder.Services.AddSingleton<FileWatchService>();
            builder.Services.AddSingleton<IOcrService, OnlineOcrService>();
            builder.Services.AddSingleton<ColorTagService>();
            builder.Services.AddSingleton<IndexingController>();
            builder.Services.AddSingleton<App>();

            using var host = builder.Build();

            DatabaseInitializer.EnsureCreated(host.Services.GetRequiredService<MemeDbContext>());

            var app = host.Services.GetRequiredService<App>();

            app.Start();
            app.Stop();
        }
    }
}