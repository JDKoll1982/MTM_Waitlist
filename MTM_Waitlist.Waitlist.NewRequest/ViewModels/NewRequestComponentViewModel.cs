using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

/// <summary>
/// Component step of the New Request wizard: it asks <b>which</b> component the operator needs and shows every
/// component the requesting job carries as a clickable card, instead of making them pick a part number out of a
/// drop-down list.
/// </summary>
/// <remarks>
/// <para>
/// The step is reached because the chosen Item's <b>stored configuration</b> declares an enumerated answer that
/// names the job's <c>component</c> list — read through
/// <see cref="RequestItemAnswerOptionsResolver.DeclaredJobListName"/> — and never because of the Item's identity
/// (FR-013). Both component rows declare exactly that, so `pickup-component` and `deliver-component` are served by
/// this one step, and a row added later that names the same list is served by it too.
/// </para>
/// <para>
/// One component is one answer, so a click records it and moves on — the dunnage step's behaviour rather than the
/// die step's, where several dies are several requests. The part number recorded becomes the request's one value,
/// which is what the card's second line reads back (FR-035).
/// </para>
/// <para>
/// The question is the <b>row's own</b> prompt, because the two component rows ask it differently — one collects,
/// one brings — and a step that hard-coded either wording would flatten a difference the configuration exists to
/// keep (FR-013, FR-022). The resource key is the fallback for a row whose prompt is blank.
/// </para>
/// </remarks>
public partial class NewRequestComponentViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;

    /// <summary>
    /// The reader this step resolves each component's picture through, before the card reaches the bound
    /// collection. Null on a headless host, which leaves every card on the one shared placeholder rather than
    /// drawing a blank space.
    /// </summary>
    private readonly IPartPictureResolver? _partPictureResolver;

    private NewRequestFlowState? _state;

    [ObservableProperty]
    public partial string WorkCenterText
    {
        get; set;
    } = string.Empty;

    /// <summary>The question the step asks — the row's own prompt, so each row keeps its wording (FR-013).</summary>
    [ObservableProperty]
    public partial string PromptText
    {
        get; set;
    } = LocalizeOrDefault("NewRequest_Component.Prompt", "Which component do you need?");

    /// <summary>True when the step has something to report — a job with no component on it.</summary>
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

    /// <summary>The components the requesting job carries, in the job's own order, one card each.</summary>
    public ObservableCollection<NewRequestComponentOption> Options { get; } = new();

    public NewRequestComponentViewModel(
        INavigationService navigationService,
        IPartPictureResolver? partPictureResolver = null)
    {
        _navigationService = navigationService;
        _partPictureResolver = partPictureResolver;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is not NewRequestFlowState state || state.Item is null)
        {
            _navigationService.GoBack();
            return;
        }

        _state = state;
        WorkCenterText = $"Work Center: {state.WorkCenter}";
        PromptText = ResolvePrompt(state);
        UnavailableMessage = string.Empty;
        IsUnavailableVisible = false;
        await LoadCardsAsync(state).ConfigureAwait(true);
    }

    public void OnNavigatedFrom()
    {
    }

    /// <summary>
    /// Binds the components the job carries as cards, marking the one already chosen so coming back to the step
    /// never hides what the operator picked. A job carrying no component leaves the step empty and says why rather
    /// than offering a card that stands for nothing (FR-026).
    /// </summary>
    /// <remarks>
    /// Each component's picture is resolved here, where the resolver is in hand, and carried on the card: the card
    /// draws a value it was handed rather than asking for one while it renders (FR-018).
    /// </remarks>
    private async Task LoadCardsAsync(NewRequestFlowState state)
    {
        Options.Clear();

        foreach (var partNumber in state.Availability?.ComponentPartNumbers ?? Array.Empty<string>())
        {
            var trimmed = partNumber.Trim();
            Options.Add(new NewRequestComponentOption
            {
                PartNumber = trimmed,
                Title = trimmed,
                ImagePath = await ResolvePartPictureAsync(trimmed).ConfigureAwait(true),
                IsSelected = !string.IsNullOrWhiteSpace(state.InputValue)
                    && string.Equals(trimmed, state.InputValue.Trim(), StringComparison.OrdinalIgnoreCase),
            });
        }

        if (Options.Count == 0)
        {
            IsUnavailableVisible = true;
            UnavailableMessage = LocalizeOrDefault(
                "NewRequest_Component.NoComponents.Message",
                "This job has no components saved on it, so there is no component to choose. Go back and choose a different item.");
        }

        StartupDebugLog.Info(
            "NewRequestComponent",
            $"Component step for work center '{state.WorkCenter}' bound {Options.Count} card(s) for item '{state.Item?.Id}'.");
    }

    /// <summary>The part's picture, or the one shared placeholder when the part cannot be pictured (FR-014, FR-023).</summary>
    private async Task<string> ResolvePartPictureAsync(string partNumber)
    {
        if (_partPictureResolver is null || string.IsNullOrWhiteSpace(partNumber))
        {
            return ImagePicturePolicy.NoImagePath;
        }

        try
        {
            var resolved = await _partPictureResolver
                .ResolvePartPicturePathAsync(PartPictureLayout.VisualPartScope, partNumber)
                .ConfigureAwait(true);

            return string.IsNullOrWhiteSpace(resolved) ? ImagePicturePolicy.NoImagePath : resolved;
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "NewRequestComponent",
                ex,
                $"Resolving the picture for component '{partNumber}' failed; the card will draw the no-image placeholder.");
            return ImagePicturePolicy.NoImagePath;
        }
    }

    /// <summary>
    /// Records the component on the card the operator clicked and moves on. The part number becomes the request's
    /// answer, which is what the card's second line reads back.
    /// </summary>
    [RelayCommand]
    private void SelectComponent(NewRequestComponentOption? option)
    {
        if (_state is null || option is null)
        {
            return;
        }

        var partNumber = option.PartNumber?.Trim() ?? string.Empty;
        if (partNumber.Length == 0)
        {
            // A component with no part number cannot be the identifier the card shows, so it is refused rather
            // than captured as an empty answer (FR-026).
            IsUnavailableVisible = true;
            UnavailableMessage = LocalizeOrDefault(
                "NewRequest_Component.NoComponents.Message",
                "This job has no components saved on it, so there is no component to choose. Go back and choose a different item.");
            return;
        }

        var state = _state;
        state.InputValue = partNumber;

        foreach (var candidate in Options)
        {
            candidate.IsSelected = ReferenceEquals(candidate, option);
        }

        StartupDebugLog.Info(
            "NewRequestComponent",
            $"Component '{partNumber}' captured for work center '{state.WorkCenter}'.");
        _navigationService.NavigateTo(NewRequestFlowRules.GetNextStepType(state).FullName!, state);
    }

    [RelayCommand]
    private void Back() => _navigationService.GoBack();

    /// <summary>
    /// The question the step asks: the row's own prompt where it declares one, the resource otherwise.
    /// </summary>
    private static string ResolvePrompt(NewRequestFlowState state)
    {
        var configured = state.ItemConfiguration?.PromptText?.Trim();
        return string.IsNullOrWhiteSpace(configured)
            ? LocalizeOrDefault("NewRequest_Component.Prompt", "Which component do you need?")
            : configured;
    }

    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? fallback
            : localized;
    }
}
