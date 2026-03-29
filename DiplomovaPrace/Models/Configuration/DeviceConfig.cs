using DiplomovaPrace.Models;

namespace DiplomovaPrace.Models.Configuration;

/// <summary>
/// Konfigurační model zařízení. Součást konfigurační domény, oddělené od vizualizační domény.
/// Id je primární klíč pro budoucí EF Core persistenci.
/// MeteringMetadata jsou volitelná metadata pro smart metering zařízení.
/// </summary>
public record DeviceConfig(
    string Id,
    string RoomId,
    string Name,
    DeviceType Type,
    DevicePosition Position,
    DeviceDisplaySettings DisplaySettings,
    /// <summary>Spotřeba zařízení v Wattech. Používá se v agregacích (souhrnné panely, ExpressionEvaluator).</summary>
    double Consumption,
    IReadOnlyList<DisplayRule> DisplayRules,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsDeleted,
    MeteringMetadata? MeteringMetadata = null
);
