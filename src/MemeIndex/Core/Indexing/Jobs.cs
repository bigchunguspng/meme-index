using MemeIndex.Core.Thumbgen;

namespace MemeIndex.Core.Indexing;

public class Job_Analysis : BackgroundService
{
    private const string CODE = "Job/Analysis";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Log(CODE, "STARTED");
        await foreach (var _ in FileProcessor.C_Analysis.Reader.ReadAllAsync(ct))
        {
            await FileProcessor.AnalyzeFiles();
            Log(CODE, "Task done!");
        }
        Log(CODE, "COMPLETED");
    }
}

public class Job_AnalysisSave : BackgroundService
{
    private const string CODE = "Job/Analysis-Save";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Log(CODE, "STARTED");
        await foreach (var result in FileProcessor.C_AnalysisSave.Reader.ReadAllAsync(ct))
        {
            await FileProcessor.AddTagsToDB(result);
            Log(CODE, $"Updated file {result.file_id,5}!");
        }
        Log(CODE, "COMPLETED");
    }
}

public class Job_Thumbgen : BackgroundService
{
    private const string CODE = "Job/Thumbgen";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Log(CODE, "STARTED");
        await foreach (var _ in FileProcessor.C_Thumbgen.Reader.ReadAllAsync(ct))
        {
            await ThumbGenerator.GenerateThumbnails();
            Log(CODE, "Task done!");
        }
        Log(CODE, "COMPLETED");
    }
}

public class Job_ThumbgenSave : BackgroundService
{
    private const string CODE = "Job/Thumbgen-Save";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Log(CODE, "STARTED");
        await foreach (var result in FileProcessor.C_ThumbgenSave.Reader.ReadAllAsync(ct))
        {
            await FileProcessor.UpdateFileThumbDateInDB(result);
            Log(CODE, $"Updated file {result.file_id,5}!");
        }
        Log(CODE, "COMPLETED");
    }
}