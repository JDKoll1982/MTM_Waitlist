using Microsoft.UI.Xaml.Controls;

namespace MTM_Waitlist.Module_Waitlist.Controls.TableHandling;

public sealed partial class TableHandlingRequestTypeImageView : UserControl
{
    public TableHandlingRequestTypeImageView()
    {
        InitializeComponent();
        DataContext = new TableHandlingRequestTypeViewModel();
    }
}
