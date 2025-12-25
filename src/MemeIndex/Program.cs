using MemeIndex.API;
using MemeIndex.Core;
using MemeIndex.Core.Indexing;
using MemeIndex.DB;
using MemeIndex.Utils;

// Log("ARGS: " + string.Join(", ", args), color: ConsoleColor.Yellow);

// HELP | VERSION
if (CLI.TryHandleArgs_Info(args)) return;

LogCM(ConsoleColor.Magenta, "START");

// HACKS

Console.CancelKeyPress        += (_, _) => App.SaveAndExit();
App.Domain.ProcessExit        += (_, _) => App.SaveAndExit();
App.Domain.UnhandledException += (_, e) =>
{
    LogError($"UNHANDLED EXCEPTION! {e.ExceptionObject}");
    App.LogException((Exception)e.ExceptionObject, ExceptionCategory.CRASH);
    Environment.Exit(1);
};

// BRANCH
if (CLI.TryHandleArgs_Action(args)) return;

LogCM(ConsoleColor.Magenta, "RUNNING NORMAL MODE (web server)");

// BUILDER

var flag_url = args.Any(x => x.StartsWith("--urls"));
var flag_log = args.Any(x => x is "-l" or "--log");
var opt_web  = args.FindIndex(x => x is "-w" or "--web") + 1 is var i
            && i > 0
            && i < args.Length
    ? args[i]
    : Dir_WebRoot.Value;

Log($"Web root path: {opt_web}");

var wa_options = new WebApplicationOptions
{
    Args = args, WebRootPath = opt_web,
};

var builder = WebApplication.CreateSlimBuilder(wa_options);

if (flag_url.Janai())
{
    var port = await HostingHelpers.GetFreePort();

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
}

builder.Logging
    .ClearProviders()
    .AddProvider(new ConsoleLoggerProvider())
    ;

builder.Services
    .AddSingleton<Mw_Logging>()
    .AddSingleton<Mw_ExceptionHandling>()
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

if (flag_log) app.UseMiddleware<Mw_Logging>();
app.UseMiddleware<Mw_ExceptionHandling>();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/find",      Endpoints.GetJson_Find);
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