# Contract: machine configuration, the setup gate, and the four permission keys

Source: brief S10, S10.1, S11.5.6, S2 rows 8, 9, 21, 22, 23. Requirements: FR-006, FR-007, FR-008, FR-009,
FR-018, FR-024, FR-029, FR-030.

---

## 1. The readiness check the pipeline enforces

```csharp
public interface IMachineConfigurationService
{
    Task<MachineConfigurationState> GetStateAsync(CancellationToken cancellationToken);

    Task<MachineConfigurationSaveResult> SaveAsync(
        MachineConfigurationDraft draft,
        CancellationToken cancellationToken);

    Task<MachineConfigurationResetResult> ResetToDefaultsAsync(
        IReadOnlyList<string> whatIsBroken,
        CancellationToken cancellationToken);
}

public sealed record MachineConfigurationState(
    bool IsConfigured,
    string? DisplayName,
    string? Description,
    IReadOnlyList<PictureSource> PictureSources,
    string? UnconfiguredReason);

public sealed record MachineConfigurationDraft(
    string DisplayName,
    string Description,
    IReadOnlyList<PictureSource> PictureSources);

public sealed record PictureSource(
    string Kind,
    string Path);
```

`UnconfiguredReason` distinguishes the four ways a machine can be unconfigured, because the diagnosis has to be
specific to the cause (FR-004): never configured, configuration removed, configuration revoked, or
configuration unreadable.

Rules drawn from requirements:

- **FR-006**: an unconfigured machine does not reach the main screens by any route.
- **FR-009**: removed, revoked and unreadable all resolve to unconfigured at the next check. Losing
  configuration never degrades into running without it.
- **There is no record destination to capture.** Records live only in the store, so machine configuration
  carries the machine's identity and its picture sources and nothing else (FR-025, brief decision 23). This is
  the second reason the old log-folder prompt disappears: not only did logging move, a folder was never the
  destination in the first place.
- `SaveAsync` refuses a display name already in use, and the screen asks for a different one.
- Picture sources are machine configuration. They were `appsettings.json` values before this work.

## 2. The setup gate

```csharp
public interface IMachineSetupGate
{
    Task<MachineSetupAuthorization> AuthorizeAsync(
        string signInName,
        string credential,
        CancellationToken cancellationToken);
}

public sealed record MachineSetupAuthorization(
    bool IsAuthorized,
    IPersonIdentity? Person,
    string? RefusalReason);
```

Rules drawn from requirements:

- **FR-007**: authorised only by a person holding `IT Department` or `Developer`. The two role codes are
  `role:it_department` and `role:developer`.
- **FR-007**: that authorisation does **not** open the main screens. It authorises configuration and nothing
  else. The person is not signed in to the application by it.
- The gate authenticates the person itself, so machine setup does not depend on the launch pipeline's identity
  handling.
- A refusal is stated. Declining the setup sign-in is an abort route and ends the process (FR-008).

## 3. No bypass

The step has exactly two outcomes: the machine is configured and the launch continues, or the process ends.
There is no third outcome in which the application runs unconfigured.

| Route | Result |
|---|---|
| Setup completes and saves | Machine configured, launch continues |
| Window close box | Process ends, reason stated first |
| Escape | Process ends, reason stated first |
| Alt+F4 | Process ends, reason stated first |
| A cancel or back control | Process ends, reason stated first |
| Setup sign-in declined or refused | Process ends, reason stated first |
| A dismissal route the screen does not know about | The pipeline still refuses to continue, so the shell is still unreachable |

**Enforcement lives in the pipeline, not only in the screen.** The screen is the invitation; the pipeline is the
gate. That is what makes the last row of the table true.

The reason is stated before the process goes, so an operator who aborts setup does not read the vanishing window
as a crash. The message is localised: an unlocalised hard close is reported as instability (S15).

## 4. What a reset may touch

`ResetToDefaultsAsync` receives the list of what is broken and resets only that.

| May reset | Must never touch |
|---|---|
| This machine's configuration rows: display name, description, picture sources | Any person, role or permission |
| A broken scoped preference row for this machine's scope | Another machine's configuration |
| | Any session |
| | Any log entry |

Rules drawn from requirements:

- **FR-018**: the preview names exactly what will be reset before anything is reset, and only this machine's
  configuration is affected.
- **FR-017**: offered only where resetting could remove the cause. A store outage offers no reset, because a
  reset cannot repair it.
- **FR-019**: a fault repairable without the person's involvement is repaired without asking.

## 5. The four permission keys

Each needs a catalogue row, a role baseline, a seed and a place on the Settings privileges page, and each is
enforced where the action happens rather than only where the control is drawn.

| Key | Held by | Gates | Requirement |
|---|---|---|---|
| `permission.settings.machine_configuration` | `IT Department`, `Developer` | Opening machine setup and saving its configuration | FR-007 |
| `permission.settings.ignored_locations_edit` | `IT Department`, `Developer` | Changing the plant-wide list of locations to hide. Reading stays open to everyone | FR-024 |
| `permission.settings.log_panel` | `Developer` | The developer log panel in Settings | US5, SC-005 |
| `permission.settings.session_length` | `IT Department`, `Developer` | Changing how long a session lasts | FR-029 |

**The existing key is not narrowed.** `permission.settings.ignored_locations` keeps its current grants, which
are 1 for `role:developer`, `role:it_department`, `role:plant_manager`, `role:production_lead`, `role:setup_lead`,
`role:production` and `role:setup`, and 0 for `role:material_handler` and `role:material_handler_lead`. Narrowing
it would remove read access from five roles and break FR-030, which requires every setting restricted to roles
today to keep the same restriction. The new `_edit` key carries FR-024's two-role requirement instead, so both
requirements hold at once.

**`permission.settings.computers` is not reused for machine setup**, even though its baseline is already
`developer` and `it_department` at 1 and every other role at 0, which is exactly the required restriction. The two
surfaces have different blast radii: machine setup admits a computer to the fleet before anyone signs in, while
the Settings computers screen edits rows afterwards. Sharing one key would mean a future loosening of the
Settings screen silently opens the pre-sign-in gate.

## 6. What stays local

Only the connection strings used to reach the store, plus one reviewed exception.

| Setting | Disposition |
|---|---|
| `StartupDatabaseOptions` connection string | kept, in the new module's own options |
| `ReceivingDatabaseOptions` connection string | kept |
| `InforVisualDatabaseOptions` connection string | kept, read directly |
| `MockServiceClient` `Endpoint` and `UserName` | **kept as a deployment setting.** It carries no secret and no per-person or per-machine state, so it is configuration rather than local state. This is the one reviewed exception |
| `ImageStorageOptions`, `DunnageImageOptions.RootFolder` | moved to machine configuration |
| `LocalSettingsOptions` | deleted with the mechanism |
| `StartupDevelopmentOptions` | deleted, the developer allow-list is retired |
| `StartupLoggingOptions` | deleted, logging goes to the store |
| `StartupWindowOptions` | deleted, or folded into the new module's window options |
| `ModuleSharedOptions`, `ModuleCoreSettingsOptions` | verified before any decision. Both bind today to defaults and neither key appears in `appsettings.json`, so neither is deleted blind |
