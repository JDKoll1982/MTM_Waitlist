# Deploying `MTM_Waitlist.Mock.Service`

Executable deployment for the on-host half of the Module_Mock cached fallback:
`install-mock-service.ps1`.

`MTM_Waitlist.Mock.Service/README.md` §2–§5 remains the narrative authority for *what* a deployment is
(publish, deploy, first run, verify, uninstall). This folder is the **executable form** of it: one
idempotent script that does all of it and refuses to lie about the result.

Run the script with **no argument** and it asks what you are trying to do before it changes anything — see
[Usage](#usage). Passing any argument skips the prompts, so the hand-run and the automated path are the same
code with the same exit codes.

---

## ✅ Any host — the service installs anywhere, and does what that machine can reach

**There is no server-only guard any more.** The service installs on any machine. What it *does* there is
decided by reachability, not by the installation:

| Work | Needs | On a machine without it |
|---|---|---|
| Refresh the `mtm_mock` mirror | Infor Visual | **Refresh is disabled** — the scheduled loop skips its cycles, `POST /api/refresh` is refused with the reason, and the status surface says so. |
| Back up or restore a store | That store's MySQL database | **Backup and restore are disabled for that store only** — its scheduled slot is skipped with no run record, `POST /api/backup` is refused with the reason, and the settings surface refuses the action. The other stores are unaffected. |
| Serve the mirror over the API and the tray | A reachable `mtm_mock` | Always attempted — this is what a workstation is for. |

The service probes at startup and every minute, so a machine that **gains** access starts the work again
without a restart. The authoritative per-store answer is the service's own: it opens a connection to each
database, because a reachable server with a missing database is not a store that can be backed up.

The **secret proofs follow the same rule**, so a mirror-only host can still complete a deployment: the MySQL
credentials are proved against the configured host when it answers, otherwise against this machine's own MySQL
(which is what the service falls back to), and the Visual login is a **`WARN`** rather than a failure when the
server cannot be reached from here. Only a proof that actually ran can fail — a reachable server that refused
the credentials is still a hard `FAIL`, because no fallback can hide a wrong password.

The script **reports rather than refuses**. It never fails a deployment because the machine is not the cache
host, and it prints what it can see so the operator knows what to expect:

```text
[PASS] cache host       this machine is the cache host (172.16.1.104)
[WARN] infor visual     VISUAL did not answer on port 1433; the service will disable refresh on this machine
[PASS] mysql host       172.16.1.104 answered on port 3306; the service probes each store and disables only the ones it cannot reach
```

| | |
|---|---|
| Verified cache host (2026-09-11) | `V-MTMFG-5.mantoolmfg.com` = `172.16.1.104` |
| `-ServerHost` | Only what the *cache host* comparison names; a mismatch is a `WARN`, never a refusal |
| `-VisualPort` | The port the Infor Visual report probes (default `1433`) |

**The workstation install is a supported configuration**, not a workaround: `HKCU\...\Run` and the tray are
per-user, the install folder is local, and the only consequence of installing off the cache host is that the
work that machine cannot do is disabled and reported.

---

## Prerequisites

| Requirement | Why |
|---|---|
| An install folder local to this machine | A first run registers auto-start with the path it was launched from. |
| `appsettings.json` present in the repo | The script derives the expected server address, the MySQL connection string and the Infor Visual login/password from it, so it is **not a second source of truth** and holds **no secrets of its own**. |
| The solution built at least once | `bin\x64\Debug\...\win-x64\{MySqlConnector,Microsoft.Data.SqlClient}.dll` are loaded to *prove* the secrets by connecting, not merely by reading them back. |
| `dotnet` SDK | Only when `-Publish` is used. |

Secrets are the owner-approved exception recorded in
`.github/memories/repo/workstation-secrets.md`: the script sets the four User-scope variables that file
describes, and nothing else.

---

## Usage

**Run it with no argument and it asks what you are trying to do.** That is the intended way to run it by hand:

```powershell
# guided: deploy, publish-then-deploy, install elsewhere, or a clean sweep
.\install-mock-service.ps1
```

The guided flow asks the menu below, then the questions that are independent of it, then shows a summary with
**the equivalent command line** and waits for a yes/no before it touches anything:

| Menu choice | What it means |
|---|---|
| **Deploy** | Deploy the publish output already on disk; the service's stored state is kept. |
| **Publish and deploy** | Build a fresh Release publish output first, then deploy it. |
| **Install elsewhere** | Deploy to a different install folder, which it asks you for. |
| **Clean sweep** | Deploy and also delete ALL service state — configuration, backups, run records. |
| **Quit** | Change nothing. |

Then, each defaulting to the safe answer: build a fresh publish output? set and verify the environment secrets?
start the service and health-check it? put the shortcut on this machine's desktops? and — off by default except
after **Clean sweep** — also delete the service's stored state. In guided mode the script waits for Enter after
the summary, so a window that closes on exit still lets the result be read.

**Passing ANY argument skips the prompts entirely**, so automation and the hand-run path are the same script with
the same exit codes:

```powershell
# normal deployment: publish fresh, deploy, install + verify secrets, start and health-check
pwsh -NoProfile -ExecutionPolicy Bypass -File ./deploy/install-mock-service.ps1 -Publish

# redeploy an existing publish output (no rebuild); -NonInteractive says "no prompts, defaults"
pwsh -NoProfile -ExecutionPolicy Bypass -File ./deploy/install-mock-service.ps1 -NonInteractive

# deploy to a different folder
pwsh -NoProfile -ExecutionPolicy Bypass -File ./deploy/install-mock-service.ps1 -TargetPath D:\Services\MTM_Waitlist.Mock.Service

# a clean sweep: also delete the stored configuration and the backups
pwsh -NoProfile -ExecutionPolicy Bypass -File ./deploy/install-mock-service.ps1 -PurgeState
```

### Where it installs

There is no hard-coded install folder. With no `-TargetPath` the script picks one, in this order:

| # | Folder | When |
|---|---|---|
| 1 | `C:\Services\MTM_Waitlist.Mock.Service` | A deployment is **already there**. An existing install is never moved — auto-start, the shortcut and the control script all point at it. |
| 2 | the same folder | This account can **actually create** it. This is the machine-level location, which is right for the cache host. |
| 3 | `%LOCALAPPDATA%\Programs\MTM_Waitlist.Mock.Service` | Neither of the above — the per-user folder, which needs no elevation. |

"Can it be used" is tested by **creating the folder**, not inferred from elevation, and the banner prints which one
was chosen and why:

```text
  target  : C:\Services\MTM_Waitlist.Mock.Service
            (default - the machine-level location)
```

A `-TargetPath` the account cannot write fails immediately, naming both candidates, instead of failing partway
through a deployment. The guided workflow shows the resolved folder as a default you accept with Enter.

### The publish output can be stale

Deploying what is already on disk is the fast path and the right one when the output is current. The moment a source
file or an asset changes it is the wrong one — the copy succeeds and the missing piece surfaces later as a validation
failure or a service that cannot find its icon. So the script checks **before** the copy: reusing an output that is
missing required content or older than the project reports a `WARN`, the step-5 failure names the cause, and the
guided workflow pre-selects a fresh publish output when that is the case.

| Parameter | Default | Effect |
|---|---|---|
| `-TargetPath` | resolved, see below | Install folder. **Must be final before the first run** — a first run registers auto-start with the path it was launched from. |
| `-SourcePath` | `MTM_Waitlist.Mock.Service/bin/publish/win-x64` | Publish output to deploy from. || `-ServerHost` | MySQL `Server=` from `appsettings.json` | Names the cache host for the disposition report. A mismatch is a `WARN`, never a refusal. |
| `-VisualPort` | `1433` | Port the Infor Visual reachability report probes. |
| `-Publish` | off | Run `dotnet publish -c Release -p:PublishProfile=win-x64-selfcontained` first. Without it the output
on disk is deployed as-is, and the script **warns** when that output is missing required content or older than the
project, because that is a source-only change waiting to look like a deployment failure. |
| `-SkipSecrets` | off | Do not set or verify the environment secrets. |
| `-SkipServiceStart` | off | Do not start the service or run the health checks. |
| `-PurgeState` | off | Also delete `%LOCALAPPDATA%\MTM_Waitlist.Mock.Service` (configuration, run records, log files, backups). |
| `-DesktopPath` | `C:\Users\jkoll\Desktop` | Desktop that receives the restart shortcut. **Hardcoded to the operator account by owner decision** — pass it explicitly if the profile ever changes (the agent's own notes record `jkoll` at work, `johnk` at home). When that path is absent the script falls back to the shell's Desktop known folder and says so, because this host redirects `jkoll`'s Desktop into OneDrive (see below). |
| `-AdditionalDesktopPaths` | `@('C:\Users\johnk\Desktop')` | Extra desktops that **also** receive the shortcut, so one installer works on both operator machines without arguments. Best effort by owner request: a profile that is not on this machine is **skipped**, an unwritable folder is a **warning**, and neither fails the deployment. A redirected desktop is found by looking beside the literal path (another profile's OneDrive folder), since `GetFolderPath` only answers for the account running the script. |
| `-SkipDesktopShortcut` | off | Do not create or replace the desktop shortcut. |
| `-NonInteractive` | off | Skip the guided prompts. **Passing any argument also skips them**; this exists to say "no prompts, use the defaults" out loud. |
| `-HealthCheckTimeoutSeconds` | 60 | How long to wait for the API port. |

---

## What it does, in order

| # | Step | Notes |
|---|---|---|
| 1 | **Host disposition** | Reports whether this machine is the cache host, whether Infor Visual answers, and whether the MySQL host answers. **Never fatal** — the service installs anywhere, and the work this machine cannot do is disabled by the service and reported. |
| 2 | **Stop the running service** | Force-stops every `MTM_Waitlist.Mock.Service` process, waits up to 30 s, then confirms the API port is free. The service exits only on an explicit tray *Quit*, so a forced stop is the only scriptable option. |
| 3 | **Delete the previous deployment** | Removes the install folder outright, then redeploys — no stale files can survive a redeploy. State under `%LOCALAPPDATA%` is **kept** unless `-PurgeState`. |
| 4 | **Publish and copy** | Optional `dotnet publish`, then a full copy. A `NETSDK1198` "publish profile not found" warning is treated as a **failure**, because it means the build silently became framework-dependent. |
| 5 | **Verify the deployment** | File count matches source; every required path present (five population reads, tray icon, source queries, restore artifacts, WinAppSDK runtime); self-contained markers `System.Private.CoreLib.dll`/`hostfxr.dll`/`coreclr.dll` present **and** `runtimeconfig.json` using `includedFrameworks`; the corrected work-order addressing guard (T144) present in the deployed population reads. |
| 6 | **Install and verify secrets** | Sets the four User-scope variables from `appsettings.json`, reads them back from `HKCU\Environment`, applies them to the script's **own** process so the service it launches inherits them, and then **proves** them: connects to `mtm_mock`, `mtm_waitlist`, `mtm_wip_application_winforms` and `mtm_receiving_application`, and authenticates to Infor Visual. |
| 7 | **Start and health-check** | Starts the service, waits for port 5760, then asserts: process responding, **no top-level window** (tray-only), `GET /api/status` returns **401** when it names no operator (T147), a **second launch redirects and exits** (single instance), and the `HKCU` auto-start entry points at *this* install folder. |
| 8 | **Desktop shortcut** | Puts one product-named shortcut on the operator's desktop that opens the deployed control prompt (`mock-service-control.ps1`, which ships inside the publish output — see the project file). An existing shortcut of that name is replaced, and the saved `.lnk` is read back to prove it opens this install folder — see below. It then **also attempts** the same shortcut on every `-AdditionalDesktopPaths` entry (best effort: skip or warn, never fail). |
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

The same shortcut is then **also attempted** on every `-AdditionalDesktopPaths` entry — by default
`C:\Users\johnk\Desktop` — so one installer serves both operator machines. That half is deliberately
best effort and cannot fail a deployment: a profile that is not present is reported as a skip, and a
folder the account cannot write to is reported as a warning with the reason. Each extra shortcut is built
the same way and points at the same deployed control prompt, so there is still exactly one entry per
desktop.

**A redirected Desktop wins over the literal path.** `C:\Users\<profile>\Desktop` is often left behind as an
empty husk when the profile's Desktop is redirected into OneDrive, so "the path exists" is not the answer — a
shortcut written there is one nobody can see. Each additional path therefore resolves to the profile's
`OneDrive*\Desktop` when that exists, and is skipped when it resolves to the desktop the primary shortcut
already covers.

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
| `3` | Precondition failure (no publish output). |

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
is what discards the schedules, the run records, the log files and the backups. Deleting the install folder alone
**keeps** all of it (see `MTM_Waitlist.Mock.Service/README.md` §5).

---

## Verified

Deployed and validated on the host `V-MTMFG-5` (`172.16.1.104`) on 2026-09-11 — see the execution note in
`specs/001-module-mock-visual-fallback/tasks.md`.
