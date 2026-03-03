namespace DiplomovaPrace.Models.Configuration;

public enum EditorTool
{
    Select,
    AddRoom,
    AddDevice,
    Delete
}

public enum EditorSelectionType
{
    None,
    Building,
    Floor,
    Room,
    Device
}

/// <summary>Stav publikace konfigurace do runtime vizualizace.</summary>
public enum PublicationState
{
    /// <summary>Konfigurace dosud nebyla publikována (nebo session je nová).</summary>
    Draft,
    /// <summary>Konfigurace byla upravena od posledního publikování.</summary>
    Modified,
    /// <summary>Aktuální konfigurace je shodná s tím, co bylo naposledy publikováno.</summary>
    Published
}
