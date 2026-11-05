using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;

namespace MTM_Waitlist.Module_Shared.ViewModels;

/// <summary>How a finished catalogue read ended.</summary>
public enum DunnageCatalogueLoadOutcome
{
    /// <summary>The read succeeded and the plant offers parts.</summary>
    Ready,

    /// <summary>The read succeeded and the plant offers no dunnage parts. An answer, not a failure.</summary>
    Empty,

    /// <summary>The read failed. The catalogue's contents are unknown, not empty.</summary>
    Unavailable,
}

/// <summary>
/// What a catalogue read reported: which of the three outcomes, and when it was attempted.
/// </summary>
/// <param name="Outcome">The outcome of the read.</param>
/// <param name="AttemptedAtUtc">When the read was attempted, so an unavailable surface can say so.</param>
public sealed record DunnageCatalogueLoadResult(DunnageCatalogueLoadOutcome Outcome, DateTimeOffset AttemptedAtUtc);

/// <summary>
/// The state and behaviour of the plant's dunnage catalogue: the one-box filter, the pictures-only choice, the
/// selection, and the outcome of the read.
/// </summary>
/// <remarks>
/// <para>
/// Two surfaces draw this catalogue — the generic request's dunnage page and the work-order substitute dialog — so
/// they share this view model and one read, and neither can offer a part the other does not (FR-002, SC-003).
/// </para>
/// <para>
/// It reports an outcome and does not choose a surface's words for a failed read. Its
/// <see cref="EmptyStateMessage"/> is the wording the substitute dialog has always shown, kept byte-identical so
/// that extracting the catalogue did not change what that dialog says; the page takes its own words from its own
/// resource keys.
/// </para>
/// </remarks>
public sealed partial class DunnageCatalogueViewModel : ObservableObject
{
    /// <summary>The scoped-preference key the parts-without-pictures choice is stored under, against the person.</summary>
    public const string ShowPartsWithoutImagesSettingsKey = "Setup.DunnageImageSearch.ShowPartsWithoutImages";

    private const string LogCategory = "DunnageCatalogue";

    private readonly IDunnageCatalogueReader _reader;
    private readonly IScopedPreferenceStore _preferences;
    private IReadOnlyList<DunnageCataloguePart> _allParts = Array.Empty<DunnageCataloguePart>();
    private bool _preferenceLoaded;
    private bool _isRestoringShowPartsPreference;
    private bool _persistedShowPartsWithoutImages = true;
    private DateTimeOffset _lastAttemptedAtUtc = DateTimeOffset.UtcNow;
    private DunnageCatalogueLoadOutcome _lastOutcome = DunnageCatalogueLoadOutcome.Empty;

