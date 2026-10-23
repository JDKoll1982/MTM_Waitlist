using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.Services;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Services;

/// <summary>
/// Maps the active setup job's parts onto the picker's availability snapshot.
/// <para>
/// This lives in the application project on purpose: it is the composition root, the only place that may see
/// both <see cref="IActiveJobItemResolverService"/> (Module_Setup) and
/// <see cref="RequestJobPartAvailability"/> (Module_Settings), so nothing has to move between the modules and
/// neither module gains a reference to the other.
/// </para>
/// <para>
/// It is also the first consumer of <see cref="RequestDispositionResolver"/> (research R1, FR-091/FR-092/FR-094).
/// The four returning pickups are offered on the work the job has, so the derivation has to run before the
/// snapshot is handed to the picker; until it does, the four gates stay false and the Items stay hidden.
/// </para>
/// </summary>
public sealed class RequestJobAvailabilityProvider : IRequestJobPartAvailabilityProvider
{
    private readonly IActiveJobItemResolverService _activeJobItemResolver;
    private readonly RequestDispositionResolver _dispositionResolver;
    private readonly IAverageCoilWeightService _averageCoilWeightService;
    private readonly ILuggerArrangementService _luggerArrangement;
    private readonly IPartStockService? _partStockService;

    public RequestJobAvailabilityProvider(
        IActiveJobItemResolverService activeJobItemResolver,
        RequestDispositionResolver dispositionResolver,
        IAverageCoilWeightService averageCoilWeightService,
        ILuggerArrangementService luggerArrangement,
        IPartStockService? partStockService = null)
    {
        ArgumentNullException.ThrowIfNull(activeJobItemResolver);
        ArgumentNullException.ThrowIfNull(dispositionResolver);
        ArgumentNullException.ThrowIfNull(averageCoilWeightService);
        ArgumentNullException.ThrowIfNull(luggerArrangement);

        _activeJobItemResolver = activeJobItemResolver;
        _dispositionResolver = dispositionResolver;
        _averageCoilWeightService = averageCoilWeightService;
        _luggerArrangement = luggerArrangement;
        _partStockService = partStockService;
    }

