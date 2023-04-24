namespace BigBang1112.Gbx.Server.Options;

public class DiscordOptions
{
    public string OwnerSnowflake { get; set; } = "";
    public DiscordClientOptions Client { get; set; } = new();
}
