# Worklog

---

### Date
[2026-05-01]

### Task
FacilityMembership milestone v1 — facility-scoped membership + minimal role gating

### What changed

**New persistence role model:**
- Added `DiplomovaPrace/Persistence/FacilityMembershipRole.cs`.
- Roles: `Owner`, `Admin`, `Viewer`.
- Added helper methods:
  - `ParseOrViewer(...)` for safe role parsing
  - `CanUseEditor(...)` for Owner/Admin edit gating

**New membership table entity:**
- Added `DiplomovaPrace/Persistence/FacilityMembershipEntity.cs`.
- Fields:
  - `Id` (PK)
  - `FacilityId` (FK -> Facilities)
  - `AppUserId` (FK -> AppUsers)
  - `Role` (string source-of-truth for facility role)
  - `CreatedAtUtc`

**Entity/navigation updates:**
- `DiplomovaPrace/Persistence/AppUserEntity.cs`
  - added `FacilityMemberships` collection.
- `DiplomovaPrace/Persistence/Schematic/FacilityEntity.cs`
  - added `FacilityMemberships` collection.

**EF model updates:**
- `DiplomovaPrace/Persistence/AppDbContext.cs`
  - added `DbSet<FacilityMembershipEntity> FacilityMemberships`.
  - configured table `FacilityMemberships` with:
    - unique index: `(FacilityId, AppUserId)`
    - index: `AppUserId`
    - required `Role` (max length 32)
    - FK cascade rules to `Facilities` and `AppUsers`.

**Schema bootstrap for existing SQLite DBs:**
- `DiplomovaPrace/Persistence/AppDbSchemaBootstrap.cs`
  - added `EnsureFacilityMembershipSchemaAsync(...)`.
  - creates `FacilityMemberships` table and indexes via `CREATE ... IF NOT EXISTS`.

**Startup wiring:**
- `DiplomovaPrace/Program.cs`
  - registered `FacilityMembershipService`.
  - calls `AppDbSchemaBootstrap.EnsureFacilityMembershipSchemaAsync(db)` during startup initialization.

**Runtime membership resolution + default bootstrap behavior:**
- Added `DiplomovaPrace/Services/FacilityMembershipService.cs`.
- Added `ResolveForUserAndFacilityAsync(appUserId, facilityId)`:
  - resolves current user membership for active facility.
  - bootstrap default behavior:
    - if facility has no memberships yet -> create `Owner` for current user
    - otherwise, if current user has no membership -> create `Viewer` for current user
  - returns resolved role and whether bootstrap was applied.

**FacilityWorkbench role-gated runtime behavior:**
- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`
  - injects `FacilityMembershipService` and `AuthenticationStateProvider`.
  - during `ReloadData(...)`, resolves current signed-in user ID claim + membership for current active facility.
  - editor permission now derived from membership role (`Owner/Admin` editable, `Viewer` read-only).
  - Edit schematic buttons are disabled for read-only role.
  - `ToggleEditMode()` now hard-blocks editor entry for read-only role and sets status message.
  - existing node edit checks now include role gate via `CanCurrentUserEditFacility`.

### What was NOT changed (per scope)
- No invitation flow.
- No facility member management UI.
- No ownership transfer feature.
- No FacilityWorkbench redesign.
- No broader auth/policy system.

### Database changes
- Added logical schema for table: `FacilityMemberships`.
- Applied through startup bootstrap SQL (`CREATE TABLE IF NOT EXISTS ...`) to support current EnsureCreated-based workflow without migration reset.

### Build
- `dotnet build` successful: 0 errors.

### Runtime sanity check
- `dotnet run --project DiplomovaPrace` startup successful.
- Verified logs:
  - FacilityMembership schema bootstrap executed (`CREATE TABLE IF NOT EXISTS FacilityMemberships` + indexes)
  - app listening on `http://localhost:5016`
  - host started without runtime exceptions.

### What remains for the next milestone
1. Membership management UI (list/add/remove members).
2. Invitation flow.
3. Explicit ownership/admin transfer mechanics.
4. Richer policy/authorization integration beyond basic editor gating.
5. Optional route-level access denied behavior based on facility membership.

---

### Date
[2026-05-01]

### Task
Auth-shell v1 milestone — local email + password, cookie authentication, account UI

### What changed

**New persistence entity — `DiplomovaPrace/Persistence/AppUserEntity.cs`:**
- Single-table user model for auth-shell v1.
- Fields: `Id` (PK), `Email` (unique), `PasswordHash` (PBKDF2), `CreatedAtUtc`, `LastLoginUtc`.
- Minimal scope: no facility ownership, roles, or memberships yet.

**Updated `DiplomovaPrace/Persistence/AppDbContext.cs`:**
- Added `DbSet<AppUserEntity> AppUsers` property.
- Added EF configuration for AppUsers table with unique index on `Email`.

**New auth service — `DiplomovaPrace/Services/AuthenticationService.cs`:**
- `HashPassword(string)` — generates random salt + PBKDF2 (SHA256, 10000 iterations).
- `VerifyPassword(string, string)` — constant-time comparison.
- No external dependencies (uses System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2).

**Updated `DiplomovaPrace/Program.cs`:**
- Registered `AuthenticationService` as scoped service.
- Added cookie authentication middleware: `.AddAuthentication("CookieAuth").AddCookie(...)`.
  - Login path: `/login`
  - Logout path: `/logout`
  - Cookie lifetime: 7 days with sliding expiration
- Added `app.UseAuthentication()` and `app.UseAuthorization()` in middleware pipeline.

**New auth pages:**
- **`DiplomovaPrace/Components/Pages/Login.razor`** (`@page "/login"`):
  - Plain HTML form (email + password).
  - POST handler validates credentials, updates `LastLoginUtc`, issues cookie.
  - Redirects to `/facility` on success.
  - Shows inline error messages.
  - Link to `/register`.
  - `@attribute [AllowAnonymous]` — accessible to unauthenticated users.

- **`DiplomovaPrace/Components/Pages/Register.razor`** (`@page "/register"`):
  - Plain HTML form (email + password + confirm).
  - POST handler validates input, checks email uniqueness, hashes password.
  - Auto-signs in new user and redirects to `/facility`.
  - Shows inline error messages.
  - Link to `/login`.
  - `@attribute [AllowAnonymous]`.

- **`DiplomovaPrace/Components/Pages/Logout.razor`** (`@page "/logout"`):
  - Simple redirect handler.
  - Signs out via `HttpContext.SignOutAsync("CookieAuth")`.
  - Redirects to `/login`.
  - `@attribute [Authorize]` — requires authentication.

**Updated `DiplomovaPrace/Components/Routes.razor`:**
- Changed `<RouteView>` to `<AuthorizeRouteView>` for automatic unauthenticated user redirection to `/login`.
- Added `@using Microsoft.AspNetCore.Components.Authorization`.

**Updated `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Added `@attribute [Authorize]` — FacilityWorkbench is now protected.
- Added `@using Microsoft.AspNetCore.Authorization`.

**Updated `DiplomovaPrace/Components/Layout/FacilityTopbar.razor`:**
- Added right-side account chip section.
- Uses `<AuthorizeView>` component to conditionally show:
  - **Authenticated:** Current email + "Odhlásit" (Logout) link (styling: red/error theme).
  - **Anonymous:** "Přihlas" (Login) link (styling: cyan/info theme).
- Links use simple navigation (`href="/logout"`, `href="/login"`).

### What was NOT changed (per scope)
- No FacilityMembership model yet (reserved for future role/ownership work).
- No password reset, email verification, MFA, SSO, external providers.
- No facility admin pages or invitation system.
- No redesign of FacilityWorkbench itself.
- FacilityLayout and other pages remain intact.

### Database changes
- New table: `AppUsers` (schema auto-created by EF Core on first run via `EnsureCreatedAsync`).
- No migration file generated yet; `EnsureCreatedAsync` is used for development convenience.
- For production, migrate to explicit migrations: `dotnet ef migrations add InitialAuth`.

### Build
- `dotnet build` — successful: 0 errors.

### Feature validation checklist
- ✅ Build passes with no errors.
- ✅ /register page: accepts email + password, creates user, hashes password securely, auto-signs in.
- ✅ /login page: validates credentials, updates LastLoginUtc, issues cookie.
- ✅ /logout page: signs out, redirects to login.
- ✅ /facility requires authentication — unauthenticated users redirected to /login.
- ✅ FacilityTopbar shows account email and logout link for authenticated users.
- ✅ Cookie expires in 7 days; sliding expiration is enabled.
- ✅ Password hashing is secure (PBKDF2, salt, constant iterations).

### What remains for the next milestone
1. **FacilityMembership table** — one-to-many relationship between AppUser and Facility.
2. **Facility ownership assignment** — which user owns/manages which facility.
3. **Facility membership UI** — list, add, remove members; assign roles.
4. **Role model** — Admin, Editor, Viewer roles (or equivalent).
5. **Role-based access control (RBAC)** — check user role when accessing facility data.
6. **Password reset flow** — email-based or security questions.
7. **Email verification** — optional for MVP, but blocks certain operations.
8. **Invitation system** — admin invites new users to facility.
9. **DB migrations** — move from `EnsureCreatedAsync` to formal migration workflow.
10. **Secrets management** — store session keys, email credentials in secure vault.

---

### Date
[2026-05-01]

### Task
Post-migration cleanup — hosting-path hardening

### What changed

**`DiplomovaPrace/appsettings.Local.json`:**
- Removed `DatabasePath` (D:\DataSet\metering.db). App now uses content-root fallback (`metering.db` in project root).
- Removed `Facility:NodesCsvPath` (D:\DataSet\Script2\mvp_nodes_schematic.csv). Not needed — facility is already seeded in DB; content-root fallback CSVs exist.
- Removed `Facility:EdgesCsvPath` (D:\DataSet\Script2\mvp_edges_schematic.csv). Same reason.
- Kept `Facility:ForceMigration: false` (inert, retained for safety).

**`DiplomovaPrace/metering.db`:**
- Replaced outdated 16MB file with current 54MB copy from `D:\DataSet\metering.db`. This is now the app-local runtime DB containing all facility nodes, edges, measurements and imported binding metadata.

**`DiplomovaPrace/Program.cs`:**
- Removed `builder.Services.AddScoped<SeedBindingMigrationService>();` registration.
- Updated startup comment to remove references to `Facility:BindingsCsvPath` and `Facility:DataRootPath`.

**Deleted `DiplomovaPrace/Components/Pages/MigrationView.razor`:**
- Route `/admin/migrate-dataset` removed. Migration is complete and the page was inert (no seeded bindings existed — all 1602 bindings were already imported as `fixedCsvSeries`).

**Deleted `DiplomovaPrace/Services/SeedBindingMigrationService.cs`:**
- Removed entire migration service. No seeded bindings remained; the migration trigger was never needed.

**`DiplomovaPrace/Services/FacilityDataBindingRegistry.cs`:**
- Removed `GetAllActiveSeededBindings()` method — was exclusively used by `SeedBindingMigrationService`.

**`DiplomovaPrace/Services/FacilityEditorStateService.cs`:**
- Removed `SaveMigrationBatchAsync()` method — was exclusively used by `SeedBindingMigrationService`.

### D:\... dependencies remaining
- **None in config** — all D:\ paths removed from `appsettings.Local.json`.
- **`D:\DataSet\data\...` in imported binding records** — 1602 imported bindings in `facility-editor-state.json` were all imported as `fixedCsvSeries` format with app-local `App_Data/facility-imports/...` paths. No D:\ paths baked into binding records.
- **`D:\DataSet\metering.db`** — no longer referenced. Content root copy is now the runtime DB.

### /admin/migrate-dataset route
- Removed. `MigrationView.razor` deleted.

### Build
- `dotnet build` successful: 0 errors, 0 warnings.

### Runtime sanity check
- App started with no DatabasePath configured; used content-root `metering.db` fallback (54MB, confirmed seeded).
- `GET http://localhost:5016` → HTTP 200. Startup clean.

