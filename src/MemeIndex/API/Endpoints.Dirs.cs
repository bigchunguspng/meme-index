using System.Text.Json;
using MemeIndex.Core.Indexing;
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

    public static async Task<IResult> Monitors_Save
	    (API_Monitors_Post_Request body)
    {
	    var response = await MonitorsDispatcher.UpdateMonitors(body);
	    var json = JsonSerializer.Serialize(response, AppJson.Default.API_Monitors_Post_Response);
	    return Results.Content(json, "application/json");
    }
}

public class API_Monitors_Post_Request
{
	public List<API_MonitorsByPath_Post> M { get; set; } // Monitors
}
public class API_MonitorsByPath_Post
{
	public string                 P { get; set; } // Path
	public List<API_Monitor_Post> M { get; set; } // Methods
}
public class API_Monitor_Post
{
	// todo int id (I): 1+ for existing, 0 for new
	// ^ new path (P) for I>0 = directory was relocated => update its path
	public byte M { get; set; } // Method
	public bool E { get; set; } // Enabled
	public bool R { get; set; } // Recursive
}

public class API_Monitors_Post_Response
{
	public int A { get; set; } // Added
	public int U { get; set; } // Updated
	public int D { get; set; } // Deleted
}