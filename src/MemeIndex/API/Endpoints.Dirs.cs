using System.Text.Json;
using MemeIndex.Core.Indexing.Selection;
using MemeIndex.Utils;

namespace MemeIndex.API;

public static partial class Endpoints
{
    public static IResult GetJson_Directory
        (string? path, string? back, string? up)
    {
	    var directory = DirectorySelector.View(path);
	    if (directory == null) return Results.NotFound();

	    directory.B ??= back;
	    directory.U ??= up;

	    var json = JsonSerializer.Serialize(directory, AppJson.Default.DirectoryResponse);
	    return Results.Content(json, "application/json");
    }
}