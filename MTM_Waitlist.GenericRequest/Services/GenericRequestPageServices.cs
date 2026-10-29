using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.GenericRequest.Services;

/// <summary>
/// The host the generic request screens resolve their view models from.
/// </summary>
/// <remarks>
/// <c>Frame.Navigate</c> activates a page through its XAML type and cannot pass a constructor argument, so each
/// page also carries a parameterless constructor that asks this for its view model. The host configures it once at
/// start-up; the preview application does that today, and the shell will do it when these screens are wired into
/// the application.
/// </remarks>
public static class GenericRequestPageServices
{
    private static IServiceProvider? s_services;

    /// <summary>Tell the screens where their view models come from. Called once by the host.</summary>
    public static void Configure(IServiceProvider services) => s_services = services;

    /// <summary>The view model of type <typeparamref name="T"/>, or a clear failure when no host was configured.</summary>
    public static T Get<T>()
        where T : GenericRequestStepViewModel =>
        s_services?.GetService(typeof(T)) is T service
            ? service
            : throw new InvalidOperationException(
                $"The generic request screens have no host, so {typeof(T).Name} cannot be resolved. "
                + $"Call {nameof(GenericRequestPageServices)}.{nameof(Configure)} before navigating to a screen.");
}
