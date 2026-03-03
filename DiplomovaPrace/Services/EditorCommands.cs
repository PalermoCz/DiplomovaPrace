namespace DiplomovaPrace.Services;

/// <summary>Konkrétní deskriptory příkazů editoru. Každý nese jen popis akce.</summary>

public record AddRoomCommand(string RoomName) : IEditorCommand
{
    public string Description => $"Přidat místnost \"{RoomName}\"";
}

public record DeleteRoomCommand(string RoomName) : IEditorCommand
{
    public string Description => $"Smazat místnost \"{RoomName}\"";
}

public record UpdateRoomCommand(string RoomName) : IEditorCommand
{
    public string Description => $"Upravit místnost \"{RoomName}\"";
}

public record AddDeviceCommand(string DeviceName) : IEditorCommand
{
    public string Description => $"Přidat zařízení \"{DeviceName}\"";
}

public record DeleteDeviceCommand(string DeviceName) : IEditorCommand
{
    public string Description => $"Smazat zařízení \"{DeviceName}\"";
}

public record MoveDeviceCommand(string DeviceName) : IEditorCommand
{
    public string Description => $"Přesunout zařízení \"{DeviceName}\"";
}

public record UpdateDeviceCommand(string DeviceName) : IEditorCommand
{
    public string Description => $"Upravit zařízení \"{DeviceName}\"";
}

public record AddFloorCommand(string FloorName) : IEditorCommand
{
    public string Description => $"Přidat patro \"{FloorName}\"";
}

public record DeleteFloorCommand(string FloorName) : IEditorCommand
{
    public string Description => $"Smazat patro \"{FloorName}\"";
}

public record UpdateFloorCommand(string FloorName) : IEditorCommand
{
    public string Description => $"Upravit patro \"{FloorName}\"";
}

public record UpdateBuildingCommand(string BuildingName) : IEditorCommand
{
    public string Description => $"Upravit budovu \"{BuildingName}\"";
}

public record ImportConfigCommand : IEditorCommand
{
    public string Description => "Import konfigurace";
}
