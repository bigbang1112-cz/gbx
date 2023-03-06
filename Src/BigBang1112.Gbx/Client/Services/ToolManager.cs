using ClipInput;
using ClipToReplay;
using CombineClips;
using GbxToolAPI;
using GbxToolAPI.Client;
using GhostToClip;
using MapViewerEngine;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ReplayViewer;
using Spike;

namespace BigBang1112.Gbx.Client.Services;

internal interface IToolManager
{
    IReadOnlyCollection<Type> Tools { get; }
    IEnumerable<IToolFactory> GetToolFactories(string searchFilter = "");
}

internal class ToolManager : IToolManager
{
    private static readonly List<Type> stronglyTypedTools = new();

    private readonly IServiceProvider provider;

    public IReadOnlyCollection<Type> Tools => stronglyTypedTools;

    public ToolManager(IServiceProvider provider)
    {
        this.provider = provider;
    }

    internal static void Services(IServiceCollection services)
    {
        AddTool<ClipInputTool>(services);
        // AddTool<ClipCheckpointTool>(services);
        AddTool<GhostToClipTool>(services);
        AddTool<ClipToReplayTool>(services);
        AddTool<ReplayViewerTool>(services);
        AddTool<MapViewerEngineTool>(services);
        AddTool<SpikeTool>(services);
        AddTool<CombineClipsTool>(services);
    }

    internal static void AddTool<T>(IServiceCollection services) where T : ITool
    {
        services.AddScoped<ToolFactory<T>>();

        foreach (var type in typeof(T).Assembly.DefinedTypes)
        {
            if (type.IsSubclassOf(typeof(ToolHubConnection)))
            {
                services.AddScoped(type, provider =>
                {
                    var hubAddress = provider.GetRequiredService<IWebAssemblyHostEnvironment>().BaseAddress;
                    var logger = provider.GetRequiredService<ILogger<T>>();
                    
                    return Activator.CreateInstance(type, hubAddress, logger)!;
                });
            }
        }

        stronglyTypedTools.Add(typeof(T));
    }

    public IEnumerable<IToolFactory> GetToolFactories(string searchFilter = "")
    {
        foreach (var tool in Tools)
        {
            var factory = (IToolFactory)provider.GetRequiredService(typeof(ToolFactory<>).MakeGenericType(tool));
            
            if (FilterFactory(factory, searchFilter))
            {
                yield return factory;
            }
        }
    }

    private static bool FilterFactory(IToolFactory factory, string searchFilter)
    {
        if (factory.Name.Contains(searchFilter, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (factory.Description.Contains(searchFilter, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}