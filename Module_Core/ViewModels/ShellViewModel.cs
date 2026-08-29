using System.Collections.ObjectModel;
using System.ComponentModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Views;
using MTM_Waitlist.Module_Settings.Views;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.Views;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Views;

using Windows.UI;

namespace MTM_Waitlist.Module_Core.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    private readonly IBuildingSelectionService _buildingSelectionService;
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
        "Job Type",
        "Subtype",
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
        StartupState startupState)
    {
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(navigationViewService);
        ArgumentNullException.ThrowIfNull(buildingSelectionService);
        ArgumentNullException.ThrowIfNull(setupWorkflowState);
        ArgumentNullException.ThrowIfNull(startupState);

        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
        NavigationViewService = navigationViewService;
        _buildingSelectionService = buildingSelectionService;
        _setupWorkflowState = setupWorkflowState;
        _setupWorkflowState.PropertyChanged += OnSetupWorkflowStateChanged;
        _startupState = startupState;
        SelectedBuilding = _buildingSelectionService.SelectedBuilding;
        HeaderText = "MTM Waitlist";
        RefreshUserInfo();
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

    private static (string Glyph, string ColorHex) GetUserPresentation(string? role)
    {
        return role?.Trim().ToLowerInvariant() switch
        {
            "developer" => ("\uE713", "#FF0078D4"),
            "admin" or "administrator" => ("\uE7EF", "#FFC4314B"),
            "supervisor" or "manager" => ("\uE716", "#FFD67D00"),
            "quality" or "quality inspector" => ("\uE73E", "#FF107C10"),
            "material handler" => ("\uE7B8", "#FF008272"),
            _ => ("\uE77B", "#FF5C5C5C")
        };
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
            return ("New Request — Job Type", 2);
        }

        if (pageType == typeof(NewRequestSubtypePage))
        {
            return ("New Request — Subtype", 3);
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
