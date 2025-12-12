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
};

var builder = WebApplication.CreateSlimBuilder(wa_options);

builder.Logging
    .ClearProviders()
    .AddProvider(new ConsoleLoggerProvider());

builder.Services
    .ConfigureHttpJsonOptions(options => options
        .SerializerOptions.TypeInfoResolverChain
        .Insert(0, AppJsonSerializerContext.Default));

// APP

var app = builder.Build();

Log("_");
var connection = await AppDB.ConnectTo_Main();
Log("var connection = await DB.OpenConnection();");
await connection.CreateDB_Main();
Log("await connection.CreateTables();");
await connection.CloseAsync();
Log("await connection.CloseAsync();");

var path = CLI.GetArgsFromFile(args[0]).First();
await FileProcessor.AddFilesToDB(path, recursive: true);
Log($"await FileProcessor.AddFilesToDB(@\"{path}\", recursive: true);");
return;
//await connection.Test_Insert();
/*await connection.Dirs_Create(@"D:\Documents\Balls");
LogDebug("await connection.Dirs_Create");
var dirs = await connection.Dirs_GetAll();
LogDebug("await connection.Dirs_GetAll");
foreach (var dir in dirs)
{
    Print($"{dir.Id} -> {dir.Path}");
}
Log("await connection.Insert();");*/

LogCM(ConsoleColor.Magenta, "[Configuration]");

app.Run();