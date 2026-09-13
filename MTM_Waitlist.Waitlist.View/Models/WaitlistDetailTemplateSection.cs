using System.Collections.ObjectModel;

using Microsoft.UI.Text;
using Microsoft.UI.Xaml;

using MTM_Waitlist.Module_Settings.Models;

using Windows.UI.Text;

namespace MTM_Waitlist.Module_Waitlist.Models;

public sealed class WaitlistDetailTemplateSection
{
    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public ObservableCollection<WaitlistDetailTemplateField> Fields { get; } = new();
}

public sealed class WaitlistDetailTemplateField
{
    public string Label { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The value type the Item's configuration declares for this field (FR-013). It travels with the field so
    /// the page presents the value the way the configuration said it is — a chosen option is not prose, and a
    /// long text answer is — rather than presenting every field the same way.
    /// </summary>
    public RequestItemValueType ValueType { get; set; } = RequestItemValueType.String;

    /// <summary>Whether the declared type is a value chosen from a bounded set.</summary>
    public bool IsEnumerated => ValueType == RequestItemValueType.Enum;

    /// <summary>
    /// How the value wraps: a declared <c>text</c> field is prose and wraps whole words; anything else is a
    /// value and wraps as one.
    /// </summary>
    public TextWrapping ValueTextWrapping
        => ValueType == RequestItemValueType.Text ? TextWrapping.WrapWholeWords : TextWrapping.Wrap;

    /// <summary>
    /// How the value is weighted: a declared <c>enum</c> field is an answer someone chose, so it reads as a
    /// deliberate value rather than as one more attribute.
    /// </summary>
    public FontWeight ValueFontWeight => IsEnumerated ? FontWeights.SemiBold : FontWeights.Normal;
}