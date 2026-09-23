namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One column of a card: what one permission in that area is called and what it gates, in the reader's words.
/// </summary>
/// <remarks>
/// The column carries the declaration's plain-language label and its what-it-gates sentence, and never the
/// permission's key, so the matrix names what a person may do rather than what the code calls it (FR-065). Both
/// sentences live in the heading rather than in every cell: the cells are marks, and a sentence repeated in
/// fourteen columns across ten rows is a sentence nobody reads.
/// </remarks>
public sealed class PermissionColumn
{
    public PermissionColumn(string key, string labelText, string gatesText)
    {
        Key = key;
        LabelText = labelText;
        GatesText = gatesText;
    }

    /// <summary>The pinned permission key. It is the column's identity and is never shown to a person.</summary>
    public string Key { get; }

    /// <summary>What the permission is called, which is what the reader reads across the top of the card.</summary>
    public string LabelText { get; }

    /// <summary>The sentence saying what the permission gates, beneath the label in the same heading (FR-065).</summary>
    public string GatesText { get; }
}
