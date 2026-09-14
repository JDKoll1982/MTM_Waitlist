using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.Services;

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

        // A die row is not always a die. The subordinate-parts query gives a job with no die a die row described
        // `No Die`, so counting rows would offer a die request for a job that has none and would put an empty
        // location on the card. The snapshot's own RealDies applies the shared rule (FR-055), so the wizard and
        // the Setup screens cannot disagree about what a die is.
        var dies = snapshot.RealDies
            .Select(MapDie)
            .ToArray();

        return new RequestJobPartAvailability(
            HasActiveJob: true,
            HasCoil: snapshot.Coils.Count > 0,
            HasFlatstock: snapshot.Flatstock.Count > 0,
            HasDie: dies.Length > 0,
            HasComponent: snapshot.Components.Count > 0,
            HasDunnage: snapshot.DunnageParts.Count > 0,
            HasScrapDecision: HasRealScrapDecision(snapshot))
            // The job's own component list travels with the snapshot: an Item whose configuration declares an
            // enumerated answer with no list of its own takes its choices from here (FR-035), which is how
            // `pickup-component`'s details step can be completed at all.
            .WithComponentPartNumbers(snapshot.Components.Select(component => component.PartNumber))
            // The job's assigned dunnage parts travel the same way: they are the dunnage step's cards, and the
            // list an Item naming the `dunnage` list draws its choices from (FR-035, FR-048).
            .WithDunnageParts(snapshot.DunnageParts.Select(MapDunnagePart))
            // The job's dies travel too: they are the die step's cards, and each one the operator picks becomes a
            // request of its own (FR-054).
            .WithDies(dies) with
        {
            // The job's identity travels with the snapshot so an Item whose template names it can show it
            // (FR-053). A job with more than one die is represented by its first die for the single-die tokens;
            // the die step itself offers every one of them.
            JobPartNumber = snapshot.PartNumber ?? string.Empty,
            DieNumber = dies.FirstOrDefault()?.PartNumber ?? string.Empty,
            DieLocation = dies.FirstOrDefault()?.Location ?? string.Empty,
        };
    }

    /// <summary>
    /// Maps one of the job's dies onto the wizard's value. Its two-part label is composed by the value itself, so
    /// a die whose location is unknown renders as the die's number alone instead of leaving a dangling separator
    /// (FR-056).
    /// </summary>
    private static RequestDiePart MapDie(SetupSubordinatePart die) => new()
    {
        PartNumber = die.PartNumber?.Trim() ?? string.Empty,
        Location = die.Location?.Trim() ?? string.Empty,
        Description = die.Description?.Trim() ?? string.Empty,
    };

    /// <summary>
    /// Maps one of the job's assigned dunnage parts onto the wizard's value, resolving its picture to an absolute
    /// path <b>here</b> — this is the only place that may see both the Setup-side part and the Settings-side value,
    /// so the wizard never has to know where the shared dunnage image root lives.
    /// </summary>
    private static RequestDunnagePart MapDunnagePart(SetupDunnagePart part) => new()
    {
        Id = part.Id ?? string.Empty,
        TypeId = part.TypeId ?? string.Empty,
        PartNumber = part.PartNumber ?? string.Empty,
        DisplayName = string.IsNullOrWhiteSpace(part.DisplayName) ? part.PartNumber ?? string.Empty : part.DisplayName,
        ImagePath = DunnageImagePathResolver.GetDisplayPath(part.ImagePath) ?? string.Empty,
        IsAssignedToJob = true,
    };

    /// <summary>
    /// Whether any subordinate part records a <b>real</b> scrap decision. Evaluated through the shared rule, so
    /// a job that chose <c>No Scrap</c> — or still carries the <c>Scrap Type Required</c> placeholder — is not
    /// offered the Scrap Item (FR-031).
    /// </summary>
    private static bool HasRealScrapDecision(SetupActiveJobSnapshot snapshot) =>
        snapshot.SubordinateParts.Any(part => ScrapDecisionRules.HasRealScrapDecision(part.SelectedScrapType));
}
