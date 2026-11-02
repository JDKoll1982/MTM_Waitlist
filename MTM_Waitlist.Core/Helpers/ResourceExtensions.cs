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

    /// <summary>
    /// Each referenced library's resource map, resolved on first use and kept.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A resw that lives in a class library is merged into the consuming application's resource index under the
    /// library's own root namespace, so a key declared in <c>MTM_Waitlist.GenericRequest</c> is stored at
    /// <c>MTM_Waitlist.GenericRequest/Resources/&lt;key&gt;</c> and is not reachable from the map root at all. Only
    /// the application's own resw lands at <c>Resources/</c>.
    /// </para>
    /// <para>
    /// Without these maps, a screen hosted from a library renders every label as its own resource key: the generic
    /// request flow showed "GenericRequest_WorkCenter_Title.Text" where its heading belongs. The subtree is named
    /// after the root namespace, which in this repository is the assembly name, so each referenced
    /// <c>MTM_Waitlist.*</c> assembly is asked whether it has one. Nothing is guessed beyond that one equality, and an
    /// assembly with no resw simply has no subtree.
    /// </para>
    /// <para>
    /// Resolved per assembly rather than once for the whole process, because the first label this application renders
    /// happens at start-up, long before the generic request assemblies are loaded. A single pass there would have
    /// cached an empty set and never looked again.
    /// </para>
    /// </remarks>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ResourceMap?> s_libraryResourceMaps =
        new(StringComparer.Ordinal);

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

            // A key declared by a class library is not at the map root: it sits under that library's own
            // namespace, so it is looked for there before the key is given up on.
            if (candidate is null)
            {
                candidate = TryLibraryMaps(resourceKey);
            }

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

    /// <summary>
    /// The <c>Resources</c> subtree of every referenced library that declares one, resolved once per assembly.
    /// </summary>
    /// <remarks>
    /// A library's resw is merged under its root namespace, so its subtree is
    /// <c>&lt;root namespace&gt;/Resources</c>. The root namespace equals the assembly name throughout this
    /// repository, which is the single equality this discovery relies on.
    /// </remarks>
    private static ResourceCandidate? TryLibraryMaps(string resourceKey)
    {
        var rootMap = GetOrCreateManager()?.MainResourceMap;
        if (rootMap is null)
        {
            return null;
        }

        var assemblyNames = AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetName().Name)
            .Where(name => !string.IsNullOrWhiteSpace(name)
                && name.StartsWith("MTM_Waitlist.", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal);

        foreach (var assemblyName in assemblyNames)
        {
            var map = s_libraryResourceMaps.GetOrAdd(assemblyName!, name => ResolveLibraryMap(rootMap, name));
            if (map is null)
            {
                continue;
            }

            try
            {
                // The dot-segmentation rule applies inside a library subtree too, and the library's resw sits under
                // its own `Resources` subtree, so the segmented form is tried first and the verbatim key second.
                var found = map.TryGetValue(ToResourcePath(resourceKey))
                    ?? map.TryGetValue(resourceKey);
                if (found is not null)
                {
                    return found;
                }
            }
            catch (COMException)
            {
                // A map whose index went away answers with a COM failure rather than null; treat it as a miss.
            }
        }

        return null;
    }

    private static ResourceMap? ResolveLibraryMap(ResourceMap rootMap, string assemblyName)
    {
        try
        {
            var assemblyMap = rootMap.TryGetSubtree(assemblyName);

            return assemblyMap?.TryGetSubtree(ResourcesSubtreeName)
                ?? rootMap.TryGetSubtree($"{assemblyName}/{ResourcesSubtreeName}");
        }
        catch (COMException)
        {
            // No subtree for that assembly: it declares no resw, which is the common case.
            return null;
        }
    }

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
