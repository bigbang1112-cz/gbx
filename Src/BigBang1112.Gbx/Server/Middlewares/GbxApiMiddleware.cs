using BigBang1112.Gbx.Server.Exceptions;

namespace BigBang1112.Gbx.Server.Middlewares;

public class GbxApiMiddleware
{
    private readonly RequestDelegate _next;

    public GbxApiMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (GbxApiClientException ex)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message, innerMessage = ex.InnerException?.Message });
        }
        catch (GbxApiServerException ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message, innerMessage = ex.InnerException?.Message });
        }
    }
}

