using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// The two lines every waitlist card reads.
/// <para>
/// <b>Line 1</b> is the Item's umbrella phrase and <b>Line 2</b> is the Item's identifier. Both are resolved
/// from the Item the request was raised with, through the catalog and
/// <see cref="RequestItemLine2Resolver"/> — so the card reads <c>category</c> + <c>item</c> and nothing else
/// (<c>contracts/card-and-identifier.md</c> §2–§3, FR-005).
/// </para>
/// <para>
/// There is deliberately <b>no legacy fallback</b>: the retired type/subtype pair is not read here, not stored
/// on the request, and not consulted when an Item cannot be resolved (FR-003, FR-023). An Item the catalog does
/// not describe yields an empty first line and the resolver's configuration report on the second, never an
/// invented phrase.
/// </para>
/// </summary>
public static class WaitlistRequestTitles
{
    private static readonly RequestItemLine2Resolver Resolver = new();

    /// <summary>
    /// Card Line 1 — the umbrella phrase: the Category's own word, or the Item's own phrase where the Item defines
    /// one, resolved against the values the request and the requesting job carry
    /// (<c>contracts/card-and-identifier.md</c> §2). Empty when the stored Item code is not catalogued; a missing
    /// code is reported through Line 2 rather than papered over here.
    /// </summary>
    public static string ResolveLine1(RequestItemDefinition? item, RequestItemLine2Context? context = null)
        => item is null ? string.Empty : Resolver.ResolveLine1(item, context);

    /// <summary>
    /// Card Line 2 — the identifier, resolved from the Item's <see cref="RequestItemDefinition.CardLine2Template"/>
    /// against the job snapshot and the captured answer (<c>contracts/card-and-identifier.md</c> §3). An
    /// unresolvable token renders the Item's own display name and reports the configuration problem: never a
    /// blank, never a fabricated value (FR-026).
    /// </summary>
    public static RequestItemLine2Result ResolveLine2(RequestItemDefinition? item, RequestItemLine2Context context)
        => item is null
            ? new RequestItemLine2Result(
                string.Empty,
                false,
                RequestItemLine2Resolver.ProblemKey,
                RequestItemLine2Resolver.ResolveProblemMessage(null!, null))
            : Resolver.Resolve(item, context ?? new RequestItemLine2Context());

