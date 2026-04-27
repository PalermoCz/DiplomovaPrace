# Worklog

### Date
[2026-04-27]

### Task
Implementační krok 1: built-in typy, signal taxonomy, multi-binding foundation, weather resolver a první legacy cleanup

### What changed

**Foundation services:**
- Added `FacilityBuiltInNodeTypes` as the central built-in `NodeType` resolver for `area`, `bus`, `weather`, plus the single legacy `weather_main` fallback point.
- Added `FacilitySignalTaxonomy` with first-class `FacilitySignalCode` and central `FacilitySignalFamily` mapping for the requested built-in exact signal codes.
- Added `FacilityWeatherSourceResolver` for facility-level weather node discovery and `Ta` binding lookup outside normal selection flow.

**`DiplomovaPrace/Services/FacilityDataBindingRegistry.cs`:**
- `BindingRecord` now exposes centralized exact signal code and signal family metadata.
- Added multi-binding-aware read helpers for all bindings, exact signal code filtering, signal family filtering, and preferred binding selection over filtered subsets.
- Preserved existing `GetPrimaryBinding()` compatibility, but moved its selection logic into the new centralized preference path.
- Exact signal matching is case-insensitive.

**Legacy cleanup and call-site adoption:**
- `FacilityNodeSemantics` now resolves weather context through the central built-in helper instead of a raw `weather_main` check.
- Built-in `area` and `bus` now feed central semantic classification.
- Replaced direct `weather_main` checks in `NodeAnalyticsPreviewService`, `SchematicComposerV2`, `EditorCanvas`, `EditorView`, and `FacilityWorkbench` with central helpers.
- Centralized the legacy weather curated fallback in `NodeAnalyticsPreviewService`; user-facing copy now refers to the weather context node instead of the raw legacy key.

### Build
- `dotnet build` successful.

### Validation scope
- Build-only validation performed.
- Import UI and analytics feature work intentionally not implemented in this step.

---

### Date
[2026-05-01 21:10]

### Task
FacilityWorkbench compact top-area redesign completion + Razor build repair

### What changed

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Finalized the top information/tools area as exactly 3 visually separated cards: Interval, Selection, Tools.
- Simplified Selection Filters UI to Type + Zone + mutation mode + Supported only; active tag filtering UI is no longer rendered.
- Removed the editor Tags section from the Edit tab; editor metadata surface now stays focused on Basic Info, Connections, and Note.
- Selection summary now renders a compact donut grouped by supported selected `NodeType` values, collapses overflow into `Other`, and exposes per-segment detail via SVG `<title>` tooltips.
- Donut segment color resolves from the dominant effective node fill color for each group.
- Repaired a corrupted `GetVisualHighlightNodeKeysForCanvas()` method that had stray injected lines and a missing closing brace, which was the root cause of the Razor parser failure at `@code`.
- Working-set semantic tag application remains deactivated; live semantic apply path no longer depends on tag filters.

**`DiplomovaPrace/wwwroot/js/editor.js`:**
- Hover card keeps Style + Note, removes Tags from the visible card, escapes rendered values, and truncates the note for compact display.

**`DiplomovaPrace/wwwroot/css/editor.css`:**
- Tightened hover-card presentation and note clamping for the simplified metadata surface.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor.css`:**
- Keeps the stronger compact card styling for the 3-card overview layout and associated selection/tools card presentation.

### Build
- `dotnet build` successful after the Razor repair and donut follow-up.

### Validation scope
- Build-only validation performed.
- Browser validation was not performed in this pass.

---

### Date
[2026-05-01]

### Task
Compact 3-card overview panel redesign + hover card + editor cleanup

### What changed

**`DiplomovaPrace/wwwroot/js/editor.js`:**
- `setupHoverTooltip`: removed Tags row, added Note row (reads `data-node-note`, truncates to 80 chars with `…`). Style already uses display name via `data-node-preset-label`.

**`DiplomovaPrace/Components/Editor/FacilitySchematicV2.razor`:**
- Added `NodeNotesByNodeKey` parameter (IReadOnlyDictionary).
- Added `data-node-note` attribute on SVG `<g>` element.
- Added `ResolveNodeNote(string nodeKey)` private helper.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Added `EffectiveNodeNotePreviewForCanvas` computed property and `BuildEffectiveNodeNotePreviewForCanvas()` method.
- Added `GetEditorNote(string nodeKey)` helper.
- Passed `NodeNotesByNodeKey="@EffectiveNodeNotePreviewForCanvas"` to all 3 `<FacilitySchematicV2>` usages.
- Replaced old `cp-section cp-interval-section` with new `cp-card cp-interval-card` — removed Anchor note and Weather context rows.
- Replaced old `cp-section cp-selection-section` (count + chip badges) with new `cp-card cp-selection-card` — count big + role donut SVG via `BuildSelectionDonutMarkup()`.
- Added `BuildSelectionDonutMarkup()` (pure SVG donut, groups by NodeType, max 4 segments, uses `GetFacilityRoleDonutColor`).
- Added `GetFacilityRoleDonutColor(FacilityNodeRole role)` static method.
- Replaced old `cp-section cp-tools-section` (with tab-bar, Tags picker, Tags AND/OR) with new `cp-card cp-tools-card` — Type/Zone 2-column grid only, no Tags filter row, no Tags AND/OR buttons.
- Removed Tags ep-section from editor Edit tab entirely.
- Removed unused `_isFilterTagsOpen` and `_isEditorTagsOpen` field declarations (kept for C# method compatibility).

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor.css`:**
- Added `.cp-card`, `.cp-card:last-child`, `.cp-card-label`, `.cp-interval-card`, `.cp-selection-card`, `.cp-sel-body`, `.cp-sel-left`, `.cp-sel-count-big`, `.cp-sel-count-label`, `.cp-sel-empty-hint`, `.cp-clear-sel-btn`, `.cp-sel-donut`, `.cp-tools-card`, `.cp-sem-row1` CSS classes.

**Build:** 0 errors, 2 warnings (CS0414 unused fields — benign).

---

### Date
[2026-04-25 20:15]

### Task
Final sprint-closing pass: tag UX + Selection Filters OR/AND + hover-card metadata cleanup

