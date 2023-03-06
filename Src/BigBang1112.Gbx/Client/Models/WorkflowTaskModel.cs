namespace BigBang1112.Gbx.Client.Models;

public class WorkflowTaskModel
{
    public string? Function { get; set; }
    public string? Tool { get; set; }
    public string[] Input { get; set; } = Array.Empty<string>();
    public string? Output { get; set; }
}
