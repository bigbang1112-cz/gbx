using BigBang1112.Gbx.Server.Exceptions;
using GraphQLParser.Exceptions;

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
        catch (GraphQLSyntaxErrorException ex)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync(ex.Message);
        }
        catch (GbxApiClientException ex)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync(ex.Message);
        }
        catch (GbxApiServerException ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync(ex.Message);
        }
    }
}

