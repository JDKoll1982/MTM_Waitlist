using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Extension methods for querying inventories through IImageLocationService.
/// Provides convenient access to request type and subtype inventory data.
/// Inventories are backed by static RequestTypeInventory and RequestSubtypeInventory classes.
/// </summary>
public static class ImageLocationServiceInventoryExtensions
{
    /// <summary>
    /// Gets all request types from the inventory.
    /// </summary>
    /// <param name="service">The image location service</param>
    /// <returns>Collection of all request types with their metadata</returns>
    /// <exception cref="InvalidOperationException">If service is not initialized</exception>
    public static IReadOnlyList<RequestTypeItem> GetAllRequestTypes(this IImageLocationService service)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (!service.IsInitialized)
        {
            throw new InvalidOperationException(
                "Image location service not initialized. Call InitializeAsync() first.");
        }

        return RequestTypeInventory.Items;
    }

    /// <summary>
    /// Gets a single request type by its stable ID.
    /// </summary>
    /// <param name="service">The image location service</param>
    /// <param name="requestTypeId">The stable GUID identifier</param>
    /// <returns>The request type item, or null if not found</returns>
    /// <exception cref="InvalidOperationException">If service is not initialized</exception>
    public static RequestTypeItem? GetRequestType(this IImageLocationService service, Guid requestTypeId)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (!service.IsInitialized)
        {
            throw new InvalidOperationException(
                "Image location service not initialized. Call InitializeAsync() first.");
        }

        return RequestTypeInventory.GetById(requestTypeId);
    }

    /// <summary>
    /// Gets all subtypes for a specific parent request type.
    /// </summary>
    /// <param name="service">The image location service</param>
    /// <param name="parentRequestTypeId">The stable GUID of the parent request type</param>
    /// <returns>Group of subtypes under this parent, or null if parent not found</returns>
    /// <exception cref="InvalidOperationException">If service is not initialized</exception>
    public static RequestSubtypeGroup? GetSubtypesForRequestType(
        this IImageLocationService service, Guid parentRequestTypeId)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (!service.IsInitialized)
        {
            throw new InvalidOperationException(
                "Image location service not initialized. Call InitializeAsync() first.");
        }

        return RequestSubtypeInventory.GetByParentId(parentRequestTypeId);
    }

    /// <summary>
    /// Gets a single subtype by its stable ID.
    /// Returns both the parent group and the subtype item for convenience.
    /// </summary>
    /// <param name="service">The image location service</param>
    /// <param name="subtypeId">The stable GUID identifier</param>
    /// <returns>Tuple with parent group and subtype item, or (null, null) if not found</returns>
    /// <exception cref="InvalidOperationException">If service is not initialized</exception>
    public static (RequestSubtypeGroup? group, RequestSubtypeItem? item) GetSubtype(
        this IImageLocationService service, Guid subtypeId)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (!service.IsInitialized)
        {
            throw new InvalidOperationException(
                "Image location service not initialized. Call InitializeAsync() first.");
        }

        return RequestSubtypeInventory.GetById(subtypeId);
    }

    /// <summary>
    /// Gets all subtypes across all parents (flattened list).
    /// Useful for auditing and inventory validation.
    /// </summary>
    /// <param name="service">The image location service</param>
    /// <returns>Collection of all 24 subtypes from all parents</returns>
    /// <exception cref="InvalidOperationException">If service is not initialized</exception>
    public static IEnumerable<RequestSubtypeItem> GetAllSubtypes(this IImageLocationService service)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (!service.IsInitialized)
        {
            throw new InvalidOperationException(
                "Image location service not initialized. Call InitializeAsync() first.");
        }

        return RequestSubtypeInventory.Groups.SelectMany(g => g.Subtypes);
    }

    /// <summary>
    /// Gets the total count of request types.
    /// </summary>
    /// <param name="service">The image location service</param>
    /// <returns>The number of request types (8)</returns>
    /// <exception cref="InvalidOperationException">If service is not initialized</exception>
    public static int GetRequestTypeCount(this IImageLocationService service)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (!service.IsInitialized)
        {
            throw new InvalidOperationException(
                "Image location service not initialized. Call InitializeAsync() first.");
        }

        return RequestTypeInventory.Items.Count;
    }

    /// <summary>
    /// Gets the total count of all subtypes across all parents.
    /// </summary>
    /// <param name="service">The image location service</param>
    /// <returns>The number of subtypes (24)</returns>
    /// <exception cref="InvalidOperationException">If service is not initialized</exception>
    public static int GetSubtypeCount(this IImageLocationService service)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (!service.IsInitialized)
        {
            throw new InvalidOperationException(
                "Image location service not initialized. Call InitializeAsync() first.");
        }

        return RequestSubtypeInventory.TotalSubtypeCount;
    }

    /// <summary>
    /// Gets the count of subtypes for a specific parent request type.
    /// </summary>
    /// <param name="service">The image location service</param>
    /// <param name="parentRequestTypeId">The stable GUID of the parent request type</param>
    /// <returns>The number of subtypes under this parent, or 0 if parent not found</returns>
    /// <exception cref="InvalidOperationException">If service is not initialized</exception>
    public static int GetSubtypeCountForRequestType(this IImageLocationService service, Guid parentRequestTypeId)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (!service.IsInitialized)
        {
            throw new InvalidOperationException(
                "Image location service not initialized. Call InitializeAsync() first.");
        }

        var group = RequestSubtypeInventory.GetByParentId(parentRequestTypeId);
        return group?.Subtypes.Count ?? 0;
    }
}
