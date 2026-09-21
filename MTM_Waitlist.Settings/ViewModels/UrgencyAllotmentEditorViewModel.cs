using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Module_Settings.ViewModels;

/// <summary>
/// Backs the "allotted minutes per Item" editor on the Settings screen. One row per catalogued Item, showing the
/// Item's <b>configured</b> minutes beside the <b>observed</b> average its completed requests actually took —
/// two values with two meanings, never one (FR-018, SC-008) — and persisting each row's minutes to the Item's
/// stored configuration on change.
/// </summary>
/// <remarks>
/// <para>
/// <b>Keyed by Item.</b> The Item is the same identity the request is stored with, and the same one the
/// deadline derives from (FR-016). The request type and the subtype are gone from this screen entirely.
/// </para>
/// <para>
/// <b>Its own role gate.</b> Editing is gated on <see cref="CanManageUrgencySettings"/> — the gate that already
/// governs this screen (FR-020). The picture screen keeps <c>CanManageImageLocationSettings</c>; the two are
/// not collapsed into one and no third gate is introduced.
/// </para>
/// </remarks>
public partial class UrgencyAllotmentEditorViewModel : ObservableObject
{
    private readonly IUrgencySettingsService _urgencySettingsService;
    private readonly IRequestItemObservedTimeService _observedTimeService;

    public UrgencyAllotmentEditorViewModel(
        IUrgencySettingsService urgencySettingsService,
        IRequestItemObservedTimeService observedTimeService)
    {
        _urgencySettingsService = urgencySettingsService ?? throw new ArgumentNullException(nameof(urgencySettingsService));
        _observedTimeService = observedTimeService ?? throw new ArgumentNullException(nameof(observedTimeService));
    }

    /// <summary>One row per catalogued Item, in catalog order.</summary>
    public ObservableCollection<UrgencyAllotmentItem> Items { get; } = new();

    /// <summary>
    /// Whether the signed-in person may change the allotted minutes, answered from
    /// <c>permission.settings.urgency_minutes</c> rather than from a list of role names kept here (FR-054).
    /// </summary>
    /// <remarks>
    /// The Settings screen asks the permission service once for every gate it and its child view models need, so
    /// this is an answer handed in rather than a second read: <see cref="ApplyPermission"/> is called with it.
    /// It stays false until that answer arrives, so no editable control is drawn on a guess.
    /// </remarks>
    [ObservableProperty]
    public partial bool CanManageUrgencySettings
    {
        get; set;
    }

    /// <summary>Applies the answer the Settings screen read for this screen's one permission.</summary>
    /// <param name="canManage">Whether the signed-in person holds it.</param>
    public void ApplyPermission(bool canManage) => CanManageUrgencySettings = canManage;

    [ObservableProperty]
    public partial bool IsBusy
    {
        get; set;
    }

    [ObservableProperty]
    public partial string StatusMessage
    {
        get; set;
    } = string.Empty;

    /// <summary>
    /// Loads the configured/observed pair for every Item through the pair service — one read, never one read
    /// per row (FR-024). A failed read is reported in plain language and leaves the screen empty rather than
    /// filling it with invented numbers (FR-026).
    /// </summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            var rows = new List<UrgencyAllotmentItem>();

            try
            {
                var pairs = await _observedTimeService
                    .GetObservedTimesAsync(cancellationToken)
                    .ConfigureAwait(true);

                foreach (var pair in pairs)
                {
                    var row = new UrgencyAllotmentItem(
                        pair.Item,
                        pair.DisplayName,
                        pair.ConfiguredMinutes.TotalMinutes,
                        pair.IsConfiguredValueDefault,
                        pair.ObservedAverage);

                    row.PropertyChanged += OnRowPropertyChanged;
                    rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                StartupDebugLog.Error("UrgencyAllotments", ex, "Failed to read the configured and observed minutes per Item.");
                StatusMessage = ResolveStatus(
                    "Settings_UrgencyAllotments.LoadFailed",
                    "The minutes could not be read from the store, so nothing is shown. Retry once the store is reachable.");
                return;
            }

            Items.Clear();
            foreach (var row in rows)
            {
                Items.Add(row);
            }

            StatusMessage = rows.Count == 0
                ? ResolveStatus("Settings_UrgencyAllotments.NoItems", "No items are configured yet.")
                : string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    ResolveStatus("Settings_UrgencyAllotments.Loaded", "{0} item(s) loaded."),
                    Items.Count);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Persists one row's edited minutes to the Item's stored configuration. The observed average is display
    /// data and is never written back (FR-019).
    /// </summary>
    private async void OnRowPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(UrgencyAllotmentItem.Minutes) || sender is not UrgencyAllotmentItem row)
        {
            return;
        }

        if (!CanManageUrgencySettings)
        {
            return;
        }

        try
        {
            var minutes = (int)Math.Round(row.Minutes);
            await _urgencySettingsService.SetMaxAllottedAsync(row.ItemCode, minutes).ConfigureAwait(true);
            StartupDebugLog.Info("UrgencyAllotments", $"Saved the allotted minutes for item '{row.ItemCode}' = {minutes} min.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("UrgencyAllotments", ex, $"Failed to save the allotted minutes for item '{row.ItemCode}'.");
            StatusMessage = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                ResolveStatus("Settings_UrgencyAllotments.SaveFailed", "Unable to save '{0}': {1}"),
                row.ItemCode,
                ex.Message);
        }
    }

    /// <summary>
    /// A message resolved through the existing resource mechanism, with a readable fallback so a person is never
    /// shown a bare resource key (FR-022).
    /// </summary>
    private static string ResolveStatus(string key, string fallback)
    {
        var localized = key.GetLocalized();

        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? fallback
            : localized;
    }
}
