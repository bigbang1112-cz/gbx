using System.ComponentModel.DataAnnotations;

namespace BigBang1112.Gbx.Server.Models;

public class Member
{
    public int Id { get; private set; }

    public required ulong Snowflake { get; set; }

    [StringLength(255)]
    public required string Name { get; set; }

    [StringLength(255)]
    public required int Discriminator { get; set; }

    [StringLength(255)]
    public string? AvatarHash { get; set; }
}
