using System.Text.Json;
using MemeIndex.Core.Indexing;
using MemeIndex.Core.Indexing.Selection;
using MemeIndex.DB;
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

    public static async Task<IResult> Monitors_Get()
    {
	    await using var con = await AppDB.ConnectTo_Main();
	    var db_monitors = await con.Monitors_GetAll();
	    var response = new API_Monitors
	    {
		    M = db_monitors
			    .GroupBy(x => x.path)
			    .Select(g => new API_MonitorsByPath
			    {
				    P = g.Key,
				    M = g.Select(x => new API_Monitor
				    {
					    M = x.method.ClampByte(),
					    E = x.enabled,
					    R = x.recurse,
				    }).ToList()
			    }).ToList()
	    };
	    var json = JsonSerializer.Serialize(response, AppJson.Default.API_Monitors);
	    return Results.Content(json, "application/json");
    }

    public static async Task<IResult> Monitors_Put
	    (API_Monitors body)
    {
	    var response = await MonitorsDispatcher.UpdateMonitors(body);
	    var json = JsonSerializer.Serialize(response, AppJson.Default.API_Monitors_Put_Response);
	    return Results.Content(json, "application/json");
    }
}

public class API_Monitors
{
	public required List<API_MonitorsByPath> M { get; set; } // Monitors
}
public class API_MonitorsByPath
{
	public required string            P { get; set; } // Pats
	public required List<API_Monitor> M { get; set; } // Methods
}
public class API_Monitor
{
	// todo int id (I): 1+ for existing, 0 for new
	// ^ new path (P) for I>0 = directory was relocated => update its path
	public byte M { get; set; } // Method
	public bool E { get; set; } // Enabled
	public bool R { get; set; } // Recursive
}

public class API_Monitors_Put_Response
{
	public int A { get; set; } // Added
	public int U { get; set; } // Updated
	public int D { get; set; } // Deleted
}