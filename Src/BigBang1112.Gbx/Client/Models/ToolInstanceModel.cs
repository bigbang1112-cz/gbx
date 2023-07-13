using GbxToolAPI;

namespace BigBang1112.Gbx.Client.Models;

public class ToolInstanceModel
{
    public ITool Tool { get; }
    public object? Output { get; set; }
    public Exception? Exception { get; set; }
    public List<string> Log { get; } = new();
    public List<ToolInstanceFileModel> Files { get; } = new();

    public ToolInstanceModel(ITool tool)
    {
        Tool = tool;
    }
}