# Data Visualization Audit

This audit is based on direct inspection of the current source files listed in section 8. It describes only behavior that is implemented in code now. It does not propose redesigns.

## 1. Executive Map Of Current Surfaces

### Current primary facility-centric implementation

The primary, current product surface is `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor` on routes `/` and `/facility`. It explicitly uses `DiplomovaPrace.Components.Layout.FacilityLayout`, not the default legacy layout. This page combines:

- the main facility schematic canvas,
- interval selection,
- weather context for supported focus nodes,
- selection summary,
- semantic-selection and working-set workflow tools,
- selection-level aggregate analytics,
- focus-node detail widgets,
- compare-set management,
- forecast/baseline/deviation outputs.

The facility workbench shell also includes `DiplomovaPrace/Components/Layout/FacilityTopbar.razor`, which displays global alert counts and an alert list for high-level energy nodes.

### Legacy but still implemented and still user-visible

The default router in `DiplomovaPrace/Components/Routes.razor` uses `MainLayout` unless a page overrides it. The legacy navigation in `DiplomovaPrace/Components/Layout/NavMenu.razor` still exposes these pages to users:

- `/dashboard` -> `DiplomovaPrace/Components/Pages/DashboardView.razor`
- `/kpi` -> `DiplomovaPrace/Components/Pages/KpiView.razor`
- `/import` -> `DiplomovaPrace/Components/Pages/ImportView.razor`
- `/editor` -> `DiplomovaPrace/Components/Pages/EditorView.razor`

These pages are not dormant code. They are still routed and linked from the current legacy navigation.

### Legacy but still routable, not linked in current nav

`DiplomovaPrace/Components/Pages/BuildingView.razor` is still implemented on `/building`. It hosts the older building-centric runtime visualization stack (`BuildingViewer`, `FloorPlan`, `RoomShape`, `DeviceIcon`, `DeviceDetailPanel`, `FloorSummaryPanel`, `RoomSummaryPanel`, `ExpressionPanel`). It is not present in `NavMenu.razor`, but the route still exists.

### Important separation

There are two different active visualization/data-display architectures in the app:

- current facility-first schematic/analytics flow: `FacilityWorkbench.razor` + `FacilitySchematicV2.razor` + `NodeAnalyticsPreviewService.cs`
- legacy building/device-centric flow: `DashboardView.razor`, `KpiView.razor`, `BuildingView.razor`, `BuildingViewer.razor`, `KpiService.cs`, `BaselineService.cs`, `BuildingStateService.cs`, `SimulationService.cs`

The current product direction is clearly the facility-first workbench, but the legacy surfaces are still implemented and in several cases still exposed to end users.

## 2. Selected / Single-Node Data Display Behavior

### 2.1 Current facility workbench focus behavior

Single-node focus in the facility workbench is controlled by `HandleFacilityNodeClicked` in `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor` and the shared selected element stored in `IEditorSessionService`.

Verified behavior:

- Clicking a leaf or analytic node toggles that node in `_selectionSet` and usually makes it the focused node.
- Clicking a structural node, or a node with traversable outgoing explicit relationships (`AdditionalLink`, `Membership`, `Semantic`), does not behave as a pure single-node selection. It toggles an expanded traversal selection for the subtree/context while still keeping one focus node.
- The selected/focused node is therefore not always the same thing as the full analytics selection set.
- Global node search in `FacilityWorkbench.razor` does not select a node. `FocusNodeFromSearchAsync` only pans the canvas to the node via `editorCanvas.panToNode`. Search results also exclude weather-context nodes.

### 2.2 Current focus-node data shown for curated facility nodes

When the focused node is treated as curated (`_isCuratedNode`), the workbench renders these focus-specific widgets in `FacilityWorkbench.razor`:

- `Deviation / Baseline Detail`
  - Uses `_curatedDeviationData` from `NodeAnalyticsPreviewService.GetCuratedDeviationSummaryAsync`.
  - Shows severity badge, reference interval count, current value, baseline value, absolute delta, and percent delta.
- `Alert Detail`
  - Uses the same deviation output to derive alert status text, alert badge, reason text, and the schematic color hint token.
- `Focus Metadata`
  - Shows label, node key, node type, resolved semantic role, and optional zone.
- `Compare Preview`
  - Shown only when `ShouldShowComparePreview` is true.
  - Uses `FacilityCompareTimeSeriesPanel` and `_curatedCompareTimeSeriesData`.

Time/history support for curated focus nodes:

- `RefreshPresetAnchorAsync` anchors presets to `NodeAnalyticsPreviewService.GetCuratedNodeMaxTimestampUtcAsync(nodeKey)`.
- Custom interval validation checks the selected curated node's actual data domain via `GetCuratedNodeTimeDomainUtcAsync`.
- Weather explanation is shown inside the interval section only for `heating_main` and `cooling_main` when `_curatedDeviationData.WeatherExplanation.IsAvailable` is true.

