using Microsoft.Windows.ApplicationModel.Resources;
using System.Runtime.InteropServices;

namespace MTM_Waitlist.Module_Core.Helpers;

public static class ResourceExtensions
{
    /// <summary>
    /// The subtree this project's <c>Strings/&lt;language&gt;/Resources.resw</c> keys are registered under.
    /// MakePri names the subtree after the resw file, so the resource URI is
    /// <c>ms-resource://&lt;package&gt;/Resources/&lt;key&gt;</c> and the bare key is not at the map root.
    /// </summary>
    private const string ResourcesSubtreeName = "Resources";

    private static readonly object s_managerSync = new();
    private static ResourceManager? s_resourceManager;

    public static string GetLocalized(this string resourceKey)
    {
        if (string.IsNullOrWhiteSpace(resourceKey))
        {
            return string.Empty;
        }

        try
        {
            // Use the resource map's TryGetValue, which returns null for a missing key instead of
            // throwing the "NamedResource Not Found" COMException (HRESULT 0x80073B17) that
            // ResourceLoader.GetString raises. This avoids first-chance COM exception noise in the
            // debugger while preserving the same fallback-to-key behavior for untranslated keys.
            var resourceManager = GetOrCreateManager();
            var resourceMap = resourceManager?.MainResourceMap;
            if (resourceMap is null)
            {
                return resourceKey;
            }

            // A resw key is registered under the subtree named after its file, so look at the map root
            // first (keys declared at the root resolve there) and then under `Resources/`, which is where
            // Strings/en-us/Resources.resw lands. Without the second form a code-side lookup such as
            // `"AppDisplayName".GetLocalized()` silently returned the key itself, which is what put
            // "AppDisplayName" in the window title bar and in the About version line.
            //
            // MakePri additionally treats a dot in a resw name as a subtree separator: the key `A.B` is
            // stored as subtree `A` plus resource `B`, so its URI is `Resources/A/B` and the dotted form is
            // not present in the map at all. Looking up `Service_Settings.Title` verbatim therefore missed
            // even after the `Resources/` fallback was added, which is why every label in the on-host
            // service (`Service_Settings.*`, `Service_Tray.*`, `Service_Shell.*`, `Service_Status.*`)
            // rendered as its own resource key. Resolve the segmented form as well.
            var candidate = resourceMap.TryGetValue(resourceKey)
                ?? resourceMap.TryGetValue($"{ResourcesSubtreeName}/{resourceKey}")
                ?? resourceMap.TryGetValue(ToResourcePath(resourceKey))
                ?? resourceMap.TryGetValue($"{ResourcesSubtreeName}/{ToResourcePath(resourceKey)}");
            if (candidate is null)
            {
                return resourceKey;
            }

            var value = candidate.ValueAsString;
            return string.IsNullOrWhiteSpace(value) ? resourceKey : value;
        }
        catch (COMException)
        {
            return resourceKey;
        }
        catch (FileNotFoundException)
        {
            return resourceKey;
        }
    }

    /// <summary>
    /// Converts a resw key to the resource path MakePri stores it under, where every dot becomes a
    /// subtree separator (the key <c>A.B</c> lives at <c>A/B</c>).
    /// </summary>
    /// <param name="resourceKey">The resw key, for example <c>Service_Settings.Title</c>.</param>
    /// <returns>The segmented resource path, for example <c>Service_Settings/Title</c>.</returns>
    private static string ToResourcePath(string resourceKey) =>
        resourceKey.Replace('.', '/');

    private static ResourceManager? GetOrCreateManager()
    {
        if (s_resourceManager is not null)
        {
            return s_resourceManager;
        }

        lock (s_managerSync)
        {
            if (s_resourceManager is not null)
            {
                return s_resourceManager;
            }

            try
            {
                s_resourceManager = new ResourceManager();
            }
            catch (COMException)
            {
                return null;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        return s_resourceManager;
    }
}