### What changed

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Selection Filters Tags picker improved with search input inside dropdown and user-facing tag filtering (reserved/system tags hidden from choices).
- Added explicit tag semantics toggle (`Tags: AND` / `Tags: OR`), default `AND`.
- Kept visible action mode buttons (`Replace / Add / Remove`) and wired apply path to honor tag semantics mode in code.
- Editor Tags block now supports compact search/select/add flow in one area (`Search existing or add tag` + `Add` button + existing checkbox picker).
- Editor tag add-new validation implemented:
  - blocks spaces/dots,
  - blocks reserved/system tags,
  - case-insensitive dedupe,
  - max 3 tags enforced with message `Maximum 3 tags allowed`.
- Added reserved-tag handling that keeps persisted reserved tags hidden from normal choices while preserving them for save (no automatic data scrubbing).
- Added canvas mappings for hover metadata:
  - style preset display label per node,
  - user-facing node tags per node.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor.css`:**
- Tag dropdown max-height increased (`190px` -> `340px`) to reduce constrained scrolling.
- Added styles for tag search input (`cp-tag-picker-search`) and compact editor tag tools block (`cp-tag-editor-tools`, `cp-tag-editor-note`).

**`DiplomovaPrace/Components/Editor/FacilitySchematicV2.razor`:**
- Added SVG data attributes for hover card:
  - `data-node-preset-label`
  - `data-node-tags`

**`DiplomovaPrace/wwwroot/js/editor.js`:**
- Hover card now reads and renders compact `Tags` row (first 3 + `+N` remainder).
- Hover `Style` now prefers display label (`data-node-preset-label`) instead of internal preset key.

**`DiplomovaPrace/Services/FacilityEditorStateService.cs`:**
- Updated tag normalization to preserve chosen display casing while keeping trim + case-insensitive dedupe.

### Build
- `dotnet build` successful (0 errors).

### Validation scope
- Browser automation was not used.
- Browser validation was not performed in this pass (manual checklist required).

### Date
[2026-04-25 18:30]

### Task
Selection Filters UX pass — Tags multi-select picker, Type/Zone/Tags 3-column layout, Add/Replace/Remove action control; Editor Tags field replacement

### What changed

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Added state fields: `HashSet<string> _filterTagsSelected`, `bool _isFilterTagsOpen`, `HashSet<string> _editorTagsSelected`, `bool _isEditorTagsOpen`, `List<string> _allDistinctTags`
- Added methods `ToggleFilterTag(string)` and `ToggleEditorTag(string)`
- `RebuildFacilityNodeIndexes()`: added `_allDistinctTags` (full tag list, no Take cap); `_availableTagChips` still uses `.Take(20)` for chip cloud only
- `ApplySemanticQueryAsync()`: fixed to use `_filterTagsSelected` (not old ParseTagQuery) and `_semanticSelectionMutationMode` (not hard-coded Replace)
- `ClearSemanticQueryFilters()`: clears `_filterTagsSelected`, closes picker, resets mode to Replace
- `BuildSemanticQueryFromFilters()`, `BuildWorkingSetSemanticSnapshot()`, `ApplySemanticFiltersFromQuery()`: all updated to use `_filterTagsSelected`
- `RestoreFocusedNodeEditorDraft()`: syncs `_editorTagsSelected` in all three branches
- Selection Filters markup: 2-column grid → 3-column (Type / Zone / Tags picker) + Replace/Add/Remove btn-group
- Editor Tags section: replaced text input with `cp-tag-picker` multi-select dropdown

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor.css`:**
- `.cp-sem-filters`: grid changed from `repeat(2,...)` to `repeat(3,...)`
- New CSS rules: `.cp-tag-picker`, `.cp-tag-picker-trigger`, `.cp-tag-picker-label`, `.cp-tag-picker-chevron`, `.cp-tag-picker-dropdown`, `.cp-tag-picker-item`, `.cp-tag-picker-empty`

### Build
0 errors, 0 warnings

### Browser validated (http://localhost:5016/facility)
- Selection Filters: Type / Zone / Tags side-by-side 3-column layout ✓
- Tags picker shows "Any tag" when nothing selected; opens dropdown with all 11 distinct tags with checkboxes ✓
- Replace / Add / Remove mode buttons present; clicking Add activates it (blue highlight) ✓
- Supported only checkbox unaffected ✓
- Editor: TAGS section renders `cp-tag-picker` with "No tags selected" trigger when node has no tags ✓
- Editor: clicking a node opens picker in right-panel editor (Electricity node validated) ✓
- Save node / Restore draft buttons unaffected ✓

### Date
[2026-04-25 15:00]

### Task
Interval module: final polish pass — format unification + summary strip strengthening

### What changed

**C# display logic** (`FacilityWorkbench.razor`, `DisplayIntervalFromTo` property):
- Both branches replaced culture-sensitive interpolation (`{x:dd.MM.yyyy}`) with explicit `.ToString("dd'/'MM'/'yyyy")` / `.ToString("dd'/'MM'/'yyyy HH:mm")` calls.
- Root cause: `{x:dd.MM.yyyy}` in C# string interpolation passes the format string to `ToString(format, currentCulture)`, and in Czech culture the `/` date separator maps to `.` — producing `18.04.2026` instead of `18/04/2026`. Single-quoting the separators forces literal slashes regardless of thread culture.
- Custom branch: `$"{fromDate:dd.MM.yyyy} - {toInclusiveDate:dd.MM.yyyy}"` → `$"{fromDate.ToString("dd'/'MM'/'yyyy")} - {toInclusiveDate.ToString("dd'/'MM'/'yyyy")}"`.
- Preset branch: same pattern with HH:mm component.

**CSS** (`FacilityWorkbench.razor.css`):
- `.cp-custom-interval-summary`: added `background: rgba(29, 78, 216, 0.04)` (faint blue tint); replaced `padding-top: 0.18rem` with `padding: 0.2rem 0.32rem 0.16rem`; increased `margin-top` from `0.22rem` to `0.28rem`; added `border-radius: 0 0 4px 4px`.
- `.cp-custom-picker-block`: removed bottom padding (`0.22rem` → `0`) — summary strip now carries its own bottom padding, eliminating the double-gap.

