namespace DiplomovaPrace.Models;

/// <summary>
/// Pozice ikony zařízení v SVG souřadnicovém systému půdorysu.
/// </summary>
public record DevicePosition(double X, double Y);

/// <summary>
/// Strukturální definice zařízení nebo senzoru v místnosti.
/// Stav zařízení je uložen odděleně v BuildingStateService — oddělení
/// statické konfigurace od dynamických dat zjednodušuje správu konkurence.
/// </summary>
public record Device(
    string Id,
    string Name,
    DeviceType Type,
    string RoomId,
    DevicePosition Position
);
