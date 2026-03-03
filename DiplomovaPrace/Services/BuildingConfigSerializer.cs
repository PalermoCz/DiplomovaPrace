namespace DiplomovaPrace.Services;

using System.Text.Json;
using System.Text.Json.Serialization;
using DiplomovaPrace.Models.Configuration;
using DiplomovaPrace.Models;

/// <summary>
/// Serializace / deserializace BuildingConfig do/ze JSON.
/// Používá System.Text.Json (bez NuGet). Pro export na disk a import zpět.
/// </summary>
public static class BuildingConfigSerializer
{
    private static readonly JsonSerializerOptions _opts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>Serializuje konfiguraci do JSON stringu.</summary>
    public static string Serialize(BuildingConfig config) =>
        JsonSerializer.Serialize(config, _opts);

    /// <summary>
    /// Deserializuje konfiguraci z JSON stringu.
    /// Vrací null při chybě (neplatný JSON nebo neplatná struktura).
    /// </summary>
    public static BuildingConfig? Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<BuildingConfig>(json, _opts);
        }
        catch
        {
            return null;
        }
    }
}
