using Microsoft.UI.Xaml.Controls;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class NewRequestJobTypeDialog : ContentDialog
{
    public string? SelectedRequestType { get; private set; }

    public NewRequestJobTypeDialog()
    {
        InitializeComponent();
    }

    public void SetContent(string selectedWorkCenter, IReadOnlyList<JobTypeDialogItem> jobTypes)
    {
        WorkCenterTextBlock.Text = $"Work Center: {selectedWorkCenter}";
        JobTypeGridView.ItemsSource = jobTypes;
    }

    private void JobTypeGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is JobTypeDialogItem item)
        {
            SelectedRequestType = item.RequestType;
            Hide();
        }
    }
}
