using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.GenericRequest.Contracts;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>
/// The details step: how many.
/// </summary>
/// <remarks>
/// <para>
/// A needed-by time was asked for here and is deliberately gone. Every request is due at the end of the allotment its
/// request type configures, exactly as the work-order flow does it: the store resolves that deadline from the item
/// and the request's own creation time when the flow leaves it unset (UrgencyDeadlineService). A hand-picked date
/// only overrode that figure, so the same item waited a different length of time depending on who raised it.
/// </para>
/// <para>
/// The description box is gone too: the step asked a handler a question nothing on the request needed answered, and
/// every request left it empty in practice. Nothing here writes a description any more, so the draft carries the one
/// it already had — none.
/// </para>
/// </remarks>
public sealed partial class GenericRequestDetailsViewModel : GenericRequestStepViewModel
{
    private readonly IGenericRequestStepData _data;

    public GenericRequestDetailsViewModel(IGenericRequestStepData data, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);

        _data = data;
    }

    public override GenericRequestStep Step => GenericRequestStep.Details;

    public override string TitleKey => "GenericRequest_Details_Title.Text";

    public override string IntroKey => "GenericRequest_Details_Intro.Text";

    public override string EmptyKey => "GenericRequest_Details_Empty.Text";

    [ObservableProperty]
    public partial double Quantity { get; set; } = 1;

    protected override async Task LoadCoreAsync()
    {
        var availability = await _data.ReadStepAvailabilityAsync(Step);

        Quantity = Draft.Quantity;

        Apply(availability);
    }

    protected override void CommitToDraft() => Draft.Quantity = Quantity;
}
