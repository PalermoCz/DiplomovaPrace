namespace DiplomovaPrace.Services;

using System.Collections.Concurrent;
using DiplomovaPrace.Models;
using DiplomovaPrace.Models.Configuration;

/// <summary>
/// In-memory implementace IBuildingConfigurationService.
/// Thread-safe přes ConcurrentDictionary + immutable record with-expressions.
/// Při startu naimportuje demo budovu z BuildingConfiguration jako seed.
///
/// Mutace vzor:
///   1. Načti BuildingConfig z _store
///   2. Vytvoř updatovanou kopii pomocí with-expression (od listu ke kořeni)
///   3. _store.TryUpdate(id, updated, current)  — optimistic swap
///   4. OnConfigurationChanged?.Invoke()
/// </summary>
public class InMemoryBuildingConfigurationService : IBuildingConfigurationService
{
    private readonly ConcurrentDictionary<string, BuildingConfig> _store = new();
    public event Action? OnConfigurationChanged;

    public InMemoryBuildingConfigurationService()
    {
        var demo = BuildingConfiguration.CreateDemoBuilding();
        var config = ImportFromDomainModel(demo);
        _store[config.Id] = config;
    }

    // ── Query ────────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<BuildingConfig>> GetAllBuildingsAsync() =>
        Task.FromResult<IReadOnlyList<BuildingConfig>>(
            _store.Values.Where(b => !b.IsDeleted).ToList());

    public Task<BuildingConfig?> GetBuildingAsync(string id)
    {
        _store.TryGetValue(id, out var config);
        return Task.FromResult(config?.IsDeleted == true ? null : config);
    }

    // ── Building CRUD ────────────────────────────────────────────────────────

    public Task<BuildingConfig> CreateBuildingAsync(string name, string? description, string? address)
    {
        var config = new BuildingConfig(
            Id: NewId(),
            Name: name,
            Description: description,
            Address: address,
            Floors: [],
            CreatedAt: Now(),
            UpdatedAt: Now(),
            CreatedBy: "editor",
            UpdatedBy: "editor",
            RowVersion: [],
            IsDeleted: false
        );
        _store[config.Id] = config;
        Notify();
        return Task.FromResult(config);
    }

    public Task<BuildingConfig> UpdateBuildingMetadataAsync(
        string buildingId, string name, string? description, string? address)
    {
        var updated = MutateBuilding(buildingId, b => b with
        {
            Name = name,
            Description = description,
            Address = address,
            UpdatedAt = Now(),
            UpdatedBy = "editor"
        });
        return Task.FromResult(updated);
    }

    public Task DeleteBuildingAsync(string buildingId)
    {
        MutateBuilding(buildingId, b => b with { IsDeleted = true, UpdatedAt = Now() });
        return Task.CompletedTask;
    }

    public Task ReplaceConfigAsync(BuildingConfig config)
    {
        _store[config.Id] = config;
        Notify();
        return Task.CompletedTask;
    }

    // ── Floor CRUD ───────────────────────────────────────────────────────────

    public Task<FloorConfig> AddFloorAsync(string buildingId, string name, int level,
        double viewBoxWidth = 800, double viewBoxHeight = 300)
    {
        var floor = new FloorConfig(
            Id: NewId(),
            BuildingId: buildingId,
            Name: name,
            Level: level,
            Description: null,
            ViewBoxWidth: viewBoxWidth,
            ViewBoxHeight: viewBoxHeight,
            Rooms: [],
            CreatedAt: Now(),
            UpdatedAt: Now(),
            IsDeleted: false
        );
        MutateBuilding(buildingId, b => b with
        {
            Floors = [.. b.Floors, floor],
            UpdatedAt = Now()
        });
        return Task.FromResult(floor);
    }

    public Task<FloorConfig> UpdateFloorAsync(string buildingId, string floorId, string name, int level)
    {
        FloorConfig? result = null;
        MutateBuilding(buildingId, b =>
        {
            var floors = b.Floors.Select(f =>
            {
                if (f.Id != floorId) return f;
                result = f with { Name = name, Level = level, UpdatedAt = Now() };
                return result;
            }).ToList();
            return b with { Floors = floors, UpdatedAt = Now() };
        });
        return Task.FromResult(result!);
    }

