using MemeIndex.DB;
using MemeIndex.Utils;

AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    LogError($"UNHANDLED EXCEPTION! {e.ExceptionObject}");
    Environment.Exit(1);
};

var builder = WebApplication.CreateSlimBuilder(args);

builder.Logging
    .ClearProviders()
    .AddProvider(new ConsoleLoggerProvider());

builder.Services
    .ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    });

var app = builder.Build();

var sw = Stopwatch.StartNew();
var connection = await DB.ConnectTo_Main();
sw.Log("ConnectTo_Main");
LogDebug("Debug message");
LogError("Error message");
LogWarn("Warning!");
Log("var connection = await DB.OpenConnection();");
await connection.CreateDB_Main();
Log("await connection.CreateTables();");
//await connection.Test_Insert();
await connection.CloseAsync();
Log("await connection.Insert();");

app.Run();