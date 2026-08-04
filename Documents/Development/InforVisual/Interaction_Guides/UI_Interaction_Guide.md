# Visual Discovery Report

**Date:** 2026-03-08  
**Machine:** JKOLL-PC (`jkoll`)  
**Conducted by:** GitHub Copilot agent — automated discovery session

---

## Section 1 — Visual Executable and Process

### Running Process (at time of enumeration — Window was initially closed)

Visual was **not running** when the session started. It was launched manually and the
Receiver Entry screen was opened before Section 2 was executed.

When running, Visual appears as:

| Property | Value |
|---|---|
| Process name | `VMRCVENT` (Receiver Entry specific window) |
| Main menu process | `VM` |
| Window title — Receiver Entry | `Purchase Receipt Entry - Infor VISUAL - MTMFG` |
| Window title — Main menu | `Infor VISUAL - MTMFG/JKOLL` |
| Window class | `Gupta:AccFrame` |
| PID (session-specific) | 3228 (Receiver Entry), 8164 (main menu) |

> **`AppActivate` string to use:** `Purchase Receipt Entry - Infor VISUAL - MTMFG`

### Installation Location

Visual runs entirely from a network share — **no local installation**.

| Property | Value |
|---|---|
| Network share | `\\visual\visual908$\VMFG` |
| Receiver Entry executable | `\\visual\visual908$\VMFG\VMRCVENT.EXE` |
| Main menu executable | `\\visual\visual908$\VMFG\VM.EXE` |
| Import/Export utility | `\\visual\visual908$\VMFG\VFIMPEXP.EXE` |
| Batch receiver import | `\\visual\visual908$\VMFG\VMBTSRCV.EXE` |
| Receiver insert utility | `\\visual\visual908$\VMFG\VMRCVINS.EXE` |

### Version Information

From `VMRCVENT.EXE`:

| Property | Value |
|---|---|
| File Description | VISUAL Manufacturing Purchase Receipt Entry |
| Product Name | VISUAL Enterprise |
| Company | Infor Global Solutions |
| File Version | **9.0.8.031** |
| Product Version | **9.0.8** |
| Copyright year | 2021 |

### Registry

No local Infor/MAPICS registry keys found on this workstation. Visual is a network-hosted
application — all configuration lives on the `\\visual` server or in per-user profile files.

```
HKLM:\SOFTWARE\Infor          → Not found
HKLM:\SOFTWARE\WOW6432Node\Infor → Not found
HKLM:\SOFTWARE\MAPICS          → Not found
HKCU:\SOFTWARE\Infor           → Not found
```

---

## Section 2 — Window Title and UI Automation Identifiers

### All Top-Level Windows (at time of Section 2 scan)

| PID | ClassName | Window Title |
|---|---|---|
| 3228 | `Gupta:AccFrame` | `Purchase Receipt Entry - Infor VISUAL - MTMFG` |
| 8164 | `Gupta:AccFrame` | `Infor VISUAL - MTMFG/JKOLL` |
| 2428 | `TV_ControlWinMinimized` | TeamViewer Panel (minimized) |
| 13176 | `MetroWindow` | MAMP |
| 12468 | `Chrome_WidgetWin_1` | MTM_Receiving_Application - Visual Studio Code |
| 8516 | `Chrome_WidgetWin_1` | Serena Dashboard - Google Chrome |

### Receiver Entry UIA Control Dump (80 controls)

Full dump saved to: `%TEMP%\visual_uia_dump.csv`  
Also copied to: `docs/InforVisual/Integration/visual_uia_dump.csv` (if attached)

#### Header Fields — Edit / Input Controls

Populated values are from the live PO `PO-061514` (Skana Aluminum Company) captured 2026-03-08.
Y-coordinate from the UIA bounding rect is used to infer approximate tab order (smaller Y = higher on screen).