### Browser validated (http://localhost:5016/facility, full restart, terminal ID 5906f3df)
- 7d preset range display: `18/04/2026 16:27 - 25/04/2026 16:27` — slashes confirmed, no dots ✓
- Custom mode range display: `18/04/2026 - 25/04/2026` — slashes confirmed ✓
- Summary text: `18/04/2026 00:00 → 25/04/2026 23:59 · UTC` — slashes confirmed ✓
- FROM picker input: `18/04/2026 00:00` — day-first, 24h, no AM/PM ✓
- Summary background computed style: `rgba(29, 78, 216, 0.04)` — blue tint applied ✓
- No dot-separated dates anywhere visible in the Interval module ✓
- Preset switching works ✓
- Custom mode activates/deactivates cleanly ✓

### App status
Left running on http://localhost:5016 (terminal ID 5906f3df-d154-4b3a-8181-bd1ba917b283).

---

### Date
[2026-04-25 14:00]

### Task
Interval module: visual redesign pass

### What changed

**Markup** (`FacilityWorkbench.razor`):
- Replaced the old `cp-section-label` div + loose `cp-interval-range-display` div layout with a new `cp-interval-header` wrapper containing a `cp-interval-label` span and the range display.
- Range display is now `cp-interval-range-display` inside the header — framed like a value, not a floating label.
- Preset button labels shortened: "7 days" → "7d", "30 days" → "30d", "This month" → "Month" — to enable a single-row segmented-control layout.
- Added `cp-custom-picker-block` wrapper div around the `cp-custom-date-row` + `cp-custom-interval-summary` — gives the custom area a distinct inset card boundary.
- FROM/TO labels changed to uppercase.

**CSS** (`FacilityWorkbench.razor.css`):
- `cp-interval-label`: new element — 0.6rem, uppercase, `#94a3b8` gray — a deliberate micro-label, not a section header.
- `cp-interval-range-display`: now has `background: #ffffff; border: 1px solid #bfdbfe; border-radius: 5px; padding: 0.22rem 0.5rem; font-weight: 700; color: #1e40af; font-size: 0.78rem; font-variant-numeric: tabular-nums` — renders as a framed value token, not a plain text line.
- `cp-preset-row`: redesigned as a segmented-control track — `background: #dbeafe; border-radius: 6px; padding: 0.15rem; flex-wrap: nowrap` — presets distribute evenly across the full row width.
- `cp-preset-btn`: `flex: 1 1 0; border: none; background: transparent; border-radius: 4px` — flat pill within the track.
- `cp-preset-btn-active`: `background: #1d4ed8; box-shadow: 0 1px 3px rgba(29,78,216,0.35)` — solid filled chip, visually dominant.
- `cp-custom-picker-block`: new — `background: #f8faff; border: 1px solid #dbeafe; border-radius: 6px; padding: 0.28rem 0.38rem 0.22rem` — inset card for the custom interval control.
- `cp-custom-interval-summary`: upgraded from weak helper text to a separated info row — `border-top: 1px solid #dbeafe; padding-top: 0.18rem; font-weight: 500; color: #475569` — feels deliberate, not leftover.
- `cp-date-label`: now uppercase `#94a3b8`, `0.6rem` — matches interval micro-label style, creates a unified label register.

### What works now (browser validated)
- Module is visibly redesigned: segmented preset control, framed range display, inset custom picker block.
- "INTERVAL" header is a micro-label, not a shouting section title.
- Range display ("18.04.2026 - 25.04.2026") renders as a bordered token on white background, bold dark blue.
- Preset row is a single-row segmented control, all 5 buttons on one line; active button is filled solid blue with drop shadow.
- Custom picker block renders as an inset card; FROM/TO are uppercase micro-labels.
- Summary row `18/04/2026 00:00 → 25/04/2026 23:59 · UTC` is separated by a top border, feels like secondary data, not leftover text.
- Day-first format (DD/MM/YYYY) confirmed in pickers. ✓
- 24-hour format confirmed (00:00 / 23:59, no AM/PM). ✓
- Preset buttons work. ✓
- Custom mode activates correctly. ✓
- Validation message behavior and analytics reload — unchanged architecture. ✓

### App status
Left running on http://localhost:5016 (terminal ID 7524e869). 

### Task
Custom interval picker: cleanup dead code + visual polish pass

### What changed
- **Markup**: Removed two per-picker `.cp-custom-date-confirm` divs (blue "dd.MM.yyyy HH:mm UTC" labels under each picker) and the `.cp-custom-date-hint` div. Added a single `.cp-custom-interval-summary` div below the `cp-custom-date-row` grid showing `DD/MM/YYYY HH:mm → DD/MM/YYYY HH:mm · UTC` in muted gray.
- **Format**: Used `ToString("dd'/'MM'/'yyyy HH:mm")` (quoted separators) to force literal `/` slashes in the summary regardless of Czech thread culture (which would otherwise produce dots).
- **Dead state fields removed**: `_customFromDate`, `_customFromTime`, `_customToDate`, `_customToTime`.
- **Dead methods removed**: `OnCustomFromInputChanged`, `OnCustomToInputChanged` (empty stubs), `OnCustomFromDateChanged`, `OnCustomFromTimeChanged`, `OnCustomToDateChanged`, `OnCustomToTimeChanged`, `TryCombineCustomFromDateTime`, `TryCombineCustomToDateTime`, `ParseHHmm`, `SyncCustomDateInputsFromState`.
- **Dead call removed**: `SyncCustomDateInputsFromState()` call in `SetIntervalPreset` when switching to "custom".
- **CSS**: Removed `.cp-custom-date-input`, `.cp-date-time-row`, `.cp-custom-date-confirm`, `.cp-custom-date-hint` rule blocks. Added `.cp-custom-interval-summary` rule (`font-size: 0.68rem; color: #64748b; font-variant-numeric: tabular-nums; margin-top: 0.25rem`).

### What works now
- Build: 0 errors, 0 warnings (confirmed).
- Browser validation (first run, before format fix): RadzenDatePickers render correctly, 24-hour format, no AM/PM, no per-picker UTC labels, single gray summary line visible. Pickers display `DD/MM/YYYY HH:mm` format.
- Summary line format fix applied (quoted separators) — slashes guaranteed regardless of server culture.

