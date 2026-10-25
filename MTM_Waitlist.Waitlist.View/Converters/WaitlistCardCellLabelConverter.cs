using Microsoft.UI.Xaml.Data;

using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Waitlist.Converters;

/// <summary>
/// The text a detail cell's label shows, resolved through the application's resources at bind time
/// (FR-104).
/// </summary>
/// <remarks>
/// <para>
/// A cell's label is the label the <b>declaration</b> carries — the card never overrides it (FR-059) — and it
/// is a localized resource key. The key is resolved here rather than in markup so a declaration that names a
/// resource shows that resource's text, and a declaration that still carries a plain word shows that word
/// rather than a bare key or nothing at all.
/// </para>
/// </remarks>
public sealed class WaitlistCardCellLabelConverter : IValueConverter
{
    /// <summary>
    /// The parameter that asks for the label's separator, so a cell that draws its value on the same line reads
    /// `Label: value` rather than running the two together.
    /// </summary>
    public const string SeparatorParameter = "separator";

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var text = value is string label ? label.GetLocalized() : string.Empty;

        return string.Equals(parameter as string, SeparatorParameter, StringComparison.Ordinal) && text.Length > 0
            ? text + ": "
            : text;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
