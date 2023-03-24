using BigBang1112.Gbx.Server.Models;
using Dapper;
using GbxToolAPI.Server;
using GbxToolAPI.Server.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Data;

namespace BigBang1112.Gbx.Server.Repos;

public interface IMemberRepo
{
    Task AddAsync(Member member, CancellationToken cancellationToken = default);
    Task<Member?> GetBySnowflakeAsync(ulong snowflake, CancellationToken cancellationToken = default);
}

public class MemberRepo : IMemberRepo
{
    private readonly GbxContext _context;

    public MemberRepo(GbxContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Member member, CancellationToken cancellationToken = default)
    {
        await _context.Members.AddAsync(member, cancellationToken);
    }

    public async Task<Member?> GetBySnowflakeAsync(ulong snowflake, CancellationToken cancellationToken = default)
    {
        return await _context.Members
            .Where(m => m.Snowflake == snowflake)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
