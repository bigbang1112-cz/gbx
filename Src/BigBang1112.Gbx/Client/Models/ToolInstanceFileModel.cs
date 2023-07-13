namespace BigBang1112.Gbx.Client.Models;

public class ToolInstanceFileModel
{
    public string? FileName { get; set; }
    public string? ShortFileName => Path.GetFileName(FileName);
    public bool IsForManiaPlanet { get; set; }
}
