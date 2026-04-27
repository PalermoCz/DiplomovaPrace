using DiplomovaPrace.Persistence.Schematic;

namespace DiplomovaPrace.Services;

public sealed record FacilityWeatherSourceResolution(
    SchematicNodeEntity Node,
    FacilityDataBindingRegistry.BindingRecord? TemperatureBinding,
    bool UsesLegacyNodeKeyFallback)
{
    public string NodeKey => Node.NodeKey;
    public string? NodeType => Node.NodeType;
    public bool HasTemperatureBinding => TemperatureBinding is not null;
}

public sealed class FacilityWeatherSourceResolver
{
    private readonly FacilityDataBindingRegistry _bindingRegistry;

    public FacilityWeatherSourceResolver(FacilityDataBindingRegistry bindingRegistry)
    {
        _bindingRegistry = bindingRegistry;
    }

    public FacilityWeatherSourceResolution? Resolve(FacilityEntity? facility)
        => facility is null ? null : Resolve(facility.Nodes);

    public FacilityWeatherSourceResolution? Resolve(IEnumerable<SchematicNodeEntity>? nodes)
    {
        if (nodes is null)
        {
            return null;
        }

        var weatherNode = nodes.FirstOrDefault(node => FacilityBuiltInNodeTypes.IsWeather(node.NodeType))
            ?? nodes.FirstOrDefault(node => FacilityBuiltInNodeTypes.IsLegacyWeatherNodeKey(node.NodeKey));

        if (weatherNode is null)
        {
            return null;
        }

        var temperatureBinding = _bindingRegistry.GetPreferredBinding(weatherNode.NodeKey, FacilitySignalCode.Ta)
            ?? _bindingRegistry.GetPreferredBinding(weatherNode.NodeKey, FacilitySignalFamily.WeatherTemperature);

        return new FacilityWeatherSourceResolution(
            weatherNode,
            temperatureBinding,
            FacilityBuiltInNodeTypes.IsLegacyWeatherNodeKey(weatherNode.NodeKey));
    }
}