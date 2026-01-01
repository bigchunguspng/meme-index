using MemeIndex.Core.Indexing;
using MemeIndex.Core.OpeningFiles;
using MemeIndex.DB;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex.API;

public static partial class Endpoints
{
    public static async Task<IResult> Get_Image(int id)
    {
        var path = await GetFilePath(id);
        if (path == null)
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

    public static async Task<IResult> Image_Open(int id)
    {
        var path = await GetFilePath(id);
        if (path == null)
            return Results.NotFound();

        FileOpener.OpenFileWithDefaultApp(path);
        return Results.Ok();
    }

    public static async Task<IResult> Image_OpenInExplorer(int id)
    {
        var path = await GetFilePath(id);
        if (path == null)
            return Results.NotFound();

        FileOpener.ShowFileInExplorer(path);
        return Results.Ok();
    }

    //

    private static async Task<string?> GetFilePath(int id)
    {
        var con = await AppDB.ConnectTo_Main();
        var file = await con.File_GetPath(id);
        if (file == null)
            return null;

        var path = file.GetPath();
        if (File.Exists(path).Janai())
            return null;

        return path;
    }

    private static async Task<IResult> GetImage_AsPng(string path)
    {
        using var image = Image.Load<Rgba32>(path);
        var memory = new MemoryStream();
        await image.SaveAsPngAsync(memory);
        memory.Position = 0;

        return Results.File(memory, "image/png");
    }
}