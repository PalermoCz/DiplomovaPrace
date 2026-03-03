# Architektura konfigurační editoru budov

*Součást diplomové práce — Interaktivní vizualizace IoT senzorů budovy*
*Technologická univerzita v Liberci, 2025*

---

## 1. Přehled

Editor konfigurací budov (`/editor`) umožňuje uživateli interaktivně sestavit
konfiguraci budovy na SVG canvasu a následně ji publikovat do runtime vizualizace (`/building`).

Klíčové požadavky:
- **Oddělení domén** — konfigurační model je nezávislý na runtime stavovém modelu
- **Připravenost na databázi** — audit pole, soft delete, `RowVersion` pro optimistickou konkurenci
- **Undo/Redo** — snapshot-based command pattern, max 50 kroků
- **Validace** — překryvy místností, zařízení mimo místnost, duplicitní názvy
- **Import/Export** — JSON serializace bez externích NuGet balíčků

---

## 2. Architektonická hranice

```
EDITOR DOMÉNA                              VIZUALIZAČNÍ DOMÉNA
─────────────────────────────────────────────────────────────
IBuildingConfigurationService              IBuildingStateService
IEditorSessionService                      SimulationService
EditorView, EditorCanvas, ...              BuildingViewer, FloorPlan, ...
Models/Configuration/*                    Models/* (Building, Floor, ...)
          │                                         │
          │  ToBuildingDomainModel()                │
          └────────────────────────────────────────►│
                    ReplaceBuilding()               │
          ◄────────────────────────────────────────┘
```

Dvě přechodové operace, žádná jiná provázanost mezi doménami.

---

## 3. Konfigurační modely (`Models/Configuration/`)

Všechny typy jsou **immutable C# records** s `with`-expressions pro aktualizace.

| Typ | Popis |
|-----|-------|
| `BuildingConfig` | Kořen agregátu; drží `IReadOnlyList<FloorConfig>` |
| `FloorConfig` | Patro s vlastním `ViewBoxWidth/Height` pro per-floor canvas |
| `RoomConfig` | Místnost s geometrií (`RoomGeometry`) a zařízeními |
| `DeviceConfig` | Zařízení s polohou (`DevicePosition`), typem a nastavením zobrazení |
| `DeviceDisplaySettings` | `OwnsOne<>` pro EF Core; jednotka, prahové hodnoty alarmu |
| `ValidationIssue` | Výsledek validace: `Message`, `IssueType`, `RoomId?`, `DeviceId?` |

**Audit pole** (každý `*Config` record):
- `CreatedAt`, `UpdatedAt` — pro EF Core `HasDefaultValueSql("GETUTCDATE()")`
- `CreatedBy`, `UpdatedBy` — pro multi-user audit
- `RowVersion byte[]` — EF Core `[Timestamp]`, optimistická konkurence
- `IsDeleted bool` — soft delete, EF Core global query filter

**Sdílené value objects** (`Models/`): `RoomGeometry`, `DevicePosition` — beze změny.

---

## 4. Služby

### 4.1 `IBuildingConfigurationService` (Singleton)

CRUD nad konfigurační doménou. Aktuální implementace: `InMemoryBuildingConfigurationService`.

**Mutační vzor** (thread-safe):
```csharp
// CAS loop — atomická záměna BuldingConfig v ConcurrentDictionary
while (true) {
    var current = _store[id];
    var updated = mutate(current);          // with-expression
    if (_store.TryUpdate(id, updated, current)) { Notify(); return updated; }
    // jiné vlákno změnilo záznam — opakuj
}
```

Klíčové operace:
- `ReplaceConfigAsync(config)` — bulk nahrazení (pro import a undo/redo)
- `ToBuildingDomainModel(config)` — konverze do vizualizační domény

**Příprava na EF Core**: swap implementace v DI bez změny rozhraní.

### 4.2 `IEditorSessionService` (Scoped — per-circuit)

Přechodný stav editoru pro jednu Blazor circuit (záložku prohlížeče).

**Proč Scoped?** Každá záložka prohlížeče v Blazor Server dostane vlastní circuit.
Singleton by sdílel vybraný prvek a nástroj mezi různými sezeními.

Stav:
- `ActiveTool`, `SelectedElementId/Type`, `ActiveFloorId`
- `HasUnsavedChanges`, `PublicationState`
- Undo/Redo zásobníky: `List<(BuildingConfig Snapshot, string Description)>`

### 4.3 `BuildingValidator` (statická třída)

Validuje `FloorConfig` bez závislostí:

| Pravidlo | Závažnost |
|----------|-----------|
| Překrývající se místnosti | Error |
| Duplicitní název místnosti | Error |
| Zařízení mimo geometrii místnosti | Warning |
| Duplicitní název zařízení v místnosti | Warning |

Pravidla `Error` blokují publikaci do vizualizace.

### 4.4 `BuildingConfigSerializer` (statická třída)

`System.Text.Json` serializace/deserializace `BuildingConfig` → JSON.
- `Serialize(config)` → `string`
- `Deserialize(json)` → `BuildingConfig?` (null při chybě)

---

## 5. Undo/Redo: Command Pattern se snapshoty

Implementace: **snapshot-based memento** — ukládáme celou `BuildingConfig` před každou mutací.

```
ExecuteCommandAsync(command, action):
  1. snapshot = CurrentConfig        ← uložíme PŘED akcí
  2. _undoStack.Push((snapshot, cmd.Description))
  3. _redoStack.Clear()
  4. await action()                  ← provedeme mutaci
  5. await configService.GetBuildingAsync(id) → RefreshConfig()
  6. MarkDirty()
```

