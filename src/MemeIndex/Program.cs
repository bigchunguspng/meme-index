using MemeIndex.API;
using MemeIndex.Core;
using MemeIndex.Core.Indexing;
using MemeIndex.DB;
using MemeIndex.Utils;

// HELP | VERSION
if (CLI.TryHandleArgs_HelpOrVersion(args)) return;

LogCM(ConsoleColor.Magenta, "START");

// HACKS

AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    LogError($"UNHANDLED EXCEPTION! {e.ExceptionObject}");
    Environment.Exit(1);
};

// BRANCH
if (CLI.TryHandleArgs_Other(args)) return;

LogCM(ConsoleColor.Magenta, "RUNNING NORMAL MODE (web server)");

// BUILDER

var port = await HostingHelpers.GetFreePort();

var wa_options = new WebApplicationOptions
{
    Args = args, WebRootPath = Dir_WebRoot,
};

var builder = WebApplication.CreateSlimBuilder(wa_options);

builder.WebHost
    .ConfigureKestrel(options =>
    {
        options.Listen(HostingHelpers.IP, port);
        if (port != HostingHelpers.DYNAMIC_PORT)
        {
            options.ListenLocalhost(port);
        }
    })
    ;

builder.Logging
    .ClearProviders()
    .AddProvider(new ConsoleLoggerProvider())
    ;

builder.Services
    .ConfigureHttpJsonOptions(options => options
        .SerializerOptions.TypeInfoResolverChain
        .Insert(0, AppJson.Default))
    .AddCors(options => options
        .AddDefaultPolicy(policy => policy
            .WithOrigins("*")
            .AllowAnyHeader()
            .AllowAnyMethod()))
    ;

// APP

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet(    "/logs",      Endpoints.GetPage_Logs);
app.MapGet(    "/logs/{id}", Endpoints.GetPage_EventViewer);
app.MapGet("/api/logs/{id}", Endpoints.GetJson_EventViewerData);

LogCM(ConsoleColor.Magenta, "SETUP");

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
    await Command_AddFilesToDB.Execute(path, recursive: false);
    Log($"await FileProcessor.AddFilesToDB(@\"{path}\", recursive: false);");
}