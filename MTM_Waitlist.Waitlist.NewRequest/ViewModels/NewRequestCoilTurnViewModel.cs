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
/// Assist coil turn step of the New Request wizard (FR-086): it asks for <c>Weight of Coil</c> as a required
/// numeric answer, and a blank answer prevents confirmation.
/// </summary>
/// <remarks>
/// <para>
/// The step is reached because the chosen Item's <b>stored configuration</b> declares an answer labelled
/// <see cref="NewRequestFlowRules.CoilWeightLabel"/>, and never because of the Item's identity (FR-013).
/// </para>
/// <para>
/// <b>This is not the job's average coil weight.</b> The requesting job's snapshot carries the average the
/// receiving store holds for the coil's part (<c>RequestJobPartAvailability.CoilAverageWeight</c>), and the card
/// already shows that average on the coil Items. What this step asks for is the weight of the coil the operator
/// is turning, which only the operator knows. The two are deliberately never read from one another (FR-054).
/// </para>
/// </remarks>
public partial class NewRequestCoilTurnViewModel : ObservableRecipient, INavigationAware
{
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
    } = LocalizeOrDefault("NewRequest_CoilTurn.Prompt", "What is the weight of the coil?");

    /// <summary>What the operator typed.</summary>
    [ObservableProperty]
    public partial string WeightText
    {
        get; set;
    } = string.Empty;

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

    public NewRequestCoilTurnViewModel(INavigationService navigationService)
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
        WeightText = state.AnswerFor(NewRequestFlowRules.CoilWeightLabel) ?? string.Empty;
        ValidationMessage = string.Empty;
        IsValidationVisible = false;
    }

    public void OnNavigatedFrom()
    {
    }

    /// <summary>
    /// Records the weight the operator supplied and moves on. A blank or non-numeric answer — or one that is not
    /// above zero — is refused on this step rather than confirmed (FR-086).
    /// </summary>
    [RelayCommand]
    private void Continue()
    {
        if (_state is null)
        {
            return;
        }

        if (!NewRequestFlowRules.DeclaresAnswerLabel(_state.ItemConfiguration, NewRequestFlowRules.CoilWeightLabel))
        {
            Refuse(
                "NewRequest_CoilTurn_NotConfigured.Message",
                "This item's coil weight question is not configured yet. A supervisor needs to check it.");
            return;
        }

        if (!decimal.TryParse(WeightText?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var weight)
            || weight <= 0)
        {
            Refuse(
                "NewRequest_CoilTurn_WeightRequired.Message",
                "Enter the weight of the coil as a number before continuing.");
            return;
        }

        var state = _state;
        state.RecordAnswer(NewRequestFlowRules.CoilWeightLabel, weight.ToString(CultureInfo.InvariantCulture));
        ValidationMessage = string.Empty;
        IsValidationVisible = false;

        AppLog.Info(
            "NewRequestCoilTurn",
            $"Weight of coil '{weight.ToString(CultureInfo.InvariantCulture)}' captured for work center '{state.WorkCenter}'.");

        _navigationService.NavigateTo(NewRequestFlowRules.GetNextStepType(state).FullName!, state);
    }

    [RelayCommand]
    private void Back() => _navigationService.GoBack();

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
