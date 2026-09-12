# Server validation prompt — T147 (role-based API authorization) and T148(c) (durable service log)

Paste everything below the line into a Copilot chat **running in VS Code on the cache host**
(`V-MTMFG-5` / `172.16.1.104`, install folder `C:\Services\MTM_Waitlist.Mock.Service\`).

Two changes were implemented off-server on 2026-09-12 and **cannot be verified anywhere but the host**: the service
API no longer uses a shared credential (it authorizes the caller's application role against `mtm_waitlist`), and the
service now writes a durable daily log file. Both need a live `mtm_waitlist` connection and a running service.

---

## Prompt

You are validating two committed changes to `MTM_Waitlist` on the machine that hosts the Module_Mock cache service.
Work through the steps in order, run every command yourself, and report a PASS/FAIL table at the end. Do not skip a
step because an earlier one looked fine. If a step fails, capture the exact output, diagnose it, and stop rather than
reporting a partial pass.

### Context: what changed

1. **T147 — the shared API credential was retired.** The service used to generate a DPAPI-protected credential it
   never displayed, so no operator and no client could ever authenticate. That whole mechanism is deleted:
   `SharedTokenAuthenticationHandler`, `SharedCredential`, `ServiceConfigurationStore.HasCredential` /
   `CredentialMatches` / `GenerateCredentialAsync`, `ApiSettings.Credential`, the settings "rotate credential" card,
   and the `credentialConfigured` status field.
   - Callers now send `X-MTM-Mock-User: <application user name>`.
   - `MTM_Waitlist.Mock.Service/Services/ServiceOperatorRoleResolver.cs` resolves that name with
     `sp_auth_user_row_get` against **`mtm_waitlist`** and requires the role to be one of
     `ServiceOperatorRoles.Approved` — `Admin`, `Developer`, `Plant Manager`, `Setup Lead`, `Production Lead`
     (`MTM_Waitlist.Mock.Service/Api/ServiceOperatorRoles.cs`), compared case-insensitively.
   - Unknown user, inactive user, no role assignment, and unapproved role **all** answer
     `401 {"error":"unauthorized"}` and are deliberately indistinguishable.
   - An unreachable store **fails closed**. The listener now refuses to start when no `mtm_waitlist` connection is
     configured, in place of the old "no credential generated yet" gate.
   - `GET /api/status` reports `operatorRoles` instead of `credentialConfigured`.
   - The app-side client (`MTM_Waitlist.Mock`) presents the user name from `MTM_MOCK_SERVICE_USER` (falling back to
     `MockServiceClient:UserName`), replacing `MTM_MOCK_SERVICE_TOKEN`. `appsettings.json` `MockServiceClient` now
     has `UserName`, not `Token`.
   - **Accepted limitation, do not treat as a defect:** the caller asserts its user name over plain HTTP. The
     service verifies the *role* of the asserted user, but this is authorization, not cryptographic authentication.
2. **T148(c) — the service keeps a durable log file.** Every service diagnostic previously went through
   `StartupDebugLog`, whose methods are `[Conditional("DEBUG")]` and were therefore compiled **out of a Release
   publish**; in Debug they reached only `Debug.WriteLine`. Now:
   - `Services/ServiceLog.cs` appends JSON Lines to
     `%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\Logs\service_daily_<yyyy_MM_dd>.jsonl`
     (fields: `TimestampUtc`, `Level`, `Area`, `Message`, `Exception`; 30-day retention by file name).
   - `Services/ServiceFileLoggerProvider.cs` is registered in `ServiceHostBuilder.Build`, so every container log
     message — including the refresh engine's cycle failures — lands in that file.
3. **T148(a) is not a code change to verify**; it already shipped in commit `c2d5e04` (a second launch parses
   `--open-status` / `--open-settings`, signals the running instance over `ServiceLocal\MTM_Waitlist.Mock.Service.Show*`
   events, and `deploy/mock-service-control.ps1` — opened by the desktop shortcut the installer places — is the
   operator's entry point). Step 8 below is a smoke check of it, not a new feature.

### Step 1 — confirm you are on the host

```powershell
hostname
(Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -notlike '127.*' }).IPAddress
```

**Expect** `V-MTMFG-5` and `172.16.1.104`. If not, **stop** — do not run the deploy script anywhere else; its own
guard refuses, and the guard must not be bypassed with `-AllowNonServerHost`.

### Step 2 — get the changes and build

```powershell
cd C:\Users\job\source\repos\MTM_Waitlist     # adjust to this machine's checkout
git fetch --all
git log --oneline -3                          # confirm the T147/T148(c) commit is present
git status --short
dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false
```

**Expect** `Build succeeded. 0 Warning(s) 0 Error(s)`.

> If you see `PRI175` / `PRI224`, stop any running `MTM_Waitlist.exe`, delete stale `*.pri` under `obj/`, and rebuild.
> `WMC9999: Could not find any resources appropriate for the specified culture … ErrorMessages.resources` is a
> **masked XAML error**, not an environment problem — surface the real error before reporting.

### Step 3 — run the tests

```powershell
dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64 /m:1
```

**Expect** `Failed: 0`. The off-server run was `Passed: 720, Skipped: 19, Total: 739`; the 19 skips are the
live-database integration suites. On the host, re-run with the live connection so the skips execute:

```powershell
$env:MTM_WAITLIST_TEST_DB_CONNECTION_STRING = $env:MTM_WAITLIST_DB_CONNECTION_STRING
dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64 /m:1
```

**Expect** `Failed: 0, Skipped: 0` (the previously skipped live suites now run). Report the two totals separately.

The new/updated tests that specifically cover the changes:

| Test | What it proves |
|---|---|
| `Module_Mock_Service/ServiceApiTests.cs` | no operator name → `401`; a real user with a non-approved role → `401`; an unknown user name → `401`; an approved operator gets `200` and the payload carries `operatorRoles` and no connection secret; `/api/restore` is `404` |
| `Module_Mock_Service/ServiceConfigurationStoreTests.cs` | the configuration file persists no `credential`/`password` field and `ApiSettings` has no `Credential` property |
| `Module_Mock_Service/ServiceLogTests.cs` | the daily file is created and appended to, the line carries level/area/message/timestamp, an error keeps the exception text, an unusable directory does not throw, and the `ILoggerProvider` routes container messages into it |
| `Module_Mock/Module_Mock/MockServiceRefreshClientTests.cs` | the client posts `X-MTM-Mock-User`, and an unconfigured endpoint/user name degrades to a reported outcome without calling the service |

### Step 4 — confirm the role lookup against the real store

The whole change rests on this read. Run it directly and record the output:

```powershell
mysql -h 172.16.1.104 -P 3306 -u root -proot mtm_waitlist -e "CALL sp_auth_user_row_get('<a real operator user name>');"
```

**Expect** exactly one row with `role_name` equal to one of the five approved roles. Then confirm the negative case
with a real shop-floor account (expect a role such as `Setup` or `Production`, i.e. **not** approved). If
`sp_auth_user_row_get` is missing from the deployed schema, that is a **blocking finding** — say so explicitly.

### Step 5 — deploy

```powershell
cd MTM_Waitlist.Mock.Service\deploy
.\install-mock-service.ps1 -Publish
```

**Expect** `DEPLOYMENT OK — 22 checks, 0 warning(s)` and exit code `0`. Do **not** pass `-AllowNonServerHost`, and do
not hand-roll the steps. The script publishes, stops the running instance, deletes the old install folder, redeploys,
sets and proves the four secrets, starts the service, and health-checks it. One of its checks is now labelled
**`API refuses an unnamed caller`** — it must report `PASS` (`GET /api/status -> 401`).

### Step 6 — verify the API's authorization behaviour (the core of T147)

```powershell
# 1. no operator named -> 401
curl.exe -i http://localhost:5760/api/status

