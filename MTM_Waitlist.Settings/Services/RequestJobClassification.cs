using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// The two answers one Visual read gives about a job's part: what <b>state</b> the job's product is in and what
/// <b>kind</b> of part it is.
/// </summary>
/// <param name="Disposition">
/// The job's product disposition — finished goods, work in process, or stock sitting in a non-conforming area. It
/// decides which product pickup is offered. It is never "outside service": a job routed to the subcontracted
/// resource is work in process whose WIP tag names that resource on its next-resource line (IT, 2026-11-04).
/// </param>
/// <param name="PartType">
/// The part's own kind, read from the plant's <c>PRODUCT_CODE</c> and <c>COMMODITY_CODE</c> — coil, flatstock,
/// hardware, stamped part, tooling and so on. It is what the card's <c>Part Type</c> cell draws.
/// </param>
/// <param name="IsOutsideServiceWork">
/// Whether the job has subcontracted work to collect, which is what offers the <c>pickup-outside-service</c> Item.
/// It is a workflow signal read from the routing, deliberately separate from the disposition above.
/// </param>
public readonly record struct RequestJobClassification(
    RequestDisposition Disposition,
    RequestPartType PartType,
    bool IsOutsideServiceWork);
