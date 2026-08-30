namespace MTM_Waitlist.Module_Waitlist.Controls.Coil;

public sealed class CoilRequestTypeViewModel
{
    public CoilRequestTypeModel Model { get; }

    public string RequestTypeName => Model.RequestTypeName;
    public string ImagePath => Model.ImagePath;
    public string RequestText { get; set; }

    public CoilRequestTypeViewModel(CoilRequestTypeModel? model = null)
    {
        Model = model ?? new CoilRequestTypeModel();
        RequestText = Model.DefaultRequestText;
    }
}
