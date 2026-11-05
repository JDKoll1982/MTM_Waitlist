using System.ComponentModel;

using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Shared.ViewModels;

namespace MTM_Waitlist.Module_Setup.ViewModels;

/// <summary>
/// ViewModel for the image-backed Dunnage part search dialog used by the Module_Setup Dunnage entry.
/// </summary>
/// <remarks>
/// <para>
/// The catalogue itself — the read, the one-box filter, the pictures-only choice, the sort and the selection — is
/// <see cref="DunnageCatalogueViewModel"/>, which the generic request's dunnage page draws too. This view model
/// exists so the dialog keeps the surface it has always had: the same members, the same words and the same
/// behaviour, now backed by the shared state rather than by a private copy (FR-002).
/// </para>
/// <para>
/// It keeps this dialog's own flat failure sentence. The page reports an unavailable catalogue with the time of
/// the attempt and a retry; this dialog says only that the read failed, which is what it has always said.
/// </para>
/// </remarks>
public partial class SetupDunnageImageSearchDialogViewModel : ObservableRecipient
{
    /// <summary>The scoped-preference key the parts-without-pictures choice is stored under, against the person.</summary>
    public const string SettingsKey = DunnageCatalogueViewModel.ShowPartsWithoutImagesSettingsKey;

    public SetupDunnageImageSearchDialogViewModel(IDunnageCatalogueReader reader, IScopedPreferenceStore preferences)
    {
        Catalogue = new DunnageCatalogueViewModel(reader, preferences);
        Catalogue.PropertyChanged += OnCataloguePropertyChanged;
    }

    /// <summary>The shared catalogue this dialog draws and reads through.</summary>
    public DunnageCatalogueViewModel Catalogue { get; }

    /// <summary>The free text the requester typed.</summary>
    public string FilterText
    {
        get => Catalogue.FilterText;
        set => Catalogue.FilterText = value;
    }

    /// <summary>The parts the picture choice and the filter currently leave.</summary>
    public IReadOnlyList<DunnageCataloguePart> DisplayedParts => Catalogue.DisplayedParts;

    /// <summary>The words for a read that ended with nothing to show.</summary>
    public string EmptyStateMessage => Catalogue.EmptyStateMessage;

    /// <summary>Whether parts with no picture are shown.</summary>
    public bool ShowPartsWithoutImages => Catalogue.ShowPartsWithoutImages;

    /// <summary>
    /// The part chosen by the user, or <c>null</c> when the dialog was dismissed.
    /// </summary>
    /// <remarks>
    /// The dialog hands a <see cref="SetupDunnagePart"/> back to the Setup workflow, which is what this projects
    /// the shared row onto. The two fields the shared row does not carry — the metadata and the pair flag — are
    /// not read by the workflow's add path, which keys the part by its identifier.
    /// </remarks>
    public SetupDunnagePart? SelectedPart => ToSetupPart(Catalogue.SelectedPart);

    public Task InitializeAsync() => Catalogue.InitializeAsync();

    /// <summary>Records the selected part. The dialog host closes and returns it to the caller.</summary>
    public void SelectPart(DunnageCataloguePart? part) => Catalogue.SelectPart(part);

    /// <summary>Records the pictures-only choice and remembers it against the person.</summary>
    public Task HandleShowPartsWithoutImagesChangedAsync(bool isChecked) =>
        Catalogue.SetShowPartsWithoutImagesAsync(isChecked);

    private static SetupDunnagePart? ToSetupPart(DunnageCataloguePart? part) =>
        part is null
            ? null
            : new SetupDunnagePart
            {
                Id = part.Id,
                TypeId = part.TypeId,
                PartNumber = part.PartNumber,
                DisplayName = part.DisplayName,
                DunnageTypeName = part.DunnageTypeName,
                HomeLocation = part.HomeLocation,
                ImagePath = part.ImagePath,
            };

    /// <summary>Repeat the catalogue's changes on this view model, so the dialog's own bindings keep working.</summary>
    private void OnCataloguePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var propertyName = e.PropertyName switch
        {
            nameof(DunnageCatalogueViewModel.FilterText) => nameof(FilterText),
            nameof(DunnageCatalogueViewModel.DisplayedParts) => nameof(DisplayedParts),
            nameof(DunnageCatalogueViewModel.EmptyStateMessage) => nameof(EmptyStateMessage),
            nameof(DunnageCatalogueViewModel.ShowPartsWithoutImages) => nameof(ShowPartsWithoutImages),
            nameof(DunnageCatalogueViewModel.SelectedPart) => nameof(SelectedPart),
            _ => null,
        };

        if (propertyName is not null)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
