﻿using BigBang1112.Gbx.Server.Exceptions;
using GBX.NET;
using GraphQLParser.AST;
using GraphQLParser.Exceptions;
using System.Diagnostics;

namespace BigBang1112.Gbx.Server.Endpoints.API.V1;

public class GbxEndpoint : IEndpoint
{
    private readonly ILogger<GbxEndpoint> _logger;

    public GbxEndpoint(ILogger<GbxEndpoint> logger)
    {
        _logger = logger;
    }

    public void Endpoint(IEndpointRouteBuilder app)
    {
        /*app.MapPost("gbx", Gbx)
            .RequireAuthorization();*/
    }

    public async Task<IResult> Gbx(IFormFile? file, string query, string? @class, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return Results.BadRequest(new { message = "File is null" });
        }

        cancellationToken.ThrowIfCancellationRequested();

        using var stream = file.OpenReadStream();

        var graphQl = default(GraphQLDocument);

        if (@class is not null)
        {
            graphQl = Validate(query, @class);
        }

        var asyncAction = new GameBoxAsyncReadAction()
        {
            AfterClassId = (classId, token) =>
            {
                token.ThrowIfCancellationRequested();

                if (!NodeManager.TryGetName(classId, out var name))
                {
                    throw new GbxApiServerException($"Server issue: unknown class 0x{classId:X8}");
                }

                name = name.Substring(name.IndexOf(':') + 2); // Will be better to fix it someday

                if (graphQl is null)
                {
                    graphQl = Validate(query, name);
                }
                else if (!string.Equals(name, @class))
                {
                    throw new GbxApiClientException($"Bad request: expected {@class} != actual {name}");
                }

                return Task.CompletedTask;
            },
        };
        
        var gbx = await GameBox.ParseAsync(stream, logger: _logger, asyncAction: asyncAction, cancellationToken: cancellationToken);

        if (graphQl is null)
        {
            throw new UnreachableException("Null graphQl. This should not happen.");
        }

        await MapByGraphQlAsync(gbx, graphQl, cancellationToken);

        return Results.Ok();
    }

    private static GraphQLDocument Validate(string query, string className)
    {
        try
        {
            var graphQl = GraphQLParser.Parser.Parse(query);

            Models.Gbx.Gbx.Validate(graphQl.Definitions, className);

            return graphQl;
        }
        catch (GraphQLSyntaxErrorException ex)
        {
            throw new GbxApiClientException("Invalid GraphQL syntax.", ex);
        }
        catch (GraphQLMaxDepthExceededException ex)
        {
            throw new GbxApiClientException("Maximum GraphQL depth exceeded.", ex);
        }
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
