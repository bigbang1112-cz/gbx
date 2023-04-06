using BigBang1112.Gbx.Shared;
using Champagne;
using ClipCheckpoint;
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
using System.Reflection;

namespace BigBang1112.Gbx.Client.Services;

internal interface IToolManager
{
    IReadOnlyCollection<Type> Tools { get; }
    IReadOnlyDictionary<string, Type> ToolsByRoute { get; }
    IEnumerable<IToolFactory> GetToolFactories(string searchFilter = "");
    IToolFactory? GetToolFactoryByRoute(string route);
}

internal class ToolManager : IToolManager
{
    private static readonly List<Type> stronglyTypedTools = new();
    private static readonly Dictionary<string, Type> toolsByRoute = new();

    private readonly IServiceProvider provider;

    public IReadOnlyCollection<Type> Tools => stronglyTypedTools;

    public IReadOnlyDictionary<string, Type> ToolsByRoute => toolsByRoute;

    public ToolManager(IServiceProvider provider)
    {
        this.provider = provider;

        foreach (var toolType in stronglyTypedTools)
        {
            typeof(AssetsManager<>).MakeGenericType(toolType)
                .GetProperty("ExternalRetrieve", BindingFlags.Static | BindingFlags.NonPublic)?
                .SetValue(null, new Func<string, Task<byte[]>>(path =>
                {
                    return provider.GetRequiredService<HttpClient>().GetByteArrayAsync($"assets/tools/{path}");
                }));
        }
    }

    internal static void Services(IServiceCollection services)
    {
        AddTool<ClipInputTool>(services);
        AddTool<ClipCheckpointTool>(services);
        AddTool<GhostToClipTool>(services);
        AddTool<ClipToReplayTool>(services);
        AddTool<ReplayViewerTool>(services);
        AddTool<MapViewerEngineTool>(services);
        AddTool<SpikeTool>(services);
        AddTool<CombineClipsTool>(services);
        AddTool<ChampagneTool>(services);
    }

    internal static void AddTool<T>(IServiceCollection services) where T : class, ITool
    {
        services.AddScoped<ToolFactory<T>>();

        var toolType = typeof(T);

        foreach (var type in toolType.Assembly.DefinedTypes)
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

        var id = toolType.Assembly.GetName().Name ?? throw new Exception("Tool requires an Id");
        var route = toolType.GetCustomAttribute<ToolRouteAttribute>()?.Route ?? RegexUtils.PascalCaseToKebabCase(id);

        stronglyTypedTools.Add(toolType);
        toolsByRoute.Add(route, toolType);
    }

    public IToolFactory? GetToolFactoryByRoute(string route)
    {
        return toolsByRoute.TryGetValue(route, out var toolType)
            ? (IToolFactory)provider.GetRequiredService(typeof(ToolFactory<>).MakeGenericType(toolType))
            : null;
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