    public Task<FloorConfig> UpdateFloorDimensionsAsync(
        string buildingId, string floorId, double width, double height)
    {
        FloorConfig? result = null;
        MutateBuilding(buildingId, b =>
        {
            var floors = b.Floors.Select(f =>
            {
                if (f.Id != floorId) return f;
                result = f with { ViewBoxWidth = width, ViewBoxHeight = height, UpdatedAt = Now() };
                return result;
            }).ToList();
            return b with { Floors = floors, UpdatedAt = Now() };
        });
        return Task.FromResult(result!);
    }

    public Task DeleteFloorAsync(string buildingId, string floorId)
    {
        MutateBuilding(buildingId, b => b with
        {
            Floors = b.Floors.Select(f =>
                f.Id == floorId ? f with { IsDeleted = true, UpdatedAt = Now() } : f
            ).ToList(),
            UpdatedAt = Now()
        });
        return Task.CompletedTask;
    }

    public Task ReorderFloorsAsync(string buildingId, IReadOnlyList<string> orderedFloorIds)
    {
        MutateBuilding(buildingId, b =>
        {
            var lookup = b.Floors.ToDictionary(f => f.Id);
            var reordered = orderedFloorIds
                .Where(id => lookup.ContainsKey(id))
                .Select(id => lookup[id])
                .ToList();
            return b with { Floors = reordered, UpdatedAt = Now() };
        });
        return Task.CompletedTask;
    }

    // ── Room CRUD ────────────────────────────────────────────────────────────

    public Task<RoomConfig> AddRoomAsync(string floorId, string name, RoomGeometry geometry)
    {
        var room = new RoomConfig(
            Id: NewId(),
            FloorId: floorId,
            Name: name,
            Geometry: geometry,
            FillColorOverride: null,
            Devices: [],
            CreatedAt: Now(),
            UpdatedAt: Now(),
            IsDeleted: false
        );
        MutateFloor(floorId, f => f with
        {
            Rooms = [.. f.Rooms, room],
            UpdatedAt = Now()
        });
        return Task.FromResult(room);
    }

    public Task<RoomConfig> UpdateRoomNameAsync(string roomId, string name)
    {
        RoomConfig? result = null;
        MutateRoom(roomId, r =>
        {
            result = r with { Name = name, UpdatedAt = Now() };
            return result;
        });
        return Task.FromResult(result!);
    }

    public Task<RoomConfig> UpdateRoomGeometryAsync(string roomId, RoomGeometry geometry)
    {
        RoomConfig? result = null;
        MutateRoom(roomId, r =>
        {
            result = r with { Geometry = geometry, UpdatedAt = Now() };
            return result;
        });
        return Task.FromResult(result!);
    }

    public Task DeleteRoomAsync(string roomId)
    {
        MutateRoom(roomId, r => r with { IsDeleted = true, UpdatedAt = Now() });
        return Task.CompletedTask;
    }

    // ── Device CRUD ──────────────────────────────────────────────────────────

    public Task<DeviceConfig> AddDeviceAsync(
        string roomId, string name, DeviceType type, DevicePosition position)
    {
        var device = new DeviceConfig(
            Id: NewId(),
            RoomId: roomId,
            Name: name,
            Type: type,
            Position: position,
            DisplaySettings: DeviceDisplaySettings.CreateDefault(type),
            Consumption: DefaultConsumption(type),
            CreatedAt: Now(),
            UpdatedAt: Now(),
            IsDeleted: false
        );
        MutateRoom(roomId, r => r with
        {
            Devices = [.. r.Devices, device],
            UpdatedAt = Now()
        });
        return Task.FromResult(device);
    }

    public Task<DeviceConfig> UpdateDevicePositionAsync(string deviceId, DevicePosition newPosition)
    {
        DeviceConfig? result = null;
        MutateDevice(deviceId, d =>
        {
            result = d with { Position = newPosition, UpdatedAt = Now() };
            return result;
        });
        return Task.FromResult(result!);
    }