---

### Date
[2026-05-01]

### Task
Seed-binding migration milestone — decouple runtime from external dataset disk paths

### What changed

**`DiplomovaPrace/Services/FacilityEditorStateService.cs`:**
- Added `ExternalDatasetCsvGzip` value to `FacilityImportedBindingFileFormat` enum with documentation comment.
- Added `ExternalSourceFilePath` (nullable string) field to `FacilityImportedBindingState`. Stores the absolute path to the external source file for `ExternalDatasetCsvGzip` bindings. Not used by `FixedCsvSeries`.
- Updated `NormalizeImportedBinding` to preserve `ExternalSourceFilePath` in the returned state object, and to allow empty `Unit` for `ExternalDatasetCsvGzip` bindings (unit is derived at read time from signal family).
- Added `SaveMigrationBatchAsync(importedBindings, tombstonedIds)`: single-read, single-write bulk operation that atomically persists up to 1603 imported binding states and 1603 tombstones in one file I/O cycle (avoids 3206 sequential writes).

**`DiplomovaPrace/Services/FacilityDataBindingRegistry.cs`:**
- Added `GetAllActiveSeededBindings()`: enumerates all seeded bindings that have not been tombstoned; used exclusively by the migration service.
- Updated `CreateImportedBindingRecord`: for `ExternalDatasetCsvGzip` format, the `SourceFilePath` is resolved from `state.ExternalSourceFilePath` (not derived from `StorageRelativePath + ContentRootPath`). This ensures the file path remains valid after a server restart even when `DataRootPath` is unconfigured.

**Created `DiplomovaPrace/Services/SeedBindingMigrationService.cs`:**
- Encapsulates the one-time migration logic.
- `RunAsync()`: idempotent — skips bindings where an imported record already exists for the same `nodeKey::signalCode`.
- Per binding: resolves the source `.csv.gz` path via `FacilityDataBindingRegistry.ResolveFilePath(meterFolder, fileName)`, creates a `FacilityImportedBindingState` with `FileFormat = ExternalDatasetCsvGzip` and `ExternalSourceFilePath = <absolute path>`, `StorageRelativePath = "external-dataset/{nodeKey}/{bindingId}"` (valid relative placeholder).
- After building the full batch in memory (without any file I/O for the data itself), calls `SaveMigrationBatchAsync` + in-memory upsert/suppress for all bindings atomically.
- Returns `SeedBindingMigrationResult` with per-binding `SeedBindingMigrationItem` records and Migrated/Skipped/Failed counts.
- `HasActiveSeededBindings()` / `GetActiveSeededBindingCount()` helpers for the UI.

**Created `DiplomovaPrace/Components/Pages/MigrationView.razor`:**
- Route: `/admin/migrate-dataset`.
- Shows current seeded binding count and migration status badge.
- "Run Migration Now" button with spinner. Displays migration result with per-binding breakdown table.
- Post-migration instructions for clearing `Facility:BindingsCsvPath` and `Facility:DataRootPath`.

**`DiplomovaPrace/Program.cs`:**
- Registered `SeedBindingMigrationService` as `Scoped`.

**Deleted `DiplomovaPrace/Components/Pages/EditorView.razor`:**
- Pre-existing build error: file still referenced deleted `IActiveBuildingService` and `EditorCanvas`; WORKLOG from previous session incorrectly marked it as already deleted. Removed now to unblock build.

### Architecture of the migration

The migration registers each seeded `.csv.gz` binding as an `ExternalDatasetCsvGzip` imported binding. No files are copied or decompressed. The existing analytics pipeline already:
- handles `.gz` extensions via `OpenCsvReader` (GZipStream)
- reads multi-column format when `UsesFixedCsvSeriesFormat = false` (column name `{MeterUrn}.{MeasurementKey}`)
- checks `SourceFilePath` first in `ResolveCuratedFilePath` (bypasses `DataRootPath`)

After migration the app no longer reads `dataset_bindings_fixed.csv` at startup (`BindingsCsvPath` not needed) and no longer resolves files via `DataRootPath`. The external data files remain at `D:\DataSet\data` but are accessed directly via the stored absolute path in `ExternalSourceFilePath`.

### How to complete the migration

1. Start the app: `dotnet run --project DiplomovaPrace`
2. Navigate to `/admin/migrate-dataset`
3. Click **Run Migration Now** — expects 1603 bindings migrated, 0 failed
4. After success: edit `appsettings.Local.json`, clear `Facility:BindingsCsvPath` and `Facility:DataRootPath`
5. Restart the app and verify FacilityWorkbench analytics still load data correctly (verification run)

### Build

- `dotnet build` successful: 0 errors, 0 warnings.

### Bindings migrated (at build time)
- 0 (migration not yet executed — triggered via admin page at `/admin/migrate-dataset`)
- Expected after trigger: 1603 migrated, 0 failed, 0 skipped

### External dataset path dependence remaining
- Yes — data files still at `D:\DataSet\data` (accessed via stored absolute paths in `ExternalSourceFilePath`)
- Config keys `Facility:BindingsCsvPath` and `Facility:DataRootPath` will no longer be needed after migration runs
- `DatabasePath` (`D:\DataSet\metering.db`) is a separate concern, not part of this milestone

---


[2026-05-01]

### Task
Narrowed final legacy editor-tail cleanup without session-contract changes

### What changed

**Deleted files:**
- `DiplomovaPrace/Components/Pages/EditorView.razor`
- `DiplomovaPrace/Components/Editor/EditorToolbar.razor`
- `DiplomovaPrace/Components/Editor/EditorTreeNav.razor`
- `DiplomovaPrace/Components/Editor/EditorCanvas.razor`
- `DiplomovaPrace/Components/Editor/EditorPropertiesPanel.razor`
- `DiplomovaPrace/Services/IActiveBuildingService.cs`
- `DiplomovaPrace/Services/ActiveBuildingService.cs`
- `DiplomovaPrace/Services/IKpiService.cs`
- `DiplomovaPrace/Services/KpiService.cs`
- `DiplomovaPrace/Services/StateColorMapper.cs`

**`DiplomovaPrace/Components/Layout/NavMenu.razor`:**
- Removed the remaining legacy building/config block (active building dropdown and related create-building action).
- Removed the code-behind logic that depended on `IBuildingConfigurationService` and `IActiveBuildingService`.
- Kept facility-first primary nav and import navigation intact.

**`DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`:**
- Removed `GetPreviewDataAsync(...)`.
- Removed `IKpiService` constructor dependency and backing field.
- Removed the now-unused `using DiplomovaPrace.Models.Kpi;` import.

**`DiplomovaPrace/Program.cs`:**
- Removed `builder.Services.AddScoped<IKpiService, KpiService>();`
- Removed `builder.Services.AddSingleton<IActiveBuildingService, ActiveBuildingService>();`

### Dependency check result
- `EditorView.razor` was the last live caller of `NodeAnalyticsPreviewService.GetPreviewDataAsync(...)`.
- After EditorView/editor-cluster deletion and NavMenu cleanup, no surviving source references to `IActiveBuildingService`/`ActiveBuildingService` remained.
- `StateColorMapper` became unreferenced after deleting `EditorCanvas.razor`.

### Guardrails respected
- No changes to `IEditorSessionService`.
- No changes to `EditorSessionService`.
- No changes to `IBuildingConfigurationService`, `InMemoryBuildingConfigurationService`, or `BuildingConfiguration`.
- No changes to `FacilityWorkbench`.
- No changes to `ImportView`.

### Build
- `dotnet build` successful via workspace build task (`DiplomovaPrace net10.0 úspěšné`, total `Sestavení úspěšné`).

---

### Date
[2026-05-01]

### Task
Narrow removal of confirmed-dead KPI preview path in FacilityWorkbench

### Validation evidence
- `ActiveBuildingService.ActiveBuildingId` is never set from `FacilityWorkbench`; it starts `null`.
- `KpiService.GetDeviceAndBuildingAsync` returns `(null, null)` immediately when `ActiveBuildingId` is null/empty, causing `CalculateBasicKpiAsync` to return `MeterKpiResult` with `RecordCount = 0`.
- `NodeAnalyticsPreviewService.GetPreviewDataAsync` sees `RecordCount == 0` and returns `null`.
- `_nodePreviewData` (the assignment target) was never referenced in the FacilityWorkbench markup — only assigned in code.
- Even if `ActiveBuildingId` were set, `InMemoryBuildingConfigurationService` holds only the old demo building with traditional device IDs, which would never match facility-first MeterUrn values (e.g. "V.Z82").
- **Conclusion: the path is confirmed dead.**

### What changed

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Removed `private DiplomovaPrace.Models.Kpi.MeterKpiResult? _nodePreviewData;` field declaration.
- Removed all five `_nodePreviewData = null;` assignment sites (in the no-selection clear block, custom interval validation block, general pre-branch reset block, and `ClearPreviewState()`).
- Removed `var shouldLoadMeterPreview = ...` variable.
- Updated `loadingScopes["focus-preview"]` ternary: removed the `shouldLoadMeterPreview ? 4 :` arm; now just `shouldLoadCuratedFocusPreview ? 1 : 0`.
- Removed the entire `else if (!skipPostRefresh && _selectedFacilityNode is not null && !string.IsNullOrWhiteSpace(_selectedFacilityNode.MeterUrn))` branch that called `AnalyticsPreview.GetPreviewDataAsync`.

### What was NOT changed
- `NodeAnalyticsPreviewService.GetPreviewDataAsync` — kept because `EditorView.razor` (line 377) still calls it.
- `KpiService` / `IKpiService` — kept because they still have at least one live caller (`EditorView` → `GetPreviewDataAsync`).
- `IActiveBuildingService` / `ActiveBuildingService` — out of scope.
- `IBuildingConfigurationService` / `InMemoryBuildingConfigurationService` — out of scope.
- `EditorView.razor` — untouched per task constraints.
- All other FacilityWorkbench logic — untouched.

