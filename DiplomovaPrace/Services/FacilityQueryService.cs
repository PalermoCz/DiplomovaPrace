using DiplomovaPrace.Persistence;
using DiplomovaPrace.Persistence.Schematic;
using Microsoft.EntityFrameworkCore;

namespace DiplomovaPrace.Services;

public class FacilityQueryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly FacilityEditorStateService _editorStateService;

    public FacilityQueryService(
        IDbContextFactory<AppDbContext> dbFactory,
        FacilityEditorStateService editorStateService)
    {
        _dbFactory = dbFactory;
        _editorStateService = editorStateService;
    }

    public async Task<FacilityEntity?> GetMainFacilityAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var facility = await db.Facilities
            .AsNoTracking()
            .Include(f => f.Nodes)
            .Include(f => f.Edges)
            .FirstOrDefaultAsync(f => f.Name == "Smart Company Facility", ct);

        if (facility is null)
        {
            return null;
        }

        var nodeStatesByKey = await _editorStateService.GetNodeStatesByKeyAsync(ct);
        foreach (var node in facility.Nodes)
        {
            if (!nodeStatesByKey.TryGetValue(node.NodeKey, out var nodeState))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(nodeState.Label))
            {
                node.Label = nodeState.Label;
            }

            if (nodeState.XHint.HasValue)
            {
                node.XHint = Math.Clamp(nodeState.XHint.Value, 0.0, 1.0);
            }

            if (nodeState.YHint.HasValue)
            {
                node.YHint = Math.Clamp(nodeState.YHint.Value, 0.0, 1.0);
            }
        }

        return facility;
    }
}