    public Task<DeviceConfig> UpdateDevicePropertiesAsync(
        string deviceId, string name, DeviceType type, DeviceDisplaySettings displaySettings,
        double consumption)
    {
        DeviceConfig? result = null;
        MutateDevice(deviceId, d =>
        {
            result = d with
            {
                Name = name,
                Type = type,
                DisplaySettings = displaySettings,
                Consumption = consumption,
                UpdatedAt = Now()
            };
            return result;
        });
        return Task.FromResult(result!);
    }

    public Task DeleteDeviceAsync(string deviceId)
    {
        MutateDevice(deviceId, d => d with { IsDeleted = true, UpdatedAt = Now() });
        return Task.CompletedTask;
    }

    // ── Domain bridge ────────────────────────────────────────────────────────

    public Building ToBuildingDomainModel(BuildingConfig config)
    {
        var floors = config.Floors
            .Where(f => !f.IsDeleted)
            .OrderBy(f => f.Level)
            .Select(f => new Floor(
                Id: f.Id,
                Name: f.Name,
                Level: f.Level,
                BuildingId: f.BuildingId,
                Rooms: f.Rooms
                    .Where(r => !r.IsDeleted)
                    .Select(r => new Room(
                        Id: r.Id,
                        Name: r.Name,
                        FloorId: r.FloorId,
                        Geometry: r.Geometry,
                        Devices: r.Devices
                            .Where(d => !d.IsDeleted)
                            .Select(d => new Device(
                                Id: d.Id,
                                Name: d.Name,
                                Type: d.Type,
                                RoomId: d.RoomId,
                                Position: d.Position))
                            .ToList()))
                    .ToList()))
            .ToList();

        return new Building(Id: config.Id, Name: config.Name, Floors: floors);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static string NewId() => Guid.NewGuid().ToString("N");
    private static DateTime Now() => DateTime.UtcNow;
    private void Notify() => OnConfigurationChanged?.Invoke();

    /// <summary>Výchozí spotřeba v Wattech dle typu zařízení.</summary>
    private static double DefaultConsumption(DeviceType type) => type switch
    {
        DeviceType.Light             => 100.0,
        DeviceType.HVAC              => 1500.0,
        DeviceType.TemperatureSensor => 5.0,
        DeviceType.HumiditySensor    => 5.0,
        DeviceType.MotionSensor      => 5.0,
        DeviceType.DoorSensor        => 3.0,
        _                            => 0.0
    };

    /// <summary>Atomicky zamění BuildingConfig dle transformační funkce. Vrací nový stav.</summary>
    private BuildingConfig MutateBuilding(string buildingId, Func<BuildingConfig, BuildingConfig> mutate)
    {
        while (true)
        {
            if (!_store.TryGetValue(buildingId, out var current))
                throw new KeyNotFoundException($"Budova '{buildingId}' nenalezena.");

            var updated = mutate(current);
            if (_store.TryUpdate(buildingId, updated, current))
            {
                Notify();
                return updated;
            }
            // Jiné vlákno změnilo záznam — opakuj (CAS loop)
        }
    }

    /// <summary>Najde patro podle floorId a provede mutaci přes MutateBuilding.</summary>
    private FloorConfig MutateFloor(string floorId, Func<FloorConfig, FloorConfig> mutate)
    {
        var (buildingId, _) = FindFloor(floorId);
        FloorConfig? result = null;
        MutateBuilding(buildingId, b =>
        {
            var floors = b.Floors.Select(f =>
            {
                if (f.Id != floorId) return f;
                result = mutate(f);
                return result;
            }).ToList();
            return b with { Floors = floors, UpdatedAt = Now() };
        });
        return result!;
    }

    /// <summary>Najde místnost podle roomId a provede mutaci přes MutateBuilding.</summary>
    private RoomConfig MutateRoom(string roomId, Func<RoomConfig, RoomConfig> mutate)
    {
        var (buildingId, floorId, _) = FindRoom(roomId);
        RoomConfig? result = null;
        MutateBuilding(buildingId, b =>
        {
            var floors = b.Floors.Select(f =>
            {
                if (f.Id != floorId) return f;
                var rooms = f.Rooms.Select(r =>
                {
                    if (r.Id != roomId) return r;
                    result = mutate(r);
                    return result;
                }).ToList();
                return f with { Rooms = rooms, UpdatedAt = Now() };
            }).ToList();
            return b with { Floors = floors, UpdatedAt = Now() };
        });
        return result!;
    }

    /// <summary>Najde zařízení podle deviceId a provede mutaci přes MutateBuilding.</summary>
    private DeviceConfig MutateDevice(string deviceId, Func<DeviceConfig, DeviceConfig> mutate)
    {
        var (buildingId, floorId, roomId, _) = FindDevice(deviceId);
        DeviceConfig? result = null;
        MutateBuilding(buildingId, b =>
        {
            var floors = b.Floors.Select(f =>
            {
                if (f.Id != floorId) return f;
                var rooms = f.Rooms.Select(r =>
                {
                    if (r.Id != roomId) return r;
                    var devices = r.Devices.Select(d =>
                    {
                        if (d.Id != deviceId) return d;
                        result = mutate(d);
                        return result;
                    }).ToList();
                    return r with { Devices = devices, UpdatedAt = Now() };
                }).ToList();
                return f with { Rooms = rooms, UpdatedAt = Now() };
            }).ToList();
            return b with { Floors = floors, UpdatedAt = Now() };
        });
        return result!;
    }

    private (string buildingId, FloorConfig floor) FindFloor(string floorId)
    {
        foreach (var (bid, building) in _store)
        foreach (var floor in building.Floors)
            if (floor.Id == floorId)
                return (bid, floor);

        throw new KeyNotFoundException($"Patro '{floorId}' nenalezeno.");
    }

    private (string buildingId, string floorId, RoomConfig room) FindRoom(string roomId)
    {
        foreach (var (bid, building) in _store)
        foreach (var floor in building.Floors)
        foreach (var room in floor.Rooms)
            if (room.Id == roomId)
                return (bid, floor.Id, room);

        throw new KeyNotFoundException($"Místnost '{roomId}' nenalezena.");
    }

    private (string buildingId, string floorId, string roomId, DeviceConfig device) FindDevice(string deviceId)
    {
        foreach (var (bid, building) in _store)
        foreach (var floor in building.Floors)
        foreach (var room in floor.Rooms)
        foreach (var device in room.Devices)
            if (device.Id == deviceId)
                return (bid, floor.Id, room.Id, device);

        throw new KeyNotFoundException($"Zařízení '{deviceId}' nenalezeno.");
    }

    /// <summary>Importuje doménový model Building do konfigurační domény při seedování.</summary>
    private static BuildingConfig ImportFromDomainModel(Building building)
    {
        var now = DateTime.UtcNow;
        var floors = building.Floors.Select(f => new FloorConfig(
            Id: f.Id,
            BuildingId: f.BuildingId,
            Name: f.Name,
            Level: f.Level,
            Description: null,
            ViewBoxWidth: 800,
            ViewBoxHeight: 300,
            Rooms: f.Rooms.Select(r => new RoomConfig(
                Id: r.Id,
                FloorId: r.FloorId,
                Name: r.Name,
                Geometry: r.Geometry,
                FillColorOverride: null,
                Devices: r.Devices.Select(d => new DeviceConfig(
                    Id: d.Id,
                    RoomId: d.RoomId,
                    Name: d.Name,
                    Type: d.Type,
                    Position: d.Position,
                    DisplaySettings: DeviceDisplaySettings.CreateDefault(d.Type),
                    Consumption: DefaultConsumption(d.Type),
                    CreatedAt: now,
                    UpdatedAt: now,
                    IsDeleted: false)).ToList(),
                CreatedAt: now,
                UpdatedAt: now,
                IsDeleted: false)).ToList(),
            CreatedAt: now,
            UpdatedAt: now,
            IsDeleted: false)).ToList();

        return new BuildingConfig(
            Id: building.Id,
            Name: building.Name,
            Description: null,
            Address: null,
            Floors: floors,
            CreatedAt: now,
            UpdatedAt: now,
            CreatedBy: "seed",
            UpdatedBy: "seed",
            RowVersion: [],
            IsDeleted: false
        );
    }
}
