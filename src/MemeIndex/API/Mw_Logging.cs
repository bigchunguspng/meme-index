using System.Net;

namespace MemeIndex.API;

/// Log HTTP requests to console.
/// Can make things a little bit slower.
public class Mw_Logging : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            LogRequest(context, sw.Elapsed);
        }
    }

    private void LogRequest(HttpContext context, TimeSpan time)
    {
        var now = DateTime.UtcNow;

        var req = context.Request;
        var res = context.Response;

        var method = req.Method.Length > 4 ? req.Method.AsSpan(0, 3) : req.Method;
        var status = res.StatusCode;

        var len_req = req.ContentLength ?? 0;
        var len_res = res.ContentLength ?? 0;

        var len_req_txt = len_req > 0 ? len_req.FileSize_Format() : "--";
        var len_res_txt = len_res > 0 ? len_res.FileSize_Format() : "--";

        var cc_status = status switch
        {
            >= 500 => ConsoleColor.DarkRed,
            >= 400 => ConsoleColor.Red,
            >= 300 => ConsoleColor.Yellow,
            >= 200 => ConsoleColor.Green,
            _      => ConsoleColor.Gray,
        };

        var cc_method = req.Method switch
        {
             "GET"    => ConsoleColor.Blue,
             "POST"   => ConsoleColor.Green,
             "PUT"    => ConsoleColor.Yellow,
             "DELETE" => ConsoleColor.Red,
             _        => ConsoleColor.Gray,
        };

        var path = req.Path.Value;
        var ix_dot = path?.LastIndexOf(".", StringComparison.Ordinal) ?? 0;
        var extension = ix_dot > 0 ? path!.Substring(ix_dot) : null;
        var cc_content =
            path != null && path.StartsWith("/api/")
                ? ConsoleColor.DarkMagenta
                : ix_dot < 0
                    ? ConsoleColor.DarkYellow
                    : extension switch
                    {
                        ".html"  => ConsoleColor.DarkYellow,
                        ".css"   => ConsoleColor.Yellow,
                        ".js"    => ConsoleColor.Yellow,
                        _        => ConsoleColor.Gray,
                    };
        var text
            = $"\e[{CC_DARK_GRAY       }m{now:MMM' 'dd', 'HH:mm:ss.fff} "
            + $"\e[{CC_YELLOW          }mT "
            + $"\e[{CC_DARK_GRAY       }m{time.ReadableTime(),10} @ "
            + $"\e[{cc_status .ToANSI()}m{status,3} "
            + $"\e[0m" +               $"{len_res_txt,9} <- {len_req_txt,9} "
            + $"\e[{cc_method .ToANSI()}m{method,4} "
            + $"\e[{cc_content.ToANSI()}m{req.Path}{WebUtility.UrlDecode(req.QueryString.Value)}\e[0m";
        Console.WriteLine(text);
    }
}