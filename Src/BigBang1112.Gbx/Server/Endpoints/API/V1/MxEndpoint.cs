using System.Net;

namespace BigBang1112.Gbx.Server.Endpoints.API.V1;

public class MxEndpoint : IEndpoint
{
    private readonly ILogger<MxEndpoint> _logger;
    private readonly IHttpClientFactory _httpFactory;

    public MxEndpoint(ILogger<MxEndpoint> logger, IHttpClientFactory httpFactory)
    {
        _logger = logger;
        _httpFactory = httpFactory;
    }
    
    public void Endpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("mx");
        group.MapGet("track/{site}/{mapId}", Track);//.RequireAuthorization();
        group.MapGet("replay/{site}/{replayId}", Replay);//.RequireAuthorization();
    }

    private async Task<IResult> Track(string site, int mapId)
    {
        return await GetGbxAsync(_httpFactory.CreateClient(Constants.MxTrack), site, $"trackgbx/{mapId}");
    }

    private async Task<IResult> Replay(string site, int replayId)
    {
        return await GetGbxAsync(_httpFactory.CreateClient(Constants.MxReplay), site, $"recordgbx/{replayId}");
    }

    private async Task<IResult> GetGbxAsync(HttpClient http, string site, string path)
    {
        var url = site switch
        {
            "tmuf" => $"https://tmuf.exchange/{path}",
            "tmnf" => $"https://tmnf.exchange/{path}",
            "nations" => $"https://nations.tm-exchange.com/{path}",
            "sunrise" => $"https://sunrise.tm-exchange.com/{path}",
            "original" => $"https://original.tm-exchange.com/{path}",
            _ => null
        };

        if (url is null)
        {
            return Results.BadRequest(new { message = $"Unknown site '{site}'" });
        }

        _logger.LogInformation("Requesting {url}", url);

        using var response = await http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => Results.NotFound(),
                HttpStatusCode.Forbidden => Results.Forbid(),
                _ => Results.Problem()
            };
        }

        _logger.LogInformation("Response {statusCode}", response.StatusCode);

        return Results.File(await response.Content.ReadAsByteArrayAsync(), "application/octet-stream");
    }
}
