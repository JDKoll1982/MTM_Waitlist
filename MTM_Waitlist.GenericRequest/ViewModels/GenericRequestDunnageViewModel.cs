using System.ComponentModel;
using System.Globalization;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Shared.ViewModels;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>
/// The dunnage step: the plant's dunnage catalogue, drawn as a page of the wizard.
/// </summary>
/// <remarks>
/// <para>
/// The catalogue itself belongs to <see cref="DunnageCatalogueViewModel"/>, which the substitute dialog draws too,
/// so the page and the dialog can never offer different parts (FR-001, FR-002). This view model owns only what is
/// the step's: which of the frame's presentations the read produced, the forward gate, and the draft.
/// </para>
/// <para>
/// Nothing is chosen until the requester chooses it. The old step pre-selected the first type and the first part,
/// which is how a part nobody picked could reach a request; <see cref="GenericRequestStepViewModel.CanAnswer"/> is
/// false until a part is actually chosen (FR-004, FR-006).
/// </para>
/// </remarks>
public sealed partial class GenericRequestDunnageViewModel : GenericRequestStepViewModel
{
    private const string LoadingKey = "GenericRequest_Dunnage_Loading.Text";
    private const string UnavailableKey = "GenericRequest_Dunnage_Unavailable.Text";
    private const string OnePartOnlyKey = "GenericRequest_Dunnage_OnePartOnly.Text";
    private const string SelectedKey = "GenericRequest_Dunnage_Selected.Text";

    public GenericRequestDunnageViewModel(DunnageCatalogueViewModel catalogue, IGenericRequestNavigator navigator)
        : base(navigator)
    {
        Catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
        Catalogue.PropertyChanged += OnCataloguePropertyChanged;
    }

    public override GenericRequestStep Step => GenericRequestStep.Dunnage;

    public override string TitleKey => "GenericRequest_Dunnage_Title.Text";

    public override string IntroKey => "GenericRequest_Dunnage_CatalogueIntro.Text";

    public override string EmptyKey => "GenericRequest_Dunnage_NoParts.Text";

    /// <summary>The shared catalogue this step draws and reads through.</summary>
    public DunnageCatalogueViewModel Catalogue { get; }

    /// <summary>What the frame says while the catalogue is loading.</summary>
    public string LoadingText => LoadingKey.GetLocalized();

    /// <summary>
    /// The step's own sentence for a catalogue that could not be read, carrying the time of the attempt.
    /// </summary>
    /// <remarks>
    /// The frame draws this instead of its generic heading and its separate attempt line, so the same moment is
    /// named once. Every other step leaves the frame's own unavailable wording in place.
    /// </remarks>
    public string UnavailableText
    {
        get
        {
            var template = UnavailableKey.GetLocalized();
            if (AttemptedAtUtc is not { } attemptedAt)
            {
                return template;
            }

            var when = attemptedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
            return string.Format(CultureInfo.CurrentCulture, template, when);
        }
    }

    /// <summary>The one-part rule, shown once below the catalogue (FR-005).</summary>
    public string SecondPartNote => OnePartOnlyKey.GetLocalized();

    /// <summary>The part currently chosen, ready to draw, or empty when nothing is chosen.</summary>
    public string SelectedPartDisplay => Catalogue.SelectedPart is { } part
        ? $"{SelectedKey.GetLocalized()} {part.PartNumber}"
        : string.Empty;

    /// <summary>
    /// A part has to be chosen before the step can be left, because a dunnage request with no part names nothing.
    /// </summary>
    protected override bool CanAnswer => Catalogue.SelectedPart is not null;

    protected override async Task LoadCoreAsync()
    {
        // The frame says it is reading rather than reporting a finished read: an empty catalogue is an answer, and
        // a read still in flight has not given one (FR-010).
        Presentation = GenericRequestPresentation.Loading;

        var result = await Catalogue.LoadAsync();

        AttemptedAtUtc = result.AttemptedAtUtc;
        Presentation = result.Outcome switch
        {
            DunnageCatalogueLoadOutcome.Ready => GenericRequestPresentation.Populated,
            DunnageCatalogueLoadOutcome.Empty => GenericRequestPresentation.Empty,
            _ => GenericRequestPresentation.Unavailable,
        };

        if (Presentation == GenericRequestPresentation.Populated)
        {
            // A revisit shows the part the draft remembers. A part that has left the catalogue is not chosen and
            // the draft keeps the value it had, which is what records it as no longer offered at raise time.
            Catalogue.SelectPartNumber(Draft.DunnagePart);
        }

        OnPropertyChanged(nameof(UnavailableText));
        OnPropertyChanged(nameof(SelectedPartDisplay));
    }

    protected override void CommitToDraft()
    {
        // Leaving without a part records nothing at all (FR-006).
        if (Catalogue.SelectedPart is not { } part)
        {
            return;
        }

        Draft.DunnageType = part.DunnageTypeName;
        Draft.DunnagePart = part.PartNumber;
    }

    private void OnCataloguePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DunnageCatalogueViewModel.SelectedPart))
        {
            OnPropertyChanged(nameof(SelectedPartDisplay));
            NotifyAnswerChanged();
        }
    }
}
