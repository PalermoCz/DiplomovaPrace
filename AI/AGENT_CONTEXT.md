## DiplomovaPrace - Agent Context

### Project identity
- This is a diploma thesis project and related application.
- The application is a facility graph / schematic / analytics workbench.
- It is not just a dashboard over CSV files.
- The main active product surface is FacilityWorkbench.
- Legacy pages are reference/legacy surfaces unless explicitly stated otherwise.

### Current product truth
- FacilityWorkbench is the primary user-facing product surface.
- The intended product direction is facility-first, not legacy building/dashboard-first.
- Older pages such as DashboardView, KpiView, BuildingView, and EditorView should be treated as legacy/reference only unless a specific task proves they still provide required value.
- ImportView should be treated as an operational/admin utility surface, not a peer primary product surface.
- Do not present legacy pages as equal peers to FacilityWorkbench in normal product-facing navigation.
- Prefer a single clear primary entry surface over multiple parallel product shells.

### Current technical truth
- Runtime source of truth for the graph is SQLite via EF Core.
- CSV files are seed/import sources, not runtime graph truth.
- facility-editor-state.json can structurally override parents and edges.
- The graph model is tree-first for layout, with explicit non-layout edges preserved separately.
- Keep one primary layout parent per node.
- Preserve additional/non-layout relationships as explicit edges.
- Topology and layout are separate concerns.
- Layout changes must not silently rewrite topology.

### Imported dataset state
- Full validated author dataset has already been imported successfully.
- Nodes: 116
- Meter nodes: 81
- Schematic edges: 137
- Non-layout edges: 22
- Bindings: 1603
- V.Z82 exists
- WeatherStation.Weather exists
- Measurement loading resolves from the external dataset root, not only old aggregate CSV files.

### Data source locations
- Project root:
  C:\Users\Matthew\Desktop\Diplomova_Prace\Code\DiplomovaPrace
- Validated dataset metadata:
  D:\DataSet\Script2
- Raw data:
  D:\DataSet\data
- Heavy local artifacts / DB should stay on D:\DataSet when possible.

### Current active FacilityWorkbench shell
- The top area is a compact facility workbench control surface, not a legacy dashboard shell.
- The expected compact top structure is:
  - Interval
  - Selection
  - Tools
- The main lower analytics shell is:
  - Overview
  - Analysis
- Overview is the executive summary surface.
- Analysis is the focused tool workspace.
- Do not reintroduce older tab-heavy lower IA as the main product shell.

### Current analytics direction to preserve
- Overview should stay visually light and executive-like:
  - KPI strip
  - one main chart
  - contributors treemap
- Analysis should stay modular and tool-like:
  - exact signal selector
  - one active module at a time
- Preferred Analysis modules already integrated or targeted:
  - Trend
  - Baseline
  - Scatter
  - Power
  - EUI
- Net / Consumption / Production belongs to Overview.
- Detail analytics in Analysis should use a consumption-oriented basis where relevant.
- Do not turn Overview into a long stacked dashboard again.
- Do not turn Analysis into a long stacked list of all modules at once.
- Prefer fewer stronger surfaces over many weak parallel panels.

### Current editor / schematic direction to preserve
- FacilityWorkbench is also the main active schematic/editor surface.
- Drag-and-drop must be editor-only.
- Grid must be visible only in editor mode.
- Save flow must go through existing dirty/save logic (orange diskette).
- Single selection and multi-selection must be mutually exclusive.
- If exactly one node is selected:
  - show only the single-node editor tabs/panels
  - visible tabs should be: Edit, Add node, Delete, Import, Style
  - do not show Layout tools in single-select mode
- If multiple nodes are selected:
  - clear single-node focus
  - show only the Layout tools panel
  - hide single-node tabs/panels
  - disable ambiguous single-node actions
- Rectangle selection must only select nodes actually inside the rectangle.
- Group drag must work by dragging any node that belongs to the selected group.
- Group drag, align, and distribute must snap to the editor grid.
- Home must focus the primary external/top root node, not a generic wrapper/container.

### Node appearance and relationship architecture
- Node appearance should be driven by a universal style preset system, not dataset-specific hardcoded rules.
- Style editing belongs to a dedicated Style surface, not mixed into the normal Edit form.
- Live preview is required.
- Selection, focus, and subtree highlight are overlays separate from base node appearance.
- Do not model the graph as a true multi-parent layout tree.
- Prefer one primary layout parent plus explicit additional links.
- UI should treat non-layout relationships as generic additional links unless a later milestone introduces a strong reason to distinguish kinds again.

### Legacy status and deletion policy
- Legacy pages/components/services should not be promoted back into the primary product flow unless explicitly approved.
- If a legacy part is not needed for the final direction, deletion is allowed.
- Backward compatibility is not a priority if it conflicts with the intended final direction.
- Before deleting legacy code, inspect dependencies first:
  - routes
  - page references
  - component references
  - service registrations in Program.cs
  - any usage from active FacilityWorkbench paths
- Prefer removing truly redundant legacy surface area over keeping confusing parallel shells indefinitely.

### Current milestone interpretation
- The application is already in a late-stage finalization phase.
- The broad facility-first product direction is already decided.
- Broad redesign should be strongly resisted unless there is a very strong reason.
- Remaining work is likely in this family:
  - final product-surface cleanup
  - remaining narrow UX/polish fixes
  - authentication / login
  - user-owned facilities
  - invitation model
  - facility-scoped roles
  - facility ownership / multi-building support
  - deployment / publish / hosting decisions
  - large data / large DB handling
  - later thesis-writing support
- Do not assume that more UI redesign is the highest-value next step.

### Planning / implementation policy
- Default workflow is plan first.
- Use audit/planning mode for:
  - repo/context recovery
  - root-cause analysis
  - scope clarification
  - architecture/security/auth/data/hosting decisions
  - deletion-feasibility decisions
- Use implementation mode only after the plan is explicitly approved.
- Do not mix audit, redesign, and implementation into one uncontrolled run.
- For architecture/security/auth/ownership/deployment decisions, stop coding momentum and recommend a planning/design milestone first.

### Context hierarchy
Use this priority order when reasoning:
1. The user’s latest message in the current conversation
2. Real observed repo/app behavior
3. AGENT_CONTEXT.md
4. CURRENT_TASK.md as the current narrow task only
5. Other explicit planning/context documents if provided
6. WORKLOG.md as historical signal only

Important:
- CURRENT_TASK.md is not global truth.
- WORKLOG.md is not source of truth.
- If sources conflict, prefer the user’s latest message and the observed current code/app state.

### Risky implementation points
- FacilityWorkbench.razor is a sensitive integration point.
- FacilityWorkbench.razor.css is a sensitive integration point.
- FacilityTimeSeriesPanel.razor is a sensitive integration point.
- facilityTimeSeriesChart.js is a sensitive integration point.
- Giant multi-file patches can easily corrupt:
  - Razor markup structure
  - CSS block boundaries
  - JS chart contracts
- Prefer small validated patches.
- Build after risky changes.
- For active FacilityWorkbench UI work, tightly scoped changes are preferred over giant agent rewrites.

### Communication/output expectations for future agents
- First read AGENT_CONTEXT.md, CURRENT_TASK.md, and recent WORKLOG.md entries.
- Do not guess.
- Verify from code before changing behavior.
- Keep outputs concise, practical, and critical.
- If something is uncertain, say so explicitly.
- Prefer the smallest meaningful next step.
- If the next problem is actually a design decision, say so clearly instead of forcing implementation.