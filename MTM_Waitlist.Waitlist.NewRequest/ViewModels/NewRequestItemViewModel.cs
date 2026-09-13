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
/// Item step of the New Request wizard (US1): the third step, between the Category step and Details. It binds the
/// Items the requesting job actually supports — built already filtered, in catalog Order, so an unsupported Item
/// is never constructed, never bound and never offered (FR-002) — and it reads each Item's stored configuration
/// once, as the step is entered, rather than per row render (FR-013, FR-024).
/// </summary>
/// <remarks>
/// Nothing here branches on the Item's identity. Which step follows is decided by the chosen Item's stored
/// configuration through <see cref="NewRequestFlowRules.GetNextStepType"/>, and a catalogued Item with no usable
/// configuration row stops the flow with a plain-language report instead of proceeding half-configured
/// (FR-014, FR-026).
/// </remarks>
public partial class NewRequestItemViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly INewRequestFlowService _flowService;
    private readonly IRequestItemConfigurationService _configurationService;

    private NewRequestFlowState? _state;

    /// <summary>
    /// The configuration rows read once when this step was entered. Held so choosing an Item can record the
    /// Item together with the behaviour its row declares, without a second read per selection (FR-013, FR-024).
    /// </summary>
    private RequestItemConfigurationSet _configurations =
        RequestItemConfigurationSet.From(Array.Empty<RequestItemConfiguration>());

    [ObservableProperty]
    public partial string WorkCenterText
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string CategoryText
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string PromptText
    {
        get; set;
    } = ResolveItemPrompt();

    [ObservableProperty]
    public partial bool IsLoading
    {
        get; set;
    }

    /// <summary>True when the chosen Item cannot be used and the flow has been stopped to say so.</summary>
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

    [ObservableProperty]
    public partial NewRequestItemOption? SelectedItem
    {
        get; set;
    }

    /// <summary>The Items this job supports under the chosen Category, in catalog Order.</summary>
    public ObservableCollection<NewRequestItemOption> Items { get; } = new();

    public NewRequestItemViewModel(
        INavigationService navigationService,
        INewRequestFlowService flowService,
        IRequestItemConfigurationService configurationService)
    {
        _navigationService = navigationService;
        _flowService = flowService;
        _configurationService = configurationService;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is not NewRequestFlowState state || state.Category is null)
        {
            _navigationService.GoBack();
            return;
        }

        _state = state;
        WorkCenterText = $"Work Center: {state.WorkCenter}";
        CategoryText = ResolveCategoryName(state.Category.Value);
        SelectedItem = null;
        IsUnavailableVisible = false;
        UnavailableMessage = string.Empty;
        await LoadItemsAsync(state).ConfigureAwait(true);
    }

    public void OnNavigatedFrom()
    {
    }

    private async Task LoadItemsAsync(NewRequestFlowState state)
    {
        IsLoading = true;
        try
        {
            var category = state.Category!.Value;

            // Read once, as the step is entered — never per row render, never per keystroke (FR-013, FR-024).
            var configurations = await _configurationService.GetConfigurationsAsync().ConfigureAwait(true);
            _configurations = configurations;

            Items.Clear();
            foreach (var item in _flowService.GetVisibleItems(category, state.Availability))
            {
                var configuration = configurations.Get(item.Id);
                Items.Add(new NewRequestItemOption
                {
                    Item = item,
                    DisplayName = ResolveItemName(item),
                    Category = item.Category,
                    CapturesAnswer = configuration.RequiresAnswer,
                    IsConfigured = configuration.IsAvailable,
                    Summary = configuration.IsAvailable
                        ? ResolveSummary(configuration)
                        : configuration.UnavailableMessage,
                });
            }

            // The availability pass guarantees at least one Item (FR-002). If that guarantee were ever broken
            // the step says so in plain language rather than rendering an empty grid with no explanation.
            if (Items.Count == 0)
            {
                IsUnavailableVisible = true;
                UnavailableMessage = RequestItemConfigurationSet.ResolveNoItemsMessage();
            }

            StartupDebugLog.Info("NewRequestItem", $"Item step for category '{category}' on work center '{state.WorkCenter}' bound {Items.Count} item(s).");
        }
        catch (Exception ex)
        {
            // A read that failed is not an empty Category: the step says so rather than rendering nothing.
            StartupDebugLog.Error("NewRequestItem", ex, "Failed to load the Item step.");
            Items.Clear();
            IsUnavailableVisible = true;
            UnavailableMessage = RequestItemConfigurationSet.ResolveUnavailableMessage();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Records the choice. An Item with no usable configuration row stops the flow here with the report the
    /// configuration reader produced (FR-014), so the request can never be raised half-configured.
    /// </summary>
    [RelayCommand]
    private void SelectItem(NewRequestItemOption? option)
    {
        if (_state is null || option?.Item is null)
        {
            return;
        }

        SelectedItem = option;

        if (!option.IsConfigured)
        {
            _state.Item = null;
            _state.ItemConfiguration = null;
            UnavailableMessage = string.IsNullOrWhiteSpace(option.Summary)
                ? RequestItemConfigurationSet.ResolveUnavailableMessage()
                : option.Summary;
            IsUnavailableVisible = true;
            StartupDebugLog.Info("NewRequestItem", $"Item '{option.Item.Id}' has no usable configuration row; the flow was stopped instead of continuing.");
            return;
        }

        _state.Item = option.Item;
        _state.ItemConfiguration = _configurations.Get(option.Item.Id);
        IsUnavailableVisible = false;
        UnavailableMessage = string.Empty;
    }

    /// <summary>
    /// Advances to the step the Item's configuration asks for. Without a choice — or without a usable
    /// configuration row for it — the flow stays exactly where it is.
    /// </summary>
    [RelayCommand]
    private async Task ContinueAsync()
    {
        if (_state?.Item is null || _state.Category is null)
        {
            return;
        }

        var configuration = await _configurationService.GetConfigurationAsync(_state.Item.Id).ConfigureAwait(true);
        if (!configuration.IsAvailable)
        {
            SelectedItem = null;
            _state.Item = null;
            _state.ItemConfiguration = null;
            UnavailableMessage = configuration.UnavailableMessage;
            IsUnavailableVisible = true;
            return;
        }

        _state.ItemConfiguration = configuration;
        _state.InputValue = null;
        _navigationService.NavigateTo(NewRequestFlowRules.GetNextStepType(_state).FullName!, _state);
    }

    [RelayCommand]
    private void Back() => _navigationService.GoBack();

    private static string ResolveSummary(RequestItemConfiguration configuration)
    {
        if (!configuration.RequiresAnswer)
        {
            return "No further detail is needed for this item.";
        }

        var valueType = configuration.AnswerValueType switch
        {
            RequestItemValueType.Enum => "one of the listed options",
            RequestItemValueType.Text => "a short description",
            _ => "an answer",
        };

        return $"This item asks for {valueType}.";
    }

    /// <summary>
    /// The name a person reads for a Category, resolved through the resource mechanism with a readable fallback
    /// so a missing entry never shows a bare resource key (FR-022).
    /// </summary>
    public static string ResolveCategoryName(RequestCategory category)
    {
        var key = $"RequestCategory.{category}.Name";
        var localized = key.GetLocalized();
        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? category.ToString()
            : localized;
    }

    /// <summary>
    /// The name a person reads for an Item, resolved from the Item's own resource key (FR-022), falling back to
    /// the catalog's normalized name rather than to a bare key.
    /// </summary>
    public static string ResolveItemName(RequestItemDefinition item)
    {
        var key = string.IsNullOrWhiteSpace(item.DisplayNameResourceKey)
            ? RequestItemCatalog.DisplayNameResourceKeyFor(item.Id)
            : item.DisplayNameResourceKey;
        var localized = key.GetLocalized();
        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? item.NormalizedName
            : localized;
    }

    /// <summary>The Category step's prompt, resolved through the resource mechanism (FR-022).</summary>
    public static string ResolveCategoryPrompt() => ResolveMessage("NewRequest_Category.Prompt.Text", "Choose a request category to continue.");

    /// <summary>The Item step's prompt, resolved through the resource mechanism (FR-022).</summary>
    public static string ResolveItemPrompt() => ResolveMessage("NewRequest_Item.Prompt.Text", "Choose an item to continue.");

    private static string ResolveMessage(string resourceKey, string fallback)
    {
        var localized = resourceKey.GetLocalized();
        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, resourceKey, StringComparison.Ordinal)
            ? fallback
            : localized;
    }
}
