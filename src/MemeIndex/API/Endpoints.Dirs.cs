using System.Text.Json;
using MemeIndex.Core.Indexing.Selection;
using MemeIndex.Utils;

namespace MemeIndex.API;

public static partial class Endpoints
{
    public static IResult GetJson_Directory
        (string? path, string? up)
    {
	    var directory = DirectorySelector.View(path);
	    if (directory == null) return Results.NotFound();

	    directory.U ??= up;

	    var json = JsonSerializer.Serialize(directory, AppJson.Default.DirectoryResponse);
	    return Results.Content(json, "application/json");
    }

    public static IResult Monitors_Save
	    (API_Monitors_Post body)
    {
	    // todo update db monitors, trigger indexing
	    return Results.Ok();
    }
}

public class API_Monitors_Post
{
	public List<API_Monitor_Post> M { get; set; } // Monitors

}
public class API_Monitor_Post
{
	public string                       P { get; set; } // Path
	public List<API_MonitorMethod_Post> M { get; set; } // Methods
}
public class API_MonitorMethod_Post
{
	public bool I { get; set; } // Indexed
	public bool E { get; set; } // Enabled
	public bool R { get; set; } // Recursive
}