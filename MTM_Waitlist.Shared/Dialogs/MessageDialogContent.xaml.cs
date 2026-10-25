using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MTM_Waitlist.Module_Shared.Dialogs;

/// <summary>
/// The body of a standard message dialog: a title, the consequence, and the named thing it is about
/// (014-adopt-lab-design-system, FR-005).
/// </summary>
/// <remarks>
/// It shows words and nothing else. Which buttons appear below it, which one is the default and what each answer means
/// are decided by <see cref="MessageKind"/>, so this control never sees them.
/// </remarks>
public sealed partial class MessageDialogContent : UserControl
{
    /// <summary>Creates the body. Call <see cref="Load"/> before showing it.</summary>
    public MessageDialogContent()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Fills the body from <paramref name="request"/> and applies the shared sizing scale.
    /// </summary>
    /// <param name="request">The question being asked.</param>
    public void Load(MessageDialogRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        TitleText.Text = request.Title.Trim();
        MessageText.Text = request.Message.Trim();

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            SubjectText.Visibility = Visibility.Collapsed;
            SubjectText.Text = string.Empty;
        }
        else
        {
            SubjectText.Text = request.Subject.Trim();
            SubjectText.Visibility = Visibility.Visible;
        }

        ApplySizing();
    }

    private void ApplySizing()
    {
        var preferred = DialogSizing.PreferredWidth;
        var minimum = DialogSizing.MinWidth;
        var maximum = DialogSizing.MaxWidth;

        // A stretch element is centred by the framework on its own, so this is a cap and a floor, not an alignment.
        MinWidth = Math.Min(minimum, preferred);
        Width = preferred;
        MaxWidth = Math.Max(maximum, preferred);

        MessageScroller.MinHeight = DialogSizing.BodyMinHeight;
        MessageScroller.MaxHeight = DialogSizing.MaxBodyHeight;
    }
}
