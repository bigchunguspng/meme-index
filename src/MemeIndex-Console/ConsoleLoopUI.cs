using System.Diagnostics;
using MemeIndex_Core.Services.Indexing;
using MemeIndex_Core.Utils;
using Microsoft.Extensions.Hosting;

namespace MemeIndex_Console;

public class ConsoleLoopUI : IHostedService
{
    private readonly IndexingService _service;
    private readonly IOcrService _ocrService;
    private readonly ColorTagService _colorTagService;

    public ConsoleLoopUI(IndexingService service, IOcrService ocrService, ColorTagService colorTagService)
    {
        _service = service;
        _ocrService = ocrService;
        _colorTagService = colorTagService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(Cycle, cancellationToken);
        _service.StartIndexing();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _service.StopIndexing();
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
                    _service.AddDirectory(input[5..].Trim('"')).Wait();
                    continue;
                }
                if (input.StartsWith("/rem "))
                {
                    _service.RemoveDirectory(input[5..].Trim('"')).Wait();
                    continue;
                }

                var path = input.Trim('"');

                if (!File.Exists(path))
                {
                    Logger.Log("File don't exist");
                    continue;
                }

                var timer = new Stopwatch();

                // online ocr eng eng-2
                //timer.Start();
                //var text = _ocrService.GetTextRepresentation(path, "eng").Result;
                //Logger.Log(ConsoleColor.Blue, "Text: {0}", text);
                //Logger.Log(ConsoleColor.Cyan, "Time: {0:F3}", timer.ElapsedMilliseconds / 1000F);

                // color tag
                timer.Start();
                var tags = _colorTagService.GetImageColorInfo(path);
                Logger.Log(ConsoleColor.Blue, "Tags: {0}", tags);
                Logger.Log(ConsoleColor.Cyan, "Time: {0:F3}", timer.ElapsedMilliseconds / 1000F);
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