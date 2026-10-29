using MTM_Waitlist.GenericRequest.Contracts;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>
/// The result step: what happened when the request was raised.
/// </summary>
/// <remarks>
/// This chunk submits nothing, so the step reports what the flow has to report and nothing more. The presentation
/// comes from the draft, which the submission in a later chunk will fill; until then an unraised request honestly
/// reads as empty.
/// </remarks>
public sealed partial class GenericRequestResultViewModel : GenericRequestStepViewModel
{
    private readonly IGenericRequestStepData _data;

    public GenericRequestResultViewModel(IGenericRequestStepData data, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);

        _data = data;
    }

    public override GenericRequestStep Step => GenericRequestStep.Result;

    public override string TitleKey => "GenericRequest_Result_Title.Text";

    public override string IntroKey => "GenericRequest_Result_Intro.Text";

    public override string EmptyKey => "GenericRequest_Result_Empty.Text";

    public string RequestNumber => Draft.RaisedRequestNumber;

    protected override async Task LoadCoreAsync() =>
        Apply(await _data.ReadStepAvailabilityAsync(Step));
}
