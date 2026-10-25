namespace MTM_Waitlist.Module_Settings.Services;

using MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Resolves the value of a declared detail field whose <c>source</c> is <c>job</c> (FR-052).
/// <para>
/// A declaration such as <c>{"label":"Die","source":"job"}</c> names a value the <b>requesting job</b> carries and
/// the request itself never stored, so the only thing that can supply it is the job snapshot. The lookup is by
/// the declared label, because the label <i>is</i> the declaration — the Item's row says <c>Die</c> or
/// <c>Pickup location</c>, and this maps that word onto the job value it means.
/// </para>
/// <para>
/// A label the job cannot supply resolves to nothing, and the row is then not drawn at all — never a blank
/// standing in for a value, never an invented one (FR-002, FR-026). That is what keeps a job with no die from
/// producing an empty "Die" row on a card.
/// </para>
/// <para>
/// One arm is a derivation rather than a carried value: <c>Item Type</c> / <c>Part Type</c> reads the disposition
/// the snapshot was handed by the resolver, so the card and the four returning pickups' visibility read the same
/// derivation instead of each running one (FR-091, FR-092, research R1).
/// </para>
/// <para>
/// The die and dunnage-location labels are <b>lists</b>: a job may carry several dies or several dunnage parts, so
/// the row names every one of them rather than the first, and the requester sees all the dies — and all the
/// locations — the job actually has (FR-057).
/// </para>
/// <para>
/// A value the requester captured under the same declared label outranks the job's own (FR-082), which is the
/// case the two wrong-part Items need for their incorrect part; see the three-argument overload.
/// </para>
/// </summary>
public static class RequestJobFieldValues
{
    /// <summary>
    /// The job's value for a declared job-sourced label, or <see langword="null" /> when the job carries nothing
    /// for it. The comparison is on the trimmed label so a configuration's own spacing cannot decide the result.
    /// </summary>
    public static string? Resolve(string? label, RequestJobPartAvailability? job) =>
        Resolve(label, job, answers: null);

