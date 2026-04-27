namespace DiplomovaPrace.Services;

public enum FacilityBuiltInNodeKind
{
    Generic,
    Area,
    Bus,
    Weather
}

public static class FacilityBuiltInNodeTypes
{
    public const string AreaNodeType = "area";
    public const string BusNodeType = "bus";
    public const string WeatherNodeType = "weather";
    public const string LegacyWeatherNodeKey = "weather_main";
    private static readonly IReadOnlyList<string> KnownNodeTypes = [AreaNodeType, BusNodeType, WeatherNodeType];

    public static IReadOnlyList<string> GetKnownNodeTypes()
        => KnownNodeTypes;

    public static FacilityBuiltInNodeKind ResolveKind(string? nodeType)
    {
        var normalizedNodeType = NormalizeNodeType(nodeType);
        return normalizedNodeType switch
        {
            AreaNodeType => FacilityBuiltInNodeKind.Area,
            BusNodeType => FacilityBuiltInNodeKind.Bus,
            WeatherNodeType => FacilityBuiltInNodeKind.Weather,
            _ => FacilityBuiltInNodeKind.Generic
        };
    }

    public static bool IsBuiltIn(string? nodeType)
        => ResolveKind(nodeType) != FacilityBuiltInNodeKind.Generic;

    public static bool IsArea(string? nodeType)
        => ResolveKind(nodeType) == FacilityBuiltInNodeKind.Area;

    public static bool IsBus(string? nodeType)
        => ResolveKind(nodeType) == FacilityBuiltInNodeKind.Bus;

    public static bool IsWeather(string? nodeType)
        => ResolveKind(nodeType) == FacilityBuiltInNodeKind.Weather;

    public static bool IsLegacyWeatherNodeKey(string? nodeKey)
        => string.Equals(nodeKey?.Trim(), LegacyWeatherNodeKey, StringComparison.OrdinalIgnoreCase);

    public static bool IsWeatherNode(string? nodeKey, string? nodeType = null)
        => IsWeather(nodeType) || IsLegacyWeatherNodeKey(nodeKey);

    public static string? NormalizeNodeType(string? nodeType)
    {
        if (string.IsNullOrWhiteSpace(nodeType))
        {
            return null;
        }

        return nodeType.Trim().ToLowerInvariant();
    }
}