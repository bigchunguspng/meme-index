using System.Diagnostics;
using MemeIndex_Core.Controllers;
using MemeIndex_Core.Services;
using MemeIndex_Core.Utils;
using Microsoft.Extensions.Hosting;

namespace MemeIndex_Console;

public class ConsoleLoopUI : IHostedService
{
    private readonly IOcrService _service;
    private readonly IndexingController _controller;

    public ConsoleLoopUI(IOcrService service, IndexingController controller)
    {
        _service = service;
        _controller = controller;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(Cycle, cancellationToken);
        _controller.StartIndexing();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _controller.StopIndexing();
        return Task.CompletedTask;
    }

    private void Cycle()
    {
        try
        {
            while (true)
            {
                var input = Console.ReadLine()?.Trim()!;

                if (input.StartsWith("/add "))
                {
                    _controller.AddDirectory(input[5..].Trim('"')).Wait();
                    continue;
                }
                if (input.StartsWith("/rem "))
                {
                    _controller.RemoveDirectory(input[5..].Trim('"')).Wait();
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
                timer.Start();
                var text = _service.GetTextRepresentation(path, "eng").Result;
                Logger.Log(ConsoleColor.Blue, "Text: {0}", text);
                Logger.Log(ConsoleColor.Cyan, "Time: {0:F3}", timer.ElapsedMilliseconds / 1000F);
            }
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