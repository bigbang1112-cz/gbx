namespace BigBang1112.Gbx.Shared;

public class Identity
{
    public string? AuthenticationType { get; set; }
    public Dictionary<string, List<string>> Claims { get; set; } = new();
    public bool? AutoLogin { get; set; }
}