| Y-pos | Label | AutomationId | ClassName | Populated Value | Notes |
|---|---|---|---|---|---|
| 181 | Site ID (combo) | 4097 | `ComboBox` | `MTM2` | Pre-populated site |
| 204 | Order ID (PO#) | 4103 | `Edit` | **`PO-061514`** | Main PO number input |
| 226 | Receiver ID | 4109 | `Edit` | *(blank — not yet posted)* | Auto-assigned or manual |
| 248 | Service Dispatch ID | 4114 | `Edit` | *(blank)* | |
| 270 | Act Recv Date | 4117 | `Edit` | **`3/8/2026`** | Actual receive date |
| 292 | Receive on | 4120 | `Edit` | `3/8/2025` | Desired receive date |
| 314 | Promise Delivery Date | 4122 | `Edit` | `3/8/2025` | |
| 336 | Promise Ship Date | 4124 | `Edit` | *(blank)* | |
| 357 | Bill of Lading ID | 4126 | `Edit` | *(blank)* | |
| 380 | Vendor Packlist ID | 4128 | `Edit` | *(blank)* | |
| 402 | Vendor Packlist Date | 4130 | `Edit` | *(blank)* | |
| 424 | Vendor Freight Bill ID | 4133 | `Edit` | *(blank)* | |
| — | *(Vendor info list)* | 4134 | `ListBox` | Skana Aluminum Company… | Scrollable vendor address list |
| 211 | Buyer | 4136 | `Edit` | **`SWITT`** | Appears in right panel |
| 233 | FOB | 4138 | `ComboBox` | **`SC`** | Dropdown |
| 255 | Ship via | 4140 | `ComboBox` | *(blank)* | Dropdown |
| 277 | Carrier ID | 4142 | `ComboBox` | *(blank)* | Dropdown |
| 324 | *(Currency)* | 4161 | `Edit` | **`US Dollar`** | Right panel, read-only |
| 346 | *(Close Short flag)* | 4149 | `Edit` | **`Closed`** | Right panel |

#### Grid Area Controls

| AutomationId | ClassName | ControlType | Notes |
|---|---|---|---|
| 4146 | `Gupta:ChildTable` | `ControlType.Pane` | **Line item grid** — bounding rect `96,451,977,239` |
| 1024 | `Gupta:ChildTable:ListClip` | `ControlType.Pane` | Only visible area inside grid; single child |
| 32791 | `Edit` | `ControlType.Pane` | **Active cell** — the only UIA-accessible edit inside the grid |
| 4149 | `Edit` | `ControlType.Pane` | `Closed` (Close Short flag) — outside the grid, in right panel |
| 4161 | `Edit` | `ControlType.Pane` | `US Dollar` — currency field, right panel |

> ⚠️ **Critical finding:** `Gupta:ChildTable` exposes **only 2 UIA descendants** — the ListClip clip region and the active edit cell. Column headers are GDI-painted and **not accessible via UIA at all**. Grid navigation must be done by Tab/Enter keystrokes, tracking column position by count. Column order is authoritative from `VMRCVENT.INI`.

#### Status Bar / Other

| Name | AutomationId | Notes |
|---|---|---|
| StatusBar | `StatusBar` | Gupta:StatusBar |
| NUM | `StatusBar.Pane2` | Num Lock indicator |

### VMRCVENT.INI — Grid Column Definitions

The INI file at `\\visual\visual908$\VMFG\VMRCVENT.INI` reveals the receiver line grid
column names (format: `colNAME=Visible/Hidden~TabOrder~Width~DisplayLabel`):

| Column | Visibility | Display Label | Tab# |
|---|---|---|---|
| `LINE_NO` | Visible | Ln# | 2 |
| `PART_ID` | Visible | Part ID | 14 |
| `VENDOR_PART_ID` | Visible | Vendor Part ID | 16 |
| `USER_ORDER_QTY` | Visible | Qty Ordered | 18 |
| `PURCHASE_UM` | Visible | Unit of Measure | 19 |
| `USER_RECEIVED_QTY` | Visible | Qty Received | 9 |
| `RECEIVED_QTY` | Visible | Stock Qty Received | 51 |
| `ORDER_QTY` | Visible | Stock Order Qty | 50 |
| `DESIRED_RECV_DATE` | Visible | Receive On | 21 |
| `PROMISE_DATE` | Visible | Promise Del Date | 22 |
| `PROMISE_SHIP_DATE` | Visible | Promise Ship Date | 23 |
| `LOCATION_ID` | Visible | Location ID | 27 |
| `WAREHOUSE_ID` | Visible | Warehouse ID | 28 |
| `DESCRIPTION` | Visible | Description | 29 |
| `LOCATION_QTY` | Visible | Location Qty | 32 |
| `EST_FREIGHT` | Visible | Est Freight | 20 |
| `LINE_STATUS` | Visible | Close Short | 24 |
| `RECEIVED_DATE` | Visible | Last Recv Date | 25 |
| `ALLOCATED_QTY` | Visible | Qty Allocated | 53 |
| `FULFILLED_QTY` | Visible | Qty Fulfilled | 54 |
| `IS_TRACED` | Hidden | Trc | 17 |
| `SERVICE_ID` | Visible | Service ID | 15 |
| `PIECES` | Visible | Pieces Returned | 3 |
| `RETURNED_QTY` | Visible | Stock Returned Qty | 52 |

---

## Section 3 — VBScript / Macro Infrastructure

### Script Hosts Available

| Executable | Path | Version |
|---|---|---|
| `wscript.exe` | `C:\WINDOWS\system32\wscript.exe` | 10.0.22621.5983 |
| `cscript.exe` | `C:\WINDOWS\system32\cscript.exe` | 10.0.22621.5983 |

**Both are present and functional.** VBScript can be invoked on this workstation.

### Macro Files on VMFG Share

No `.vbs`, `.vba`, `.bas`, `.hta`, or `.mac` files were found in `\\visual\visual908$\VMFG`.
Visual does not ship macro files — any automation must be written externally.

### INI Configuration Files Found on VMFG Share

Key INI files that store per-module preferences (including `VMRCVENT.INI`):

- `VMRCVENT.INI` — Receiver Entry column layout (see Section 2 for contents)
- `VMPURENT.INI` — Purchase Order Entry
- `VMSHPENT.INI` — Shipment Entry
- `VMBCLABR.INI` — Bar Code Label
- `VMLABENT.INI` — Labor Entry

The XML config files found:
- `VISUALCONFIG.XML` — Core Visual configuration
- `INFORVISUALENTERPRISEVIEWS.XML` — Enterprise view definitions
- `CLOSED.xml` — Closed period marker (last modified 2025-05-05)

### Execution Policy

| Scope | Policy |
|---|---|
| MachinePolicy | Undefined |
| UserPolicy | Undefined |
| Process | Undefined |
| CurrentUser | Undefined |
| **LocalMachine** | **RemoteSigned** |

### AppLocker / WDAC

- **AppLocker:** Policy found but `RuleCollections` is empty — no script blocking rules.
- **WDAC (CI Policy):** `EmodePolicyRequired=0`, `SkuPolicyRequired=0`, `VerifiedAndReputablePolicyState=0` — enforcement is off.

**Conclusion:** `wscript.exe` can run unsigned `.vbs` files on this workstation without restriction.

---

## Section 4 — Import / EDI Facility

### Import/Export Utility

`VFIMPEXP.EXE` is present at `\\visual\visual908$\VMFG\VFIMPEXP.EXE`.  
`VMBTSRCV.EXE` (Batch Receiver) is also present — this is the primary batch import executable.  
`VMRCVINS.EXE` (Receiver Insert utility) is also present.

### Schema / Format Files

No `.xsd`, `.imp`, `.fmt`, or `.dtd` files found in `\\visual\visual908$\VMFG`.

Only XML files found:

| File | Purpose |
|---|---|
| `VISUALCONFIG.XML` | Core Visual configuration |
| `INFORVISUALENTERPRISEVIEWS.XML` | View definitions |
| `INFORQUALITYMANAGEMENTVIEWS.XML` | QM view definitions |
| `CLOSED.xml` | Closed period flag |

### Import Drop-Folder Configuration

No INI files in the VMFG share contain keywords matching `import`, `folder`, `path`, or `drop`.
No network-mapped import drop folder was discovered automatically.

**Next step:** Open `VFIMPEXP.EXE` or `VMBTSRCV.EXE` manually to inspect the import file
format wizard and determine the expected input file schema (likely fixed-width or CSV).

---

## Section 5 — ION / Infor OS

### Services

No `ION`-specific services were found. Services matching the pattern returned only standard
Windows services (e.g., `LanmanWorkstation`, `SessionEnv`, Chrome/Edge elevation services).

### Processes

No ION-related processes are running.

### Registry

```
HKLM:\SOFTWARE\Infor\ION          → Not found
HKLM:\SOFTWARE\WOW6432Node\Infor\ION → Not found
HKCU:\SOFTWARE\Infor\ION           → Not found
```

### Conclusion

**ION is not installed, licensed, or reachable from this workstation.**  
Any integration approach requiring ION (Approach 6) is not viable without a separate server
deployment. Only Approach 2 (VBScript/SendKeys), Approach 3 (UI Automation), and
Approach 5 (batch file import via `VMBTSRCV.EXE`) are feasible on this environment.

---

## Section 6 — Network and Server Connectivity

### ODBC Data Sources

| DSN Name | Type | Driver | Server | Database |
|---|---|---|---|---|
| **VISUAL** | System 32-bit | SQL Server (`SQLSRV32.dll`) | *(no Server field stored — uses named pipe/default)* | `MTMFG` |
| **VQ** | System 32-bit | SQL Server (`SQLSRV32.dll`) | `sql04` | `VQ` |
| MTM Receiving Application | System 32-bit | MySQL ODBC 8.0 Unicode Driver | — | — |
| MTM Receiving Application | System 64-bit | MySQL ODBC 9.6 Unicode Driver | — | — |

### TCP Connectivity Tests

| Host | Port | Result |
|---|---|---|
| `visual` | 1433 | ✅ **Connected** |
| `sql04` | 1433 | ❌ Not reachable |

**The MTMFG SQL Server database is on host `visual`, reachable at port 1433.**  
The VISUAL ODBC DSN connects to `visual` (the Server field is blank because it uses the
default/named pipe connection which resolves via the DSN name itself).

### Mapped Network Drives

| Drive | Remote Path | Description |
|---|---|---|
| G: | `\\mtmanu-fs01\Users\jkoll` | User home drive |
| H: | `\\mtmanu-fs01\Users\mhandler` | Another user home |
| X: | `\\mtmanu-fs01\Expo Drive` | Shared Expo Drive |
| Z: | `\\mtmanu-fs01\Users` | All users folder |

**File server for drop-folder use:** `\\mtmanu-fs01`  
The `X:` drive (`\\mtmanu-fs01\Expo Drive`) may be a viable handoff location for
Approach 5 import files if write permissions allow.

---

## Section 7 — Manual Inspection Notes

### Inspect.exe

Opened from: `C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\inspect.exe`

### Fields to Complete Manually

Use Inspect.exe or Accessibility Insights while Receiver Entry is open to confirm:

1. **Tab order** — press Tab from the Order ID field and number each stop
2. **Grid entry method** — does Tab advance to the line grid, or must you click?
3. **Keyboard shortcut** — how to open Receiver Entry from the main menu
4. **Whether the header Save/Post button** has an `AutomationId`

#### Confirmed AutomationId Map (with live PO data)

Field order inferred from Y-coordinate of bounding rectangle. Tab order within right panel (Buyer/FOB/Carrier)
relative to left panel is TBD — complete with Inspect.exe.

| Approx Order | Field Label | AutomationId | Populated Value | Notes |
|---|---|---|---|---|
| 1 | Site ID | 4097 | `MTM2` | ComboBox — usually pre-filled |
| 2 | Order ID (PO#) | **4103** | `PO-061514` | **Primary target field** |
| 3 | Receiver ID | **4109** | *(blank)* | Auto-assigned on post |
| 4 | Service Dispatch ID | 4114 | *(blank)* | |
| 5 | Act Recv Date | **4117** | `3/8/2026` | Date picker (calendar button at 4118) |
| 6 | Receive on | 4120 | `3/8/2025` | |
| 7 | Promise Delivery Date | 4122 | `3/8/2025` | |
| 8 | Promise Ship Date | 4124 | *(blank)* | |
| 9 | Bill of Lading ID | **4126** | *(blank)* | |
| 10 | Vendor Packlist ID | **4128** | *(blank)* | |
| 11 | Vendor Packlist Date | 4130 | *(blank)* | Calendar button at 4131 |
| 12 | Vendor Freight Bill ID | 4133 | *(blank)* | |
| ? | Buyer | **4136** | `SWITT` | Right panel |
| ? | FOB | **4138** | `SC` | Right panel, ComboBox |
| ? | Ship via | **4140** | *(blank)* | Right panel, ComboBox |
| ? | Carrier ID | **4142** | *(blank)* | Right panel, ComboBox |
| — | **Line item grid** | **4146** | — | `Gupta:ChildTable` — Tab into from last header field |
| — | Active grid cell | 32791 | — | Only UIA-accessible cell in grid; navigate via Tab/Enter |

---

## Open Questions Resolved

| Open Question | Answer |
|---|---|
| Exact process name for `AppActivate` | Process `VMRCVENT`; use window title `Purchase Receipt Entry - Infor VISUAL - MTMFG` |
| ClassName of Visual windows | `Gupta:AccFrame` |
| `AutomationId` for Order ID field | **4103** (Edit control) |
| `AutomationId` for Receiver ID field | **4109** (Edit control) |
| `AutomationId` for Act Recv Date | **4117** (Edit control) |
| `AutomationId` for Bill of Lading | **4126** |
| `AutomationId` for Vendor Packlist ID | **4128** |
| `AutomationId` for Buyer | **4136** |
| `AutomationId` for Ship via | **4140** (ComboBox) |
| `AutomationId` for line grid area | **4146** (`Gupta:ChildTable`) — only active cell (AutomationId 32791) accessible within it |
| Tab order | ⚠️ Requires manual inspection — complete with Inspect.exe |
| Whether `wscript.exe` can run unsigned VBS | **Yes** — RemoteSigned policy, no AppLocker rules |
| Visual import facility | `VFIMPEXP.EXE` and `VMBTSRCV.EXE` present; format TBD manually |
| ION present? | **No** — not installed, not reachable |
| SQL Server hostname for MTMFG | **`visual`** — reachable on port 1433 ✅ |
| Mapped shares for drop-folder | `\\mtmanu-fs01\Expo Drive` (X:) is a candidate |
| Visual installation type | **Network share only** — `\\visual\visual908$\VMFG` |
| Visual version | **9.0.8.031** |
| Grid column names | Confirmed via `VMRCVENT.INI` — see Section 2 grid table |

---

## Approach Feasibility Summary

| Approach | Feasible? | Notes |
|---|---|---|
| **Approach 2** — VBScript/SendKeys | ✅ Yes | `wscript.exe` available, no policy blocks, window title confirmed, AutomationIds captured |
| **Approach 3** — UI Automation | ✅ Yes | `AutomationId` values confirmed for all header fields; grid is `Gupta:ChildTable` — column headers **NOT exposed via UIA**, only active cell (32791) accessible; grid navigation must use Tab/Enter keystrokes |
| **Approach 5** — Batch file import (`VMBTSRCV.EXE`) | ⚠️ Possible | Executable found; file format must be confirmed by running the utility manually |
| **Approach 6** — ION API | ❌ No | ION is not installed on this site |

---

## Recommended Next Steps

1. **Approach 2/3 (preferred):** Use the `AutomationId` map above to write a UI Automation
   script targeting `Purchase Receipt Entry - Infor VISUAL - MTMFG`, class `Gupta:AccFrame`.
   - Focus Order ID field (AutomationId `4103`) → enter PO number → press Enter/Tab
   - Visual will auto-populate: Buyer (`4136`), FOB (`4138`), dates, vendor info
   - Fill remaining header fields (Act Recv Date `4117`, Bill of Lading `4126`, etc.)
   - Tab into the line grid (`4146` — `Gupta:ChildTable`); the active cell `32791` becomes the entry point
   - Navigate columns with **Tab** (next column), **Enter** (next row); track column by INI tab-order numbers
   - **Column headers are not addressable via UIA** — must know column position by count

2. **Complete manual Tab-order walk:** With Inspect.exe open, press Tab from Order ID and
   record every stop number. This is required for Approach 2 SendKeys sequencing.

3. **Investigate `VMBTSRCV.EXE`:** Launch it and document the expected import file format
   (column count, delimiters, field order). This resolves the remaining Approach 5 unknown.

4. **Confirm MTMFG server name:** The ODBC DSN `VISUAL` connects to host `visual` (confirmed
   TCP). Update `appsettings.json` SQL Server connection string to use `Server=visual`.
