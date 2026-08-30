using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Shared.Models;

namespace MTM_Waitlist.Module_Shared.ViewModels;

public partial class ControlInspectorDetailViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private IReadOnlyList<string> _allAssociatedFiles = Array.Empty<string>();
    private IReadOnlyList<ControlInspectorSection> _allSections = Array.Empty<ControlInspectorSection>();

    [ObservableProperty]
    public partial ControlInspectorDetail? Detail { get; set; }

    /// <summary>
    /// When true, tooltip infrastructure files are hidden from associated-file lists.
    /// </summary>
    [ObservableProperty]
    public partial bool HideTooltipRelatedFiles { get; set; } = true;

    public ObservableCollection<ControlInspectorSection> Sections { get; } = new();

    public ObservableCollection<string> VisibleAssociatedFiles { get; } = new();

    public string ElementTypeLabel => Detail?.ElementType ?? "Unknown element";

    public string AutomationIdLabel => string.IsNullOrWhiteSpace(Detail?.AutomationId) ? "(no automation id)" : Detail!.AutomationId;

    public string VisibilityLabel => string.IsNullOrWhiteSpace(Detail?.Visibility) ? "(unknown visibility)" : Detail!.Visibility;

    public string AssociatedFilesSummary => $"{VisibleAssociatedFiles.Count} file(s) shown";

    public IReadOnlyList<ControlInspectorWorkflowStep> WorkflowSteps =>
        Detail?.WorkflowSteps ?? Array.Empty<ControlInspectorWorkflowStep>();

    public ControlInspectorDetailViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public void OnNavigatedTo(object parameter)
    {
        Detail = parameter as ControlInspectorDetail;
        _allAssociatedFiles = Detail?.AssociatedFiles ?? Array.Empty<string>();
        _allSections = Detail?.Sections ?? Array.Empty<ControlInspectorSection>();
        RefreshVisibleCollections();
    }

    public void OnNavigatedFrom()
    {
    }

    partial void OnDetailChanged(ControlInspectorDetail? value)
    {
        OnPropertyChanged(nameof(ElementTypeLabel));
        OnPropertyChanged(nameof(AutomationIdLabel));
        OnPropertyChanged(nameof(VisibilityLabel));
        OnPropertyChanged(nameof(AssociatedFilesSummary));
        OnPropertyChanged(nameof(WorkflowSteps));
    }

    partial void OnHideTooltipRelatedFilesChanged(bool value)
    {
        RefreshVisibleCollections();
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.GoBack();
    }

    private void RefreshVisibleCollections()
    {
        VisibleAssociatedFiles.Clear();
        foreach (var file in FilterAssociatedFiles(_allAssociatedFiles, HideTooltipRelatedFiles))
        {
            VisibleAssociatedFiles.Add(file);
        }

        Sections.Clear();
        foreach (var section in _allSections)
        {
            // The "Associated files" section is rendered by the dedicated
            // "Associated source files" card on the page, not by this list.
            if (string.Equals(section.Title, "Associated files", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Sections.Add(section);
        }

        OnPropertyChanged(nameof(AssociatedFilesSummary));
    }

    internal static IEnumerable<string> FilterAssociatedFiles(
        IEnumerable<string> files,
        bool hideTooltipRelatedFiles)
    {
        if (!hideTooltipRelatedFiles)
        {
            return files;
        }

        return files.Where(static file => !IsTooltipRelatedFile(file));
    }

    internal static bool IsTooltipRelatedFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalized = path.Replace('\\', '/');

        if (normalized.Contains("/Tooltip", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("TooltipBehavior", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("TooltipService", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("TooltipPresentation", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("ITooltipService", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("TooltipResources", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("Styles/Tooltip", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("TooltipStandard.xaml", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("TooltipDeveloper.xaml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