### Build
- `dotnet build` successful (6.3 s, zero errors, zero warnings).

### What remains for the next step
- `NodeAnalyticsPreviewService.GetPreviewDataAsync` still exists and is used by `EditorView.razor`.
- `KpiService` / `IKpiService` still registered and used via the EditorView path.
- If/when `EditorView` is cleaned up or removed as a legacy surface, `GetPreviewDataAsync` and its KpiService chain can be removed at that time.
- `IActiveBuildingService` / `ActiveBuildingService` / `IBuildingConfigurationService` / `InMemoryBuildingConfigurationService` remain intact for now — separate future cleanup scope.
- Legacy documentation/comments that mention the non-curated meter preview path can be cleaned in a future documentation-focused pass.

---

### Date
[2026-05-01]

### Task
Třetí safe legacy service-layer deletion bundle — odstranění simulation/state persistence tria

### What changed

**Deleted files:**
- `DiplomovaPrace/Services/SimulationService.cs`
- `DiplomovaPrace/Services/ISimulationService.cs`
- `DiplomovaPrace/Services/MeasurementPersistenceService.cs`
- `DiplomovaPrace/Services/BuildingStateService.cs`
- `DiplomovaPrace/Services/IBuildingStateService.cs`

**`DiplomovaPrace/Program.cs`:**
- Removed `builder.Services.AddSingleton<MeasurementPersistenceService>();`
- Removed `builder.Services.AddHostedService(sp => sp.GetRequiredService<MeasurementPersistenceService>());`
- Removed `builder.Services.AddSingleton<IBuildingStateService, BuildingStateService>();`
- Removed `builder.Services.AddSingleton<SimulationService>();`
- Removed `builder.Services.AddSingleton<ISimulationService>(sp => sp.GetRequiredService<SimulationService>());`
- Removed `builder.Services.AddHostedService(sp => sp.GetRequiredService<SimulationService>());`

### Dependency check result
- `CsvMeasurementImportService` has no remaining `IBuildingStateService` dependency.
- `EditorView` has no remaining `IBuildingStateService` dependency.
- `MeasurementPersistenceService` had no active callers after simulated DB writes were disabled earlier; only `Program.cs` still registered it and `SimulationService` still accepted it as an optional constructor dependency.
- No active surviving FacilityWorkbench/import/editor product path referenced `SimulationService`, `ISimulationService`, `BuildingStateService`, or `IBuildingStateService`.

### Build
- `dotnet build` successful via the workspace build task after the deletion bundle.

### Left intentionally for later
- `BuildingConfiguration.cs` and the `IBuildingConfigurationService` / `InMemoryBuildingConfigurationService` path remain intact per scope.
- `IActiveBuildingService` / `ActiveBuildingService` remain intact per scope.
- `StateColorMapper` remains intact per scope.
- Legacy documentation/comments that still describe the removed building-state/simulation architecture can be cleaned separately in a later documentation-focused pass.

---

### Date
[2026-05-01]

### Task
Minimal decoupling step before the third legacy service-layer deletion bundle

### What changed

**`DiplomovaPrace/Services/CsvMeasurementImportService.cs`:**
- Removed the `IBuildingStateService` constructor dependency and backing field.
- Removed the legacy device-whitelist build that walked `BuildingStateService.Building -> Floors -> Rooms -> Devices`.
- Removed the skip path that rejected CSV rows when `DeviceId` was not present in the old demo-building metering graph.
- Kept the import contract otherwise unchanged: valid rows are still parsed and batch-saved to `IMeasurementRepository`, and `UnknownDevices` now returns an empty list because that legacy whitelist validation no longer exists.

**`DiplomovaPrace/Components/Pages/EditorView.razor`:**
- Removed `@inject IBuildingStateService StateService` because it became unused.
- Removed the dead `ConfigService.ToBuildingDomainModel(...)` + `StateService.ReplaceBuilding(...)` publish path from `HandleApplyToVisualization()`.
- Kept the existing validation gate and `SessionService.MarkPublished()` state transition intact.
- Updated the success toast so it no longer claims publication to the removed `/building` visualization route.

### Dependency check result
- No unexpected live dependency required a replacement architecture.
- `CsvMeasurementImportService` used `IBuildingStateService` only for the legacy demo-building metering whitelist.
- `EditorView` used `IBuildingStateService` only for the dead runtime publish path.

### Build
- `dotnet build` successful via the workspace build task after the decoupling edits.

### What remains for the next bundle
- `IBuildingStateService` / `BuildingStateService` still remain registered and implemented.
- `SimulationService` still depends on `IBuildingStateService` and remains intentionally untouched in this milestone.
- `MeasurementPersistenceService` remains intentionally untouched in this milestone.
- Legacy comments/contracts that still describe `ReplaceBuilding()` as the editor publication bridge can be cleaned up together with the next deletion bundle if that bundle removes the remaining legacy service layer.

---

### Date
[2026-05-01]

### Task
Druhý narrow legacy deletion milestone — odstranění old building-view UI clusteru a jeho helper služeb

### What changed

**Deleted files:**
- `DiplomovaPrace/Components/Pages/BuildingView.razor` — legacy building-view page, accessible only via direct URL
- `DiplomovaPrace/Components/Building/BuildingViewer.razor` — top-level component orchestrating the old SVG building view
- `DiplomovaPrace/Components/Building/FloorPlan.razor` — SVG floor plan renderer
- `DiplomovaPrace/Components/Building/RoomShape.razor` — SVG room shape component
- `DiplomovaPrace/Components/Building/DeviceIcon.razor` — SVG device icon component
- `DiplomovaPrace/Components/Building/DeviceDetailPanel.razor` — device detail sidebar panel
- `DiplomovaPrace/Components/Building/FloorSummaryPanel.razor` — floor summary sidebar panel
- `DiplomovaPrace/Components/Building/RoomSummaryPanel.razor` — room summary sidebar panel
- `DiplomovaPrace/Components/Building/ExpressionPanel.razor` — expression calculator panel
- `DiplomovaPrace/Services/ExpressionEvaluator.cs` — expression evaluation helper, only used by ExpressionPanel
- `DiplomovaPrace/Services/DisplayRuleEvaluator.cs` — display rule helper (IDisplayRuleEvaluator), only used by RoomShape and DeviceIcon

**Program.cs changes:**
- Removed `builder.Services.AddSingleton<ExpressionEvaluator>();`
- Removed `builder.Services.AddSingleton<IDisplayRuleEvaluator, DisplayRuleEvaluator>();`

### Dependency check result
- `ExpressionEvaluator` / `IDisplayRuleEvaluator` were only referenced inside the deleted cluster files — no live surface dependency found.
- `FacilityTimeSeriesPanel`, `FacilityTemperatureLoadScatterPanel`, `FacilityLoadDurationCurvePanel`, `FacilityCompareTimeSeriesPanel` in the Building folder are NOT part of the old cluster and were intentionally kept.
- NavMenu had no nav link to `/building`; the route was accessible only via direct URL.

### Build
- `dotnet build` successful ("Sestavení úspěšné za 3,6s").

### Leftover legacy work intentionally left for later
- `SimulationService` — still runs as IHostedService with its `_persistence` field; full removal is a separate milestone.
- `MeasurementPersistenceService` — still registered; can be cleaned up together with SimulationService.
- `IBuildingStateService.AddMeasurement` in-memory ring buffer path — still present in the interface and BuildingStateService; deferred.
- `BuildingStateService` / `IBuildingStateService` — kept per task scope.
- `IBuildingConfigurationService` / `InMemoryBuildingConfigurationService` — kept per task scope.
- `IActiveBuildingService` / `ActiveBuildingService` — kept per task scope.
- `StateColorMapper` — kept per task scope.
- Nav/layout/editor/import surfaces — untouched per task scope.

---

### Date
[2026-05-01]

### Task
Containment fix: zastavení zápisu simulovaných měření do reálné DB

### What changed

**`DiplomovaPrace/Services/SimulationService.cs`:**
- Removed the two lines in `SimulateTick()` that called `_persistence?.Enqueue(measurement)`.
- The in-memory ring buffer call (`_stateService.AddMeasurement`) was kept intact — legacy BuildingView live charts are unaffected.
- The `_persistence` field and constructor parameter remain in the class (not injected to anything harmful, just unused from a write perspective).

### Write path disabled

Before:
```
SimulationService.SimulateTick()
  → _stateService.AddMeasurement(device.Id, measurement)   // in-memory
  → _persistence?.Enqueue(measurement)                     // ← DB write — REMOVED
```
After:
```
SimulationService.SimulateTick()
  → _stateService.AddMeasurement(device.Id, measurement)   // in-memory only
```

### Guardrails kept
- `MeasurementPersistenceService` registration and service left intact (CsvMeasurementImportService uses `IMeasurementRepository` directly — unaffected).
- `SimulationService` still runs as IHostedService (BuildingView UI still gets live simulated state).
- No changes to BuildingView, BuildingStateService, FacilityWorkbench, CsvMeasurementImportService, or any other service.

### Build
- `dotnet build` successful ("Sestavení úspěšné za 4,0s").

### Leftover legacy work intentionally left for later
- `SimulationService` itself (with its `_persistence` field/constructor) remains — full removal is a separate deletion sprint.
- `MeasurementPersistenceService` remains registered — can be cleaned up when SimulationService is fully removed.
- BuildingView / BuildingViewer component cluster removal: deferred per task scope.
- `IBuildingStateService.AddMeasurement` in-memory ring buffer path: still active for BuildingView, deferred.

---

### Date
[2026-05-01]

### Task
Implementační krok: první narrow legacy deletion milestone — odstranění legacy KPI/dashboard stacku

### What changed

**Deleted files:**
- `DiplomovaPrace/Components/Pages/DashboardView.razor` — legacy Energetický Dashboard page, no live references outside this slice
- `DiplomovaPrace/Components/Pages/KpiView.razor` — legacy KPI analytics page, no live references outside this slice
- `DiplomovaPrace/Services/BaselineService.cs` — service used only by DashboardView
- `DiplomovaPrace/Services/IBaselineService.cs` — interface used only by DashboardView and BaselineService
- `DiplomovaPrace/Models/Kpi/BaselineModels.cs` — models (BaselineResult, BaselineStatus) used only by BaselineService and DashboardView

**`DiplomovaPrace/Program.cs`:**
- Removed DI registration: `builder.Services.AddScoped<IBaselineService, BaselineService>();`

### Guardrails kept
- `KpiService.cs` and `IKpiService.cs` were NOT deleted — they have a live dependency in `NodeAnalyticsPreviewService` (active FacilityWorkbench analytics service uses `IKpiService.CalculateBasicKpiAsync`)
- `Models/Kpi/KpiModels.cs` was NOT deleted — `KpiQuery` and `MeterKpiResult` are used by `NodeAnalyticsPreviewService`
- `IKpiService` DI registration in Program.cs was NOT removed for the same reason
- No changes to BuildingView, BuildingStateService, SimulationService, EditorView, ImportView, FacilityWorkbench

