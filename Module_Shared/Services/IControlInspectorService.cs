using Microsoft.UI.Xaml;
using MTM_Waitlist.Module_Shared.Models;

namespace MTM_Waitlist.Module_Shared.Services;

public interface IControlInspectorService
{
    FrameworkElement? ActiveElement { get; }

    ControlInspectorDetail? ActiveDetail { get; }

    /// <summary>
    /// True only while the pointer is currently hovering a tracked control in developer mode.
    /// </summary>
    bool CanOpenActiveDetail { get; }

    void TrackElement(FrameworkElement element, string? resourceKey, IEnumerable<string>? associatedFiles = null, string? fallbackText = null);

    void SetActiveElement(FrameworkElement? element);

    void ClearActiveElement(FrameworkElement? element = null);

    ControlInspectorDetail BuildDetail(FrameworkElement element, string? resourceKey, IEnumerable<string>? associatedFiles = null, string? fallbackText = null);

    bool TryOpenActiveDetail();
}
