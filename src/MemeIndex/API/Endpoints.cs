using System.Text.Json;
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
}