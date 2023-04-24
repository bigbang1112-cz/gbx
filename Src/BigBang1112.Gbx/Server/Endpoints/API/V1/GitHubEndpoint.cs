using Octokit;

namespace BigBang1112.Gbx.Server.Endpoints.API.V1;

public class GitHubEndpoint : IEndpoint
{
    private readonly ILogger<GitHubEndpoint> logger;

    public GitHubEndpoint(ILogger<GitHubEndpoint> logger)
    {
        this.logger = logger;
    }

    public void Endpoint(IEndpointRouteBuilder app)
    {
        
    }
}
