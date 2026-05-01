# CURRENT_TASK.md

## Goal

Prepare the application as the final release candidate for Azure App Service deployment.

This is the final release stabilization task.

Do not add new product features.
Do not redesign UI.
Do not migrate persistence.
Do not touch graph/data/import logic unless a critical release blocker is found.

The goal is to:
- verify the current working state
- create a final golden snapshot
- prepare the Azure App Service deployment package/checklist
- verify production safety
- verify publish output
- produce a clear final "ready/not ready" deployment report

## Current final product status

The following features are considered complete and must not be redesigned:

### Facility graph/data
- Restored rich `Smart Company Facility`
- 116 schematic nodes
- 137 schematic edges
- 81 facility measurement maps
- CSV-backed imported bindings restored
- `App_Data/facility-imports` restored and working

### Users/auth
- login/logout/register
- change password
- invite-by-email / set-password token flow
- account dropdown in topbar
- role model:
  - Owner
  - Admin
  - Viewer

### Members
- Owner can manage Owner/Admin/Viewer, subject to safeguards
- Admin can manage Admin/Viewer only
- Admin cannot modify Owner rows
- Viewer cannot access Members management

### Multi-facility MVP
- Active Facility selector works
- Add/Create facility works
- New facility gets minimal graph:
  - Facility
  - Weather
  - Electricity
- Creator becomes Owner
- Switching between facilities works
- Existing `Smart Company Facility` remains intact

### Legacy cleanup
- old legacy sidebar removed
- visible CSV Import navigation removed/hidden
- unknown route no longer shows legacy UI
- user-facing runtime text should be English only

### Chart polish
- Overview aggregate chart no longer shifts/jumps when treemap hover preview appears

## Final architecture decision

For this thesis release, the final deployment architecture is:

- Azure App Service
- SQLite database file: `metering.db`
- runtime editor-state JSON:
  - `facility-editor-state.json`
  - any facility-scoped state files if created
- local file imports:
  - `App_Data/facility-imports`
- SMTP email provider configured through Azure App Service app settings / environment variables

Do NOT migrate to Azure SQL.
Do NOT move files to Azure Blob Storage.
Do NOT redesign persistence.
Do NOT implement another deployment architecture.

This SQLite + file-storage approach is the final MVP/prototype architecture for the diploma thesis release.

## Current known final demo accounts

Use these credentials exactly.

Owner:
- email: `matej.klibr@tul.cz`
- password: `password`

Admin:
- email: `admin@example.com`
- password: `password`

Viewer:
- email: `viewer@example.com`
- password: `password`

Do not use Czech word `heslo` as password.

## Required work

### A. Final DB sanity check

Inspect `DiplomovaPrace/metering.db`.

Verify and report:

1. Users:
   - `matej.klibr@tul.cz`
   - `admin@example.com`
   - `viewer@example.com`
2. Memberships:
   - `matej.klibr@tul.cz` is Owner of `Smart Company Facility`
   - `admin@example.com` is Admin of `Smart Company Facility`
   - `viewer@example.com` is Viewer of `Smart Company Facility`
3. If additional facilities exist:
   - report their names
   - report their memberships
   - do not delete them unless they are obvious failed test garbage and removal is explicitly safe
4. Orphan memberships:
   - must be none
5. Schematic node counts by facility
6. Schematic edge counts by facility
7. Measurement map counts by facility
8. Confirm `Smart Company Facility` still has:
   - 116 nodes
   - 137 edges
   - 81 measurement maps

If any DB issue is found, stop and report before making destructive changes.

### B. Final runtime asset sanity check

Verify the following files/folders exist and are not empty:

- `DiplomovaPrace/metering.db`
- `DiplomovaPrace/facility-editor-state.json`
- any facility-scoped state files such as `facility-editor-state.2.json`
- `DiplomovaPrace/App_Data/facility-imports`

Report:
- DB file size
- editor-state JSON size
- number of files under `App_Data/facility-imports`
- whether any `metering.db-wal` / `metering.db-shm` files exist

Do not delete WAL/SHM unless the app is stopped and it is clearly safe. Prefer reporting.

### C. Create final golden release snapshot

Create a new timestamped snapshot folder:

- `release-snapshots/final-release-candidate-YYYYMMDD-HHMMSS/`

Snapshot at minimum:

- `DiplomovaPrace/metering.db`
- `DiplomovaPrace/facility-editor-state.json`
- any `facility-editor-state.*.json` files
- `DiplomovaPrace/App_Data/facility-imports/`
- `AI/AZURE_DEPLOYMENT_CHECKLIST.md` if it exists
- current `.gitignore`
- current `appsettings.json`

Create a manifest file:

- `manifest.md`

The manifest must include:

1. timestamp
2. snapshot path
3. DB file size
4. editor-state file sizes
5. facility-imports file count
6. final users
7. final memberships
8. facilities list
9. graph counts by facility
10. edge counts by facility
11. measurement map counts by facility
12. note that this is the final SQLite/App_Data based thesis release candidate

### D. Production safety audit

Inspect and verify:

1. Development-only account normalization runs only when `IsDevelopment()` is true.
2. No demo password seeding runs in Production.
3. Invite tokens are not logged in Production.
4. `appsettings.json` contains only safe placeholders.
5. No real SMTP secret is committed.
6. SMTP secrets are expected from environment variables / Azure App Service Application Settings.
7. No hardcoded local absolute path is required in Production runtime.
8. If `DatabasePath` or similar setting exists, document exactly how it should be configured in Azure.
9. Confirm `.gitignore` excludes:
   - local secrets
   - runtime sidecar DB files if appropriate
   - temporary build-validation artifacts if appropriate