    /// <summary>
    /// The request's value for a declared job-sourced label, preferring the answer the request captured under that
    /// same label and falling through to the job when it captured none (FR-082).
    /// <para>
    /// The answer wins because it is the request's own value: the incorrect part on the two wrong-part Items is
    /// typed by the requester, so the job must not answer over the top of it. The label comparison is the trimmed,
    /// case-insensitive one the job arms use, so neither the answer's nor the declaration's spacing decides the
    /// row, and an answer that is blank is no answer at all (FR-006).
    /// </para>
    /// </summary>
    /// <param name="label">The declared label the row reads.</param>
    /// <param name="job">The requesting job's snapshot, or <see langword="null" /> before one has arrived.</param>
    /// <param name="answers">The answers the request captured, keyed by the label each was declared under.</param>
    public static string? Resolve(
        string? label,
        RequestJobPartAvailability? job,
        IReadOnlyDictionary<string, string>? answers)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return null;
        }

        return Captured(label, answers) ?? ComponentValue(label, job, answers) ?? JobValue(label, job);
    }

    /// <summary>
    /// The two cells that describe the component a request is about: its description and the total quantity on
    /// hand. The component is the one the requester picked (the <c>Component</c> answer); a component the job
    /// does not carry facts for resolves to nothing, so the cell is left out.
    /// </summary>
    private static string? ComponentValue(
        string label,
        RequestJobPartAvailability? job,
        IReadOnlyDictionary<string, string>? answers)
    {
        if (job is null)
        {
            return null;
        }

        var declared = label.Trim().ToLowerInvariant();
        if (declared is not ("part description" or "total qty in house"))
        {
            return null;
        }

        var component = Captured("Component", answers);
        if (component is null || !job.ComponentFacts.TryGetValue(component, out var facts))
        {
            return null;
        }

        return declared == "part description" ? Trimmed(facts.Description) : Trimmed(facts.TotalQuantityOnHand);
    }

    /// <summary>
    /// The value the request captured under the declared label, or <see langword="null" /> when it captured none
    /// or captured only a blank.
    /// </summary>
    private static string? Captured(string label, IReadOnlyDictionary<string, string>? answers)
    {
        if (answers is null)
        {
            return null;
        }

        var declared = label.Trim().ToLowerInvariant();
        foreach (var answer in answers)
        {
            if (string.Equals(answer.Key.Trim().ToLowerInvariant(), declared, StringComparison.Ordinal)
                && Trimmed(answer.Value) is { } captured)
            {
                return captured;
            }
        }

        return null;
    }

    private static string? JobValue(string? label, RequestJobPartAvailability? job)
    {
        if (job is null || string.IsNullOrWhiteSpace(label))
        {
            return null;
        }

        return label.Trim().ToLowerInvariant() switch
        {
            // The dies the job carries, as one row: each written as its number and where it lives, so the
            // requester reads which dies the job has rather than only the first one (FR-057, FR-056). A job whose
            // die list is empty falls back to the single die the snapshot names.
            "die" => job.Dies.Count > 0
                ? Join(job.Dies.Select(die => RequestDiePart.ComposeLabel(die.PartNumber)))
                : Trimmed(RequestDiePart.ComposeLabel(job.DieNumber)),

            // Where the dies are, which for a die request is the place the handler collects them from. Every
            // location the job records is listed, and a job recording none yields nothing — a location the job
            // does not have is never back-filled from another die's row (FR-057).
            //
            // Both spellings answer: `Pickup location` is the label the die Items declare today, and `Die
            // Location` is the corrected label this feature moves the seed to (FR-059, D33, research R1). They
            // resolve to the same value, so the seed move cannot silently blank the die cell.
            "pickup location" or "die location" => job.Dies.Count > 0
                ? Join(job.Dies.Select(die => die.Location))
                : Trimmed(job.DieLocation),

            // The part the die is assigned to — the requesting job's own part number.
            "part number" or "job part number" or "part" => Trimmed(job.JobPartNumber),

            // The coil the job carries: the coil Items name it, and on the wrong-coil Item it is also the
            // correct coil, because the correct coil is the job's own rather than the one typed on the card
            // (FR-088). A job carrying no coil yields nothing, and the cell is left out.
            "coil number" or "correct coil" => Trimmed(job.CoilPartNumber),

            // The flatstock the job carries, which is the correct flatstock on the wrong-flatstock Item, read
            // exactly as the coil's is (FR-088).
            "correct flatstock" => Trimmed(job.FlatstockPartNumber),

            // The scrap decision the job has already recorded, so the Scrap Item's card reads the type the job
            // names rather than the Item's own display name (FR-030, FR-031, FR-088). Empty when no decision was
            // made or the decision was `No Scrap`.
            "scrap type" => Trimmed(job.ScrapType),

            // Two properties of the coil part the job already names. The description is carried by the coil
            // subordinate read, so it resolves from the snapshot rather than from a second read (FR-088). The
            // average weight is the receiving store's own average for the part — what the store has actually
            // weighed — never Visual's single `PART.WEIGHT` figure, and never a weighed figure for this coil, so
            // the assist coil turn's own `Weight of Coil` answer does not come from here either
            // (FR-030, FR-041, FR-054, FR-086).
            "coil description" => Trimmed(job.CoilDescription),
            "average coil weight" => Trimmed(job.CoilAverageWeight),

            // The kind of part the job makes, read from the plant's own PRODUCT_CODE and COMMODITY_CODE. This is
            // what the card's `Part Type` cell draws (redefined 2026-10-23): coil, flatstock, hardware, stamped
            // part and so on. It answers nothing when neither code named a kind, so the cell is left out rather
            // than filled with a guess (FR-089).
            "part type" => RequestPartTypeLabels.Of(job.PartType),

            // The item type of the component Items, which reads the job's product disposition — what state the part
            // the job is making is in. A disposition nothing has derived answers nothing, so the cell is left out
            // rather than filled with a guess (FR-089).
            "item type" => RequestDispositionLabels.Of(job.Disposition),

            // Where the job's dunnage lives: one entry per part, so the dunnage Items' row names every home
            // location the job records, in the job's own order. A job whose parts record none yields nothing —
            // a location the job does not have is never back-filled from another part's row (FR-057, FR-058,
            // FR-089).
            "home location" => Join(job.DunnageParts.Select(part => part.HomeLocation)),

            // Where the job's scrap is dumped: the lugger the arrangement named for the job's own scrap type,
            // resolved when the list loaded (FR-022, FR-023). A type the arrangement has no lugger for, and a job
            // with no real scrap decision, answer nothing — the cell is then left out rather than filled with a
            // dash, a blank or a guess (FR-025, FR-026).
            "lugger location" => Trimmed(job.LuggerLocation),

            // Any other declared job label is a value this snapshot does not carry. It resolves to nothing, and
            // its row is omitted rather than drawn empty.
            _ => null,
        };
    }

    /// <summary>
    /// The values as one readable list, blanks dropped so a value the job does not record is omitted rather than
    /// drawn as a gap. <see langword="null" /> when nothing was left to join.
    /// </summary>
    private static string? Join(IEnumerable<string?> values)
    {
        var present = values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()).ToArray();
        return present.Length == 0 ? null : string.Join(", ", present);
    }

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
