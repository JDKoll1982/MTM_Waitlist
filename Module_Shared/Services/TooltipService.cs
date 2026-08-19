using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Shared.Services;

public sealed class TooltipService : ITooltipService
{
    private const string StandardChromeStyleKey = "TooltipStandardChromeStyle";
    private const string StandardContainerStyleKey = "TooltipStandardContainerStyle";
    private const string StandardTextStyleKey = "TooltipStandardTextStyle";

    private const string DeveloperChromeStyleKey = "TooltipDeveloperChromeStyle";
    private const string DeveloperContainerStyleKey = "TooltipDeveloperContainerStyle";
    private const string DeveloperRootPanelStyleKey = "TooltipDeveloperRootPanelStyle";
    private const string DeveloperTitleStyleKey = "TooltipDeveloperTitleStyle";
    private const string DeveloperShortcutStyleKey = "TooltipDeveloperShortcutStyle";

    private readonly StartupState _startupState;

    public TooltipService(StartupState startupState)
    {
        _startupState = startupState;
    }

    public TooltipPresentation ResolvePresentation(string? resourceKey, IEnumerable<string>? associatedFiles = null, string? fallbackText = null)
    {
        var files = associatedFiles?
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Select(static item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? Array.Empty<string>();

        if (string.IsNullOrWhiteSpace(resourceKey))
        {
            return new TooltipPresentation(
                string.IsNullOrWhiteSpace(fallbackText) ? "More details" : fallbackText!,
                files,
                _startupState.IsDeveloper);
        }

        return new TooltipPresentation(
            GetTooltipText(resourceKey, fallbackText),
            files,
            _startupState.IsDeveloper);
    }

    public ToolTip CreateTooltip(string? resourceKey, IEnumerable<string>? associatedFiles = null, string? fallbackText = null)
    {
        var presentation = ResolvePresentation(resourceKey, associatedFiles, fallbackText);
        return presentation.IsDeveloperMode
            ? CreateDeveloperTooltip(presentation.Text)
            : CreateStandardTooltip(presentation.Text);
    }

    public void ApplyToElement(FrameworkElement element, string? resourceKey, IEnumerable<string>? associatedFiles = null, string? fallbackText = null)
    {
        if (element is null)
        {
            return;
        }

        var tooltip = CreateTooltip(resourceKey, associatedFiles, fallbackText);
        ToolTipService.SetToolTip(element, tooltip);
    }

    private string GetTooltipText(string resourceKey, string? fallbackText)
    {
        var preferredMap = _startupState.IsDeveloper ? "TooltipResources.developer" : "TooltipResources";
        var localized = TryGetNamedResourceString(preferredMap, resourceKey);

        if (string.IsNullOrWhiteSpace(localized) && _startupState.IsDeveloper)
        {
            // Developer map missing a key: fall back to normal tooltip resources.
            localized = TryGetNamedResourceString("TooltipResources", resourceKey);
        }

        if (!string.IsNullOrWhiteSpace(localized))
        {
            return localized;
        }

        if (!string.IsNullOrWhiteSpace(fallbackText))
        {
            return fallbackText;
        }

        return resourceKey;
    }

    private static string? TryGetNamedResourceString(string mapName, string resourceKey)
    {
        if (string.IsNullOrWhiteSpace(mapName) || string.IsNullOrWhiteSpace(resourceKey))
        {
            return null;
        }

        // Use ResourceManager path/subtree lookup. Named ResourceLoader(mapName)
        // constructors can throw first-chance FileNotFoundException in unpackaged hosts
        // even when the subtree exists in the main PRI map.
        try
        {
            var manager = new ResourceManager();
            var pathValue = manager.MainResourceMap.TryGetValue($"{mapName}/{resourceKey}");
            var pathText = pathValue?.ValueAsString;
            if (!string.IsNullOrWhiteSpace(pathText))
            {
                return pathText;
            }

            var subtree = manager.MainResourceMap.TryGetSubtree(mapName);
            var subtreeValue = subtree?.TryGetValue(resourceKey);
            var subtreeText = subtreeValue?.ValueAsString;
            if (!string.IsNullOrWhiteSpace(subtreeText))
            {
                return subtreeText;
            }
        }
        catch (Exception)
        {
            // Fall through to null and let caller use fallback text.
        }

        return null;
    }

    private static ToolTip CreateStandardTooltip(string text)
    {
        var textBlock = new TextBlock
        {
            Text = text,
        };
        ApplyStyle(textBlock, StandardTextStyleKey);

        var content = new Border
        {
            Child = textBlock,
        };
        ApplyStyle(content, StandardContainerStyleKey);

        return CreateChromeTooltip(content, StandardChromeStyleKey);
    }

    private static ToolTip CreateDeveloperTooltip(string text)
    {
        var root = new StackPanel();
        ApplyStyle(root, DeveloperRootPanelStyleKey);

        var title = new TextBlock
        {
            Text = text,
        };
        ApplyStyle(title, DeveloperTitleStyleKey);
        root.Children.Add(title);

        var shortcut = new TextBlock
        {
            Text = "Ctrl+Alt+Right-Click  •  open full control details",
        };
        ApplyStyle(shortcut, DeveloperShortcutStyleKey);
        root.Children.Add(shortcut);

        var chrome = new Border
        {
            Child = root,
        };
        ApplyStyle(chrome, DeveloperContainerStyleKey);

        return CreateChromeTooltip(chrome, DeveloperChromeStyleKey);
    }

    private static ToolTip CreateChromeTooltip(UIElement content, string chromeStyleKey)
    {
        var tooltip = new ToolTip
        {
            Content = content,
        };
        ApplyStyle(tooltip, chromeStyleKey);
        return tooltip;
    }

    private static void ApplyStyle(FrameworkElement element, string styleKey)
    {
        if (TryGetResource(styleKey, out Style? style) && style is not null)
        {
            element.Style = style;
        }
    }

    private static bool TryGetResource<T>(string resourceKey, out T? resource)
        where T : class
    {
        resource = null;

        try
        {
            if (Application.Current?.Resources.TryGetValue(resourceKey, out var value) == true
                && value is T typed)
            {
                resource = typed;
                return true;
            }
        }
        catch (Exception)
        {
            // Resource lookup can fail in headless hosts; callers keep safe defaults.
        }

        return false;
    }
}
