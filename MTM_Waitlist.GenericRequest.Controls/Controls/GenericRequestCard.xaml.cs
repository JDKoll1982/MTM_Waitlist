using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Controls.Helpers;
using MTM_Waitlist.GenericRequest.Controls.Models;

namespace MTM_Waitlist.GenericRequest.Controls;

/// <summary>
/// The generic request card: only the facts a generic request has, in the arrangement its variant draws.
/// </summary>
/// <remarks>
/// The cells come from <see cref="GenericRequestCardRules"/>, so a work-order value is not representable here and a
/// cell with no value is never drawn.
/// </remarks>
public sealed partial class GenericRequestCard : UserControl
{
    public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
        nameof(Model), typeof(GenericRequestCardModel), typeof(GenericRequestCard), new PropertyMetadata(null, OnModelChanged));

    public GenericRequestCard()
    {
        InitializeComponent();
    }

    /// <summary>The request this card draws.</summary>
    public GenericRequestCardModel? Model
    {
        get => (GenericRequestCardModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    public IReadOnlyList<GenericRequestCardCell> Cells =>
        Model is null ? Array.Empty<GenericRequestCardCell>() : GenericRequestCardRules.CellsFor(Model);

    public string VariantLabel => Model is null ? string.Empty : GenericRequestCardRules.VariantLabel(Model.Variant);

    public string RequestNumber => Model?.RequestNumber ?? string.Empty;

    private static void OnModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        ((GenericRequestCard)sender).Bindings.Update();
}
