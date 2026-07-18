namespace MTM_Waitlist.Core.Models;

// Model for the SampleDataService. Replace with your own model.
public class SampleCompany
{
    // Edge Case 1: Initialize strings to string.Empty to satisfy the compiler and ensure UI stability
    public string CompanyID { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string ContactName { get; set; } = string.Empty;

    public string ContactTitle { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Fax { get; set; } = string.Empty;

    // Edge Case 2: Always instantiate child collections to block foreach/LINQ iteration crashes
    public ICollection<SampleOrder> Orders { get; set; } = new List<SampleOrder>();
}
