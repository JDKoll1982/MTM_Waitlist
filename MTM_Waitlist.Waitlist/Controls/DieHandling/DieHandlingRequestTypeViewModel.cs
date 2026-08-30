namespace MTM_Waitlist.Module_Waitlist.Controls.DieHandling;

public sealed class DieHandlingRequestTypeViewModel
{
    public DieHandlingRequestTypeModel Model { get; }

    public string RequestTypeName => Model.RequestTypeName;
    public string ImagePath => Model.ImagePath;
    public string RequestText { get; set; }

    public DieHandlingRequestTypeViewModel(DieHandlingRequestTypeModel? model = null)
    {
        Model = model ?? new DieHandlingRequestTypeModel();
        RequestText = Model.DefaultRequestText;
    }
}
