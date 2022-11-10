using GBX.NET;

namespace BigBang1112.Gbx.Server.Endpoints;

public class GbxEndpoint : IEndpoint
{
    public void Endpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("gbx", Gbx);
        app.MapGet("gbx/fields/{className}", GbxFields);
    }

    public IResult Gbx(IFormFile? file, string fields, string? cacheKey)
    {
        if (file is null)
        {
            return Results.BadRequest();
        }

        using var stream = file.OpenReadStream();
        
        return Results.Ok(GameBox.ParseNode(stream));
    }

    public IResult GbxFields(string className)
    {
        return Results.Ok();
    }
}
