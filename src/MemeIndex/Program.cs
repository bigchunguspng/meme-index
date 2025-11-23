using MemeIndex.Core.Analysis.Color;
using MemeIndex.Utils;

Log("[Start]", color: ConsoleColor.Magenta);

AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    LogError($"UNHANDLED EXCEPTION! {e.ExceptionObject}");
    Environment.Exit(1);
};

// BRANCH

switch (args.Length)
{
    case > 0 when args[0] is "-?" or "--help":
        Print(Texts.HELP);
        return;
    case > 1 when args[0] is "-d" or "--demo":
        args.Skip(1).ForEachTry(ColorTagService_Demo.Run);
        return;
}

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

/*
var connection = await DB.ConnectTo_Main();
Log("var connection = await DB.OpenConnection();");
await connection.CreateDB_Main();
Log("await connection.CreateTables();");
//await connection.Test_Insert();
await connection.Dirs_Create(@"D:\Documents\Balls");
LogDebug("await connection.Dirs_Create");
var dirs = await connection.Dirs_GetAll();
LogDebug("await connection.Dirs_GetAll");
foreach (var dir in dirs)
{
    Print($"{dir.Id} -> {dir.Path}");
}
await connection.CloseAsync();
Log("await connection.Insert();");
*/

Log("[Configuration]", color: ConsoleColor.Magenta);

app.Run();