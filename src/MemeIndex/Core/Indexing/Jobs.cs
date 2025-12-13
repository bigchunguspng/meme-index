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
            // todo try batch results
            await FileProcessor.AddTagsToDB(result);
            Log(CODE, $"Updated file {result.file_id,5}!");
        }
        Log(CODE, "COMPLETED");
    }
}