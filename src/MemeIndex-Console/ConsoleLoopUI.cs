using System.Diagnostics;
using MemeIndex_Core.Controllers;
using MemeIndex_Core.Model;
using MemeIndex_Core.Utils;
using Microsoft.Extensions.Hosting;

namespace MemeIndex_Console;

public class ConsoleLoopUI : IHostedService
{
    private readonly IndexController _indexController;
    private readonly SearchController _searchController;

    public ConsoleLoopUI(IndexController indexController, SearchController searchController)
    {
        _indexController = indexController;
        _searchController = searchController;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(Cycle, cancellationToken);
        _indexController.StartIndexing();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _indexController.StopIndexing();
        return Task.CompletedTask;
    }

    private void Cycle()
    {
        while (true)
        {
            try
            {
                var input = Console.ReadLine()?.Trim()!;

                if (input.StartsWith("/add "))
                {
                    var path = input[5..].Trim('"');
                    var op = new MonitoringOptions(path, true, MonitoringOptions.DefaultMeans);
                    _indexController.AddDirectory(op); //.Wait();
                    continue;
                }
                if (input.StartsWith("/rem "))
                {
                    _indexController.RemoveDirectory(input[5..].Trim('"')); //.Wait();
                    continue;
                }

                // if /search ...

                /*var path = input.Trim('"');

                if (!File.Exists(path))
                {
                    Logger.Log("File don't exist");
                    continue;
                }*/

                //var timer = new Stopwatch();

                // online ocr eng eng-2
                /*timer.Start();
                var text = _ocrService.GetTextRepresentation(path, "eng").Result;
                if (text is null) continue;
                Logger.Log(ConsoleColor.Blue, "Text:  \n{0}", string.Join('\n', text.OrderBy(x => x.Rank)));
                Logger.Log(ConsoleColor.Cyan, "Time: {0:F3}", timer.ElapsedMilliseconds / 1000F);*/

                // color tag
                //timer.Start();
                //var tags = _colorTagService.GetImageColorInfo(path);
                //Logger.Log(ConsoleColor.Blue, "Tags: {0}", tags);
                //Logger.Log(ConsoleColor.Cyan, "Time: {0:F3}", timer.ElapsedMilliseconds / 1000F);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Logger.Log("Unexpected Error: " + e.Message);
                Logger.Log("Details: ");
                Logger.Log(e.ToString());
            }
        }
    }
}