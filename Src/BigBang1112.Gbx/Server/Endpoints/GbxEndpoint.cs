using GBX.NET;
using GraphQLParser.AST;

namespace BigBang1112.Gbx.Server.Endpoints;

public class GbxEndpoint : IEndpoint
{
    private readonly ILogger<GbxEndpoint> _logger;

    public GbxEndpoint(ILogger<GbxEndpoint> logger)
    {
        _logger = logger;
    }

    public void Endpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("gbx", Gbx);
    }

    public async Task<IResult> Gbx(IFormFile? file, string query, string? @class, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return Results.BadRequest();
        }

        using var stream = file.OpenReadStream();

        var graphQl = default(GraphQLDocument);

        if (@class is not null)
        {
            graphQl = await ValidateAsync(query, @class, cancellationToken);
        }
        
        var asyncAction = new GameBoxAsyncReadAction()
        {
            AfterClassId = async (classId, token) =>
            {
                token.ThrowIfCancellationRequested();

                if (!NodeManager.TryGetName(classId, out var name))
                {
                    throw new Exception($"Server issue: unknown class 0x{classId:X8}");
                }
                
                if (graphQl is null)
                {
                    graphQl = await ValidateAsync(query, name, token);
                }
                else if (!string.Equals(name, @class))
                {
                    throw new Exception($"Bad request: expected {@class} != actual {name}");
                }
            },
        };
        
        var gbx = await GameBox.ParseAsync(stream, logger: _logger, asyncAction: asyncAction, cancellationToken: cancellationToken);

        if (graphQl is null)
        {
            throw new Exception("Null graphQl. This should not happen.");
        }

        await MapByGraphQlAsync(gbx, graphQl, cancellationToken);
        
        return Results.Ok(gbx);
    }

    private static async Task<GraphQLDocument> ValidateAsync(string query, string className, CancellationToken cancellationToken)
    {
        var graphQl = GraphQLParser.Parser.Parse(query);

        return await Task.FromResult(graphQl);
    }

    private Task MapByGraphQlAsync(GameBox gbx, GraphQLDocument graphQl, CancellationToken cancellationToken)
    {
        foreach (var node in graphQl.Definitions)
        {
            if (node is not GraphQLOperationDefinition operation)
            {
                continue;
            }

            foreach (var selection in operation.SelectionSet.Selections)
            {

            }
        }
        
        return Task.CompletedTask;
    }
}
