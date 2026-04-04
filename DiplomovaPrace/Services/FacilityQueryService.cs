using DiplomovaPrace.Persistence;
using DiplomovaPrace.Persistence.Schematic;
using Microsoft.EntityFrameworkCore;

namespace DiplomovaPrace.Services;

public class FacilityQueryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public FacilityQueryService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<FacilityEntity?> GetMainFacilityAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Facilities
            .Include(f => f.Nodes)
            .Include(f => f.Edges)
            .FirstOrDefaultAsync(f => f.Name == "Smart Company Facility", ct);
    }
}
