using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Services;

/// <summary>
/// Maps the active setup job's parts onto the picker's availability snapshot.
/// <para>
/// This lives in the application project on purpose: it is the composition root, the only place that may see
/// both <see cref="IActiveJobItemResolverService"/> (Module_Setup) and
/// <see cref="RequestJobPartAvailability"/> (Module_Settings), so nothing has to move between the modules and
/// neither module gains a reference to the other.
/// </para>
/// </summary>
public sealed class RequestJobAvailabilityProvider : IRequestJobPartAvailabilityProvider
{
    private readonly IActiveJobItemResolverService _activeJobItemResolver;

    public RequestJobAvailabilityProvider(IActiveJobItemResolverService activeJobItemResolver)
    {
        _activeJobItemResolver = activeJobItemResolver;
    }

    /// <inheritdoc />
    public async Task<RequestJobPartAvailability> GetAvailabilityAsync(string workCenter, CancellationToken cancellationToken = default)
    {
        var snapshot = await _activeJobItemResolver
            .ResolveAsync(workCenter ?? string.Empty, cancellationToken)
            .ConfigureAwait(false);

        return Map(snapshot);
    }

    /// <summary>Pure mapping, so the translation from a job snapshot to the snapshot flags is unit-testable.</summary>
    public static RequestJobPartAvailability Map(SetupActiveJobSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return RequestJobPartAvailability.None;
        }

        return new RequestJobPartAvailability(
            HasActiveJob: true,
            HasCoil: snapshot.Coils.Count > 0,
            HasFlatstock: snapshot.Flatstock.Count > 0,
            HasDie: snapshot.Dies.Count > 0,
            HasComponent: snapshot.Components.Count > 0,
            HasDunnage: snapshot.DunnageParts.Count > 0,
            HasScrapDecision: HasRealScrapDecision(snapshot))
            // The job's own component list travels with the snapshot: an Item whose configuration declares an
            // enumerated answer with no list of its own takes its choices from here (FR-035), which is how
            // `pickup-component`'s details step can be completed at all.
            .WithComponentPartNumbers(snapshot.Components.Select(component => component.PartNumber));
    }

    /// <summary>
    /// Whether any subordinate part records a <b>real</b> scrap decision. Evaluated through the shared rule, so
    /// a job that chose <c>No Scrap</c> — or still carries the <c>Scrap Type Required</c> placeholder — is not
    /// offered the Scrap Item (FR-031).
    /// </summary>
    private static bool HasRealScrapDecision(SetupActiveJobSnapshot snapshot) =>
        snapshot.SubordinateParts.Any(part => ScrapDecisionRules.HasRealScrapDecision(part.SelectedScrapType));
}
