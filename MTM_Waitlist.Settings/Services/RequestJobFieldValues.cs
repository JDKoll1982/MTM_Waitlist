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
            // The die's identifier as one value, so the row reads which die it is and where the die is. Composed
            // through the die's own rule, so a die with no known location cannot leave a dangling separator.
            "die" => Trimmed(RequestDiePart.ComposeLabel(job.DieNumber, job.DieLocation)),

            // Where the die currently is, which for a die request is the place the handler collects it from.
            "pickup location" => Trimmed(job.DieLocation),

            // The part the die is assigned to — the requesting job's own part number.
            "part number" or "job part number" or "part" => Trimmed(job.JobPartNumber),

            // Any other declared job label is a value this snapshot does not carry. It resolves to nothing, and
            // its row is omitted rather than drawn empty.
            _ => null,
        };
    }

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
