using MemeIndex.Core.Indexing;
using MemeIndex.DB;

namespace MemeIndex.API;

public static partial class Endpoints
{
    // THUMBS

    public static async Task<IResult> Thumbs_Regenerate()
    {
        await using var con = await AppDB.ConnectTo_Main();
        await con.Files_ResetThumbDate();
        FileProcessor.Trigger();

        return Results.Ok();
    }

    public static async Task<IResult> Thumbs_Delete()
    {
        await using var con = await AppDB.ConnectTo_Main();
        await con.Thumbs_Delete();
        await con.VACUUM();

        return Results.Ok();
    }
}