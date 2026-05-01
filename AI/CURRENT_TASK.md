# CURRENT_TASK.md

## Goal
Clean up the local project file clutter safely without losing the currently restored working schematic or the restored CSV-backed data bindings.

## Problem
The richer schematic and bindings are now working again, but the repository/project folder is cluttered with many temporary forensic, recovery, export, and backup files.

We must clean up the mess safely.

Critical rule:
The currently working schematic and restored data bindings must NOT be lost again.

## Current known working runtime core
Treat these as critical runtime assets unless proven otherwise:
- `DiplomovaPrace/metering.db`
- `DiplomovaPrace/facility-editor-state.json`
- `DiplomovaPrace/App_Data/facility-imports/`

These must be preserved.

## Likely cleanup candidates
Examples currently visible in the repo/project folder:
- helper scripts:
  - `add_membership.py`
  - `check_db.py`
  - `check_git_db.py`
  - `check_null_types.py`
  - `check_users.py`
  - `get_user.py`
  - `reconstruct_added_nodes.py`
  - `validate_added.py`
- export/temp forensic files:
  - `combined_db_export.json`
  - `ddrive_combined_db_export.json`
  - `db_export/...`
  - `db_export/ddrive_temp_db/...`
- safety backups / restore snapshots:
  - `facility-editor-state.pre-binding-fix.json`
  - `facility-editor-state.pre-importedBindings-restore.json`
  - `facility-editor-state.pre-restore-20260501.json`
  - `metering.db.backup-pre-ddrive-restore`

## Desired direction
Do a safe cleanup only.
Do NOT implement new features.
Do NOT touch graph logic or auth logic unless required for cleanup safety.

## Scope
Implementation only.

Read first:
- AI/AGENT_CONTEXT.md
- AI/CURRENT_TASK.md
- AI/WORKLOG.md

Then inspect at minimum:
- current repo/project file tree
- current runtime file paths actually used by the app
- current working DB/editor-state/imports state

## Required work
1. First create a **golden snapshot** of the currently working runtime state in a safe archive location.
   At minimum snapshot:
   - `DiplomovaPrace/metering.db`
   - `DiplomovaPrace/facility-editor-state.json`
   - `DiplomovaPrace/App_Data/facility-imports/`
2. Then classify current clutter into:
   - KEEP
   - ARCHIVE
   - DELETE
3. Perform cleanup safely:
   - preserve critical runtime files
   - move useful recovery artifacts to an archive folder if they may still be valuable
   - delete only clearly temporary/helper/export clutter
4. Do NOT delete the recovered working graph/data state.
5. After cleanup, verify runtime:
   - app starts
   - login still works
   - facility schematic still renders
   - bindings/data still appear on nodes

## Do NOT change
- Do not alter the currently restored graph content
- Do not alter imported binding behavior unless required for cleanup safety
- Do not start a new feature milestone
- Do not delete D-drive source artifacts blindly if they might still be the only provenance backup

## Constraints
- Safety first
- Snapshot first, cleanup second
- Runtime verification required after cleanup
- All user-facing text must remain English only

## Guardrails
- If any file is uncertain, archive it instead of deleting it
- Prefer moving uncertain artifacts to an archive folder over hard deletion
- Report exactly what was kept, archived, and deleted