# MTM_Waitlist.Mock.Service — deploy and run

The on-host half of the Module_Mock cached fallback: a tray-only service that keeps the `mtm_mock` mirror
warm from Infor Visual, exposes an operator-gated API, and backs up the four MySQL stores.

It is **unpackaged** and **self-contained**. It is installed on the MySQL/Infor Visual host, not on the
workstations — the workstations run the client application, which reaches the mirror through its own
`mtm_mock` connection and asks this service for refreshes over the network.

The service's own contract lives in
`specs/001-module-mock-visual-fallback/contracts/mock-service-configuration.md` and
`…/contracts/mock-service-http-api.md`; the operational walkthrough is `…/quickstart.md` §2.

## 1. Publish

```powershell
dotnet publish MTM_Waitlist.Mock.Service/MTM_Waitlist.Mock.Service.csproj `
    -c Release -p:Platform=x64 -p:PublishProfile=win-x64-selfcontained
```

The profile is `Properties/PublishProfiles/win-x64-selfcontained.pubxml` and writes to
`MTM_Waitlist.Mock.Service/bin/publish/win-x64/`.

**Or let the deployment script do sections 1–4 for you.** `deploy/install-mock-service.ps1` publishes, redeploys,
installs and verifies the secrets, starts the service and health-checks it — and refuses to run unless the local
machine *is* the cache host. See `deploy/README.md`.

What the profile guarantees, and why each matters:

| Property | Value | Why |
|---|---|---|
| `RuntimeIdentifier` | `win-x64` | The host is 64-bit Windows; the project targets x64 only. |
| `SelfContained` | `true` | The host is not required to have a .NET runtime installed. |
| `WindowsAppSDKSelfContained` | `true` | The host is not required to have the Windows App SDK runtime installed. |
| `WindowsPackageType` | `None` | Unpackaged: no MSIX identity, no package signing, no Store dependency. |

## 2. Deploy

Copy the whole publish folder to the host (for example `C:\Services\MTM_Waitlist.Mock.Service\`). The
folder must be copied whole: the service reads its shape population scripts from
`Database/InforVisual/Queues/Module_Mock/Populations/` beside the executable, and the tray icon from
`Assets/`.

## 3. First run

1. Launch `MTM_Waitlist.Mock.Service.exe`. Expected: **no main window**; a tray icon appears and the
   background engines start.
2. Open **Settings** from the tray and configure the cache connection, the Infor Visual connection, the
   four-store backup settings, the refresh interval (default: **3 hours**, on the local-midnight grid
   `00:00/03:00/06:00/09:00/12:00/15:00/18:00/21:00`), and the API bind address/port (default `0.0.0.0:5760`).
3. **Operator access needs no provisioning.** The API admits a caller whose user name resolves to an approved
   application role in `mtm_waitlist` (`Admin`, `Developer`, `Plant Manager`, `Setup Lead`, `Production Lead` —
   `Api/ServiceOperatorRoles.cs`). There is no password, token or key to generate, record or install, and this
   replaced the old shared credential entirely (T147). The signed-in application user is the operator, so a
   workstation needs only `MTM_MOCK_SERVICE_ENDPOINT` (and `MTM_MOCK_SERVICE_USER` if the name it should present
   differs from the account it runs as).
4. Confirm auto-start. It is **on by default** (`ServiceConfiguration.AutoStartAtLogon = true`), so the first
   run has already registered the per-user `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` entry — and it
   records the path the executable was launched from, so **install the folder at its final location before the
   first run**. Turn the setting off if this host should not auto-start it; the service reconciles the entry.
5. Set the secrets the configuration deliberately does not store: the MySQL password as
   `MTM_MYSQL_PASSWORD` (or a complete `MTM_MOCK_DB_CONNECTION_STRING` / `MTM_WAITLIST_DB_CONNECTION_STRING`),
   and the Infor Visual password as `INFOR_VISUAL_SQL_PASSWORD`. Without the cache connection the service still
   starts and reports the condition; no refresh or backup can run. The API also needs the `mtm_waitlist`
   connection, because that is where a caller's role is resolved.

## 4. Verify

```powershell
# refused when no operator is named
curl.exe -i http://<host>:5760/api/status
# 401 {"error":"unauthorized"}

# refused when the named user's role is not approved — the refusal looks the same
curl.exe -i -H "X-MTM-Mock-User: shop.user" http://<host>:5760/api/status
# 401 {"error":"unauthorized"}

# status as an approved operator (an application user whose role is one of the approved roles)
curl.exe -H "X-MTM-Mock-User: <user name>" http://<host>:5760/api/status

# on-demand refresh
curl.exe -X POST -H "X-MTM-Mock-User: <user name>" -H "Content-Type: application/json" `
  -d '{\"shapeKeys\":null}' http://<host>:5760/api/refresh

# restore is NOT reachable over the network — it is a host-only, confirmation-gated UI action
curl.exe -i -H "X-MTM-Mock-User: <user name>" http://<host>:5760/api/restore
# 404
```

When something is wrong, the answer is in the service's own daily log file — the durable diagnostic (T148(c)):
`%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\Logs\service_daily_<yyyy_MM_dd>.jsonl`, one JSON object per line with
`TimestampUtc`, `Level`, `Area`, `Message` and `Exception`. Every container log message and every startup
decision lands there, including a refusal's reason and the reason a refresh cycle failed.

## 5. Uninstall

Quit from the tray menu (the process exits only on an explicit Quit), then remove the auto-start entry
from `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` and delete the install folder.

A **clean reinstall also needs the service's own app-data folder deleted** — the configuration, the run
records and the backup artifacts live *there*, not beside the executable:

| Item | Location |
|---|---|
| Configuration (`service-configuration.json`) | `%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\` |
| Daily log files (`service_daily_<yyyy_MM_dd>.jsonl`) | `%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\Logs\` |
| Refresh run records | `%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\` |
| Backup artifacts | `%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\backups\<database>\` |

(`ServiceHostBuilder.GetDefaultAppDataRoot()`, confirmed by running the published build on 2026-09-11.)
Deleting only the install folder therefore leaves the configuration, the schedules and the
backups behind on the host.
