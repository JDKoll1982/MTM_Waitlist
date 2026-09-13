using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One row of the minutes screen: an <b>Item</b>, its <b>configured</b> allotted minutes and the <b>observed</b>
/// average its completed requests actually took.
/// </summary>
/// <remarks>
/// <para>
/// The two numbers are deliberately two different members, not one formatted string: the configured figure is
/// the editable value (<see cref="Minutes"/>, backed by a NumberBox) and the observed average is read-only
/// display data (<see cref="ObservedAverage"/>) that is never written back as though it were configured
/// (FR-018, FR-019, SC-008).
/// </para>
/// <para>
/// The row is keyed by the <b>Item</b>, the same identity the request is stored with. There is no request type
/// and no subtype anywhere on it (FR-016, FR-023).
/// </para>
/// </remarks>
public partial class UrgencyAllotmentItem : ObservableObject
{
    /// <summary>Resource key for the marker shown when the figure was configured for this Item (FR-022).</summary>
    public const string ConfiguredLabelKey = "Settings_UrgencyAllotments.ConfiguredValue";

    /// <summary>Resource key for the marker shown when the figure is the labelled fallback (FR-017, FR-022).</summary>
    public const string DefaultLabelKey = "Settings_UrgencyAllotments.ConfiguredDefault";

    /// <summary>Resource key for the statement shown when an Item has no completed request yet (FR-026).</summary>
    public const string NoObservedAverageKey = "Settings_UrgencyAllotments.NoObservedAverage";

    public UrgencyAllotmentItem(
        string itemCode,
        string displayName,
        double configuredMinutes,
        bool isConfiguredValueDefault,
        TimeSpan? observedAverage)
    {
        ItemCode = itemCode;
        DisplayName = displayName;
        Minutes = configuredMinutes;
        IsConfiguredValueDefault = isConfiguredValueDefault;
        ObservedAverage = observedAverage;
        ConfiguredValueLabel = ResolveLabel(isConfiguredValueDefault);
        ObservedAverageText = ResolveObservedText(observedAverage);
    }

    /// <summary>The Item code this row configures — the write's key.</summary>
    public string ItemCode { get; }

    /// <summary>The Item's name as a person reads it.</summary>
    public string DisplayName { get; }

    /// <summary>The Item's <b>configured</b> allotted minutes, editable through this member.</summary>
    [ObservableProperty]
    public partial double Minutes
    {
        get; set;
    }

    /// <summary>
    /// Whether <see cref="Minutes"/> is the fallback <b>default</b> rather than a value someone configured for
    /// this Item (FR-017).
    /// </summary>
    public bool IsConfiguredValueDefault { get; }

    /// <summary>"configured" or "default" — so the number's meaning is visible, not inferred (FR-017).</summary>
    public string ConfiguredValueLabel { get; }

    /// <summary>
    /// The observed average of the Item's completed requests, or <b>null</b> when it has none. Null means "no
    /// value", never zero minutes (FR-019, FR-026).
    /// </summary>
    public TimeSpan? ObservedAverage { get; }

    /// <summary>Whether the Item has an observed average to show at all.</summary>
    public bool HasObservedAverage => ObservedAverage.HasValue;

    /// <summary>The observed average in words, or a plain statement that there is none yet.</summary>
    public string ObservedAverageText { get; }

    private static string ResolveLabel(bool isDefault) => Resolve(
        isDefault ? DefaultLabelKey : ConfiguredLabelKey,
        isDefault ? "default" : "configured");

    private static string ResolveObservedText(TimeSpan? observedAverage)
    {
        if (observedAverage is not TimeSpan observed)
        {
            return Resolve(NoObservedAverageKey, "No completed requests yet");
        }

        return observed.TotalHours >= 1
            ? $"{(int)observed.TotalHours}h {observed.Minutes}m"
            : $"{Math.Max(1, (int)Math.Round(observed.TotalMinutes))}m";
    }

    /// <summary>
    /// Resolves a string through the existing resource mechanism, falling back to readable words so a person is
    /// never shown a bare resource key (FR-022).
    /// </summary>
    private static string Resolve(string key, string fallback)
    {
        var localized = key.GetLocalized();

        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? fallback
            : localized;
    }
}