    /// <inheritdoc />
    public async Task<RequestJobPartAvailability> GetAvailabilityAsync(string workCenter, CancellationToken cancellationToken = default)
    {
        var snapshot = await _activeJobItemResolver
            .ResolveAsync(workCenter ?? string.Empty, cancellationToken)
            .ConfigureAwait(false);

        var availability = Map(snapshot);
        if (snapshot is null)
        {
            return availability;
        }

        availability = await ApplyDispositionAsync(availability, snapshot, workCenter, cancellationToken).ConfigureAwait(false);
        availability = await ApplyComponentQuantitiesAsync(availability, cancellationToken).ConfigureAwait(false);
        availability = await ApplyCoilAverageWeightAsync(availability, cancellationToken).ConfigureAwait(false);
        return await ApplyLuggerLocationAsync(availability, workCenter, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Fills each component's total quantity on hand from the system that owns the part (the WIP Application when
    /// its floor holds the part, Infor Visual otherwise). A component whose stock cannot be read keeps an empty
    /// quantity, so its cell is left out rather than showing a guess.
    /// </summary>
    private async Task<RequestJobPartAvailability> ApplyComponentQuantitiesAsync(
        RequestJobPartAvailability availability,
        CancellationToken cancellationToken)
    {
        if (_partStockService is null || availability.ComponentFacts.Count == 0)
        {
            return availability;
        }

        try
        {
            var reads = availability.ComponentFacts.Select(async pair =>
            {
                var stock = await _partStockService.GetStockAsync(pair.Key, cancellationToken).ConfigureAwait(false);
                if (!stock.WasRead)
                {
                    return pair;
                }

                var quantity = stock.TotalQuantity.ToString("#,0.##", System.Globalization.CultureInfo.CurrentCulture);
                return new KeyValuePair<string, RequestComponentFacts>(pair.Key, pair.Value with { TotalQuantityOnHand = quantity });
            });

            return availability.WithComponentFacts(await Task.WhenAll(reads).ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Error("WaitlistNewRequest", ex, "The components' quantities on hand could not be read, so their quantity cells are left out.");
            return availability;
        }
    }

    /// <summary>
    /// Resolves the coil's <c>Average coil weight</c> from the <b>receiving store</b>, not from Visual.
    /// <para>
    /// Visual holds a single <c>PART.WEIGHT</c> figure per part, which is not the average the card is required to
    /// show: the card shows what the receiving store has actually weighed, pooled over every receiving-history row
    /// for the coil's part, through <c>sp_receiving_history_average_coil_weight</c> — the same live read the coil
    /// turn page already uses, so the two surfaces cannot disagree (FR-030, FR-041, FR-054). Nothing is
    /// substituted when the part has no history: the read returns no row and the property stays empty, so the
    /// cell is left out rather than filled with a guess (FR-089).
    /// </para>
    /// <para>
    /// A receiving store that cannot be read leaves the value empty and records the failure, so the rest of the
    /// snapshot still reaches the card and the missing cell is visible as a gap rather than as a wrong number
    /// (FR-001, FR-089).
    /// </para>
    /// </summary>
    private async Task<RequestJobPartAvailability> ApplyCoilAverageWeightAsync(
        RequestJobPartAvailability availability,
        CancellationToken cancellationToken)
    {
        if (availability.CoilPartNumber.Length == 0)
        {
            return availability;
        }

        try
        {
            var averageWeight = await _averageCoilWeightService
                .ResolveAverageCoilWeightTextAsync(availability.CoilPartNumber, cancellationToken)
                .ConfigureAwait(false);

            return availability with { CoilAverageWeight = averageWeight };
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "WaitlistNewRequest",
                ex,
                $"The average coil weight for coil '{availability.CoilPartNumber}' could not be read from the receiving store, so the card's `Average coil weight` cell is left empty.");
            return availability;
        }
    }

    /// <summary>
    /// Names the lugger the job's scrap is dumped into, from the arrangement as it stands now.
    /// <para>
    /// The arrangement is read through its own seam, once for the availability this call builds, so a card shows
    /// the lugger named when the list loaded rather than anything the request stored; a status change or a rename
    /// therefore shows on the next load (FR-023). The job's real scrap type is the key: it is matched ignoring
    /// letter case and surrounding spaces, and nothing else (FR-009, FR-022).
    /// </para>
    /// <para>
    /// Two absences are not the same. A job with no real scrap decision is the designed absence and resolves
    /// nothing, so the cell is simply left out (FR-025). A real scrap type with no lugger assigned is a
    /// configuration gap: it is recorded once through <see cref="AppLog.Warn"/>, naming the scrap type and the work
    /// centre so the arrangement can be corrected, and the value stays null so no substitute reaches the card
    /// (FR-026). An arrangement that could not be read at all arrives as an empty snapshot, which is an outage
    /// rather than a gap — the list keeps working with no value and no warning is raised about configuration that
    /// was never seen.
    /// </para>
    /// </summary>
    private async Task<RequestJobPartAvailability> ApplyLuggerLocationAsync(
        RequestJobPartAvailability availability,
        string? workCenter,
        CancellationToken cancellationToken)
    {
        if (availability.ScrapType.Length == 0)
        {
            return availability;
        }

        LuggerArrangementSnapshot arrangement;
        try
        {
            arrangement = await _luggerArrangement.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Waitlist",
                ex,
                $"The scrap lugger arrangement could not be read, so the `Lugger Location` cell for scrap type '{availability.ScrapType}' at work center '{workCenter}' is left out.");
            return availability;
        }

        var resolution = arrangement.Resolve(availability.ScrapType);

        if (resolution.IsConfigurationGap && arrangement.Rows.Count > 0)
        {
            AppLog.Warn(
                "Waitlist",
                $"The scrap type '{availability.ScrapType}' has no lugger assigned, so the `Lugger Location` cell at work center '{workCenter}' is left out. Assign the type to a lugger in the lugger arrangement so handlers know where to walk.");
        }

        return resolution.LuggerLocation is null
            ? availability
            : availability with { LuggerLocation = resolution.LuggerLocation };
    }

    /// <summary>
    /// Writes the job part's derivation onto the snapshot: the single item type the card's <c>Item Type</c> /
    /// <c>Part Type</c> cell reads, and the four returning pickups' gates.
    /// <para>
    /// The gates come from the derivation rather than from a second reading of the same signals, so a job is
    /// offered exactly the pickups whose item type the derivation can name for it (FR-094). A read that fails
    /// inside the resolver degrades there to <see cref="RequestDisposition.Unknown"/>, and a failure that escapes
    /// it is caught here and leaves the mapped snapshot untouched: either way all four gates stay false, the four
    /// Items stay hidden, and nothing is invented in their place (FR-089).
    /// </para>
    /// </summary>
    private async Task<RequestJobPartAvailability> ApplyDispositionAsync(
        RequestJobPartAvailability availability,
        SetupActiveJobSnapshot snapshot,
        string? workCenter,
        CancellationToken cancellationToken)
    {
        RequestDisposition disposition;
        try
        {
            disposition = await _dispositionResolver
                .GetDispositionAsync(snapshot.WorkOrder, snapshot.PartNumber, workCenter, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The derivation needs both the plant system and the floor store, and neither may take the snapshot
            // down with it: without it the part-kind gates are exactly what they were before, the four returning
            // pickups stay hidden, and the failure is recorded rather than turned into a value nothing supports
            // (FR-089). The caller's own catch is the last resort, not the first.
            AppLog.Error(
                "WaitlistNewRequest",
                ex,
                $"The disposition for job '{snapshot.WorkOrder}' part '{snapshot.PartNumber}' could not be derived, so the returning pickups stay hidden and no item type is reported.");
            return availability;
        }

        return availability.WithDisposition(disposition);
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
            // Each component's description comes from the same subordinate read; its quantity on hand is filled in
            // afterwards by the provider, because it is read from the system that owns the part.
            .WithComponentFacts(snapshot.Components
                .Where(component => !string.IsNullOrWhiteSpace(component.PartNumber))
                .Select(component => new KeyValuePair<string, RequestComponentFacts>(
                    component.PartNumber,
                    new RequestComponentFacts(component.Description?.Trim() ?? string.Empty, string.Empty))))
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
            // The job's own work order travels too: it is the only place a request can learn it, and the details
            // page's Work order row reads it from here rather than from the request's stored job id — until
            // 2026-10-16 the wizard wrote the work centre into that column, so a row reading it printed the press
            // where a work order belongs (FR-053).
            WorkOrder = (snapshot.WorkOrder ?? string.Empty).Trim(),
            DieNumber = dies.FirstOrDefault()?.PartNumber ?? string.Empty,
            DieLocation = dies.FirstOrDefault()?.Location ?? string.Empty,
            // The materials the job holds travel too. An Item whose identifier names the part it is about reads
            // the coil or flatstock the job actually carries, so the card names a real part instead of repeating
            // the Item's own name (FR-005).
            CoilPartNumber = snapshot.Coils.FirstOrDefault()?.PartNumber ?? string.Empty,
            // The coil's own description travels with the snapshot so the coil Items' `Coil description` cell
            // resolves from it instead of from a second read (FR-088). A coil the job does not carry leaves it
            // empty rather than substituting a value (FR-089). The coil's `Average coil weight` is deliberately
            // NOT mapped here: it comes from the receiving store's own weighing history in
            // `ApplyCoilAverageWeightAsync` below, never from Visual (FR-030, FR-041, FR-054).
            CoilDescription = snapshot.Coils.FirstOrDefault()?.Description?.Trim() ?? string.Empty,
            FlatstockPartNumber = snapshot.Flatstock.FirstOrDefault()?.PartNumber ?? string.Empty,
            // The scrap type the job already decided, which is what the Scrap Item's card shows (FR-030).
            ScrapType = RealScrapType(snapshot),
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
        // The home location the receiving read returned for the part travels with it, so the card's `Home Location`
        // cell has a value on the snapshot rather than only in the store (FR-033, FR-045, FR-089).
        HomeLocation = part.HomeLocation ?? string.Empty,
        ImagePath = DunnageImagePathResolver.GetDisplayPath(part.ImagePath) ?? string.Empty,
        IsAssignedToJob = true,
    };

    /// <summary>
    /// Whether any subordinate part records a <b>real</b> scrap decision. Evaluated through the shared rule, so
    /// a job that chose <c>No Scrap</c> — or still carries the <c>Scrap Type Required</c> placeholder — is not
    /// offered the Scrap Item (FR-031).
    /// </summary>
    private static bool HasRealScrapDecision(SetupActiveJobSnapshot snapshot) => RealScrapType(snapshot).Length > 0;

    /// <summary>
    /// The scrap type the job's first <b>real</b> scrap decision names, trimmed, or empty when no subordinate part
    /// records one. The placeholder and <c>No Scrap</c> are not decisions, so neither is ever carried — which is
    /// what keeps both the Scrap Item's visibility and its card reading the same rule (FR-031).
    /// </summary>
    private static string RealScrapType(SetupActiveJobSnapshot snapshot) =>
        snapshot.SubordinateParts
            .Select(part => part.SelectedScrapType)
            .FirstOrDefault(scrapType => ScrapDecisionRules.HasRealScrapDecision(scrapType))
            ?.Trim() ?? string.Empty;
}
