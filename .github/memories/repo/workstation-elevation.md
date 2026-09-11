# Workstation elevation on the current development environment

> **Owner-approved exception (2026-09-11).** `.github/memories/README.md` says to *"reference dev accounts/env
> vars/appsettings rather than echoing passwords"*, and `.github/copilot-instructions.md` says *"no hardcoded
> secrets"*. The repository owner explicitly authorised this one exception so that an agent or operator working
> on this workstation can elevate without asking. The exception is scoped to **this document and this
> workstation only** — it does not license credentials anywhere else in the repository, in `appsettings.json`,
> in code, or in test fixtures.

## The environment

- **Windows host**: `MTMFG-161` — the local development workstation used for the MTM_Waitlist work (this
  machine, not the shared MySQL/Infor Visual host at `172.16.1.104`).
- **Signed-in user**: the ordinary developer account (`jkoll`). That account is **not** a member of the local
  `Administrators` group, so elevation raises a **credential** prompt rather than a consent prompt — you will be
  asked for the admin password, not just a Yes/No.
- **Who is a local administrator on this host** (verified 2026-09-11): `Administrator`, `johnadmin`,
  `MANTOOLMFG\Domain Admins`, `MANTOOLMFG\Local Admin Group`, `MTMFG`. The domain groups are the site's
  enterprise path; `johnadmin` is the local account this note documents.
- **Live internal stores** the app reads and writes (always live — never cached, FR-001/FR-027):
  `mtm_waitlist`, `mtm_wip_application_winforms`, `mtm_receiving_application`, all on `172.16.1.104`.

## When elevation is needed

Use the local admin account below for anything that requires administrator rights, for example:

- Installing or repairing the Windows App SDK / MSIX tooling used by the WinUI 3 build.
- Writing machine-wide registry keys (`HKLM`), services, or firewall rules.
- Running a tool that must bind a privileged port or touch another user's profile.
- Driving an **elevated** window with UI Automation: a non-elevated shell cannot automate an elevated window at
  a different integrity level (see `.github/instructions/winui3-ui-automation.instructions.md` → *Rules*).

Elevation is **not** needed for the normal loop: `dotnet build`, `dotnet test`, launching the unpackaged
`MTM_Waitlist.exe`, running `MTM_Waitlist.Mock.Service.exe` from a user-writable folder, or the
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run` auto-start entry the service manages.

## The account

| Field | Value |
|---|---|
| Username | `.\johnadmin` |
| Password | `JohnKoll4330` |

Notes on the form:

- The `.\` prefix means the **local machine** SAM database (`MACHINE\johnadmin`), not a domain account. Keep it
  when the prompt or command is ambiguous about the realm. Verified on `MTMFG-161` (2026-09-11): the account's
  full name resolves to `MTMFG-161\johnadmin`, so `.\johnadmin` is the correct local form on this host.
- From a non-elevated shell, elevate with:
  - `runas /user:.\johnadmin "C:\path\to\app.exe"` — prompts for the password interactively, or
  - `Start-Process -Verb RunAs -FilePath "C:\path\to\app.exe"` — raises the UAC prompt for the admin account.
  - Do **not** pass the password on a command line (`runas` has no non-interactive password switch that keeps it
    out of the process arguments), and do not route it through a model or a chat message when a prompt can take
    it directly.

## Verification (2026-09-11)

Checked against the local SAM on `MTMFG-161`, without elevating:

| Claim | Check | Result |
|---|---|---|
| The password in this note is correct | `PrincipalContext('Machine').ValidateCredentials(...)` | **valid** |
| The account exists and can sign in | `Get-LocalUser -Name johnadmin` | exists, **enabled**, password last set 2026-07-08 |
| The account is a local administrator | `net localgroup administrators` | **member** |
| `.\johnadmin` is the right form on this host | `Win32_UserAccount` caption | resolves to `MTMFG-161\johnadmin` |
| Elevation is genuinely required (not just consent) | `net localgroup administrators` | `jkoll` is **not** a member |
| The normal loop runs unelevated | `WindowsPrincipal.IsInRole(Administrator)` | **False**, as intended |

**Not proven here:** the UAC handshake itself — that typing this password into the elevation prompt actually
grants an elevated token. That step can only be performed at the prompt, by a human. To close it out, run
`Start-Process -Verb RunAs -FilePath "C:\Windows\System32\cmd.exe"`, enter `.\johnadmin` and the password, and
confirm the new window reports elevated.

## If this workstation stops being the developer's own machine

Because the credential now lives in git history, moving this repo to a shared machine, a CI runner, or an
off-host backup means the password should be rotated on the workstation. The document is then out of date and
this section is the thing to update.
