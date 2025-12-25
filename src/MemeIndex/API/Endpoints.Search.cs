using System.Text.Json;
using MemeIndex.Core.Search;
using MemeIndex.Utils;

namespace MemeIndex.API;

public static partial class Endpoints
{
    public static async Task<IResult> GetJson_Find(string? color, string? text)
    {
        if (color != null)
        {
            var colors = color.Split(' ');
            // ignore mulipliers for now

            var tags = await Jarvis.Search(colors);
            var json = JsonSerializer.Serialize(tags, AppJson.Default.IEnumerableFile_UI);
            return Results.Content(json, "application/json");
        }

        // ignore text for now
        throw new NotImplementedException();
    }
}