### 2.3 Current focus-node data shown for measurable non-curated nodes

When the focused facility node is not curated but has `MeterUrn`, `FacilityWorkbench.razor` renders:

- a `Měřitelný uzel` card with the node's `MeterUrn`
- a `KPI Přehled` card with:
  - total consumption in kWh
  - average power in kW

That data comes from `NodeAnalyticsPreviewService.GetPreviewDataAsync`, which in turn uses the legacy KPI/repository path.

No dedicated single-node chart is rendered for these non-curated measurable nodes in the current workbench.

### 2.4 Current focus-node data shown for structural nodes

When the focused node has no `MeterUrn` and is not treated as curated, `FacilityWorkbench.razor` renders only a structural-node message:

- `Strukturální uzel`
- explanation that the node is acting as a selector and that main analytics run over the aggregate selection set

There is no direct per-node numeric summary for pure structural nodes.

### 2.5 Important current single-node nuance

Even when exactly one node is selected, the main charts and headline KPI are not separate focus-only widgets. They are rendered from `_selectionAggregateOverview`, which represents the current selection set. If the selection set contains one supported node, the aggregate widgets effectively become single-node analytics, but they are still implemented through the selection-aggregate path.

### 2.6 Legacy editor single-node preview

`DiplomovaPrace/Components/Pages/EditorView.razor` contains a separate, smaller selected-node analytics preview for facility nodes in the legacy editor route `/editor`.

Verified behavior:

- If the selected facility node key is one of `pv_main`, `chp_main`, `cooling_main`, `heating_main`, `weather_main`, the editor loads `AnalyticsPreview.GetCuratedSummaryAsync`.
- Otherwise, if the selected node has `MeterUrn`, the editor loads `AnalyticsPreview.GetPreviewDataAsync(meterUrn, from, to)`.
- The interval is hardcoded to `2020-01-01 UTC` through `2021-01-01 UTC`.
- The preview renders summary cards only. It does not render a chart.

### 2.7 Legacy building-view single-device detail

In the old building-centric view (`BuildingView.razor` -> `BuildingViewer.razor`), clicking a device opens `DeviceDetailPanel.razor`.

Verified behavior:

- Shows device id, type, room id, current numeric value if available, active/inactive badge, alarm badge, and last update time.
- For numeric device types only, also renders a Radzen line chart named `Historie hodnot`.
- That chart reads from `IBuildingStateService.GetDeviceHistory(deviceId)` and therefore uses the in-memory 100-sample ring buffer in `BuildingStateService.cs`.

## 3. Multi-Selection / Subtree / Group Data Display Behavior

### 3.1 Current facility workbench selection semantics

The workbench has a distinct notion of selection set, focus node, and visual highlight.

Verified behavior from `FacilityWorkbench.razor`:

- `_selectionSet` stores the effective selected node keys.
- `_selectionOrder` preserves ordering and fallback focus behavior.
- `Selection` summary shows selected-node count, visual-highlight count, and context-only highlight count.
- Weather nodes are explicitly excluded from aggregate analytics by `GetSelectionAggregateNodeKeys`.
- A selection can be built in several ways:
  - repeated node clicks,
  - expanded structural/subtree click behavior,
  - semantic preset/query application,
  - working-set application,
  - edit-mode rectangle selection or ctrl-click additive selection.

### 3.2 Multi-select analytics scope

For multi-selection in the current workbench, the main analytics surface is the selection aggregate overview returned by `NodeAnalyticsPreviewService.GetCuratedSelectionAggregateOverviewAsync`.

That aggregate overview separates selected nodes into:

- `SupportedNodeKeys`
- `UnsupportedNodeKeys`
- `ContextOnlyNodeKeys`
- `NoDataNodeKeys`
- `IncludedNodeKeys`

This separation is surfaced in the UI through:

- source-map categories,
- coverage counts,
- operational-health signals,
- disaggregation badges,
- aggregate message text describing ignored/unsupported/no-data nodes.

### 3.3 Current multi-select workflow tools

The control panel in `FacilityWorkbench.razor` exposes two workflow tools for building multi-node selections:

- `Semantic Selection`
  - preset selector
  - mutation mode selector
  - role filter
  - semantic-type filter
  - tag input
  - supported-only checkbox
  - apply preset / apply filter / reset actions
- `Working Sets`
  - save current selection under a name
  - optional description
  - list saved sets with node count and updated date
  - apply and delete actions

These tools do not create new visualization types themselves, but they directly control what the downstream analytics widgets display.

### 3.4 Current compare-set behavior

The compare set is separate from the general selection set.

Verified behavior:

- Compare set capacity is hardcoded to 4 (`MaxCompareSetSize`).
- Only `heating_main`, `cooling_main`, `pv_main`, and `chp_main` are compare-supported in the UI (`CompareSetSupportedNodeKeys`).
- `AddSelectionSetToCompareSet` adds compare-supported nodes from the current selection set, skips duplicates, and reports skipped items when full.
- The compare-set list is visible in its own widget and can drive focus-node compare preview and selection-level forecast comparison.

