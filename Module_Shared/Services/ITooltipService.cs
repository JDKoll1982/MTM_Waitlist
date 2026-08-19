using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MTM_Waitlist.Module_Shared.Services;

public interface ITooltipService
{
    TooltipPresentation ResolvePresentation(string? resourceKey, IEnumerable<string>? associatedFiles = null, string? fallbackText = null);

    ToolTip CreateTooltip(string? resourceKey, IEnumerable<string>? associatedFiles = null, string? fallbackText = null);

    void ApplyToElement(FrameworkElement element, string? resourceKey, IEnumerable<string>? associatedFiles = null, string? fallbackText = null);
}
