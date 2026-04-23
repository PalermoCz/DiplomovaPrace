# Worklog

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
