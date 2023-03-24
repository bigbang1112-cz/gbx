using BigBang1112.Gbx.Server.Repos;

namespace BigBang1112.Gbx.Server;

public interface IGbxUnitOfWork
{
    IMemberRepo Members { get; }

    void Save();
    Task SaveAsync(CancellationToken cancellationToken = default);
}

public class GbxUnitOfWork : IGbxUnitOfWork
{
    private readonly GbxContext _context;

    public IMemberRepo Members { get; }

    public GbxUnitOfWork(GbxContext context)
    {
        _context = context;

        Members = new MemberRepo(context);
    }
    
    public void Save()
    {
        _context.SaveChanges();
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
