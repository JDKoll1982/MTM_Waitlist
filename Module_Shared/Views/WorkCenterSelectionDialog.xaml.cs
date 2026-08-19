using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Shared.Models;

namespace MTM_Waitlist.Module_Shared.Views;

public sealed partial class WorkCenterSelectionDialog : ContentDialog
{
    private HashSet<string> _activeJobWorkCenters = new(StringComparer.OrdinalIgnoreCase);

    public string? SelectedWorkCenter { get; private set; }

    public WorkCenterSelectionDialog()
    {
        InitializeComponent();
    }

    public void SetContent(
        string workstationName,
        IReadOnlyList<WorkCenterSelectionItem> hotWorkCenters,
        IReadOnlyList<WorkCenterSelectionItem> otherWorkCenters,
        IReadOnlyList<string> activeJobWorkCenters)
    {
        WorkstationTextBlock.Text = $"Current Work Center Station: {workstationName}";
        HotWorkCentersGridView.ItemsSource = hotWorkCenters;
        OtherWorkCentersGridView.ItemsSource = otherWorkCenters;
        _activeJobWorkCenters = new HashSet<string>(
            activeJobWorkCenters.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()),
            StringComparer.OrdinalIgnoreCase);

        StartupDebugLog.Info("WorkCenterSelectionDialog", $"SetContent completed. Workstation='{workstationName}', HotCount={hotWorkCenters.Count}, OtherCount={otherWorkCenters.Count}, ActiveJobCount={_activeJobWorkCenters.Count}.");
    }

    private void WorkCenter_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is WorkCenterSelectionItem workCenterItem
            && !string.IsNullOrWhiteSpace(workCenterItem.WorkCenterName))
        {
            var normalizedWorkCenter = workCenterItem.WorkCenterName.Trim();
            if (!_activeJobWorkCenters.Contains(normalizedWorkCenter))
            {
                if (sender is GridView gridView)
                {
                    gridView.SelectedItem = null;
                }

                StartupDebugLog.Info("WorkCenterSelectionDialog", $"Blocked workstation selection for '{normalizedWorkCenter}' because no active setup job exists.");

                NoActiveJobInfoBar.IsOpen = true;
                return;
            }

            SelectedWorkCenter = workCenterItem.WorkCenterName;
            StartupDebugLog.Info("WorkCenterSelectionDialog", $"Selected workstation '{normalizedWorkCenter}'.");
            Hide();
        }
    }
}
