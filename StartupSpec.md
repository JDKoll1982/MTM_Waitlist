# App Startup Resilience & Implementation Plan

## Document Status & Baseline Approval

This technical specification has moved from a high-level architecture proposal to an actionable engineering baseline.

The following decisions are locked for implementation:

- Splash First Screen: The application startup experience must begin with a new standalone Splash window.
- Shared Workstation Identification: The application will query the local host using standard Windows APIs to retrieve the computer hostname and MAC address. These values will be used together as a composite key during the MySQL workstation validation check.
- Identity Single Source of Truth: The Windows environment username (`%USERNAME%`) is the absolute primary source value for startup identity checks.
- Startup Schema Authoring: A final startup schema does not currently exist and must be created with users, workstations, and sessions artifacts organized under `./Database` subfolders by file type.
- Duplicate User Records Mitigation: If a query returns multiple records for the same user, the system will silently delete the oldest record based on update timestamps and retain the newest record.
- Settings UX Recovery Flow: If Phase 1 configuration loading fails, the user will be given two options: `Try Again` or `Reset to Defaults`. If the corrupt setting can be identified, the system should reset only that setting. If no targeted remediation can be found, the system should reset all settings before re-running Phase 1.
- MySQL Timeout and Circuit Breaking: The connection engine will enforce a 10-second connection timeout. It will attempt a maximum of 2 retries using exponential backoff before entering the `Database Down` state.
- Session Time Validation: Session validity must be checked against server time from a database function.
- Session Token Source Priority: Session token data must be read from both local storage and the database, with local data taking precedence.
- Database Down UX: Database failures must be shown as an error state within the Splash Screen with a `Retry` button and a `Close App` button. Automatic retrying is not allowed.
- Workstation Override Policy: If hostname and MAC address do not match a known shared workstation record, no manual override path is allowed.
- Saved User Record Source: Saved user records come from the database only.
- Unknown Workstation Routing: Startup must route to Login first. `New User` is shown only when workstation registration status is authoritatively confirmed as unregistered.
- Role Resolution Source: Admin/developer determination must come from role values stored in the database.
- Developer Mode: The application must include a Developer Mode that allows logs to be read inside the app and exported into readable, printable workflow reports.
- Developer Mode Access: Developer Mode is available only to users with Developer Access. If the current session user is not a developer, the app must prompt for developer username and password before access is granted.
- Developer Mode Export Format: Developer Mode exports workflow reports in HTML format.
- Developer Mode Export Visibility: Exported Developer Mode workflow reports do not require redaction.
- Developer Mode Release Scope: Developer Mode is included in the MVP.
- Splash Screen Text Standard: All user-facing startup text must be written for end users, avoid code jargon, and present every startup step on the Splash Screen.
- Splash Window Chrome Standard: The startup splash window must hide the title bar and caption buttons (close, minimize, maximize) and use a compact centered startup footprint.
- MVP Logging Destination: Secure startup logs must be written to the hosted VM logging location and forwarded to a centralized logging database in the MVP.
- Centralized Logging Target Selection: The centralized logging destination must be configurable by an admin or developer. If the centralized logging target is unset and the current user is not an admin or developer, the app must not allow startup to continue. If the centralized logging target is unset and the current user is an admin or developer, the app must require that user to choose or set the destination before startup can proceed.
- Centralized Logging Prompt UX: When destination setup is required, the prompt must open automatically during startup and include direct folder selection support (including a browse action).
- Centralized Logging Destination Baseline: No centralized destination is confirmed at this time. If any legacy destinations are discovered, they must be migrated to the new logging workflow.
- Logging Setup Cancellation Rule: If an admin or developer cancels centralized logging destination setup, startup must stop.
- Hosted VM Log Retention Policy: Startup logs must be stored at the configured hosted VM logging location using the daily file pattern `startup_daily_YYYY_MM_DD.log`. The log format must use plaintext JSON Lines (`.jsonl`) for MVP.
- MVP Log Format Selection: The MVP startup log format is plaintext JSONL.
- Hosted VM Log Cleanup Rules: During Phase 1 cleanup, files older than 14 calendar days must be deleted. If the hosted VM log directory exceeds 250 MB, the oldest daily files must be purged until the directory drops below the threshold.
- Hosted VM Log Cleanup Performance: Hosted VM directory scanning and cleanup must run asynchronously alongside configuration loading so the Splash Screen remains non-blocking.
- Splash Screen Progress Strings: The startup progress text is finalized and must use the approved five-step wording defined in this specification.
- Developer Mode Export Location: Developer Mode workflow reports must be exported to `%USERPROFILE%\Downloads\MTM_Waitlist_Diagnostic_Reports\`.
- Developer Mode Export Naming Convention: Exported reports must use the format `WorkflowReport_{Hostname}_{WindowsUsername}_YYYYMMDD_HHmmss.html` so each report is unique and sortable.
- Developer Mode Export Directory Handling: If the export directory does not exist, the application must create it automatically when the user exports a report.
- Developer Mode Post-Export Feedback: After a successful export, the application must show a non-blocking toast notification with a direct shortcut to open the export directory.
- Startup Administrative Controls: Access to startup-required administrative controls is restricted to the Developer role.
- RBAC Role Set (Current): Material Handler, Production, Production Lead, Setup, Setup Lead, Plant Manager, Developer.
- Cloud PC Recovery: This is a future enhancement and is excluded from the MVP scope.

## Technical Architecture & Implementation Details

```text
+---------------------------------------+
|       Phase 1: Environment Boot       |
| (Read %USERNAME% and load local config) |
+-------------------+-------------------+
                    |
                    v
