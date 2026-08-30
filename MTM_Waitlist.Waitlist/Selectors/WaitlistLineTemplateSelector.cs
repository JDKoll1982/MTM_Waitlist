using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Selectors;

public sealed class WaitlistLineTemplateSelector : DataTemplateSelector
{
    public DataTemplate? CoilTemplate { get; set; }

    public DataTemplate? PickupFgTemplate { get; set; }

    public DataTemplate? PickupNcmTemplate { get; set; }

    public DataTemplate? PickupOsTemplate { get; set; }

    public DataTemplate? PickupWipTemplate { get; set; }

    public DataTemplate? ScrapTemplate { get; set; }

    public DataTemplate? DefaultTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        if (item is not SampleOrder sampleOrder)
        {
            return DefaultTemplate;
        }

        var imagePath = sampleOrder.ImagePath?.Trim().ToLowerInvariant();
        return imagePath switch
        {
            "coil.png" => CoilTemplate ?? DefaultTemplate,
            "pickup_fg.png" => PickupFgTemplate ?? DefaultTemplate,
            "pickup_ncm.png" => PickupNcmTemplate ?? DefaultTemplate,
            "pickup_os.png" => PickupOsTemplate ?? DefaultTemplate,
            "pickup_wip.png" => PickupWipTemplate ?? DefaultTemplate,
            "scrap.png" => ScrapTemplate ?? DefaultTemplate,
            _ => DefaultTemplate
        };
    }
}