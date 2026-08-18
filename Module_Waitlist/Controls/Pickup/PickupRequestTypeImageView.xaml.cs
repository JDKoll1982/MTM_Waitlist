using Microsoft.UI.Xaml.Controls;

namespace MTM_Waitlist.Module_Waitlist.Controls.Pickup;

public sealed partial class PickupRequestTypeImageView : UserControl
{
    public PickupRequestTypeImageView()
    {
        InitializeComponent();
        DataContext = new PickupRequestTypeViewModel();
    }
}
