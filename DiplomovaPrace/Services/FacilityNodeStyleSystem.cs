namespace DiplomovaPrace.Services;

public enum FacilityNodeShapeKind
{
    Circle,
    Square,
    Diamond
}

public sealed class FacilityNodeStylePreset
{
    public required string Key { get; init; }
    public string Name { get; init; } = string.Empty;
    public FacilityNodeShapeKind Shape { get; init; } = FacilityNodeShapeKind.Circle;
    public double Size { get; init; } = 28;
    public string FillColor { get; init; } = "#f1f5f9";
    public string StrokeColor { get; init; } = "#475569";
    public double StrokeWidth { get; init; } = 1.8;
    public string Symbol { get; init; } = string.Empty;
    public string SymbolColor { get; init; } = "#334155";
    public string LabelColor { get; init; } = "#0f172a";
    public double LabelFontSize { get; init; } = 8.5;
    public int LabelFontWeight { get; init; } = 600;
}

public sealed class ResolvedFacilityNodeStyle
{
    public required string PresetKey { get; init; }
    public required string PresetName { get; init; }
    public required FacilityNodeShapeKind Shape { get; init; }
    public required double Size { get; init; }
    public required string FillColor { get; init; }
    public required string StrokeColor { get; init; }
    public required double StrokeWidth { get; init; }
    public required string Symbol { get; init; }
    public required string SymbolColor { get; init; }
    public required string LabelColor { get; init; }
    public required double LabelFontSize { get; init; }
    public required int LabelFontWeight { get; init; }
}

public static class FacilityNodeStyleSystem
{
    public const string DefaultPresetKey = "default-node";

