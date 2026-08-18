namespace MTM_Waitlist.Module_Waitlist.Controls.ForkliftAssist;

public sealed class ForkliftAssistRequestTypeViewModel
{
    public ForkliftAssistRequestTypeModel Model { get; }

    public string RequestTypeName => Model.RequestTypeName;
    public string ImagePath => Model.ImagePath;
    public string RequestText { get; set; }

    public ForkliftAssistRequestTypeViewModel(ForkliftAssistRequestTypeModel? model = null)
    {
        Model = model ?? new ForkliftAssistRequestTypeModel();
        RequestText = Model.DefaultRequestText;
    }
}
