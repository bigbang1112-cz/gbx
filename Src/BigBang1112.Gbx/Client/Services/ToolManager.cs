using BigBang1112.Gbx.Shared;
using ClipCheckpoint;
using ClipInput;
using ClipInput.Client;
using ClipToReplay;
using CombineClips;
using EnvimixForTmuf;
using GbxToolAPI;
using GbxToolAPI.Client;
using GhostToClip;
using MapViewerEngine;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ReplayViewer;
using Spike;
using Spike.Client;
using System.Diagnostics.CodeAnalysis;
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
        AddTool<ClipInputTool, ClipInputToolComponent>(services);
        AddTool<ClipCheckpointTool>(services);
        AddTool<ReplayViewerTool, ReplayViewerToolComponent>(services, isProceedComponent: true);
        AddTool<MapViewerEngineTool, MapViewerEngineToolComponent>(services, isProceedComponent: true);
        AddTool<GhostToClipTool>(services);
        AddTool<ClipToReplayTool>(services);
        AddTool<EnvimixForTmufTool>(services);
        AddTool<SpikeTool, SpikeToolComponent>(services, isProceedComponent: true);
        AddTool<CombineClipsTool>(services);
        //AddTool<ChampagneTool>(services);
    }

    internal static void AddTool<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TTool>(IServiceCollection services, Type? toolComponent = null, Type? toolProceedComponent = null) where TTool : class, ITool
    {
        services.AddScoped<ToolFactory<TTool>>(provider => new(provider.GetRequiredService<ILogger<ToolFactory<TTool>>>(), toolComponent, toolProceedComponent));

        var toolType = typeof(TTool);

        foreach (var type in toolType.Assembly.DefinedTypes)
        {
            if (type.IsSubclassOf(typeof(ToolHubConnection)))
            {
                services.AddScoped(type, provider =>
                {
                    var hubAddress = provider.GetRequiredService<IWebAssemblyHostEnvironment>().BaseAddress;
                    var logger = provider.GetRequiredService<ILogger<TTool>>();
                    
                    return Activator.CreateInstance(type, hubAddress, logger)!;
                });
            }
        }

        var id = toolType.Assembly.GetName().Name ?? throw new Exception("Tool requires an Id");
        var route = toolType.GetCustomAttribute<ToolRouteAttribute>()?.Route ?? RegexUtils.PascalCaseToKebabCase(id);

        stronglyTypedTools.Add(toolType);
        toolsByRoute.Add(route, toolType);
        
    }

    internal static void AddTool<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TTool, TToolComponent>(IServiceCollection services, bool isProceedComponent = false) where TTool : class, ITool where TToolComponent : ToolComponentBase
    {
        AddTool<TTool>(services,
            toolComponent: isProceedComponent ? null : typeof(TToolComponent),
            toolProceedComponent: isProceedComponent ? typeof(TToolComponent) : null);
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