    /// <summary>
    /// The values the Item's templates resolve against, taken from what the request carries and from the
    /// requesting job.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The request holds one captured answer, and where the Item's identifier <b>is</b> that answer the template
    /// says so: a template naming <c>{dunnage_part}</c> is filled from the captured answer, which is the dunnage
    /// part the operator ended on — one the job carried, or their substitute — and a template naming
    /// <c>{component}</c> is filled from it for the same reason, because the component Items ask the operator
    /// which component they need just as the dunnage Items ask which part. The stored request cannot tell those
    /// apart and does not need to: the request stores what the operator said they needed (FR-035, FR-048, FR-051).
    /// </para>
    /// <para>
    /// A template naming <c>{part_number}</c> is filled with the <b>part the request is about</b> — see
    /// <see cref="PartNumberFor"/> — and a template naming <c>{scrap_type}</c> with the scrap type the job has
    /// already decided (FR-030). Both are job values the request never stored, so both come from the job snapshot
    /// the composition root hands in. Each one is filled only when the Item's templates actually name it, so an
    /// Item that asks for none of them is handed none of them and its card is unchanged (FR-053). Every token left
    /// unfilled stays unresolved, with the Item's own display name shown — never a substituted value, never a blank
    /// (<c>contracts/card-and-identifier.md</c> §3).
    /// </para>
    /// <para>
    /// A die request is the one place where the captured answer <b>is</b> a die: <c>input_value</c> carries which
    /// die the request is for, which is what keeps two entries raised from one job apart (FR-054, D22). So where
    /// the request stored a die, that die is the one the card names — matched against the job's own list so its
    /// number and its location are read apart from the stored label rather than guessed at — and the job's primary
    /// die stands in only when nothing was stored, so a request raised before the picker learned to record its die
    /// still renders (FR-057).
    /// </para>
    /// </remarks>
    public static RequestItemLine2Context ResolveContext(
        WaitlistRequest? request,
        RequestJobPartAvailability? jobAvailability = null)
    {
        var answer = request?.InputValue;
        var item = request?.ItemDefinition;
        var job = jobAvailability;

        // The die is handed over when a template names the die at all — as its own composed value, or as either
        // half of it — so an Item that writes `{die}` is not left unresolved while one that writes
        // `{die_number}` still resolves (FR-053, FR-056).
        var namesDie = NamesToken(item, "die")
            || NamesToken(item, "die_number")
            || NamesToken(item, "die_location");

        var storedDie = namesDie ? answer : null;
        var choseADie = !string.IsNullOrWhiteSpace(storedDie);
        var chosen = choseADie ? FindDie(job, storedDie) : null;

        return new RequestItemLine2Context(
            Answer: answer,
            // The component the operator picked is the one answer the request carries, exactly as the dunnage part
            // they picked is — so a component Item's identifier is that answer and never a word for the kind of
            // thing it is (FR-035).
            Component: NamesToken(item, "component") ? answer : null,
            PartNumber: NamesToken(item, "part_number") ? PartNumberFor(item, job) : null,
            ScrapType: NamesToken(item, "scrap_type") ? job?.ScrapType : null,
            DunnagePart: NamesToken(item, "dunnage_part") ? answer : null,
            JobPartNumber: NamesToken(item, "job_part_number") ? job?.JobPartNumber : null,
            DieNumber: namesDie ? (choseADie ? chosen?.PartNumber ?? storedDie : job?.DieNumber) : null,
            DieLocation: namesDie ? (choseADie ? chosen?.Location : job?.DieLocation) : null);
    }

    /// <summary>
    /// The <b>material part</b> the request is about, or <see langword="null"/> when the request names no
    /// material at all.
    /// </summary>
    /// <param name="request">The request, whose stored Item and captured answer are read.</param>
    /// <param name="jobAvailability">The requesting job, which is where a material the request never stored comes from.</param>
    /// <returns>The part number, or <see langword="null"/>.</returns>
    /// <remarks>
    /// This is the same value the card's identifier resolves <c>{part_number}</c> from — the coil or the flatstock
    /// the job holds, or the job's own part number for an Item that is about no particular material — stated once
    /// so the picture the card draws and the part the card names can never disagree (FR-005, FR-019). A request
    /// that names no material answers null rather than a guess, and its card draws the one shared placeholder
    /// instead of artwork chosen on its behalf (FR-014, FR-020).
    /// </remarks>
    public static string? ResolveMaterialPartNumber(
        WaitlistRequest? request,
        RequestJobPartAvailability? jobAvailability = null)
        => ResolveMaterialPartNumber(request?.ItemDefinition, jobAvailability);

    /// <summary>
    /// The same rule, stated on the Item, so a surface that is drawing a request the store has not been asked
    /// about yet — the confirmation step and the preview — names and pictures the same part the card will.
    /// </summary>
    /// <param name="item">The catalogued Item the request was raised with.</param>
    /// <param name="jobAvailability">The requesting job.</param>
    /// <returns>The part number, or <see langword="null"/>.</returns>
    public static string? ResolveMaterialPartNumber(
        RequestItemDefinition? item,
        RequestJobPartAvailability? jobAvailability = null)
    {
        if (item is null || NamesToken(item, "part_number") is false)
        {
            return null;
        }

        return Trimmed(PartNumberFor(item, jobAvailability));
    }

