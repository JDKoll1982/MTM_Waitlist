---
description: 'How to work on the rebuilt startup module: its entry point, its host seam, its contracts, how to add a step, a log entry or a machine-configuration field, and the invariants a change must not break'
applyTo: "**/MTM_Waitlist.Startup/**,**/Module_Startup/**,**/App.xaml.cs"
---

# The rebuilt startup module

Feature `010-startup-rebuild` replaced the whole previous launch surface. Read this before changing anything under
`MTM_Waitlist.Startup/` or `Module_Startup/`, because the shape is deliberate and the guards fail the build when
it is broken.

Its living spec is `capabilities/startup-launch/spec.md`. The specification, plan and task list are under
`specs/010-startup-rebuild/`.

## The entry point

The host calls **one** method and listens to **two** events, all on `ILaunchPipeline`:

```csharp
var pipeline = App.GetService<ILaunchPipeline>();
pipeline.ShellReady += (_, outcome) => /* show the surface that outcome means */;
pipeline.ProcessEnding += (_, reason) => /* state the reason, then exit */;
_ = pipeline.RunAsync(CancellationToken.None);
```

- `RunAsync` walks `LaunchStepCatalog.Steps` in order and ends at exactly one of five
  `LaunchOutcome` values: `MainScreens`, `SignIn`, `MachineSetup`, `Blocked`, `Ended`.
- `RetryFromAsync(stepId)` runs the named step and everything after it, and nothing before it.
- A second entry while a run is in flight starts nothing and answers the run already going, so one machine never
  has two launches against it.
- `End(reason)` is how an abort route reaches the host: the surface states the reason, then calls it.

`App.xaml.cs`'s `ShowLaunchSurface(LaunchOutcome)` is the only place a surface is chosen. The shell is reachable
**only** from the `MainScreens` branch, and there is no continue-anyway path.

## Steps are data, and the catalogue is the sequence

`LaunchStepCatalog` declares every operation the launch performs: its id, its name, its description, its
category, its **stated maximum wait**, whether it is best effort, and what it is about (`Target`). The count the
launch surface shows is derived from that list, never stored, so adding a step cannot leave a stale total.

The catalogue validates itself at construction. It refuses an entry with no maximum, one past the thirty-second
ceiling, a best-effort entry at the ceiling, a duplicated id, a blank target or a nameless entry.

**A catalogued entry with no implementation is passed over but still counted.** Each phase of the feature
supplied implementations; if you add an entry, register its `ILaunchStep` in the module's DI extension in the same
change, or the launch will silently skip it.

## Adding a step

1. Add the entry to `LaunchStepCatalog` and, if you change a maximum, change the table in
   `contracts/launch-step-contract.md` §1.1 first: that table is where FR-003 is stated and the catalogue is what
   implements it.
2. Write a class implementing `ILaunchStep` and take its descriptor from the catalogue:

   ```csharp
   internal sealed class MyStep : ILaunchStep
   {
       private const string StepId = "my-step";
       private readonly LaunchStep _descriptor;

       internal MyStep(LaunchStepCatalog catalog) => _descriptor = LaunchStepSupport.DescriptorFor(catalog, StepId);

       public LaunchStep Descriptor => _descriptor;

       public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
           => LaunchStepSupport.RunGuardedAsync("What could not finish", WorkAsync, cancellationToken);
   }
   ```

3. Register it: `services.AddSingleton<ILaunchStep, MyStep>();`.

**Rules a step obeys.** It never announces itself: the runner writes the `StepStarted` line before calling it, so a
step that named itself would write it twice. It never routes: it returns a `LaunchStepOutcome` and the pipeline
decides. It never reads the store directly: it calls a service, and that service calls a stored procedure
(constitution III). `LaunchStepSupport.RunGuardedAsync` turns a dependency's exception into a stated failure
instead of letting it escape as an unattributed crash.

## Adding a log entry

`ILogService` in `MTM_Waitlist.Logging` is the seam. Call `Info`, `Warn`, `Error` or `Critical` with the module
name and the message, and pass the exception to `Error`/`Critical` rather than formatting it into the text.

Everything else is captured by the seam and must not be supplied by the caller: the machine, the person, the
severity vocabulary, the runtime and store context, the serialized exception chain and the fingerprint that
groups repeated faults. The seam is **unconditional** — no `[Conditional]`, no `#if DEBUG` — because a Release
build has to record.

Never write a credential, a token, a salt, the remembered-sign-in payload, key material or a connection string.
The shared key file's **path** is the one secret-adjacent value that may be written, deliberately, so a support
reader can see which key a decryption failure used. `contracts/logging-contract.md` §6 is the authoritative list,
and `ExceptionDetailSerializer.IsNeverWritten` is the check that enforces it.

## Adding a machine-configuration field

`IMachineConfigurationService` is the **only** writer of this machine's display name, its description and its
three picture sources, and `MachineConfigurationService` is its only implementation.

- For a field on the machine's own registry row, extend `MachineConfigurationDraft` and the service's save and
  read, over the `sp_core_computers_registry_*` procedures. Do not write the row from a screen.
- For a further picture source, the three kinds are in `MachineConfigurationSourceKinds` and are written as one
  act through `sp_config_images_locations_computer_sources_set`. Add the kind to that set, to the machine-setup
  screen and to the save's completeness check together, or a machine will be reported unconfigured.
- A reset is offered only where it could remove the cause and always previews what it will touch.
  `ResetToDefaultsAsync` takes the parts to reset, so a caller cannot reset more than it named.

**A preference about a person is not machine configuration.** Person preferences are scoped store settings, so
they follow the person to another computer. Nothing but what is needed to reach the store stays on the machine.

## Invariants a change must not break

- **Every wait on the launch path has a stated maximum** (FR-003). No unbounded wait, ever, including a read the
  pipeline itself makes for a routing decision.
- **A stop states a cause specific to it** and offers only remedies that could remove it (FR-004, FR-017).
- **The pipeline, not a screen, refuses to continue while the machine is unconfigured** (FR-006).
- **Roles come from the store only.** There is no local role override and no demo or mock mode
  (FR-022, FR-014 of feature 001).
- **Every data operation goes through a stored procedure**, and every schema artifact ships `create.sql` and
  `rollback.sql` (constitution III). `InlineSqlAuditTests` fails the build otherwise.
- **The replaced launch behaviour must not come back.** `RetiredSymbolAuditTests` fails the build if it does, and
  it asserts the rebuilt view set positively rather than only by absence.
- **Nothing is kept on the machine** except what it needs to reach the store and the local copy of the pictures
  (FR-025).

## Where the pieces are

| What | Where |
|---|---|
| The launch, its routing and retry | `MTM_Waitlist.Startup/Services/LaunchPipeline.cs` |
| The sequence, as data | `MTM_Waitlist.Startup/Services/LaunchStepCatalog.cs` |
| Running one step under its bound | `MTM_Waitlist.Startup/Services/LaunchStepRunner.cs` |
| The launch surface's lines | `MTM_Waitlist.Startup/Services/LaunchActivityFeed.cs` |
| The steps that run before sign-in | `ConfigurationStep.cs`, `StoreReachabilityStep.cs`, `MachineReadinessStep.cs` |
| The sign-in steps | `SignInSteps.cs` |
| This machine's configuration | `MachineConfigurationService.cs` |
| Diagnostics | `MTM_Waitlist.Logging/` |
| The surfaces | `Module_Startup/Views/` |
| Registrations for the module | `MTM_Waitlist.Startup/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs` |
