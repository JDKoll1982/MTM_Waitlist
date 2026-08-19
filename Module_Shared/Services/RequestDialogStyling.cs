using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace MTM_Waitlist.Module_Shared.Services;

/// <summary>
/// Applies consistent styling and sizing to the Request-workflow dialogs
/// (Work Center -&gt; Request Type -&gt; Request Subtype -&gt; validation/confirmation).
/// Follows WinUI guidance: style via the default ContentDialog style (BasedOn) and
/// size through the ContentDialog* theme keys so every dialog matches.
/// </summary>
public static class RequestDialogStyling
{
    public const double DialogWidth = 900;

    /// <summary>
    /// Applies the shared dialog style and the standard width to a code-created dialog.
    /// Safe to call before <see cref="ContentDialog.ShowAsync"/>.
    /// </summary>
    public static void Apply(ContentDialog dialog, double width = DialogWidth)
    {
        ApplyStyle(dialog);
        ApplySizing(dialog, width);
    }

    public static void ApplyStyle(ContentDialog dialog)
    {
        if (dialog is null)
        {
            return;
        }

        try
        {
            if (Application.Current?.Resources.TryGetValue("RequestContentDialogStyle", out var value) == true
                && value is Style style)
            {
                dialog.Style = style;
            }
        }
        catch (Exception)
        {
            // Styling is best-effort; never break the dialog flow.
        }
    }

    public static void ApplySizing(ContentDialog dialog, double width = DialogWidth)
    {
        if (dialog?.Resources is not ResourceDictionary resources)
        {
            return;
        }

        try
        {
            resources["ContentDialogMaxWidth"] = width;
            resources["ContentDialogThemeMaxWidth"] = width;
            resources["ContentDialogThemeMinWidth"] = width;
            resources["ContentDialogMinWidth"] = width;
        }
        catch (Exception)
        {
            // Best-effort.
        }
    }

    /// <summary>
    /// Resolves a style from the application resources (e.g. a RequestDialogs.xaml style).
    /// </summary>
    public static Style? GetStyle(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true
            ? value as Style
            : null;

    /// <summary>
    /// Resolves a theme brush from the application resources.
    /// </summary>
    public static Brush? GetBrush(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true
            ? value as Brush
            : null;
}
