using MemeIndex.Core.Indexing;
using MemeIndex.DB;

namespace MemeIndex.API;

public static partial class Endpoints
{
    // THUMBS

    public static async Task<IResult> Thumbs_Regenerate()
    {
        DeleteThumbnails();

        await using var con = await AppDB.ConnectTo_Main();
        await con.Files_ResetThumbDate();
        await FileProcessor.Trigger();

        return Results.Ok();
    }

    public static IResult Thumbs_Delete()
    {
        DeleteThumbnails();
        return Results.Ok();
    }

    /// Delete thumbs for removed ids.
    /// Delete older thumbs for existing ids.
    public static async Task<IResult> Thumbs_DeleteUnused()
    {
        await using var con = await AppDB.ConnectTo_Main();
        var db_file_ids = await con.Files_GetIds();
        var time_by_file_id = db_file_ids.ToDictionary(id => id, _ => "");

        foreach (var path in Dir_Thumbs.EnumerateFiles("*.webp"))
        {
            var name = new FilePath(path).FileNameWithoutExtension;

            var i_separator = name.IndexOf('-');
            if (i_separator < 0)
                continue;

            var              id_raw = name[..i_separator];
            if (int.TryParse(id_raw, out var id).Failed())
                continue;

            if (time_by_file_id.TryGetValue(id, out var time_stored)) // id exist
            {
                var time_curr = name[(i_separator + 1)..];

                if (time_stored == "") // first thumb for given id
                {
                    time_by_file_id[id] = time_curr;
                }
                else // value stores time for this id we already checked
                {
                    var curr_is_newer = string.Compare(time_curr, time_stored, StringComparison.Ordinal) > 0;
                    if (curr_is_newer)
                    {
                        time_by_file_id[id] = time_curr;
                        Dir_Thumbs.Combine($"{id}-{time_stored}.webp").DeleteFile();
                    }
                    else File.Delete(path);
                }
            }
            else File.Delete(path); // id doesn't exist anymore
        }

        return Results.Ok();
    }

    private static void DeleteThumbnails()
    {
        foreach (var path in Dir_Thumbs.EnumerateFiles("*.webp"))
            File.Delete(path);
    }
}

// check - can we just delete thumbs dir when app is running?