using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Selectors;

/// <summary>
/// Picks the shell header stepper template for a step based on its
/// <see cref="HeaderStepState"/> so each state can use theme-aware resources.
/// </summary>
public sealed class HeaderStepTemplateSelector : DataTemplateSelector
{
    public DataTemplate? PendingTemplate
    {
        get; set;
    }

    public DataTemplate? CurrentTemplate
    {
        get; set;
    }

    public DataTemplate? CompleteTemplate
    {
        get; set;
    }

    protected override DataTemplate SelectTemplateCore(object item) =>
        SelectTemplateCore(item, null!);

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is not HeaderStep step)
        {
            return PendingTemplate ?? base.SelectTemplateCore(item, container);
        }

        var template = step.State switch
        {
            HeaderStepState.Current => CurrentTemplate,
            HeaderStepState.Complete => CompleteTemplate,
            _ => PendingTemplate,
        };

        return template ?? base.SelectTemplateCore(item, container);
    }
}
