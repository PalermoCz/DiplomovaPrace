# Azure Deployment Checklist

This document describes the steps required to deploy the final release candidate of the energy monitoring platform to Azure App Service.

## Target architecture

- **Host:** Azure App Service (Windows or Linux)
- **Database:** SQLite `metering.db`
- **Runtime Editor State:** `facility-editor-state.json` (and any `facility-editor-state.*.json` files)
- **Local File Imports:** `App_Data/facility-imports`
- **Email:** SMTP provider configured through app settings

## Required Azure App Service settings

The application relies on the following environment variables (Application Settings in Azure App Service):

```text
ASPNETCORE_ENVIRONMENT=Production
Email__SmtpHost=<smtp-host>
Email__SmtpPort=<smtp-port>
Email__From=<sender-email>
Email__Username=<smtp-username>
Email__Password=<smtp-password-or-key>
Email__EnableSsl=true
```

No hardcoded `DatabasePath` is required, the application defaults to placing `metering.db` in the ContentRootPath which works well for simple App Service setups.

## Deployment data warning

> [!CAUTION]
> The application uses local SQLite and file storage for production data.

- **Initial Deployment:** The first deployment **must** upload the runtime DB (`metering.db`), the editor state JSON files, and the `App_Data/facility-imports` folder from the golden release snapshot.
- **Subsequent Deployments:** Later deployments **MUST NOT** accidentally overwrite production `metering.db`. Use deployment slots or exclude `metering.db` and the data folders from web deploy publish commands.
- **Subsequent Deployments:** Later deployments **MUST NOT** accidentally overwrite `facility-editor-state.json` and `App_Data/facility-imports`.

## Manual live smoke checklist

Once deployed, perform the following validation:

1.  [ ] Open live `/login`
2.  [ ] Login as Owner (`matej.klibr@tul.cz` / `password`)
3.  [ ] Verify facility graph/data loads and is interactive
4.  [ ] Verify Members management opens
5.  [ ] Login as Admin (`admin@example.com` / `password`)
6.  [ ] Verify Admin cannot modify Owner role (locked)
7.  [ ] Login as Viewer (`viewer@example.com` / `password`)
8.  [ ] Verify Viewer restrictions (cannot access members, cannot create facility)
9.  [ ] Verify invite email if SMTP is configured (by adding a new user)
10. [ ] Verify set-password link from the email
11. [ ] Verify change password functionality
12. [ ] Verify unknown route page (e.g., `/unknown123`) shows safe 404 page
13. [ ] Verify Active Facility switching works properly
