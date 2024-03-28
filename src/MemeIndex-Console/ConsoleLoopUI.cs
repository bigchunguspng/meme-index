using System.Diagnostics;
using MemeIndex_Core.Services;
using MemeIndex_Core.Utils;
using Microsoft.Extensions.Hosting;

namespace MemeIndex_Console;

public class ConsoleLoopUI : IHostedService
{
    private readonly IOcrService _service;

    public ConsoleLoopUI(IOcrService service)
    {
        _service = service;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(Cycle, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void Cycle()
    {
        try
        {
            while (true)
            {
                var input = Console.ReadLine()?.Trim().Trim('"');

                if (!File.Exists(input))
                {
                    Logger.Log("File don't exist");
                    continue;
                }

                var timer = new Stopwatch();

                // online ocr eng eng-2
                timer.Start();
                var text = _service.GetTextRepresentation(input, "eng").Result;
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