If you find a production safety blocker, fix it narrowly and report it.

### E. Build verification

Run:

```powershell
dotnet clean DiplomovaPrace/DiplomovaPrace.csproj
dotnet build DiplomovaPrace/DiplomovaPrace.csproj
````

Build must pass.

### F. Publish verification

Run:

```powershell
dotnet publish DiplomovaPrace/DiplomovaPrace.csproj -c Release -o build-validation/final-release-publish
```

Then run from publish output only for publish verification:

```powershell
cd build-validation/final-release-publish
$env:ASPNETCORE_ENVIRONMENT="Production"
.\DiplomovaPrace.exe --urls http://localhost:5020
```

Verify:

*   `/login` returns 200
*   `/set-password` returns 200 or safe expired-token page
*   `/change-password` redirects to login when unauthenticated
*   `/facility` redirects to login when unauthenticated
*   `app.css` loads
*   `css/building.css` loads if it exists/is expected
*   static assets are served correctly
*   no content-root/static-assets failure

Stop the publish verification app after testing.

### G. Local Development final smoke test

Only do a short smoke test. Do not spend excessive time on long browser automation.

Runtime validation rules:

*   Use a fresh Antigravity browser session.
*   Do not rely on an already-open/shared browser tab.
*   Stop old dotnet processes before running.
*   Run from repo root:
    `C:\Users\Matthew\Desktop\Diplomova_Prace\Code\DiplomovaPrace`
*   Start app with:
    `dotnet run --project .\DiplomovaPrace\DiplomovaPrace.csproj`
*   Open:
    `http://localhost:5016/login`
*   Do not run from publish output unless explicitly doing publish verification.
*   Do not touch graph/data/bindings unless explicitly instructed.
*   Build success is not enough for auth/routing/facility switching changes.
*   Do not use the default Open Browser preview URL if it opens a random localhost port.

Use these credentials exactly:

Owner:

*   email: `matej.klibr@tul.cz`
*   password: `password`

Admin:

*   email: `admin@example.com`
*   password: `password`

Viewer:

*   email: `viewer@example.com`
*   password: `password`

Do not use Czech word `heslo` as password.

Validate as Owner:

1.  login works
2.  `/facility` loads
3.  `Smart Company Facility` loads
4.  rich graph is visible
5.  node data/bindings still appear for at least one known meter such as `H1.Z10`
6.  Active Facility selector opens
7.  if another facility exists, switching works
8.  Members opens
9.  Owner/Admin/Viewer demo accounts are visible

Validate as Admin:

1.  login works
2.  Members opens
3.  Owner row is locked/read-only
4.  Admin can manage Viewer/Admin rows only

Validate as Viewer:

1.  login works
2.  Viewer cannot access Members management
3.  Viewer cannot create facility

For small visual-only checks, manual validation by the user is acceptable. Do not spend excessive time on browser automation.

### H. Azure deployment checklist

Create or update:

*   `AI/AZURE_DEPLOYMENT_CHECKLIST.md`

The checklist must be clear enough for a human to follow.

Include:

#### Target architecture

*   Azure App Service
*   SQLite `metering.db`
*   `facility-editor-state.json`
*   facility-scoped editor-state files if present
*   `App_Data/facility-imports`
*   SMTP provider through app settings

#### Required Azure App Service settings

At minimum:

```text
ASPNETCORE_ENVIRONMENT=Production
Email__SmtpHost=<smtp-host>
Email__SmtpPort=<smtp-port>
Email__From=<sender-email>
Email__Username=<smtp-username>
Email__Password=<smtp-password-or-key>
Email__EnableSsl=true
```

If the app uses any database path setting, include it explicitly.

#### Deployment data warning

Clearly state:

*   first deployment must upload runtime DB and data files
*   later deployments must not accidentally overwrite production `metering.db`
*   later deployments must not accidentally overwrite `facility-editor-state.json`
*   later deployments must not accidentally overwrite `App_Data/facility-imports`

#### Manual live smoke checklist

Include:

1.  open live `/login`
2.  login as Owner
3.  verify facility graph/data
4.  verify Members
5.  verify Admin cannot modify Owner
6.  verify Viewer restrictions
7.  verify invite email if SMTP is configured
8.  verify set-password link
9.  verify change password
10. verify unknown route page
11. verify Active Facility switching

### I. Git/release status report

Do not commit automatically unless explicitly asked.

Report:

1.  `git status -sb`
2.  list of modified files
3.  list of untracked files
4.  whether `metering.db` is modified
5.  whether snapshot folder is untracked
6.  what should be committed
7.  what should remain uncommitted
8.  what should be ignored

## Do NOT change

Do not:

*   add features
*   redesign UI
*   modify graph/data/import logic
*   delete existing working facility data
*   migrate database
*   change deployment architecture
*   clean up more DB data unless it is clearly safe and reported
*   remove final demo accounts
*   remove `New Facility` / additional facility unless explicitly requested

## Expected final report

At the end report:

1.  exact files changed
2.  final DB sanity result
3.  final users/memberships/facilities
4.  final graph/data counts
5.  final runtime asset check
6.  golden snapshot path
7.  production safety result
8.  build result
9.  publish verification result
10. local smoke result
11. Azure deployment checklist path
12. git status summary
13. whether app is ready for Azure App Service deployment
14. exact manual steps remaining before live
15. any remaining release risks