```
UndoAsync():
  1. entry = _undoStack.Pop()
  2. _redoStack.Push((CurrentConfig, entry.Description))
  3. await configService.ReplaceConfigAsync(entry.Snapshot)
  4. RefreshConfig(entry.Snapshot) → MarkDirty()
```

**Výhody**: jednoduchost, žádná implementační logika v příkazech.
**Nevýhody**: paměťová náročnost při velkých konfiguracích (mitigováno: max 50 kroků, records sdílejí nezměněné části díky reference semantice).

**`IEditorCommand`** je čistý deskriptor (jen `Description`), ne Command s `Execute/Undo`.

---

## 6. SVG Canvas a JS Interop

### 6.1 Pan/Zoom

JS modul `editorCanvas` (IIFE) drží `_transform = { tx, ty, scale }`.
Transformace se aplikuje na `<g id="svgId-content">` jako CSS `transform="translate(tx ty) scale(s)"`.

**Klíčový invariant**: Blazor nikdy nevypisuje atribut `transform` na content group,
takže JS-nastavená transformace přežije Blazor re-rendery.

### 6.2 Souřadnicová konverze

```javascript
function clientToContent(clientX, clientY) {
    var svgPt = pt.matrixTransform(svg.getScreenCTM().inverse());
    return {
        x: (svgPt.x - _transform.tx) / _transform.scale,
        y: (svgPt.y - _transform.ty) / _transform.scale
    };
}
```

Tato funkce správně zohledňuje SVG viewBox, CSS scaling i pan/zoom transform.

### 6.3 JS → Blazor komunikace (`[JSInvokable]`)

| Metoda | Spouštěč |
|--------|----------|
| `JsOnRoomDrawn(x,y,w,h)` | Konec rubber-band kreslení (min 20×20 px) |
| `JsOnDeviceDragEnd(id,x,y)` | Puštění zařízení po tahu |
| `JsOnElementClicked(id,type)` | Klik v Select nástroji |
| `JsOnCanvasClicked(x,y)` | Klik v AddDevice nástroji |
| `JsOnDeleteClicked(id,type)` | Klik v Delete nástroji |
| `JsOnKeyboardShortcut(shortcut)` | Ctrl+Z (→ "undo"), Ctrl+Y/Shift+Z (→ "redo") |

### 6.4 Globální JS utility

- `window.downloadTextFile(filename, content)` — stáhneme JSON export
- `window.triggerClick(elementId)` — programatická aktivace skrytého `<input type="file">`

---

## 7. Validační vizualizace

**EditorCanvas**: barva ohraničení místností a zařízení se odvíjí od validačního stavu:

| Stav | Barva ohraničení | Výplň |
|------|-----------------|-------|
| Vybrán | `#2980b9` (modrá) | `#dbeafe` |
| Chyba (Error) | `#dc2626` (červená) | `#fee2e2` |
| Varování (Warning) | `#d97706` (žlutá) | `#fffbeb` |
| Normální | `#78909c` (šedá) | `#f0f4f8` |

**EditorPropertiesPanel**: validační zprávy se zobrazí nad formulářem vybraného prvku.

**EditorToolbar**: tlačítko „Použít ve vizualizaci" je zakázáno dokud existují chyby.

---

## 8. Datové toky

### Import konfigurace
```
User vybere .json soubor
  → HandleImportFileSelected → BuildingConfigSerializer.Deserialize()
  → SessionService.ExecuteCommandAsync(ImportConfigCommand,
      () => ConfigService.ReplaceConfigAsync(config))
  → SessionService.LoadConfig(config)   ← vymaže undo historii
```

### Export konfigurace
```
User klikne Export
  → BuildingConfigSerializer.Serialize(CurrentConfig) → JSON string
  → window.downloadTextFile(filename, json)            ← trigger download
```

### Publikace do vizualizace
```
User klikne „Použít ve vizualizaci"
  → BuildingValidator.ValidateFloor() — kontrola chyb
  → ConfigService.ToBuildingDomainModel(config) → Building
  → StateService.ReplaceBuilding(building)
      → _building = building; _deviceStates.Clear(); InitializeDefaultStates();
      → NotifyStateChanged()
  → SessionService.MarkPublished()
  → SimulationService pokračuje nad novou strukturou automaticky
```

---

## 9. Ochrana dat (UX)

**Navigation guard**: `NavigationManager.RegisterLocationChangingHandler` — při neuložených změnách
vyvolá `window.confirm()` a zabrání navigaci pokud uživatel potvrzení odmítne.

**Stavový indikátor** (EditorToolbar badge):

| Stav | Badge |
|------|-------|
| `PublicationState.Draft` + bez změn | ⊘ Návrh (šedá) |
| `HasUnsavedChanges` | ● Neuloženo (žlutá) |
| `PublicationState.Modified` | ● Změněno (žlutá) |
| `PublicationState.Published` | ● Publikováno (zelená) |

---

## 10. Extension points

**Persistentní úložiště**: swap `InMemoryBuildingConfigurationService` → `EfCoreBuildingConfigurationService` v DI,
bez jakékoliv změny rozhraní nebo UI kódu.

**Multi-user**: SignalR hub broadcastuje `OnConfigurationChanged` všem sessions sledujícím stejnou budovu;
`RowVersion` v `BuildingConfig` zajistí detekci konfliktů v EF Core (optimistická konkurence).

**Konflikty undo v multi-user**: při přijetí externího `OnConfigurationChanged` vymazat undo zásobník
(konzistentní stav je vždy ten z úložiště).
