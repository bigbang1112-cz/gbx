using GbxToolAPI;

namespace BigBang1112.Gbx.Client.Models;

public class ToolInstanceModel
{
    public ITool Tool { get; }
    public object? Output { get; set; }
    public Exception? Exception { get; set; }
    public List<string> Log { get; } = new();
    public string? FileName { get; set; }
    public string? ShortFileName => Path.GetFileName(FileName);
    public bool IsForManiaPlanet { get; set; }

    public ToolInstanceModel(ITool tool)
    {
        Tool = tool;
    }
}