using Microsoft.Windows.ApplicationModel.Resources;
using System.Runtime.InteropServices;

namespace MTM_Waitlist.Module_Core.Helpers;

public static class ResourceExtensions
{
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

            var candidate = resourceMap.TryGetValue(resourceKey);
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
