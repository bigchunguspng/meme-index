using System.Text.Json;
using MemeIndex.Core.Search;
using MemeIndex.Utils;

namespace MemeIndex.API;

public static partial class Endpoints
{
    public static async Task<IResult> GetJson_Find(string? color, string? text, int skip = 0, int take = 100)
    {
        if (color != null)
        {
            var tags = await Jarvis.Search_ByColor(color, skip, take);
            var json = JsonSerializer.Serialize(tags, AppJson.Default.SearchResponse);
            return Results.Content(json, "application/json");
        }

        // ignore text for now
        throw new NotImplementedException();
    }
}