### 3.5 Edit-mode group selection behavior

In edit mode, the same workbench schematic supports rectangle selection, ctrl-click additive selection, multi-node group drag, align, and distribute operations. These are implemented in `FacilityWorkbench.razor`, `FacilitySchematicV2.razor`, and `wwwroot/js/editor.js`.

This is primarily topology editing behavior, not analytics computation, but it does affect which nodes are visually highlighted and which nodes the right-side editor tabs operate on.

## 4. Visualization And Chart Inventory

### Current facility-centric surfaces

#### 4.1 Facility schematic canvas

Files:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`
- `DiplomovaPrace/Components/Editor/FacilitySchematicV2.razor`
- `DiplomovaPrace/wwwroot/js/editor.js`

Behavior:

- Renders the facility graph as SVG nodes and straight links.
- Nodes use style presets resolved by `FacilityNodeStyleSystem`, including shape, size, fill, stroke, symbol, and label styling.
- Renders selection halos, focus halos, and subtree emphasis.
- Non-selected/non-subtree nodes are dimmed when any selection exists.
- Links change width/opacity/color based on focus and subtree inclusion.
- Each node exposes a simple SVG `<title>` tooltip containing truncated label and node type.
- Empty state text is shown when there is no layout data.

Interactions:

- click selection
- pan and zoom
- home / fit-to-view
- search-triggered `panToNode`
- edit-mode rectangle selection
- edit-mode node drag
- edit-mode group drag
- edit-mode keyboard shortcuts (`undo`, `redo`, `escape`)

Time handling:

- none directly; it is a topology/selection surface

#### 4.2 Facility topbar global alert summary

Files:

- `DiplomovaPrace/Components/Layout/FacilityLayout.razor`
- `DiplomovaPrace/Components/Layout/FacilityTopbar.razor`
- `DiplomovaPrace/Services/FacilityAlertSummaryService.cs`

Behavior:

- Shows the active facility name.
- Shows a critical-count badge and an elevated-count badge.
- Opens a dropdown panel listing alerted nodes with label, node key, facility name, and severity badge.
- Clicking an alert item navigates to `/facility` if needed and selects the target facility node.

Data scope:

- only global alert summary for high-level nodes, not all selected nodes

#### 4.3 Interval and weather panel

File:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`

Behavior:

- Displays the current interval range.
- Exposes preset buttons: `24h`, `7 dni`, `30 dni`, `Tento mesic`, `Vlastni`.
- Exposes custom `from` and `to` date inputs when custom mode is active.
- Shows validation message for invalid custom intervals.
- Shows `Anchor: posledni data focus uzlu` when preset anchor note is active.
- Shows weather explanation status and outdoor-temperature delta only when supported for the current focus node.

#### 4.4 Selection summary panel

File:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`

Behavior:

- Displays selected-node count.
- Displays visual-highlight count and context-only highlight count.
- Displays up to three role-group badges, plus a `+N roli` overflow badge.
- Shows a `clear selection` action.

#### 4.5 Semantic Selection and Working Sets panel

File:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`

Behavior:

- Displays semantic query controls and status message.
- Displays saved working sets with name, node count, updated date, apply button, and delete button.

This is a data-display/control surface rather than a chart.

#### 4.6 Disaggregation / Top Contributors

Files:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`
- `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`

Behavior:

- Shows measured/load/generation counts.
- Shows mixed-sign, no-data, unsupported, and context-only badges when applicable.
- Shows composition summary and dominant-source summary.
- Displays top 3 contributors as signed kWh rows with role label, contribution-role label, direction label, and share bar.
- Provides a details expander for additional contributors (up to 8 more shown there).

Data scope:

- selection aggregate only

#### 4.7 Role Breakdown

Files:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`
- `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`

Behavior:

- Displays number of roles and included nodes.
- Shows up to top 6 role rows.
- Each row shows role label, contributing-node count vs total-node count, signed kWh, share percent, and bar fill.
- Shows overflow note when more than 6 roles exist.

#### 4.8 Operational Health

Files:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`
- `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`

Behavior:

- Displays anomaly/attention status badge.
- Shows deviation high/elevated counts.
- Shows summary text and signal chips/messages.
- This is a textual analytic summary, not a chart.

#### 4.9 Source Map

Files:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`
- `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`

Behavior:

- Displays source-map summary.
- Displays category-coded source rows for included measured, unsupported, no-data, and context-only nodes.
- Shows up to 6 rows before truncation note.

#### 4.10 Compare Set widget

File:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`

Behavior:

- Displays compare-set count against max capacity.
- Displays add-from-selection action.
- Displays current compare-set items and remove actions.
- Displays status messages after add/remove attempts.

#### 4.11 Headline KPI widget

Files:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`
- `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`

Behavior:

