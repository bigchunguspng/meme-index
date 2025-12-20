namespace MemeIndex.API;

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

        var status_color = status switch
        {
            >= 500 => ConsoleColor.DarkRed,
            >= 400 => ConsoleColor.Red,
            >= 300 => ConsoleColor.Yellow,
            >= 200 => ConsoleColor.Green,
            _      => ConsoleColor.Gray,
        };

        var method_color = req.Method switch
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
        var content_color =
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

        lock (typeof(ConsoleLogger))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"{now:MMM' 'dd', 'HH:mm:ss.fff} ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("T ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"{time.ReadableTime(),-10} | ");
            Console.ForegroundColor = status_color;
            Console.Write($"{status,3} ");
            Console.ResetColor();
            Console.Write($"{len_res_txt,9} | {len_req_txt,9} ");
            Console.ForegroundColor = method_color;
            Console.Write($"{method,4} ");
            Console.ForegroundColor = content_color;
            Console.WriteLine($"{req.Path}{req.QueryString}");
            Console.ResetColor();
        }
    }
}