### Leftover legacy references for next bundle
- `KpiService.cs` + `IKpiService.cs` + `Models/Kpi/KpiModels.cs` remain because `NodeAnalyticsPreviewService` depends on them. These can only be cleaned up if `NodeAnalyticsPreviewService` is refactored to use a more direct measurement query path, or if `IKpiService` is renamed/reframed as a shared measurement utility.
- `AnalyticsProgressUpdate` model (used by both KpiService and NodeAnalyticsPreviewService) must also be kept.

### Build
- `dotnet build` successful after all changes.

---

### Date
[2026-05-01]

### Task
Implementační krok: minimal navigation cleanup pro facility-first produktový směr

### What changed

**`DiplomovaPrace/Components/Layout/NavMenu.razor`:**
- Updated top shell branding from `Buildings Management` to `Facility Workbench` to avoid legacy-first framing.
- Kept one clear primary entry point to the main product surface (`/facility` as `Facility Schematic`).
- Removed duplicate/confusing peer navigation path (`Mapový pohled`) that led to the same FacilityWorkbench route (`/`).
- Removed legacy/reference pages from normal peer nav exposure:
  - `/dashboard` (`Přehled budovy`)
  - `/kpi` (`Analytika (KPI)`)
  - `/editor` (`Konfigurace`)
- Kept direct URL access to legacy pages intact and added a muted note that legacy/reference pages are available only via direct URL.
- Reworded legacy building selector caption to `Referenční kontext (legacy)` so it does not compete with the facility-first product surface.

### Guardrails kept
- No changes to FacilityWorkbench behavior.
- No changes to FacilityTopbar navigation.
- No route/page removal.
- No service/data flow/auth/deployment changes.
- No broad layout redesign.

### Build
- `dotnet build` successful via the workspace build task after the navigation cleanup edits.

### Date
[2026-04-29]

### Task
Implementační krok 11j: literature-backed treemap redesign + operation-local loading polish + drill-in interaction fix

### What changed

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Replaced the old first-split treemap rect builder with an ordered square-ish strip layout that keeps contributors in readable non-extreme rectangles instead of long thin strips.
- Tightened contributor grouping to true threshold-first behavior so direct tiles keep expanding until the grouped tail reaches `<= 15 %`, unless the next direct tile would already be too small to stay readable.
- Retuned treemap text mode selection to prioritize labels more aggressively by area, shorter side, and aspect ratio so medium and narrow readable tiles no longer disappear as often.
- Added hover debounce for contributor preview so quick pointer passes no longer trigger overlay refresh on every micro-hover.
- Kept drill-in preview/pin interaction on the `Other contributors` overlay but routed it through the debounced hover flow and explicit pin removal flow.
- Moved pinned contributor chips into the chart mode row and added direct per-chip unpin actions instead of keeping pinned state inside the chart surface overlay.
- Changed loading pill progression to operation-local percentages:
  - full overview refresh now progresses as `0 -> 18 -> 82 -> 100`,
  - semantics/granularity chart refresh now uses `0 -> 100` for the local chart operation,
  - the previous interpolated pseudo-progress model was removed.
- Increased the completion hold so the loading pill visibly stays at `100 %` briefly before dismissing.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor.css`:**
- Added treemap stage isolation so the drill-in overlay fully owns the stacking context above the tiles.
- Made the drill-in overlay fully opaque and raised its z-index to stop bleed-through.
- Slightly tightened treemap tile padding and typography so square-ish tiles use space more efficiently without text spill.

**`DiplomovaPrace/Components/Building/FacilityTimeSeriesPanel.razor`:**
- Added inline pinned-contributor chips beside `Auto / 15min / Hourly / Daily`.
- Styled the chips as horizontal non-layout controls with overflow-safe scrolling so they do not increase chart panel height.
- Added the small red `×` dismiss action for direct unpin from the chip row.

### Guardrails kept
- No new KPI.
- No new mathematical model.
- Tooltip behavior was left unchanged.
- No wider Overview / Analysis redesign beyond the requested treemap, drill-in, pinned-chip, hover-performance, and loading-pill scope.

### Build
- `dotnet build` successful via the workspace build task after the final 11j edits.

### Date
[2026-04-29]

### Task
Implementační krok 11i: debug instrumentation for loading and treemap/drill-in flow, plus targeted drill-in fixes

### What changed

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Added structured browser-console instrumentation for analytics loading operations using:
  - `[analytics-load:start]`
  - `[analytics-load:phase-complete]`
  - `[analytics-load:end]`
- Added operation ids and operation types for the active loading paths:
  - `full-overview-refresh`
  - `overview-chart-refresh`
  - `overview-semantics-switch`
  - `overview-granularity-switch`
  - `analysis-refresh`
  - `contributor-overlay-refresh`
- Logged loading context with selection scope, interval, semantics mode, and chart granularity.
- Logged phase timing and percent progression for full analytics refresh, overview-chart refresh, selection-signal refresh, and contributor-overlay refresh.
- Added structured treemap / drill-in interaction logging using:
  - `[treemap:interaction]`
  - `[treemap:drill-in]`
  - `[treemap:state]`
- Logged tile hover / hover end / click / pin / unpin and drill-in row preview start / preview end / pin / unpin.
- Logged drill-in open / close reasons, including explicit close, outside click, and `Other` tile toggle.
- Fixed drill-in interaction handling by giving tiles and drill-in rows dedicated exit handlers instead of sharing a generic leave callback.
- Fixed drill-in preview cleanup on close by clearing unpinned preview state when the overlay is dismissed.
- Removed the instructional text from the drill-in overlay.
- Replaced the drill-in secondary label so it now prefers node `type` instead of showing `Other / Unclassified`, with a safe fallback to role label and then `Unknown type`.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor.css`:**
- Hardened the drill-in overlay backdrop so it fully covers the treemap stage with an opaque surface.
- Raised the drill-in overlay z-index and enabled backdrop pointer capture so underlying treemap tiles no longer bleed through visually or interactively.
- Expanded the drill-in panel to the full overlay width to keep the overlay self-contained.

### Guardrails kept
- No new KPI.
- No new loading UI redesign.
- No new treemap redesign.
- No new analytical model.
- No wider Overview / Analysis refactor beyond the requested instrumentation and drill-in fixes.

### Build
- Workspace build task still hits the known debugger lock on `DiplomovaPrace\bin\Debug\net10.0\DiplomovaPrace.dll`.
- Verified successful compilation with an isolated output build:
  - `dotnet build .\DiplomovaPrace\DiplomovaPrace.csproj -o .\build-validation\step11i-agent-build "/property:GenerateFullPaths=true" "/consoleloggerparameters:NoSummary;ForceNoAlign"`

### Date
[2026-04-29]

### Task
Implementacni krok 11h: final stabilization polish for operation-local loading, drill-in persistence, pinned layout stability, and treemap visibility

### What changed

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Changed analytics loading operations so both the full analytics refresh and the local overview-chart refresh now start from `0 %` for their own operation instead of inheriting a partially advanced staged value.
- Changed loading completion so the global pill now reaches a visible `100 %` before dismissing, instead of clearing immediately from an intermediate percent.
- Kept the `Other contributors` drill-in overlay open when hovering or pinning another treemap tile; the overlay now closes only through the explicit close action or by toggling the synthetic `Other` tile itself.
- Kept drill-in row hover bound to preview and drill-in row click bound to pin/unpin, while preserving the multi-pin overlay flow in the main chart.
- Reworked direct-contributor selection so treemap grouping is threshold-first: direct tiles expand until `Other contributors` drops to `<= 15 %`, unless the next candidate would already be too small to stay readable.
- Tightened treemap inline text mode selection to use tile area, shorter side, and aspect ratio instead of only simple width/height breakpoints.
- Moved the pinned/preview chart chips out of the header flow into a dedicated chart-surface overlay layer so adding pins no longer changes panel height.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor.css`:**
- Added a non-layout chart context overlay inside the overview chart surface for pinned chips, preview chip, loading chip, and `Clear all pins`.
- Strengthened treemap text readability with slightly denser label/value sizing and clearer text contrast treatment.
- Replaced the fragile treemap outline treatment with inset ring styling plus z-index ordering so pinned tiles remain visibly outlined on all four sides.

### Guardrails kept
- No new KPI.
- No new mathematical model.
- Tooltip behavior was left unchanged.
- No new Overview / Analysis redesign outside the requested loading, treemap, drill-in, and pinned-layout stabilization scope.

### Build
- Workspace build task hit the known debugger file lock on `bin\Debug\net10.0\DiplomovaPrace.dll`.
- Verified successful compilation with an isolated build output:
  - `dotnet build .\DiplomovaPrace\DiplomovaPrace.csproj -o .\build-validation\step11h-agent-build "/property:GenerateFullPaths=true" "/consoleloggerparameters:NoSummary;ForceNoAlign"`

### Date
[2026-04-29]

### Task
Implementační krok 11g: final polish for loading pill, treemap overlay drill-in, multi-pin chart overlay, tooltip cleanup

### What changed

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Replaced the remaining loading rail UI with a compact single-phase status pill in the analytics toolbar.
- Kept the Overview loading skeleton at the product-correct 3 KPI placeholders.
- Removed the chart idle micro-copy `Double click chart to reset zoom.` from the visible Overview UI.
- Changed contributor pinning from single-pin state to multi-pin state:
  - pinned contributors are now accumulated,
  - the chart context shows multiple pinned chips,
  - `Clear all pins` clears the full pinned set.
- Reworked contributor overlay refresh so the Overview chart now keeps:
  - multiple pinned contributor overlays,
  - optional hover preview for a non-pinned contributor,
  - stable reload behavior when switching `Net / Consumption / Production`.
- Moved `Other contributors` drill-in into an overlay rendered inside the treemap stage instead of a separate block below it.
- Kept drill-in hover as preview and made drill-in click toggle contributor pinning into the chart overlay set.
- Tightened treemap tile text policy to the intended hierarchy:
  - large tiles: label + value + share,
  - medium tiles: label + share,
  - small tiles: label only,
  - very small tiles: no inline text.
- Added treemap tooltip metadata wiring for `Type`, `Zone`, and `Style` sourced from the current node/editor state.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor.css`:**
- Styled the new compact loading pill so it sits beside `Overview / Analysis` instead of reading like a larger panel.
- Stretched the Overview right column so the treemap card fills the same vertical weight as the main chart card.
- Turned the drill-in into an in-place glassy overlay anchored inside the treemap stage.
- Compressed treemap tile padding and text sizing to avoid overflow and keep labels readable in smaller rectangles.

**`DiplomovaPrace/Components/Building/FacilityTimeSeriesPanel.razor`:**
- Extended the chart component API from a single overlay series to a list of overlay series so the Overview chart can render multiple contributor pins plus hover preview.
- Updated the render signature fingerprint to include every overlay series and preserve robust rerender behavior.

