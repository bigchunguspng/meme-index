using MemeIndex.Core.Search.Actions;
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
        await using var con = await AppDB.ConnectTo_Main();
        var bytes = await con.Thumb_Get(id);
        if (bytes.Length == 0)
            return Results.NotFound();

        return await GetImage_AsPng(bytes);
    }

    public static async Task<IResult> Get_Thumb_WEBP
        (HttpResponse response, int id)
    {
        await using var con = await AppDB.ConnectTo_Main();
        var bytes = await con.Thumb_Get(id);
        if (bytes.Length == 0)
            return Results.NotFound();

        response.Headers.CacheControl = "public,max-age=31536000,immutable";

        return Results.File(bytes, "image/webp");
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

    private static async Task<IResult> GetImage_AsPng(byte[] bytes)
    {
        using var image = Image.Load<Rgba32>(bytes);
        return await GetImage_AsPng(image);
    }

    private static async Task<IResult> GetImage_AsPng(string path)
    {
        using var image = Image.Load<Rgba32>(path);
        return await GetImage_AsPng(image);
    }

    private static async Task<IResult> GetImage_AsPng(Image<Rgba32> image)
    {
        var memory = new MemoryStream();
        await image.SaveAsPngAsync(memory);
        memory.Position = 0;

        return Results.File(memory, "image/png");
    }
}