using Microsoft.Windows.ApplicationModel.Resources;
using System.Runtime.InteropServices;

namespace MTM_Waitlist.Module_Core.Helpers;

public static class ResourceExtensions
{
    private static readonly object s_loaderSync = new();
    private static ResourceLoader? s_resourceLoader;

    public static string GetLocalized(this string resourceKey)
    {
        if (string.IsNullOrWhiteSpace(resourceKey))
        {
            return string.Empty;
        }

        try
        {
            var loader = GetOrCreateLoader();
            if (loader is null)
            {
                return resourceKey;
            }

            var localized = loader.GetString(resourceKey);
            return string.IsNullOrWhiteSpace(localized) ? resourceKey : localized;
        }
        catch (COMException)
        {
            // Some WinUI resource lookups throw instead of returning empty when the key is missing.
            return resourceKey;
        }
    }

    private static ResourceLoader? GetOrCreateLoader()
    {
        if (s_resourceLoader is not null)
        {
            return s_resourceLoader;
        }

        lock (s_loaderSync)
        {
            if (s_resourceLoader is not null)
            {
                return s_resourceLoader;
            }

            try
            {
                s_resourceLoader = new ResourceLoader();
                return s_resourceLoader;
            }
            catch (COMException)
            {
                // Loader can fail very early in app startup; retry on the next call.
                return null;
            }
        }
    }

}
