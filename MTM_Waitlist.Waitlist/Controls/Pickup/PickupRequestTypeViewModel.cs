namespace MTM_Waitlist.Module_Waitlist.Controls.Pickup;

public sealed class PickupRequestTypeViewModel
{
    public PickupRequestTypeModel Model { get; }

    public string RequestTypeName => Model.RequestTypeName;
    public string ImagePath => Model.ImagePath;
    public string RequestText { get; set; }

    public PickupRequestTypeViewModel(PickupRequestTypeModel? model = null)
    {
        Model = model ?? new PickupRequestTypeModel();
        RequestText = Model.DefaultRequestText;
    }
}