- Displays headline kWh value.
- Displays headline label and headline description.
- Displays sub-cells for total consumption, total generation, net energy, and sample count.
- Uses net-style signed formatting when `_selectionAggregateOverview.IsNetHeadline` is true.

#### 4.12 Aggregate time-series chart

Files:

- `DiplomovaPrace/Components/Building/FacilityTimeSeriesPanel.razor`
- `DiplomovaPrace/wwwroot/js/facilityTimeSeriesChart.js`
- `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`

Behavior:

- Renders the main current workbench chart.
- Uses ECharts canvas renderer.
- Supports Auto / 15min / Hourly / Daily mode buttons.
- Supports baseline overlay toggle.
- Shows point count, granularity label, y-axis label, and baseline availability status.
- Shows no-data and JS-runtime-unavailable states.

Chart interactions:

- axis tooltip with `cs-CZ` date/value formatting
- inside zoom
- slider zoom
- resize observer
- adaptive label formatting based on date range
- LTTB sampling for dense series

#### 4.13 Forecast vs Actual compare chart

Files:

- `DiplomovaPrace/Components/Building/FacilityCompareTimeSeriesPanel.razor`
- `DiplomovaPrace/wwwroot/js/facilityTimeSeriesChart.js`
- `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`

Behavior:

- Renders multi-series compare output for forecast vs actual on the selection aggregate.
- Displays series count, requested mode label, and granularity label.
- Shows no-data and JS-runtime-unavailable states.
- Uses the same ECharts zoom/tooltip/resize/sampling behavior as the main chart.

#### 4.14 Forecast Diagnostics

Files:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`
- `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`

Behavior:

- Displays forecast status badge.
- Displays aligned-point count vs actual-point count.
- Displays MAE, RMSE, Bias, and WAPE.
- Displays up to 4 explanatory diagnostic points.
- This is a metric-and-text summary, not a chart.

#### 4.15 Load Profile

Files:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`
- `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`

Behavior:

- Displays mode badge, point count, distinct-day count, and mixed-sign badge.
- Displays summary text.
- Displays up to 8 bucket rows sorted by absolute average kW, each with label, proportional fill bar, and signed average value.
- Displays a note distinguishing the load profile from the main chart.

This is a bar-list visualization, not a JS chart.

#### 4.16 Peak Analysis

Files:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`
- `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`

Behavior:

- Displays peak significance badge.
- Displays significance ratio.
- Displays mixed-sign badge when relevant.
- Lists available peak events for demand peak, generation peak/export, and net absolute peak with signed kW and timestamp.
- Displays methodology text.

#### 4.17 Operating Regime

Files:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`
- `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`

Behavior:

- Displays summary text.
- Displays micro-KPIs for baseload proxy, peak/average ratio, variability coefficient, and weekday/weekend delta.
- Displays up to 4 operating-signal points.
- Displays methodology text.

#### 4.18 EMS Operational Scorecards

Files:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`
- `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`

Behavior:

- Displays EMS summary.
- Displays one or more scorecards, each with:
  - label
  - summary
  - metric label/value
  - thresholds
- Displays methodology text.

#### 4.19 Schedule Inefficiency

Files:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`
- `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`

Behavior:

- Displays schedule inefficiency items when present.
- Shows up to 4 inefficiency rows with label, summary, and evidence.
- Otherwise shows a no-major-inefficiency message.
- Displays distinction note when available.

#### 4.20 Opportunity Summary

Files:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`
- `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`

Behavior:

- Displays up to 3 opportunity summary items.
- Adds generation-aware and load-aware badges when applicable.
- Falls back to EMS summary text when opportunities are unavailable.

#### 4.21 Focus-node Deviation / Baseline Detail

Files:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`
- `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`

Behavior:

- Focus-node card showing current value, baseline value, absolute delta, percent delta, severity, and reference interval count.

#### 4.22 Focus-node Alert Detail

Files:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`

Behavior:

- Focus-node card showing alert badge, alert reason, and the schematic color token derived from severity/weather handling.

#### 4.23 Focus Metadata

File:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`

Behavior:

- Focus-node card showing label, node key, type, semantic role, and optional zone.

#### 4.24 Focus Compare Preview

Files:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`
- `DiplomovaPrace/Components/Building/FacilityCompareTimeSeriesPanel.razor`

Behavior:

- Renders a compare chart for the focused curated node and compare-set companions when compare preview is enabled by current state.

#### 4.25 Focus measurable-node KPI card

Files:

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`
- `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`

Behavior:

- Shows meter URN, total consumption, and average power for a non-curated measurable node.

### Legacy but still implemented/routable surfaces

#### 4.26 Legacy dashboard KPI cards

Files:

- `DiplomovaPrace/Components/Pages/DashboardView.razor`
- `DiplomovaPrace/Services/KpiService.cs`

Behavior:

- Total consumption card with exact/estimated badge and optional specific consumption.
- Peak power card.
- Average power card.
- Grid-quality averages card for voltage/current/power factor.
- Each main KPI card can show comparison-vs-previous-period deltas.

