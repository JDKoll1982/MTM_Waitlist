# Contract: Launch steps, the runner, and the host seam

Source: brief S4 (target architecture), S5 (splash and activity feed), S6 (the step model). Requirements:
FR-001 to FR-005, FR-016, FR-017, FR-019, FR-020, FR-026, FR-027.

This is the interface the splash binds to, the interface the host calls, and the interface a test drives to
prove retry repeats only what failed.

---

## 1. The step

Steps are data. The displayed count is derived from the list, never stored.

```csharp
public sealed record LaunchStep(
    string Id,
    string Name,
    string Description,
    LaunchStepCategory Category,
    TimeSpan MaximumWait,
    bool IsBestEffort);

public enum LaunchStepCategory
{
    Configuration,
    Machine,
    Session,
    Pictures,
    ExternalSystem,
    Shell
}
```

| Member | Rule | Requirement |
|---|---|---|
| `Id` | stable, unique, the value retry resumes at | FR-020 |
| `Name` | shown before the work runs, never after | FR-002 |
| `Description` | plain language, no step numbers | FR-002 |
| `Category` | groups the feed and lets a remedy be matched to a cause | FR-004 |
| `MaximumWait` | always set. 30 seconds is the ceiling; `IsBestEffort` steps are bounded more tightly | FR-003, SC-002 |
| `IsBestEffort` | true for the picture refresh and the external-system priming. A best-effort step cannot stop the launch | FR-026, FR-027 |

Derived, not stored: `TotalCount` from the step list, `CompletedCount`, and the progress text. Adding or
removing a step must never leave a stale "of 5".

## 2. Running one step

```csharp
public interface ILaunchStep
{
    LaunchStep Descriptor { get; }

    Task<LaunchStepOutcome> RunAsync(
        LaunchStepContext context,
        CancellationToken cancellationToken);
}

public sealed record LaunchStepContext(
    IPersonIdentity? Person,
    IMachineFacts Machine,
    ILaunchActivityFeed Feed);

public sealed record LaunchStepOutcome(
    LaunchStepStatus Status,
    string? Diagnosis,
    LaunchRemedySet Remedies);

public enum LaunchStepStatus
{
    Succeeded,
    Skipped,
    Failed
}
```

A step receives the person only after identity has been resolved, so `Person` is null for the steps that run
before sign-in. A step never reads the store directly; it calls a service, and that service calls a stored
procedure.

## 3. The activity feed

One line per thing the process does, append-only, with a timestamp, so a lock-up is attributable to a named
line rather than a step number (S5).

```csharp
public interface ILaunchActivityFeed
{
    void Append(LaunchFeedEntry entry);
    IReadOnlyList<LaunchFeedEntry> Entries { get; }
    event EventHandler<LaunchFeedEntry> EntryAppended;
}

public sealed record LaunchFeedEntry(
    DateTimeOffset TimestampUtc,
    string StepId,
    LaunchFeedEntryKind Kind,
    string Text,
    string? Target,
    bool? Succeeded);

public enum LaunchFeedEntryKind
{
    StepStarted,
    StepCompleted,
    StepFailed,
    SubOperation
}
```

Rules:

- The feed names the operation, its target (store, share, server) and its outcome (S5).
- A step announces itself before it runs, so a stall has a name attached (FR-002, US1 scenario 1).
- The feed is the debugging surface for a live fault. It is never cleared while a launch is running.
- A best-effort failure still gets a line. It is reported, not hidden (FR-026).

## 4. The diagnosis and the error toast

```csharp
public sealed record LaunchDiagnostic(
    string StepId,
    LogSeverity Severity,
    string Message,
    string? Cause);

public sealed record LaunchRemedySet(
    bool CanRetry,
    bool CanRestoreDefaults,
    string? RestoreDefaultsPreview);
```

`LogSeverity` is the same type the logging seam uses, defined in `logging-contract.md`, so the diagnosis shown
on the launch surface and the entry written to the store are filterable with one vocabulary.

Rules:

- The diagnosis is specific to what stopped the launch (FR-004). A step number is never the whole message.
- The message is shown in a strip anchored to the bottom of the launch window, and it does not cover the feed
  (FR-005).
- `CanRestoreDefaults` is false when resetting could not remove the cause. A store outage never offers a reset
  (FR-017, US4 scenario 1).
- When `CanRestoreDefaults` is true, `RestoreDefaultsPreview` names exactly what will be reset before anything
  is reset (FR-018).
- A fault repairable without the person's involvement produces no prompt at all and does not reach this type
  (FR-019).

## 5. The pipeline and retry

```csharp
public interface ILaunchPipeline
{
    Task<LaunchOutcome> RunAsync(CancellationToken cancellationToken);

    Task<LaunchOutcome> RetryFromAsync(
        string failedStepId,
        CancellationToken cancellationToken);

    event EventHandler<LaunchOutcome> ShellReady;
    event EventHandler<string> ProcessEnding;
}

public enum LaunchOutcome
{
    MainScreens,
    SignIn,
    MachineSetup,
    Blocked,
    Ended
}
```

Rules:

- `RunAsync` ends at exactly one of `MainScreens`, `SignIn`, `MachineSetup`, `Blocked` or `Ended` (FR-001).
- `RetryFromAsync` runs the named step and the steps after it, in order, and nothing before it. A database-only
  retry never re-runs step 1 (FR-020, S6).
- A second entry into the launch surface while a sequence is running returns without starting anything, so a
  re-entrant navigation cannot run two pipelines against one machine.
- The pipeline, not the screen, refuses to continue while the machine is unconfigured (FR-006, S10.1).
- `ProcessEnding` carries the reason, and the reason is stated before the process goes, so an abort does not
  read as a crash (FR-008).

## 6. The host seam

The app host owns one call and one event. It does not own window handoff, which is what replaces
`IAppLifecycleService` (S4).

```csharp
// App.xaml.cs, on launch
var pipeline = App.GetService<ILaunchPipeline>();
pipeline.ShellReady += (_, outcome) => /* show the main window and navigate */;
pipeline.ProcessEnding += (_, reason) => /* state the reason, then exit */;
_ = pipeline.RunAsync(CancellationToken.None);
```

Rules:

- The host never calls a window-handoff method, and no equivalent of `ShowSplashWindow`,
  `ShowMainWindowAndCloseSplash`, `ShowLoginWindowAndCloseSplash`, `ShowMainWindowAndCloseLoginWindow` or
  `CloseSplashWindow` exists after Phase 2 (S3.1).
- The shell is reachable only through `ShellReady`. There is no bypass and no continue-anyway path (S4).
- Navigation stays hidden and the window stays at its launch size until `ShellReady` fires.
- A window that cannot be maximized falls back to the configured main size and the transition still completes.