# 2. a real user whose role is NOT approved -> 401, and the body is identical to case 1
curl.exe -i -H "X-MTM-Mock-User: <shop-floor user name from step 4>" http://localhost:5760/api/status

# 3. an invented user name -> 401, identical again
curl.exe -i -H "X-MTM-Mock-User: nobody.at.all" http://localhost:5760/api/status

# 4. an approved operator -> 200, and the payload reports operatorRoles
curl.exe -s -H "X-MTM-Mock-User: <approved operator user name from step 4>" http://localhost:5760/api/status
```

| Check | Expected |
|---|---|
| Cases 1–3 | all `401` with body `{"error":"unauthorized"}` — **byte-identical**, so the surface cannot be used to enumerate users or roles |
| Case 4 | `200`, JSON contains `"operatorRoles":"Admin, Developer, Plant Manager, Setup Lead, Production Lead"` and contains **no** `credential`-shaped field and no connection password |
| Case 4, from another workstation (not the host) | also `200` — remote clients must keep working. Run the same `curl` from a plant PC against `http://172.16.1.104:5760/...` |

Also confirm the old scheme is genuinely gone:

```powershell
curl.exe -i -H "X-MTM-Mock-Token: anything" http://localhost:5760/api/status   # expect 401
```

### Step 7 — verify the durable log (the core of T148(c))

