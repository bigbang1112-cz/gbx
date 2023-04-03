using BigBang1112.Gbx.Shared;
using GbxToolAPI;
using System.Reflection;

namespace BigBang1112.Gbx.Client.Services;

public interface IToolFactory
{
    Type ToolType { get; }

    string Id { get; }
    string Name { get; }
    string Description { get; }
    string GitHubRepository { get; }
    string Route { get; }

    ITool CreateTool(params object?[] args);
}

public class ToolFactory<T> : IToolFactory where T : ITool
{
    private readonly ILogger<ToolFactory<T>> logger;

    public Type ToolType { get; }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string GitHubRepository { get; }
    public string Route { get; }

    public ToolFactory(ILogger<ToolFactory<T>> logger)
    {
        this.logger = logger;

        ToolType = typeof(T);

        Id = ToolType.Assembly.GetName().Name ?? throw new Exception("Tool requires an Id");
        Name = ToolType.GetCustomAttribute<ToolNameAttribute>()?.Name ?? ToolType.Name;
        Description = ToolType.GetCustomAttribute<ToolDescriptionAttribute>()?.Description ?? string.Empty;
        GitHubRepository = ToolType.GetCustomAttribute<ToolGitHubAttribute>()?.Repository ?? string.Empty;
        Route = ToolType.GetCustomAttribute<ToolRouteAttribute>()?.Route ?? RegexUtils.PascalCaseToKebabCase(Id);
    }

    ITool IToolFactory.CreateTool(params object?[] args)
    {
        return CreateTool(args);
    }

    public T CreateTool(params object?[] args)
    {
        logger.LogInformation("Creating tool {ToolType} with args {Args}", typeof(T), args);

        return (T)Activator.CreateInstance(typeof(T), args)!;
    }

    // tool instantiation code
    // constructor needs to be analyzed with configuration and provided with necessary parameters
}