### Remaining
- None for this pass. All cleanup and polish items complete.

### Task
Custom interval picker: replace native inputs with RadzenDatePicker

### What changed
- Replaced the 4 native `<input type="date/time">` elements in the custom interval picker with 2 `RadzenDatePicker<DateTime>` components (`ShowTime=true`, `DateFormat="dd/MM/yyyy HH:mm"`, `Culture=en-GB`).
- Added `_pickerCulture` field (`CultureInfo("en-GB")`) — en-GB uses 24-hour `HH:mm` short time pattern, which Radzen uses to determine 12h vs 24h display.
- Added `OnCustomFromPickerChanged(DateTime?)` and `OnCustomToPickerChanged(DateTime?)` handlers. These set `_customFromUtc` and `_customToUtcExclusive` directly from the picker's `DateTime` value, preserving the exclusive upper-bound convention (`+1 minute`).
- Old string state fields (`_customFromDate`, `_customFromTime`, `_customToDate`, `_customToTime`), old handlers (`OnCustomFromDateChanged`, etc.), and `TryCombineCustomFrom/ToDateTime`, `SyncCustomDateInputsFromState`, `ParseHHmm` remain in code but are now dead — intentionally not removed in this pass to keep the change minimal.
- Added `overflow: visible` to `.cp-custom-date-row` and `.cp-radzen-picker` sizing CSS to prevent popup clipping.
- `RadzenComponents` is already registered in `FacilityLayout.razor`; Radzen CSS and JS already loaded in `App.razor`; `@using Radzen.Blazor` already in `_Imports.razor` — no new dependency or setup required.

### What works now
- Build: 0 errors, 0 warnings.
- Radzen renders its own calendar popup — completely independent of browser/OS locale, making the `lang="en-GB"` native limitation irrelevant.
- `DateFormat="dd/MM/yyyy HH:mm"` controls displayed format explicitly.
- en-GB culture forces 24-hour time in Radzen's time picker.

### Remaining issue / next step
- **Browser validation was not completed**: The app was not running at the time of validation attempt. The shared page at `https://localhost:7278/facility` returned "Failed to Load Page".
- Required manual validation after restart:
  1. Start the app (`dotnet run` or F5)
  2. Open FacilityWorkbench → click Custom preset
  3. Confirm: RadzenDatePicker renders, calendar popup opens, day-first date display, 24-hour time (no AM/PM)
  4. Confirm confirm labels update correctly
  5. Confirm popup is not clipped in the sidebar panel
  6. Confirm analytics reload after date selection
  7. Confirm preset buttons still work
- If the Radzen popup is clipped by the panel overflow, the parent panel CSS may also need `overflow: visible` — check after restart.

### Date
[2026-04-25 00:00]

### Task
Interval picker UX repair + startup viewport-to-root fix

### What changed
- Added `lang="en-GB"` attribute to all 4 native HTML date/time inputs in the custom interval picker (`FacilityWorkbench.razor` lines ~1026–1047). This instructs the browser to render the date picker as day-first (DD/MM/YYYY) and the time picker as 24-hour (no AM/PM). The internal value format (`yyyy-MM-dd`, `HH:mm`) and all C# parsing/state logic are unchanged.
- Added `OnAfterRenderAsync(bool firstRender)` override to `FacilityWorkbench`. On first render, fires the existing 80ms-delayed `GoHomeAsync()` — the topology-aware root pan that `ResolveHomeTargetNode` + `editorCanvas.panToNode` already performs on mode transitions. Reuses the established fire-and-forget delay pattern already present in fullscreen/edit mode toggles.

### What works now
- Build compiles clean (no CS errors; file-lock MSB warning only because running app held the DLL).
- The `lang="en-GB"` fix is correct for Chromium-based browsers (Chrome, Edge) and Firefox on Windows — these respect the `lang` attribute on `<input type="date">` and `<input type="time">` for picker locale display.
- Startup schematic now calls `GoHomeAsync()` after 80ms following first render, producing the same topology-aware viewport as the Home button.

### Remaining issue / next step
- **Browser validation is required by the user.** The `lang="en-GB"` fix cannot be confirmed as effective without opening the app in the actual browser and inspecting the picker UI. Browser-native picker locale behavior is OS/browser-version-dependent and cannot be verified by build alone.
- If the browser running this app is Chrome/Edge on Windows, `lang="en-GB"` is expected to work. If the picker still shows MM/DD/YYYY or AM/PM after the fix, a JS datepicker library (e.g., Flatpickr) would be needed — this is a scope escalation not done here.
- Startup viewport timing (80ms) is consistent with existing mode-toggle behavior. If the canvas is not ready within 80ms on a slow machine, the delay can be increased.

## Entry template
### Date
[YYYY-MM-DD HH:MM]

### Task
[short title]

### What changed
- ...

### What works now
- ...

### Remaining issue / next step
- ...

## Initial entry
### Date
[2026-04-21 00:00]

### Task
AI workflow initialization

### What changed
- Initialized AI workflow files for lightweight persistent, plan-first development.
- Added AGENT_CONTEXT.md with project context, data source reality, and editor/graph principles.
- Added CURRENT_TASK.md as a minimal task template.
- Added WORKLOG.md with reusable entry template.

### What works now
- A reusable low-token AI workflow scaffold exists under AI/.
- Future agents can read context/task/worklog before implementation.

### Remaining issue / next step
- Replace template placeholders in CURRENT_TASK.md when starting the next real task.

### Date
[2026-04-21 13:40]

### Task
Editor UX: selection-first multi-select/home/grid fixes

### What changed
- Enforced single-select vs multi-select exclusivity in editor selection flow.
- Limited multi-select highlight to actual selected nodes to prevent unrelated ancestor highlighting.
- Split right-panel tabs by mode: single-select shows Edit/Add/Delete/Import, multi-select shows only Layout.
- Updated group drag persistence to snap final coordinates to grid per node.
- Changed Home targeting to topology-first root resolution with unary-wrapper collapse.
- Aligned edit-mode entry viewport with Home behavior.
- Removed persistence/layout constraints blocking movement outside old region by allowing finite negative hints and treating out-of-range hints as absolute pinned coordinates.

