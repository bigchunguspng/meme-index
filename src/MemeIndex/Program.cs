using MemeIndex.Core;
using MemeIndex.Core.Indexing;
using MemeIndex.DB;
using MemeIndex.Utils;

LogCM(ConsoleColor.Magenta, "[Start]");

AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    LogError($"UNHANDLED EXCEPTION! {e.ExceptionObject}");
    Environment.Exit(1);
};

// BRANCH

if (CLI.TryHandleArgs(args)) return;

// BUILDER

var wa_options = new WebApplicationOptions
{
    Args = args,
    ApplicationName = "Meme-Index-Web",
    WebRootPath = Dir_WebRoot,
};

var builder = WebApplication.CreateSlimBuilder(wa_options);

builder.Logging
    .ClearProviders()
    .AddProvider(new ConsoleLoggerProvider())
    ;

builder.Services
    .ConfigureHttpJsonOptions(options => options
        .SerializerOptions.TypeInfoResolverChain
        .Insert(0, AppJsonSerializerContext.Default))
    .AddHostedService<Job_FileProcessing>()
    ;

// APP

var app = builder.Build();

LogCM(ConsoleColor.Magenta, "[Configuration]");

app.UseDefaultFiles();
app.UseStaticFiles();

_ = TestCode();

app.Run();

async Task TestCode()
{
    await Task.Delay(3000);
    Log("_");
    await using (var connection = await AppDB.ConnectTo_Main())
    {
        Log("var connection = await DB.OpenConnection();");
        await connection.CreateDB_Main();
        Log("await connection.CreateTables();");
        await connection.CloseAsync();
        Log("await connection.CloseAsync();");
    }

    var path = CLI.GetArgsFromFile(args[0]).First();
    await FileProcessor.AddFilesToDB(path, recursive: false);
    Log($"await FileProcessor.AddFilesToDB(@\"{path}\", recursive: false);");
}