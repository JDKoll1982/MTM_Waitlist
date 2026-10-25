using Microsoft.Extensions.DependencyInjection;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Services.DependencyInjection;

// The accessibility is omitted because the type's public part is declared in ServiceRegistrationExtensions.cs. The
// naming guard reads a repeated `public` on a partial class as a public type whose name does not match the file's,
// which is why the sibling contribution files sit in the grandfather list; this one is not going to join them.
static partial class ServiceRegistrationExtensions
{
    /// <summary>
    /// The scrap-lugger arrangement's one seam, shared by the waitlist card path (which reads a snapshot per list
    /// load) and the Settings editor (which maintains it). It is a singleton because it holds no state of its own
    /// beyond the store options and the seam beneath it.
    /// </summary>
    static partial void RegisterLuggerArrangementServices(IServiceCollection services)
    {
        services.AddSingleton<ILuggerArrangementService, LuggerArrangementService>();
    }
}
