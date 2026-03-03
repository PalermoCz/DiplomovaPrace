namespace DiplomovaPrace.Services;

using System.Text.RegularExpressions;
using DiplomovaPrace.Models.Configuration;

/// <summary>
/// Vyhodnocuje výpočtové výrazy nad konfigurační a stavovou doménou budovy.
///
/// Syntaxe výrazu: FUNC(TARGET.PROPERTY)
///   FUNC     — SUM | AVG | COUNT | MIN | MAX
///   TARGET   — Floor[n] | Room["název"] | Building
///   PROPERTY — Consumption (z DeviceConfig) | Value (z IBuildingStateService)
///
/// Příklady:
///   SUM(Building.Consumption)
///   AVG(Floor[1].Value)
///   MAX(Room["Kancelář"].Value)
/// </summary>
public class ExpressionEvaluator
{
    private readonly IBuildingConfigurationService _configService;
    private readonly IBuildingStateService _stateService;

    // Regex: GROUP1=FUNC, GROUP2=TARGET, GROUP3=PROPERTY
    private static readonly Regex ExprRegex =
        new(@"^([A-Z]+)\(([^.]+)\.([A-Za-z]+)\)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Floor[n] nebo Room["název"]
    private static readonly Regex FloorIndexRegex  = new(@"^Floor\[(\d+)\]$",              RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex RoomNameRegex     = new(@"^Room\[""([^""]+)""\]$",        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BuildingRegex     = new(@"^Building$",                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ExpressionEvaluator(IBuildingConfigurationService configService, IBuildingStateService stateService)
    {
        _configService = configService;
        _stateService  = stateService;
    }

    /// <summary>
    /// Vyhodnotí výraz v kontextu dané budovy.
    /// Vrací (výsledek, null) při úspěchu nebo (null, chybová zpráva) při chybě.
    /// </summary>
    public async Task<(double? Result, string? Error)> EvaluateAsync(string expression, string buildingId)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return (null, "Prázdný výraz.");

        var m = ExprRegex.Match(expression.Trim());
        if (!m.Success)
            return (null, $"Neplatná syntaxe. Použijte: FUNC(Target.Property). Příklad: SUM(Building.Consumption)");

        var funcName  = m.Groups[1].Value.ToUpperInvariant();
        var targetStr = m.Groups[2].Value.Trim();
        var propName  = m.Groups[3].Value.ToUpperInvariant();

        // Načti konfiguraci
        var config = await _configService.GetBuildingAsync(buildingId);
        if (config is null)
            return (null, $"Budova '{buildingId}' nenalezena.");

        // Získej hodnoty dle targetu a property
        IReadOnlyList<double> values;
        try
        {
            values = GetValues(config, targetStr, propName);
        }
        catch (EvaluationException ex)
        {
            return (null, ex.Message);
        }

        if (values.Count == 0 && funcName is "AVG" or "MIN" or "MAX")
            return (null, "Žádná zařízení pro výpočet (prázdný výběr).");

        double result = funcName switch
        {
            "SUM"   => values.Sum(),
            "AVG"   => values.Count == 0 ? 0 : values.Average(),
            "COUNT" => values.Count,
            "MIN"   => values.Count == 0 ? 0 : values.Min(),
            "MAX"   => values.Count == 0 ? 0 : values.Max(),
            _       => throw new EvaluationException($"Neznámá funkce: \"{funcName}\". Povoleno: SUM, AVG, COUNT, MIN, MAX")
        };

        return (result, null);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IReadOnlyList<double> GetValues(
        DiplomovaPrace.Models.Configuration.BuildingConfig config,
        string targetStr, string propName)
    {
        var devices = ResolveDevices(config, targetStr);

        return propName switch
        {
            "CONSUMPTION" => devices.Select(d => d.Consumption).ToList(),
            "VALUE"       => devices
                                .Select(d => _stateService.GetDeviceState(d.Id)?.NumericValue)
                                .Where(v => v.HasValue)
                                .Select(v => v!.Value)
                                .ToList(),
            _ => throw new EvaluationException($"Neznámá vlastnost: \"{propName}\". Povoleno: Consumption, Value")
        };
    }

    private static IReadOnlyList<DeviceConfig> ResolveDevices(
        DiplomovaPrace.Models.Configuration.BuildingConfig config, string targetStr)
    {
        if (BuildingRegex.IsMatch(targetStr))
        {
            return config.Floors
                .Where(f => !f.IsDeleted)
                .SelectMany(f => f.Rooms.Where(r => !r.IsDeleted))
                .SelectMany(r => r.Devices.Where(d => !d.IsDeleted))
                .ToList();
        }

        var floorMatch = FloorIndexRegex.Match(targetStr);
        if (floorMatch.Success)
        {
            int level = int.Parse(floorMatch.Groups[1].Value);
            var floor = config.Floors
                .Where(f => !f.IsDeleted && f.Level == level)
                .FirstOrDefault()
                ?? throw new EvaluationException($"Patro s Level={level} nenalezeno.");

            return floor.Rooms
                .Where(r => !r.IsDeleted)
                .SelectMany(r => r.Devices.Where(d => !d.IsDeleted))
                .ToList();
        }

        var roomMatch = RoomNameRegex.Match(targetStr);
        if (roomMatch.Success)
        {
            string roomName = roomMatch.Groups[1].Value;
            var room = config.Floors
                .Where(f => !f.IsDeleted)
                .SelectMany(f => f.Rooms.Where(r => !r.IsDeleted))
                .FirstOrDefault(r => string.Equals(r.Name, roomName, StringComparison.OrdinalIgnoreCase))
                ?? throw new EvaluationException($"Místnost \"{roomName}\" nenalezena.");

            return room.Devices.Where(d => !d.IsDeleted).ToList();
        }

        throw new EvaluationException(
            $"Neznámý target: \"{targetStr}\". Povoleno: Building, Floor[n], Room[\"název\"]");
    }

    private sealed class EvaluationException(string message) : Exception(message);
}
