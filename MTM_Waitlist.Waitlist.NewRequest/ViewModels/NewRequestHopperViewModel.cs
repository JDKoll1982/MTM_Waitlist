using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

/// <summary>
/// Hopper-size step of the New Request wizard (FR-077): it asks for the size as three choices — Small, Medium and
/// Large — and draws the chosen size's reference image beneath them.
/// </summary>
/// <remarks>
/// <para>
/// The step is reached because the chosen Item's <b>stored configuration</b> declares an answer labelled
/// <see cref="NewRequestFlowRules.HopperSizeLabel"/>, and never because of the Item's identity (FR-013). Both
/// hopper Items declare exactly that, so one step serves both.
/// </para>
/// <para>
/// The reference image is held to the application's existing picture rules and is resolved through the same reader
/// every other New Request surface draws a picture with (FR-079). A size whose image is missing, unreadable, not
/// square, too small, too large or not a permitted file type draws the one shared placeholder rather than a blank
/// (FR-080).
/// </para>
/// <para>
/// <b>Where a size's image comes from.</b> The step asks the picture reader for the picture filed under the size's
/// own name in the hopper folder — <c>Waitlist/Hoppers/Small.png</c>, and so on — so a reference image per size is
/// configured in one known place, named after the size word. The folder, the three size words and the file names
/// all come from <see cref="PartPictureLayout"/>, which is also what the card reads, so the step and the card
/// cannot ask for two different pictures for one size (D22, FR-078). The naming contract is documented in
/// <c>docs/IMAGE-NAMING.md</c>.
/// </para>
/// </remarks>
public partial class NewRequestHopperViewModel : ObservableRecipient, INavigationAware
{
    /// <summary>The smallest hopper size, as the card's <c>Size</c> cell spells it.</summary>
    public const string SmallSize = "Small";

    /// <summary>The middle hopper size, as the card's <c>Size</c> cell spells it.</summary>
    public const string MediumSize = "Medium";

    /// <summary>The largest hopper size, as the card's <c>Size</c> cell spells it.</summary>
    public const string LargeSize = "Large";

    /// <summary>
    /// The rules a hopper's reference image is held to, which are the application's existing picture rules rather
    /// than a second set written for this step (FR-079).
    /// </summary>
    private static readonly ImageValidationRules s_pictureRules = new();

    private readonly INavigationService _navigationService;

    /// <summary>
    /// The reader each size's reference image is resolved through. Null on a headless host, which leaves every size
    /// on the one shared placeholder rather than drawing a blank space.
    /// </summary>
    private readonly IPartPictureResolver? _partPictureResolver;

    /// <summary>The reference image resolved for each size, keyed by the size word.</summary>
    private readonly Dictionary<string, string> _pictures = new(StringComparer.OrdinalIgnoreCase);

    private NewRequestFlowState? _state;

    /// <summary>The three sizes the step offers, in the order it offers them (FR-077).</summary>
    public IReadOnlyList<string> Sizes { get; } = [SmallSize, MediumSize, LargeSize];

    [ObservableProperty]
    public partial string WorkCenterText
    {
        get; set;
    } = string.Empty;

    /// <summary>The question the step asks, resolved through the resource mechanism (FR-022).</summary>
    [ObservableProperty]
    public partial string PromptText
    {
        get; set;
    } = LocalizeOrDefault("NewRequest_Hopper.Prompt", "Which size hopper do you need?");

    /// <summary>The chosen size, or empty when nothing has been chosen.</summary>
    [ObservableProperty]
    public partial string SelectedSize
    {
        get; set;
    } = string.Empty;

    /// <summary>The chosen size's reference image, or the one shared placeholder (FR-077, FR-080).</summary>
    [ObservableProperty]
    public partial string SelectedImagePath
    {
        get; set;
    } = ImagePicturePolicy.NoImagePath;

    /// <summary>Whether the smallest size is the chosen one — drives that radio button.</summary>
    [ObservableProperty]
    public partial bool IsSmallSelected
    {
        get; set;
    }

