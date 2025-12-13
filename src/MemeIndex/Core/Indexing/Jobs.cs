using System.Threading.Channels;
using MemeIndex.Core.Thumbgen;

namespace MemeIndex.Core.Indexing;

public abstract class ChannelJob<T>
(
    string code,
    Channel<T> channel,
    Func<T, Task> process_item,
    Func<T, string>? log_item = null
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Log(code, "STARTED");
        await foreach (var item in channel.Reader.ReadAllAsync(ct))
        {
            await process_item(item);

            if (log_item != null)
                Log(code, log_item(item));
        }
        Log(code, "COMPLETED");
    }
}

public abstract class ChannelJob_X
(
    string code,
    Channel<int> channel,
    Func<Task> execute,
    string? log_string = null
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Log(code, "STARTED");
        await foreach (var _ in channel.Reader.ReadAllAsync(ct))
        {
            await execute();

            if (log_string != null)
                Log(code, log_string);
        }
        Log(code, "COMPLETED");
    }
}

//

public class Job_Thumbgen()
    : ChannelJob_X
    (
        "Job/Thumbgen",
        FileProcessor.C_Thumbgen,
        ThumbGenerator.GenerateThumbnails,
        "Task done!"
    );

public class Job_Analysis()
    : ChannelJob_X
    (
        "Job/Analysis",
        FileProcessor.C_Analysis,
        FileProcessor.AnalyzeFiles,
        "Task done!"
    );

//

public class Job_ThumbgenResize()
    : ChannelJob<ThumbgenContext>
    (
        "Job/Thumbgen-Resize",
        ThumbGenerator.C_Resize,
        ThumbGenerator.Thumbnail_Resize
    );

public class Job_ThumbgenSaveWebp()
    : ChannelJob<ThumbgenContext>
    (
        "Job/Thumbgen-Save-Webp",
        ThumbGenerator.C_SaveWebp,
        ThumbGenerator.Thumbnail_Save
    );

public class Job_ThumbgenSave()
    : ChannelJob<ThumbgenResult>
    (
        "Job/Thumbgen-Save-DB",
        FileProcessor.C_ThumbgenSave,
        FileProcessor.UpdateFileThumbDateInDB,
        result => $"Update file {result.file_id,5}!"
    );

public class Job_AnalysisSave()
    : ChannelJob<AnalysisResult>
    (
        "Job/Analysis-Save-DB",
        FileProcessor.C_AnalysisSave,
        FileProcessor.AddTagsToDB,
        result => $"Update file {result.file_id,5}!"
    );