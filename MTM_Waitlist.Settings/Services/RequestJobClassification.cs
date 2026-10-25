using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// The two answers one Visual read gives about a job's part: what <b>state</b> the job's product is in and what
/// <b>kind</b> of part it is.
/// </summary>
/// <param name="Disposition">
/// The job's product disposition — finished goods, work in process, outside service, or stock sitting in a
/// non-conforming area. It decides which product pickup is offered.
/// </param>
/// <param name="PartType">
/// The part's own kind, read from the plant's <c>PRODUCT_CODE</c> and <c>COMMODITY_CODE</c> — coil, flatstock,
/// hardware, stamped part, tooling and so on. It is what the card's <c>Part Type</c> cell draws.
/// </param>
public readonly record struct RequestJobClassification(RequestDisposition Disposition, RequestPartType PartType);
