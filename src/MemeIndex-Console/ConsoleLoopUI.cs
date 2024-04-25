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
                    var split = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                    var path = split[1].Trim('"');
                    var recursive = split.Length <= 2 || split[2].Contains("-r");
                    var eng = split.Length > 2 && split[2].Contains("eng");
                    var rgb = split.Length > 2 && split[2].Contains("rgb");
                    var means = eng || rgb
                        ? new MonitoringOptions.MeansBuilder().WithRgb(rgb).WithEng(eng).Build()
                        : MonitoringOptions.DefaultMeans;
                    var op = new MonitoringOptions(path, recursive, means);
                    _indexController.AddDirectory(op).Wait();
                    continue;
                }
                if (input.StartsWith("/rem "))
                {
                    _indexController.RemoveDirectory(input[5..].Trim('"')).Wait();
                    continue;
                }
                if (input.StartsWith("/upd "))
                {
                    var split = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                    var path = split[1].Trim('"');
                    var recursive = split.Length <= 2 || split[2].Contains("-r");
                    var eng = split.Length > 2 && split[2].Contains("eng");
                    var rgb = split.Length > 2 && split[2].Contains("rgb");
                    var means = eng || rgb
                        ? new MonitoringOptions.MeansBuilder().WithRgb(rgb).WithEng(eng).Build()
                        : MonitoringOptions.DefaultMeans;
                    var op = new MonitoringOptions(path, recursive, means);
                    _indexController.UpdateDirectory(op).Wait();
                    continue;
                }

                if (input.StartsWith("/s ")) // /s -2& me when -1| b0 g2
                {
                    var split = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    var query = split[1];
                    var qbm = query.Split('-', StringSplitOptions.RemoveEmptyEntries);
                    var queries = qbm.Select(x =>
                    {
                        var meanId = int.Parse(x[0].ToString());
                        var words = x.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1);
                        var op = x[1] == '&' ? LogicalOperator.AND : LogicalOperator.OR;
                        return new SearchQuery(meanId, words, op);
                    });
                    var files = _searchController.Search(queries, LogicalOperator.AND).Result;
                    if (files.Count == 0) continue;

                    foreach (var file in files)
                    {
                        Logger.Log(ConsoleColor.Yellow, $"{file.Directory.Path}{Path.DirectorySeparatorChar}{file.Name}");
                    }
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