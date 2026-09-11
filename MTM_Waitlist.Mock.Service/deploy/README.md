# Deploying `MTM_Waitlist.Mock.Service`

Executable deployment for the on-host half of the Module_Mock cached fallback:
`install-mock-service.ps1`.

`MTM_Waitlist.Mock.Service/README.md` §2–§5 remains the narrative authority for *what* a deployment is
(publish, deploy, first run, verify, uninstall). This folder is the **executable form** of it: one
idempotent script that does all of it and refuses to lie about the result.

---

## ⛔ Server-only — do not run this from a developer workstation

**Run it only when VS Code (and therefore the agent) is running on the cache host itself.**

The service belongs on the machine that reaches both MySQL and Infor Visual. Running this from a
workstation would install a tray-only, auto-starting service on the wrong machine and point
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run` at whatever folder it was launched from.

The script enforces this. It checks that one of **this machine's own IPv4 addresses** is the expected
server address — by default the MySQL `Server=` value in `appsettings.json`, i.e. `172.16.1.104` — and
**refuses to run with exit code 2 otherwise**:

```text
REFUSING TO RUN: this machine is not the cache host.
  this machine : 10.0.0.5
  expected host: 172.16.1.104
```

| | |
|---|---|
| Verified cache host (2026-09-11) | `V-MTMFG-5.mantoolmfg.com` = `172.16.1.104` |
| Override for a deliberately chosen non-server host | `-AllowNonServerHost` — and record *why* in the change that uses it |

**Agent instruction: do not attempt to run this script, and do not work around the guard, unless the
environment is the server (`172.16.1.104`).** If you are not on that machine, hand the deployment to
someone who is, or move the workspace to the server.

---

## Prerequisites

| Requirement | Why |
|---|---|
| Running **on the cache host** | The guard above; and the install folder must be local to the host. |
| `appsettings.json` present in the repo | The script derives the expected server address, the MySQL connection string and the Infor Visual login/password from it, so it is **not a second source of truth** and holds **no secrets of its own**. |
| The solution built at least once | `bin\x64\Debug\...\win-x64\{MySqlConnector,Microsoft.Data.SqlClient}.dll` are loaded to *prove* the secrets by connecting, not merely by reading them back. |
| `dotnet` SDK | Only when `-Publish` is used. |

Secrets are the owner-approved exception recorded in
`.github/memories/repo/workstation-secrets.md`: the script sets the four User-scope variables that file
describes, and nothing else.

---

## Usage

```powershell
# normal deployment: publish fresh, deploy, install + verify secrets, start and health-check
pwsh -NoProfile -ExecutionPolicy Bypass -File ./deploy/install-mock-service.ps1 -Publish

# redeploy an existing publish output (no rebuild)
pwsh -NoProfile -ExecutionPolicy Bypass -File ./deploy/install-mock-service.ps1

# deploy to a different folder (must still be the cache host)
pwsh -NoProfile -ExecutionPolicy Bypass -File ./deploy/install-mock-service.ps1 -TargetPath D:\Services\MTM_Waitlist.Mock.Service

