namespace MTM_Waitlist.Module_Core.Contracts.Services;

public interface ISampleDataService
{
    IReadOnlyList<object> GetSampleOrders(string? building = null);
}
