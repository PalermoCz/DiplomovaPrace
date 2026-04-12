using DiplomovaPrace.Persistence.Schematic;

namespace DiplomovaPrace.Services;

public enum FacilityNodeRole
{
    GridTransformer,
    EnergyProduction,
    HvacCoolingVentilation,
    TestingSimulation,
    PowerDistribution,
    ServersItCooling,
    OtherUnclassified
}

public enum FacilityNodeSemanticType
{
    Infrastructure,
    Production,
    HvacThermal,
    TestingLab,
    Distribution,
    ItServer,
    WeatherContext,
    ZonalContext,
    Other
}

public enum FacilitySemanticPreset
{
    Hvac,
    Production,
    Distribution,
    Servers,
    ProductionAndHvac
}

public sealed class FacilityNodeMetadata
{
    public required string NodeKey { get; init; }
    public string? NodeType { get; init; }
    public string? Label { get; init; }
    public string? Zone { get; init; }
    public string? MeterUrn { get; init; }
    public FacilityNodeRole Role { get; init; }
    public FacilityNodeSemanticType SemanticType { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}

public sealed class FacilityNodeSemanticQuery
{
    public FacilityNodeRole? Role { get; init; }
    public FacilityNodeSemanticType? SemanticType { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public bool IncludeWeatherContext { get; init; }
}

public static class FacilityNodeSemantics
{
    private static readonly Dictionary<string, FacilityNodeRole> NodeKeyRoleOverrides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ext_grid"] = FacilityNodeRole.GridTransformer,
        ["trafo_vz81"] = FacilityNodeRole.GridTransformer,
        ["trafo_vz82"] = FacilityNodeRole.GridTransformer,
        ["trafo_h2z35"] = FacilityNodeRole.GridTransformer,
        ["trafo_h2z36"] = FacilityNodeRole.GridTransformer,
        ["pv_main"] = FacilityNodeRole.EnergyProduction,
        ["chp_main"] = FacilityNodeRole.EnergyProduction,
        ["cooling_main"] = FacilityNodeRole.HvacCoolingVentilation,
        ["heating_main"] = FacilityNodeRole.HvacCoolingVentilation,
        ["em_test_1"] = FacilityNodeRole.TestingSimulation,
        ["em_test_2"] = FacilityNodeRole.TestingSimulation,
        ["ds_sim_1"] = FacilityNodeRole.TestingSimulation,
        ["ds_sim_2"] = FacilityNodeRole.TestingSimulation,
        ["em_dist_1"] = FacilityNodeRole.PowerDistribution,
        ["em_dist_2"] = FacilityNodeRole.PowerDistribution,
        ["ds_dist_1"] = FacilityNodeRole.PowerDistribution,
        ["ds_dist_2"] = FacilityNodeRole.PowerDistribution,
        ["office_main"] = FacilityNodeRole.PowerDistribution,
        ["workshop_main"] = FacilityNodeRole.PowerDistribution,
        ["ds_main"] = FacilityNodeRole.PowerDistribution,
        ["ds_server_1"] = FacilityNodeRole.ServersItCooling,
        ["workshop_server_1"] = FacilityNodeRole.ServersItCooling,
        ["workshop_server_2"] = FacilityNodeRole.ServersItCooling
    };

    private static readonly Dictionary<string, FacilityNodeRole> NodeTypeRoleMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["grid"] = FacilityNodeRole.GridTransformer,
        ["transformer"] = FacilityNodeRole.GridTransformer,
        ["generator_pv"] = FacilityNodeRole.EnergyProduction,
        ["generator_chp"] = FacilityNodeRole.EnergyProduction,
        ["hvac_branch"] = FacilityNodeRole.HvacCoolingVentilation,
        ["cooling_branch"] = FacilityNodeRole.HvacCoolingVentilation,
        ["ventilation"] = FacilityNodeRole.HvacCoolingVentilation,
        ["utility_cooling"] = FacilityNodeRole.HvacCoolingVentilation,
        ["utility_heating"] = FacilityNodeRole.HvacCoolingVentilation,
        ["test"] = FacilityNodeRole.TestingSimulation,
        ["simulation"] = FacilityNodeRole.TestingSimulation,
        ["subzone"] = FacilityNodeRole.PowerDistribution,
        ["server"] = FacilityNodeRole.ServersItCooling
    };

