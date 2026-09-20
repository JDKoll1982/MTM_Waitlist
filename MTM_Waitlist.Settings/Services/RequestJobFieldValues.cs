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
/// The two die labels are <b>lists</b>: a job may carry several dies, so the row names every one of them rather
/// than the first, and the requester sees all the dies — and all the locations — the job actually has (FR-057).
/// </para>
/// </summary>
public static class RequestJobFieldValues
{
    /// <summary>
    /// The job's value for a declared job-sourced label, or <see langword="null" /> when the job carries nothing
    /// for it. The comparison is on the trimmed label so a configuration's own spacing cannot decide the result.
    /// </summary>
    public static string? Resolve(string? label, RequestJobPartAvailability? job)
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
            "pickup location" => job.Dies.Count > 0
                ? Join(job.Dies.Select(die => die.Location))
                : Trimmed(job.DieLocation),

            // The part the die is assigned to — the requesting job's own part number.
            "part number" or "job part number" or "part" => Trimmed(job.JobPartNumber),

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
