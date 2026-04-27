namespace DiplomovaPrace.Services;

public enum FacilitySignalFamily
{
    Power,
    Energy,
    Voltage,
    Current,
    PowerFactor,
    ReactivePower,
    WeatherTemperature,
    Custom
}

public readonly record struct FacilitySignalCode
{
    public static readonly FacilitySignalCode P = new("P");
    public static readonly FacilitySignalCode P1 = new("P1");
    public static readonly FacilitySignalCode P2 = new("P2");
    public static readonly FacilitySignalCode P3 = new("P3");
    public static readonly FacilitySignalCode W = new("W");
    public static readonly FacilitySignalCode WIn = new("W_in");
    public static readonly FacilitySignalCode WOut = new("W_out");
    public static readonly FacilitySignalCode U1 = new("U1");
    public static readonly FacilitySignalCode U2 = new("U2");
    public static readonly FacilitySignalCode U3 = new("U3");
    public static readonly FacilitySignalCode I1 = new("I1");
    public static readonly FacilitySignalCode I2 = new("I2");
    public static readonly FacilitySignalCode I3 = new("I3");
    public static readonly FacilitySignalCode PF = new("PF");
    public static readonly FacilitySignalCode PF1 = new("PF1");
    public static readonly FacilitySignalCode PF2 = new("PF2");
    public static readonly FacilitySignalCode PF3 = new("PF3");
    public static readonly FacilitySignalCode Q = new("Q");
    public static readonly FacilitySignalCode Ta = new("Ta");
    public static readonly FacilitySignalCode Custom = new("custom");

    public FacilitySignalCode(string? value)
    {
        Value = value?.Trim() ?? string.Empty;
    }

    public string Value { get; }

    public bool IsEmpty => string.IsNullOrEmpty(Value);

    public override string ToString() => Value ?? string.Empty;
}

public static class FacilitySignalTaxonomy
{
    private static readonly IReadOnlyDictionary<string, FacilitySignalCode> KnownCodes =
        new Dictionary<string, FacilitySignalCode>(StringComparer.OrdinalIgnoreCase)
        {
            [FacilitySignalCode.P.Value] = FacilitySignalCode.P,
            [FacilitySignalCode.P1.Value] = FacilitySignalCode.P1,
            [FacilitySignalCode.P2.Value] = FacilitySignalCode.P2,
            [FacilitySignalCode.P3.Value] = FacilitySignalCode.P3,
            [FacilitySignalCode.W.Value] = FacilitySignalCode.W,
            [FacilitySignalCode.WIn.Value] = FacilitySignalCode.WIn,
            [FacilitySignalCode.WOut.Value] = FacilitySignalCode.WOut,
            [FacilitySignalCode.U1.Value] = FacilitySignalCode.U1,
            [FacilitySignalCode.U2.Value] = FacilitySignalCode.U2,
            [FacilitySignalCode.U3.Value] = FacilitySignalCode.U3,
            [FacilitySignalCode.I1.Value] = FacilitySignalCode.I1,
            [FacilitySignalCode.I2.Value] = FacilitySignalCode.I2,
            [FacilitySignalCode.I3.Value] = FacilitySignalCode.I3,
            [FacilitySignalCode.PF.Value] = FacilitySignalCode.PF,
            [FacilitySignalCode.PF1.Value] = FacilitySignalCode.PF1,
            [FacilitySignalCode.PF2.Value] = FacilitySignalCode.PF2,
            [FacilitySignalCode.PF3.Value] = FacilitySignalCode.PF3,
            [FacilitySignalCode.Q.Value] = FacilitySignalCode.Q,
            [FacilitySignalCode.Ta.Value] = FacilitySignalCode.Ta,
            [FacilitySignalCode.Custom.Value] = FacilitySignalCode.Custom,
        };

