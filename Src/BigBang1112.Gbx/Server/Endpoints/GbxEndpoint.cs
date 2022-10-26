using GBX.NET;

namespace BigBang1112.Gbx.Server.Endpoints;

public class GbxEndpoint : IEndpoint
{
    public void Endpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/gbx", Gbx);
    }

    public IResult Gbx(IFormFile? file, string? fields, string? cacheKey)
    {
        if (file is null)
        {
            return Results.BadRequest();
        }

        using var stream = file.OpenReadStream();
        
        return Results.Ok(GameBox.ParseNode(stream));
    }
}
