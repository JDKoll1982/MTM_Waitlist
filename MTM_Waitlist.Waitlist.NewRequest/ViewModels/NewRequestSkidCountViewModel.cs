using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

/// <summary>
/// Skid count step of the New Request wizard (FR-087): the four returning pickups ask how many skids must be
/// picked up, as a numeric answer that defaults to one and can be changed.
/// </summary>
/// <remarks>
/// The step is reached because the chosen Item's <b>stored configuration</b> declares an answer labelled
/// <see cref="NewRequestFlowRules.SkidCountLabel"/>, and never because of the Item's identity (FR-013). The count
/// is what the card's <c># of Skids</c> cell reads, beside the derived <c>Part Type</c>.
/// </remarks>
public partial class NewRequestSkidCountViewModel : ObservableRecipient, INavigationAware
{
    /// <summary>
    /// The count the step starts on. One is what the spec settles on, so accepting the step unchanged is a real
    /// answer rather than an empty one (FR-087).
    /// </summary>
    public const string DefaultCount = "1";

    private readonly INavigationService _navigationService;

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
    } = LocalizeOrDefault("NewRequest_SkidCount.Prompt", "How many skids must be picked up?");

    /// <summary>What the operator typed, starting at the default of one.</summary>
    [ObservableProperty]
    public partial string SkidCountText
    {
        get; set;
    } = DefaultCount;

    /// <summary>True when the answer was refused, so the step can say why.</summary>
    [ObservableProperty]
    public partial bool IsValidationVisible
    {
        get; set;
    }

    /// <summary>The plain-language refusal, resolved through the resource mechanism — never a bare resource key.</summary>
    [ObservableProperty]
    public partial string ValidationMessage
    {
        get; set;
    } = string.Empty;

    public NewRequestSkidCountViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
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
        SkidCountText = state.AnswerFor(NewRequestFlowRules.SkidCountLabel) ?? DefaultCount;
        ValidationMessage = string.Empty;
        IsValidationVisible = false;
    }

    public void OnNavigatedFrom()
    {
    }

    /// <summary>
    /// Records the count and moves on. A blank answer is the default of one rather than a refusal, because one is
    /// what the step already answers; a count that is not a whole number above zero is refused (FR-087).
    /// </summary>
    [RelayCommand]
    private void Continue()
    {
        if (_state is null)
        {
            return;
        }

        if (!NewRequestFlowRules.DeclaresAnswerLabel(_state.ItemConfiguration, NewRequestFlowRules.SkidCountLabel))
        {
            Refuse(
                "NewRequest_SkidCount_NotConfigured.Message",
                "This item's skid count question is not configured yet. A supervisor needs to check it.");
            return;
        }

        var typed = SkidCountText?.Trim() ?? string.Empty;
        if (typed.Length == 0)
        {
            Capture(DefaultCount);
            return;
        }

        if (!int.TryParse(typed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count <= 0)
        {
            Refuse(
                "NewRequest_SkidCount_WholeNumberRequired.Message",
                "Enter how many skids are being picked up, as a whole number.");
            return;
        }

        Capture(count.ToString(CultureInfo.InvariantCulture));
    }

    [RelayCommand]
    private void Back() => _navigationService.GoBack();

    private void Capture(string count)
    {
        if (_state is null)
        {
            return;
        }

        var state = _state;
        state.RecordAnswer(NewRequestFlowRules.SkidCountLabel, count);
        ValidationMessage = string.Empty;
        IsValidationVisible = false;

        AppLog.Info(
            "NewRequestSkidCount",
            $"Skid count '{count}' captured for work center '{state.WorkCenter}'.");

        _navigationService.NavigateTo(NewRequestFlowRules.GetNextStepType(state).FullName!, state);
    }

    private void Refuse(string key, string fallback)
    {
        ValidationMessage = LocalizeOrDefault(key, fallback);
        IsValidationVisible = true;
    }

    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? fallback
            : localized;
    }
}
