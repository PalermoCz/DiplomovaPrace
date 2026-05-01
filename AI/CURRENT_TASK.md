# CURRENT_TASK.md

## Goal
Implement the next users/auth milestone properly:
- premium topbar account area UX
- invite-by-email access flow
- set-password / account-activation flow
- change password for signed-in users
- role policy guard: Admin cannot invite or promote Owner

## Product direction
The current members feature is no longer enough.

We now want a real and simple collaboration flow that behaves like a normal web app:

1. Facility members can be managed from the Members panel.
2. If the typed email belongs to an existing account:
   - attach that account to the active facility with the selected role.
3. If the typed email does NOT belong to an existing account:
   - create the account
   - send a real invite email
   - the invited user should open a link and set their own password
   - no plaintext or pre-generated password should be emailed

This should behave like a real invite/access flow, not only "attach an already registered user".

## Why this direction
ASP.NET Core Identity already supports:
- users
- passwords
- confirmation/reset tokens
- email confirmation / recovery style flows

We should use token-based account setup instead of sending a generated password by email.

## Scope
Implementation only.

Read first:
- AI/AGENT_CONTEXT.md
- AI/CURRENT_TASK.md
- AI/WORKLOG.md

Then inspect at minimum:
- current authentication service and login/register flows
- current members panel flow
- current topbar implementation
- current membership service/model
- current startup/auth configuration
- any existing email sender abstractions or placeholders
- any current account management/profile surfaces

## Required work

### A. Topbar redesign
Make the right side of the topbar feel premium and visually aligned with the quality of:
- the app title
- the Active Facility selector

Requirements:
1. Members stays as a separate action for Owner/Admin.
2. Email + sign-out must no longer look like three random panels.
3. Redesign the account area into a single cohesive account control/menu.
4. It should feel visually premium and consistent with the rest of the topbar.
5. Keep all user-facing text English only.

### B. Invite-by-email flow
Replace the current "existing user only" semantics with real invite/access semantics.

Required behavior:
1. Owner/Admin enters email + target role in Members panel.
2. If the email belongs to an existing account:
   - add membership for the active facility if not already present
3. If the email does NOT belong to an existing account:
   - create the account
   - generate a secure token-based setup flow
   - send a real email with a link that allows the user to set their own password
4. Do NOT send plaintext or pre-generated passwords by email.
5. Keep clear validation for:
   - invalid email format
   - already a member
6. For invalid email format, show only the single lower error message (do not show duplicate inline + lower message).

### C. Account activation / set-password flow
Implement the invited-user onboarding path.

Requirements:
1. Add a page/flow for invited users to set their password from the emailed link.
2. Use a proper Identity-style token flow.
3. If needed, combine with email confirmation logic.
4. Keep the flow small and practical; do not over-engineer invitation lifecycle yet.

### D. Change password
Add a simple change-password feature for signed-in users.

Requirements:
1. Signed-in users must be able to change their password.
2. Prefer placing this under the account area/menu.
3. Require current password + new password + confirmation.
4. Keep the flow simple and clear.

### E. Role policy rules
Enforce role-creation/promotion restrictions.

Required policy:
1. Owner can invite / assign / promote:
   - Owner
   - Admin
   - Viewer
2. Admin can invite / assign / promote only:
   - Admin
   - Viewer
3. Admin must NOT be able to:
   - invite Owner
   - promote anyone to Owner
4. Apply this both:
   - in UI
   - and in backend/service validation
5. Keep last-owner protection intact.
6. Keep self-lockout protection intact.

### F. Email sending implementation
Implement a real outgoing email path suitable for this app milestone.

Requirements:
1. Use a proper email sender abstraction/service.
2. Prefer a real email provider approach compatible with ASP.NET Core Identity practices.
3. Avoid fragile ad-hoc SMTP-only design unless absolutely required.
4. Keep secrets/config outside source code.
5. If real sending cannot be completed fully in the current environment, implement the flow cleanly and document the exact remaining configuration step.

## Do NOT change
- Do not touch schematic graph restore
- Do not touch bindings/import logic
- Do not redesign graph/workbench architecture
- Do not broaden into a full enterprise collaboration/invitation lifecycle
- Do not weaken current role safeguards

## Constraints
- Keep the milestone practical and shippable
- Build must pass
- Runtime sanity check must pass
- All user-facing text must remain English only

## Guardrails
- Use token-based password setup, not emailed plaintext passwords
- Treat backend role restrictions as mandatory, not only UI hints
- If any email infrastructure step cannot be completed automatically, leave the code clean and report the exact manual configuration needed
- Update AI/WORKLOG.md after implementation