    private static readonly IReadOnlyDictionary<string, FacilitySignalFamily> FamiliesByCode =
        new Dictionary<string, FacilitySignalFamily>(StringComparer.OrdinalIgnoreCase)
        {
            [FacilitySignalCode.P.Value] = FacilitySignalFamily.Power,
            [FacilitySignalCode.P1.Value] = FacilitySignalFamily.Power,
            [FacilitySignalCode.P2.Value] = FacilitySignalFamily.Power,
            [FacilitySignalCode.P3.Value] = FacilitySignalFamily.Power,
            [FacilitySignalCode.W.Value] = FacilitySignalFamily.Energy,
            [FacilitySignalCode.WIn.Value] = FacilitySignalFamily.Energy,
            [FacilitySignalCode.WOut.Value] = FacilitySignalFamily.Energy,
            [FacilitySignalCode.U1.Value] = FacilitySignalFamily.Voltage,
            [FacilitySignalCode.U2.Value] = FacilitySignalFamily.Voltage,
            [FacilitySignalCode.U3.Value] = FacilitySignalFamily.Voltage,
            [FacilitySignalCode.I1.Value] = FacilitySignalFamily.Current,
            [FacilitySignalCode.I2.Value] = FacilitySignalFamily.Current,
            [FacilitySignalCode.I3.Value] = FacilitySignalFamily.Current,
            [FacilitySignalCode.PF.Value] = FacilitySignalFamily.PowerFactor,
            [FacilitySignalCode.PF1.Value] = FacilitySignalFamily.PowerFactor,
            [FacilitySignalCode.PF2.Value] = FacilitySignalFamily.PowerFactor,
            [FacilitySignalCode.PF3.Value] = FacilitySignalFamily.PowerFactor,
            [FacilitySignalCode.Q.Value] = FacilitySignalFamily.ReactivePower,
            [FacilitySignalCode.Ta.Value] = FacilitySignalFamily.WeatherTemperature,
            [FacilitySignalCode.Custom.Value] = FacilitySignalFamily.Custom,
        };

    public static IReadOnlyCollection<FacilitySignalCode> GetKnownCodes()
        => KnownCodes.Values.ToList();

    public static FacilitySignalCode NormalizeExactCode(string? rawSignalCode)
    {
        if (TryParseExactCode(rawSignalCode, out var code))
        {
            return code;
        }

        return new FacilitySignalCode(rawSignalCode);
    }

    public static bool TryParseExactCode(string? rawSignalCode, out FacilitySignalCode code)
    {
        var normalized = rawSignalCode?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            code = default;
            return false;
        }

        if (KnownCodes.TryGetValue(normalized, out code))
        {
            return true;
        }

        code = new FacilitySignalCode(normalized);
        return true;
    }

    public static bool IsKnownExactCode(string? rawSignalCode)
        => !string.IsNullOrWhiteSpace(rawSignalCode) && KnownCodes.ContainsKey(rawSignalCode.Trim());

    public static bool MatchesExactCode(FacilitySignalCode left, FacilitySignalCode right)
    {
        if (left.IsEmpty || right.IsEmpty)
        {
            return false;
        }

        return string.Equals(left.Value, right.Value, StringComparison.OrdinalIgnoreCase);
    }

    public static bool MatchesExactCode(string? rawSignalCode, FacilitySignalCode exactSignalCode)
        => MatchesExactCode(NormalizeExactCode(rawSignalCode), exactSignalCode);

    public static FacilitySignalFamily ResolveFamily(string? rawSignalCode)
        => ResolveFamily(NormalizeExactCode(rawSignalCode));

    public static FacilitySignalFamily ResolveFamily(FacilitySignalCode exactSignalCode)
    {
        if (exactSignalCode.IsEmpty)
        {
            return FacilitySignalFamily.Custom;
        }

        return FamiliesByCode.TryGetValue(exactSignalCode.Value, out var family)
            ? family
            : FacilitySignalFamily.Custom;
    }

    public static bool IsInFamily(FacilitySignalCode exactSignalCode, FacilitySignalFamily family)
        => ResolveFamily(exactSignalCode) == family;
}