    /// <summary>Whether the middle size is the chosen one — drives that radio button.</summary>
    [ObservableProperty]
    public partial bool IsMediumSelected
    {
        get; set;
    }

    /// <summary>Whether the largest size is the chosen one — drives that radio button.</summary>
    [ObservableProperty]
    public partial bool IsLargeSelected
    {
        get; set;
    }

    /// <summary>True when the step has something to report — a refused continue, or a row that declares no size.</summary>
    [ObservableProperty]
    public partial bool IsUnavailableVisible
    {
        get; set;
    }

    /// <summary>The plain-language report, resolved through the resource mechanism — never a bare resource key.</summary>
    [ObservableProperty]
    public partial string UnavailableMessage
    {
        get; set;
    } = string.Empty;

    public NewRequestHopperViewModel(
        INavigationService navigationService,
        IPartPictureResolver? partPictureResolver = null)
    {
        _navigationService = navigationService;
        _partPictureResolver = partPictureResolver;
    }

    /// <summary>The reference image resolved for a size, or the one shared placeholder (FR-080).</summary>
    /// <param name="size">The size word.</param>
    public string ImageFor(string? size) =>
        !string.IsNullOrWhiteSpace(size) && _pictures.TryGetValue(size.Trim(), out var picture)
            ? picture
            : ImagePicturePolicy.NoImagePath;

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is not NewRequestFlowState state || state.Item is null)
        {
            _navigationService.GoBack();
            return;
        }

        _state = state;
        WorkCenterText = $"Work Center: {state.WorkCenter}";
        UnavailableMessage = string.Empty;
        IsUnavailableVisible = false;

        SelectedSize = state.AnswerFor(NewRequestFlowRules.HopperSizeLabel) is { } captured
            && Sizes.Contains(captured, StringComparer.OrdinalIgnoreCase)
                ? Sizes.First(size => string.Equals(size, captured, StringComparison.OrdinalIgnoreCase))
                : string.Empty;
        SyncChoices();

        await LoadPicturesAsync().ConfigureAwait(true);
        SelectedImagePath = ImageFor(SelectedSize);

        if (!NewRequestFlowRules.DeclaresAnswerLabel(state.ItemConfiguration, NewRequestFlowRules.HopperSizeLabel))
        {
            // A row that does not declare the size has nothing for this step to answer into, so the step says so
            // rather than capturing a value no cell reads (FR-013).
            Refuse(
                "NewRequest_Hopper_NotConfigured.Message",
                "This item's size question is not configured yet. A supervisor needs to check it.");
        }
    }

    public void OnNavigatedFrom()
    {
    }

    /// <summary>
    /// Records the chosen size and moves on. Nothing chosen is refused rather than advanced, because the size is
    /// the whole answer this step exists for (FR-077).
    /// </summary>
    [RelayCommand]
    private void Continue()
    {
        if (_state is null)
        {
            return;
        }

        var chosen = Sizes.FirstOrDefault(size => string.Equals(size, SelectedSize, StringComparison.OrdinalIgnoreCase));
        if (chosen is null)
        {
            Refuse("NewRequest_Hopper_NothingChosen.Message", "Choose a size before continuing.");
            return;
        }

        var state = _state;
        state.RecordAnswer(NewRequestFlowRules.HopperSizeLabel, chosen);
        ClearRefusal();

        AppLog.Info(
            "NewRequestHopper",
            $"Hopper size '{chosen}' captured for work center '{state.WorkCenter}'.");

        _navigationService.NavigateTo(NewRequestFlowRules.GetNextStepType(state).FullName!, state);
    }

    [RelayCommand]
    private void Back() => _navigationService.GoBack();

    /// <summary>
    /// Resolves the reference image of every size as the step loads, so choosing one draws immediately rather than
    /// waiting for a read while the operator is looking at the step.
    /// </summary>
    private async Task LoadPicturesAsync()
    {
        _pictures.Clear();

        foreach (var size in Sizes)
        {
            _pictures[size] = await ResolveSizePictureAsync(size).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// A size's reference image, or the one shared placeholder when the size cannot be pictured — which is also
    /// what happens when the picture does not meet the application's own picture rules (FR-079, FR-080).
    /// </summary>
    /// <param name="size">The size word, which is the name the picture is filed under.</param>
    private async Task<string> ResolveSizePictureAsync(string size)
    {
        if (_partPictureResolver is null)
        {
            return ImagePicturePolicy.NoImagePath;
        }

        try
        {
            var resolved = await _partPictureResolver
                .ResolvePartPicturePathAsync(PartPictureLayout.HopperSizeScope, size)
                .ConfigureAwait(true);

            return IsAcceptedHopperPicture(resolved) ? resolved! : ImagePicturePolicy.NoImagePath;
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "NewRequestHopper",
                ex,
                $"Resolving the reference image for the '{size}' hopper size failed; the step will draw the no-image placeholder.");
            return ImagePicturePolicy.NoImagePath;
        }
    }

    /// <summary>
    /// Whether a resolved path is a hopper reference image the application will draw: a picture at all, of a
    /// permitted file type, between one kilobyte and ten megabytes, and no more than
    /// <see cref="ImageValidationRules.MaxDimensionPixels"/> pixels a side (FR-079).
    /// </summary>
    /// <param name="path">What the picture reader answered, or null.</param>
    private static bool IsAcceptedHopperPicture(string? path)
    {
        var file = ImagePicturePolicy.ResolveExistingFile(path);
        if (file is null || !ImagePicturePolicy.IsUsable(file))
        {
            return false;
        }

        var picture = new FileInfo(file);
        if (picture.Length < s_pictureRules.MinFileSizeBytes || picture.Length > s_pictureRules.MaxFileSizeBytes)
        {
            return false;
        }

        return s_pictureRules.AllowedExtensions.Contains(picture.Extension.TrimStart('.'))
            && ImagePicturePolicy.TryReadSize(file, out var width, out var height)
            && width <= s_pictureRules.MaxDimensionPixels
            && height <= s_pictureRules.MaxDimensionPixels;
    }

    /// <summary>
    /// Marks the radio for the size already chosen, so coming back to the step shows what was picked.
    /// </summary>
    private void SyncChoices()
    {
        IsSmallSelected = string.Equals(SelectedSize, SmallSize, StringComparison.OrdinalIgnoreCase);
        IsMediumSelected = string.Equals(SelectedSize, MediumSize, StringComparison.OrdinalIgnoreCase);
        IsLargeSelected = string.Equals(SelectedSize, LargeSize, StringComparison.OrdinalIgnoreCase);
    }

    partial void OnIsSmallSelectedChanged(bool value) => ChooseIfChosen(SmallSize, value);

    partial void OnIsMediumSelectedChanged(bool value) => ChooseIfChosen(MediumSize, value);

    partial void OnIsLargeSelectedChanged(bool value) => ChooseIfChosen(LargeSize, value);

    partial void OnSelectedSizeChanged(string value) => SelectedImagePath = ImageFor(value);

    /// <summary>
    /// Takes a radio being turned on as the choice. Turning one off is not a choice: a radio group turns the
    /// previous one off only because another was chosen, and that other one arrives here as the new choice.
    /// </summary>
    private void ChooseIfChosen(string size, bool chosen)
    {
        if (chosen && !string.Equals(SelectedSize, size, StringComparison.Ordinal))
        {
            SelectedSize = size;
        }

        ClearRefusal();
    }

    private void Refuse(string key, string fallback)
    {
        UnavailableMessage = LocalizeOrDefault(key, fallback);
        IsUnavailableVisible = true;
    }

    private void ClearRefusal()
    {
        IsUnavailableVisible = false;
        UnavailableMessage = string.Empty;
    }

    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? fallback
            : localized;
    }
}
