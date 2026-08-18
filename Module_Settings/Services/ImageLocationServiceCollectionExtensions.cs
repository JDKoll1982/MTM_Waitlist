using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Extension methods for registering image location services in the Dependency Injection container.
/// Call from App.xaml.cs during application startup.
/// </summary>
public static class ImageLocationServiceCollectionExtensions
{
    /// <summary>
    /// Registers all image location services in the DI container.
    /// Must be called during application initialization before any image location queries.
    /// 
    /// Registered Services:
    /// - IImageLocationService (singleton) → ImageLocationService
    /// - IRequestTypeDisplayLabelService (singleton) → RequestTypeDisplayLabelService
    /// - IRequestSubtypeDisplayLabelService (singleton) → RequestSubtypeDisplayLabelService
    /// - IImageStorageConfigurationResolver (singleton) → ImageStorageConfigurationResolver
    /// - IWorkCenterCatalogService (from Module_Shared, assumed pre-registered)
    /// - ImageStorageOptions (from IOptions<ImageStorageOptions>)
    /// 
    /// NOTE: IConfigSettingsValueService must be registered separately (in the data access layer)
    /// before calling this method. The configuration resolver depends on this service for
    /// reading database overrides. If not registered, dependency injection will fail.
    /// 
    /// NOTE: IWorkCenterCatalogService must be registered in Module_Shared before calling this method.
    /// The image location service depends on this for loading active work centers.
    /// 
    /// Typical Usage in App.xaml.cs:
    /// 
    /// var services = new ServiceCollection();
    /// // First register data access services (IConfigSettingsValueService implementation)
    /// services.AddConfigSettingsValueService(); // When created in Phase 2
    /// // Register Module_Shared services (IWorkCenterCatalogService)
    /// services.AddWorkCenterServices(); // Already registered in Module_Shared
    /// // Then register image location services
    /// services.AddImageLocationServices(configuration);
    /// var serviceProvider = services.BuildServiceProvider();
    /// 
    /// // Then initialize at startup:
    /// var imageLocationService = serviceProvider.GetRequiredService<IImageLocationService>();
    /// await imageLocationService.InitializeAsync();
    /// </summary>
    /// <param name="services">The IServiceCollection to register services in</param>
    /// <param name="configuration">The IConfiguration for binding ImageStorageOptions</param>
    /// <returns>The IServiceCollection for chaining</returns>
    /// <exception cref="ArgumentNullException">If services or configuration is null</exception>
    public static IServiceCollection AddImageLocationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        // Register configuration options
        services.Configure<ImageStorageOptions>(
            configuration.GetSection(ImageStorageOptions.SectionName));

        // Register display label services (singletons - initialized once, never change for session)
        services.AddSingleton<IRequestTypeDisplayLabelService, RequestTypeDisplayLabelService>();
        services.AddSingleton<IRequestSubtypeDisplayLabelService, RequestSubtypeDisplayLabelService>();

        // Register configuration resolver (singleton - coordinates with IOptions<T>)
        services.AddSingleton<IImageStorageConfigurationResolver, ImageStorageConfigurationResolver>();

        // Register override services (singletons - stateless queries, can be reused)
        services.AddSingleton<IImageOverrideReadService, ImageOverrideReadService>();
        services.AddSingleton<IImageOverrideWriteService, ImageOverrideWriteService>();

        // Register image storage service (singleton - file validation and copy operations)
        services.AddSingleton<IImageStorageService, ImageStorageService>();

        // Register main orchestration service (singleton - coordinates all sub-services)
        services.AddSingleton<IImageLocationService, ImageLocationService>();

        return services;
    }

    /// <summary>
    /// Registers only the image storage configuration resolver (without display label services).
    /// Use this if you need configuration resolution only, without the full image location service.
    /// </summary>
    /// <param name="services">The IServiceCollection to register services in</param>
    /// <param name="configuration">The IConfiguration for binding ImageStorageOptions</param>
    /// <returns>The IServiceCollection for chaining</returns>
    public static IServiceCollection AddImageStorageConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        services.Configure<ImageStorageOptions>(
            configuration.GetSection(ImageStorageOptions.SectionName));

        services.AddSingleton<IImageStorageConfigurationResolver, ImageStorageConfigurationResolver>();

        return services;
    }
}

/// <summary>
/// Service initialization extension for coordinating startup.
/// Call this during application startup to initialize all image location services.
/// 
/// Typical Usage in App.xaml.cs startup:
/// 
/// public App()
/// {
///     var host = Host.CreateDefaultBuilder()
///         .ConfigureServices((context, services) =>
///         {
///             services.AddImageLocationServices(context.Configuration);
///         })
///         .Build();
///     
///     var imageLocationService = host.Services.GetRequiredService<IImageLocationService>();
///     await imageLocationService.InitializeAsync();
/// }
/// </summary>
public interface IImageLocationServiceInitializer
{
    /// <summary>
    /// Initializes all image location services at application startup.
    /// </summary>
    /// <param name="serviceProvider">The dependency injection service provider</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task InitializeImageLocationServicesAsync(IServiceProvider serviceProvider);
}

/// <summary>
/// Implementation of IImageLocationServiceInitializer.
/// Handles coordinated startup of all image location services with error handling.
/// </summary>
public sealed class ImageLocationServiceInitializer : IImageLocationServiceInitializer
{
    private readonly ILogger<ImageLocationServiceInitializer> _logger;

    /// <summary>
    /// Initializes a new ImageLocationServiceInitializer.
    /// </summary>
    /// <param name="logger">Logger for startup diagnostics</param>
    public ImageLocationServiceInitializer(ILogger<ImageLocationServiceInitializer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task InitializeImageLocationServicesAsync(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        try
        {
            _logger.LogInformation("Starting image location service initialization...");

            var imageLocationService = serviceProvider.GetRequiredService<IImageLocationService>();
            await imageLocationService.InitializeAsync();

            _logger.LogInformation("Image location services initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to initialize image location services. Application startup blocked.");
            throw;
        }
    }
}

/// <summary>
/// Extension methods for IServiceProvider to get initialized image location services.
/// Use these to safely retrieve the service with validation.
/// </summary>
public static class ImageLocationServiceProviderExtensions
{
    /// <summary>
    /// Gets an initialized IImageLocationService instance.
    /// Throws if service is not initialized.
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <returns>The initialized service instance</returns>
    /// <exception cref="InvalidOperationException">If service not initialized</exception>
    public static IImageLocationService GetInitializedImageLocationService(
        this IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        var service = serviceProvider.GetRequiredService<IImageLocationService>();
        
        if (!service.IsInitialized)
        {
            throw new InvalidOperationException(
                "Image location service is not initialized. Call InitializeAsync() first.");
        }

        return service;
    }

    /// <summary>
    /// Tries to get an initialized IImageLocationService instance.
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="service">The service instance if found and initialized; null otherwise</param>
    /// <returns>True if service is available and initialized; false otherwise</returns>
    public static bool TryGetInitializedImageLocationService(
        this IServiceProvider serviceProvider,
        out IImageLocationService? service)
    {
        service = null;

        try
        {
            service = serviceProvider.GetService<IImageLocationService>();
            return service?.IsInitialized ?? false;
        }
        catch
        {
            return false;
        }
    }
}