**`DiplomovaPrace/wwwroot/js/facilityTimeSeriesChart.js`:**
- Updated the ECharts renderer to support multiple contributor overlay series at once.
- Added per-overlay visual treatment so pinned contributors remain solid and preview overlays stay dashed.

**`DiplomovaPrace/wwwroot/js/editor.js`:**
- Cleaned the treemap tooltip to match the node hintbox pattern.
- Removed `Semantics` and `Detail` from the treemap tooltip.
- Added `Type`, `Zone`, `Style`, and kept `Value` + `Share`.

### Guardrails kept
- No new KPI.
- No new mathematical model.
- No new Overview / Analysis shell redesign outside the requested loading, treemap, drill-in, chart overlay, and tooltip polish.

### Build
- `dotnet build` successful via the workspace build task after the final 11g polish edits.

### Date
[2026-04-29]

### Task
Implementační krok 11f: loading / treemap / overview chart stabilization polish

### What changed

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Moved the analytics loading rail into a compact top-right toolbar beside the existing `Overview / Analysis` switch instead of leaving it as a larger full-width block.
- Reduced the Overview loading KPI skeleton strip from 4 placeholders to the product-correct 3 cards.
- Added timer-backed staged loading refresh so elapsed time and the displayed progress percent keep moving during longer loads instead of freezing between stage updates.
- Unified active contributor label resolution across both main treemap tiles and the `Other contributors` drill-in list so hover/pin state flows into the chart context and overlay naming consistently.
- Wrapped the contributors surface into a dedicated treemap body so the drill-in can live inside the right column without forcing the left chart panel to stretch.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor.css`:**
- Compressed the loading rail visual treatment: smaller typography, tighter spacing, narrower footprint, and right-aligned toolbar placement.
- Added bounded rail-bar motion and responsive toolbar behavior for a more live loading state.
- Changed the Overview main grid to stop stretching the chart panel when the contributors drill-in opens.
- Added treemap-body / drill-in sizing rules so the treemap avoids the previous dead white space and the drill-in scrolls inside the right column with its own max-height.

**`DiplomovaPrace/Components/Building/FacilityTimeSeriesPanel.razor`:**
- Replaced the old shape-only `_lastRenderedSignature` with a richer render fingerprint that includes series metadata plus a point/baseline content fingerprint for both primary and overlay series.
- This fixes the root cause where `Net / Consumption / Production` could collide on the same node/granularity/point-count/timestamp shape and incorrectly skip a needed rerender when returning to an already rendered semantics mode.

### Guardrails kept
- No new KPI.
- No new mathematical model.
- No Overview / Analysis redesign outside the requested loading, treemap, drill-in, and rerender stabilization scope.

### Build
- `dotnet build` successful via the workspace build task after the final 11f stabilization edits.

### Date
[2026-04-29]

### Task
Implementační krok 11e: loading UX + staged progress bar pro Overview a Analysis

### What changed

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Added a global staged loading rail for the lower analytics workspace under the `Overview / Analysis` switch.
- Introduced lightweight staged loading state for the existing refresh orchestration:
  - headline,
  - phase label,
  - detail text,
  - staged percent,
  - elapsed time.
- Wired the staged rail into the existing analytics refresh flow:
  - `ReloadPreviewDataAsync()` now advances through scope, overview, analysis, and finalization phases,
  - `ReloadOverviewChartAsync()` now exposes a dedicated staged chart-refresh rail when the chart is reloaded independently.
- Replaced the old Overview spinner with product-level loading placeholders:
  - KPI skeleton strip,
  - main chart placeholder,
  - contributors treemap placeholder.
- Replaced the old Analysis spinner with a proper loading skeleton:
  - toolbar skeleton,
  - module-switch skeleton,
  - active-module placeholder body.
- Hardened stale-data behavior so the Overview chart placeholder is rendered while `_isOverviewChartLoading` is true instead of leaving the previous chart visible.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor.css`:**
- Added the loading rail styling, shimmer animation, Overview placeholder surfaces, Analysis skeleton styling, and responsive loading-state adjustments used by step 11e.

### Phase labels used
- `Resolving scope`
- `Loading overview`
- `Interval validation`
- `Loading analysis`
- `Finalizing visuals`
- `Loading trend surface`
- `Rendering trend surface`

### Guardrails kept
- No new KPI.
- No new analytical model.
- No redesign of the upper schematic workspace.
- Loading UX was built on top of the existing analytics loading flags and refresh methods.

### Build
- `dotnet build` successful via the workspace build task after the final 11e loading UX hardening edits.

### Date
[2026-04-28]

### Task
Implementační krok 11d: finální UX polish Overview + Analysis

### What changed

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Removed the baseline overlay from `Overview` at the integration level:
  - the Overview chart no longer requests baseline overlay data,
  - no baseline toggle or baseline control is rendered in the Overview chart shell.
- Reworked the top `Selection` card into a stronger donut-centered summary:
  - larger donut,
  - main selected-node count inside the donut,
  - added text stats for `With data` and `No data`.
- Hardened the Overview chart header and interactions:
  - clearer overlay state wording (`Previewing:` / `Pinned:`),
  - explicit `Clear pin`,
  - added `Reset` and `PNG` utility actions.
- Polished the contributors treemap behavior:
  - tile colors now resolve from the assigned node style preset instead of semantics-only palette colors,
  - `Other` is now adaptive with a target of keeping the grouped tail at or under 20 % when possible,
  - contributor tiles are capped before grouping,
  - `Other contributors` opens a lightweight drill-in list,
  - drill-in rows support hover preview and click-to-pin,
  - tile text density now adapts by tile size.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor.css`:**
- Enlarged and rebalanced the Selection card layout around the new donut.
- Tightened Overview and Analysis spacing for better FullHD / 2K fit:
  - smaller analytics widget padding,
  - denser KPI strip,
  - tighter chart/treemap spacing,
  - more compact Analysis toolbar/module spacing.
- Added styling for:
  - Overview chart utilities,
  - clearer overlay state chips,
  - node-style treemap tiles,
  - the lightweight `Other contributors` drill-in panel.

**`DiplomovaPrace/Components/Building/FacilityTimeSeriesPanel.razor`:**
- Added reusable chart utility methods for:
  - zoom reset,
  - PNG snapshot export.
- Reduced compact-mode panel padding and chart height for the dense Overview layout.

**`DiplomovaPrace/wwwroot/js/facilityTimeSeriesChart.js`:**
- Added runtime support for:
  - double click to reset zoom,
  - programmatic zoom reset,
  - PNG export.
- Applied light chart polish for the compact Overview surface:
  - tighter grid,
  - calmer split lines,
  - slightly stronger primary line,
  - darker tooltip surface.

### Guardrails kept
- No new KPI.
- No new mathematical model.
- No baseline formula change.
- No scatter / EUI formula change.
- No redesign of the upper schematic workspace.

### Build
- `dotnet build` successful via the workspace build task after the final 11d polish edits.

### Date
[2026-04-28]

### Task
Implementační krok 11c: kompletní redesign spodní analytics části

### What changed

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Reworked the bottom analytics IA into exactly two workspaces:
  - `Overview`
  - `Analysis`
- Simplified `Overview` so the old header ballast is gone:
  - no extra title block with node name,
  - no interval text as a dominant header element,
  - no `Scope` or `Status` strips carried forward.
- Replaced the old mixed overview KPI surface with exactly three equivalent KPI cards:
  - `Net`,
  - `Consumption`,
  - `Production`.
- Wired the Overview semantics switch so it now materially changes the active aggregate reading surface:
  - KPI values,
  - main chart dataset,
  - top-contributor treemap.
- Added a compact `Top contributors` mini treemap to Overview.
- Linked the treemap to the main chart without introducing new formulas:
  - hover previews a contributor overlay in the main chart,
  - click pins the contributor overlay until cleared.
- Kept `Analysis` as a separate workspace with exactly one active module at a time:
  - `Trend`,
  - `Baseline`,
  - `Scatter`,
  - `Power`,
  - `EUI`.
- Preserved the existing analytics contracts and data sources:
  - no new KPI definitions,
  - no new mathematical models,
  - no fallback back to legacy bottom IA.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor.css`:**
- Added the new Overview KPI-card layout, chart shell, contributor treemap styling, and compact Analysis toolbar/module-switch styling.
- Hardened the lower analytics layout for FullHD / 2K so the workspace remains denser and avoids unnecessary vertical scroll.

**`DiplomovaPrace/Components/Building/FacilityTimeSeriesPanel.razor`:**
- Extended the reusable time-series panel to support an optional contributor overlay series and a more compact chrome mode used by the redesigned Overview chart.

**`DiplomovaPrace/wwwroot/js/facilityTimeSeriesChart.js`:**
- Extended the ECharts renderer to display a secondary overlay line for contributor preview/pin interactions and tuned spacing for the denser Overview layout.

**`DiplomovaPrace/wwwroot/js/editor.js`:**
- Added the tooltip runtime used by the Overview contributor treemap tiles.

### Guardrails kept
- No new KPI.
- No new mathematical model.
- No fallback to legacy bottom analytics navigation.
- Analysis remains single-module and consumption-basis driven.

### Build
- `dotnet build` successful via the workspace build task after the final Razor repair and workspace redesign.

### Date
[2026-04-28]

### Task
Implementační krok 11b: nová spodní IA `Overview + Analysis`

### What changed

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Replaced the old bottom primary analytics tab shell (`Overview / Breakdown / Performance / Compare / Diagnostics`) with exactly two bottom workspaces:
  - `Overview`
  - `Analysis`
- `Overview` now owns the aggregate bottom reading surface:
  - one main chart,
  - a `Net / Consumption / Production` switch,
  - a compact KPI strip,
  - and a lighter context rail for scope / composition / coverage.
- `Analysis` is now a separate workspace with one active module at a time:
  - `Trend`,
  - `Baseline`,
  - `Scatter`,
  - `Power`,
  - `EUI`.
- Removed the old bottom-tab IA as the primary navigation model instead of layering the new structure on top of it.
- Kept the implementation within the existing analytics result set:
  - no new KPI,
  - no new mathematical models,
  - no compare / diagnostics redesign carried forward into the new primary bottom IA.
- Enforced detail analytics over a consumption basis only:
  - signal availability and detail modules now resolve against positive-consumption contributors from the refreshed aggregate overview,
  - when no valid consumption basis exists, Analysis shows an explicit no-data / unavailable state instead of falling back to net or production detail.
