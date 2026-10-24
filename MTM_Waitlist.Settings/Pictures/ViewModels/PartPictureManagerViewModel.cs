using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Settings.ViewModels;

/// <summary>
/// The part-picture screen: find a part, set or replace its picture, read back who changed what, and work through
/// the parts that still have none.
/// </summary>
/// <remarks>
/// <para>
/// The screen resolves a picture through the same reader every surface draws with, so what it shows for a part is
/// what a card draws for it rather than a second opinion. It is offered only to an account holding the picture
/// entitlement — the gate is on the Settings entry point, which is what keeps an unentitled account from ever
/// reaching this view model.
/// </para>
/// <para>
/// Every refusal is stated in the person's words. The write path answers with a code and, where it applies, the
/// part that already holds the file name; the sentences themselves live in the resources so they are translated
/// with the rest of the screen.
/// </para>
/// </remarks>
public sealed partial class PartPictureManagerViewModel : ObservableObject
{
    private readonly IPartPictureResolver _resolver;
    private readonly IPartPictureWriteService _writeService;
    private readonly PartPictureCoverageService _coverageService;
    private readonly ILogger<PartPictureManagerViewModel> _logger;

    public PartPictureManagerViewModel(
        IPartPictureResolver resolver,
        IPartPictureWriteService writeService,
        PartPictureCoverageService coverageService,
        ILogger<PartPictureManagerViewModel> logger)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _writeService = writeService ?? throw new ArgumentNullException(nameof(writeService));
        _coverageService = coverageService ?? throw new ArgumentNullException(nameof(coverageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Systems =
        [
            new PartPictureSystemOption(PartPictureSystem.Visual, "Settings_PartPictures_System_Visual.Content".GetLocalized()),
            new PartPictureSystemOption(PartPictureSystem.Wip, "Settings_PartPictures_System_Wip.Content".GetLocalized()),
        ];

        SelectedSystem = Systems[0];
        CoverageNote = BuildCoverageNote();
    }

    /// <summary>The two systems a part can belong to, in the order the screen offers them.</summary>
    public ObservableCollection<PartPictureSystemOption> Systems { get; }

    /// <summary>The system the person is working in.</summary>
    [ObservableProperty]
    public partial PartPictureSystemOption? SelectedSystem { get; set; }

    /// <summary>What the person typed, or the part number an entry point handed over.</summary>
    [ObservableProperty]
    public partial string PartNumberInput { get; set; } = string.Empty;

    /// <summary>The part the person looked up, with the picture the application would draw for it.</summary>
    [ObservableProperty]
    public partial PartPictureRow? CurrentPart { get; set; }

    /// <summary>The picture the person chose, before it is saved.</summary>
    [ObservableProperty]
    public partial string SourceFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// The last refusal in the form the screen reasons about rather than translates: its code, the part it
    /// collided with, and the limit it exceeded. The sentence a person reads is <see cref="ErrorMessage"/>; these
    /// are what let the refusal be proved without reading a translated string.
    /// </summary>
    public string? LastRefusalCode { get; private set; }

    /// <summary>The part that already held the name, when the refusal is a name collision.</summary>
    public string? LastRefusalPartNumber { get; private set; }

    /// <summary>The limit that was exceeded, when the refusal is an over-long part number.</summary>
    public int? LastRefusalLimit { get; private set; }

    /// <summary>The part's change record, newest first.</summary>
    public ObservableCollection<PartPictureChangeDisplay> ChangeRecord { get; } = new();

    /// <summary>The parts the application can name that still have no picture.</summary>
    public ObservableCollection<PartPictureMissRow> MissingParts { get; } = new();

    /// <summary>One line naming the systems this screen cannot list parts for, or empty when it can list them all.</summary>
    [ObservableProperty]
    public partial string CoverageNote { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);
    public bool HasCurrentPart => CurrentPart is not null;

    public bool HasChangeRecord => ChangeRecord.Count > 0;

    public bool HasMissingParts => MissingParts.Count > 0;

    /// <summary>
    /// The visibility answers the markup binds to directly, because the only converters this application declares
    /// are one-way-to-visible ones: a rule spelled as a converter would need a second converter for its inverse.
    /// </summary>
    public Visibility NoCurrentPictureVisibility =>
        HasCurrentPart && !CurrentPartHasPicture ? Visibility.Visible : Visibility.Collapsed;

    public Visibility HistoryEmptyVisibility => HasChangeRecord ? Visibility.Collapsed : Visibility.Visible;

    public Visibility MissingEmptyVisibility => HasMissingParts ? Visibility.Collapsed : Visibility.Visible;

    public Visibility CoverageNoteVisibility =>
        string.IsNullOrWhiteSpace(CoverageNote) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>The picture to draw for the part in front of the person: its own, or the shared placeholder.</summary>
    public string CurrentPicturePath => CurrentPart?.ImagePath ?? ImagePicturePolicy.NoImagePath;

    /// <summary>Whether the part in front of the person already has a picture, which the button replaces.</summary>
    public bool CurrentPartHasPicture => CurrentPart?.HasPicture ?? false;

    /// <summary>Where the part's picture is stored, relative to the configured picture root.</summary>
    public string CurrentPartStoredAt => CurrentPart?.RelativeFolderPath ?? string.Empty;

    /// <summary>The action's own words: setting a first picture, or replacing one that is already there.</summary>
    public string PictureActionLabel => CurrentPartHasPicture
        ? "Settings_PartPictures_Replace.Label".GetLocalized()
        : "Settings_PartPictures_Set.Label".GetLocalized();

    /// <summary>The missing-picture count in the person's words.</summary>
    public string MissingCountText => string.Format(
        System.Globalization.CultureInfo.CurrentCulture,
        "Settings_PartPictures_Missing_Count.Text".GetLocalized(),
        MissingParts.Count);

    /// <summary>The export's body, built from the list as it stands.</summary>
    public string ExportCsv => PartPictureCoverageService.BuildExportCsv(MissingParts);

    /// <summary>The name the save picker suggests for the export.</summary>
    public string ExportSuggestedFileName => "Settings_PartPictures_Missing_Export_SuggestedFileName.Text".GetLocalized();

    public string PartPicturesTitle => "Settings_PartPictures_Title.Text".GetLocalized();

    public string SystemLabel => "Settings_PartPictures_System.Label".GetLocalized();

    public string PartNumberLabel => "Settings_PartPictures_PartNumber.Label".GetLocalized();

    public string PartNumberPlaceholder => "Settings_PartPictures_PartNumber.PlaceholderText".GetLocalized();

    public string FindLabel => "Settings_PartPictures_Find.Label".GetLocalized();

    public string CurrentPictureLabel => "Settings_PartPictures_CurrentPicture.Text".GetLocalized();

    public string NoCurrentPictureText => "Settings_PartPictures_NoCurrentPicture.Text".GetLocalized();

    public string StoredAtLabel => "Settings_PartPictures_StoredAt.Text".GetLocalized();

    public string HistoryTitle => "Settings_PartPictures_History_Title.Text".GetLocalized();

    public string HistoryEmptyText => "Settings_PartPictures_History_Empty.Text".GetLocalized();

    public string MissingTitle => "Settings_PartPictures_Missing_Title.Text".GetLocalized();

    public string MissingEmptyText => "Settings_PartPictures_Missing_Empty.Text".GetLocalized();

    public string ExportLabel => "Settings_PartPictures_Missing_Export.Label".GetLocalized();

    /// <summary>The title the picture picker wears.</summary>
    public string PickPictureTitle => "Settings_PartPictures_PickPicture_Title.Text".GetLocalized();

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatus));

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    partial void OnCoverageNoteChanged(string value) => OnPropertyChanged(nameof(CoverageNoteVisibility));

    partial void OnCurrentPartChanged(PartPictureRow? value)
    {
        OnPropertyChanged(nameof(HasCurrentPart));
        OnPropertyChanged(nameof(CurrentPicturePath));
        OnPropertyChanged(nameof(CurrentPartHasPicture));
        OnPropertyChanged(nameof(CurrentPartStoredAt));
        OnPropertyChanged(nameof(PictureActionLabel));
        OnPropertyChanged(nameof(NoCurrentPictureVisibility));
    }

    /// <summary>
    /// Looks up one part and answers with the picture the application would draw for it, plus its change record.
    /// </summary>
    /// <param name="partNumber">The part number to look up; defaults to what the person typed.</param>
    /// <param name="cancellationToken">A token to cancel the lookups.</param>
    /// <returns>The part, or <see langword="null"/> when no system or part number was given.</returns>
    /// <remarks>
    /// A part number typed by hand is enough to picture a part the application cannot list, which is deliberate:
    /// the gap in this feature is in which parts can be <em>listed</em>, never in which can be pictured.
    /// </remarks>
    public async Task<PartPictureRow?> FindPartAsync(string? partNumber = null, CancellationToken cancellationToken = default)
    {
        var system = SelectedSystem?.System ?? PartPictureSystem.Visual;
        var normalized = (partNumber ?? PartNumberInput ?? string.Empty).Trim();

        PartNumberInput = normalized;
        ChangeRecord.Clear();
        NotifyChangeRecordChanged();
        ClearRefusal();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            CurrentPart = null;
            ErrorMessage = "Settings_PartPictures_Refusal_NotAPart.Text".GetLocalized();
            StatusMessage = string.Empty;
            return null;
        }

        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;
        IsBusy = true;

        try
        {
            var scope = system.ToScopeString();
            var resolved = await _resolver.ResolvePartPicturePathAsync(scope, normalized, cancellationToken).ConfigureAwait(true);

            var part = new PartPictureRow
            {
                Scope = scope,
                PartNumber = normalized,
                ImagePath = resolved ?? ImagePicturePolicy.NoImagePath,
            };

            CurrentPart = part;

            var record = await _writeService.GetChangeRecordAsync(system, normalized, cancellationToken).ConfigureAwait(true);
            foreach (var entry in record)
            {
                ChangeRecord.Add(ToDisplay(entry));
            }

            NotifyChangeRecordChanged();
            return part;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Stores the chosen picture for the part in front of the person and reads the record back.
    /// </summary>
    /// <param name="sourceFilePath">The picture the person chose.</param>
    /// <param name="userId">The person making the change, for the record.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> when the picture was stored and recorded.</returns>
    /// <remarks>
    /// The refusal is shown, not thrown: a number past the storage's limit and a name another part already holds
    /// are both things the person can act on, and both are stated in their own words.
    /// </remarks>
    public async Task<bool> ApplyPictureAsync(string sourceFilePath, long? userId = null, CancellationToken cancellationToken = default)
    {
        SourceFilePath = sourceFilePath ?? string.Empty;
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;
        ClearRefusal();

        if (string.IsNullOrWhiteSpace(SourceFilePath))
        {
            ErrorMessage = "Settings_PartPictures_Refusal_NotAPart.Text".GetLocalized();
            return false;
        }

        var system = SelectedSystem?.System ?? PartPictureSystem.Visual;
        var partNumber = (PartNumberInput ?? string.Empty).Trim();

        IsBusy = true;
        try
        {
            var result = await _writeService
                .SetPictureAsync(system, partNumber, SourceFilePath, userId, cancellationToken)
                .ConfigureAwait(true);

            if (!result.Success)
            {
                ErrorMessage = DescribeRefusal(result);
                return false;
            }

            // The screen re-reads rather than assuming, so the picture it shows is the one the store now holds and
            // the record is the one the store recorded. The re-read clears the messages, so the confirmation is
            // written after it rather than before, or the person would never see that the save worked.
            await FindPartAsync(result.PartNumber.Length > 0 ? result.PartNumber : partNumber, cancellationToken).ConfigureAwait(true);
            StatusMessage = "Settings_PartPictures_Saved.Text".GetLocalized();
            return true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Reads the parts the application can name that still have no picture.</summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    public async Task LoadMissingPartsAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var rows = await _coverageService.GetMissingPartsAsync(cancellationToken).ConfigureAwait(true);

            MissingParts.Clear();
            foreach (var row in rows)
            {
                MissingParts.Add(row);
            }

            NotifyMissingPartsChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The parts with no picture could not be listed.");
            MissingParts.Clear();
            NotifyMissingPartsChanged();
            ErrorMessage = "Settings_PartPictures_Unavailable.Text".GetLocalized();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Records that the list was written to the file the person chose.</summary>
    /// <param name="writtenPartCount">How many parts the file holds.</param>
    public void NoteExportWritten(int writtenPartCount) =>
        StatusMessage = string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            "Settings_PartPictures_Export_Written.Text".GetLocalized(),
            writtenPartCount);

    /// <summary>Reports that the file could not be written, rather than leaving the screen saying nothing.</summary>
    /// <param name="reason">What the platform said, which is the part of the failure the person can act on.</param>
    public void ReportExportFailure(string reason)
    {
        StatusMessage = string.Empty;
        ErrorMessage = string.IsNullOrWhiteSpace(reason)
            ? "Settings_PartPictures_Unavailable.Text".GetLocalized()
            : reason;
    }

    [RelayCommand]
    private Task FindPart() => FindPartAsync();

    [RelayCommand]
    private Task SetPicture() => ApplyPictureAsync(SourceFilePath);

    [RelayCommand]
    private Task LoadMissingParts() => LoadMissingPartsAsync();

    private static PartPictureChangeDisplay ToDisplay(PartPictureChangeEntry entry)
    {
        var what = entry.IsFirstPicture
            ? "Settings_PartPictures_History_FirstPicture.Text".GetLocalized()
            : "Settings_PartPictures_History_Replaced.Text".GetLocalized();

        var who = string.IsNullOrWhiteSpace(entry.ChangedByDisplayName)
            ? entry.ChangedByUserId?.ToString(System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty
            : entry.ChangedByDisplayName;

        var when = entry.ChangedUtc == default
            ? string.Empty
            : entry.ChangedUtc.ToLocalTime().ToString("g", System.Globalization.CultureInfo.CurrentCulture);

        var detail = entry.IsFirstPicture
            ? entry.NewImagePath
            : $"{what} {entry.PreviousImagePath} → {entry.NewImagePath}";

        return new PartPictureChangeDisplay(
            Title: entry.IsFirstPicture ? what : $"{what}: {entry.NewImagePath}",
            Detail: string.Join(" · ", new[] { who, when, detail }.Where(part => !string.IsNullOrWhiteSpace(part))));
    }

    /// <summary>
    /// The refusal in the person's words, from the code the write path answered with, and the same refusal in the
    /// form the screen can reason about.
    /// </summary>
    /// <param name="result">The refused save.</param>
    /// <returns>A sentence the person can act on.</returns>
    private string DescribeRefusal(PartPictureSetResult result)
    {
        LastRefusalCode = result.ErrorCode;

        switch (result.ErrorCode)
        {
            case "PART_NUMBER_TOO_LONG":
                LastRefusalLimit = PartPictureLayout.MaxPartNumberLength;
                return string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    "Settings_PartPictures_Refusal_TooLong.Text".GetLocalized(),
                    PartPictureLayout.MaxPartNumberLength);

            case "NAME_COLLISION":
                LastRefusalPartNumber = result.CollidingPartNumber ?? result.PartNumber;
                return string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    "Settings_PartPictures_Refusal_Collision.Text".GetLocalized(),
                    LastRefusalPartNumber);

            case "PART_NUMBER_REQUIRED":
            case "PICTURE_REQUIRED":
            case "NAME_NOT_USABLE":
                return "Settings_PartPictures_Refusal_NotAPart.Text".GetLocalized();

            default:
                return string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? "Settings_PartPictures_Unavailable.Text".GetLocalized()
                    : result.ErrorMessage!;
        }
    }

    private void ClearRefusal()
    {
        LastRefusalCode = null;
        LastRefusalPartNumber = null;
        LastRefusalLimit = null;
    }

    /// <summary>
    /// The one line naming the systems this screen cannot list parts for.
    /// </summary>
    /// <returns>The note, or an empty string when every system can be listed.</returns>
    private string BuildCoverageNote()
    {
        var covered = _coverageService.CoveredSystems;
        var nameable = PartPictureLayout.PartScopes
            .Select(PartPictureSystemExtensions.TryFromScopeString)
            .Where(system => system.HasValue)
            .Select(system => system!.Value)
            .Where(system => !covered.Contains(system))
            .Select(PartPictureCoverageService.SystemNameFor)
            .ToList();

        return nameable.Count == 0
            ? string.Empty
            : string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                "Settings_PartPictures_Missing_NotListable.Text".GetLocalized(),
                string.Join(", ", nameable));
    }

    private void NotifyChangeRecordChanged()
    {
        OnPropertyChanged(nameof(HasChangeRecord));
        OnPropertyChanged(nameof(HistoryEmptyVisibility));
    }

    private void NotifyMissingPartsChanged()
    {
        OnPropertyChanged(nameof(HasMissingParts));
        OnPropertyChanged(nameof(MissingCountText));
        OnPropertyChanged(nameof(ExportCsv));
        OnPropertyChanged(nameof(MissingEmptyVisibility));
    }
}

/// <summary>One of the two systems, as the screen's chooser offers it.</summary>
public sealed class PartPictureSystemOption
{
    public PartPictureSystemOption(PartPictureSystem system, string label)
    {
        System = system;
        Label = label;
    }

    /// <summary>The system this option stands for.</summary>
    public PartPictureSystem System { get; }

    /// <summary>What the person reads.</summary>
    public string Label { get; }

    /// <summary>What the list and the card bind to.</summary>
    public override string ToString() => Label;
}

/// <summary>One line of the change record, already written in the person's words.</summary>
/// <param name="Title">What happened.</param>
/// <param name="Detail">Who made the change, when, and what the picture was before it.</param>
public sealed record PartPictureChangeDisplay(string Title, string Detail);
