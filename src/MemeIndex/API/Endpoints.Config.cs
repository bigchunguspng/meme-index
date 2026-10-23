using System.Text.Json;
using MemeIndex.Core;
using MemeIndex.Utils;

namespace MemeIndex.API;

public static partial class Endpoints
{
    public static IResult Config_Get()
    {
        var json = JsonSerializer.Serialize(Config.API_Get(), AppJson.Default.API_Config!);
        return Results.Content(json, "application/json");
    }

    public static async Task<IResult> Config_Update
        (API_Config data)
    {
        var restart_needed = await Config.API_Update(data);
        return Results.Ok(restart_needed);
    }
}