using System.Threading.Channels;
using MemeIndex.DB;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace MemeIndex.Core.Indexing;

public partial class FileProcessor
{
    private async Task StartThumbnailGeneration()
    {
        const string CODE = "Tmb/Gen";
        Log(CODE, "START");

        // GET FILES
        await using var con = await AppDB.ConnectTo_Main();
        var db_files = await con.Files_GetToBeThumbed();
        await con.CloseAsync();
        Log(CODE, "GET FILES");

        var files = db_files.Select(x => x.Compile()).ToArray();
        if (files.Length == 0)
        {
            Log(CODE, "NOTHING TO PROCESS");
            return;
        }

        ImagePool.Book(files.Select(x => x.Path), files.Length);

        if (InitJob_DB_Write() is { } job)
            await job.StartAsync(CancellationToken.None);

        // PROCESS

        job_thumbsWebp = new Job_ThumbgenSaveWebp(this);
        await job_thumbsWebp.StartAsync(CancellationToken.None);

        foreach (var file in files)
        {
            try
            {
                await Thumbnail_Resize(new ThumbgenContext(file));
            }
            catch (Exception e)
            {
                LogError(e);
                // todo add file id to broken files
            }
        }

        C_TG_SaveWebp.Writer.Complete();

        Log(CODE, "DONE");
    }

    // LOAD + RESIZE

    private static readonly Size _fitSize = new (174, 174);

    private async Task Thumbnail_Resize(ThumbgenContext c)
    {
        var id = c.FileId;
        Tracer.LogOpen(id, THUMB_LOAD);
        c.Source = await ImagePool.Load(c.Path);
        Tracer.LogJoin(id, THUMB_LOAD, THUMB_SIZE);
        var size = c.Source.Size.FitSize(_fitSize);
        c.Thumb = c.Source.Clone(x => x.Resize(size, LanczosResampler.Lanczos3, compand: false));
        ImagePool.Return(c.Path);
        Tracer.LogDone(id, THUMB_SIZE);
        await C_TG_SaveWebp.Writer.WriteAsync(c);
    }

    // SAVE FILE

    private readonly    Channel<ThumbgenContext>
        C_TG_SaveWebp = Channel.CreateUnbounded<ThumbgenContext>();

    private Job_ThumbgenSaveWebp? job_thumbsWebp;

    public class Job_ThumbgenSaveWebp(FileProcessor task)
        : ChannelJob<ThumbgenContext>
        (
            "Job/Thumbgen-Save-Webp",
            task.C_TG_SaveWebp,
            task.Thumbnail_Save
        );

    private static readonly WebpEncoder _encoder = new()
    {
        FileFormat = WebpFileFormatType.Lossy,
        Quality = 85,
        Method = WebpEncodingMethod.Level2,
        TransparentColorMode = WebpTransparentColorMode.Clear,
    };

    private async Task Thumbnail_Save(ThumbgenContext c)
    {
        var id = c.FileId;
        Tracer.LogOpen(id, THUMB_SAVE);
        var save = Dir_Thumbs
            .EnsureDirectoryExist()
            .Combine(GetThumbFilename(id));
        await c.Thumb.SaveAsWebpAsync(save, _encoder);
        Tracer.LogDone(id, THUMB_SAVE);
        LogDebug($"File {id,6} -> thumbnail generated");

        var result = c.ToDB_File();
        await C_DB_Write.Writer.WriteAsync(async connection =>
        {
            Tracer.LogOpen(id, DB_W_FT);
            await connection.File_UpdateDateThumbGenerated(result);
            Tracer.LogDone(id, DB_W_FT);
        });
    }

    [MethodImpl(AggressiveInlining)]
    public static string GetThumbFilename(int id) => $"{id:x6}.webp";
}

public struct ThumbgenContext(FilePathRecord file)
{
    public readonly string Path   = file.Path;
    public readonly int    FileId = file.Id;
    public Image  Source;
    public Image  Thumb;

    public DB_File_UpdateDateSize ToDB_File
        () => new(FileId, DateTime.UtcNow, Source.Size);
}