    private static readonly string[] GridTransformerTokens = ["grid", "transformer", "trafo"];
    private static readonly string[] EnergyProductionTokens = ["pv", "chp", "generator", "production", "solar"];
    private static readonly string[] HvacTokens = ["cooling", "heating", "hvac", "ventilation", "vent"];
    private static readonly string[] TestingTokens = ["test", "simulation", "simulator"];
    private static readonly string[] DistributionTokens = ["distribution", "dist", "subzone", "office_main", "workshop_main", "ds_main", "main feeder"];
    private static readonly string[] ServersTokens = ["server", "it cooling", "datacenter"];

    public static FacilityNodeMetadata BuildMetadata(SchematicNodeEntity node)
    {
        return BuildMetadata(node.NodeKey, node.NodeType, node.Label, node.Zone, node.MeterUrn);
    }

    public static FacilityNodeMetadata BuildMetadata(
        string nodeKey,
        string? nodeType,
        string? label,
        string? zone,
        string? meterUrn)
    {
        var role = ResolveRole(nodeKey, nodeType, label);
        var semanticType = ResolveSemanticType(nodeKey, nodeType, label, role);
        var tags = DeriveTags(nodeKey, nodeType, label, zone, meterUrn, role, semanticType);

        return new FacilityNodeMetadata
        {
            NodeKey = nodeKey,
            NodeType = nodeType,
            Label = label,
            Zone = zone,
            MeterUrn = meterUrn,
            Role = role,
            SemanticType = semanticType,
            Tags = tags
        };
    }

    public static FacilityNodeSemanticType ResolveSemanticType(
        string? nodeKey,
        string? nodeType,
        string? label,
        FacilityNodeRole? role = null)
    {
        if (IsWeatherContextNode(nodeKey))
        {
            return FacilityNodeSemanticType.WeatherContext;
        }

        var effectiveRole = role ?? ResolveRole(nodeKey, nodeType, label);
        if (effectiveRole == FacilityNodeRole.GridTransformer)
        {
            return FacilityNodeSemanticType.Infrastructure;
        }

        if (effectiveRole == FacilityNodeRole.EnergyProduction)
        {
            return FacilityNodeSemanticType.Production;
        }

        if (effectiveRole == FacilityNodeRole.HvacCoolingVentilation)
        {
            return FacilityNodeSemanticType.HvacThermal;
        }

        if (effectiveRole == FacilityNodeRole.TestingSimulation)
        {
            return FacilityNodeSemanticType.TestingLab;
        }

        if (effectiveRole == FacilityNodeRole.PowerDistribution)
        {
            return FacilityNodeSemanticType.Distribution;
        }

        if (effectiveRole == FacilityNodeRole.ServersItCooling)
        {
            return FacilityNodeSemanticType.ItServer;
        }

        if (string.Equals(nodeType, "zone", StringComparison.OrdinalIgnoreCase)
            || string.Equals(nodeType, "subzone", StringComparison.OrdinalIgnoreCase))
        {
            return FacilityNodeSemanticType.ZonalContext;
        }

        return FacilityNodeSemanticType.Other;
    }

    public static IReadOnlyList<string> DeriveTags(
        string? nodeKey,
        string? nodeType,
        string? label,
        string? zone,
        string? meterUrn,
        FacilityNodeRole? role = null,
        FacilityNodeSemanticType? semanticType = null)
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var effectiveRole = role ?? ResolveRole(nodeKey, nodeType, label);
        var effectiveSemanticType = semanticType ?? ResolveSemanticType(nodeKey, nodeType, label, effectiveRole);

        tags.Add("facility");
        tags.Add($"role:{ToTagSlug(effectiveRole)}");
        tags.Add($"semantic:{ToTagSlug(effectiveSemanticType)}");

        if (!string.IsNullOrWhiteSpace(nodeType))
        {
            tags.Add($"type:{ToTagSlug(nodeType)}");
        }

