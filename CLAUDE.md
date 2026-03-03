# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Run

```bash
dotnet build                          # compile
dotnet run --project DiplomovaPrace   # run (http://localhost:5016)
```

No test projects exist yet. No external NuGet packages — only the shared ASP.NET Core 10.0 framework.

## Architecture

Blazor Server application (.NET 10) for interactive SVG-based building visualization with simulated sensor data. Three-layer architecture with unidirectional dependencies:

```
Models (C# records, immutable) → Services (state + simulation) → Components (Blazor Razor UI)
```

### Data flow

`SimulationService` (IHostedService, PeriodicTimer 2s, seed=42) generates device states via random walk → writes to `BuildingStateService` (singleton, `ConcurrentDictionary`) → fires `Action? OnStateChanged` event → Blazor components call `await InvokeAsync(StateHasChanged)` → SVG attributes re-render via SignalR diff.

### Domain model

`Building` → `Floor` → `Room` → `Device` → `DeviceState`. All are immutable C# records. Device state is stored separately in `BuildingStateService`, not on the `Device` record. Six device types: TemperatureSensor, HumiditySensor, Light, HVAC, MotionSensor, DoorSensor.

### Service registration (Program.cs)

`BuildingStateService` is singleton (state container with Observer pattern). `SimulationService` is registered as both singleton and `IHostedService` using the forward-resolution pattern (`sp.GetRequiredService<SimulationService>()`).

### Key patterns

- **Observer (GoF):** `IBuildingStateService.OnStateChanged` event. Components subscribe in `OnInitialized`, unsubscribe in `Dispose`.
- **Thread safety:** `ConcurrentDictionary` for state storage. `InvokeAsync(StateHasChanged)` marshals to Blazor circuit sync context. `async void` handler is intentional for event delegates.
- **Batch notification:** `NotifyStateChanged()` fires once per simulation tick after all devices are updated.

## SVG in Blazor — critical caveat

Razor treats `<text>` as its own directive, not as an SVG element. All SVG `<text>` elements must use `@((MarkupString)$"<text ...>{value}</text>")` workaround. This applies to FloorPlan.razor, RoomShape.razor, and DeviceIcon.razor.

## Key directories

- `Models/` — domain records (Building, Floor, Room, Device, DeviceState, DeviceType)
- `Services/` — IBuildingStateService, SimulationService, BuildingConfiguration (demo building factory), StateColorMapper (state→SVG color)
- `Components/Building/` — SVG visualization components (BuildingViewer subscribes to state, FloorPlan renders `<svg viewBox="0 0 800 300">`, RoomShape renders `<rect>`, DeviceIcon renders `<circle>`)
- `Components/Pages/BuildingView.razor` — main page at `/building` with `@rendermode InteractiveServer`
- `Docs/Architecture.md` — academic justification text in Czech

## Extension points

Adding a new device type: add to `DeviceType` enum → add generation case in `SimulationService.GenerateDeviceState` → add color case in `StateColorMapper.GetDeviceColor` → add symbol in `DeviceIcon` → add devices in `BuildingConfiguration`.

Replacing simulation with real data: implement `IBuildingStateService` with a real data source (MQTT, OPC UA). No UI changes needed.

## Language

This is a Czech diploma thesis project (TUL). Code identifiers are in English, UI labels and documentation in Czech.
