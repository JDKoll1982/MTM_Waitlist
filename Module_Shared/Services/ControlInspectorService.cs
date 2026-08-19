using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.ViewModels;
using MTM_Waitlist.Module_Startup.Models;
using Windows.System;
using Windows.UI.Core;

namespace MTM_Waitlist.Module_Shared.Services;

public sealed class ControlInspectorService : IControlInspectorService
{
    private static readonly ConditionalWeakTable<FrameworkElement, ControlInspectorTrackState> s_trackStates = new();

    private readonly StartupState _startupState;
    private readonly ITooltipService _tooltipService;
    private readonly INavigationService _navigationService;

    public ControlInspectorService(
        StartupState startupState,
        ITooltipService tooltipService,
        INavigationService navigationService)
    {
        _startupState = startupState;
        _tooltipService = tooltipService;
        _navigationService = navigationService;
    }

    public FrameworkElement? ActiveElement { get; private set; }

    public ControlInspectorDetail? ActiveDetail { get; private set; }

    private bool IsPointerHoveringActiveElement { get; set; }

    public bool CanOpenActiveDetail =>
        _startupState.IsDeveloper
        && IsPointerHoveringActiveElement
        && ActiveElement is not null
        && ActiveDetail is not null;

    public void TrackElement(
        FrameworkElement element,
        string? resourceKey,
        IEnumerable<string>? associatedFiles = null,
        string? fallbackText = null)
    {
        if (element is null || !_startupState.IsDeveloper)
        {
            return;
        }

        var state = new ControlInspectorTrackState(
            resourceKey ?? string.Empty,
            associatedFiles?
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Select(static x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
                ?? Array.Empty<string>(),
            fallbackText ?? string.Empty);

        s_trackStates.AddOrUpdate(element, state);

        // Ctrl+Alt+Right-Click opens the inspector for this control (developer only).
        element.PointerPressed -= OnTrackedPointerPressed;
        element.PointerPressed += OnTrackedPointerPressed;
        element.PointerEntered -= OnTrackedPointerEntered;
        element.PointerEntered += OnTrackedPointerEntered;
        element.PointerExited -= OnTrackedPointerExited;
        element.PointerExited += OnTrackedPointerExited;
        element.Unloaded -= OnTrackedUnloaded;
        element.Unloaded += OnTrackedUnloaded;
    }

    public void SetActiveElement(FrameworkElement? element)
    {
        if (!_startupState.IsDeveloper)
        {
            ActiveElement = null;
            ActiveDetail = null;
            IsPointerHoveringActiveElement = false;
            return;
        }

        ActiveElement = element;
        if (element is null)
        {
            ActiveDetail = null;
            IsPointerHoveringActiveElement = false;
            return;
        }

        s_trackStates.TryGetValue(element, out var state);
        ActiveDetail = BuildDetail(
            element,
            state?.ResourceKey,
            state?.AssociatedFiles,
            state?.FallbackText);
        IsPointerHoveringActiveElement = true;
    }

    public void ClearActiveElement(FrameworkElement? element = null)
    {
        if (element is not null && !ReferenceEquals(ActiveElement, element))
        {
            return;
        }

        ActiveElement = null;
        ActiveDetail = null;
        IsPointerHoveringActiveElement = false;
    }

    public ControlInspectorDetail BuildDetail(
        FrameworkElement element,
        string? resourceKey,
        IEnumerable<string>? associatedFiles = null,
        string? fallbackText = null)
    {
        var presentation = _tooltipService.ResolvePresentation(resourceKey, associatedFiles, fallbackText);
        var files = ExpandAssociatedFiles(element, presentation.AssociatedFiles, resourceKey);
        var elementName = ResolveControlDisplayName(element, resourceKey, fallbackText, presentation.Text);
        var elementType = element.GetType().FullName ?? element.GetType().Name;
        var automationId = AutomationProperties.GetAutomationId(element);
        var dataContextType = element.DataContext?.GetType().FullName ?? "(null)";
        var parentChain = BuildParentChain(element);
        var declaringViewHint = InferDeclaringView(element, files);
        var commandInfo = BuildCommandInfo(element);
        var bindingHints = BuildBindingHints(element);
        var styleKey = element.Style?.TargetType?.Name ?? "(default/no explicit style)";
        var isEnabled = element is Control enabledControl ? enabledControl.IsEnabled : (bool?)null;
        var padding = element is Control paddedControl ? paddedControl.Padding.ToString() : "(n/a)";

        var identityFields = new List<ControlInspectorField>
        {
            new() { Label = "Element name", Value = elementName },
            new() { Label = "Element type", Value = elementType },
            new() { Label = "AutomationId", Value = string.IsNullOrWhiteSpace(automationId) ? "(none)" : automationId },
            new() { Label = "Tag", Value = element.Tag?.ToString() ?? "(null)" },
            new() { Label = "DataContext type", Value = dataContextType },
            new() { Label = "Declaring view hint", Value = declaringViewHint },
        };

        var layoutFields = new List<ControlInspectorField>
        {
            new() { Label = "IsEnabled", Value = isEnabled?.ToString() ?? "(n/a)" },
            new() { Label = "IsHitTestVisible", Value = element.IsHitTestVisible.ToString() },
            new() { Label = "Visibility", Value = element.Visibility.ToString() },
            new() { Label = "Actual size", Value = $"{element.ActualWidth:0.##} x {element.ActualHeight:0.##}" },
            new() { Label = "Render size", Value = $"{element.RenderSize.Width:0.##} x {element.RenderSize.Height:0.##}" },
            new() { Label = "Margin", Value = element.Margin.ToString() },
            new() { Label = "Padding", Value = padding },
            new() { Label = "MinWidth/MinHeight", Value = $"{element.MinWidth:0.##} / {element.MinHeight:0.##}" },
            new() { Label = "MaxWidth/MaxHeight", Value = $"{element.MaxWidth:0.##} / {element.MaxHeight:0.##}" },
            new() { Label = "Horizontal/Vertical alignment", Value = $"{element.HorizontalAlignment} / {element.VerticalAlignment}" },
        };

        var tooltipFields = new List<ControlInspectorField>
        {
            new() { Label = "Resource key", Value = string.IsNullOrWhiteSpace(resourceKey) ? "(none)" : resourceKey! },
            new() { Label = "Fallback text", Value = string.IsNullOrWhiteSpace(fallbackText) ? "(none)" : fallbackText! },
            new() { Label = "Resolved tooltip text", Value = presentation.Text },
            new() { Label = "Developer mode", Value = presentation.IsDeveloperMode.ToString() },
            new() { Label = "Resource maps", Value = "TooltipResources | TooltipResources.developer" },
            new() { Label = "Style dictionaries", Value = "Styles/TooltipStandard.xaml | Styles/TooltipDeveloper.xaml" },
            new() { Label = "Shortcut", Value = "Ctrl+Alt+Right-Click on a control opens this inspector" },
        };

        var fileFields = files
            .Select((file, index) => new ControlInspectorField
            {
                Label = CategorizeAssociatedFile(file),
                Value = file,
            })
            .ToList();

        if (fileFields.Count == 0)
        {
            fileFields.Add(new ControlInspectorField { Label = "Files", Value = "(none configured)" });
        }

        var interactionFields = new List<ControlInspectorField>
        {
            new() { Label = "Command / interaction", Value = commandInfo },
            new() { Label = "Binding hints", Value = bindingHints },
            new() { Label = "Style", Value = styleKey },
            new() { Label = "Parent chain", Value = parentChain },
            new() { Label = "Modification tips", Value = BuildModificationTips(element, files, resourceKey) },
        };

        var sections = new List<ControlInspectorSection>
        {
            new() { Title = "Identity", Fields = identityFields },
            new() { Title = "Layout & state", Fields = layoutFields },
            new() { Title = "Tooltip resources", Fields = tooltipFields },
            new() { Title = "Associated files", Fields = fileFields },
            new() { Title = "Interaction & edit guidance", Fields = interactionFields },
        };

        var workflowSteps = BuildControlWorkflow(
            element,
            element.DataContext?.GetType().Name ?? "(no ViewModel)",
            declaringViewHint);

        return new ControlInspectorDetail
        {
            Title = $"{elementName} Details",
            Summary = string.IsNullOrWhiteSpace(presentation.Text)
                ? $"Developer inspector for {elementType}"
                : presentation.Text,
            ResourceKey = resourceKey ?? string.Empty,
            FallbackText = fallbackText ?? string.Empty,
            LocalizedTooltipText = presentation.Text,
            ElementName = elementName,
            ElementType = elementType,
            AutomationId = string.IsNullOrWhiteSpace(automationId) ? "(none)" : automationId,
            Tag = element.Tag?.ToString() ?? "(null)",
            DataContextType = dataContextType,
            ParentChain = parentChain,
            DeclaringViewHint = declaringViewHint,
            IsEnabled = isEnabled ?? false,
            IsHitTestVisible = element.IsHitTestVisible,
            Visibility = element.Visibility.ToString(),
            ActualSize = $"{element.ActualWidth:0.##} x {element.ActualHeight:0.##}",
            RenderSize = $"{element.RenderSize.Width:0.##} x {element.RenderSize.Height:0.##}",
            Margin = element.Margin.ToString(),
            Padding = padding,
            StyleKey = styleKey,
            CommandInfo = commandInfo,
            BindingHints = bindingHints,
            AssociatedFiles = files,
            Fields = sections.SelectMany(section => section.Fields).ToArray(),
            Sections = sections,
            WorkflowSteps = workflowSteps,
        };
    }

    public bool TryOpenActiveDetail()
    {
        // Ctrl+Alt+Right-Click opens details; the tooltip must be actively showing.
        if (!CanOpenActiveDetail || ActiveDetail is null || ActiveElement is null)
        {
            return false;
        }

        if (!ActiveElement.IsLoaded)
        {
            ClearActiveElement(ActiveElement);
            return false;
        }

        var detail = ActiveDetail;
        return _navigationService.NavigateTo(typeof(ControlInspectorDetailViewModel).FullName!, detail);
    }

    private void OnTrackedPointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            SetActiveElement(element);
        }
    }

    private void OnTrackedPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            ClearActiveElement(element);
        }
    }

    private void OnTrackedPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_startupState.IsDeveloper || sender is not FrameworkElement element)
        {
            return;
        }

        if (!e.GetCurrentPoint(element).Properties.IsRightButtonPressed || !IsCtrlAltHeld())
        {
            return;
        }

        // The developer tooltip must be actively showing for the shortcut to work.
        if (ToolTipService.GetToolTip(element) is not ToolTip { IsOpen: true })
        {
            return;
        }

        SetActiveElement(element);
        if (TryOpenActiveDetail())
        {
            e.Handled = true;
        }
    }

    private static bool IsCtrlAltHeld()
    {
        var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        var alt = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu);
        return (ctrl & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down
            && (alt & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
    }

    private void OnTrackedUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            element.PointerPressed -= OnTrackedPointerPressed;
            element.PointerEntered -= OnTrackedPointerEntered;
            element.PointerExited -= OnTrackedPointerExited;
            element.Unloaded -= OnTrackedUnloaded;
            ClearActiveElement(element);
        }
    }

    // Internal for unit tests validating models/converters/shared/core expansion.
    internal static IReadOnlyList<string> ExpandAssociatedFiles(
        FrameworkElement? element,
        IReadOnlyList<string> seedFiles,
        string? resourceKey)
    {
        var repoRoot = TryFindRepositoryRoot();
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCandidate(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            var normalized = NormalizeRelativePath(relativePath);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            if (repoRoot is null || File.Exists(Path.Combine(repoRoot, normalized.Replace('/', Path.DirectorySeparatorChar))))
            {
                candidates.Add(normalized);
            }
        }

        foreach (var seed in seedFiles)
        {
            AddCandidate(seed);
            AddPairedSourceFiles(seed, AddCandidate);
        }

        // Always include shared/core tooltip + inspector stack used by the control metadata path.
        foreach (var infrastructureFile in GetTooltipInfrastructureFiles())
        {
            AddCandidate(infrastructureFile);
        }

        if (!string.IsNullOrWhiteSpace(resourceKey))
        {
            AddCandidate("Strings/en-us/TooltipResources.resw");
            AddCandidate("Strings/en-us/TooltipResources.developer.resw");
        }

        // Runtime type graph: control, DataContext, and owning page.
        if (element is not null)
        {
            AddTypeCandidate(element.GetType(), AddCandidate);
            if (element.DataContext is not null)
            {
                AddTypeCandidate(element.DataContext.GetType(), AddCandidate);
            }

            var owningPageType = FindOwningPageType(element);
            if (owningPageType is not null)
            {
                AddTypeCandidate(owningPageType, AddCandidate);
            }
        }

        // Scan known source files for models/converters/shared/core dependencies.
        if (repoRoot is not null)
        {
            ExpandFromSourceImports(repoRoot, candidates.ToArray(), AddCandidate);
        }

        return candidates
            .OrderBy(GetAssociatedFileSortKey)
            .ThenBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> GetTooltipInfrastructureFiles()
    {
        yield return "Module_Shared/Services/TooltipBehavior.cs";
        yield return "Module_Shared/Services/TooltipService.cs";
        yield return "Module_Shared/Services/ITooltipService.cs";
        yield return "Module_Shared/Services/TooltipPresentation.cs";
        yield return "Module_Shared/Services/ControlInspectorService.cs";
        yield return "Module_Shared/Services/IControlInspectorService.cs";
        yield return "Module_Shared/Models/ControlInspectorDetail.cs";
        yield return "Module_Shared/ViewModels/ControlInspectorDetailViewModel.cs";
        yield return "Module_Shared/Views/ControlInspectorDetailPage.xaml";
        yield return "Module_Shared/Views/ControlInspectorDetailPage.xaml.cs";
        yield return "Styles/TooltipStandard.xaml";
        yield return "Styles/TooltipDeveloper.xaml";
        yield return "Module_Core/Views/ShellPage.xaml";
        yield return "Module_Core/Views/ShellPage.xaml.cs";
        yield return "Module_Core/ViewModels/ShellViewModel.cs";
        yield return "Module_Core/Helpers/ResourceExtensions.cs";
        yield return "Module_Core/Contracts/Services/INavigationService.cs";
        yield return "Module_Core/Contracts/Services/IBuildingSelectionService.cs";
        yield return "Module_Core/Services/BuildingSelectionService.cs";
        yield return "Module_Core/Services/NavigationService.cs";
        yield return "Module_Core/Services/PageService.cs";
        yield return "Module_Startup/Models/StartupState.cs";
        yield return "Module_Waitlist/Models/SampleOrder.cs";
    }

    private static void AddPairedSourceFiles(string relativePath, Action<string?> addCandidate)
    {
        var normalized = NormalizeRelativePath(relativePath);
        if (normalized.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
        {
            addCandidate(normalized + ".cs");
            return;
        }

        if (normalized.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase))
        {
            addCandidate(normalized[..^3]); // strip trailing .cs
            return;
        }

        if (normalized.EndsWith("ViewModel.cs", StringComparison.OrdinalIgnoreCase))
        {
            var viewPath = normalized
                .Replace("/ViewModels/", "/Views/", StringComparison.OrdinalIgnoreCase)
                .Replace("ViewModel.cs", "Page.xaml", StringComparison.OrdinalIgnoreCase);
            addCandidate(viewPath);
            addCandidate(viewPath + ".cs");
        }
    }

    private static void AddTypeCandidate(Type type, Action<string?> addCandidate)
    {
        foreach (var current in EnumerateTypeClosure(type))
        {
            var relativePath = TryMapTypeToRelativePath(current);
            if (!string.IsNullOrWhiteSpace(relativePath))
            {
                addCandidate(relativePath);
                AddPairedSourceFiles(relativePath, addCandidate);
            }
        }
    }

    private static IEnumerable<Type> EnumerateTypeClosure(Type rootType)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<Type>();
        queue.Enqueue(rootType);

        while (queue.Count > 0)
        {
            var type = queue.Dequeue();
            if (type is null)
            {
                continue;
            }

            var fullName = type.FullName;
            if (string.IsNullOrWhiteSpace(fullName) || !seen.Add(fullName))
            {
                continue;
            }

            if (!fullName.StartsWith("MTM_Waitlist.", StringComparison.Ordinal))
            {
                continue;
            }

            yield return type;

            if (type.BaseType is not null)
            {
                queue.Enqueue(type.BaseType);
            }

            foreach (var interfaceType in type.GetInterfaces())
            {
                queue.Enqueue(interfaceType);
            }
        }
    }

    private static string? TryMapTypeToRelativePath(Type type)
    {
        var fullName = type.FullName;
        if (string.IsNullOrWhiteSpace(fullName) || !fullName.StartsWith("MTM_Waitlist.", StringComparison.Ordinal))
        {
            return null;
        }

        // Nested types are not mapped to files.
        if (fullName.Contains('+', StringComparison.Ordinal))
        {
            return null;
        }

        var withoutRoot = fullName["MTM_Waitlist.".Length..];
        var path = withoutRoot.Replace('.', '/') + ".cs";
        return NormalizeRelativePath(path);
    }

    private static Type? FindOwningPageType(FrameworkElement element)
    {
        DependencyObject? current = element;
        while (current is not null)
        {
            if (current is Page page)
            {
                return page.GetType();
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static void ExpandFromSourceImports(
        string repoRoot,
        IReadOnlyCollection<string> seedRelativePaths,
        Action<string?> addCandidate)
    {
        foreach (var relativePath in seedRelativePaths)
        {
            if (!relativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                && !relativePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var absolutePath = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absolutePath))
            {
                continue;
            }

            string content;
            try
            {
                content = File.ReadAllText(absolutePath);
            }
            catch (Exception)
            {
                continue;
            }

            var typeMatches = System.Text.RegularExpressions.Regex.Matches(
                content,
                @"\bMTM_Waitlist\.(Module_(?:Core|Shared|Waitlist|Settings|Setup|Startup|Reporting)(?:\.[A-Za-z0-9_]+)+)",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant);

            for (var i = 0; i < typeMatches.Count; i++)
            {
                var match = typeMatches[i];
                var typeName = "MTM_Waitlist." + match.Groups[1].Value;
                var mapped = TryMapNamespaceTypeNameToPath(typeName);
                if (mapped is null)
                {
                    continue;
                }

                // Keep this expansion focused on models/converters/shared/core support files.
                if (!IsSupportAssociatedFile(mapped))
                {
                    continue;
                }

                addCandidate(mapped);
            }
        }
    }

    private static string? TryMapNamespaceTypeNameToPath(string fullTypeName)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName) || !fullTypeName.StartsWith("MTM_Waitlist.", StringComparison.Ordinal))
        {
            return null;
        }

        var withoutRoot = fullTypeName["MTM_Waitlist.".Length..];
        // Ignore namespace-only values that don't end with a type-looking segment.
        var lastSegment = withoutRoot.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (string.IsNullOrWhiteSpace(lastSegment) || !char.IsUpper(lastSegment[0]))
        {
            return null;
        }

        return NormalizeRelativePath(withoutRoot.Replace('.', '/') + ".cs");
    }

    private static bool IsSupportAssociatedFile(string relativePath)
    {
        var normalized = NormalizeRelativePath(relativePath);
        return normalized.Contains("/Models/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/Converters/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/Contracts/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/Helpers/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("Module_Shared/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("Module_Core/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("Styles/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("Strings/", StringComparison.OrdinalIgnoreCase);
    }

    private static string CategorizeAssociatedFile(string relativePath)
    {
        var normalized = NormalizeRelativePath(relativePath);
        if (normalized.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) || normalized.Contains("/Views/", StringComparison.OrdinalIgnoreCase))
        {
            return "View";
        }

        if (normalized.Contains("/ViewModels/", StringComparison.OrdinalIgnoreCase))
        {
            return "ViewModel";
        }

        if (normalized.Contains("/Models/", StringComparison.OrdinalIgnoreCase))
        {
            return "Model";
        }

        if (normalized.Contains("/Converters/", StringComparison.OrdinalIgnoreCase))
        {
            return "Converter";
        }

        if (normalized.StartsWith("Module_Shared/", StringComparison.OrdinalIgnoreCase))
        {
            return "Shared";
        }

        if (normalized.StartsWith("Module_Core/", StringComparison.OrdinalIgnoreCase))
        {
            return "Core";
        }

        if (normalized.StartsWith("Styles/", StringComparison.OrdinalIgnoreCase) || normalized.StartsWith("Strings/", StringComparison.OrdinalIgnoreCase))
        {
            return "Resources";
        }

        return "File";
    }

    private static int GetAssociatedFileSortKey(string relativePath)
    {
        var category = CategorizeAssociatedFile(relativePath);
        return category switch
        {
            "View" => 0,
            "ViewModel" => 1,
            "Model" => 2,
            "Converter" => 3,
            "Shared" => 4,
            "Core" => 5,
            "Resources" => 6,
            _ => 7,
        };
    }

    private static string NormalizeRelativePath(string path)
    {
        return path
            .Replace('\\', '/')
            .Trim()
            .TrimStart('/');
    }

    private static string? TryFindRepositoryRoot()
    {
        try
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var projectPath = Path.Combine(directory.FullName, "MTM_Waitlist.csproj");
                if (File.Exists(projectPath))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }
        catch (Exception)
        {
            return null;
        }

        return null;
    }

    private static string ResolveControlDisplayName(
        FrameworkElement element,
        string? resourceKey,
        string? fallbackText,
        string resolvedTooltipText)
    {
        if (!string.IsNullOrWhiteSpace(element.Name))
        {
            return HumanizeIdentifier(element.Name);
        }

        var automationName = AutomationProperties.GetName(element);
        if (!string.IsNullOrWhiteSpace(automationName))
        {
            return automationName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(resourceKey))
        {
            var keyName = resourceKey
                .Replace("_Tooltip", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("Tooltip", string.Empty, StringComparison.OrdinalIgnoreCase);
            var humanizedKey = HumanizeIdentifier(keyName);
            if (!string.IsNullOrWhiteSpace(humanizedKey))
            {
                return humanizedKey;
            }
        }

        if (!string.IsNullOrWhiteSpace(resolvedTooltipText)
            && !string.Equals(resolvedTooltipText, "More details", StringComparison.OrdinalIgnoreCase))
        {
            return TrimForDisplay(resolvedTooltipText);
        }

        if (!string.IsNullOrWhiteSpace(fallbackText)
            && !string.Equals(fallbackText, "More details", StringComparison.OrdinalIgnoreCase))
        {
            return TrimForDisplay(fallbackText);
        }

        return HumanizeIdentifier(element.GetType().Name);
    }

    private static string HumanizeIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var cleaned = value.Replace('_', ' ').Replace('-', ' ').Trim();
        var builder = new StringBuilder(cleaned.Length + 8);
        for (var i = 0; i < cleaned.Length; i++)
        {
            var current = cleaned[i];
            if (i > 0)
            {
                var previous = cleaned[i - 1];
                var isBoundary =
                    char.IsUpper(current) && (char.IsLower(previous) || char.IsDigit(previous))
                    || char.IsDigit(current) && char.IsLetter(previous);

                if (isBoundary && builder[^1] != ' ')
                {
                    builder.Append(' ');
                }
            }

            builder.Append(current);
        }

        var humanized = string.Join(
            ' ',
            builder.ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(static part => part.Length <= 1
                    ? part.ToUpperInvariant()
                    : char.ToUpperInvariant(part[0]) + part[1..]));

        return humanized;
    }

    private static string BuildParentChain(FrameworkElement element)
    {
        var parts = new List<string>();
        DependencyObject? current = element;
        var depth = 0;
        while (current is not null && depth < 8)
        {
            var typeName = current.GetType().Name;
            var name = current is FrameworkElement fe && !string.IsNullOrWhiteSpace(fe.Name)
                ? fe.Name
                : null;
            parts.Add(name is null ? typeName : $"{typeName}[{name}]");
            current = VisualTreeHelper.GetParent(current);
            depth++;
        }

        return string.Join(" -> ", parts);
    }

    private static string InferDeclaringView(FrameworkElement element, IReadOnlyList<string> files)
    {
        var xamlFile = files.FirstOrDefault(static file => file.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(xamlFile))
        {
            return xamlFile!;
        }

        DependencyObject? current = element;
        while (current is not null)
        {
            if (current is Page page)
            {
                return page.GetType().FullName ?? page.GetType().Name;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return "(unknown)";
    }

    private static string BuildCommandInfo(FrameworkElement element)
    {
        if (element is ButtonBase button)
        {
            if (button.Command is not null)
            {
                return $"Command={button.Command.GetType().Name}; Parameter={button.CommandParameter ?? "(null)"}";
            }

            return "ButtonBase without ICommand (likely Click handler in code-behind)";
        }

        if (element is MenuFlyoutItem menuItem)
        {
            return menuItem.Command is null
                ? "MenuFlyoutItem without ICommand"
                : $"Command={menuItem.Command.GetType().Name}; Parameter={menuItem.CommandParameter ?? "(null)"}";
        }

        if (element is TextBox or PasswordBox or AutoSuggestBox or ComboBox or NumberBox)
        {
            return "Editable/input control (no ICommand by default)";
        }

        return "(no command surface detected)";
    }

    private static IReadOnlyList<ControlInspectorWorkflowStep> BuildControlWorkflow(
        FrameworkElement element,
        string dataContextType,
        string declaringViewHint)
    {
        var steps = new List<(string Title, string Caption)>();

        var (interactionTitle, interactionCaption) = DescribeInteraction(element);
        steps.Add((interactionTitle, interactionCaption));

        if (element is ButtonBase button)
        {
            if (button.Command is { } command)
            {
                var commandType = command.GetType().Name;
                var methodName = InferCommandMethodName(button);
                var parameterType = button.CommandParameter?.GetType().Name;

                var commandCaption = methodName is null
                    ? $"{commandType} bound to ViewModel"
                    : parameterType is null
                        ? $"{commandType} \u2192 {methodName}"
                        : $"{commandType} \u2192 {methodName}({parameterType})";
                steps.Add(("Command executes", commandCaption));

                if (!string.IsNullOrWhiteSpace(dataContextType))
                {
                    steps.Add(("ViewModel reacts", methodName is null ? dataContextType : $"{methodName} on {dataContextType}"));
                }
            }
            else
            {
                var viewHint = string.IsNullOrWhiteSpace(declaringViewHint) ? "declaring view" : declaringViewHint;
                steps.Add(("Code-behind handler", $"Click handler in {viewHint}"));
            }
        }
        else if (!string.IsNullOrWhiteSpace(dataContextType))
        {
            steps.Add(("Binding updates", $"{dataContextType} property binding"));
            steps.Add(("ViewModel reacts", dataContextType));
        }

        steps.Add(("UI updates", "ObservableProperty changes rebind XAML"));

        return steps
            .Select((step, index) => new ControlInspectorWorkflowStep
            {
                Index = index + 1,
                Title = step.Title,
                Caption = step.Caption,
                ShowChevron = index > 0,
            })
            .ToArray();
    }

    private static (string Title, string Caption) DescribeInteraction(FrameworkElement element)
    {
        switch (element)
        {
            case CheckBox or RadioButton or ToggleSwitch:
                return ("User toggles", $"{element.GetType().Name} raises Checked / Unchecked");
            case ToggleButton:
                return ("User toggles", "ToggleButton raises Checked / Unchecked");
            case ButtonBase or MenuFlyoutItem:
                return ("Pointer released", $"{element.GetType().Name} raises Click");
            case TextBox:
                return ("User types", "TextBox raises TextChanged");
            case PasswordBox:
                return ("User types", "PasswordBox raises PasswordChanged");
            case AutoSuggestBox:
                return ("User types / submits", "AutoSuggestBox raises TextChanged / QuerySubmitted");
            case ComboBox or ListView or GridView or ListBox:
                return ("User selects", $"{element.GetType().Name} raises SelectionChanged");
            case Slider:
                return ("User drags", "Slider raises ValueChanged");
            case CalendarDatePicker:
                return ("User picks a date", "CalendarDatePicker raises DateChanged");
            case DatePicker:
                return ("User picks a date", "DatePicker raises DateChanged");
            case TimePicker:
                return ("User picks a time", "TimePicker raises TimeChanged");
            default:
                return ("User interacts", $"{element.GetType().Name} raises a routed event");
        }
    }

    private static string? InferCommandMethodName(ButtonBase button)
    {
        var command = button.Command;
        if (command is null || button.DataContext is null)
        {
            return null;
        }

        var dataContext = button.DataContext;
        foreach (var property in dataContext.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!typeof(ICommand).IsAssignableFrom(property.PropertyType))
            {
                continue;
            }

            if (ReferenceEquals(property.GetValue(dataContext), command))
            {
                var name = property.Name;
                return name.EndsWith("Command", StringComparison.Ordinal)
                    ? name[..^"Command".Length]
                    : name;
            }
        }

        return null;
    }

    private static string BuildBindingHints(FrameworkElement element)
    {
        var hints = new List<string>();

        if (element is ButtonBase)
        {
            hints.Add("Check Command / Click / IsEnabled bindings");
        }

        if (element is Selector selector)
        {
            hints.Add($"ItemsSource type={(selector.ItemsSource?.GetType().Name ?? "(null)")}");
            hints.Add($"SelectedItem type={(selector.SelectedItem?.GetType().Name ?? "(null)")}");
        }

        if (element is TextBox textBox)
        {
            hints.Add($"Text length={textBox.Text?.Length ?? 0}");
            hints.Add($"PlaceholderText={textBox.PlaceholderText}");
        }

        if (element is AutoSuggestBox suggestBox)
        {
            hints.Add($"Text={suggestBox.Text}");
            hints.Add($"PlaceholderText={suggestBox.PlaceholderText}");
            hints.Add($"ItemsSource type={(suggestBox.ItemsSource?.GetType().Name ?? "(null)")}");
        }

        if (element is TextBlock textBlock)
        {
            hints.Add($"Text={TrimForDisplay(textBlock.Text)}");
        }

        return hints.Count == 0 ? "(none detected)" : string.Join(" | ", hints);
    }

    private static string BuildModificationTips(FrameworkElement element, IReadOnlyList<string> files, string? resourceKey)
    {
        var builder = new StringBuilder();
        builder.Append("1) Edit associated XAML/code files listed above. ");
        if (!string.IsNullOrWhiteSpace(resourceKey))
        {
            builder.Append($"2) Update tooltip copy in TooltipResources*.resw key '{resourceKey}'. ");
        }

        builder.Append("3) Adjust visual chrome in Styles/TooltipStandard.xaml or Styles/TooltipDeveloper.xaml. ");
        builder.Append("4) Re-run app and Ctrl+Alt+Right-Click the control to verify.");

        if (files.Count == 0)
        {
            builder.Append(" 5) Add shared:TooltipBehavior.AssociatedFiles on the control to link source files.");
        }

        if (element is ButtonBase)
        {
            builder.Append(" 6) For actions, inspect Command binding or Click handler in the declaring view/viewmodel.");
        }

        return builder.ToString();
    }

    private static string TrimForDisplay(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "(empty)";
        }

        var trimmed = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return trimmed.Length <= 120 ? trimmed : trimmed[..117] + "...";
    }

    private sealed class ControlInspectorTrackState
    {
        public ControlInspectorTrackState(string resourceKey, IReadOnlyList<string> associatedFiles, string fallbackText)
        {
            ResourceKey = resourceKey;
            AssociatedFiles = associatedFiles;
            FallbackText = fallbackText;
        }

        public string ResourceKey { get; }

        public IReadOnlyList<string> AssociatedFiles { get; }

        public string FallbackText { get; }
    }
}