    /// <summary>
    /// Built-in structural presets that are hidden from the user-facing preset picker
    /// unless the node currently has one of them assigned.
    /// </summary>
    public static readonly IReadOnlySet<string> SystemPresetKeys =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "container-node", "connector-node", "endpoint-node" };

    private static readonly IReadOnlyDictionary<string, FacilityNodeStylePreset> DefaultPresets =
        new Dictionary<string, FacilityNodeStylePreset>(StringComparer.OrdinalIgnoreCase)
        {
            [DefaultPresetKey] = new FacilityNodeStylePreset
            {
                Key = DefaultPresetKey,
                Name = "Default node",
                Shape = FacilityNodeShapeKind.Circle,
                Size = 26,
                FillColor = "#f1f5f9",
                StrokeColor = "#475569",
                StrokeWidth = 1.8,
                Symbol = string.Empty,
                SymbolColor = "#334155",
                LabelColor = "#0f172a",
                LabelFontSize = 8.5,
                LabelFontWeight = 600
            },
            ["container-node"] = new FacilityNodeStylePreset
            {
                Key = "container-node",
                Name = "Container node",
                Shape = FacilityNodeShapeKind.Square,
                Size = 30,
                FillColor = "#e2e8f0",
                StrokeColor = "#475569",
                StrokeWidth = 2.0,
                Symbol = string.Empty,
                SymbolColor = "#334155",
                LabelColor = "#0f172a",
                LabelFontSize = 9.0,
                LabelFontWeight = 650
            },
            ["connector-node"] = new FacilityNodeStylePreset
            {
                Key = "connector-node",
                Name = "Connector node",
                Shape = FacilityNodeShapeKind.Diamond,
                Size = 28,
                FillColor = "#dbeafe",
                StrokeColor = "#2563eb",
                StrokeWidth = 2.0,
                Symbol = string.Empty,
                SymbolColor = "#1d4ed8",
                LabelColor = "#1e3a8a",
                LabelFontSize = 8.5,
                LabelFontWeight = 620
            },
            ["endpoint-node"] = new FacilityNodeStylePreset
            {
                Key = "endpoint-node",
                Name = "Endpoint node",
                Shape = FacilityNodeShapeKind.Circle,
                Size = 22,
                FillColor = "#ecfccb",
                StrokeColor = "#4d7c0f",
                StrokeWidth = 1.8,
                Symbol = string.Empty,
                SymbolColor = "#3f6212",
                LabelColor = "#365314",
                LabelFontSize = 7.8,
                LabelFontWeight = 580
            }
        };

    public static IReadOnlyDictionary<string, FacilityNodeStylePreset> CreateDefaultPresetLibrary()
    {
        return DefaultPresets.ToDictionary(
            pair => pair.Key,
            pair => NormalizePreset(pair.Value),
            StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyDictionary<string, FacilityNodeStylePreset> NormalizePresetLibrary(
        IEnumerable<FacilityNodeStylePreset>? presets)
    {
        var normalized = CreateDefaultPresetLibrary()
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        if (presets is null)
        {
            return normalized;
        }

        foreach (var preset in presets)
        {
            var next = NormalizePreset(preset);
            normalized[next.Key] = next;
        }

        if (!normalized.ContainsKey(DefaultPresetKey))
        {
            normalized[DefaultPresetKey] = NormalizePreset(DefaultPresets[DefaultPresetKey]);
        }

        return normalized;
    }

    public static FacilityNodeStylePreset NormalizePreset(FacilityNodeStylePreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);

        var key = NormalizePresetKey(preset.Key);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Style preset key must be provided.");
        }

        return new FacilityNodeStylePreset
        {
            Key = key,
            Name = string.IsNullOrWhiteSpace(preset.Name) ? key : preset.Name.Trim(),
            Shape = preset.Shape,
            Size = Math.Clamp(EnsureFinite(preset.Size, 28), 16, 60),
            FillColor = NormalizeColor(preset.FillColor, "#f1f5f9"),
            StrokeColor = NormalizeColor(preset.StrokeColor, "#475569"),
            StrokeWidth = Math.Clamp(EnsureFinite(preset.StrokeWidth, 1.8), 1, 6),
            Symbol = NormalizeSymbol(preset.Symbol),
            SymbolColor = NormalizeColor(preset.SymbolColor, "#334155"),
            LabelColor = NormalizeColor(preset.LabelColor, "#0f172a"),
            LabelFontSize = Math.Clamp(EnsureFinite(preset.LabelFontSize, 8.5), 6, 18),
            LabelFontWeight = Math.Clamp(preset.LabelFontWeight, 400, 800)
        };
    }

    public static string NormalizePresetKey(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }

    public static string ResolvePresetKey(string? nodePresetKey, IReadOnlyDictionary<string, FacilityNodeStylePreset>? presets)
    {
        var normalizedKey = NormalizePresetKey(nodePresetKey);
        if (!string.IsNullOrWhiteSpace(normalizedKey)
            && presets is not null
            && presets.ContainsKey(normalizedKey))
        {
            return normalizedKey;
        }

        return DefaultPresetKey;
    }

    public static ResolvedFacilityNodeStyle ResolveStyle(
        string? nodePresetKey,
        IReadOnlyDictionary<string, FacilityNodeStylePreset>? presets)
    {
        var library = presets is null || presets.Count == 0
            ? CreateDefaultPresetLibrary()
            : presets;

        var resolvedKey = ResolvePresetKey(nodePresetKey, library);
        var preset = library.TryGetValue(resolvedKey, out var value)
            ? value
            : library[DefaultPresetKey];

        return new ResolvedFacilityNodeStyle
        {
            PresetKey = preset.Key,
            PresetName = preset.Name,
            Shape = preset.Shape,
            Size = preset.Size,
            FillColor = preset.FillColor,
            StrokeColor = preset.StrokeColor,
            StrokeWidth = preset.StrokeWidth,
            Symbol = preset.Symbol,
            SymbolColor = preset.SymbolColor,
            LabelColor = preset.LabelColor,
            LabelFontSize = preset.LabelFontSize,
            LabelFontWeight = preset.LabelFontWeight
        };
    }

    private static double EnsureFinite(double value, double fallback)
    {
        return double.IsFinite(value) ? value : fallback;
    }

    private static string NormalizeColor(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }

    private static string NormalizeSymbol(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= 3 ? trimmed : trimmed[..3];
    }
}