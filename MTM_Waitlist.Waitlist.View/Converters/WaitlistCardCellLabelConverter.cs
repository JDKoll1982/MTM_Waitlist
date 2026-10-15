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
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is string label ? label.GetLocalized() : string.Empty;

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
