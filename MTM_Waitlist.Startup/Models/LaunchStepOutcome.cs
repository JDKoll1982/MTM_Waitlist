namespace MTM_Waitlist.Module_Startup.Models;

/// <summary>
/// What one launch step reports back when it ends (`contracts/launch-step-contract.md` §2).
/// </summary>
/// <remarks>
/// <para>
/// <b>The diagnosis is specific to what stopped the launch.</b> <see cref="Diagnosis"/> says what happened in
/// plain language; a step number is never the whole message (FR-004). It is <c>null</c> when the step has
/// nothing to add — a step that simply did its work reports no diagnosis.
/// </para>
/// <para>
/// <b>The remedies are what the person may do about it.</b> They are carried with the outcome so the surface
/// never has to guess from the status alone (FR-016, FR-017).
/// </para>
/// </remarks>
/// <param name="Status">Whether the work finished, was left undone, or failed.</param>
/// <param name="Diagnosis">What happened, in plain language, or <c>null</c> when there is nothing to add.</param>
/// <param name="Remedies">The actions that could remove the cause.</param>
public sealed record LaunchStepOutcome(
    LaunchStepStatus Status,
    string? Diagnosis,
    LaunchRemedySet Remedies);
