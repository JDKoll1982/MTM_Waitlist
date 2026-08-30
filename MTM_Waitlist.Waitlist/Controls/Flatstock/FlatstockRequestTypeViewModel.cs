namespace MTM_Waitlist.Module_Waitlist.Controls.Flatstock;

public sealed class FlatstockRequestTypeViewModel
{
    public FlatstockRequestTypeModel Model { get; }

    public string RequestTypeName => Model.RequestTypeName;
    public string ImagePath => Model.ImagePath;
    public string RequestText { get; set; }

    public FlatstockRequestTypeViewModel(FlatstockRequestTypeModel? model = null)
    {
        Model = model ?? new FlatstockRequestTypeModel();
        RequestText = Model.DefaultRequestText;
    }
}