### What works now
- Selection mode and panel mode are mutually exclusive in editor behavior.
- Layout operations remain grid-snapped and reuse existing dirty/save/undo flow.
- Home and initial edit viewport share the same logical targeting path.

### Remaining issue / next step
- Execute focused manual UI checks for rectangle-selection highlighting, group drag interaction feel, and external-root targeting on current dataset.

### Date
[2026-04-21 14:15]

### Task
Editor UX: group drag fix and single-select Edit tab

### What changed
- Fixed facility group drag click handling so clicking a selected node no longer collapses multi-selection before drag starts.
- Changed group drag persistence to apply the already snapped group delta without per-node re-snapping, preserving relative spacing.
- In edit mode, single-clicking a node now forces the right panel back to the Edit tab.

### What works now
- Group drag can stay armed after box selection and does not require a modifier key.
- Single-select node clicks in edit mode return the properties panel to Edit.

### Remaining issue / next step
- Run focused manual UI verification for multi-select drag feel, relative spacing preservation, and dirty/save/undo behavior on the current dataset.

### Date
[2026-04-22 00:00]

### Task
Editor UX milestone: simplified single-node panel and global node search

### What changed
- Simplified the single-node edit panel to Basic Info, Connections, Tags, and Note.
- Unified non-layout relationships in the UI as additional_link while keeping the primary layout parent separate.
- Added normalized searchable pickers for changing parent and adding additional links.
- Added one global normalized node search bar for both normal and editor view toolbars.

### What works now
- Single-node editing keeps the existing draft/save/undo/discard pipeline with a smaller UI surface.
- Existing non-layout relationships are shown uniformly in the panel and can be removed from there.
- Node search matches key and label case-insensitively while ignoring dots, underscores, and spaces.

### Remaining issue / next step
- Run focused manual UI checks for parent reassignment, additional-link add/remove, and search-to-focus behavior on the live schematic.

### Date
[2026-04-22 10:25]

### Task
Phase 1 relationship architecture: topology-preserving edges

### What changed
- Added explicit relationship metadata to schematic edges so topology can preserve relationship kind and layout-edge intent.
- Updated startup/schema bootstrap for existing SQLite DBs so the new edge columns and unique index are created without switching to full EF migrations.
- Changed facility import to persist explicit edge metadata and to materialize layout-primary edges separately from additional relationships.
- Changed facility query reconstruction so runtime facility keeps full topology edges while layout still follows the single ParentNodeKey projection.

### What works now
- One primary layout parent still drives deterministic layout.
- Explicit edges are no longer collapsed away in the query layer.
- Phase 1 contract is in place for future selection semantics and relationship authoring work.

### Remaining issue / next step
- Run manual UI verification that the current facility/editor views render expected layout and explicit links on the existing dataset.

### Date
[2026-04-23 00:00]

### Task
Phase 1 FacilityWorkbench cleanup: tabbed analytics and alert removal

### What changed
- Added tab-based analytics sections in FacilityWorkbench for Overview, Breakdown, Performance, Compare, and Diagnostics.
- Kept Overview as the eager default view and moved the remaining analytics sections behind tab activation with lazy service requests.
- Removed the topbar alert dropdown and deleted the related global alert refresh path from the workbench.
- Trimmed the default Overview focus area to the core baseline/detail slice and moved compare preview into the Compare tab.

### What works now
- The main workbench opens on a lighter Overview tab instead of rendering the full analytics stack immediately.
- Breakdown, Performance, Compare, and Diagnostics sections only render after the corresponding tab is opened.
- Compare preview data is fetched only after the Compare tab is activated.
- The topbar no longer triggers phase-1 alert-summary UI or background refresh work.

### Remaining issue / next step
- Run focused manual UI checks for first-open tab loading, compare-tab activation, and diagnostics/performance visibility on the live facility dataset.

### Date
[2026-04-23 00:40]

### Task
FacilityWorkbench UX polish: analytics presentation and English copy

### What changed
- Polished the analytics tab navigation into a more intentional card-like navigation shell with tab metadata and an active-panel header.
- Translated the active analytics strip in FacilityWorkbench to English, including interval/selection copy, analytics widget labels, helper bullet text, and compare/working-set status messages shown in the analytics surface.
- Improved sparse tab presentation, especially Compare, with clearer setup guidance and calmer empty-state cards instead of abrupt blank space.

### What works now
- The analytics area reads as a more coherent product surface while keeping the existing tab logic and lazy loading strategy.
- The active analytics UI is English-only in the touched FacilityWorkbench slice.
- Compare feels intentional even before a chart can render.

### Remaining issue / next step
- Run manual UI checks for visual balance of the new tab shell, long English copy wrapping, and compare-tab behavior on the live dataset.

### Date
[2026-04-22 14:35]

### Task
Phase 2 selection semantics: strict vs expanded subtree

### What changed
- Added a shared selection traversal helper for strict layout subtree and expanded subtree resolution.
- Kept strict subtree selection on the existing single-parent layout tree and excluded layout_primary from expanded topology traversal.
- Added explicit expanded subtree action in the workbench selection panel and wired workbench selection/highlight to the resolved concrete node set.

### What works now
- Normal click behavior remains strict layout subtree selection.
- Expanded subtree can be triggered explicitly from a focused structural node.
- Expanded traversal applies allowed relationship kinds with deduplication and cycle protection while leaving layout deterministic.

### Remaining issue / next step
- Run manual UI checks for explicit expanded selection behavior, topology-driven inclusion, and non-regression of current editor selection UX.

### Date
[2026-04-22 15:10]

### Task
Phase 2 adjustment: structural click uses expanded traversal

### What changed
- Removed the explicit Expanded subtree button from the workbench selection panel.
- Changed structural-node click selection to use expanded topology-aware traversal by default.
- Changed structural subtree selection to include all reachable nodes from traversal, including nodes without data/bindings.

### What works now
- Normal structural click expands through allowed additional relationship kinds automatically.
- Reachable no-data nodes are included in selection.
- Cycle protection and deduplication remain in the shared traversal helper.

### Remaining issue / next step
- Run manual UI verification that structural click expansion, no-data inclusion, and current editor behavior all remain stable on the current dataset.

### Date
[2026-04-22 16:05]

