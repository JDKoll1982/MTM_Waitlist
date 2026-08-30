using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Maps a ViewModel full-name key to its page <see cref="Type"/>. Library-resident and
/// app-agnostic: page registrations are supplied by the app composition root via
/// <see cref="Configure{TViewModel,TPage}"/>, keeping this type testable without the WinExe app.
/// </summary>
public class PageService : IPageService
{
    private readonly Dictionary<string, Type> _pages = new();

    public PageService()
    {
    }

    public Type GetPageType(string key)
    {
        Type? pageType;
        lock (_pages)
        {
            if (!_pages.TryGetValue(key, out pageType))
            {
                throw new ArgumentException($"Page not found: {key}. Did you forget to call PageService.Configure?");
            }
        }

        return pageType;
    }

    public void Configure<TViewModel, TPage>()
        where TViewModel : class
        where TPage : class
    {
        lock (_pages)
        {
            var key = typeof(TViewModel).FullName!;
            if (_pages.ContainsKey(key))
            {
                throw new ArgumentException($"The key {key} is already configured in PageService");
            }

            var type = typeof(TPage);
            if (_pages.ContainsValue(type))
            {
                throw new ArgumentException($"This type is already configured with key {_pages.First(p => p.Value == type).Key}");
            }

            _pages.Add(key, type);
        }
    }
}
