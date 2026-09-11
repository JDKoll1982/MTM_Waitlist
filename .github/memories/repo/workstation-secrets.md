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
| `MTM_MYSQL_PASSWORD` | `root` | Documented option 3's secret path (settings host/login + this password). Named in `MTM_Waitlist.Mock.Service/README.md` §3. |
| `INFOR_VISUAL_SQL_USER` | `SHOP2` | `VisualSourceSettings.UserId` defaults to **empty**, so the operator-supplied user must come from here (`VisualConnectionStringProvider` env precedence). Also the value `appsettings.json` already carries, so the app and the service agree. |
| `INFOR_VISUAL_SQL_PASSWORD` | `SHOP` | Same resolution path; the Infor Visual password is never stored in the service configuration (FR-026). |

## Source of truth, and why this adds no new exposure

These are **pre-existing development credentials that are already committed** in `appsettings.json`:
`StartupDatabaseOptions.ConnectionString` (`User ID=root;Password=root;`) and
`InforVisualDatabaseOptions` (`User=SHOP2;Password=SHOP` — note both `SHOP` and `SHOP2` authenticate with password
`SHOP`; verified live 2026-09-11). Recording them here discloses nothing that the repository does not already
disclose; the environment variables change *where the service reads them from*, not who can already see them.

If `appsettings.json` changes, this note and the variables must be re-derived from it — treat the connection string as
the source of truth, not this table.

## Verified (2026-09-11, after setting)

- Read back from `HKCU\Environment`; all four values match what was written.
- `MTM_WAITLIST_DB_CONNECTION_STRING` resolves and connects to **all four** databases (`mtm_mock`, `mtm_waitlist`,
  `mtm_wip_application_winforms`, `mtm_receiving_application`) on MySQL **5.7.24**.
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