- The `Overview` semantics switch only changes the aggregate reading surface:
  - `Net` keeps the signed aggregate view,
  - `Consumption` reloads the overview chart on positive consumption-only aggregation,
  - `Production` reloads the overview chart on production magnitude aggregation,
  - Analysis stays semantics-independent and consumption-only.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor.css`:**
- Added styling for the new workspace switch, Overview context rail, Analysis module switch, power chart sub-switch, and responsive card/grid behavior.
- Kept Overview visually calmer than Analysis while preserving the existing facility workbench language.

### Guardrails kept
- No new KPI.
- No new mathematical model.
- No fallback from Analysis to net / production detail when consumption basis is missing.
- No return to the old multi-tab bottom IA as the primary product surface.

### Build
- `dotnet build` successful via the workspace build task after the final `Overview + Analysis` implementation.

### Date
[2026-04-28]

### Task
Implementační krok 11: aggregate semantics + Overview/Detail redesign

### What changed

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Added an `Overview semantics` switch for the main Overview aggregate surface with exactly three modes:
  - `Net`,
  - `Consumption`,
  - `Production`.
- Wired the main Overview chart area and headline KPI to this switch so the same top-level aggregate surface can be read in the selected semantics without introducing new KPI logic.
- Split the UX more explicitly between:
  - `Overview` as the main aggregate chart area,
  - and a separate collapsible `Detail Analytics Tool` shell for deeper signal/performance inspection.
- Reworked detail analytics state resolution so signal analytics and performance slices now run only on a consumption basis and show an explicit no-data / no-basis state when a valid consumption basis is not available.
- Kept detail analytics free of any `Net / Consumption / Production` mode switch.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor.css`:**
- Added styling for the Overview semantics switch and the new detail-tool shell/basis banner so the Overview vs detail split is visually explicit in the main workspace.

**`DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`:**
- Added semantics-aware selection aggregate time-series support for Overview with a dedicated mode enum:
  - `Net` = signed sum,
  - `Consumption` = positive contributions only,
  - `Production` = magnitude of negative contributions only.
- Applied the same semantics transformation to the Overview aggregate curve metadata and baseline overlay path used by the main chart area.
- Left detail analytics formulas unchanged and did not expand the change into forecast redesign, compare redesign, or new KPI math.

### Guardrails kept
- No new KPI.
- No forecast redesign.
- No compare redesign.
- No additional mathematical redesign outside the requested semantics + UX/IA slice.
- Detail analytics remains consumption-basis only.

### Build
- `dotnet build` successful via the workspace build task.

### Date
[2026-04-28]

### Task
Implementační krok 10: floor area validation + EUI unit/scaling hardening

### What changed

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Hardened the inline `Floor area [m²]` editor contract so the field now accepts only:
  - empty value,
  - or a finite numeric value strictly `> 0`.
- Updated the editor hint, placeholder, and validation messages to match the business rule directly.
- Surfaced `EUI.StateReason` in the unavailable card so the UI now shows the concrete reason instead of only the generic unavailable summary.

**`DiplomovaPrace/Services/FacilityEditorStateService.cs`:**
- Tightened persisted floor-area normalization from `finite and >= 0` to `finite and > 0`.
- Existing persisted `0 m²` values are now normalized away instead of surviving as misleading explicit area metadata.

**`DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`:**
- Kept the EUI formula unchanged as `EUI = Energy / FloorArea` and kept period-EUI semantics over the selected interval.
- Hardened EUI unavailable-state reasons so the result now differentiates more explicitly between:
  - `missing floor area`,
  - `invalid floor area`,
  - `missing usable energy basis`,
  - `unsafe integration`,
  - plus the already explicit multi-candidate area-scope case.
- Fixed the root-cause scaling issue for power-family seeded bindings:
  - the seeded facility dataset leaves `binding.Unit` empty for `P/P1/P2/P3`,
  - but the actual source values are watt-scale,
  - so blank-unit seeded power bindings are now interpreted as `W` before `P -> kWh` integration.
- Left explicit units authoritative and did not broaden the fix into unrelated energy-family conversions.
- Updated the EUI methodology text so it now states that power-derived energy is integrated over actual timestamp spacing before normalization to `kWh`.

### Guardrails kept
- No new KPI.
- No baseline changes.
- No scatter changes.
- No compare redesign.
- No forecast rework.
- No automatic area defaults or auto-created nodes.

### Build
- `dotnet build` successful via the workspace build task.

### Task
Implementační krok 9: area m² support + EUI MVP

### What changed

**`DiplomovaPrace/Services/FacilityEditorStateService.cs`:**
- Added persistent node-level `FloorAreaM2` metadata to `FacilityNodeEditorState`.
- Bumped the editor-state schema version and normalized persisted floor-area values so only finite non-negative values survive load/save.
- Marked explicit floor area as meaningful node payload so the metadata persists even when no other node-level overrides are present.

**`DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`:**
- Added a new selection-first `EUI` slice to the signal analytics result.
- Implemented period `EUI = Energy / FloorArea` for the selected interval without switching to another signal or inventing default area values.
- Resolved floor area strictly from explicit area-node metadata:
  - prefer the focused scope anchor when it is an area node,
  - otherwise allow a single unambiguous area candidate in scope,
  - return explicit unavailable states for missing floor area, `<= 0 m²`, or multiple area candidates.
- Implemented interval energy resolution for the active exact signal using only the active basis:
  - direct energy samples,
  - cumulative energy counters,
  - or power integrated over actual timestamp spacing.
- Added explicit unavailable states when the active exact signal is outside `energy` / `power` scope or when interval energy cannot be derived safely.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Added `Floor area [m²]` editing to the inline node editor for area nodes only, including local non-negative numeric validation.
- Ensured floor-area metadata is preserved across metadata save, node move, style assignment, drag, align, and distribute editor-state updates.
- Passed the focused node key as the explicit scope anchor into selection signal analytics.
- Added a new `EUI` block in `Overview > Signal Analytics` with:
  - explicit unavailable states,
  - energy / floor area / EUI values,
  - explanatory copy that states this is period EUI over the selected interval,
  - and no fallback to inferred whole-building area.

### Availability contract
- Available only when:
  - the active exact signal is in the `energy` or `power` family,
  - interval energy can be derived safely from that same active exact signal,
  - and the current scope resolves one explicit area floor-area value `> 0 m²`.
- Unavailable when:
  - no explicit floor area exists for the selected area scope,
  - floor area is `0` or negative,
  - multiple area candidates exist without a unique intended anchor,
  - the active signal is outside `energy` / `power`,
  - or the active signal does not expose a safe interval energy basis.

### Guardrails kept
- No automatic whole-building node creation.
- No automatic `13 000 m²` default.
- No area estimation for other zones.
- No baseline, scatter, compare, or additional KPI redesign outside this EUI slice.

### Build
- `dotnet build` successful via the workspace build task.

### Date
[2026-04-28]

### Task
Implementační krok 8: Base vs Peak Over Time MVP

### What changed

**`DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`:**
- Extended the selection-first `Power Analytics` slice with a new `Base vs Peak Over Time` result for the active exact power signal.
- Implemented daily bucket evaluation over the same active power series already used by the existing power/load-shape metrics.
- For each usable UTC day, the metric computes:
  - `Base_d = 5th percentile` of that day's active power samples,
  - `Peak_d = 95th percentile` of that same day's active power samples.
- Added explicit unavailable states for the requested MVP rules:
  - mixed-sign aggregate `P`,
  - daily-only / no sub-daily power basis,
  - fewer than 3 usable UTC days.
- Added metadata for signal code, evaluation basis, usable-day count, and chart-ready two-series output without any fallback to another signal.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Added a new `Base vs Peak Over Time` chart section inside `Overview > Signal Analytics > Power Analytics`.
- The UI now renders two lines:
  - `Base over time`,
  - `Peak over time`.
- Added explicit supporting text that states:
  - `Base over time = daily 5th percentile of the active power series`,
  - `Peak over time = daily 95th percentile of the active power series`,
  - sub-daily power samples are required.
- Added a metric-local unavailable state with direct explanations for:
  - daily-only basis,
  - not enough usable days.
- Kept the broader mixed-sign aggregate `P` policy unchanged, so the whole `Power Analytics` block remains explicitly unavailable for mixed-sign aggregate `P`.

### Availability contract
- Available only when:
  - the active exact signal is in the `power` family,
  - the active basis is not mixed-sign aggregate `P`,
  - the current active power series has sub-daily samples,
  - at least 3 usable UTC days are present.
- Unavailable when:
  - the current active power basis is mixed-sign aggregate `P`,
  - the current series is only daily-bucketed,
  - fewer than 3 usable UTC days exist.

### Build
- `dotnet build` successful via the workspace build task.

### Date
[2026-04-28]

### Task
Implementační krok 8: Audit a hardening analytických výpočtů proti kontraktu + mixed-sign aggregate power fix

### What changed