+---------------------------------------+
|     Phase 2: Identity and DB Match    |
|   (Verify workstation and user in DB) |
+-------------------+-------------------+
                    |
                    v
+---------------------------------------+
|    Phase 3: Session and Cache Validate|
|   (Check token state and prime cache) |
+-------------------+-------------------+
                    |
         +----------+----------+
         |                     |
         v                     v
 [Valid Session Token]   [Invalid or Expired Token]
         |                     |
         v                     v
+----------------------+  +----------------------+
| Route to Main Window |  | Route to Login Screen|
+----------------------+  +----------------------+
```

## 1. Detailed Phase-Based Startup Sequence

### Phase 1: Environment

Priority: App-Blocking

System tasks:

- Read the local Windows environment username through system environment strings.
- Initialize the local JSON configuration parser.
- Load base application configuration values.

Failure impact:

- A failure here indicates a corrupted local workspace or a host access restriction.
- The application must log a critical event locally and terminate immediately without continuing startup.

### Phase 2: Identity and DB

Priority: Critical

System tasks:

- Establish a secure connection pool to the central MySQL instance.
- Query the workstation configuration table using the local machine hostname and MAC address.
- Determine whether the device is a registered shared workstation.
- Search for an existing profile that matches the Windows username.

Failure impact:

- If the database fails to respond within the timeout window or throws a connection error, the Splash Screen must show an error state with `Retry` and `Close App` actions.
- The app must not retry automatically.

### Phase 3: Session and Cache

Priority: Recoverable

System tasks:

- Inspect local secure storage for an active authentication token.
- Inspect the database token source for startup token state.
- If a token exists, verify its expiration timestamp against server time using a database function.
- When local and database token values differ, local token data has precedence.
- If the token is valid, deserialize the cached session data into memory and prepare the landing screen.

Failure impact:

- If the token is expired, missing, or malformed, the session is treated as invalid.
- The application must safely purge the cached session and route to the standalone login screen.

## 2. State-Based Routing Rules

The startup orchestration module routes users according to the following evaluation order:

1. If the Windows username matches a saved user record:
   - If a local session token exists and is valid, route directly to Main Window.
   - If no valid session token exists, route to Login Screen.
2. If the Windows username does not match a saved user record:
  - If the workstation hostname and MAC address are registered in MySQL as a shared workstation, route to Login Screen.
  - If workstation status is authoritatively known and the workstation is unregistered, route to Login Screen and expose a `New User` action.
  - If workstation status is not authoritative (for example, startup could not verify workstation registration from the database), route to Login Screen without exposing a false `New User` unregistered-workstation state.

No manual override path exists for unknown or unregistered workstations.

## 3. Step-by-Step Recovery Flows

### Configuration Failure Recovery

1. Phase 1 catches an I/O read error or JSON parsing exception.
2. The splash flow stops and shows a user-facing system dialog.
3. The dialog presents the problem along with `Try Again` and `Reset to Defaults`.
4. If the user selects `Reset to Defaults`, the system first attempts to identify and reset only the corrupt setting.
5. If the corrupt setting cannot be isolated or remediated, the system resets all settings.
6. The startup state machine restores the required default configuration state and re-runs Phase 1.

### Duplicate or Damaged User Records

1. Phase 2 detects multiple rows for the same identity.
2. The data access layer enters an internal resolution transaction.
3. The resolution logic identifies the oldest record using metadata timestamps.
4. The oldest record is deleted.
5. The newest valid record is loaded into memory.
6. Startup continues without prompting the user.

## 4. Security, Compliance, and Observability

### Security Compliance Blueprint

- Framework Alignment: The operational checks and event pipeline are aligned to the NIST Cybersecurity Framework Detect function and ISO/IEC 27002 controls for preservation and log protection.
- Audit Accountability: Telemetry must support sequential, reviewable records suitable for incident reconstruction and forensic audits.

### Standardized Log Metadata Schema

Every startup log event must use structured logging. Unstructured string logs are not allowed.

```json
{
  "timestamp": "2026-07-18T10:00:00.124Z",
  "level": "CRITICAL",
  "source_info": "IdentityService.cs:line_142",
  "actor": {
    "id": "usr_9921",
    "type": "HUMAN"
  },
  "context": {
    "host_id": "WKSTN-PROD-042",
    "mac_address": "00:1A:2B:3C:4D:5E",
    "environment": "Production"
  },
  "event": {
    "action": "WORKSTATION_REGISTRATION_CHECK",
    "outcome": "SUCCESS",
    "message": "Workstation identified as a validated shared terminal."
  }
}
```

Logging requirements:

- Timestamp: Zero-padded ISO-8601 format for consistent sorting.
- Source Info: File and line trace in the form `{filename}:{lineno}`.
- Actor Attribution: Must identify whether the action came from a human, service, system routine, or automated process.
- Context Matrix: Must capture relevant infrastructure details such as host IDs, network markers, and runtime context.
- Outcome Focus: Each event must record a definitive outcome such as `SUCCESS`, `FAILURE`, or `BLOCKED`.

### Log Integrity and Tamper Protection

- Cryptographic Chaining: Each log payload contains a hash of its own data plus the hash of the previous log item.
- Validation Verification: If a record is altered or removed, the chain breaks and the integrity issue is detectable.
- Immutable Storage: Production telemetry should be sent to write-once or otherwise tamper-resistant storage.
- MVP Log Routing: In the MVP, startup telemetry must be written to the hosted VM logging location and forwarded securely to the centralized logging database.
- Target Configuration Rule: The centralized logging target must be set by an admin or developer. If no target is configured, non-admin and non-developer users must be blocked from continuing startup. Admin and developer users must be prompted to choose or set the centralized destination before startup continues; if they cancel setup, startup must stop.
- Hosted VM Storage Directory: Startup logs must be stored at the configured hosted VM logging location.
- Hosted VM File Pattern: Daily startup log files must follow the pattern `startup_daily_YYYY_MM_DD.log`.
- Hosted VM Log Format: Startup logs must use plaintext JSON Lines (`.jsonl`) for MVP.
- Time-Based Expiration: Files older than 14 calendar days must be deleted during Phase 1 cleanup routines.
- Size-Based Retention: If the hosted VM logging directory exceeds 250 MB, the oldest daily files must be deleted until the directory is below the size threshold.
- Cleanup Execution: Hosted VM log retention scans and cleanup must run asynchronously alongside configuration loading to avoid blocking Splash Screen progress.

### Log Performance Isolation and Non-Blocking Queue

To prevent local disk latency, centralized logging latency, or intermittent network disconnections from slowing the Splash Screen startup sequence, direct synchronous log writing from the main startup flow is prohibited.

- Architecture Pattern: Logging must use a producer-consumer pattern with in-memory buffering between the startup sequence and the log delivery layer.
- Main Thread Behavior: The main application thread must generate log events and hand them off through an immediate non-blocking in-memory operation. Startup logging must not perform direct file I/O, network I/O, or database writes on the UI thread.
- Memory Buffer: Log events must be placed into a thread-safe in-memory queue or channel designed for high-throughput sequential background processing.
- Background Processing: A dedicated background worker must pull log events from the in-memory buffer and handle all actual persistence work out-of-band from startup execution.
- Centralized Delivery: The background worker is responsible for writing logs to the hosted VM logging location and forwarding them to the centralized logging destination.
- Fault Tolerance: If the logging destination becomes temporarily unreachable, the background worker must apply a bounded retry strategy with backoff while keeping failures isolated from the main application thread.
- Startup Protection: Logging failures must never throw exceptions back to the startup sequence, block the Splash Screen, or prevent the app from continuing through its defined routing and failure-handling rules.
- Shutdown Handling: On application shutdown, the logging subsystem must attempt a graceful flush of remaining in-memory log events according to the final retention and delivery rules.

### Developer Mode Logging Requirements

- The application must provide a Developer Mode for authorized use.
- Developer Mode must allow startup and runtime logs to be read from within the application.
- Developer Mode must support exporting logs into readable HTML workflow reports that can be printed or shared for troubleshooting.
- Exported workflow reports should translate raw log events into a step-by-step sequence of what the application attempted, what succeeded, what failed, and where the process stopped.
- Exported workflow reports should remain readable by non-developers while preserving enough technical detail for support and engineering review.
- Developer Mode must not change normal startup behavior for standard users unless it has been explicitly enabled.
- If the current session user does not already have Developer Access, the app must require developer credentials before opening Developer Mode.
- Exported workflow reports do not require redaction.
- Export Directory: Developer Mode workflow reports must be saved under `%USERPROFILE%\Downloads\MTM_Waitlist_Diagnostic_Reports\`.
- Export File Naming: Exported workflow reports must use the format `WorkflowReport_{Hostname}_{WindowsUsername}_YYYYMMDD_HHmmss.html`.
- Export Directory Creation: If the export subfolder does not exist, the app must create it automatically during export.
- Post-Export Notification: After a successful export, the app must show a non-blocking toast notification with a direct link to open the export directory.

## 5. Module Access Control (RBAC Matrix)

Role-based access control rules are enforced at the application engine level based on the confirmed user profile resolved during Phase 2.

### Waitlist Module

| Action | Required Role Boundary |
|---|---|
| View Requests | Material Handler, Production, Production Lead, Setup, Setup Lead, Plant Manager, Developer |
| Add New Request | Production, Production Lead, Setup, Setup Lead, Plant Manager, Developer |
| Take Request | Material Handler, Production Lead, Setup Lead, Plant Manager, Developer |
| Edit/Cancel Request | Production Lead, Setup Lead, Plant Manager, Developer |

### Work Stations Module

| Action | Required Role Boundary |
|---|---|
| View Work Stations | Material Handler, Production, Production Lead, Setup, Setup Lead, Plant Manager, Developer |
| Add/Update Station | Setup, Setup Lead, Plant Manager, Developer |
| Delete Work Station | Setup Lead, Plant Manager, Developer |

### Startup Administrative Controls

| Action | Required Role Boundary |
|---|---|
| Configure centralized logging destination | Developer only |
| Override startup diagnostics settings | Developer only |

## 6. Splash Screen UX and Communication Standards

The startup experience should prioritize clarity, responsiveness, and reduced user friction.

```text
+---------------------------------------------------------------+
|                       SYSTEM INITIALIZING                     |
|                                                               |
|                        [==============>      ]                |
|                                                               |
|                Step 2 of 5: Validating Workstation...         |
+---------------------------------------------------------------+
```

- Determinate Progress Mapping: The splash interface must use contextual `X of Y` progress steps.
- All startup steps must be shown on the Splash Screen using end-user-facing language and no code jargon.
- Progressive Disclosure: Error cards must show clear, actionable messaging first and keep technical details hidden unless explicitly expanded.
- Inclusive Visual Semantics: Alerts must not rely only on color. Use iconography and strong text contrast.
- Action-Oriented Context: Error copy must remain polite, clear, and non-accusatory.

Approved user-facing progress strings:

- Step 1 of 5: Loading application settings...
- Step 2 of 5: Checking device registration...
- Step 3 of 5: Verifying user identity...
- Step 4 of 5: Validating login session...
- Step 5 of 5: Loading data dashboards...

## Needs To Be Addressed

The following items still need clarification before startup behavior is fully specified:

- No open clarification items remain in this specification at this time.

## Default Output Data Locations

The following default file system locations are used for output data described in this specification:

- Startup logs at hosted VM location: Admin- or developer-configured hosted VM logging path
- Developer Mode HTML workflow reports: `%USERPROFILE%\Downloads\MTM_Waitlist_Diagnostic_Reports\`
