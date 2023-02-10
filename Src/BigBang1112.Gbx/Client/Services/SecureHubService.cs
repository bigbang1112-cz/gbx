using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;

namespace BigBang1112.Gbx.Client.Services;

public interface ISecureHubService
{
    bool Connected { get; }

    void On(string method, Action action);
    Task PingAsync(CancellationToken cancellationToken = default);
    Task<bool> StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

public class SecureHubService : ISecureHubService
{
    private readonly HubConnection connection;

    private readonly IWebAssemblyHostEnvironment _host;
    private readonly ILogger<SecureHubService> _logger;

    public bool Connected { get; private set; }

    public SecureHubService(IWebAssemblyHostEnvironment host, ILogger<SecureHubService> logger)
    {
        _host = host;
        _logger = logger;

        connection = new HubConnectionBuilder()
            .WithUrl($"{_host.BaseAddress}securehub")
            .WithAutomaticReconnect()
            .Build();
    }

    public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await connection.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to hub");

            Connected = false;
            return false;
        }

        Connected = true;
        return true;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await connection.StopAsync(cancellationToken);
    }

    public async Task PingAsync(CancellationToken cancellationToken = default)
    {
        await connection.SendAsync("Ping", cancellationToken);
    }

    public void On(string method, Action action)
    {
        connection.On(method, action);
    }
}
