using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>
/// The details step: when the part is needed, how many, and anything a handler should know.
/// </summary>
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
    public partial DateTimeOffset? NeededByDate { get; set; }

    [ObservableProperty]
    public partial TimeSpan? NeededByTime { get; set; }

    [ObservableProperty]
    public partial double Quantity { get; set; } = 1;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    protected override async Task LoadCoreAsync()
    {
        var availability = await _data.ReadStepAvailabilityAsync(Step);

        NeededByDate = Draft.NeededBy?.Date ?? DateTimeOffset.Now.Date;
        NeededByTime = Draft.NeededBy?.TimeOfDay ?? new TimeSpan(7, 0, 0);
        Quantity = Draft.Quantity;
        Description = Draft.Description;

        Apply(availability);
    }

    protected override void CommitToDraft()
    {
        Draft.NeededBy = NeededByDate is { } date
            ? new DateTimeOffset(date.Date.Add(NeededByTime ?? TimeSpan.Zero), date.Offset)
            : null;
        Draft.Quantity = Quantity;
        Draft.Description = Description;
    }
}
