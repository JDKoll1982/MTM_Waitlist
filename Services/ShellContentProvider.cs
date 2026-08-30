using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Services;

public sealed class ShellContentProvider : IShellContentProvider
{
    private readonly IServiceProvider _serviceProvider;

    public ShellContentProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public FrameworkElement CreateShellContent() => _serviceProvider.GetRequiredService<Module_Core.Views.ShellPage>();
}
