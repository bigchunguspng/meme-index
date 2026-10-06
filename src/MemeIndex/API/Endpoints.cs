using System.Text.Json;
using MemeIndex.Core;
using MemeIndex.Utils;

namespace MemeIndex.API;

public static partial class Endpoints
{
    public static IResult GetJson_Traces()
    {
        var files = Dir_Traces.GetFiles("*.txt").Select(Path.GetFileNameWithoutExtension);
        var json = JsonSerializer.Serialize(files, AppJson.Default.IEnumerableString!);
        return Results.Content(json, "application/json");
    }

    public static IResult GetText_TraceFile(string id)
    {
        var file = Dir_Traces.GetFiles($"{id}.txt").First();
        return Results.File(file, "text/plain");
    }

    public static IResult GetText_Errors()
    {
        return Results.File(File_Err, "text/plain");
    }

    public static async Task Get_SSE(HttpContext context)
    {
        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        try
        {
            var ct = context.RequestAborted;
            await SendEvent_Status(App.Status);
            await foreach (var message in App.C_Events.Reader.ReadAllAsync(ct))
            {
                ct.ThrowIfCancellationRequested();
                await SendEvent_Status(message);
            }

            async Task SendEvent_Status(string? status)
            {
                await context.Response.WriteAsync($"data: {status ?? App.DefaultStatus}\n\n", ct);
                await context.Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Sayonara...
        }
    }
}