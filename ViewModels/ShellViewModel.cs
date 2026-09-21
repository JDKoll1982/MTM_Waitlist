using System.Collections.ObjectModel;
using System.ComponentModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Core.Views;
using MTM_Waitlist.Module_Settings.Views;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.Views;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Views;

using Windows.UI;

namespace MTM_Waitlist.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    private readonly IBuildingSelectionService _buildingSelectionService;
    private readonly IWaitlistSortPreferenceService _sortPreferenceService;
    private readonly ISignOutService _signOutService;
    private readonly SetupWorkflowState _setupWorkflowState;
    private readonly StartupState _startupState;
    private Type? _currentPageType;
    private bool _isWaitlistPageActive;

    private static readonly string[] s_setupStepLabelKeys =
    {
        "Setup_Header.Step1",
        "Setup_Header.Step2",
        "Setup_Header.Step3",
        "Setup_Header.Step4",
        "Setup_Header.Step5",
        "Setup_Header.Step6",
        "Setup_Header.Step7",
    };

    private static readonly string[] s_setupStepLabelFallbacks =
    {
        "Work Station",
        "Work Order",
        "Part",
        "Operation",
        "Dunnage & Scrap",
        "Review",
        "Result",
    };

    private static readonly string[] s_newRequestStepLabelKeys =
    {
        "NewRequest_Header.Step1",
        "NewRequest_Header.Step2",
        "NewRequest_Header.Step3",
        "NewRequest_Header.Step4",
        "NewRequest_Header.Step5",
        "NewRequest_Header.Step6",
        "NewRequest_Header.Step7",
    };

    private static readonly string[] s_newRequestStepLabelFallbacks =
    {
        "Work Center",
        "Category",
        "Item",
        "Details",
        "Preview",
        "Confirm",
        "Complete",
    };

    [ObservableProperty]
    public partial bool IsBackEnabled
    {
        get; set;
    }

    [ObservableProperty]
    public partial object? Selected
    {
        get; set;
    }

    [ObservableProperty]
    public partial string? SelectedBuilding
    {
        get; set;
    }

    [ObservableProperty]
    public partial string HeaderText
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsHeaderProgressVisible
    {
        get; set;
    }

    /// <summary>True while the Waitlist (list) page is the active view, so the header's My Requests filter is shown.</summary>
    [ObservableProperty]
    public partial bool IsMyRequestsVisible
    {
        get; set;
    }

    /// <summary>Header My Requests filter state (kept in sync with the active Waitlist view model).</summary>
    [ObservableProperty]
    public partial bool ShowMyRequestsOnly
    {
        get; set;
    }

    /// <summary>Label text reflecting the current filter: "My Requests" when filtering, else "All Requests".</summary>
    public string MyRequestsFilterLabel => ShowMyRequestsOnly ? "My Requests" : "All Requests";

    partial void OnShowMyRequestsOnlyChanged(bool value) => OnPropertyChanged(nameof(MyRequestsFilterLabel));

    /// <summary>
    /// The order the list is currently shown in, as one of the five <see cref="WaitlistSortOrder"/> keys
    /// (FR-011). It starts at the default the list itself starts at, and picks up the viewer's remembered
    /// choice from the list when that screen comes up.
    /// </summary>
    [ObservableProperty]
    public partial string SelectedSortOrder
    {
        get; set;
    } = WaitlistSortOrder.MostUrgent;

    /// <summary>Label of the control itself (FR-022).</summary>
    public string SortOrderLabel => "Shell_SortOrder.Label".GetLocalized();

    /// <summary>
    /// The five orders the control offers, in the order it offers them, each resolved through the resource
    /// mechanism (FR-011, FR-022).
    /// </summary>
    public string SortOrderMostUrgentLabel => "Shell_SortOrder.MostUrgent".GetLocalized();

    public string SortOrderLongestWaitingLabel => "Shell_SortOrder.LongestWaiting".GetLocalized();

    public string SortOrderPressLabel => "Shell_SortOrder.Press".GetLocalized();

    public string SortOrderRequestedByLabel => "Shell_SortOrder.RequestedBy".GetLocalized();

    public string SortOrderStatusLabel => "Shell_SortOrder.Status".GetLocalized();

    /// <summary>
    /// Applies the order the viewer just chose and remembers it for them (FR-011). The list itself is told by
    /// the shell page, because it is the page that holds the active waitlist view model.
    /// </summary>
    public void ApplySortOrder(string? sortOrder)
    {
        var normalized = WaitlistSortOrder.Normalize(sortOrder);
        if (string.Equals(SelectedSortOrder, normalized, StringComparison.Ordinal))
        {
            return;
        }

        SelectedSortOrder = normalized;
        _ = PersistSortOrderAsync(normalized);
    }

    /// <summary>
    /// Shows the order the list resolved for this viewer without writing it back — used when the list reports
    /// the choice it read, so the control opens on the option that is actually in force.
    /// </summary>
    public void SyncSortOrder(string? sortOrder)
    {
        var normalized = WaitlistSortOrder.Normalize(sortOrder);
        if (!string.Equals(SelectedSortOrder, normalized, StringComparison.Ordinal))
        {
            SelectedSortOrder = normalized;
        }
    }

    /// <summary>
    /// Remembers the choice for the viewer. A preference that cannot be written costs the viewer their choice
    /// next time, so it is reported rather than swallowed (FR-026); it never blocks the list.
    /// </summary>
    private async Task PersistSortOrderAsync(string sortOrder)
    {
        try
        {
            await _sortPreferenceService.SetSortOrderAsync(sortOrder);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("ShellViewModel", ex, $"The sort order '{sortOrder}' could not be remembered; the list is still ordered by it.");
        }
    }

    /// <summary>
    /// Ordered steps shown in the shell header stepper while a multi-step
    /// workflow (Work Center Setup or New Request) is active.
    /// </summary>
    public ObservableCollection<HeaderStep> HeaderSteps { get; } = new();

    [ObservableProperty]
    public partial string CurrentUserDisplayName
    {
        get; set;
    } = "Not signed in";

    [ObservableProperty]
    public partial string CurrentUserRole
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string CurrentUserIconGlyph
    {
        get; set;
    } = "\uE77B";

    [ObservableProperty]
    public partial Brush CurrentUserBadgeBrush
    {
        get; set;
    } = CreateUserBadgeBrush("#FF5C5C5C");

    /// <summary>
    /// The label of the badge flyout's Sign out entry (FR-022, FR-033). The fallback is readable text rather
    /// than the resource key, so a missing entry never shows the person a key.
    /// </summary>
    public string SignOutLabel => ResolveShellString("Shell_SignOut.Label", "Sign out");

    /// <summary>The Sign out entry's tooltip (FR-022).</summary>
    public string SignOutTooltip => ResolveShellString("Shell_SignOut.Tooltip", "Sign out and return to the sign-in screen.");

    private static string ResolveShellString(string resourceKey, string fallback)
    {
        var localized = resourceKey.GetLocalized();
        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, resourceKey, StringComparison.Ordinal)
            ? fallback
            : localized;
    }

    public IReadOnlyList<string> Buildings => _buildingSelectionService.Buildings;

    public INavigationService NavigationService
    {
        get;
    }

    public INavigationViewService NavigationViewService
    {
        get;
    }

    public ShellViewModel(
        INavigationService navigationService,
        INavigationViewService navigationViewService,
        IBuildingSelectionService buildingSelectionService,
        SetupWorkflowState setupWorkflowState,
        StartupState startupState,
        IWaitlistSortPreferenceService sortPreferenceService,
        ISignOutService signOutService)
    {
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(navigationViewService);
        ArgumentNullException.ThrowIfNull(buildingSelectionService);
        ArgumentNullException.ThrowIfNull(setupWorkflowState);
        ArgumentNullException.ThrowIfNull(startupState);
        ArgumentNullException.ThrowIfNull(sortPreferenceService);
        ArgumentNullException.ThrowIfNull(signOutService);

        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
        NavigationViewService = navigationViewService;
        _buildingSelectionService = buildingSelectionService;
        _sortPreferenceService = sortPreferenceService;
        _signOutService = signOutService;
        _setupWorkflowState = setupWorkflowState;
        _setupWorkflowState.PropertyChanged += OnSetupWorkflowStateChanged;
        _startupState = startupState;
        SelectedBuilding = _buildingSelectionService.SelectedBuilding;
        HeaderText = "MTM Waitlist";
        RefreshUserInfo();
    }

    /// <summary>
    /// Signs out from the badge (FR-033, FR-034): the service clears the remembered credential and the local
    /// session, relaunches the application and exits this process, so the person lands back at the sign-in
    /// screen. The badge's displayed name is deliberately <b>not</b> touched here — blanking it while the
    /// session persists is exactly what FR-034 rules out, and it is the service, not this method, that ends
    /// the session.
    /// </summary>
    /// <returns>
    /// The outcome, so the view can tell the person when the relaunch could not be performed rather than
    /// leaving them apparently signed in (FR-026).
    /// </returns>
    public Task<SignOutResult> SignOutAsync(CancellationToken cancellationToken = default)
    {
        // The replacement instance is started by the service; when it succeeds this process is on its way out.
        return _signOutService.SignOutAsync(cancellationToken);
    }

    public void RefreshUserInfo()
    {
        CurrentUserDisplayName = string.IsNullOrWhiteSpace(_startupState.Username)
            ? "Not signed in"
            : _startupState.Username;
        CurrentUserRole = _startupState.CurrentRole;
        var userPresentation = GetUserPresentation(_startupState.CurrentRole);
        CurrentUserIconGlyph = userPresentation.Glyph;
        CurrentUserBadgeBrush = CreateUserBadgeBrush(userPresentation.ColorHex);
    }

    private static (string Glyph, string ColorHex) GetUserPresentation(string? roleCode)
    {
        // Keyed on the role code through the one lookup, so every role the catalogue holds gets its own badge and
        // no role reaches the grey default (FR-105). The badge stays presentation: it is fixed rather than gated,
        // and it is not a permission (FR-058).
        var badge = RoleBadgeCatalog.For(roleCode);

        return (badge.Glyph, badge.ColorHex);
    }

    private static SolidColorBrush CreateUserBadgeBrush(string colorHex)
    {
        var hex = colorHex.TrimStart('#');
        var alpha = byte.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber);
        var red = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        var green = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        var blue = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
        return new SolidColorBrush(Color.FromArgb(alpha, red, green, blue));
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        _currentPageType = e.SourcePageType;
        IsBackEnabled = NavigationService.CanGoBack;
        IsMyRequestsVisible = e.SourcePageType == typeof(WaitlistViewPage);

        if (e.SourcePageType == typeof(WaitlistViewDetailPage))
        {
            _isWaitlistPageActive = false;
            Selected = null;
            UpdateWaitlistDetailHeader();
            HideHeaderProgress();
            return;
        }

        _isWaitlistPageActive = e.SourcePageType == typeof(WaitlistViewPage);

        if (_isWaitlistPageActive)
        {
            Selected = NavigationViewService.GetSelectedItem(e.SourcePageType);
            UpdateWaitlistHeader();
            HideHeaderProgress();
            return;
        }

        if (e.SourcePageType == typeof(SettingsPage))
        {
            StartupDebugLog.Info("ShellViewModel", "Settings page navigated to.");
            Selected = NavigationViewService.SettingsItem;
            HeaderText = "Settings";
            HideHeaderProgress();
            return;
        }

        if (e.SourcePageType.Namespace?.StartsWith("MTM_Waitlist.Module_Setup.Views", StringComparison.Ordinal) == true)
        {
            Selected = NavigationViewService.GetSelectedItem(typeof(SetupWorkCenterPage));
            UpdateSetupHeader(e.SourcePageType);
            return;
        }

        // New Request wizard pages keep the shell header visible and step through a
        // changing title and progress stepper so the user always knows where they
        // are in the flow.
        if (e.SourcePageType.Namespace?.StartsWith("MTM_Waitlist.Module_Waitlist.Views", StringComparison.Ordinal) == true
            && e.SourcePageType.Name.StartsWith("NewRequest", StringComparison.Ordinal))
        {
            UpdateNewRequestHeader(e.SourcePageType);
            return;
        }

        var selectedItem = NavigationViewService.GetSelectedItem(e.SourcePageType);
        if (selectedItem != null)
        {
            Selected = selectedItem;
            if (selectedItem is ContentControl contentControl)
            {
                HeaderText = contentControl.Content?.ToString() ?? string.Empty;
            }
            else
            {
                HeaderText = string.Empty;
            }

            HideHeaderProgress();
            return;
        }

        HeaderText = string.Empty;
        HideHeaderProgress();
    }

    private void UpdateSetupHeader(Type pageType)
    {
        var (title, stepIndex) = GetSetupStep(pageType, _setupWorkflowState);
        HeaderText = title;
        UpdateHeaderProgress(s_setupStepLabelKeys, s_setupStepLabelFallbacks, stepIndex);
    }

    private void OnSetupWorkflowStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SetupWorkflowState.CurrentStep))
        {
            return;
        }

        if (_currentPageType?.Namespace?.StartsWith("MTM_Waitlist.Module_Setup.Views", StringComparison.Ordinal) != true)
        {
            return;
        }

        // The Work Order page hosts inline part/operation selection, so advancing
        // the workflow step (e.g. after a part is selected) must re-evaluate the
        // header's active step even though the page itself did not change.
        var dispatcherQueue = App.MainWindow?.DispatcherQueue;
        if (dispatcherQueue is not null)
        {
            _ = dispatcherQueue.TryEnqueue(() => UpdateSetupHeader(_currentPageType!));
        }
    }

    private void UpdateNewRequestHeader(Type pageType)
    {
        _isWaitlistPageActive = false;
        Selected = null;
        var (title, stepIndex) = GetNewRequestStep(pageType);
        HeaderText = title;
        UpdateHeaderProgress(s_newRequestStepLabelKeys, s_newRequestStepLabelFallbacks, stepIndex);
    }

    private static (string Title, int Step) GetSetupStep(Type pageType, SetupWorkflowState setupWorkflowState)
    {
        if (pageType == typeof(SetupWorkCenterPage))
        {
            return ("Work Center Setup — Select Work Station", 1);
        }

        if (pageType == typeof(SetupWorkOrderPage))
        {
            // The Work Order page hosts inline part/operation selection, so the
            // active step also depends on how far the workflow has progressed.
            return setupWorkflowState.CurrentStep >= SetupWorkflowStep.PartSelection
                ? ("Work Center Setup — Part", 3)
                : ("Work Center Setup — Work Order", 2);
        }

        if (pageType == typeof(SetupPartSelectionPage))
        {
            return ("Work Center Setup — Part", 3);
        }

        if (pageType == typeof(SetupSequenceSelectionPage))
        {
            return ("Work Center Setup — Operation", 4);
        }

        if (pageType == typeof(SetupDunnageTypePage))
        {
            return ("Work Center Setup — Dunnage & Scrap", 5);
        }

        if (pageType == typeof(SetupReviewPage))
        {
            return ("Work Center Setup — Review", 6);
        }

        if (pageType == typeof(SetupCompletionPage))
        {
            return ("Work Center Setup — Result", 7);
        }

        return ("Work Center Setup", 0);
    }

    private static (string Title, int Step) GetNewRequestStep(Type pageType)
    {
        if (pageType == typeof(NewRequestWorkCenterPage))
        {
            return ("New Request — Select Work Center", 1);
        }

        if (pageType == typeof(NewRequestJobTypePage))
        {
            return ("New Request — Category", 2);
        }

        if (pageType == typeof(NewRequestItemPage))
        {
            return ("New Request — Item", 3);
        }

        if (pageType == typeof(NewRequestDetailsPage))
        {
            return ("New Request — Details", 4);
        }

        if (pageType == typeof(NewRequestPreviewPage))
        {
            return ("New Request — Preview", 5);
        }

        if (pageType == typeof(NewRequestSummaryPage))
        {
            return ("New Request — Confirm", 6);
        }

        if (pageType == typeof(NewRequestResultPage))
        {
            return ("New Request — Complete", 7);
        }

        return ("New Request", 0);
    }

    private void UpdateHeaderProgress(string[] labelKeys, string[] fallbacks, int stepIndex)
    {
        HeaderSteps.Clear();
        for (var index = 0; index < labelKeys.Length; index++)
        {
            var localized = labelKeys[index].GetLocalized();
            var label = string.Equals(localized, labelKeys[index], StringComparison.Ordinal)
                ? fallbacks[index]
                : localized;
            var state = index + 1 < stepIndex
                ? HeaderStepState.Complete
                : index + 1 == stepIndex
                    ? HeaderStepState.Current
                    : HeaderStepState.Pending;
            HeaderSteps.Add(new HeaderStep
            {
                Label = label,
                State = state,
                StepNumber = index + 1,
                IsFirst = index == 0,
                IsLast = index == labelKeys.Length - 1,
                PreviousComplete = index > 0 && HeaderSteps[^1].State == HeaderStepState.Complete,
            });
        }

        IsHeaderProgressVisible = stepIndex >= 1 && stepIndex <= labelKeys.Length;
    }

    private void HideHeaderProgress()
    {
        HeaderSteps.Clear();
        IsHeaderProgressVisible = false;
    }

    private void UpdateWaitlistHeader()
    {
        HeaderText = string.IsNullOrWhiteSpace(SelectedBuilding)
            ? "Waitlist"
            : $"Waitlist for \"{SelectedBuilding}\"";
    }

    private void UpdateWaitlistDetailHeader()
    {
        if (NavigationService.Frame?.Content is not WaitlistViewDetailPage detailPage
            || detailPage.ViewModel.Item is not SampleOrder item)
        {
            HeaderText = "Details";
            return;
        }

        var jobType = item.ImagePath.Trim().ToLowerInvariant() switch
        {
            "coil.png" => "Coil",
            "pickup_fg.png" => "Finished Goods",
            "pickup_ncm.png" => "NCM",
            "pickup_os.png" => "Outside Service",
            "pickup_wip.png" => "WIP",
            "scrap.png" => "Scrap",
            _ => item.Title
        };
        var workCenter = string.IsNullOrWhiteSpace(item.RequestedPressName)
            ? "Unknown Work Center"
            : item.RequestedPressName;

        HeaderText = $"Details for {jobType}, requested by {workCenter}";
    }

    partial void OnSelectedBuildingChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _buildingSelectionService.SelectedBuilding = value;

        if (_isWaitlistPageActive)
        {
            UpdateWaitlistHeader();
        }
    }
}
