using BigBang1112.Gbx.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace BigBang1112.Gbx.Server;

public class GbxContext : DbContext
{
    public DbSet<Member> Members { get; set; }

    public GbxContext(DbContextOptions<GbxContext> options) : base(options)
    {
        
    }
}