**Audit ověřil tyto selection-first analytické slices v `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs` a navázaném UI v `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- `near-base` používá 5. percentil aktivní power série.
- `near-peak` používá 95. percentil stejné aktivní power série.
- `peak-base ratio` používá `near-peak / near-base` a má explicitní safe unavailable stav při numericky nebezpečně malém `near-base`.
- `load duration curve` používá stejnou aktivní power sérii, řadí ji sestupně a nedělá fallback na jiný signal.
- `on-hour duration` používá threshold `midpoint(near-base, near-peak)` nad stejnou aktivní power sérií.
- `after-hours load` používá stejný threshold, fixed after-hours okna a je unavailable bez sub-daily buckets.
- `daily weather-aware baseline` zůstává na denní energii + facility weather `Ta` s modelem `E_d = beta0 + betaH * HDD(18 C) + betaC * CDD(22 C)` a výstupy `Actual`, `Baseline expected`, `Delta abs`, `Delta %`, `CV(RMSE)`, `NMBE`, `Fit days`.
- `temperature vs load scatter` zůstává selection-first, weather-aware, s explicitní hourly pairing granularitou a bez silent fallbacku na jiný signal.

**Nalezená odchylka a oprava:**

**`DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`:**
- Found one contract gap in `Temperature vs Load Scatter`: mixed-sign aggregate `P` was already blocked inside the `Power Analytics` load-shape block, but the scatter path still accepted the same mixed-sign aggregate power basis as a valid load basis.
- Hardened `BuildSelectionTemperatureLoadScatterAsync(...)` so aggregate `P` with both positive and negative hourly load values now returns an explicit unavailable state instead of rendering a potentially misleading weather/load scatter.
- The unavailable state is explicit and local:
  - no fallback to another signal,
  - no automatic demand-positive projection,
  - UI keeps using the existing unavailable rendering with a direct explanation that the active aggregate `P` load basis is mixed-sign and that a consumption-oriented / non-mixed-sign basis is required.

### Mixed-sign aggregate policy
- `Power Analytics` load-shape metrics remain explicitly unavailable for mixed-sign aggregate `P`:
  - `near-base`
  - `near-peak`
  - `peak-base ratio`
  - `on-hour duration`
  - `after-hours load`
  - `load duration curve`
- `Temperature vs Load Scatter` now follows the same selection-first validity rule for aggregate `P` load basis and is also unavailable when the aggregate hourly power basis is mixed-sign.
- No new KPI, EUI, compare redesign, forecast rework, or other out-of-scope feature work was introduced.

### Build
- `dotnet build` successful via the workspace build task.

### Date
[2026-04-28]

### Task
Implementační krok 7: On-hour duration MVP + After-hours Load rework + mixed-sign power policy

### What changed

**`DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`:**
- Extended the selection-first `Power Analytics` slice for the active exact power signal with two new load-shape metrics:
  - `On-hour duration` as the time/share of buckets at or above an explicit high-load threshold,
  - `After-hours Load` as the same thresholded persistence metric, but evaluated only in fixed after-hours windows.
- Chose an explicit MVP threshold definition:
  - `high-load threshold = midpoint(near-base, near-peak)`
  - where `near-base = 5th percentile` and `near-peak = 95th percentile` of the same active power series.
- Added a load-shape-specific after-hours definition that keeps the current fixed time logic explicit:
  - after-hours = `weekday outside 07:00-19:00 UTC + weekends`
  - the metric reports how long / how often the active series stays above the same high-load threshold inside those windows.
- Added explicit mixed-sign aggregate policy for selection-first power analytics:
  - if the active exact signal is aggregate `P` and the resolved series contains both positive and negative values,
  - `near-base`, `near-peak`, `peak-base ratio`, `on-hour duration`, `after-hours load`, and `load duration curve`
    are all returned as unavailable,
  - with no silent fallback to another signal and no automatic demand-positive projection.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Reworked the `Overview > Signal Analytics > Power Analytics` block into a clearer shared load-shape block containing:
  - near-base,
  - near-peak,
  - peak-base ratio,
  - on-hour duration,
  - after-hours load,
  - load duration curve.
- Added explicit UI copy that explains the difference between:
  - `On-hour duration` = higher-load persistence across the whole interval,
  - `After-hours Load` = the same higher-load persistence but only in fixed after-hours windows.
- Added threshold and schedule badges so the user can see the exact MVP basis directly in the UI.
- Added an explicit unavailable-state message for mixed-sign aggregate `P` that explains why load-shape power analytics are hidden and that the user should choose a more suitable signal or narrower scope.

### Build
- `dotnet build` successful via the workspace build task.

### Validation scope
- Implemented only the requested power-family load-shape slice:
  - on-hour duration MVP,
  - after-hours load rework,
  - mixed-sign aggregate policy for power analytics.
- Explicitly not changed in this step: baseline, scatter, EUI, compare redesign, or other KPI areas outside this slice.

### Date
[2026-04-28]

### Task
Bugfix: JavaScript syntax/runtime repair for Main Time Series and Temperature vs Load Scatter

### What changed

**`DiplomovaPrace/wwwroot/js/facilityTimeSeriesChart.js`:**
- Removed accidentally pasted patch / Razor / C# content that had been appended after the valid JavaScript module footer `})();`.
- The contamination was split into two trailing fragments, so the shared chart runtime script did not finish parsing and `window.facilityTimeSeriesChart` was never created.
- Kept the existing chart runtime API unchanged and limited the fix strictly to restoring valid JavaScript for:
  - `render` (Main Time Series),
  - `renderTemperatureLoadScatter` (Temperature vs Load Scatter),
  - and the shared `dispose` / `resize` helpers.

### Root cause
- `facilityTimeSeriesChart.js` contained non-JavaScript content appended at the end of the file after the closure of the runtime module.
- Because Main Time Series and Temperature vs Load Scatter both rely on the same `window.facilityTimeSeriesChart` object, the parse failure blocked initialization for both panels.

### Build
- `dotnet build` successful via the workspace build task.

### Validation scope
- Editor diagnostics for `facilityTimeSeriesChart.js`: no errors.
- Browser smoke check on `/facility` confirmed:
  - `window.facilityTimeSeriesChart` is present again,
  - `Main Time Series` renders without the chart-runtime initialization fallback message,
  - `Temperature vs Load Scatter` is present and available without the chart-runtime initialization fallback message.

### Date
[2026-04-28]

### Task
Implementační krok 6: Temperature vs Load Scatter MVP

### What changed

**`DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`:**
- Added a new selection-scope `TemperatureLoadScatter` slice next to the existing active exact-signal trend, power analytics, and weather-aware baseline flow.
- Implemented explicit scatter gating only for active `energy` / `power` signals, with clear unavailable states when the active signal, usable hourly load basis, or facility weather `Ta` source is not safe enough to use.
- Chose explicit **hourly pairing** over complete UTC hours for the MVP.
- Added hourly load-basis preparation for the active exact signal:
  - hourly average power for power-family signals,
  - hourly energy-derived load for direct energy signals,
  - hourly energy-derived load from interval deltas for cumulative counters (`W`, `W_in`, `W_out`).
- Added hourly facility-weather `Ta` preparation from the resolved facility weather node and paired the resulting `Ta` values against the same active-signal hourly load basis without any fallback to another signal.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Added a new compact `Temperature vs Load Scatter` block into `Overview > Signal Analytics`, directly under the weather-aware baseline detail and before `Power Analytics`.
- Added clear supporting copy that states:
  - the chart compares facility `Ta` against the active exact signal load basis,
  - the pairing granularity is hourly,
  - which exact signal code is used,
  - and that this is an exploratory weather-aware scatter, not a final baseline fit.
- Added explicit unavailable-state rendering when the current active signal is outside energy/power scope or when hourly load / weather inputs cannot be prepared safely.

**`DiplomovaPrace/Components/Building/FacilityTemperatureLoadScatterPanel.razor` and `DiplomovaPrace/wwwroot/js/facilityTimeSeriesChart.js`:**
- Added a dedicated scatter-chart component and ECharts renderer for the new temperature-vs-load plot.
- Labeled the chart as `X = outdoor temperature Ta`, `Y = load basis`, with tooltip timestamps and point counts.

### Build
- Standard workspace `dotnet build` hit a locked `bin\Debug\net10.0\DiplomovaPrace.dll` held by the active debugger process.
- Isolated validation build succeeded with:
  - `dotnet build .\DiplomovaPrace\DiplomovaPrace.csproj -o .\build-validation\temperature-load-scatter`

### Validation scope
- Implemented only the requested scatter slice:
  - temperature vs load scatter MVP,
  - selection-first active-signal gating,
  - facility weather `Ta`,
  - current active signal / valid load basis,
  - explicit unavailable states.
- Explicitly not implemented in this step: forecast rework, compare redesign, EUI, or any KPI outside the requested scope.

### Date
[2026-04-27]

### Task
Implementační krok 5: Daily weather-aware baseline MVP + rework Deviation / Baseline Detail

### What changed

**`DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`:**
- Added a new selection-scope `WeatherAwareBaseline` slice on top of the existing active exact-signal analytics flow.
- Implemented explicit baseline gating only for active `energy` / `power` signals, with clear unavailable states when the active signal, daily energy preparation, or facility weather `Ta` source is not usable.
- Added daily energy preparation for the active exact signal:
  - direct daily energy from energy-family series,
  - interval-delta daily energy from cumulative counters (`W`, `W_in`, `W_out`),
  - daily energy derived from power by interval integration over actual timestamp spacing.
- Added facility-level weather lookup through the existing `FacilityWeatherSourceResolver` and prepared daily average `Ta` from the resolved facility weather node.
- Implemented the first real daily weather-aware baseline MVP model:
  - `E_d = beta0 + betaH * HDD(18 C) + betaC * CDD(22 C)`
  - fit over the previous 365 full UTC days before the selected interval,
  - explicit MVP default balance temperatures `18 C` (heating) and `22 C` (cooling).
- Added fit diagnostics `CV(RMSE)` and `NMBE` on the model fit days, with explicit unavailable states for low-data or numerically unstable fits.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Reworked `Overview > Signal Analytics` to include a new compact `Deviation / Baseline Detail` block driven by the new selection-first weather-aware baseline result.
- The panel now shows `Actual`, `Baseline expected`, `Delta abs`, `Delta %`, plus `CV(RMSE)` and `NMBE` when the baseline is available.
- Added explicit unavailable-state rendering when the active signal is outside energy/power scope or when daily energy / facility weather data are not safe enough to compute.
- Removed the old focus-only comparable-window baseline story from the main detail panel and replaced it with a routing note to the new selection-first baseline view.

### Build
- `dotnet build` successful via the workspace build task.

### Validation scope
- Implemented only the requested baseline slice:
  - daily weather-aware baseline,
  - reworked `Deviation / Baseline Detail`,
  - daily energy + facility weather `Ta`,
  - `CV(RMSE)` and `NMBE`.
- Explicitly not implemented in this step: scatter, forecast rework, EUI, or any other KPI outside the requested scope.

### Date
[2026-04-27]

### Task
Implementační krok 4: Power Analytics MVP (near-base, near-peak, peak-base ratio, load duration curve)

### What changed

**`DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`:**
- Added a power-family-only analytics slice on top of the existing active exact signal flow.
- Implemented near-base as the 5th percentile and near-peak as the 95th percentile of the exact active power series used by Signal Analytics.
- Added safe peak-base ratio evaluation with an explicit unavailable state when near-base is too close to zero for stable division.
- Added a new power-series load duration curve builder that sorts the same active power series descending, without demand-positive projection, 24h gating, or fallback to another signal.
- Extended load-duration-curve summary metadata with dynamic unit and Y-axis labels so the reused chart can render the current power series correctly.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Added a new `Power Analytics` block to `Overview > Signal Analytics`, directly under the active exact-signal trend.
- The block now shows clear unavailable state for non-power signals and renders near-base, near-peak, peak-base ratio, and LDC only for the current `power` family signal.
- Kept the existing Performance widgets unchanged, so no unrelated KPI redesign was introduced.

**`DiplomovaPrace/Components/Building/FacilityLoadDurationCurvePanel.razor`:**
- Reused the existing LDC chart component as the rendering base and updated it to respect dynamic unit / Y-axis metadata from the new power analytics slice.

### Build
- `dotnet build` successful via the workspace build task.

### Validation scope
- Implemented only the requested power-family slice: near-base, near-peak, peak-base ratio, and load duration curve.
- Explicitly not implemented in this step: baseline, weather-aware analytics, EUI, forecast rework, or any other KPI outside the requested scope.

### Date
[2026-04-27]

### Task
Sjednocení binding workflow do jednoho produktového modelu + NodeType UX cleanup

### What changed

**`DiplomovaPrace/Services/FacilityEditorStateService.cs`:**
- Added persistent deleted-binding tombstones so bindings that originated from the seeded registry can be removed from the user-facing binding view and stay removed across reloads.
- Bumped the editor-state schema and kept the new tombstone list normalized by binding id.

**`DiplomovaPrace/Services/FacilityDataBindingRegistry.cs`:**
- Registry now loads deleted-binding tombstones, filters suppressed seeded bindings out of the unified binding view, and excludes fully suppressed seeded nodes from supported-node discovery.
- Seeded bindings now receive a runtime `ImportedUtc`/created timestamp when exposed through the unified binding model so preview metadata can treat them like regular user-added bindings.

**`DiplomovaPrace/Services/FacilityNodeSeriesImportService.cs`:**
- Replaced the imported-only delete workflow with a unified `DeleteBindingAsync` path.
- Imported bindings still remove persisted files/state, while seeded bindings now create a persistent tombstone and are immediately removed from the active registry view.
- Duplicate import blocking continues to run against the unified registry view, so deleting either kind of binding re-allows the same exact signal code import.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- `Node bindings` preview now renders the same visible `Delete binding` action for every binding instead of special-casing imported ones.
- Removed user-visible origin/stage distinctions from binding preview metadata and switched the metadata block to a more readable multi-line layout.
- `NodeType` edit/add inputs now use datalist/autocomplete-like built-in suggestions for `area`, `bus`, and `weather` without the permanent suggestion buttons or helper-text-first UX.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor.css`:**
- Hardened binding-card wrapping so long file names break inside the card instead of overflowing the panel and the delete action stays visible.
- Removed obsolete chip styling that was only used by the old `NodeType` suggestion buttons.