### Task
Relationship inspector and selection/highlight clarity

### What changed
- Added a read-only relationship inspector to the single-node Edit panel with primary layout parent, layout children, incoming explicit relationships, outgoing explicit relationships, relationship kind, and note.
- Split workbench-derived canvas state into explicit resolved selection and visual highlight concepts.
- Added UI copy that shows when visual highlight includes context-only nodes beyond the resolved selection count.

### What works now
- The currently selected node can expose its topology context without enabling relationship editing.
- Selection count and canvas highlight are now represented as intentionally separate concerns in the workbench logic.

### Remaining issue / next step
- Run focused manual UI checks for nodes like V.Z82 and H1.Z28 to verify that the inspector explains expanded selection and context-only highlight on the live dataset.

### Date
[2026-04-22 16:35]

### Task
Selection policy fix for traversable leaf nodes

### What changed
- Narrowed workbench click policy so non-structural nodes with allowed outgoing explicit relationships now enter the same expanded traversal path as structural nodes.
- Kept traversal helper architecture unchanged and reused the existing expanded traversal resolution.
- Left context-only ancestor highlight logic intact, but it no longer drives the H1.Z28 case because that click now resolves through expanded traversal first.

### What works now
- Nodes like H1.Z28 can behave consistently with V.Z82 when they have traversable outgoing semantic or membership edges.
- Resolved selection and visual highlight stay separate concepts without the previous leaf-click mismatch.

### Remaining issue / next step
- Run manual UI verification on the live facility view for V.Z82, H1.Z28, and nearby nodes to confirm the new traversal trigger feels correct and editor behavior remains stable.

### Date
[2026-04-22 18:10]

### Task
Relationship editing UI for additional explicit relationships

### What changed

### What works now

### Remaining issue / next step

### Date
[2026-04-22 19:05]

### Task
Follow-up UX fix for global search and simplified connections panel

### What changed
- Changed global node search to navigation-only and added it to the Focus schematic view.
- Made empty focused search show node candidates without mutating the current selection.
- Collapsed the parent and additional-link pickers until explicitly opened.
- Removed remaining technical helper text from the simplified connections panel and made additional links render as solid lines.

### What works now
- Search can pan to nodes in dashboard, edit, and Focus views without replacing the current edit target.
- The simplified panel keeps a smaller footprint until a picker is actively used.
- Additional links visually match the simplified model as solid non-layout connections.

### Remaining issue / next step
- Run manual UI checks for navigation-only search behavior, collapsed picker interaction, and solid-link rendering on the live page.

### Date
[2026-04-22 19:35]

### Task
Follow-up UX fix for search lifecycle and Connections cleanup

### What changed
- Changed empty focused global search to show all node options while keeping click behavior navigation-only.
- Kept search live after result clicks and tightened pan-to-node centering.
- Simplified Connections again so Primary parent uses a chip-like row with inline Change action and additional-link add flow sits directly under the list.
- Removed the remaining link-note path from the simplified panel flow.

### What works now
- Search stays usable immediately after choosing a result in normal, edit, and Focus views.
- Connections uses a smaller chip/list presentation without extra headings or helper text.
- Additional links remain visually solid in the schematic.

### Remaining issue / next step
- Run focused manual UI checks for centered jump accuracy and the final Connections interaction on the live facility page.

### Date
[2026-04-22 22:05]

### Task
Style tab UX follow-up: preset duplication and label cleanup

### What changed
- Added a duplicate action for the currently selected style preset in the Style tab.
- Changed preset dropdown and preset-editor labels to show display names without internal preset keys.
- Kept duplicated presets on the existing preset persistence path and made the duplicate editable immediately.

### What works now
- A selected preset can be duplicated into a new internal key without adding a separate empty preset flow.
- The duplicated preset becomes the active editable preset immediately after duplication.
- User-facing preset labels no longer show keys in parentheses.

### Remaining issue / next step
- Run manual UI checks for duplicate preset flow, rename flow, and Style tab live preview behavior.

### Date
[2026-04-23 16:20]

### Task
FacilityWorkbench Phase 1: aggregate Overview and interval anchoring

### What changed
- Changed Overview so structural focus no longer falls back to a structural-only message when the resolved selection contains supported analytics descendants.
- Changed preset anchoring and custom interval validation to use the supported aggregate selection scope for structural and other aggregate selections.
- Reworked the default Overview layout so the Headline KPI and detail card share the left column while the main time-series panel remains dominant on the right.

### What works now
- Structural subtree selections can stay on the default Overview surface and reuse the existing aggregate KPI and time-series model.
- Presets such as last 7 days can anchor to the latest supported descendant data inside the current analytics selection instead of only the focused node.
- The default Overview requires less vertical space on normal desktop widths because the detail slice no longer sits in a separate full-width row.

### Remaining issue / next step
- Run manual UI verification for structural aggregate focus, aggregate custom-interval overlap validation, and the compact Overview layout on the live dataset.

### Date
[2026-04-22 19:55]

### Task
Connections polish: line consistency and selected target preview

### What changed
- Normalized additional-link line stroke styling so explicit links render with one consistent visual weight.
- Restyled the Primary parent Change action to match the Remove button family with a blue outline variant.
- Added a compact selected-target preview for Add additional link so the chosen node is visible before submission.

### What works now
- Additional links look visually consistent instead of mixing lighter and darker line appearances.
- Primary parent action styling now matches the surrounding control language in the Connections section.
- Add-link selection state is visible before clicking Add Link.

### Remaining issue / next step
- Run a focused manual UI check for explicit-link appearance and the selected-target preview on the live schematic.

### Date
[2026-04-22 21:35]

### Task
Universal node style system and Style tab milestone

### What changed
- Added a universal style preset library and per-node style preset reference in the editor state model.
- Reworked FacilitySchematicV2 so base node appearance resolves from style presets instead of hardcoded layer/type and alert-based rules.
- Added a separate Style tab in the single-select right panel for preset assignment, preset editing, and live preview.

### What works now
- Nodes can reference reusable style preset keys through the existing editor-state persistence layer.
- Style edits live-preview through the workbench across schematic surfaces while selection/focus/subtree remain overlay behavior.
- Alert-based node appearance is no longer part of the base facility renderer.

