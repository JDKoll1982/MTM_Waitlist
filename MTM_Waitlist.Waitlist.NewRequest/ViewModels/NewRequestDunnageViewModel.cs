using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

/// <summary>
/// Dunnage step of the New Request wizard (FR-048): it asks <b>which</b> dunnage the operator actually needs from
/// the material handlers, showing the parts the requesting job has assigned as picture cards, and offers the
/// substitute picker for a part the job does not carry (FR-049).
/// </summary>
/// <remarks>
/// <para>
/// The step is reached because the chosen Item's <b>stored configuration</b> declares an enumerated answer that
/// names the job's <c>dunnage</c> list — read through
/// <see cref="RequestItemAnswerOptionsResolver.DeclaredJobListName"/> — and never because of the Item's identity
/// (FR-013).
/// </para>
/// <para>
/// The part the operator ends on is captured as the request's one answer, so the card's second line can read it
/// back for an assigned part and for a substitute alike (FR-051). Nothing is chosen on the operator's behalf: the
/// job's list is offered, they say which one they need.
/// </para>
/// </remarks>
public partial class NewRequestDunnageViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly IDunnageSubstitutePicker _substitutePicker;

    private NewRequestFlowState? _state;

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
    } = LocalizeOrDefault("NewRequest_Dunnage.Prompt", "Which dunnage do you need from the material handlers?");

    /// <summary>True when the step has something to report — no assigned parts, or a picker that failed.</summary>
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

    /// <summary>True while the substitute picker is open, so the step cannot be driven twice at once.</summary>
    [ObservableProperty]
    public partial bool IsBusy
    {
        get; set;
    }

    /// <summary>The parts this job has assigned, with the operator's substitute first when one was chosen.</summary>
    public ObservableCollection<NewRequestDunnageOption> Options { get; } = new();

    public NewRequestDunnageViewModel(INavigationService navigationService, IDunnageSubstitutePicker substitutePicker)
    {
        _navigationService = navigationService;
        _substitutePicker = substitutePicker;
    }

    public void OnNavigatedTo(object parameter)
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
        LoadCards(state);
    }

    public void OnNavigatedFrom()
    {
    }

    /// <summary>
    /// Binds the job's assigned dunnage parts as cards, with the current choice marked. A substitute the operator
    /// already chose is shown again as the first card even though the job does not carry it, so coming back to this
    /// step never hides what they picked.
    /// </summary>
    private void LoadCards(NewRequestFlowState state)
    {
        Options.Clear();

        var chosen = state.SelectedDunnagePart;
        if (chosen is not null && !IsAmongAssigned(chosen, state))
        {
            Options.Add(CreateOption(chosen, chosen.PartNumber));
        }

        foreach (var part in state.Availability?.DunnageParts ?? Array.Empty<RequestDunnagePart>())
        {
            Options.Add(CreateOption(part, state.InputValue));
        }

        if (Options.Count == 0)
        {
            // The Item is only offered to a job that has dunnage, so this is a defensive report rather than the
            // expected path — and the substitute picker stays reachable, which is the one useful thing left to do
            // here (FR-026).
            IsUnavailableVisible = true;
            UnavailableMessage = LocalizeOrDefault(
                "NewRequest_Dunnage.NoAssignedParts.Message",
                "This job has no dunnage saved on it. Use a substitute if you need one, or go back and choose a different item.");
        }

        StartupDebugLog.Info(
            "NewRequestDunnage",
            $"Dunnage step for work center '{state.WorkCenter}' bound {Options.Count} card(s) for item '{state.Item?.Id}'.");
    }

    /// <summary>
    /// Records the part on the card the operator clicked and moves on. The part becomes the request's answer, which
    /// is what the card's second line reads back.
    /// </summary>
    [RelayCommand]
    private void SelectPart(NewRequestDunnageOption? option)
    {
        if (_state is null || option?.Part is null)
        {
            return;
        }

        CaptureAndAdvance(option.Part, option);
    }

    /// <summary>
    /// Opens the receiving dunnage catalogue so a part the job does not carry can be used instead (FR-049).
    /// A dismissed picker changes nothing: the step stays exactly where it was, with whatever was chosen before.
    /// </summary>
    [RelayCommand]
    private async Task UseSubstituteAsync()
    {
        if (_state is null || IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var substitute = await _substitutePicker.PickSubstituteAsync().ConfigureAwait(true);
            if (substitute is null)
            {
                return;
            }

            CaptureAndAdvance(substitute, option: null);
        }
        catch (Exception ex)
        {
            // A picker that could not open is reported rather than swallowed, and the assigned cards stay usable.
            StartupDebugLog.Error("NewRequestDunnage", ex, "The substitute dunnage picker failed to open.");
            IsUnavailableVisible = true;
            UnavailableMessage = LocalizeOrDefault(
                "NewRequest_Dunnage.SubstituteFailed.Message",
                "The dunnage list could not be opened. Try again, or choose one of the parts listed here.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Back() => _navigationService.GoBack();

    private void CaptureAndAdvance(RequestDunnagePart part, NewRequestDunnageOption? option)
    {
        if (_state is null)
        {
            return;
        }

        var partNumber = part.PartNumber?.Trim() ?? string.Empty;
        if (partNumber.Length == 0)
        {
            // A part with no number cannot be the identifier the card shows, so it is refused rather than
            // captured as an empty answer (FR-026).
            IsUnavailableVisible = true;
            UnavailableMessage = LocalizeOrDefault(
                "NewRequest_Dunnage.SubstituteFailed.Message",
                "The dunnage list could not be opened. Try again, or choose one of the parts listed here.");
            return;
        }

        var state = _state;
        state.InputValue = partNumber;
        state.SelectedDunnagePart = part;

        foreach (var candidate in Options)
        {
            candidate.IsSelected = ReferenceEquals(candidate, option);
        }

        if (option is null)
        {
            // A substitute is not one of the job's cards, so it is shown as the chosen card instead of being
            // silently dropped before the flow moves on.
            Options.Insert(0, CreateOption(part, partNumber));
        }

        StartupDebugLog.Info(
            "NewRequestDunnage",
            $"Dunnage part '{partNumber}' captured for work center '{state.WorkCenter}' (substitute={option is null}).");
        _navigationService.NavigateTo(NewRequestFlowRules.GetNextStepType(state).FullName!, state);
    }

    private static bool IsAmongAssigned(RequestDunnagePart part, NewRequestFlowState state) =>
        (state.Availability?.DunnageParts ?? Array.Empty<RequestDunnagePart>())
            .Any(candidate => string.Equals(candidate.PartNumber?.Trim(), part.PartNumber?.Trim(), StringComparison.OrdinalIgnoreCase));

    private static NewRequestDunnageOption CreateOption(RequestDunnagePart part, string? chosenPartNumber) => new()
    {
        Part = part,
        Title = string.IsNullOrWhiteSpace(part.DisplayName) ? part.PartNumber : part.DisplayName,
        Summary = part.Summary,
        ImagePath = part.ImagePath,
        IsSelected = !string.IsNullOrWhiteSpace(chosenPartNumber)
            && string.Equals(part.PartNumber?.Trim(), chosenPartNumber.Trim(), StringComparison.OrdinalIgnoreCase),
    };

    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? fallback
            : localized;
    }
}