#### 4.27 Legacy dashboard baseline analytics card

Files:

- `DiplomovaPrace/Components/Pages/DashboardView.razor`
- `DiplomovaPrace/Services/BaselineService.cs`

Behavior:

- Shows actual consumption, expected baseline consumption, deviation in kWh, deviation percent, and status badge.
- Shows insufficient-data message when reference history is too weak.

#### 4.28 Legacy dashboard active-power line chart

Files:

- `DiplomovaPrace/Components/Pages/DashboardView.razor`
- `DiplomovaPrace/Services/IMeasurementRepository.cs`
- `DiplomovaPrace/Services/EfMeasurementRepository.cs`

Behavior:

- Radzen line chart of `MeasurementRecord.ActivePowerKW` over time.
- Uses repository range query for the selected device and interval.
- Downsamples when more than 1000 records exist, targeting roughly 500 points.

#### 4.29 Legacy dashboard consumption donut chart

Files:

- `DiplomovaPrace/Components/Pages/DashboardView.razor`
- `DiplomovaPrace/Services/KpiService.cs`

Behavior:

- Radzen donut chart splitting consumption into `Pracovni doba` and `Mimo prac. dobu`.
- Uses working-hours classification from `KpiService.IsWorkingHour` and building metadata.

#### 4.30 Legacy KPI page cards

Files:

- `DiplomovaPrace/Components/Pages/KpiView.razor`
- `DiplomovaPrace/Services/KpiService.cs`

Behavior:

- `Energie & Datovy rozsah` card
- `Vykon` card
- `Porovnani k min. obdobi` card

This page has no chart. It is a card-based inspection view.

#### 4.31 Legacy building floor-plan SVG

Files:

- `DiplomovaPrace/Components/Pages/BuildingView.razor`
- `DiplomovaPrace/Components/Building/BuildingViewer.razor`
- `DiplomovaPrace/Components/Building/FloorPlan.razor`
- `DiplomovaPrace/Components/Building/RoomShape.razor`
- `DiplomovaPrace/Components/Building/DeviceIcon.razor`
- `DiplomovaPrace/Services/StateColorMapper.cs`

Behavior:

- Renders building floors as an SVG floor plan.
- Renders clickable room rectangles.
- Renders clickable device icons with symbol glyphs and optional numeric value labels.
- Room fill color comes from display-rule evaluation when rules exist; otherwise from `StateColorMapper.GetRoomFillColor` based on alarm state and temperature.
- Device fill color comes from display-rule evaluation when rules exist; otherwise from `StateColorMapper.GetDeviceColor` based on device type and current state.

#### 4.32 Legacy floor and room summary panels

Files:

- `DiplomovaPrace/Components/Building/FloorSummaryPanel.razor`
- `DiplomovaPrace/Components/Building/RoomSummaryPanel.razor`

Behavior:

- Floor summary shows room count, device count, configured total power/consumption, average temperature, and per-room table.
- Room summary shows device count, configured total power/consumption, average temperature, and per-device table with live formatted values.

#### 4.33 Legacy device detail panel

File:

- `DiplomovaPrace/Components/Building/DeviceDetailPanel.razor`

Behavior:

- Shows device metadata, current state badges, alarm status, and last update time.

#### 4.34 Legacy device numeric history chart

Files:

- `DiplomovaPrace/Components/Building/DeviceDetailPanel.razor`
- `DiplomovaPrace/Services/BuildingStateService.cs`
- `DiplomovaPrace/Services/SimulationService.cs`

Behavior:

- Radzen line chart for numeric devices only.
- Uses `GetDeviceHistory(deviceId)` from the in-memory state service.
- History depth is capped at 100 samples.
- In the simulated runtime, new samples arrive every 2 seconds.

#### 4.35 Legacy expression panel

Files:

- `DiplomovaPrace/Components/Building/ExpressionPanel.razor`
- `DiplomovaPrace/Services/ExpressionEvaluator.cs`

Behavior:

- Displays formula inputs, numeric results, or error messages.
- Supports aggregate functions over building/floor/room targets.

This is a computed data-display surface, not a chart.

#### 4.36 Legacy editor selected-node preview

File:

- `DiplomovaPrace/Components/Pages/EditorView.razor`

Behavior:

- Displays either curated summary data or simple meter KPI preview for the selected facility node.
- Uses a hardcoded 2020 full-year interval.
- No chart is rendered.

#### 4.37 Import result summary

File:

- `DiplomovaPrace/Components/Pages/ImportView.razor`

Behavior:

- Displays import totals for total lines, imported count, skipped count, and format-error count.
- Displays unknown-device list.
- Displays up to first 50 row-level errors, with overflow note if more exist.

This is an administrative data-result surface, not an analytics chart.

## 5. Time Handling And Interval Semantics

### 5.1 Current facility workbench interval behavior

