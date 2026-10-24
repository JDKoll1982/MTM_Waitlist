using Microsoft.UI.Xaml.Data;

using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Settings.Converters;

/// <summary>
/// Shows what the application would draw as "no picture" if the path in the box were the one saved.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately not <see cref="ResolvedImagePathToSourceConverter"/>: that one applies the picture rule, which
/// refuses anything that is not square. The placeholder is drawn by existence alone — a plant's own "no picture"
/// artwork is allowed to be any shape — so a preview that policed the shape would show a different picture from the
/// one the cards show.
/// </para>
/// <para>
/// The empty box is the shipped artwork, and a path that names no file previews as the shipped artwork too, because
/// that is exactly what every surface will draw while the file is missing.
/// </para>
/// </remarks>
public sealed class NoImagePathToSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        PictureSource.FromNoImage(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
