namespace DiplomovaPrace.Persistence;

public enum FacilityMembershipRole
{
    Owner,
    Admin,
    Viewer
}

public static class FacilityMembershipRoleExtensions
{
    public static FacilityMembershipRole ParseOrViewer(string? value)
    {
        if (Enum.TryParse<FacilityMembershipRole>(value, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return FacilityMembershipRole.Viewer;
    }

    public static bool CanUseEditor(this FacilityMembershipRole role)
        => role is FacilityMembershipRole.Owner or FacilityMembershipRole.Admin;
}
