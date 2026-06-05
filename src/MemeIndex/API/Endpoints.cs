using System.Text;

namespace MemeIndex.API;

public static partial class Endpoints
{
    public static IResult GetPage_Logs()
    {
        var files = Dir_Traces.GetFiles("*.json");
        var sb = new StringBuilder();
        sb.Append("""
                  <!DOCTYPE html>
                  <html>
                  <head>
                  <meta charset="utf-8">
                  <title>Logs</title>
                  <style>
                  body { font-family: sans-serif; padding: 20px; }
                  ul { list-style: none; padding: 0; }
                  li { margin: 6px 0; }
                  a { text-decoration: none; color: #0066cc; }
                  a:hover { text-decoration: underline; }
                  </style>
                  </head>
                  <body>
                  <h2>Log Files</h2>
                  <ul>
                  """);

        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            sb.Append($"""<li><a href="/logs/{name}">{name}</a></li>""");
        }

        sb.Append("""
                  </ul>
                  </body>
                  </html>
                  """);

        return Results.Content(sb.ToString(), "text/html");
    }

    public static IResult GetPage_EventViewer(string id)
    {
        var sb = new StringBuilder();
        var inserted = false;
        using var reader = new StreamReader(Dir_WebRoot.Combine("logs-file.html"));
        while (reader.ReadLine() is { } line)
        {
            if (inserted.Janai() && line.StartsWith("    // INSERT"))
            {
                sb.AppendLine($"    const LOG_FILE_URL = \"/api/logs/{id}\";");
                inserted = true;
            }
            else
                sb.AppendLine(line);
        }

        return Results.Content(sb.ToString(), "text/html");
    }

    public static IResult GetJson_EventViewerData(string id)
    {
        var file = Dir_Traces.GetFiles($"{id}.json").First();
        return Results.Content(File.ReadAllText(file), "application/json");
    }
}