```powershell
$log = Get-ChildItem "$env:LOCALAPPDATA\MTM_Waitlist.Mock.Service\Logs\service_daily_*.jsonl" |
       Sort-Object LastWriteTime -Descending | Select-Object -First 1
$log.FullName
Get-Content $log.FullName -Tail 40
```

| Check | Expected |
|---|---|
| The folder and today's file exist | `%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\Logs\service_daily_<today>.jsonl` is present and non-empty |
| Startup is recorded | a line with `"Area":"ServiceApp"` and a message starting `Start requested`, naming the log directory |
| Refusals are recorded | a line with `"Level":"WARN"` whose message names the refusal reason for each of the `401` attempts in step 6 — this is what makes a misconfigured client diagnosable |
| A refresh cycle is recorded | after one interval (or after `POST /api/refresh` with an approved operator), lines from the refresh engine reporting each shape's outcome; if a cycle fails, the **reason** is here |
| No secret in the file | the file contains no password, connection string, or token value |

Then confirm the file is written by a **Release** publish, which is the whole point:

```powershell
Get-Process MTM_Waitlist.Mock.Service | Select-Object Id, Path, StartTime
```

**Expect** the process path to be `C:\Services\MTM_Waitlist.Mock.Service\MTM_Waitlist.Mock.Service.exe`, and the log
file to keep growing while it runs. Before this change a Release build wrote nothing at all.

### Step 8 — smoke-check the operator's entry point (T148(a), already shipped)

```powershell
# The desktop shortcut the installer places opens this control prompt.
Get-ChildItem "$([Environment]::GetFolderPath('Desktop'))" -Filter '*.lnk' |
    Select-Object Name, FullName
```

**Expect** one shortcut named after the product. Open it (or run the script it points at) and choose **Show UI**;
the service window must appear even with the tray icon hidden in the `∧` overflow. Record whether the tray icon is
itself promoted (`HKCU\Control Panel\NotifyIconSettings` → `IsPromoted`) — a hidden tray icon is expected on
Windows 11 and is no longer a blocker now that the control prompt exists.

### Step 9 — one real refresh cycle (§2 step 7, was the T147 blocker)

```powershell
curl.exe -X POST -H "X-MTM-Mock-User: <approved operator>" -H "Content-Type: application/json" -d "{\"shapeKeys\":null}" http://localhost:5760/api/refresh
Start-Sleep -Seconds 30
curl.exe -s -H "X-MTM-Mock-User: <approved operator>" http://localhost:5760/api/status
```

**Expect** per-shape outcomes and, in the status payload, each shape's `lastOutcome` = `succeeded` with a fresh
`refreshedUtc`. If a shape does not succeed, use step 7's log file to name the cause — that recovery path is the point
of T148(c).

### Step 10 — restore the host to a clean state and report

- If you started the service only to test, leave it running if it is the production deployment; otherwise stop it and
  remove any auto-start entry you created.
- Report a PASS/FAIL table covering steps 1–9, with the exact commands and the observed output for anything that
  failed.
- If everything passed, update `specs/001-module-mock-visual-fallback/tasks.md` and `OPEN-TASKS.md` to record the
  host verification (T147/T148 are currently ticked on the strength of code + off-server tests only) and note that
  T106 §2 step 7 is now satisfied.

### Explicitly out of scope for this run

- **T144** (the work-order input rule cannot express 42,143 of 42,852 open orders) is a product decision, not a bug
  to fix here.
- **T106's SC-007/SC-008** 30-day observation windows: start them if you can, but do not attempt to complete them.
- Do **not** reintroduce a shared credential, a token header, or a mock/demo toggle. `RetiredSymbolAuditTests` and the
  inline-SQL audit must both still pass.