### Remaining issue / next step
- Run focused manual UI checks for Style tab flow, preset assignment, preset edit live preview, and overlay behavior on the live schematic.

### Date
[2026-04-23 21:46]

### Task
Sprint 2: Overview chart interaction optimization

### What changed
- Split FacilityWorkbench Overview so chart mode changes use a chart-only aggregate time-series reload instead of the full Overview reload path.
- Made baseline overlay a local chart visibility toggle backed by already loaded baseline points.
- Added a narrow chart cache keyed by aggregate selection scope, interval, and chart mode.

### What works now
- Headline KPI, Selection Scope, and Deviation / Baseline Detail remain on the stable Overview payload during chart-only interactions.
- The Main Time Series can refresh independently for Auto / 15min / Hourly / Daily changes.
- Baseline overlay no longer requests a full Overview recomputation.

### Remaining issue / next step
- Run manual UI checks to confirm only the Main Time Series area shows refresh behavior and that no visible Overview-core flicker remains in the live workbench.

### Date
[2026-04-24 15:30]

### Task
Sprint 4: Load Duration Curve implementation

### What changed
- Added CuratedSelectionLoadDurationCurvePoint model (DurationPercent 0-100, DemandKw).
- Added CuratedSelectionLoadDurationCurveSummary model with full state pattern (IsAvailable, State, StateReason, HasMixedSigns, Notes, EvaluationBasis, peak/average/point-count, Points list).
- Implemented BuildLoadDurationCurveSummary service method with explicit edge-case handling:
  - Returns unavailable for null series, insufficient points (<24 for 15-min or <8 for hourly), <24-hour intervals, generation-only profiles, or zero positive demand.
  - Uses demand-positive projection for mixed-sign (clamp to 0 kW, explicit note in summary).
  - Sorts aggregate demand descending, normalizes x-axis to 0-100% duration.
  - State set to Indicative for hourly granularity or broad intervals.
- Added GetMinimumLoadDurationCurvePointCount helper (24 for 15-min, 8 for hourly/default).
- Integrated LoadDurationCurve property into CuratedSelectionAggregateOverviewResult.
- Added loadDurationCurve building in aggregate builder (conditional on IncludePerformance option).
- Replaced Load Shape Snapshot widget in FacilityWorkbench Performance tab with Load Duration Curve panel.
- Added SelectionLoadDurationCurve property binding in FacilityWorkbench component.
- Updated NodeAnalyticsPreviewService string formatting to use ToString("N2", CultureInfo.InvariantCulture) instead of non-existent FormatMetricValue method.

### What works now
- Load Duration Curve appears in the secondary Performance slot with title, evaluation basis, state badge, point count, and peak/average kW metrics.
- Mixed-sign datasets show demand-positive projection with explicit badge and summary note.
- Consumption-only profiles render curve normally.
- Generation-only and insufficient-data scenarios show unavailable with clear reasoning.
- Edge cases (null series, sparse data, <24h intervals) are explicitly handled with appropriate state and messaging.
- Performance tab remains compact with coherent KPI story (Peak Demand, Load Factor, After-hours Load as primary, Load Duration Curve as secondary context).

### Remaining issue / next step
- Run manual UI testing to verify Load Duration Curve displays in secondary slot and replaces Load Shape Snapshot.

### Date
[2026-04-25 14:00]

### Task
Sprint 5 repair pass: hover card, viewport, interval clarity, filter compactness

