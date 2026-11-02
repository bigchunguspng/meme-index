using System.Text.Json;
using MemeIndex.Core;
using MemeIndex.Utils;

namespace MemeIndex.API;

public static partial class Endpoints
{
    public static async Task<IResult> Config_Get()
    {
        var config = await Config.SendTo_API();
        var json = JsonSerializer.Serialize(config, AppJson.Default.API_Config!);
        return Results.Content(json, "application/json");
    }

    public static async Task<IResult> Config_Put
        (API_Config data)
    {
        var restart_needed = await Config.ReceiveFrom_API(data);
        return Results.Ok(restart_needed);
    }
}