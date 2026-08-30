namespace MTM_Waitlist.Module_Waitlist.Controls.TableHandling;

public sealed class TableHandlingRequestTypeViewModel
{
    public TableHandlingRequestTypeModel Model { get; }

    public string RequestTypeName => Model.RequestTypeName;
    public string ImagePath => Model.ImagePath;
    public string RequestText { get; set; }

    public TableHandlingRequestTypeViewModel(TableHandlingRequestTypeModel? model = null)
    {
        Model = model ?? new TableHandlingRequestTypeModel();
        RequestText = Model.DefaultRequestText;
    }
}
