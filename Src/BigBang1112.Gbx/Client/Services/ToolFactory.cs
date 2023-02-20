using GbxToolAPI;

namespace BigBang1112.Gbx.Client.Services;

public interface IToolFactory
{
    ITool CreateTool(params object?[] args);
}

public class ToolFactory<T> : IToolFactory where T : ITool
{
    private readonly ILogger<ToolFactory<T>> logger;

    public ToolFactory(ILogger<ToolFactory<T>> logger)
    {
        this.logger = logger;
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