using MemeIndex.DB;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex.API;

public static partial class Endpoints
{
    public static async Task<IResult> Get_Image(string id)
    {
        var con = await AppDB.ConnectTo_Main();
        var file = await con.File_GetPath(id);
        if (file == null)
            return Results.NotFound();

        var path = file.GetPath();
        if (File.Exists(path).Janai())
            return Results.NotFound();

        using var image = Image.Load<Rgba32>(path);
        var memory = new MemoryStream();
        await image.SaveAsPngAsync(memory);
        memory.Position = 0;

        return Results.File(memory, "image/png");
    }

    public static IResult Image_Open(string id)
    {
        throw new NotImplementedException();
    }

    public static IResult Image_OpenInExplorer(string id)
    {
        throw new NotImplementedException();
    }
}