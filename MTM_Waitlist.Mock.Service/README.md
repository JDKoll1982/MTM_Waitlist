# MTM_Waitlist.Mock.Service — deploy and run

The on-host half of the Module_Mock cached fallback: a tray-only service that keeps the `mtm_mock` mirror
warm from Infor Visual, exposes a credential-gated API, and backs up the four MySQL stores.

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
3. Generate the shared credential. It is written DPAPI-protected for the current user and is **never shown
   again**; record it out of band. This is the value the client application installs through
   `MTM_MOCK_SERVICE_TOKEN` (see `MTM_Waitlist.Mock/Models/MockServiceClientOptions.cs`).
4. Enable auto-start at logon and confirm the per-user `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
   entry exists.

## 4. Verify

```powershell
# refused without the credential
curl.exe -i http://<host>:5760/api/status
# 401 {"error":"unauthorized"}

# status with the credential — the payload never contains the credential itself
curl.exe -H "X-MTM-Mock-Token: <credential>" http://<host>:5760/api/status

# on-demand refresh
curl.exe -X POST -H "X-MTM-Mock-Token: <credential>" -H "Content-Type: application/json" `
  -d '{\"shapeKeys\":null}' http://<host>:5760/api/refresh

# restore is NOT reachable over the network — it is a host-only, confirmation-gated UI action
curl.exe -i -H "X-MTM-Mock-Token: <credential>" http://<host>:5760/api/restore
# 404
```

## 5. Uninstall

Quit from the tray menu (the process exits only on an explicit Quit), then remove the auto-start entry
from `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` and delete the install folder. The service-local
configuration and run records live beside the executable; delete them too if a clean reinstall is wanted.
