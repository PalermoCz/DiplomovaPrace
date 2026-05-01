# CURRENT_TASK.md

## Goal
Implement the smallest possible first auth-shell milestone for the app.

## Problem
The project is ready to move from data/runtime cleanup into users/auth, but the full ownership/invitation/role system would be too large as a first step.

We need the smallest working v1 auth shell:
- local user accounts
- login / register / logout
- persistent cookie auth
- authenticated shell
- account identity visible in the topbar

## Desired direction
Implement only the first auth-shell milestone.
Do not implement full invitations, memberships UI, or facility admin pages yet.

## Scope
Implementation only.

Read first:
- AI/AGENT_CONTEXT.md
- AI/CURRENT_TASK.md
- AI/WORKLOG.md
- AI/DATA_VISUALIZATION_AUDIT.md

Then inspect at minimum:
- DiplomovaPrace/Program.cs
- DiplomovaPrace/Components/Layout/FacilityTopbar.razor
- DiplomovaPrace/Components/Layout/FacilityLayout.razor
- DiplomovaPrace/Components/Pages/FacilityWorkbench.razor
- DiplomovaPrace/Persistence/AppDbContext.cs
- existing facility entities

## Required implementation
1. Add `AppUsers` persistence model/table.
2. Add local email + password registration.
3. Add local login/logout with cookie authentication.
4. Add the minimum auth pipeline registration in Program.cs.
5. Add a basic authenticated-shell guard so unauthenticated users are redirected to `/login`.
6. Add a right-side account chip in the topbar showing the current signed-in email and a sign-out action.
7. Keep the rest of the product surface intact.

## Do NOT change
- Do not implement invitations yet
- Do not implement facility membership management UI yet
- Do not implement role administration UI yet
- Do not implement password reset
- Do not implement email verification
- Do not implement MFA
- Do not redesign FacilityWorkbench

## Constraints
- Keep the milestone as small and buildable as possible
- Use local email + password auth only
- Keep the UI change minimal: account chip only
- Do not create duplicate ownership source-of-truth logic yet

## Guardrails
- This is the first auth-shell milestone, not the whole user-management system
- Prefer FacilityMembership as future role source-of-truth; do not add conflicting ownership fields unless strictly necessary
- Build must pass
- Update AI/WORKLOG.md after implementation
