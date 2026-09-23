namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Which active-job part (if any) an auto-populated canonical Item depends on for
/// visibility in the New Request picker / card render. The per-Item visibility rules are recorded in
/// specs/004-unified-card-item-picker/contracts/request-picker-flow.md §4.
/// </summary>
public enum RequestJobPartKind
{
    /// <summary>No job part required — always available (manual equipment/consumable or free text).</summary>
    None,

    /// <summary>Visible only when the requesting job has a coil (MMC / Coil subordinate).</summary>
    Coil,

    /// <summary>
    /// Visible when the requesting job has a coil <b>or</b> flatstock. This is the merged Pickup Item
    /// (<c>pickup-coil</c>), whose one request covers either material (FR-002, D21): a flatstock-only job that
    /// withheld it would silently lose a supported Item, which is what SC-004 measures.
    /// </summary>
    CoilOrFlatstock,

    /// <summary>Visible only when the requesting job has flatstock (MMF / Flatstock subordinate).</summary>
    Flatstock,

    /// <summary>Visible only when the requesting job has a die (FGT / Die subordinate).</summary>
    Die,

    /// <summary>Visible only when the requesting job has a component (non-coil/flatstock subordinate).</summary>
    Component,

    /// <summary>Visible only when the requesting job has an assigned dunnage part.</summary>
    Dunnage,

    /// <summary>Visible only when the requesting job has any subordinate part (used by Assist table place/remove).</summary>
    AnySubordinate,

    /// <summary>
    /// Visible only when the requesting job has a <b>real scrap decision</b> — a scrap value that is set, is
    /// not <c>No Scrap</c> and is not the <c>Scrap Type Required</c> placeholder (FR-031, contract §5).
    /// Evaluated through the canonical predicate in <c>MTM_Waitlist.Core</c> so Setup and New Request
    /// cannot diverge.
    /// </summary>
    Scrap,

    /// <summary>Visible only when the requesting job has a work order/sequence (FG / WIP / Outside, NCM defect).</summary>
    RequiresActiveJob,
}

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
    public static RequestJobPartAvailability All { get; } = new(true, true, true, true, true, true, true);

    public bool HasAnySubordinate => HasCoil || HasFlatstock || HasDie || HasComponent || HasDunnage;

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