### What changed
- **Hover card dedup**: Removed SVG `<title>` element from FacilitySchematicV2.razor to eliminate the browser native tooltip that was appearing simultaneously with the custom JS hover card.
- **Hover card style preset**: Added `data-node-preset` attribute to each schematic `<g>` element via `ResolveNodeStylePresetKey()`. Added a "Style" row to the hover card HTML in editor.js.
- **Viewport on Close focus view**: Fixed `ToggleSchematicFullscreen()` — the leaving-fullscreen branch now calls `GoHomeAsync()` (C# method with pan-to-root logic) instead of the raw `editorCanvas.goHome` JS call, so the viewport correctly resets when the user closes the focus view.
- **Interval date clarity**: Made date and time inputs appear side by side (`.cp-date-time-row` flex row) for each From/To column instead of stacked. Added a small `cp-custom-date-confirm` label below each pair showing the confirmed datetime in `dd.MM.yyyy HH:mm UTC` format. Changed the hint text to `DD.MM.YYYY · HH:MM · UTC` so the expected format is explicit.
- **Selection Filters compact**: Removed the `_availableTagChips` chip display block from the Selection Filters tab. The tag text input is still present; the chip row was causing vertical overflow and pushing the action buttons out of view.

### What works now
- Schematic hover card shows cleanly without duplicate browser-native tooltip.
- Hover card includes Style preset name for each node.
- Closing focus view resets the viewport to the root node view (same as pressing Home).
- Custom interval section is more compact; date/time inputs sit on one row per endpoint; confirmed parsed datetime shown in unambiguous DD.MM.YYYY format.
- Selection Filters tab fits its content without overflow scroll, keeping the Apply/Reset action row always visible.

### Remaining issue / next step
- Run browser validation at 1920×1080 to confirm layout fits without page scroll and all 5 repair items are visually correct.
- Verify consumption-only curve renders and mixed-sign wording is clear.
- Verify generation-only shows unavailable with demand-focused message.
- Confirm Performance tab responsive behavior on <900px breakpoint.
- Validate build completes cleanly after stopping debugger (current build blocked by DLL file lock from running debugger).

### Date
[2026-04-22 22:20]

### Task
Style tab follow-up: safe preset deletion

### What changed
- Added a Delete preset action to the Style tab.
- Blocked deletion for the default fallback preset and for presets still used by persisted node assignments.
- Added clear user-facing block reasons and refreshed the Style tab state after successful deletion.

### What works now
- Delete preset is disabled when deletion is not allowed.
- Successful preset deletion persists through the existing preset library save path and updates the current Style tab selection cleanly.
- Blocked deletion explains whether the preset is protected as default or still used by nodes.

### Remaining issue / next step
- Run manual UI checks for delete-disabled states, blocked-delete explanations, and successful preset removal in the live Style tab.

### Date
[2026-04-23 13:55]

### Task
Final FacilityWorkbench cleanup bundle

### What changed
- Removed the compare-set capacity limit and rebuilt the Compare tab around a sidebar plus chart layout with removable compare-set entries.
- Compacted the analytics navigation shell into a smaller tab strip and removed the large analytics header panel.
- Translated the remaining active-workbench UI copy in FacilityWorkbench to English, including editor actions, layout controls, discard guard text, status messages, and undo labels.
- Translated the remaining active analytics strings in NodeAnalyticsPreviewService, including EMS evaluation text, baseline-reference labels, baseline overlay fallback messages, and aggregate headline copy.

### What works now
- Compare accepts any number of dataset-backed nodes and no longer blocks additions at four items.
- The active workbench path is English-only in the touched page/service slices and no longer surfaces the previous mojibake in EMS or baseline overlay messages.
- The compact analytics shell and dedicated compare layout compile successfully in the validated alternate build output.

### Remaining issue / next step
- Run focused manual UI checks for Compare tab density on desktop/mobile, remove-from-compare interactions, and the updated EMS/baseline English copy on live data.

### Date
[2026-04-23 01:15]

### Task
FacilityWorkbench follow-up: dataset-backed compare support and active-path language cleanup

### What changed
- Removed the remaining hardcoded compare-node support rule and delegated compare eligibility to current dataset capability through the analytics preview service.
- Updated Compare empty states and selection-set status messaging so unsupported selections now explain the lack of dataset-backed compare capability in English.
- Translated the active analytics-facing copy in FacilityWorkbench, FacilityTimeSeriesPanel, FacilityCompareTimeSeriesPanel, and the main visible NodeAnalyticsPreviewService paths, including baseline/deviation failures, weather explanation copy, focused-node summary labels, granularity interpretation notes, and reduced-source fallback messages.

### What works now
- Compare availability follows the current analytics dataset instead of the old fixed node list.
- If no compare-compatible nodes exist, the workbench shows an English empty state instead of relying on legacy assumptions.
- The touched active analytics/workbench presentation path now renders English copy in the main compare, overview, deviation, and time-series flows.

### Remaining issue / next step
- Run manual UI verification on the live workbench for Compare, Overview baseline/deviation details, and any still-untranslated editor-only surfaces outside the analytics-focused path.

### Date
[2026-04-23 15:10]

### Task
FacilityWorkbench Performance tab: KPI-first demand slice

### What changed
- Reworked the Performance tab around explicit demand KPIs: Peak Demand, Load Factor, and After-hours Load.
- Kept Peak Analysis as the supporting peak-detail surface.
- Replaced the previous Operating Regime-first presentation in the active Performance tab with a schedule-based after-hours / night / weekend demand panel.
- Demoted Load Profile into a smaller load-shape snapshot so it no longer defines the main Performance story.

### What works now
- The Performance tab stays inside the existing lazy tabbed FacilityWorkbench structure.
- Peak demand is visible as a headline KPI with Peak Analysis still available underneath.
- Load factor and after-hours demand now have explicit service summaries and dedicated UI cards.

### Remaining issue / next step
- Run manual UI verification for the new Performance layout on representative consumption-only, mixed-sign, generation-only, short-interval, and no-data selections.

### Date
[2026-04-23 18:10]

### Task
FacilityWorkbench Phase 1 stabilization implementation

### What changed
- Reworked Overview detail behavior so aggregate analytics scopes now show a dedicated Selection Scope card instead of falling back to misleading structural-only focus detail.
- Changed custom interval entry to an explicit day-first date format with consistent parsing, normalization, and aggregate-scope validation messaging.
- Tightened the desktop workspace/control-panel layout so the default Overview wastes less vertical space.

### What works now
- Aggregate Overview semantics, aggregate preset anchoring, and aggregate custom-interval validation follow the resolved analytics scope more consistently.
- Custom date entry no longer depends on browser locale behavior.
- The default desktop shell is more compact while keeping the existing tabbed workbench structure.

### Remaining issue / next step
- Run focused manual UI verification for structural aggregate focus, custom day-first date entry, aggregate interval overlap validation, and the 1920x1080 no-scroll target.

### Date
[2026-04-24 15:12]

### Task
Sprint 3: Performance KPI validation and hardening

### What changed
- Added an explicit KPI state model for the active Performance tab summaries so Peak Demand, Load Factor, and After-hours Load can explain when they are available, indicative, or unavailable.
- Decoupled Performance KPI evaluation from the main chart's daily aggregation by resolving a separate performance evaluation series that stays at 15-minute or hourly granularity.
- Hardened load factor and after-hours summaries for mixed-sign selections, generation-only selections, very short intervals, broad intervals, and low-demand schedule-reference cases.
- Updated the active FacilityWorkbench Performance UI so demand-focused mixed-sign behavior, intentional generation-only unavailability, and schedule-heuristic wording are visible to the user.

### What works now
- Demand KPI no longer silently inherit daily chart aggregation for long intervals in the active Performance slice.
- Mixed-sign selections surface as demand-focused projections instead of reading like generic net-load KPI.
- Generation-only selections keep intentional unavailable states for demand KPI with clearer wording.
- After-hours / Night / Weekend reads as schedule heuristic v1 and can suppress thin night/weekend ratios instead of overstating them.

### Remaining issue / next step
- Run focused manual UI verification on representative consumption-only, mixed-sign, generation-only, short-interval, and long-interval selections in the live FacilityWorkbench dataset.

### Date
[2026-04-24 16:30]

### Task
Sprint 4 repair: Load Duration Curve rendering path

### What changed
- Added a dedicated FacilityLoadDurationCurvePanel component that renders LDC points through the existing JS chart runtime.
- Extended facilityTimeSeriesChart.js with renderLoadDuration for numeric duration-axis (0-100%) and demand-axis (kW) rendering.
- Wired the new panel into the existing available-state branch of the Performance tab LDC widget in FacilityWorkbench.

### What works now
- Available LDC states now have a real chart host/container and invoke chart rendering instead of showing text-only output.
- Existing LDC state badges, mixed-sign wording, and unavailable text-only branches remain intact.

### Remaining issue / next step
- Run manual UI verification for available, mixed-sign, generation-only, and insufficient-data selections in the live Performance tab.
