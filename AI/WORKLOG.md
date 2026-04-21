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
