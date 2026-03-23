namespace DiplomovaPrace.Models.Configuration;

/// <summary>
/// Podmíněné formátování — barva aplikovaná když je splněna podmínka.
/// Pravidla vyhodnocena v pořadí, první match vyhrává.
/// </summary>
public record DisplayRule(
    string Condition,  // Např. "Room[\"Lab\"].Value > 30"
    string Color       // Hex formát: #RRGGBB nebo #RRGGBBAA
);
