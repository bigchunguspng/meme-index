using System.Threading.Channels;
using MemeIndex.DB;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace MemeIndex.Core.Indexing;

public partial class FileProcessingTask
{
    private static readonly Size _fitSize = new (275, 180); // Similar to Google Images thumb size.

    public async Task GenerateThumbnails()
    {
        Log("GenerateThumbnails", "START");

        // GET FILES
        await using var con = await AppDB.ConnectTo_Main();
        var files = await con.Files_GetToBeThumbed();
        await con.CloseAsync();
        Log("GenerateThumbnails", "GET FILES");

        var filesIP = files
            .Select(x => new { Id = x.id, Path = x.GetPath() })
            .ToArray();
        ImagePool.Book(filesIP.Select(x => x.Path), filesIP.Length);

        foreach (var file in filesIP)
        {
            try
            {
                var ctx = new ThumbgenContext
                {
                    Path = file.Path,
                    FileId = file.Id,
                };
                //await Thumbnail_Load(ctx);
                await Thumbnail_Resize(ctx);
            }
            catch (Exception e)
            {
                LogError(e);
                // todo add file id to broken files
            }
        }

        C_SaveWebp.Writer.Complete();

        Log("GenerateThumbnails", "DONE");
    }

    private static readonly WebpEncoder _encoder = new()
    {
        FileFormat = WebpFileFormatType.Lossy,
        Quality = 85,
        Method = WebpEncodingMethod.Level2,
        TransparentColorMode = WebpTransparentColorMode.Clear,
    };

    /*public async Task Thumbnail_Load(ThumbgenContext ctx)
    {
        Logger.LogStart(THUMB_LOAD, ctx.FileId);
        ctx.Source = await ImagePool.Load(ctx.Path);
        Logger.LogEnd  (THUMB_LOAD, ctx.FileId);
        await C_Resize.Writer.WriteAsync(ctx);
    }*/

    // todo - thumb load logic -> thumb resize
    
    public async Task Thumbnail_Resize(ThumbgenContext ctx)
    {
        Tracer.LogStart(THUMB_LOAD, ctx.FileId);
        ctx.Source = await ImagePool.Load(ctx.Path);
        Tracer.LogBoth (THUMB_LOAD, ctx.FileId, THUMB_SIZE);
        var size = ctx.Source.Size.FitSize(_fitSize);
        ctx.Thumb = ctx.Source.Clone(x => x.Resize(size, LanczosResampler.Lanczos3, compand: false));
        ImagePool.Return(ctx.Path);
        Tracer.LogEnd  (THUMB_SIZE, ctx.FileId);
        await C_SaveWebp.Writer.WriteAsync(ctx);
    }

    public async Task Thumbnail_Save(ThumbgenContext ctx)
    {
        Tracer.LogStart(THUMB_SAVE, ctx.FileId);
        var save = Dir_Thumbs
            .EnsureDirectoryExist()
            .Combine($"{ctx.FileId:x6}.webp");
        await ctx.Thumb.SaveAsWebpAsync(save, _encoder);
        Tracer.LogEnd  (THUMB_SAVE, ctx.FileId);
        Log($"Save thumb {ctx.FileId,5}");
        
        var result = new ThumbgenResult(ctx.FileId, DateTime.UtcNow, ctx.Source.Size);
        await C_DB_Write.Writer.WriteAsync(async connection =>
        {
            await connection.File_UpdateDateThumbGenerated(result.ToDB_File());
        });
    }

    public Channel<ThumbgenContext>
        //C_Resize   = Channel.CreateUnbounded<ThumbgenContext>(),
        C_SaveWebp = Channel.CreateUnbounded<ThumbgenContext>();
}

public struct ThumbgenContext
{
    public string Path;
    public int    FileId;
    public Image  Source;
    public Image  Thumb;
}