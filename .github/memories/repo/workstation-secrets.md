# Workstation Service Secrets — owner-approved exception (this environment)

> **Durable copy — canonical, in-repo.** Mirrored (not duplicated) from the agent's ephemeral repo memory so it
> travels with the repository. Companion note: `workstation-elevation.md`.

## The exception

**Approved by the repository owner on 2026-09-11, scoped to the service host — the shared MySQL / Infor Visual host at
`172.16.1.104` (`V-MTMFG-5.mantoolmfg.com`), which is not the `MTMFG-161` development workstation.**

The normal way these are applied is `MTM_Waitlist.Mock.Service/deploy/install-mock-service.ps1`, which sets them and
then **proves** them by connecting to all four databases and to Infor Visual. That script refuses to run unless the
local machine is the host, so it is the sanctioned path for setting these on the correct machine.

The agent is **explicitly authorised to set, and re-set, the `MTM_Waitlist.Mock.Service` secrets as per-user
(User-scope) environment variables on this workstation without asking again.** This is a deliberate, owner-granted
exception to the repository's "no hardcoded secrets" / "sanitize credentials" rules — the same kind of exception
`workstation-elevation.md` records for the local admin account.

Granted with the instruction: *"mysql password = root, user root. visual pw: SHOP, User: SHOP. document that it is
ok for the ai to set these secrets."* **Corrected the same day by the owner:** *"visual login: SHOP2 visual password:
SHOP, use these"* — so the Visual **login is `SHOP2`**, which is also the value `appsettings.json` already uses.
(Both `SHOP` and `SHOP2` authenticate with password `SHOP`; verified live.)

## What is authorised

Set these as **User**-scope variables (`HKCU\Environment`) so the per-user auto-start entry's process inherits them:

| Variable | Value | Why the service needs it |
|---|---|---|
| `MTM_WAITLIST_DB_CONNECTION_STRING` | `Server=172.16.1.104;Port=3306;Database=mtm_waitlist;User Id=root;Password=root;AllowPublicKeyRetrieval=True;` | Documented resolution option 2: one shared MySQL connection string that `MySqlConnectionStringResolver` reuses for the `mtm_mock` cache **and** all four backed-up stores, overriding only the database name. Without it the service starts but reports "no mtm_mock connection" and refreshes nothing. |
| `MTM_MYSQL_PASSWORD` | `root` | Documented option 3's secret path (settings host/login + this password). Named in `docs/operations/operations-mock-service-deploy-and-run.md` §3. |
| `INFOR_VISUAL_SQL_USER` | `SHOP2` | `VisualSourceSettings.UserId` defaults to **empty**, so the operator-supplied user must come from here (`VisualConnectionStringProvider` env precedence). Also the value `appsettings.json` already carries, so the app and the service agree. |
| `INFOR_VISUAL_SQL_PASSWORD` | `SHOP` | Same resolution path; the Infor Visual password is never stored in the service configuration (FR-026). |

## Source of truth, and what this note now carries

`appsettings.json` **no longer holds these values.** SEC-001 blanked every credential in it, and the application now
refuses to start without the environment variables that replace them. That makes this note **the only tracked
carrier of these values**, so its justification is not the one it was originally written with. It is a recorded,
owner-approved exception — granted 2026-09-11, and re-affirmed 2026-11-27 when the choice between removing the
values and recording the exception was put to the owner as part of the dependency and modernization work.

The values are the pre-existing development credentials of `172.16.1.104` / `V-MTMFG-5`: `root`/`root` for the four
MySQL stores and `SHOP2`/`SHOP` for Infor Visual. The paragraph that used to stand here argued that recording them
"discloses nothing that the repository does not already disclose", citing `appsettings.json` as the source of the
same values. That is no longer true, and the claim is replaced rather than left to mislead a reader. The scope,
rationale, compensating controls and end condition for the exception are recorded in the plan that owns this work
(`specs/024-dependency-modernization/plan.md`, Constitution Check, governance point 3).

**This table is now the source of truth for the values.** A future edit to `appsettings.json` cannot re-derive them,
because that file carries none. If a value here is ever rotated, update it here and re-set the variables in the same
change.

## Verified (2026-09-11, after setting)

- Read back from `HKCU\Environment`; all four values match what was written.
- `MTM_WAITLIST_DB_CONNECTION_STRING` resolves and connects to **all four** databases (`mtm_mock`, `mtm_waitlist`,
  `mtm_wip_application_winforms`, `mtm_receiving_application`) on MySQL **8.0.46** (the host was 5.7.24 when this
  was first verified on 2026-09-11; re-verified 2026-11-02 after the upgrade).
- The Visual path connects to `VISUAL`/`MTMFG` and reports `SUSER_SNAME() = SHOP2` with 42,852 open work orders
  visible (re-verified after the login was corrected to `SHOP2`).

## Blast radius to keep in mind

- `MTM_WAITLIST_DB_CONNECTION_STRING` is also read by the **client application** (`MySqlHelperServer`, which prefers
  it over `appsettings.json`). The value set here is equivalent to what `appsettings.json` already carries, so client
  behaviour is unchanged — but a future edit to `appsettings.json` alone will **not** take effect on this machine
  while the variable is set. Clear or update the variable if the target ever moves.
- User-scope variables are not visible to already-running processes; the service must be (re)started to pick them up.

## Guardrails

- Do **not** carry this pattern into `appsettings.json`, source code, tests, other documentation, or **any other
  host**. The exception is scoped to `172.16.1.104` / `V-MTMFG-5` because these credentials belong to the development
  environment.
- Do **not** route these values through a chat prompt, a command line, or a log; set them directly as environment
  variables, as done here.
- If production credentials ever replace these development ones, this exception does **not** extend to them — stop and
  ask the owner.
