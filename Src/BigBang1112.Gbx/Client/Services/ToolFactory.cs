using GbxToolAPI;

namespace BigBang1112.Gbx.Client.Services;

public class ToolFactory<T> : ToolFactory where T : ITool
{
    private readonly ILogger<ToolFactory<T>> logger;

    public ToolFactory(ILogger<ToolFactory<T>> logger)
    {
        this.logger = logger;
    }

	// tool instantiation code
	// constructor needs to be analyzed with configuration and provided with necessary parameters
}

public class ToolFactory
{

}