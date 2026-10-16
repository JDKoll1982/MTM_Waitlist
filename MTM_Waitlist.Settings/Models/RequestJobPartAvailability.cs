namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// What parts the requesting work center's active setup job actually has. Drives the
/// conditional-visibility rule for auto-populated canonical Items. Pure value snapshot so
/// picker rules stay testable and DB-free; a caller (Phase 3) maps an
/// <c>IActiveJobItemResolverService</c> snapshot onto these flags.
/// </summary>
public sealed record RequestJobPartAvailability(
    bool HasActiveJob,
    bool HasCoil,
    bool HasFlatstock,
    bool HasDie,
    bool HasComponent,
    bool HasDunnage,
    bool HasScrapDecision)
{
    /// <summary>An empty job (nothing present) — used for offline "no active job" rendering.</summary>
    public static RequestJobPartAvailability None { get; } = new(false, false, false, false, false, false, false);

    /// <summary>A job with every part present — renders all auto-populated items.</summary>
    public static RequestJobPartAvailability All { get; } = new(true, true, true, true, true, true, true)
    {
        HasFinishedGoodsToReturn = true,
        HasNonconformingStockToReturn = true,
        HasWorkInProgressToReturn = true,
        HasOutsideServiceWorkToReturn = true,
    };

    public bool HasAnySubordinate => HasCoil || HasFlatstock || HasDie || HasComponent || HasDunnage;

    /// <summary>
    /// Whether the requesting job has <b>finished goods</b> to pick up — stock completed at a finished-goods
    /// location. This is the work the <c>pickup-fg</c> Item names, so the Item is offered on the job's real work
    /// rather than hidden by a fixed list (FR-094).
    /// </summary>
    public bool HasFinishedGoodsToReturn { get; init; }

    /// <summary>
    /// Whether the requesting job has <b>non-conforming stock</b> to pick up — material sitting at an NCM
    /// location. This is the work the <c>pickup-ncm</c> Item names, and it is a signal of the location the stock
    /// occupies, never of the part's recorded item type (FR-093, FR-094).
    /// </summary>
    public bool HasNonconformingStockToReturn { get; init; }

    /// <summary>
    /// Whether the requesting job has <b>work in progress</b> to pick up — quantity still open on the work order.
    /// This is the work the <c>pickup-wip</c> Item names (FR-094).
    /// </summary>
    public bool HasWorkInProgressToReturn { get; init; }

    /// <summary>
    /// Whether the requesting job has <b>subcontracted work</b> to pick up — an operation an outside vendor
    /// performs. This is the work the <c>pickup-outside-service</c> Item names (FR-094).
    /// </summary>
    public bool HasOutsideServiceWorkToReturn { get; init; }

    /// <summary>
    /// The requesting job's own work order, as <c>WO-######</c>, or empty when no job is known.
    /// <para>
    /// It travels with the snapshot because the job is the only thing that knows it: a request stores the work
    /// centre it was raised at and the job that was active there, and nothing else on the request carries the
    /// work order itself. A caller that cannot supply one leaves it empty, and the surfaces that show it omit
    /// the row rather than printing something else in its place (FR-002).
    /// </para>
    /// </summary>
    public string WorkOrder { get; init; } = string.Empty;

    /// <summary>
    /// The item type the requesting job's part is derived to be — the single derivation behind `Item Type` on
    /// the component Items and `Part Type` on the four returning pickups (FR-091, FR-092).
    /// <para>
    /// It is produced by <c>RequestDispositionResolver</c> and travels with the snapshot so the card reads a
    /// value it was handed rather than running a second derivation of its own (research R1). It is
    /// <see cref="RequestDisposition.Unknown"/> when nothing has been derived — a snapshot built by a caller
    /// that cannot reach the derivation — and the cell is then left out rather than filled with a guess
    /// (FR-089).
    /// </para>
    /// </summary>
    public RequestDisposition Disposition { get; init; } = RequestDisposition.Unknown;

    /// <summary>
    /// The requesting job's component part numbers, in the job's own order (FR-035).
    /// <para>
    /// This is the list an Item whose configuration declares an enumerated answer <b>with no list of its own</b>
    /// draws its choices from — the job-derived path, keyed on the row's declarations and never on the Item's
    /// identity (FR-013). Empty when the job carries no component.
    /// </para>
    /// </summary>
    public IReadOnlyList<string> ComponentPartNumbers { get; init; } = Array.Empty<string>();

    /// <summary>
    /// The number of the coil the requesting job carries (its <c>MMC</c> subordinate), or empty when it has none.
    /// <para>
    /// This is the part a card's identifier names for a request that is <b>about</b> a material the job holds — the
    /// coil Items, the wrong-coil Item and the coil-turn assist — so the second line reads a real part number
    /// instead of falling back to the Item's own display name (FR-005).
    /// </para>
    /// </summary>
    public string CoilPartNumber { get; init; } = string.Empty;

    /// <summary>
    /// The number of the flatstock the requesting job carries (its <c>MMF</c> subordinate), or empty when it has
    /// none. It is the part the flatstock Items' identifier names, read exactly as the coil's is.
    /// </summary>
    public string FlatstockPartNumber { get; init; } = string.Empty;

    /// <summary>
    /// The description the plant records for the coil the job carries, or empty when the job has no coil.
    /// <para>
    /// It is the coil subordinate read's own <c>Description</c> output (FR-088), so `Coil description` resolves
    /// from the snapshot rather than from a second read. Nothing is substituted when it is empty: the cell is
    /// left out instead of being filled with the coil's number or the Item's name (FR-089).
    /// </para>
    /// </summary>
    public string CoilDescription { get; init; } = string.Empty;

    /// <summary>
    /// The average the <b>receiving store</b> holds for the coil's part: <c>receiving_history.quantity</c> averaged
    /// over every row for that part through <c>sp_receiving_history_average_coil_weight</c>, rendered whole pounds
    /// with a unit suffix (FR-030, FR-041). It is what the store has actually weighed, deliberately not Visual's
    /// single <c>PART.WEIGHT</c> figure for the part, and not a weighed figure for this coil either — so the assist
    /// coil turn's `Weight of Coil` answer (<c>input_value</c>) is never read from here (FR-054, FR-086). Empty
    /// when the job has no coil or the store holds no weighted history for the part, and never substituted with a
    /// guess (FR-089).
    /// </summary>
    public string CoilAverageWeight { get; init; } = string.Empty;

    /// <summary>
    /// The dunnage parts assigned to the requesting job, in the job's own order (FR-035).
    /// <para>
    /// This is the list the dunnage step shows as picture cards, and the list an Item whose configuration names
    /// the <c>dunnage</c> list draws its choices from. Empty when the job carries no dunnage — which is also the
    /// condition that keeps the dunnage Items out of the picker entirely.
    /// </para>
    /// </summary>
    public IReadOnlyList<RequestDunnagePart> DunnageParts { get; init; } = Array.Empty<RequestDunnagePart>();

    /// <summary>
    /// The requesting job's own part number — the part its parts are subordinate to, and the part a die is
    /// assigned to (FR-053). It is what the die Items' first line names, so a die request reads which part it is
    /// for rather than only which die it is.
    /// </summary>
    public string JobPartNumber { get; init; } = string.Empty;

    /// <summary>The number of the job's primary die (its <c>FGT</c> part number), or empty when it has no die.</summary>
    public string DieNumber { get; init; } = string.Empty;

    /// <summary>The primary die's location, or empty. Read from the die subordinate row's <c>Location</c> (FR-053).</summary>
    public string DieLocation { get; init; } = string.Empty;

    /// <summary>
    /// The scrap type the requesting job's <b>real</b> scrap decision names, or empty when no decision was made or
    /// the decision was <c>No Scrap</c>. The Scrap Item's card shows this, so its line reads the type the job has
    /// already recorded rather than the Item's own display name (FR-030, FR-031).
    /// </summary>
    public string ScrapType { get; init; } = string.Empty;

    /// <summary>
    /// Every die assigned to the requesting job, in the job's own order (FR-054).
    /// <para>
    /// This is the list the die step shows as selectable cards, and the list an Item whose configuration names
    /// the <c>die</c> list draws its choices from. A job whose only die row is the <c>No Die</c> placeholder has
    /// an <b>empty</b> list — the placeholder is how Setup records "this job has no die", so it is not a die
    /// (FR-055).
    /// </para>
    /// </summary>
    public IReadOnlyList<RequestDiePart> Dies { get; init; } = Array.Empty<RequestDiePart>();

    /// <summary>
    /// A copy of this snapshot carrying the job's dies, with rows that carry no die number dropped so a caller can
    /// never offer an empty choice.
    /// </summary>
    public RequestJobPartAvailability WithDies(IEnumerable<RequestDiePart>? dies) => this with
    {
        Dies = dies is null
            ? Array.Empty<RequestDiePart>()
            : dies
                .Where(die => die is not null && !string.IsNullOrWhiteSpace(die.PartNumber))
                .ToArray(),
    };

    /// <summary>
    /// A copy of this snapshot carrying the part's <b>derived</b> item type: the value the card's
    /// <c>Item Type</c> / <c>Part Type</c> cell reads, together with the four returning pickups' gates, which the
    /// same derivation decides (FR-091, FR-094).
    /// <para>
    /// Exactly one gate is ever set, because the gates are read from one derivation rather than from four
    /// separate probes — so the pickups a job is offered are exactly the ones whose item type the derivation can
    /// name for it. <see cref="RequestDisposition.Unknown"/> means nothing was derived (no job, no part, or a read
    /// that failed) and sets none of them, which keeps the four hidden rather than inventing an item type for
    /// them (FR-089).
    /// </para>
    /// </summary>
    public RequestJobPartAvailability WithDisposition(RequestDisposition disposition) => this with
    {
        Disposition = disposition,
        HasFinishedGoodsToReturn = disposition == RequestDisposition.FinishedGoods,
        HasNonconformingStockToReturn = disposition == RequestDisposition.Ncm,
        HasWorkInProgressToReturn = disposition == RequestDisposition.WorkInProcess,
        HasOutsideServiceWorkToReturn = disposition == RequestDisposition.OutsideService,
    };

    /// <summary>
    /// A copy of this snapshot carrying the job's component part numbers, trimmed and with blanks dropped so a
    /// caller can never offer an empty choice.
    /// </summary>
    public RequestJobPartAvailability WithComponentPartNumbers(IEnumerable<string>? partNumbers) => this with
    {
        ComponentPartNumbers = partNumbers is null
            ? Array.Empty<string>()
            : partNumbers
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part.Trim())
                .ToArray(),
    };

    /// <summary>
    /// A copy of this snapshot carrying the job's assigned dunnage parts, with parts that carry no part number
    /// dropped so a caller can never offer an empty choice.
    /// </summary>
    public RequestJobPartAvailability WithDunnageParts(IEnumerable<RequestDunnagePart>? parts) => this with
    {
        DunnageParts = parts is null
            ? Array.Empty<RequestDunnagePart>()
            : parts
                .Where(part => part is not null && !string.IsNullOrWhiteSpace(part.PartNumber))
                .ToArray(),
    };
}
