using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Shared.Dialogs;

/// <summary>
/// The body of a standard message dialog: a title, the consequence, and the named thing it is about
/// (014-adopt-lab-design-system, FR-005).
/// </summary>
/// <remarks>
/// <para>
/// It shows words and nothing else. Which buttons appear below it, which one is the default and what each answer means
/// are decided by <see cref="MessageKind"/>, so this body never sees them.
/// </para>
/// <para>
/// The body is composed here rather than loaded from markup, because this library holds no view type and a
/// <c>UserControl</c> is one (024-dependency-modernization, MOD-006). The shape is unchanged: a title row, a scrolling
/// message row, and a subject row that appears only when the question names something.
/// </para>
/// <para>
/// Sizing comes from <see cref="DialogSizing"/> and never from a number written here. The text styles and brushes are
/// the platform's, resolved by key while an application is running and left at the control's own defaults when there is
/// none, which is the same fallback the sizing scale uses.
/// </para>
/// </remarks>
public sealed class MessageDialogContent : Grid
{
    private const string TitleStyleKey = "SubtitleTextBlockStyle";
    private const string MessageStyleKey = "BodyTextBlockStyle";
    private const string SubjectStyleKey = "BodyStrongTextBlockStyle";
    private const string MessageBrushKey = "TextFillColorSecondaryBrush";
    private const string SubjectBrushKey = "TextFillColorPrimaryBrush";

    private readonly TextBlock _title;
    private readonly TextBlock _message;
    private readonly ScrollViewer _messageScroller;
    private readonly TextBlock _subject;

    /// <summary>Creates the body. Call <see cref="Load"/> before showing it.</summary>
    public MessageDialogContent()
    {
        RowSpacing = 12;
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _title = new TextBlock
        {
            TextWrapping = TextWrapping.WrapWholeWords,
        };
        ApplyStyle(_title, TitleStyleKey);
        SetRow(_title, 0);
        Children.Add(_title);

        _message = new TextBlock
        {
            TextWrapping = TextWrapping.WrapWholeWords,
        };
        ApplyStyle(_message, MessageStyleKey);
        ApplyBrush(_message, MessageBrushKey);

        _messageScroller = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollMode = ScrollMode.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollMode = ScrollMode.Auto,
            Content = _message,
        };
        SetRow(_messageScroller, 1);
        Children.Add(_messageScroller);

        _subject = new TextBlock
        {
            TextWrapping = TextWrapping.WrapWholeWords,
            Visibility = Visibility.Collapsed,
        };
        ApplyStyle(_subject, SubjectStyleKey);
        ApplyBrush(_subject, SubjectBrushKey);
        SetRow(_subject, 2);
        Children.Add(_subject);
    }

    /// <summary>
    /// Fills the body from <paramref name="request"/> and applies the shared sizing scale.
    /// </summary>
    /// <param name="request">The question being asked.</param>
    public void Load(MessageDialogRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        _title.Text = request.Title.Trim();
        _message.Text = request.Message.Trim();

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            _subject.Visibility = Visibility.Collapsed;
            _subject.Text = string.Empty;
        }
        else
        {
            _subject.Text = request.Subject.Trim();
            _subject.Visibility = Visibility.Visible;
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

        _messageScroller.MinHeight = DialogSizing.BodyMinHeight;
        _messageScroller.MaxHeight = DialogSizing.MaxBodyHeight;
    }

    private static void ApplyStyle(FrameworkElement element, string styleKey)
    {
        if (TryGetResource(styleKey, out Style? style) && style is not null)
        {
            element.Style = style;
        }
    }

    private static void ApplyBrush(TextBlock text, string brushKey)
    {
        if (TryGetResource(brushKey, out Brush? brush) && brush is not null)
        {
            text.Foreground = brush;
        }
    }

    private static readonly HashSet<string> s_reportedMissingResources = new(StringComparer.Ordinal);

    private static bool TryGetResource<T>(string resourceKey, out T? resource)
        where T : class
    {
        resource = null;

        try
        {
            if (Application.Current?.Resources.TryGetValue(resourceKey, out var value) == true
                && value is T typed)
            {
                resource = typed;
                return true;
            }
        }
        catch (Exception)
        {
            // Resource lookup can fail before the dictionaries are loaded; the control keeps its own defaults.
        }

        // A miss is not fatal — the body still shows its words on the control's own defaults — but it is reported once
        // per key, because a dialog that quietly lost its typography is a difference nobody would notice in a test and
        // everybody would notice on the screen (FR-015).
        if (s_reportedMissingResources.Add(resourceKey))
        {
            AppLog.Warn("MessageDialogContent", $"The dialog body's resource '{resourceKey}' did not resolve, so the body is drawing with the control's defaults.");
        }

        return false;
    }
}
