# Contract: the two identity contracts

Source: brief S7. Requirements: FR-010, FR-015, FR-022, FR-007, FR-024, FR-029. These two interfaces replace
`StartupState`, which is deleted in Phase 4 once its roughly twenty consumers have migrated.

Both are **read-only**. There is no writer other than the launch pipeline, and the pipeline writes through
services these interfaces do not expose. A consumer that needs to change something calls the owning service, not
these.

---

## 1. Person identity

```csharp
public interface IPersonIdentity
{
    long UserId { get; }
    string SignInName { get; }
    string DisplayName { get; }
    string? EmployeeNumber { get; }
    string CurrentRoleCode { get; }
    IReadOnlyList<string> HeldRoleCodes { get; }
    bool IsSignedIn { get; }
    bool Holds(string roleCode);
}
```

| Member | Source | Rule |
|---|---|---|
| `UserId` | `core_users_profiles.id` | |
| `SignInName` | `core_users_profiles.username_normalized` | unique and upper-normalised |
| `DisplayName` | `core_users_profiles.display_name` | the name the shell badge and the tooltips show |
| `EmployeeNumber` | `core_users_profiles.employee_identifier` | nullable |
| `CurrentRoleCode` | the role in force for this session | read from the store, never from a local claim |
| `HeldRoleCodes` | every assignment for the person | the set. `Holds` is answered from this |
| `IsSignedIn` | derived | false before sign-in completes, and false after sign-out |

Rules drawn from requirements:

- **FR-022**: available to the rest of the application for reading only. Nothing outside the launch pipeline may
  assign to it, and the implementation exposes no setter.
- Roles come from the store only. The `appsettings.json` developer allow-list is retired, and the development
  seed grants `Developer` to `JKoll` and `JohnK` so each seeded machine still has a developer who can sign in.
- A role is never inferred from the workstation, a build configuration or a file.
- `Holds("role:it_department")` and `Holds("role:developer")` are the gate for machine setup (FR-007), the
  plant-wide locations list (FR-024) and the session-length setting (FR-029). The literal role codes are
  `role:it_department` and `role:developer`.
- The identity is not persisted anywhere on the machine. Sign-out ends it (FR-011).

Consumers to migrate, in the order the brief lists: permissions, user management, waitlist attribution, tooltips
and the control inspector, Settings computer screens, picture preferences, and the shell's user badge.

## 2. Machine facts

```csharp
public interface IMachineFacts
{
    string Hostname { get; }
    string? MacAddress { get; }
    ComputerRecord? RegisteredComputer { get; }
    bool IsRegistered { get; }
    bool HardwareIdentityReadable { get; }
}
```

`ComputerRecord` is the existing model at `MTM_Waitlist.Core/Models/ComputerRecord.cs`, reused rather than
replaced. It already carries `Id`, `ComputerName`, `DisplayName`, `Description`, `MacAddressNormalized` and
`IsRegistered`, which is every fact the machine-configuration screen and the shell badge need.

| Member | Source | Rule |
|---|---|---|
| `Hostname` | the machine, normalised | surfaced by the registry row's `ComputerName` and matched on `hostname_normalized` |
| `MacAddress` | the machine, normalised | null when no usable address exists. Matched on `mac_address_normalized` |
| `RegisteredComputer` | `core_computers_registry`, matched on identity | null when the machine is not registered |
| `IsRegistered` | the registry row's `is_registered`, confirmed by the match | |
| `HardwareIdentityReadable` | derived | false when no usable address exists |

Rules drawn from requirements:

- **FR-015**: a machine whose hardware identity cannot be read is admitted, because a fact that could not be read
  is not a failed check. `HardwareIdentityReadable` false does not make `IsRegistered` false.
- **FR-009**: a machine whose configuration is removed, revoked or unreadable resolves to unconfigured at the
  next check. Losing configuration never degrades into running without it.
- **FR-022**: read-only. Only the launch pipeline writes, and it writes through
  `MachineConfigurationService`, not through this interface.
- The registered row is the machine configuration's identity half. Where its pictures come from is the other
  half, described in `machine-configuration-contract.md`.

## 3. What the two contracts do not carry

Deliberately absent, and each for a stated reason:

- No session state. Session validity is judged against the store's clock (FR-010) and lives in
  `LaunchSessionService`. Putting a mutable expiry on the identity contract is how the old object drifted.
- No remember-me state. It is per person and per machine, and it has to survive sign-out.
- No credentials, tokens or key material, in any form.
- No temporary-credential attempt count. That is a store-side fact, not an identity fact.
