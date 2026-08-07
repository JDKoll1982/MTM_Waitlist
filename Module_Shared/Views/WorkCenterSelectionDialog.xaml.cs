using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist.Module_Core.Helpers;

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
        IReadOnlyList<string> hotWorkCenters,
        IReadOnlyList<string> otherWorkCenters,
        IReadOnlyList<string> activeJobWorkCenters)
    {
        WorkstationTextBlock.Text = $"Workstation: {workstationName}";
        HotWorkCentersGridView.ItemsSource = hotWorkCenters;
        OtherWorkCentersGridView.ItemsSource = otherWorkCenters;
        _activeJobWorkCenters = new HashSet<string>(
            activeJobWorkCenters.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()),
            StringComparer.OrdinalIgnoreCase);

        StartupDebugLog.Info("WorkCenterSelectionDialog", $"SetContent completed. Workstation='{workstationName}', HotCount={hotWorkCenters.Count}, OtherCount={otherWorkCenters.Count}, ActiveJobCount={_activeJobWorkCenters.Count}.");
    }

    private async void WorkCenter_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is string workCenter && !string.IsNullOrWhiteSpace(workCenter))
        {
            var normalizedWorkCenter = workCenter.Trim();
            if (!_activeJobWorkCenters.Contains(normalizedWorkCenter))
            {
                if (sender is GridView gridView)
                {
                    gridView.SelectedItem = null;
                }

                StartupDebugLog.Info("WorkCenterSelectionDialog", $"Blocked workstation selection for '{normalizedWorkCenter}' because no active setup job exists.");

                var infoDialog = new ContentDialog
                {
                    XamlRoot = XamlRoot,
                    Title = "No Active Setup Job",
                    Content = "No Job is currently set up for that Press. Please contact a Setup Tech.",
                    CloseButtonText = "OK",
                    DefaultButton = ContentDialogButton.Close,
                };

                _ = await infoDialog.ShowAsync();
                return;
            }

            SelectedWorkCenter = workCenter;
            StartupDebugLog.Info("WorkCenterSelectionDialog", $"Selected workstation '{normalizedWorkCenter}'.");
            Hide();
        }
    }
}
