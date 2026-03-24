namespace DiplomovaPrace.Services;

using DiplomovaPrace.Models;

/// <summary>
/// Statická továrna pro vytvoření demo budovy s kompletní strukturou a SVG souřadnicemi.
/// Slouží jako jediný zdroj pravdy pro konfiguraci budovy.
/// SVG souřadnice odpovídají viewBox 0 0 800 300.
/// </summary>
public static class BuildingConfiguration
{
    public static Building CreateDemoBuilding()
    {
        return new Building(
            Id: "building-main",
            Name: "Budova Mechatroniky",
            Floors:
            [
                CreateGroundFloor(),
                CreateFirstFloor()
            ]
        );
    }

    private static Floor CreateGroundFloor()
    {
        return new Floor(
            Id: "floor-0",
            Name: "Přízemí",
            Level: 0,
            BuildingId: "building-main",
            Rooms:
            [
                new Room("room-001", "Vstupní hala", "floor-0",
                    new RoomGeometry(50, 50, 220, 180),
                    [
                        D("dev-001-temp",   "Teploměr",        DeviceType.TemperatureSensor, "room-001", new DevicePosition(100, 100)),
                        D("dev-001-light",  "Stropní světlo",  DeviceType.Light,             "room-001", new DevicePosition(180, 100)),
                        D("dev-001-motion", "Pohybový senzor", DeviceType.MotionSensor,      "room-001", new DevicePosition(140, 170)),
                    ]),
                new Room("room-002", "Serverovna", "floor-0",
                    new RoomGeometry(290, 50, 220, 180),
                    [
                        D("dev-002-temp",  "Teploměr",      DeviceType.TemperatureSensor, "room-002", new DevicePosition(340, 100)),
                        D("dev-002-humid", "Vlhkoměr",      DeviceType.HumiditySensor,    "room-002", new DevicePosition(420, 100)),
                        D("dev-002-door",  "Dveřní senzor", DeviceType.DoorSensor,        "room-002", new DevicePosition(380, 170)),
                    ]),
                new Room("room-003", "Kancelář", "floor-0",
                    new RoomGeometry(530, 50, 220, 180),
                    [
                        D("dev-003-temp",  "Teploměr",       DeviceType.TemperatureSensor, "room-003", new DevicePosition(580, 100)),
                        D("dev-003-light", "Stropní světlo", DeviceType.Light,             "room-003", new DevicePosition(660, 100)),
                        D("dev-003-hvac",  "Klimatizace",    DeviceType.HVAC,              "room-003", new DevicePosition(620, 170)),
                    ]),
            ]
        );
    }

    private static Floor CreateFirstFloor()
    {
        return new Floor(
            Id: "floor-1",
            Name: "1. patro",
            Level: 1,
            BuildingId: "building-main",
            Rooms:
            [
                new Room("room-101", "Přednášková místnost", "floor-1",
                    new RoomGeometry(50, 50, 340, 180),
                    [
                        D("dev-101-temp",   "Teploměr",        DeviceType.TemperatureSensor, "room-101", new DevicePosition(120, 100)),
                        D("dev-101-light",  "Stropní světlo",  DeviceType.Light,             "room-101", new DevicePosition(220, 100)),
                        D("dev-101-hvac",   "Klimatizace",     DeviceType.HVAC,              "room-101", new DevicePosition(320, 100)),
                        D("dev-101-motion", "Pohybový senzor", DeviceType.MotionSensor,      "room-101", new DevicePosition(220, 170)),
                    ]),
                new Room("room-102", "Počítačová učebna", "floor-1",
                    new RoomGeometry(410, 50, 170, 180),
                    [
                        D("dev-102-temp",  "Teploměr",       DeviceType.TemperatureSensor, "room-102", new DevicePosition(460, 100)),
                        D("dev-102-humid", "Vlhkoměr",       DeviceType.HumiditySensor,    "room-102", new DevicePosition(520, 100)),
                        D("dev-102-light", "Stropní světlo", DeviceType.Light,             "room-102", new DevicePosition(490, 170)),
                    ]),
                new Room("room-103", "Zasedací místnost", "floor-1",
                    new RoomGeometry(600, 50, 150, 180),
                    [
                        D("dev-103-temp",   "Teploměr",        DeviceType.TemperatureSensor, "room-103", new DevicePosition(640, 100)),
                        D("dev-103-light",  "Stropní světlo",  DeviceType.Light,             "room-103", new DevicePosition(700, 100)),
                        D("dev-103-motion", "Pohybový senzor", DeviceType.MotionSensor,      "room-103", new DevicePosition(670, 170)),
                    ]),
            ]
        );
    }

    /// <summary>Vytvoří Device s výchozím příkonem odvozeným z typu.</summary>
    private static Device D(string id, string name, DeviceType type, string roomId, DevicePosition pos) =>
        new(id, name, type, roomId, pos, Device.DefaultConsumption(type));
}
