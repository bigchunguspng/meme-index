using MemeIndex.Core.Indexing;
using MemeIndex.DB;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex.API;

public static partial class Endpoints
{
    public static async Task<IResult> Get_Image(int id)
    {
        var con = await AppDB.ConnectTo_Main();
        var file = await con.File_GetPath(id);
        if (file == null)
            return Results.NotFound();

        var path = file.GetPath();
        if (File.Exists(path).Janai())
            return Results.NotFound();

        return await GetImage_AsPng(path);
    }

    public static async Task<IResult> Get_Thumb(int id)
    {
        var path = Dir_Thumbs.Combine(FileProcessor.GetThumbFilename(id));
        if (File.Exists(path).Janai())
            return Results.NotFound();

        return await GetImage_AsPng(path);
    }

    private static async Task<IResult> GetImage_AsPng(string path)
    {
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