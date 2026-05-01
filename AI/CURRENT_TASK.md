# CURRENT_TASK.md

## Goal
Simulate the state as if the current thesis dataset CSV files had been imported manually, by migrating active seeded bindings into app-local imported storage and removing runtime dependence on external dataset disk paths.

## Problem
The intended final product should run on hosted server-side .db files and uploaded CSV runtime data only.
However, the current runtime still appears to mix:
- seeded bindings resolved from external dataset paths
- app-local imported bindings stored under App_Data/facility-imports

We need to use the existing import/binding model to migrate the currently used seed-bound CSV files into app-local storage.

## Desired direction
Implement a narrow one-time migration milestone only.
Do not redesign the architecture.
Do not implement users/auth yet.
Do not implement multi-facility hardening yet.

## Scope
Implementation only.

Read first:
- AI/AGENT_CONTEXT.md
- AI/CURRENT_TASK.md
- AI/WORKLOG.md
- AI/DATA_VISUALIZATION_AUDIT.md

Then inspect at minimum:
- DiplomovaPrace/Services/FacilityDataBindingRegistry.cs
- DiplomovaPrace/Services/FacilityNodeSeriesImportService.cs
- DiplomovaPrace/Services/FacilityEditorStateService.cs
- DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs
- DiplomovaPrace/Program.cs
- DiplomovaPrace/appsettings.json
- DiplomovaPrace/appsettings.Local.json
- any binding/state types needed for imported binding persistence

## Required implementation
1. Enumerate currently active seeded bindings for the current active facility/runtime.
2. For each seeded binding:
   - resolve the current source CSV file using the existing seeded binding path resolution
   - convert/copy it into the same app-local imported storage format used by manual node CSV import
   - persist imported binding metadata in the same way as manual import
3. Only after successful per-binding migration, suppress/tombstone the old seeded binding.
4. Produce a migration result summary:
   - migrated
   - skipped
   - failed
   - unresolved source paths
5. Validate that runtime analytics still works after migration.
6. If safe, perform one verification run with external dataset binding paths disabled/empty (or otherwise clearly bypassed) to confirm hosting-readiness direction.

## Do NOT change
- Do not redesign the whole data architecture
- Do not implement multi-facility support yet
- Do not implement users/auth yet
- Do not change FacilityWorkbench UI
- Do not broaden into deployment implementation yet

## Constraints
- Keep the migration narrow and reversible
- Use the existing app-local import/binding persistence model whenever possible
- Do not tombstone old bindings before successful imported replacement exists
- Build must pass
- Update AI/WORKLOG.md after implementation

## Guardrails
- This is a one-time dataset-decoupling milestone, not a broad data rewrite
- Focus on removing runtime dependency on external dataset disk paths
- If any binding cannot be migrated cleanly, stop and report it explicitly