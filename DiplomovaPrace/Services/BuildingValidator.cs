namespace DiplomovaPrace.Services;

using DiplomovaPrace.Models.Configuration;

/// <summary>
/// Statická třída pro validaci konfigurace patra.
/// Detekuje: překryv místností, zařízení mimo svoji místnost, duplicitní názvy.
/// Neobsahuje závislosti — čisté funkce nad konfiguračními modely.
/// </summary>
public static class BuildingValidator
{
    /// <summary>
    /// Validuje jeden floor. Vrací seznam problémů (prázdný = vše OK).
    /// </summary>
    public static IReadOnlyList<ValidationIssue> ValidateFloor(FloorConfig floor)
    {
        var issues = new List<ValidationIssue>();
        var activeRooms = floor.Rooms.Where(r => !r.IsDeleted).ToList();

        // Duplicitní názvy místností
        var roomNameGroups = activeRooms.GroupBy(r => r.Name.Trim(), StringComparer.OrdinalIgnoreCase);
        foreach (var g in roomNameGroups.Where(g => g.Count() > 1))
            foreach (var room in g)
                issues.Add(new ValidationIssue(
                    $"Duplicitní název místnosti: \"{room.Name}\"",
                    IssueType.Error, RoomId: room.Id));

        // Překryv místností
        for (int i = 0; i < activeRooms.Count; i++)
        for (int j = i + 1; j < activeRooms.Count; j++)
        {
            var a = activeRooms[i].Geometry;
            var b = activeRooms[j].Geometry;
            if (Overlaps(a.X, a.Y, a.Width, a.Height, b.X, b.Y, b.Width, b.Height))
            {
                issues.Add(new ValidationIssue(
                    $"Překryv místností: \"{activeRooms[i].Name}\" a \"{activeRooms[j].Name}\"",
                    IssueType.Error, RoomId: activeRooms[i].Id));
                issues.Add(new ValidationIssue(
                    $"Překryv místností: \"{activeRooms[j].Name}\" a \"{activeRooms[i].Name}\"",
                    IssueType.Error, RoomId: activeRooms[j].Id));
            }
        }

        // Zařízení mimo svoji místnost a duplicitní názvy zařízení
        foreach (var room in activeRooms)
        {
            var activeDevices = room.Devices.Where(d => !d.IsDeleted).ToList();

            // Duplicitní názvy zařízení v rámci místnosti
            var deviceNameGroups = activeDevices.GroupBy(d => d.Name.Trim(), StringComparer.OrdinalIgnoreCase);
            foreach (var g in deviceNameGroups.Where(g => g.Count() > 1))
                foreach (var device in g)
                    issues.Add(new ValidationIssue(
                        $"Duplicitní název zařízení: \"{device.Name}\" v místnosti \"{room.Name}\"",
                        IssueType.Warning, RoomId: room.Id, DeviceId: device.Id));

            // Zařízení mimo geometrii místnosti
            var geom = room.Geometry;
            foreach (var device in activeDevices)
            {
                if (device.Position.X < geom.X || device.Position.X > geom.X + geom.Width ||
                    device.Position.Y < geom.Y || device.Position.Y > geom.Y + geom.Height)
                {
                    issues.Add(new ValidationIssue(
                        $"Zařízení \"{device.Name}\" leží mimo místnost \"{room.Name}\"",
                        IssueType.Warning, RoomId: room.Id, DeviceId: device.Id));
                }
            }
        }

        return issues;
    }

    /// <summary>Validuje celou budovu (všechna neodstraněná patra).</summary>
    public static IReadOnlyList<ValidationIssue> ValidateBuilding(BuildingConfig config)
    {
        var all = new List<ValidationIssue>();
        foreach (var floor in config.Floors.Where(f => !f.IsDeleted))
            all.AddRange(ValidateFloor(floor));
        return all;
    }

    /// <summary>Vrací true pokud se obdélníky překrývají (toleruje sdílené hrany 1 px).</summary>
    private static bool Overlaps(
        double ax, double ay, double aw, double ah,
        double bx, double by, double bw, double bh)
    {
        const double eps = 1.0;
        return ax + eps < bx + bw &&
               ax + aw - eps > bx &&
               ay + eps < by + bh &&
               ay + ah - eps > by;
    }
}
