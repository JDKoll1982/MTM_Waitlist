using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Lets the New Request wizard offer a dunnage part that is <b>not</b> assigned to the requesting job, so an
/// operator who has to use a substitute can say which part they actually need from the material handlers
/// (FR-049).
/// <para>
/// Declared here, in Settings, beside the availability provider the wizard already consumes, and implemented in
/// the application project — the composition root — because opening the picker needs the Setup-side dunnage
/// catalogue and an app-owned dialog view. That is the same arrangement
/// <see cref="IRequestJobPartAvailabilityProvider"/> uses, and it keeps the wizard free of any reference to
/// <c>Module_Setup</c>.
/// </para>
/// </summary>
public interface IDunnageSubstitutePicker
{
    /// <summary>
    /// Shows the image-backed dunnage catalogue and returns the part the operator picked, or <c>null</c> when they
    /// dismissed it without choosing or when no picker could be shown.
    /// </summary>
    Task<RequestDunnagePart?> PickSubstituteAsync(CancellationToken cancellationToken = default);
}
