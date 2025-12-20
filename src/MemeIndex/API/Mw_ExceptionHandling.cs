using MemeIndex.Core;

namespace MemeIndex.API;

public class Mw_ExceptionHandling : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception e)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            var req = context.Request;
            var con = context.Connection;
            var len = req.ContentLength;
            var body = len is > 0 ? $" {{…}}^{len}" : null;
            var ctx = $"{req.Method} {req.Path}{req.QueryString}{body}\n   "
                    + $"{con.RemoteIpAddress}:{con.RemotePort} -> {con.LocalIpAddress}:{con.LocalPort}";
            App.LogException(e, ExceptionCategory.API, ctx);
        }
    }
}