Time behavior is centered in `FacilityWorkbench.razor` and `NodeAnalyticsPreviewService.cs`.

Verified behavior:

- Presets are `last24h`, `last7d`, `last30d`, `thisMonth`, and `custom`.
- Preset windows are anchored to `_presetAnchorUtc`.
- `_presetAnchorUtc` is refreshed from the focused curated node's maximum available timestamp using `GetCuratedNodeMaxTimestampUtcAsync`.
- If there is no curated focus node, the anchor falls back to `DateTime.UtcNow`.
- Custom date input is day-based in the UI.
- Internally, custom `to` is stored as an exclusive upper bound at the next day midnight UTC.

Custom interval validation in `ValidateCustomIntervalForAnalyticsAsync` enforces:

- both dates must be present
- end must be after start
- start year must be greater than `BaselineHistoricalYearsForValidation` (hardcoded `3`)
- if a curated node is focused, the custom interval must overlap that node's available time domain

### 5.2 Current time-series aggregation thresholds

`NodeAnalyticsPreviewService.ResolveTimeSeriesGranularity` uses these verified thresholds:

- up to 7 days -> raw 15-minute series
- more than 7 days and up to 45 days -> hourly average
- more than 45 days -> daily average

Manual overrides are supported through `FacilityTimeSeriesPanel.razor`:

- `Auto`
- `Raw15Min`
- `HourlyAverage`
- `DailyAverage`

### 5.3 Current baseline and forecast window semantics

`NodeAnalyticsPreviewService.cs` defines these baseline constants:

- `MaxHistoricalYearsForBaseline = 3`
- `RecentComparableWindowsForBaseline = 4`
- `MinimumReferenceCoverageRatio = 0.60`

Verified baseline strategy:

- priority 1: same period in previous years
- fallback: recent comparable previous windows of equal length
- reference windows need enough sample coverage relative to the current interval
- baseline overlay is built only when a supported reference choice survives coverage checks

Verified forecast strategy:

- uses historical comparable windows before the target interval
- does not use target-leakage
- computes MAE, RMSE, Bias, WAPE, and alignment coverage on aligned timestamps only

### 5.4 Current chart-runtime time behavior

`wwwroot/js/facilityTimeSeriesChart.js` adapts axis label format by visible time range and enables zooming without server round-trips for each interaction.

### 5.5 Legacy dashboard time behavior

`DashboardView.razor` uses:

- `24h`, `7d`, `30d`, or `custom`
- `datetime-local` UTC inputs for custom range
- previous-period comparison where the previous interval is the immediately preceding window of equal span
- baseline analysis against previous same-length windows shifted by whole weeks through `BaselineService.CalculateBaselineSummaryAsync`

### 5.6 Legacy KPI page time behavior

`KpiView.razor` uses:

- date-only `from` and `to`
- manual calculate action
- previous-period comparison based on the current result duration shifted backward by the same span

### 5.7 Legacy building view time behavior

The old building view does not query arbitrary historical intervals.

Verified behavior:

- `SimulationService` ticks every 2 seconds
- `BuildingStateService` stores only the latest 100 numeric state samples per device
- the history chart always reflects that live in-memory ring buffer

### 5.8 Legacy editor preview time behavior

`EditorView.razor` hardcodes preview time to calendar year 2020 for both curated summaries and meter KPI previews.

## 6. Data Flow And Runtime Sources

### 6.1 Runtime registration

`DiplomovaPrace/Program.cs` is the runtime registration source of truth.

Relevant registrations verified there:

- `IMeasurementRepository -> EfMeasurementRepository` as singleton
- `IKpiService -> KpiService`
- `IBaselineService -> BaselineService`
- `FacilityEditorStateService` singleton
- `FacilityQueryService` scoped
- `FacilityDataBindingRegistry` singleton
- `NodeAnalyticsPreviewService` scoped
- `FacilityAlertSummaryService` scoped
- `IBuildingStateService -> BuildingStateService` singleton
- `SimulationService` singleton + hosted service

The app also initializes SQLite and seeds the facility-centric schematic model at startup.

### 6.2 Current facility-centric data flow

Verified flow:

1. `FacilityImportService` seeds facility graph records into SQLite.
2. `FacilityQueryService.GetMainFacilityAsync` reads the main facility from SQLite, includes nodes and edges, normalizes edges, then applies editor-state structure edits and per-node overrides from `FacilityEditorStateService`.
3. `FacilityWorkbench.razor` loads that facility graph and computes schematic layout.
4. User interactions build `_selectionSet`, `_selectionOrder`, compare-set state, and focused node state.
5. `ReloadPreviewDataAsync` calls into `NodeAnalyticsPreviewService`.
6. `NodeAnalyticsPreviewService` resolves current-node sources via `FacilityDataBindingRegistry`.
7. For binding-supported nodes, analytics are computed from reduced file sources under the configured data root.
8. For non-curated measurable nodes with `MeterUrn`, the workbench uses `GetPreviewDataAsync`, which relies on the legacy KPI/repository path.
9. `RefreshGlobalAlertSummaryAsync` recomputes alert counts for `pv_main`, `chp_main`, `cooling_main`, and `heating_main` over the same currently selected analysis window and pushes them into `FacilityAlertSummaryService`.
10. `FacilityTopbar.razor` renders that global alert summary.

