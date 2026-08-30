namespace MTM_Waitlist.Module_Waitlist.Controls.Other;

public sealed class OtherRequestTypeViewModel
{
    public OtherRequestTypeModel Model { get; }

    public string RequestTypeName => Model.RequestTypeName;
    public string ImagePath => Model.ImagePath;
    public string RequestText { get; set; }

    public OtherRequestTypeViewModel(OtherRequestTypeModel? model = null)
    {
        Model = model ?? new OtherRequestTypeModel();
        RequestText = Model.DefaultRequestText;
    }
}
