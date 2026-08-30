namespace MTM_Waitlist.Module_Waitlist.Controls.Scrap;

public sealed class ScrapRequestTypeViewModel
{
    public ScrapRequestTypeModel Model { get; }

    public string RequestTypeName => Model.RequestTypeName;
    public string ImagePath => Model.ImagePath;
    public string RequestText { get; set; }

    public ScrapRequestTypeViewModel(ScrapRequestTypeModel? model = null)
    {
        Model = model ?? new ScrapRequestTypeModel();
        RequestText = Model.DefaultRequestText;
    }
}
