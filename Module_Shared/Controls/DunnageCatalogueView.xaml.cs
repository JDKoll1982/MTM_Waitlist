using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.ViewModels;

namespace MTM_Waitlist.Module_Shared.Controls;

/// <summary>
/// The plant's dunnage catalogue, drawn as one control so the generic request's page and the work-order substitute
/// dialog show one list from one read (FR-001, FR-002).
/// </summary>
/// <remarks>
/// <para>
/// The markup and the state behind it were lifted out of <c>SetupDunnageImageSearchDialog</c> unchanged: the same
/// tiles, the same one-box filter, the same pictures-only switch, the same sort. Its accessible names are the ones
/// the dialog's automation already used, so a UI test that targets the dialog keeps working when the control
/// moves underneath it.
/// </para>
/// <para>
/// The control owns nothing but the drawing and the two gestures a person makes on it. Everything else — the read,
/// the filter, the picture choice, the selection — lives on <see cref="DunnageCatalogueViewModel"/>, which both
/// hosts share.
/// </para>
/// </remarks>
public sealed partial class DunnageCatalogueView : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(DunnageCatalogueViewModel),
        typeof(DunnageCatalogueView),
        new PropertyMetadata(null, OnViewModelChanged));

    public DunnageCatalogueView()
    {
        InitializeComponent();
    }

    /// <summary>The catalogue state this control draws, supplied by whichever surface hosts it.</summary>
    public DunnageCatalogueViewModel? ViewModel
    {
        get => (DunnageCatalogueViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    /// <summary>A card was clicked: that part is the chosen one.</summary>
    private void OnPartClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is DunnageCataloguePart part)
        {
            ViewModel?.SelectPart(part);
        }
    }

    /// <summary>The pictures-only switch moved: record the choice and remember it against the person.</summary>
    private async void OnShowPartsWithoutImagesToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch && ViewModel is { } viewModel)
        {
            await viewModel.SetShowPartsWithoutImagesAsync(toggleSwitch.IsOn);
        }
    }

    private static void OnViewModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        ((DunnageCatalogueView)sender).Bindings.Update();
}
