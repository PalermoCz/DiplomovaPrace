using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiplomovaPrace.Persistence;

/// <summary>
/// EF Core persistence entita pro záznam elektrického měření.
///
/// Záměrně oddělená od doménového MeasurementRecord (record type):
///   - EF Core potřebuje mutable třídu s bezparametrickým konstruktorem
///   - doménový record je neměnný a vhodný pro transfer dat
///   - mapování probíhá v MeasurementRecordMapper
///
/// Indexy:
///   - (DeviceId, Timestamp) — nejčastější dotaz: "historie pro zařízení v čase"
///   - Timestamp DESC — cleanup starých dat, stránkování
/// </summary>
[Table("MeasurementRecords")]
public class MeasurementRecordEntity
{
    /// <summary>Surrogate PK — automaticky generovaný SQLite ROWID.</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>ID měřicího bodu — odpovídá Device.Id z doménového modelu.</summary>
    [Required]
    [MaxLength(100)]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Čas měření — ukládán jako UTC.</summary>
    public DateTime Timestamp { get; set; }

    // ── Energie (kumulativní) ─────────────────────────────────────────────

    /// <summary>Činná energie kumulativní (kWh). Null pokud měřicí bod neměří energii.</summary>
    public double? ActiveEnergyKWh { get; set; }

    /// <summary>Jalová energie kumulativní (kVArh).</summary>
    public double? ReactiveEnergyKVArh { get; set; }

    // ── Výkon (okamžitý) ──────────────────────────────────────────────────

    /// <summary>Činný výkon (kW).</summary>
    public double? ActivePowerKW { get; set; }

    /// <summary>Jalový výkon (kVAr).</summary>
    public double? ReactivePowerKVAr { get; set; }

    /// <summary>Zdánlivý výkon (kVA).</summary>
    public double? ApparentPowerKVA { get; set; }

    // ── Síťové veličiny (okamžité) ────────────────────────────────────────

    /// <summary>Napětí (V).</summary>
    public double? VoltageV { get; set; }

    /// <summary>Proud (A).</summary>
    public double? CurrentA { get; set; }

    /// <summary>Účiník / Power Factor (0.0–1.0).</summary>
    public double? PowerFactor { get; set; }

    /// <summary>Frekvence sítě (Hz).</summary>
    public double? FrequencyHz { get; set; }
}
