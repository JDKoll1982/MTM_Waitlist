using System.Globalization;
using System.Text;

namespace MTM_Waitlist.Module_Logging;

/// <summary>
/// The panel's only export: the entries it is showing, as plain text a developer can paste to someone who has no
/// access to the store (FR-038, SC-017; `contracts/logging-contract.md` §7.1).
/// </summary>
/// <remarks>
/// <para>
/// <b>The copy is the evidence, not a summary of it.</b> This type re-formats nothing and redacts nothing beyond
/// what §6 already forbade the store to hold. The exception chain and the payload are written exactly as they
/// were read, the chain link is written with them, and each entry names the filter the copy came from. A support
/// reader has to be able to see what the application saw, so anything this formatter decided to paraphrase would
/// be a fact it removed from the record.
/// </para>
/// <para>
/// <b>One formatter serves both affordances.</b> The entry card copies one entry and the panel header copies the
/// entries currently listed, which the query has already bounded by its page size. Both go through
/// <see cref="Format"/>, so the two copies cannot disagree about what an entry looks like.
/// </para>
/// <para>
/// <b>The column order lives here, once.</b> The panel draws the same order, so a column that appears on the
/// screen cannot go missing from a pasted copy.
/// </para>
/// <para>
/// <b>Nothing is written to the machine.</b> A copy is a read: it is not itself recorded, it changes no row, and
/// no file is created anywhere. The clipboard call is the caller's, because a formatter has no business holding a
/// clipboard (FR-038).
/// </para>
/// </remarks>
public static class LogExportFormatter
{
    /// <summary>
    /// The ceiling on one copy, in characters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A megabyte of text. That is the point past which a clipboard payload stops being something a person pastes
    /// into a message or a work item and starts being a file transfer, and it is high enough that a page of
    /// ordinary entries is never cut. The ceiling exists for the pathological entry, not the ordinary one: a
    /// single fault whose stack and payload are enormous must not make a copy unbounded.
    /// </para>
    /// <para>
    /// The value is fixed here and recorded in `contracts/logging-contract.md` §7, so the cut is verifiable
    /// against a number rather than against a phrase.
    /// </para>
    /// </remarks>
    public const int CeilingCharacters = 1_048_576;

    /// <summary>The line written in place of whatever the ceiling cut away.</summary>
    /// <remarks>
    /// It is a line of its own rather than a trailing character, so a reader who sees it knows the copy is a
    /// prefix and not a whole. A silent cut would let a truncated history read as a complete one, which is the
    /// one failure this whole feature exists to prevent (SC-003).
    /// </remarks>
    public static readonly string CutMarker =
        string.Format(
            CultureInfo.InvariantCulture,
            "--- CUT: this copy reached its {0} character ceiling and the rest was not included ---",
            CeilingCharacters);

    /// <summary>The line that separates one entry from the next.</summary>
    public const string EntrySeparator = "----------------------------------------";

    /// <summary>
    /// Formats the entries as one text a reader can paste.
    /// </summary>
    /// <param name="filterDescription">The filter the copy came from, in the reader's words.</param>
    /// <param name="entries">The entries to write, in the order the panel lists them.</param>
    /// <returns>
    /// The text, never longer than <see cref="CeilingCharacters"/> characters and cut with
    /// <see cref="CutMarker"/> when it would have been.
    /// </returns>
    /// <remarks>
    /// A blank filter description is written as "no filter", because a copy that said nothing about where it came
    /// from would leave a reader unable to tell a filtered sample from a whole history.
    /// </remarks>
    public static string Format(string? filterDescription, IReadOnlyList<LogPanelEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var text = new StringBuilder();

        text.AppendLine("MTM Waitlist developer log copy");
        text.Append("Filter: ")
            .AppendLine(string.IsNullOrWhiteSpace(filterDescription) ? "no filter" : filterDescription);
        text.Append("Entries: ")
            .AppendLine(entries.Count.ToString(CultureInfo.InvariantCulture));

        for (var index = 0; index < entries.Count; index++)
        {
            text.AppendLine(EntrySeparator);
            text.Append("Entry ")
                .Append((index + 1).ToString(CultureInfo.InvariantCulture))
                .Append(" of ")
                .AppendLine(entries.Count.ToString(CultureInfo.InvariantCulture));

            Append(text, entries[index]);
        }

        text.AppendLine(EntrySeparator);

        return Bound(text.ToString());
    }

    /// <summary>
    /// Writes one entry's columns, in the order the panel shows them.
    /// </summary>
    /// <remarks>
    /// Every column is written even when it holds nothing, and a column that holds nothing is written as the word
    /// "none". A line that is simply absent would leave a reader unable to tell "this entry has no payload" from
    /// "this entry's payload was left out of the copy".
    /// </remarks>
    private static void Append(StringBuilder text, LogPanelEntry entry)
    {
        Line(text, "Created (UTC)", entry.CreatedText);
        Line(text, "Severity", entry.Level);
        Line(text, "Module", entry.Module);
        Line(text, "Action", entry.EventAction);
        Line(text, "Outcome", entry.Outcome);
        Line(text, "Machine", entry.HostId);
        Line(text, "Machine address", entry.MacAddress);
        Line(text, "Person", entry.ActorId);
        Line(text, "Person kind", entry.ActorKind);
        Line(text, "Error type", entry.ErrorType);
        Line(text, "Message", entry.Message);
        Line(text, "Exception detail", entry.ExceptionDetail);
        Line(text, "Fingerprint", entry.ErrorFingerprint);
        Line(text, "Chain link", entry.ChainLinkText);
        Line(text, "Payload", entry.PayloadJson);
        Line(text, "Entry id", entry.PublicId);
        Line(text, "Correlation id", entry.CorrelationId);
    }

    private static void Line(StringBuilder text, string label, string? value)
    {
        text.Append(label)
            .Append(": ")
            .AppendLine(string.IsNullOrWhiteSpace(value) ? "none" : value);
    }

    /// <summary>
    /// Cuts the text at the ceiling with an explicit marker, rather than handing on a text that happens to be
    /// short.
    /// </summary>
    /// <remarks>
    /// The cut is taken back to the previous line end so the last thing a reader sees is a whole line rather than
    /// half of one, and back off one more character when that would split a surrogate pair. Both steps keep the
    /// result valid text: a paste that raised an encoding fault would fail at exactly the moment the copy matters.
    /// </remarks>
    private static string Bound(string text)
    {
        var marker = CutMarker + Environment.NewLine;

        if (text.Length + marker.Length <= CeilingCharacters)
        {
            return text;
        }

        var keep = CeilingCharacters - marker.Length;
        var cut = text.LastIndexOf('\n', Math.Max(0, keep - 1));

        if (cut <= 0)
        {
            cut = keep;
        }

        if (cut > 0 && char.IsHighSurrogate(text[cut - 1]))
        {
            cut--;
        }

        return text[..cut] + marker;
    }
}
