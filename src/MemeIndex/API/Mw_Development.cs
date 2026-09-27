namespace MemeIndex.API;

/// Don't cache CSS and JS files.
public class Mw_Development : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path.Value;
        if (path != null
            && (path.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
            ||  path.EndsWith(".js",  StringComparison.OrdinalIgnoreCase)))
        {
            context.Response.Headers.CacheControl = "no-store";
        }

        await next(context);
    }
}