        if (!string.IsNullOrWhiteSpace(zone))
        {
            tags.Add($"zone:{ToTagSlug(zone)}");
        }

        tags.Add(string.IsNullOrWhiteSpace(meterUrn) ? "unmetered" : "metered");

        var combined = $"{nodeKey} {nodeType} {label}";
        if (ContainsAnyToken(combined, HvacTokens))
        {
            tags.Add("hvac");
        }

        if (ContainsAnyToken(combined, EnergyProductionTokens))
        {
            tags.Add("production");
        }

        if (ContainsAnyToken(combined, DistributionTokens))
        {
            tags.Add("distribution");
        }

        if (ContainsAnyToken(combined, ServersTokens))
        {
            tags.Add("servers");
        }

        if (IsWeatherContextNode(nodeKey))
        {
            tags.Add("weather-context");
            tags.Add("context-only");
        }

        return tags
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<string> ParseTagQuery(string? rawTags)
    {
        if (string.IsNullOrWhiteSpace(rawTags))
        {
            return [];
        }

        return rawTags
            .Split([',', ';', '|', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ToTagSlug)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<string> ResolveMatchingNodeKeys(
        IEnumerable<SchematicNodeEntity> nodes,
        FacilityNodeSemanticQuery query)
    {
        var matchingNodeKeys = new List<string>();

        foreach (var node in nodes)
        {
            var metadata = BuildMetadata(node);
            if (MatchesQuery(metadata, query))
            {
                matchingNodeKeys.Add(node.NodeKey);
            }
        }

        return matchingNodeKeys;
    }

    public static bool MatchesQuery(FacilityNodeMetadata metadata, FacilityNodeSemanticQuery query)
    {
        if (!query.IncludeWeatherContext && metadata.SemanticType == FacilityNodeSemanticType.WeatherContext)
        {
            return false;
        }

        if (query.Role.HasValue && metadata.Role != query.Role.Value)
        {
            return false;
        }

        if (query.SemanticType.HasValue && metadata.SemanticType != query.SemanticType.Value)
        {
            return false;
        }

        if (query.Tags.Count == 0)
        {
            return true;
        }

        foreach (var rawRequiredTag in query.Tags)
        {
            var requiredTag = ToTagSlug(rawRequiredTag);
            var hasMatch = metadata.Tags.Any(tag => TagMatches(tag, requiredTag));
            if (!hasMatch)
            {
                return false;
            }
        }

        return true;
    }

    public static FacilityNodeSemanticQuery GetPresetQuery(FacilitySemanticPreset preset)
    {
        return preset switch
        {
            FacilitySemanticPreset.Hvac => new FacilityNodeSemanticQuery
            {
                Role = FacilityNodeRole.HvacCoolingVentilation,
                Tags = ["hvac"]
            },
            FacilitySemanticPreset.Production => new FacilityNodeSemanticQuery
            {
                Role = FacilityNodeRole.EnergyProduction,
                Tags = ["production"]
            },
            FacilitySemanticPreset.Distribution => new FacilityNodeSemanticQuery
            {
                Role = FacilityNodeRole.PowerDistribution,
                Tags = ["distribution"]
            },
            FacilitySemanticPreset.Servers => new FacilityNodeSemanticQuery
            {
                Role = FacilityNodeRole.ServersItCooling,
                Tags = ["servers"]
            },
            _ => new FacilityNodeSemanticQuery
            {
                Tags = ["production", "hvac"]
            }
        };
    }

    public static string GetSemanticTypeLabel(FacilityNodeSemanticType semanticType)
    {
        return semanticType switch
        {
            FacilityNodeSemanticType.Infrastructure => "Infrastructure",
            FacilityNodeSemanticType.Production => "Production",
            FacilityNodeSemanticType.HvacThermal => "HVAC / Thermal",
            FacilityNodeSemanticType.TestingLab => "Testing / Lab",
            FacilityNodeSemanticType.Distribution => "Distribution",
            FacilityNodeSemanticType.ItServer => "IT / Servers",
            FacilityNodeSemanticType.WeatherContext => "Weather Context",
            FacilityNodeSemanticType.ZonalContext => "Zonal Context",
            _ => "Other"
        };
    }

    public static string GetPresetLabel(FacilitySemanticPreset preset)
    {
        return preset switch
        {
            FacilitySemanticPreset.Hvac => "HVAC",
            FacilitySemanticPreset.Production => "Production",
            FacilitySemanticPreset.Distribution => "Distribution",
            FacilitySemanticPreset.Servers => "Servers",
            FacilitySemanticPreset.ProductionAndHvac => "Production + HVAC",
            _ => preset.ToString()
        };
    }

    public static FacilityNodeRole ResolveRole(string? nodeKey, string? nodeType = null, string? label = null)
    {
        if (IsWeatherContextNode(nodeKey))
        {
            return FacilityNodeRole.OtherUnclassified;
        }

        if (!string.IsNullOrWhiteSpace(nodeKey) && NodeKeyRoleOverrides.TryGetValue(nodeKey, out var keyRole))
        {
            return keyRole;
        }

        if (!string.IsNullOrWhiteSpace(nodeType) && NodeTypeRoleMap.TryGetValue(nodeType.Trim(), out var typeRole))
        {
            return typeRole;
        }

        var combined = $"{nodeKey} {nodeType} {label}".ToLowerInvariant();

        if (ContainsAnyToken(combined, GridTransformerTokens))
        {
            return FacilityNodeRole.GridTransformer;
        }

        if (ContainsAnyToken(combined, EnergyProductionTokens))
        {
            return FacilityNodeRole.EnergyProduction;
        }

        if (ContainsAnyToken(combined, HvacTokens))
        {
            return FacilityNodeRole.HvacCoolingVentilation;
        }

        if (ContainsAnyToken(combined, TestingTokens))
        {
            return FacilityNodeRole.TestingSimulation;
        }

        if (ContainsAnyToken(combined, DistributionTokens))
        {
            return FacilityNodeRole.PowerDistribution;
        }

        if (ContainsAnyToken(combined, ServersTokens))
        {
            return FacilityNodeRole.ServersItCooling;
        }

        return FacilityNodeRole.OtherUnclassified;
    }

    public static string GetRoleLabel(FacilityNodeRole role)
    {
        return role switch
        {
            FacilityNodeRole.GridTransformer => "Grid / Transformer",
            FacilityNodeRole.EnergyProduction => "Energy Production",
            FacilityNodeRole.HvacCoolingVentilation => "HVAC / Cooling / Ventilation",
            FacilityNodeRole.TestingSimulation => "Testing / Simulation",
            FacilityNodeRole.PowerDistribution => "Power Distribution",
            FacilityNodeRole.ServersItCooling => "Servers / IT Cooling",
            _ => "Other / Unclassified"
        };
    }

    public static bool IsWeatherContextNode(string? nodeKey)
    {
        return string.Equals(nodeKey, "weather_main", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TagMatches(string tag, string requiredTag)
    {
        if (tag.Equals(requiredTag, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !requiredTag.Contains(':')
            && tag.EndsWith($":{requiredTag}", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToTagSlug(object value)
    {
        return ToTagSlug(value.ToString());
    }

    private static string ToTagSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var lowered = value.Trim().ToLowerInvariant();
        var buffer = new char[lowered.Length];
        var index = 0;
        var previousWasHyphen = false;

        foreach (var ch in lowered)
        {
            if (char.IsLetterOrDigit(ch))
            {
                buffer[index++] = ch;
                previousWasHyphen = false;
                continue;
            }

            if (ch is '-' or ':' or '_')
            {
                buffer[index++] = ch == '_' ? '-' : ch;
                previousWasHyphen = false;
                continue;
            }

            if (char.IsWhiteSpace(ch) && !previousWasHyphen)
            {
                buffer[index++] = '-';
                previousWasHyphen = true;
            }
        }

        return new string(buffer, 0, index).Trim('-');
    }

    private static bool ContainsAnyToken(string text, IReadOnlyList<string> tokens)
    {
        foreach (var token in tokens)
        {
            if (text.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