    public DunnageCatalogueViewModel(IDunnageCatalogueReader reader, IScopedPreferenceStore preferences)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        RefreshCommand = new AsyncRelayCommand(() => LoadAsync());
    }

    /// <summary>The one-box free-text filter, matching part number, type or home location.</summary>
    [ObservableProperty]
    public partial string FilterText { get; set; } = string.Empty;

    /// <summary>Whether parts with no picture are shown. Remembered per person, and true by default.</summary>
    [ObservableProperty]
    public partial bool ShowPartsWithoutImages { get; set; } = true;

    /// <summary>The parts the picture choice and the filter currently leave.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<DunnageCataloguePart> DisplayedParts { get; set; } = Array.Empty<DunnageCataloguePart>();

    /// <summary>The chosen part, or <c>null</c> when nothing has been chosen. Nothing is ever pre-selected.</summary>
    [ObservableProperty]
    public partial DunnageCataloguePart? SelectedPart { get; set; }

    /// <summary>Whether a read is in flight.</summary>
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>The words for a read that ended with nothing to show, in the substitute dialog's own wording.</summary>
    [ObservableProperty]
    public partial string EmptyStateMessage { get; set; } = "Loading Dunnage parts...";

    /// <summary>True when the drawing list is empty, which is when a surface shows the empty-state message.</summary>
    public bool HasNoResults => DisplayedParts.Count == 0;

    /// <summary>Run the read again, as the refresh control does.</summary>
    public IAsyncRelayCommand RefreshCommand { get; }

    /// <summary>Read the picture choice from the person's preferences, then read the catalogue.</summary>
    public async Task<DunnageCatalogueLoadResult> InitializeAsync(CancellationToken cancellationToken = default)
    {
        await LoadShowPartsPreferenceAsync().ConfigureAwait(true);
        return await LoadAsync(cancellationToken).ConfigureAwait(true);
    }

    /// <summary>
    /// Read the catalogue and report which of the three outcomes it produced.
    /// </summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The outcome, with the time the read was attempted.</returns>
    public async Task<DunnageCatalogueLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        // A second read while one is in flight changes nothing: the standing outcome is reported instead, so a
        // double-clicked refresh cannot leave the screen claiming a result the store has not answered yet.
        if (IsBusy)
        {
            return new DunnageCatalogueLoadResult(_lastOutcome, _lastAttemptedAtUtc);
        }

        _lastAttemptedAtUtc = DateTimeOffset.UtcNow;
        IsBusy = true;

        try
        {
            EmptyStateMessage = ShowPartsWithoutImages
                ? "Loading Dunnage parts..."
                : "Loading parts with images...";

            var parts = await _reader.ReadAllAsync(cancellationToken).ConfigureAwait(true);
            _allParts = parts
                .OrderBy(part => part.PartNumber, StringComparer.OrdinalIgnoreCase)
                .ToList();

            ApplyFilter();

            _lastOutcome = OutcomeFor(_allParts.Count);
            return new DunnageCatalogueLoadResult(_lastOutcome, _lastAttemptedAtUtc);
        }
        catch (OperationCanceledException)
        {
            // A cancelled read is not a failed read, so it changes nothing on screen.
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Error(LogCategory, ex, "Failed to load the dunnage catalogue.");
            _allParts = Array.Empty<DunnageCataloguePart>();
            DisplayedParts = Array.Empty<DunnageCataloguePart>();
            EmptyStateMessage = "Failed to load Dunnage parts.";

            _lastOutcome = DunnageCatalogueLoadOutcome.Unavailable;
            return new DunnageCatalogueLoadResult(_lastOutcome, _lastAttemptedAtUtc);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Record the chosen part, or clear the choice when nothing is passed.</summary>
    public void SelectPart(DunnageCataloguePart? part) => SelectedPart = part;

    /// <summary>
    /// Choose the part with this number when the catalogue holds it, so a step can show the part already chosen.
    /// </summary>
    /// <param name="partNumber">The part number to look for, matched case-insensitively.</param>
    /// <returns><see langword="true"/> when a part was found and chosen.</returns>
    public bool SelectPartNumber(string? partNumber)
    {
        if (string.IsNullOrWhiteSpace(partNumber))
        {
            return false;
        }

        var match = _allParts.FirstOrDefault(part =>
            string.Equals(part.PartNumber, partNumber.Trim(), StringComparison.OrdinalIgnoreCase));

        SelectedPart = match;
        return match is not null;
    }

    /// <summary>Record the pictures-only choice and remember it against the person.</summary>
    public async Task SetShowPartsWithoutImagesAsync(bool isChecked, CancellationToken cancellationToken = default)
    {
        if (_isRestoringShowPartsPreference is false && ShowPartsWithoutImages != isChecked)
        {
            _isRestoringShowPartsPreference = true;
            ShowPartsWithoutImages = isChecked;
            _isRestoringShowPartsPreference = false;
            ApplyFilter();
        }

        if (isChecked == _persistedShowPartsWithoutImages)
        {
            return;
        }

        await PersistShowPartsPreferenceAsync(isChecked, cancellationToken).ConfigureAwait(true);
    }

    partial void OnFilterTextChanged(string value) => ApplyFilter();

    partial void OnShowPartsWithoutImagesChanged(bool value)
    {
        if (_preferenceLoaded)
        {
            ApplyFilter();
        }
    }

    partial void OnDisplayedPartsChanged(IReadOnlyList<DunnageCataloguePart> value) =>
        OnPropertyChanged(nameof(HasNoResults));

    private static DunnageCatalogueLoadOutcome OutcomeFor(int partCount) =>
        partCount == 0 ? DunnageCatalogueLoadOutcome.Empty : DunnageCatalogueLoadOutcome.Ready;

    private void ApplyFilter()
    {
        IEnumerable<DunnageCataloguePart> filteredParts = _allParts;

        if (ShowPartsWithoutImages is false)
        {
            filteredParts = filteredParts.Where(static part => part.HasImage);
        }

        if (string.IsNullOrWhiteSpace(FilterText) is false)
        {
            var filterText = FilterText;
            filteredParts = filteredParts.Where(part =>
                part.PartNumber.Contains(filterText, StringComparison.OrdinalIgnoreCase)
                || part.DunnageTypeName.Contains(filterText, StringComparison.OrdinalIgnoreCase)
                || part.HomeLocation.Contains(filterText, StringComparison.OrdinalIgnoreCase));
        }

        DisplayedParts = filteredParts.ToArray();
        EmptyStateMessage = BuildEmptyStateMessage();
    }

    private string BuildEmptyStateMessage()
    {
        if (_allParts.Count == 0)
        {
            return ShowPartsWithoutImages
                ? "No Dunnage parts found."
                : "No Dunnage parts currently have image paths configured.";
        }

        return "No Dunnage parts matched the current filter.";
    }

    private async Task LoadShowPartsPreferenceAsync()
    {
        if (_preferenceLoaded)
        {
            return;
        }

        _isRestoringShowPartsPreference = true;

        try
        {
            // Default to "Show all" (true) so every part is visible on first use; only narrow to images-only when
            // the person has explicitly saved that.
            var savedPreference = await _preferences
                .ReadFlagAsync(ShowPartsWithoutImagesSettingsKey, PreferenceScope.Person)
                .ConfigureAwait(true);

            var resolved = savedPreference ?? true;
            _persistedShowPartsWithoutImages = resolved;
            ShowPartsWithoutImages = resolved;
        }
        catch (Exception ex)
        {
            AppLog.Error(LogCategory, ex, "Failed to load 'show parts without images' preference. Falling back to show all.");

            _persistedShowPartsWithoutImages = true;
            ShowPartsWithoutImages = true;
        }
        finally
        {
            _isRestoringShowPartsPreference = false;
            _preferenceLoaded = true;
            ApplyFilter();
        }
    }

    private async Task PersistShowPartsPreferenceAsync(bool isChecked, CancellationToken cancellationToken)
    {
        try
        {
            await _preferences
                .WriteFlagAsync(ShowPartsWithoutImagesSettingsKey, PreferenceScope.Person, isChecked, cancellationToken)
                .ConfigureAwait(true);

            _persistedShowPartsWithoutImages = isChecked;
        }
        catch (Exception ex)
        {
            AppLog.Error(LogCategory, ex, "Failed to save 'show parts without images' preference.");

            _isRestoringShowPartsPreference = true;
            ShowPartsWithoutImages = _persistedShowPartsWithoutImages;
            _isRestoringShowPartsPreference = false;
            ApplyFilter();
        }
    }
}