# a clean sweep: also delete the stored configuration, the DPAPI credential and the backups
pwsh -NoProfile -ExecutionPolicy Bypass -File ./deploy/install-mock-service.ps1 -PurgeState
```

| Parameter | Default | Effect |
|---|---|---|
| `-TargetPath` | `C:\Services\MTM_Waitlist.Mock.Service` | Install folder. **Must be final before the first run** — a first run registers auto-start with the path it was launched from. |
| `-SourcePath` | `MTM_Waitlist.Mock.Service/bin/publish/win-x64` | Publish output to deploy from. |
| `-ServerHost` | MySQL `Server=` from `appsettings.json` | Expected cache host for the guard. |
| `-Publish` | off | Run `dotnet publish -c Release -p:PublishProfile=win-x64-selfcontained` first. |
| `-SkipSecrets` | off | Do not set or verify the environment secrets. |
| `-SkipServiceStart` | off | Do not start the service or run the health checks. |
| `-PurgeState` | off | Also delete `%LOCALAPPDATA%\MTM_Waitlist.Mock.Service` (configuration, credential, backups). |
| `-AllowNonServerHost` | off | Bypass the server-only guard. Deliberate use only. |
| `-HealthCheckTimeoutSeconds` | 60 | How long to wait for the API port. |

---

## What it does, in order

| # | Step | Notes |
|---|---|---|
| 1 | **Server-only guard** | Refuses with exit code 2 when this machine is not the host. |
| 2 | **Stop the running service** | Force-stops every `MTM_Waitlist.Mock.Service` process, waits up to 30 s, then confirms the API port is free. The service exits only on an explicit tray *Quit*, so a forced stop is the only scriptable option. |
| 3 | **Delete the previous deployment** | Removes the install folder outright, then redeploys — no stale files can survive a redeploy. State under `%LOCALAPPDATA%` is **kept** unless `-PurgeState`. |
| 4 | **Publish and copy** | Optional `dotnet publish`, then a full copy. A `NETSDK1198` "publish profile not found" warning is treated as a **failure**, because it means the build silently became framework-dependent. |
| 5 | **Verify the deployment** | File count matches source; every required path present (five population reads, tray icon, source queries, restore artifacts, WinAppSDK runtime); self-contained markers `System.Private.CoreLib.dll`/`hostfxr.dll`/`coreclr.dll` present **and** `runtimeconfig.json` using `includedFrameworks`; the corrected work-order addressing guard (T144) present in the deployed population reads. |
| 6 | **Install and verify secrets** | Sets the four User-scope variables from `appsettings.json`, reads them back from `HKCU\Environment`, applies them to the script's **own** process so the service it launches inherits them, and then **proves** them: connects to `mtm_mock`, `mtm_waitlist`, `mtm_wip_application_winforms` and `mtm_receiving_application`, and authenticates to Infor Visual. |
| 7 | **Start and health-check** | Starts the service, waits for port 5760, then asserts: process responding, **no top-level window** (tray-only), `GET /api/status` returns **401** without a credential, a **second launch redirects and exits** (single instance), and the `HKCU` auto-start entry points at *this* install folder. |
| 8 | **Summary** | A PASS/FAIL/WARN table. Exit `0` on success, `1` if any check failed, `2` refused (not the server host), `3` precondition failure. |

**If any check in steps 1–6 fails, the service is not started** — a half-deployed service that looks
healthy is worse than a failed deployment.

---

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Deployed and validated. |
| `1` | A validation check failed; the service was not started. |
| `2` | Refused: this machine is not the cache host. |
| `3` | Precondition failure (no publish output, expected server undeterminable). |

---

## Side effects to expect

- **Auto-start.** `ServiceConfiguration.AutoStartAtLogon` defaults to `true`, so a first run registers
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\MTM_Waitlist.Mock.Service` pointing at the install
  folder. The script reports where it points and warns if it points somewhere else.
- **A refresh cycle.** A freshly started service with no run records treats the first cycle as due, so
  the first start refreshes the shared `mtm_mock` cache from Infor Visual. On the host that is the
  service's job; be aware that it rewrites the mirror snapshots.
- **Backups** run on their configured local times (01:00 / 01:20 / 01:40 / 02:00 by default) and write
  under `%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\backups\<database>\`.
- The service is **left running** by design.

---

## Rollback

```powershell
# stop it, drop the auto-start entry, remove the install folder
Get-Process MTM_Waitlist.Mock.Service -ErrorAction SilentlyContinue | Stop-Process -Force
Remove-ItemProperty HKCU:\Software\Microsoft\Windows\CurrentVersion\Run -Name MTM_Waitlist.Mock.Service -ErrorAction SilentlyContinue
Remove-Item C:\Services\MTM_Waitlist.Mock.Service -Recurse -Force
```

Add `Remove-Item "$env:LOCALAPPDATA\MTM_Waitlist.Mock.Service" -Recurse -Force` for a full reset — that
is what discards the DPAPI credential, the schedules and the backups. Deleting the install folder alone
**keeps** all of it (see `MTM_Waitlist.Mock.Service/README.md` §5).

---

## Verified

Deployed and validated on the host `V-MTMFG-5` (`172.16.1.104`) on 2026-09-11 — see the execution note in
`specs/001-module-mock-visual-fallback/tasks.md`.
