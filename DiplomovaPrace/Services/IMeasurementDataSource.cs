namespace DiplomovaPrace.Services;

using DiplomovaPrace.Models;

/// <summary>
/// Abstrakce datového zdroje měření — umožňuje v budoucnu nahradit
/// simulaci za reálný import (CSV, REST API, MQTT, databáze).
///
/// Fáze 1: SimulationService implementuje generování dat přímo
///         do IBuildingStateService. V dalších fázích se vytvoří
///         konkrétní implementace (CsvMeasurementSource, ApiMeasurementSource)
///         a data budou proudit přes toto rozhraní.
///
/// Pattern: Separation of Concerns — odděluje zdroj dat od úložiště.
/// </summary>
public interface IMeasurementDataSource
{
    /// <summary>
    /// Získá měření pro daný měřicí bod v časovém rozsahu.
    /// Vrací prázdný seznam pokud nejsou data k dispozici.
    /// </summary>
    IReadOnlyList<MeasurementRecord> GetMeasurements(
        string deviceId, DateTime from, DateTime to);

    /// <summary>
    /// Získá poslední měření pro daný měřicí bod.
    /// Vrací null pokud neexistuje žádné měření.
    /// </summary>
    MeasurementRecord? GetLatestMeasurement(string deviceId);

    /// <summary>
    /// Vrací ID všech měřicích bodů, pro které jsou dostupná data.
    /// </summary>
    IReadOnlyList<string> GetAvailableDeviceIds();
}
