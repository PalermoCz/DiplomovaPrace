namespace DiplomovaPrace.Models.Configuration;

public record FacilityMetadata(
    string? OrganizationName = null,
    string? SiteName = null,
    string? BuildingName = null,
    double? GrossFloorAreaM2 = null,
    TimeSpan WorkingDayStart = default,
    TimeSpan WorkingDayEnd = default,
    IReadOnlyList<DayOfWeek>? WorkingDays = null,
    double? ElectricityTariffPerKWh = null
);
