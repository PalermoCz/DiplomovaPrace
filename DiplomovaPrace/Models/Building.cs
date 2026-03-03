namespace DiplomovaPrace.Models;

/// <summary>
/// Agregátní kořen doménového modelu budovy.
/// </summary>
public record Building(
    string Id,
    string Name,
    IReadOnlyList<Floor> Floors
);
