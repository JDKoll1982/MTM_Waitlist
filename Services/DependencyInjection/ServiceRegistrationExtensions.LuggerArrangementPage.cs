using Microsoft.Extensions.DependencyInjection;

using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Module_Settings.Views;

namespace MTM_Waitlist.Services.DependencyInjection;

// The accessibility is omitted here because the type's public part is declared in
// ServiceRegistrationExtensions.cs. Every other contribution file repeats the modifier, and the naming guard then
// reads the partial class as a public type whose name does not match the file's; omitting it keeps the type public
// without adding another line to the grandfather list.
/// <summary>
/// The lugger arrangement's page registration (T054). One file per story phase, so no phase edits the composition
/// root's factory.
/// </summary>
/// <remarks>
/// The route is contributed here rather than written into the main list for the same reason each other feature's
/// pages are: the registration and the markup it needs land together, and the page service applies the
/// contributions as it builds its route table. The route key is the view model's full name, which is what
/// <see cref="SettingsViewModel.LuggerArrangementViewModelName"/> names.
/// </remarks>
static partial class ServiceRegistrationExtensions
{
    /// <summary>
    /// Registers the lugger arrangement's view model and page, and contributes its route.
    /// </summary>
    static partial void RegisterLuggerArrangementPage(IServiceCollection services)
    {
        services.AddTransient<LuggerArrangementViewModel>();
        services.AddTransient<LuggerArrangementPage>();

        s_pageRouteRegistrations.Add(pageService =>
            pageService.Configure<LuggerArrangementViewModel, LuggerArrangementPage>());
    }
}
