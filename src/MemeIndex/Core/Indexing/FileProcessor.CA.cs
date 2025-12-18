using MemeIndex.Core.Analysis.Color.v2;
using MemeIndex.DB;

namespace MemeIndex.Core.Indexing;

public partial class FileProcessor
{
    private async Task StartColorAnalysis()
    {
        const string CODE = "Clr/Anl";
        Log(CODE, "START");

        // GET FILES
        await using var con = await AppDB.ConnectTo_Main();
        var db_files = await con.Files_GetToBeAnalyzed();
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

        foreach (var file in files)
        {
            try
            {
                await AnalyzeImage_Color(file.Id, file.Path);
            }
            catch (Exception e)
            {
                LogError(e);
                // todo add file id to broken files
            }
        }
        Log(CODE, "DONE");
    }

    private async Task AnalyzeImage_Color
        (int id, string path, int minScore = 10)
    {
        Tracer.LogOpen(id, CA_LOAD);
        var image = await ImagePool.Load(path);
        Tracer.LogJoin(id, CA_LOAD, CA_SCAN);
        var report = ColorAnalyzer_v2.ScanImage(image);
        ImagePool.Return(path);
        Tracer.LogJoin(id, CA_SCAN, CA_ANAL);
        var tags = ColorTagger_v2.AnalyzeImageScan(report, minScore);
        Tracer.LogDone(id, CA_ANAL);
        LogDebug($"File {id,6} -> color analysis done");

        var db_file = new DB_File_UpdateDate(id, DateTime.UtcNow);
        var db_tags = tags
            .Select(x => new TagContent(x.Key, x.Value))
            .Select(x => new DB_Tag_Insert(x, id));

        await C_DB_Write.Writer.WriteAsync(async connection =>
        {
            Tracer.LogOpen(id, DB_W_TAGS);
            await connection.Tags_CreateMany        (db_tags);
            Tracer.LogJoin(id, DB_W_TAGS, DB_W_FA);
            await connection.File_UpdateDateAnalyzed(db_file);
            Tracer.LogDone(id, DB_W_FA);
        });
    }
}