### 6.3 Binding-registry behavior for current analytics

`FacilityDataBindingRegistry.cs` is the lookup layer from facility node id to dataset file binding.

Verified behavior:

- loads all bindings from configured `Facility:BindingsCsvPath`
- groups bindings by `node_id`
- prefers `P@15min`, then `P`, then `Ta@15min`, then `Ta`, then any `15min`, then first available
- resolves absolute file paths as `DataRootPath/meterFolder/fileName`

This is the active dataset-binding mechanism for facility analytics.

### 6.4 Legacy KPI/dashboard data flow

Verified flow:

1. Legacy pages enumerate valid metering devices from the active building configuration.
2. `KpiService` loads measurement rows via `IMeasurementRepository.GetRangeAsync`.
3. `EfMeasurementRepository` executes SQLite queries through `IDbContextFactory<AppDbContext>` and returns domain `MeasurementRecord` rows ordered by timestamp.
4. `KpiService` computes total consumption, working-hours/off-hours split, peak, average power, average voltage/current/PF, and specific consumption.
5. `BaselineService` calls `KpiService` repeatedly on previous weekly windows to derive expected consumption and deviation status.
6. `DashboardView.razor` renders the cards and Radzen charts from those outputs.

### 6.5 Legacy building-view runtime flow

Verified flow:

1. `BuildingStateService` holds the current domain building, per-device state, and per-device history queues in memory.
2. `SimulationService` runs as a hosted service every 2 seconds.
3. Each tick generates new `DeviceState` values and, for metering devices, `MeasurementRecord` values.
4. `BuildingStateService.NotifyStateChanged()` raises one event after each tick.
5. `BuildingViewer`, `RoomShape`, `DeviceDetailPanel`, `FloorSummaryPanel`, and `RoomSummaryPanel` subscribe and rerender.

This is separate from the current facility analytics flow.

## 7. Verified Limitations, Gaps, And Hardcoded Constraints

### Current facility-first stack

- Compare-set support is hardcoded to only `heating_main`, `cooling_main`, `pv_main`, and `chp_main`.
- Global alert summary is also hardcoded to only those four high-level nodes (`EnergyAlertNodeKeys`).
- Weather node is excluded from aggregate selection analytics and is treated as explanatory context, not an alert target.
- Search in the workbench only pans to a node; it does not select it or trigger analytics by itself.
- For non-curated measurable facility nodes, the current workbench shows only a small KPI summary card. It does not render a dedicated single-node history chart.
- `FacilityWorkbench.razor` loads `_curatedPreviewData` and `_curatedTimeSeriesData`, but the focus-detail UI for curated nodes currently renders deviation, alert, metadata, and compare preview only. There is no dedicated focus summary card or dedicated focus-only time-series panel in that section.
- Custom interval validation checks the focused curated node's time domain. If there is no curated focus node, there is no equivalent domain validation for all selected nodes.
- Baseline overlay is available only when the node/source supports deviation and enough reference windows survive coverage checks.
- Current charts depend on JS runtime and `window.echarts`. The Blazor wrappers explicitly show fallback alerts when chart runtime initialization fails.
- The schematic canvas has only basic SVG title tooltips. There is no richer hover drilldown overlay in the current view mode.
- Source Map, Role Breakdown, and contributor lists visibly truncate in the UI (`top 3`, `top 6`, `top 6`, `top 8 extra in details`) rather than exposing full lists inline.

### Current analytics methodology constraints visible in code

- Selection aggregate analytics explicitly separate supported, unsupported, no-data, and context-only nodes instead of forcing every selected node into the same metric.
- Forecast diagnostics are limited by aligned timestamp overlap and can degrade to `LimitedData` when overlap or coverage is weak.
- Operational-health coverage thresholds and forecast fit thresholds are hardcoded in `NodeAnalyticsPreviewService.cs`.

### Legacy surfaces

- `DashboardView.razor` and `KpiView.razor` still depend on the active building configuration's metering device ids, not on the facility graph/binding model.
- `DashboardView.razor` uses simple interval step downsampling for the line chart when records exceed 1000; it is not a more advanced resampling method.
- `BaselineService.cs` uses a simple weekly-window baseline heuristic and requires at least two valid past windows plus non-trivial expected-per-day consumption.
- The old building view uses simulated/in-memory live data and short ring buffers, not arbitrary historical retrieval.
- `EditorView.razor` hardcodes 2020 for selected-node previews.
- The legacy pages remain implemented, and several remain linked in navigation, so they can still affect user understanding of what the product currently contains.

## 8. Exact Files Involved

### Routing, layouts, and navigation