    /// <summary>
    /// The part a request is <b>about</b>, for the Items whose identifier names <c>{part_number}</c>.
    /// <para>
    /// An Item that is about a material the job holds names <b>that material's own number</b> — the coil or the
    /// flatstock the job carries, which is the part a handler goes and gets — and an Item that is about no
    /// particular material (a table assist, and the finished-goods, non-conforming, work-in-process and
    /// outside-service Items) names the <b>job's own part number</b>, which is the part the request is run
    /// against. That is the same value the request page shows for a declared <c>Part</c> row, so the two surfaces
    /// cannot disagree about the same request.
    /// </para>
    /// <para>
    /// Which material an Item is about comes from the same rule that decides whether the Item is offered at all
    /// (<see cref="RequestItemPickerRules.RequiredJobPart"/>), so the picker and the card cannot disagree about
    /// what an Item is about (FR-002, FR-013). An Item the job cannot supply a part for yields nothing, and its
    /// card reports the configuration problem rather than showing a name in place of a part (FR-026).
    /// </para>
    /// </summary>
    private static string? PartNumberFor(RequestItemDefinition? item, RequestJobPartAvailability? job)
    {
        if (item is null || job is null)
        {
            return null;
        }

        // The merged Pickup Item covers a coil or a flatstock, so the material the job actually holds decides
        // (D21); every other material Item names the one material it is about.
        return RequestItemPickerRules.RequiredJobPart(item) switch
        {
            RequestJobPartKind.Coil => Trimmed(job.CoilPartNumber),
            RequestJobPartKind.CoilOrFlatstock => Trimmed(job.CoilPartNumber) ?? Trimmed(job.FlatstockPartNumber),
            RequestJobPartKind.Flatstock => Trimmed(job.FlatstockPartNumber),
            _ => Trimmed(job.JobPartNumber),
        };
    }

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// The die the request stored, found among the dies the job carries so its number and its location can be read
    /// apart. A stored value the job no longer carries — or a job snapshot that was not handed in — resolves to
    /// <see langword="null" />, and the stored value is then treated as the die's number alone rather than being
    /// split at a separator that also occurs inside a die's own number.
    /// </summary>
    private static RequestDiePart? FindDie(RequestJobPartAvailability? job, string? storedDie) =>
        job?.Dies.FirstOrDefault(die => NamesThisDie(die, storedDie));

    /// <summary>
    /// Whether a stored answer names this die. <b>Every spelling the app has ever composed is accepted on purpose:</b>
    /// the die's number alone (<c>FGT0002000</c>, what it composes today), the bracketed form
    /// (<c>FGT0002000 - (DIE SHOP)</c>, shipped briefly on 2026-09-20) and the hyphen-joined form
    /// (<c>FGT0002000-DIE SHOP</c>, the original). A request raised under an earlier rule still carries that
    /// spelling in <c>input_value</c>, and it must keep naming its own die — losing that would silently fall the
    /// card back to the job's primary die, which is the very failure the stored value exists to prevent
    /// (FR-054, FR-057).
    /// </summary>
    private static bool NamesThisDie(RequestDiePart die, string? storedDie)
    {
        var stored = storedDie?.Trim();
        if (string.IsNullOrEmpty(stored))
        {
            return false;
        }

        if (string.Equals(die.Label, stored, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!die.HasLocation)
        {
            return false;
        }

        // The two retired spellings, both of which folded the location into the name.
        return string.Equals($"{die.PartNumber} - ({die.Location})", stored, StringComparison.OrdinalIgnoreCase)
            || string.Equals($"{die.PartNumber}-{die.Location}", stored, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Whether an Item's templates ask for the named token. Both lines are consulted: an Item's first line may
    /// name a job value its identifier does not (the die Items name the job's part number on Line 1 and the die
    /// itself on Line 2), and a value is only handed over where something actually asks for it.
    /// </summary>
    private static bool NamesToken(RequestItemDefinition? item, string token) =>
        NamesTokenIn(item?.CardLine1Template, token) || NamesTokenIn(item?.CardLine2Template, token);

    private static bool NamesTokenIn(string? template, string token) =>
        template?.Contains($"{{{token}}}", StringComparison.OrdinalIgnoreCase) == true;
}
