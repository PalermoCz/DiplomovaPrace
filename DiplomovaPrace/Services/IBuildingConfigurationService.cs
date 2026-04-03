namespace DiplomovaPrace.Services;

using DiplomovaPrace.Models;
using DiplomovaPrace.Models.Configuration;

/// <summary>
/// Správa konfigurací budov — CRUD operace nad konfigurační doménou.
/// Singleton. Aktuální implementace: InMemoryBuildingConfigurationService.
/// Budoucí implementace: EfCoreBuildingConfigurationService (swap v DI, rozhraní se nemění).
///
/// Jediný přechod do vizualizační domény: ToBuildingDomainModel() + IBuildingStateService.ReplaceBuilding().
/// </summary>
public interface IBuildingConfigurationService
{
    // ── Query ────────────────────────────────────────────────────────────────

    Task<IReadOnlyList<BuildingConfig>> GetAllBuildingsAsync();
    Task<BuildingConfig?> GetBuildingAsync(string id);

    // ── Building CRUD ────────────────────────────────────────────────────────

    Task<BuildingConfig> CreateBuildingAsync(string name, string? description, string? address);
    Task<BuildingConfig> UpdateBuildingMetadataAsync(string buildingId, string name, string? description, string? address, FacilityMetadata? metadata = null);
    Task DeleteBuildingAsync(string buildingId);

    // ── Floor CRUD ───────────────────────────────────────────────────────────

    Task<FloorConfig> AddFloorAsync(string buildingId, string name, int level,
        double viewBoxWidth = 800, double viewBoxHeight = 300);
    Task<FloorConfig> UpdateFloorAsync(string buildingId, string floorId, string name, int level);

    /// <summary>Aktualizuje rozměry plátna patra (SVG viewBox). Okamžitě se projeví v canvasu editoru.</summary>
    Task<FloorConfig> UpdateFloorDimensionsAsync(string buildingId, string floorId, double width, double height);
    Task DeleteFloorAsync(string buildingId, string floorId);

    /// <summary>Přeřadí patra dle zadaného pořadí ID.</summary>
    Task ReorderFloorsAsync(string buildingId, IReadOnlyList<string> orderedFloorIds);

    // ── Room CRUD ────────────────────────────────────────────────────────────

    Task<RoomConfig> AddRoomAsync(string floorId, string name, RoomGeometry geometry);
    Task<RoomConfig> UpdateRoomNameAsync(string roomId, string name);
    Task<RoomConfig> UpdateRoomGeometryAsync(string roomId, RoomGeometry geometry);
    Task<RoomConfig> UpdateRoomDisplayRulesAsync(string roomId, IReadOnlyList<DisplayRule> displayRules);
    Task DeleteRoomAsync(string roomId);

    // ── Device CRUD ──────────────────────────────────────────────────────────

    Task<DeviceConfig> AddDeviceAsync(string roomId, string name, DeviceType type, DevicePosition position);
    Task<DeviceConfig> UpdateDevicePositionAsync(string deviceId, DevicePosition newPosition);

    /// <summary>Aktualizuje vlastnosti zařízení včetně spotřeby (Consumption v Wattech).</summary>
    Task<DeviceConfig> UpdateDevicePropertiesAsync(string deviceId, string name, DeviceType type,
        DeviceDisplaySettings displaySettings, double consumption, IReadOnlyList<DisplayRule> displayRules);
    Task DeleteDeviceAsync(string deviceId);

    // ── Bulk replace ─────────────────────────────────────────────────────────

    /// <summary>
    /// Atomicky nahradí celou BuildingConfig (např. při importu ze souboru).
    /// Zachová Id, přepíše vše ostatní.
    /// </summary>
    Task ReplaceConfigAsync(BuildingConfig config);

    // ── Domain bridge ────────────────────────────────────────────────────────

    /// <summary>
    /// Konvertuje konfiguraci na doménový model Building pro BuildingStateService.
    /// Jediný přechod mezi konfigurační a vizualizační doménou.
    /// </summary>
    Building ToBuildingDomainModel(BuildingConfig config);

    // ── Observer ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Vyvoláno po každé změně konfigurace.
    /// Editor komponenty se přihlašují a volají InvokeAsync(StateHasChanged).
    /// </summary>
    event Action? OnConfigurationChanged;
}
