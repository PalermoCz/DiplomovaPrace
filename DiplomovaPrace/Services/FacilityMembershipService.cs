using DiplomovaPrace.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiplomovaPrace.Services;

public sealed class FacilityMembershipService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public FacilityMembershipService(
        IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<FacilityMembershipResolution?> ResolveForUserAndFacilityAsync(int appUserId, int facilityId, CancellationToken ct = default)
    {
        if (appUserId <= 0)
        {
            return null;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var membership = await db.FacilityMemberships
            .FirstOrDefaultAsync(m => m.FacilityId == facilityId && m.AppUserId == appUserId, ct);

        var wasBootstrapApplied = false;

        if (membership is null)
        {
            var facilityHasAnyMembership = await db.FacilityMemberships
                .AnyAsync(m => m.FacilityId == facilityId, ct);

            if (facilityHasAnyMembership)
            {
                return null;
            }

            membership = new FacilityMembershipEntity
            {
                FacilityId = facilityId,
                AppUserId = appUserId,
                Role = FacilityMembershipRole.Owner.ToString(),
                CreatedAtUtc = DateTime.UtcNow
            };

            db.FacilityMemberships.Add(membership);
            await db.SaveChangesAsync(ct);
            wasBootstrapApplied = true;
        }

        var role = FacilityMembershipRoleExtensions.ParseOrViewer(membership.Role);

        return new FacilityMembershipResolution(
            facilityId,
            appUserId,
            role,
            role.CanUseEditor(),
            wasBootstrapApplied);
    }
}

public sealed record FacilityMembershipResolution(
    int FacilityId,
    int AppUserId,
    FacilityMembershipRole Role,
    bool CanUseEditor,
    bool WasBootstrapApplied);
