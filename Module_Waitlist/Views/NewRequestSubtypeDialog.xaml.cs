using Microsoft.UI.Xaml.Controls;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class NewRequestSubtypeDialog : ContentDialog
{
    public string? SelectedSubtypeName { get; private set; }

    public NewRequestSubtypeDialog()
    {
        InitializeComponent();
    }

    public void SetContent(string requestTypeName, IReadOnlyList<JobTypeDialogItem> subtypes)
    {
        Title = $"{requestTypeName} - Choose subtype";
        RequestTypeTextBlock.Text = requestTypeName;
        SubtypeGridView.ItemsSource = subtypes;
    }

    private void SubtypeGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is JobTypeDialogItem item)
        {
            SelectedSubtypeName = item.RequestType;
            Hide();
        }
    }
}