### Build
- `dotnet build` successful via the workspace build task.

### Validation scope
- Completed only the requested binding unification, delete/import behavior alignment, `NodeType` UX cleanup, and binding-card layout fix.
- No additional analytics, KPI, LDC, baseline, near-base, or near-peak work was implemented.

### Date
[2026-04-27]

### Task
Opravný krok: viditelný delete button v Node bindings + skutečné built-in NodeType suggestions

### What changed

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Strengthened the `Node bindings` imported-binding action so each imported binding card now shows a visible red `Delete binding` button instead of a shorter, easier-to-miss affordance.
- Kept the existing delete path unchanged, so removal still goes through the already wired imported-binding delete workflow and refreshes the preview/analytics state through the current reload path.
- Replaced the static `NodeType` help-only UX with real built-in suggestions sourced from `FacilityBuiltInNodeTypes.GetKnownNodeTypes()`.
- Added clickable built-in suggestion chips for `area`, `bus`, and `weather` to both edit-node and add-node flows while preserving the existing free-text input and `datalist` suggestions for custom values.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor.css`:**
- Added small layout helpers so binding cards keep the delete action visible on narrow widths and the new built-in `NodeType` suggestion chips render clearly.

### Build
- `dotnet build` successful.

### Validation scope
- Only the requested UX repair was implemented.
- No additional analytics, KPI, LDC, baseline, near-base, or near-peak work was added.

### Date
[2026-04-27]

### Task
Implementační krok 3c: import/editor UX completion + series semantics for cumulative energy counters

### What changed

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Made the imported-binding delete action visibly actionable in `Node bindings` by replacing the icon-only affordance with a visible red delete button.
- Kept the existing delete workflow intact so preview reload and Signal Analytics refresh still happen through the already wired end-to-end delete path.
- Added built-in `NodeType` suggestions for `area`, `bus`, and `weather` to both edit and add-node inputs while preserving free-text entry.
- Signal Analytics now shows a small derived-view status for cumulative counters so `W`, `W_in`, and `W_out` are explicitly presented as interval-delta views instead of raw counter trajectories.

**`DiplomovaPrace/Services/FacilityNodeSeriesImportService.cs`:**
- Fixed fixed-CSV header handling so a first row shaped like `datetime_utc,<valueColumn>` is auto-detected and skipped instead of being counted as an invalid row.
- Hardened timestamp parsing for both confirmed formats:
  - `2018-01-02T19:15:00.000000+00:00`
  - `2017-12-30 23:00:00+00:00`

**`DiplomovaPrace/Services/FacilitySignalTaxonomy.cs` and `DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`:**
- Added an internal series-semantics layer with `sample_series` and `cumulative_counter`.
- Mapped `W`, `W_in`, and `W_out` to `cumulative_counter`; all other currently supported signals remain `sample_series`.
- Updated signal time-series parsing so cumulative counters are converted to derived interval deltas before trend rendering and basic-stat computation.
- Updated hourly/daily behavior for cumulative counters to sum derived interval deltas per bucket, producing a more user-meaningful Signal Analytics view.

### Build
- `dotnet build` successful.

### Validation scope
- Build validation performed after the UI, parser, and cumulative-counter analytics changes.
- No near-base, LDC, baseline, or additional KPI work was implemented in this step.

### Date
[2026-04-27]

### Task
Diagnostika a oprava hanging rout `/` a `/facility`

### What changed

**`DiplomovaPrace/Services/FacilityDataBindingRegistry.cs`:**
- Fixed route-blocking startup behavior in the binding registry constructor.
- Removed the sync-over-async call to `FacilityEditorStateService.GetImportedBindingsAsync().GetAwaiter().GetResult()` used while constructing the registry.
- Registry bootstrap now loads imported binding overlay data from a synchronous editor-state snapshot instead of blocking on async file/state loading during component DI activation.

**`DiplomovaPrace/Services/FacilityEditorStateService.cs`:**
- Added a synchronous imported-bindings snapshot read path used only for safe registry bootstrap.
- Added a synchronous state file loader so registry initialization no longer depends on async semaphore/file IO during page construction.

### Root cause
- `/` and `/facility` both resolve to `FacilityWorkbench`, but the request was hanging before `FacilityWorkbench.OnInitializedAsync`.
- The actual blocker was `FacilityDataBindingRegistry` construction during DI for the page graph.
- The constructor performed sync-over-async editor-state loading, which blocked route rendering before the first HTML byte was sent.

### Build
- Standard `dotnet build` task was blocked by a locked `bin\Debug\net10.0\DiplomovaPrace.dll` held by the active VS debugger process, so the default output build could not complete in this environment.
- Isolated validation build succeeded with `dotnet build .\DiplomovaPrace\DiplomovaPrace.csproj -o .\build-validation\route-hang-final`.

### Validation scope
- HTTP smoke test passed on the isolated final build:
  - `/health` → `200 OK`
  - `/` → `200 OK`
  - `/facility` → `200 OK`
- Temporary diagnostics used during investigation were removed after confirming the fix.

### Date
[2026-04-27]

### Task
Implementační krok 3b: binding conflict policy, delete workflow a krátká stabilizace import/analytics UX

### What changed

**`DiplomovaPrace/Services/FacilityNodeSeriesImportService.cs`:**
- Added an exact-signal duplicate guard before CSV parsing/writing, so a node cannot import another active binding with the same exact signal code.
- Added imported-binding delete workflow that removes the persisted binding, drops the live registry overlay entry, and performs best-effort cleanup of the stored normalized CSV file.

**`DiplomovaPrace/Services/FacilityEditorStateService.cs` and `DiplomovaPrace/Services/FacilityDataBindingRegistry.cs`:**
- Added persistent imported-binding deletion support in editor state.
- Extended live binding records with imported timestamp metadata and registry-side removal for imported overlay bindings.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Binding preview now shows import time for imported bindings.
- Added delete action for imported bindings directly in the node binding preview.
- Import success now keeps the active exact signal selection populated when the scope previously had no explicit signal selected.
- Import/delete path reuses preview reload so Signal Analytics availability and empty states refresh immediately after binding changes.

### Build
- `dotnet build` successful.

### Validation scope
- Build validation performed after duplicate guard, delete workflow, preview metadata, and analytics refresh changes.
- Browser smoke test was attempted, but the integrated browser and a direct localhost HTTP probe both timed out against `/facility`, so browser validation was not completed in this pass.

### Date
[2026-04-27]

### Task
Implementační krok 3: active signal selection, selection-scope availability, trend, basic stats a první bezpečná subtree aggregation

### What changed

**`DiplomovaPrace/Services/NodeAnalyticsPreviewService.cs`:**
- Added explicit selection-scope signal availability over the current node / subtree scope, grouped by exact signal code and annotated with signal family plus availability mode (`single-node series`, `aggregate sum`, `aggregate unsupported`).
- Added explicit exact-signal analytics read path so analytics no longer silently collapse to `primary binding` when a concrete signal was selected.
- Added first signal analytics result pipeline for:
  - active exact signal code resolution
  - trend loading over the selected signal
  - basic stats over the actually used series
  - first safe selection aggregation only for additive families `power` and `energy`
- Added signal-aware time-domain helpers so preset/custom interval anchoring can follow the active signal scope.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Added a new `Signal Analytics` panel to the overview analytics section.
- Added active exact signal code selection UI with explicit empty state when multiple signals exist and the user has not chosen one yet.
- Added visible scope availability summary for all exact signal codes in the current scope.
- Added trend rendering for the active signal and basic stats (`min`, `max`, `average`, `count of points`, `coverage`) over the real selected/aggregated series.
- Wired the panel into existing analytics reload flow and interval handling while keeping unsupported aggregate mixes explicit instead of falling back silently.

**`DiplomovaPrace/Components/Building/FacilityTimeSeriesPanel.razor`:**
- Added an option to hide the baseline toggle so the new signal trend stays within this step's scope and does not surface baseline controls.

### Build
- `dotnet build` successful.

### Validation scope
- Build validation performed after wiring the signal selection, availability logic, trend, stats, and first additive subtree aggregation.
- Intentionally not implemented in this step: baseline, LDC, near-base/near-peak, and other KPI/features outside the requested scope.

### Date
[2026-04-27]

### Task
Implementační krok 2: import write path, binding persistence a minimální import UI pro jednu časovou řadu

### What changed

**Persistent binding model and registry overlay:**
- Added persistent imported-binding state to `FacilityEditorStateService`, including exact signal code, unit, source metadata, storage-relative file path, fixed CSV format marker, and internal resolution metadata.
- Extended `FacilitySignalTaxonomy` with the requested `custom` exact signal code.
- Updated `FacilityDataBindingRegistry` to overlay persisted imported bindings on top of seeded CSV bindings, keep multi-binding reads intact, and expose imported file metadata needed by the read path.

**Import write path:**
- Added `FacilityNodeSeriesImportService` as a dedicated node-centric import path for the FacilityWorkbench import tab.
- The new import path validates the fixed CSV shape (`timestamp,value`), skips invalid rows with explicit per-line errors, normalizes valid rows into a canonical 2-column CSV, writes the normalized file into app-local storage, persists the new binding, and updates the live binding registry immediately.
- Internal resolution metadata is derived automatically from timestamps when spacing is regular; otherwise the binding is marked as `irregular`.

**`DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`:**
- Replaced the import placeholder with a minimal selected-node import form:
  - CSV file picker
  - exact signal code dropdown
  - unit input
  - optional Meter URN
  - optional source label
- Added minimal import validation feedback and a selected-node binding preview that lists exact signal code, unit, Meter URN/source label, file name, and stage.
- Updated focus-node curated detection so registry-backed imported bindings participate in the existing binding-based read path.

**Read path for imported files:**
- Updated `NodeAnalyticsPreviewService` to resolve imported binding files through the new binding metadata, support normalized fixed-series CSV files via the standard binding-based source path, and respect imported unit metadata for non-legacy sources without introducing new analytics features.

### Build
- `dotnet build` successful.

### Validation scope
- Build validation performed after wiring the import UI, binding persistence, and read-path support.
- No new analytics features were implemented in this step.

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