- `DiplomovaPrace/Components/Routes.razor` - default routing/layout entry point
- `DiplomovaPrace/Components/Layout/FacilityLayout.razor` - facility-specific shell
- `DiplomovaPrace/Components/Layout/FacilityTopbar.razor` - current facility alert/facility selector topbar
- `DiplomovaPrace/Components/Layout/NavMenu.razor` - legacy navigation that still links dashboard/kpi/import/editor

### Current primary facility-centric implementation

- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor` - current primary workbench page, selection logic, interval logic, analytics UI, compare-set UI, focus-detail UI
- `DiplomovaPrace/Components/Editor/FacilitySchematicV2.razor` - current schematic renderer
- `DiplomovaPrace/Components/Building/FacilityTimeSeriesPanel.razor` - current single-series/aggregate ECharts wrapper
- `DiplomovaPrace/Components/Building/FacilityCompareTimeSeriesPanel.razor` - current compare ECharts wrapper
- `DiplomovaPrace/wwwroot/js/facilityTimeSeriesChart.js` - ECharts runtime for current charts
- `DiplomovaPrace/wwwroot/js/editor.js` - canvas interaction runtime for schematic pan/zoom/select/drag/keyboard

### Current facility-centric services and data access

- `DiplomovaPrace/Program.cs` - service registration and startup initialization
- `DiplomovaPrace/Services/FacilityQueryService.cs` - loads facility graph from SQLite and applies editor-state overrides
- `DiplomovaPrace/Services/FacilityAlertSummaryService.cs` - stores current alert counts/list for the facility shell
- `DiplomovaPrace/Services/FacilityDataBindingRegistry.cs` - node-to-dataset binding registry and file-path resolver
- `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs` - main current analytics engine for curated nodes and selection aggregates

### Legacy analytics and measurement path

- `DiplomovaPrace/Components/Pages/DashboardView.razor` - legacy dashboard route and Radzen charts/cards
- `DiplomovaPrace/Components/Pages/KpiView.razor` - legacy KPI inspection route
- `DiplomovaPrace/Services/IKpiService.cs` - KPI service contract
- `DiplomovaPrace/Services/KpiService.cs` - KPI computations over measurement records
- `DiplomovaPrace/Services/IMeasurementRepository.cs` - measurement repository contract
- `DiplomovaPrace/Services/EfMeasurementRepository.cs` - SQLite-backed measurement repository
- `DiplomovaPrace/Services/BaselineService.cs` - legacy baseline heuristic

### Legacy building-centric runtime visualization

- `DiplomovaPrace/Components/Pages/BuildingView.razor` - old building visualization route
- `DiplomovaPrace/Components/Building/BuildingViewer.razor` - old building-centric root UI
- `DiplomovaPrace/Components/Building/FloorPlan.razor` - SVG floor plan
- `DiplomovaPrace/Components/Building/RoomShape.razor` - room rectangle rendering and room click behavior
- `DiplomovaPrace/Components/Building/DeviceIcon.razor` - device icon rendering and value label
- `DiplomovaPrace/Components/Building/DeviceDetailPanel.razor` - old device detail and history chart
- `DiplomovaPrace/Components/Building/FloorSummaryPanel.razor` - floor summary side panel
- `DiplomovaPrace/Components/Building/RoomSummaryPanel.razor` - room summary side panel
- `DiplomovaPrace/Components/Building/ExpressionPanel.razor` - expression/evaluation panel
- `DiplomovaPrace/Services/BuildingStateService.cs` - in-memory device state and history store
- `DiplomovaPrace/Services/SimulationService.cs` - 2-second simulation loop
- `DiplomovaPrace/Services/StateColorMapper.cs` - old room/device color mapping
- `DiplomovaPrace/Services/ExpressionEvaluator.cs` - expression evaluation backend

### Other still-implemented data-display surfaces

- `DiplomovaPrace/Components/Pages/EditorView.razor` - legacy editor selected-node analytics preview
- `DiplomovaPrace/Components/Pages/ImportView.razor` - import result summary surface

## 9. Short Audit Summary

The codebase currently contains one dominant, current analytics surface and several older but still-implemented surfaces.

The dominant current implementation is the facility-first schematic workbench on `/` and `/facility`. It uses the SQLite-backed facility graph, file-bound curated analytics, ECharts time-series rendering, selection-aggregate analytics, deviation/baseline logic, forecast comparison, workflow tools, and a topbar global alert summary.

At the same time, the repository still contains a live legacy analytics stack and a live legacy building-visualization stack. `/dashboard`, `/kpi`, `/import`, and `/editor` are still linked from the legacy navigation. `/building` is still routable. These legacy surfaces use different data paths and mental models from the facility-first workbench.

The most important architectural fact from the audit is therefore this: the app does not have one unified visualization system today. It has a current facility-centric workbench plus multiple still-implemented legacy data-display/visualization surfaces that should be treated separately in any future cleanup or redesign work.