# DiplomovaPrace - Agent Context

## Project identity
- This is a diploma thesis project and related application.
- The application is a facility graph / schematic / analytics workbench.
- It is not just a dashboard over CSV files.

## Current technical state
- Runtime source of truth for graph is DB (SQLite via EF Core).
- CSV files are seed/import source, not runtime graph truth.
- facility-editor-state.json can structurally override parents and edges.
- The current graph model is tree-first for layout, but explicit edges exist.
- One primary layout parent plus explicit non-layout additional links.
- Secondary/non-layout edges are important and must be preserved.

## Imported dataset state
- Full validated author dataset has already been imported successfully.
- Nodes: 116
- Meter nodes: 81
- Schematic edges: 137
- Non-layout edges: 22
- Bindings: 1603
- V.Z82 exists
- WeatherStation.Weather exists
- Measurement loading now resolves from external dataset root, not only old aggregate CSV files.

## Data source locations
- Project root:
  C:\Users\Matthew\Desktop\Diplomova_Prace\Code\DiplomovaPrace
- Validated dataset metadata:
  D:\DataSet\Script2
- Raw data:
  D:\DataSet\data
- Heavy local artifacts / DB should stay on D:\DataSet when possible.

## Graph/editor principles
- Topology != layout.
- Layout changes must not silently rewrite topology.
- Editor behavior should stay general and future-safe.
- Do not introduce building-specific UI logic like Electricity/HVAC global view modes.
- Drag and grid must be editor-only.
- Save flow must go through the existing dirty/save logic (orange diskette).
- Multi-selection and single-selection must be mutually exclusive.
- If multiple nodes are selected:
  - single-node panel must not remain active
  - only multi-selection layout tools should be shown
  - ambiguous single-node actions should be disabled
  - global meaningful actions should remain active

## Long-term product direction
- Long-term goal is manual authoring/editing of tree/graph in the app.
- Each node should eventually support attached CSV or DB data source.
- Current full dataset import is a temporary bypass so the graph does not need to be entered manually.
- Backward compatibility is not a priority.
- If something conflicts with the final editor vision, it can be removed or refactored.

## Preferred AI workflow
- Always work plan-first.
- Keep prompts concise and delta-based.
- Reuse this file instead of repeating large context in every prompt.
- Prefer:
  1. plan
  2. implementation
  3. validation
- Avoid unnecessary parallel workflows.
- Avoid building-specific hacks.
- Prefer minimal, robust, general solutions.

## Communication/output expectations for future agents
- First read AGENT_CONTEXT.md, CURRENT_TASK.md, and recent WORKLOG.md entries.
- Do not guess.
- Verify from code before changing behavior.
- Propose minimal changes first.
- Do not mix audit, implementation, and validation into one uncontrolled giant run.
- Keep outputs concise and practical.


## Editor UX rules
- Single selection and multi-selection must be mutually exclusive.
- If exactly one node is selected:
  - show only the single-node editor tabs/panels
  - visible tabs should be: Edit, Add node, Delete, Import, Style
  - do not show Layout tools in single-select mode
- If multiple nodes are selected:
  - clear single-node focus
  - show only the Layout tools panel
  - Layout tools should occupy the full right-side panel area
  - hide single-node tabs/panels in multi-select mode
  - disable ambiguous single-node actions such as Add node, Delete, and Import if they require a single-node target
  - keep globally meaningful actions active
- Rectangle selection must only select nodes actually inside the selection rectangle.
- Multi-selection must not highlight unrelated nodes outside the selected set.
- Group drag must work by dragging any node that belongs to the selected group.
- Group drag, align, and distribute must snap to the editor grid.
- Grid must be visible only in editor mode.
- Grid should be effectively unbounded from the user perspective and must not artificially limit node placement.
- Entering editor mode should initialize the viewport in the same logical position as Home.
- Home must focus the primary external/top root node of the graph, not a generic facility wrapper/container node.
- Layout changes must go through the existing dirty/save flow and activate the orange save diskette.
- In single-select mode, clicking a node should automatically switch the right panel to Edit.
- Group drag must work without holding any modifier key:
  if multiple nodes are selected and the user drags one of the selected nodes, the whole selected group must move.



## AI workflow mode policy
- Default workflow is plan-first.
- Use Plan mode for new tasks, repo audit, root-cause analysis, scope clarification, and implementation planning.
- Use Agent mode only after the plan is explicitly approved.
- Prefer:
  1. Plan mode
  2. plan approval
  3. Agent mode implementation
  4. validation
- Keep planning concise and avoid unnecessary over-exploration or parallel research.


## Current milestone status
- Full dataset import is working.
- Binding-based data loading is working.
- Editor-only grid/drag/save flow is working.
- Single-select and multi-select editor behavior is functional.
- Group drag is working.
- Relationship editing UI for additional links is implemented.
- Universal node style preset system is implemented.
- Style tab with live preview is implemented.
- FacilityWorkbench analytics have been split into tabs.
- Topbar alert dropdown was removed.
- Current active milestone is data presentation cleanup and literature-backed analytics design.

## Graph relationship architecture
- Do not model the graph as a true multi-parent layout tree.
- Prefer exactly one primary layout parent per node.
- Preserve additional parent-like relationships as explicit non-layout edges.
- Keep layout structure, topology, and selection semantics as separate concerns.
- UI should treat non-layout relationships as one generic additional-link concept unless a later milestone introduces a strong reason to distinguish kinds again.

## Relationship editing status
- Relationship inspection UI was a transitional/debugging step.
- Relationship editing for additional links is now implemented.
- Primary layout parent editing and additional-link editing must remain separate actions.


## Node appearance direction
- Node appearance should be driven by a universal style preset system, not dataset-specific hardcoded layer/type rules.
- Style editing should live in a separate Style editor surface, not in the normal single-node Edit form.
- Live preview is required.
- Alert-based appearance is not part of the intended base style model.
- Selection, focus, and subtree highlight should remain overlay states separate from base node appearance.


## Data presentation direction
- FacilityWorkbench is the primary future product surface for analytics and data display.
- Older pages such as DashboardView, KpiView, BuildingView, and EditorView should be treated as legacy/reference only.
- The workbench analytics area should move toward a cleaner tab-based structure.
- Prefer a smaller default analytics surface and lazy/conditional loading for secondary tabs.
- Future visualization work should be literature-backed and prioritized.
- Preferred candidate directions currently include:
  - benchmark / peer comparison
  - load duration curve
  - scatter: temperature vs load
  - baseline vs actual
  - peak demand / load factor / after-hours load
  - EUI / cost per m²
- Do not add new analytics opportunistically; first keep the workbench information architecture coherent.



  
