namespace DiplomovaPrace.Services;

using System.Text.RegularExpressions;
using DiplomovaPrace.Models.Configuration;

/// <summary>
/// Vyhodnocuje výpočtové výrazy nad konfigurační a stavovou doménou budovy.
///
/// Syntaxe výrazu:
///   1) Agregační funkce: FUNC(TARGET.PROPERTY)
///      FUNC     — SUM | AVG | COUNT | MIN | MAX
///      TARGET   — Floor[n] | Room["název"] | Building
///      PROPERTY — Consumption (z DeviceConfig) | Value (z IBuildingStateService)
///
///   2) Porovnávací operátory: OPERAND OPERATOR OPERAND
///      OPERAND  — literal číslo nebo TARGET.PROPERTY
///      OPERATOR — > | < | >= | <= | == | !=
///      Výsledek: 1.0 (true) nebo 0.0 (false)
///
/// Příklady:
///   SUM(Building.Consumption)
///   AVG(Floor[1].Value)
///   Room["Kancelář"].Value > 30
///   Room["Lab"].Value >= 25.5
/// </summary>
public class ExpressionEvaluator
{
    private readonly IBuildingConfigurationService _configService;
    private readonly IBuildingStateService _stateService;

    // Regex: GROUP1=FUNC, GROUP2=TARGET, GROUP3=PROPERTY
    private static readonly Regex ExprRegex =
        new(@"^([A-Z]+)\(([^.]+)\.([A-Za-z]+)\)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Regex pro porovnávací operátory: GROUP1=left, GROUP2=operator, GROUP3=right
    private static readonly Regex ComparisonRegex =
        new(@"^([^><=!]+)\s*([><=!]+)\s*(.+)$", RegexOptions.Compiled);

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
    /// Pro comparison operátory vrací 1.0 (true) nebo 0.0 (false).
    /// </summary>
    public async Task<(double? Result, string? Error)> EvaluateAsync(string expression, string buildingId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(expression))
                return (null, "Prázdný výraz.");

            var expr = expression.Trim();

            // Priorita 1: Comparison operátory (vyhodnoceny první)
            var compMatch = ComparisonRegex.Match(expr);
            if (compMatch.Success)
            {
                return await EvaluateComparisonAsync(compMatch, buildingId);
            }

            // Priorita 2: Agregační funkce (původní logika)
            var m = ExprRegex.Match(expr);
            if (!m.Success)
                return (null, $"Neplatná syntaxe. Použijte: FUNC(Target.Property) nebo Target.Property > Value");

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
        catch (Exception ex)
        {
            Console.WriteLine($"Evaluator error: {ex.Message}");
            return (0.0, null);
        }
    }

    /// <summary>
    /// Vyhodnotí comparison expression (např. "Room["Lab"].Value > 30").
    /// Vrací (1.0, null) pro true, (0.0, null) pro false.
    /// </summary>
    private async Task<(double? Result, string? Error)> EvaluateComparisonAsync(
        Match match, string buildingId)
    {
        var leftExpr = match.Groups[1].Value.Trim();
        var op = match.Groups[2].Value.Trim();
        var rightExpr = match.Groups[3].Value.Trim();

        // Vyhodnotit levou stranu (může být Target.Property nebo literal)
        var (leftVal, leftErr) = await EvaluateOperandAsync(leftExpr, buildingId);
        if (leftErr != null) return (null, leftErr);

        // Vyhodnotit pravou stranu
        var (rightVal, rightErr) = await EvaluateOperandAsync(rightExpr, buildingId);
        if (rightErr != null) return (null, rightErr);

        if (!leftVal.HasValue || !rightVal.HasValue)
            return (null, "Nelze porovnat prázdné hodnoty.");

        bool result = op switch
        {
            ">"  => leftVal.Value > rightVal.Value,
            "<"  => leftVal.Value < rightVal.Value,
            ">=" => leftVal.Value >= rightVal.Value,
            "<=" => leftVal.Value <= rightVal.Value,
            "==" => Math.Abs(leftVal.Value - rightVal.Value) < 0.0001,
            "!=" => Math.Abs(leftVal.Value - rightVal.Value) >= 0.0001,
            _    => throw new EvaluationException($"Neznámý operátor: \"{op}\". Povoleno: >, <, >=, <=, ==, !=")
        };

        return (result ? 1.0 : 0.0, null);
    }

    /// <summary>
    /// Vyhodnotí operand (levou nebo pravou stranu comparison).
    /// Operand může být literal číslo (např. "30.0") nebo Target.Property (např. "Room["Lab"].Value").
    /// Pro Target.Property vrací průměrnou hodnotu všech zařízení.
    /// </summary>
    private async Task<(double? Result, string? Error)> EvaluateOperandAsync(
        string operand, string buildingId)
    {
        // Pokus o parsování jako literal number
        if (double.TryParse(operand, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double literal))
        {
            return (literal, null);
        }

        // Pokus o parsování jako Target.Property
        var targetPropertyRegex = new Regex(@"^([^.]+)\.([A-Za-z]+)$", RegexOptions.IgnoreCase);
        var targetPropertyMatch = targetPropertyRegex.Match(operand);
        if (!targetPropertyMatch.Success)
            return (null, $"Neplatný operand: \"{operand}\". Použijte číslo nebo Target.Property");

        var config = await _configService.GetBuildingAsync(buildingId);
        if (config is null)
            return (null, $"Budova '{buildingId}' nenalezena.");

        try
        {
            var targetStr = targetPropertyMatch.Groups[1].Value.Trim();
            var propName = targetPropertyMatch.Groups[2].Value.ToUpperInvariant();

            var devices = ResolveDevices(config, targetStr);
            var values = GetValuesForProperty(devices, propName);

            // Pro porovnání používáme průměr hodnot
            if (values.Count == 0)
                return (null, $"Žádná data pro {operand}");

            return (values.Average(), null);
        }
        catch (EvaluationException ex)
        {
            return (null, ex.Message);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Získá hodnoty property ze seznamu zařízení.
    /// Extrahováno z původní GetValues metody pro reuse.
    /// </summary>
    private IReadOnlyList<double> GetValuesForProperty(
        IReadOnlyList<DeviceConfig> devices, string propName)
    {
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

    private IReadOnlyList<double> GetValues(
        DiplomovaPrace.Models.Configuration.BuildingConfig config,
        string targetStr, string propName)
    {
        var devices = ResolveDevices(config, targetStr);
        return GetValuesForProperty(devices, propName);
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
