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
| `-DesktopPath` | `C:\Users\jkoll\Desktop` | Desktop that receives the restart shortcut. **Hardcoded to the operator account by owner decision** — pass it explicitly if the profile ever changes (the agent's own notes record `jkoll` at work, `johnk` at home). When that path is absent the script falls back to the shell's Desktop known folder and says so, because this host redirects `jkoll`'s Desktop into OneDrive (see below). |
| `-SkipDesktopShortcut` | off | Do not create or replace the desktop shortcut. |
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
| 8 | **Desktop shortcut** | Puts one product-named shortcut on the operator's desktop that opens the deployed control prompt (`mock-service-control.ps1`, which ships inside the publish output — see the project file). An existing shortcut of that name is replaced, and the saved `.lnk` is read back to prove it opens this install folder — see below. |
| 9 | **Summary** | A PASS/FAIL/WARN table. Exit `0` on success, `1` if any check failed, `2` refused (not the server host), `3` precondition failure. |

**If any check in steps 1–6 fails, the service is not started** — a half-deployed service that looks
healthy is worse than a failed deployment.

---

## The desktop shortcut it leaves behind

One shortcut, named after the product, opens a short control prompt:

| | |
|---|---|
| Where | `MTM mock cache service.lnk` on `jkoll`'s desktop — `C:\Users\jkoll\Desktop`, or the resolved Desktop known folder when that literal path is absent, which is the case on this host (see the redirect note below). The label is read from `Service_Shell.Title` in the app's `resw`, so it cannot drift from the window title. |
| Opens | `pwsh.exe` when the host has PowerShell 7 (falling back to `powershell.exe`) with `-NoProfile -ExecutionPolicy Bypass -File "C:\Services\MTM_Waitlist.Mock.Service\mock-service-control.ps1"`, with **no `-Action`**, so the prompt is shown. `ExecutionPolicy Bypass` is required: this machine reports `Undefined` at every scope, so the effective policy is the `Restricted` default. |
| Working folder | the install folder |
| Icon | the service executable, so it matches the tray icon |
| Elevation | none — it writes to the operator's own desktop |

An existing shortcut of that name is **replaced**, not left alone, so a redeploy after the install
folder moves cannot leave a shortcut pointing at nothing. The saved `.lnk` is read back and its target
and arguments are compared with what was intended: a shortcut that silently kept an old target would be
worse than no shortcut, because nothing would tell the operator why double-clicking it did nothing.

**On this host the Desktop is redirected.** `C:\Users\jkoll\Desktop` does not exist — `jkoll`'s Desktop
is `C:\Users\jkoll\OneDrive - Manitowoc Tool and Manufacturing\Desktop`. The script therefore falls back to
the shell's Desktop known folder (which is `jkoll`'s, because the installer runs as that account) and
records the substitution in the step's detail, rather than writing a shortcut into a path nobody can see.
If a future profile stores its Desktop somewhere the shell cannot resolve, pass `-DesktopPath` explicitly.

### What the prompt offers

| Choice | What it does |
|---|---|
| **1. Show the UI** | Opens the service window, where the cache service's state can be seen and its settings changed. Nothing is stopped or restarted. |
| **2. Restart the service** | Stops the service and starts it again. The window does not open by itself. |
| **3. Shut the service down** | Stops the service. The applications that read the cached data keep working, but nothing refreshes that data and no backups are taken until it is started again — and because auto-start defaults to on, it returns at the next sign-in unless that is turned off in Settings. |

It is deliberately one prompt rather than three shortcuts: each choice is described where it is offered,
so the operator can read what it does before choosing it. Pressing Enter with no choice, or typing
anything unrecognised, closes it **without changing anything**, so a stray double-click is harmless.

### Why the prompt, and not a shortcut straight to the app

The service is deliberately tray-only. A bare launch creates **no window** — the health check above
asserts exactly that — and because auto-start defaults to on the service is normally already running, so
a second launch redirects to the running instance and exits.

"Show the UI" therefore does not simply launch the executable. The launch carries `--open-status`, and
when the service is already running the second launch hands that activation to it
(`AppInstance.RedirectActivationToAsync` → `AppInstance.Activated`), so the **running** instance opens
its window. `--open-settings` does the same for the Settings surface. A bare launch still stays
tray-only, which is why the tray-only health check is unaffected by any of this.

The notification-area icon still works (left-click = **Status**, right-click = **Settings** /
**Back up now** / **Quit**); Windows frequently keeps that icon in the taskbar overflow.

### `mock-service-control.ps1`

| | |
|---|---|
| Default | shows the prompt |
| `-Action` | `ShowUi`, `Restart`, `ShutDown` — skips the prompt, for scripted use |
| Exit code | `0` on success or when nothing was chosen, `1` on any failed step |

Two details are load-bearing:

- **It re-reads the four User-scope secrets from `HKCU\Environment` into its own process before it
  starts the service.** A process launched from the desktop inherits the environment block captured when
  the shell started, so a service started after the secrets were installed or changed would otherwise
  come up with no credentials and fail every read with
  `Access denied for user ''@'localhost' (using password: NO)`. The installer sets the same variables on
  its own process for the same reason.
- **The stop is forced.** The service exits only on an explicit tray *Quit*, so there is no scriptable
  graceful stop — the same constraint step 2 documents.

It touches nothing else: no configuration, no credential and no backup is read or written, and a restart
reuses the stored state exactly as it is.

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
# stop it, drop the auto-start entry, remove the install folder and the desktop shortcut
Get-Process MTM_Waitlist.Mock.Service -ErrorAction SilentlyContinue | Stop-Process -Force
Remove-ItemProperty HKCU:\Software\Microsoft\Windows\CurrentVersion\Run -Name MTM_Waitlist.Mock.Service -ErrorAction SilentlyContinue
Remove-Item C:\Services\MTM_Waitlist.Mock.Service -Recurse -Force
Remove-Item "$env:USERPROFILE\Desktop\MTM mock cache service.lnk" -Force -ErrorAction SilentlyContinue
```

Add `Remove-Item "$env:LOCALAPPDATA\MTM_Waitlist.Mock.Service" -Recurse -Force` for a full reset — that
is what discards the DPAPI credential, the schedules and the backups. Deleting the install folder alone
**keeps** all of it (see `MTM_Waitlist.Mock.Service/README.md` §5).

---

## Verified

Deployed and validated on the host `V-MTMFG-5` (`172.16.1.104`) on 2026-09-11 — see the execution note in
`specs/001-module-mock-visual-fallback/tasks.md`.
