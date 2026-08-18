using Microsoft.UI.Xaml.Controls;

namespace MTM_Waitlist.Module_Waitlist.Controls.ForkliftAssist;

public sealed partial class ForkliftAssistRequestTypeImageView : UserControl
{
    public ForkliftAssistRequestTypeImageView()
    {
        InitializeComponent();
        DataContext = new ForkliftAssistRequestTypeViewModel();
    }
}
