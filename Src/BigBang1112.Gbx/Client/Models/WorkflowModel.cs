namespace BigBang1112.Gbx.Client.Models;

public class WorkflowModel
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string[] Input { get; set; } = Array.Empty<string>();
    public WorkflowTaskModel[] Tasks { get; set; } = Array.Empty<WorkflowTaskModel>();
}
