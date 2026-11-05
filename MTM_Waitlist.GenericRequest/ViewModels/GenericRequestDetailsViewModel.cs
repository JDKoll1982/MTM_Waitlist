using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>
/// The details step: how many, and anything a handler should know.
/// </summary>
/// <remarks>
/// A needed-by time was asked for here and is deliberately gone. Every request is due at the end of the allotment its
/// request type configures, exactly as the work-order flow does it: the store resolves that deadline from the item
/// and the request's own creation time when the flow leaves it unset (UrgencyDeadlineService). A hand-picked date
/// only overrode that figure, so the same item waited a different length of time depending on who raised it.
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

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    protected override async Task LoadCoreAsync()
    {
        var availability = await _data.ReadStepAvailabilityAsync(Step);

        Quantity = Draft.Quantity;
        Description = Draft.Description;

        Apply(availability);
    }

    protected override void CommitToDraft()
    {
        Draft.Quantity = Quantity;
        Draft.Description = Description.Trim();
    }
}
