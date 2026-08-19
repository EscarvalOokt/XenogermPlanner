# Xenogerm Planner compatibility and known limitations

This document defines the public support scope and known limitations for Xenogerm Planner.

It describes supported product behavior, not internal implementation order or a guarantee of compatibility with every third-party genetics mod.

## Supported baseline

The supported release baseline is:

* RimWorld 1.6;
* Biotech enabled;
* Xenogerm Planner loaded after Biotech;
* English, Russian, or Ukrainian interface language;
* the current Xenogerm Planner save format.

The production mod does not require or include Harmony.

Compatibility and resilience validation for the release baseline covered clean Biotech and representative data-driven modded-gene configurations that continue to use standard RimWorld genetics boundaries.

## Supported vanilla boundaries

Xenogerm Planner integrates through standard RimWorld 1.6 and Biotech data and runtime boundaries, including:

* runtime `GeneDef` entries;
* physical vanilla `Genepack` objects and their `GeneSet` data;
* the standard current-map holder graph used to find loose and contained things;
* connected Gene Banks and Gene Processors exposed by a selected Gene Assembler;
* pawn gene data used by the vanilla Gene Extractor selection rules;
* save-local vanilla `CustomXenogerm` templates;
* runtime premade `XenotypeDef` entries used as plan-creation sources;
* saved custom xenotype `.xtp` files loaded through the vanilla xenotype loading flow;
* active orbital `TradeShip` instances exposed by the current map and their current `ITrader.Goods`;
* visiting trader caravans exposed through current-map trade-with-colony Lords, their trader pawns and current `ITrader.Goods`;
* RimWorld Scribe save data for mod-owned plans.

The supported baseline assumes those boundaries retain their normal meaning.

## Third-party gene mods

### Expected compatibility

Data-driven gene mods are expected to work when they add normal runtime `GeneDef` entries and continue to use vanilla-compatible:

* genepacks and gene sets;
* map holders and storage containers;
* Gene Assembler facilities and input discovery;
* Gene Extractor gene data;
* xenogerm template save and load behavior;
* Scribe definition references.

Xenogerm Planner uses the active runtime gene catalog, so supported modded genes can be selected, persisted by `defName`, analyzed, and included in plans without a per-mod adapter.

### Unverified or unsupported behavior

A general compatibility guarantee does not apply to mods that:

* replace vanilla genepacks, Gene Sets, Gene Assemblers, Gene Extractors, or xenogerm templates with unrelated systems;
* substantially change the relevant behavior through patches;
* store gene items outside the standard map-holder graph;
* replace vanilla Scribe resolution or `GeneDef` lifecycle behavior;
* change conflict, prerequisite, extraction, or assembly semantics after Xenogerm Planner has analyzed them.

No individual third-party mod is part of the declared support list unless it is named in a later release policy. Absence from such a list does not prove incompatibility; it means the configuration is not covered by a specific guarantee.

## Save compatibility

The current save format establishes the save-compatibility baseline.

### Adding Xenogerm Planner to an existing save

Xenogerm Planner can be added to an existing save. No pre-existing Planner data is required: an existing colony starts with no Planner-owned plans and can begin using the Planner normally.

### Removing Xenogerm Planner from an existing save

Xenogerm Planner can be removed from an existing save, with one expected first-load caveat.

A save written while the mod is active contains Planner-owned `GameComponent` records. On the first load after disabling the mod, RimWorld cannot resolve those missing component classes and may write missing-class / `GameComponent` deserialization errors to the log. The tested save still loaded and continued normally.

After saving the game once without Xenogerm Planner:

* the stale Planner component records are no longer present in the rewritten save;
* a subsequent load without the mod no longer produces those Planner missing-component errors;
* saved Xenogerm Planner plans, planning modes and plan-owned notification state are no longer present and should be treated as lost;
* normal RimWorld `CustomXenogerm` templates previously created through the Planner remain available because they are stored in RimWorld's vanilla xenogerm-template database rather than as Planner-owned plan data.

The validated removal scenario contained multiple saved Planner plans and a vanilla xenogerm template created from a ready plan. Comparison of the save before removal and the rewritten save after removal confirmed that Planner-owned component data was removed while the vanilla template remained unchanged.

### Temporarily missing planned gene definitions

A saved `XenogermPlan` stores desired gene requirements by `GeneDef.defName`. When a planned definition is temporarily unavailable:

* the unresolved name remains part of the plan;
* the plan enters a degraded state;
* the missing requirement is not treated as satisfied;
* other valid plans continue to load and operate independently.

This behavior belongs to Xenogerm Planner's own plan persistence.

Vanilla `CustomXenogerm` templates use a different save path. RimWorld can remove unresolved genes from a loaded vanilla `GeneSet`. Recovery of those removed template entries after restoring the source mod is not guaranteed, especially after the affected save has been written again. Source-based plan creation can only use the gene data still present in the current runtime template.

## Current-map inventory scope

Product-level plan readiness uses physical non-empty genepacks recognized on the current active map.

The scope can include:

* loose spawned genepacks;
* genepacks stored in Gene Banks or other standard published holders;
* nested genepacks rooted in a spawned current-map holder;
* genepacks carried or held by a spawned pawn.

The scope excludes:

* caravans and other world-rooted holders;
* travelling transporters after they leave the map;
* genepacks on another map;
* detached objects;
* Passing Ship trade stock;
* explicitly foreign-faction holder branches.

Forbidden state, Gene Bank power, and connection to a Gene Assembler do not by themselves define product-inventory membership.

A selected Gene Assembler has a separate scope containing only the physical genepacks visible through its connected Gene Banks. Plan readiness and selected-assembler readiness can therefore differ.

## Trader advisory scope

Built-in trader advisory is limited to supported traders on the current active map:

* orbital traders represented by active `TradeShip` instances;
* visiting trader caravans represented by the exact trader pawn resolved from a current `LordJob_TradeWithColony`.

Current trader stock is read from native `ITrader.Goods` state and refreshed through Planner-owned current-state polling. Relevant trader genepacks remain advisory offers: they are not added to the physical product inventory, do not make a plan ready and do not alter the persisted readiness-notification cursor.

Trader advisory does not automate purchases or open an unverified orbital trade/navigation path. Visiting-caravan notifications can use the current trader pawn as a map-navigation target; orbital notifications are text-only.

## Functional limitations

* A plan represents the desired physical gene payload of a xenogerm. It does not predict the final phenotype or complete active gene state of a specific pawn.
* Conflicting genes can remain valid physical requirements even when one may suppress another after implantation.
* Potential donors are advisory. A positive donor result means the desired gene can participate in at least one valid extraction sequence, not that the next extraction is guaranteed to produce it.
* Donor analysis does not require a currently usable Gene Extractor, path, reservation, or hauling setup.
* Xenogerm Planner does not automate extraction, trading, assembly jobs, implantation, or pawn assignment.
* Genepacks are not reserved between plans. Several plans may refer to the same reusable physical packs.
* Product-level planning covers only the current active map, not the entire faction across maps and caravans.
* Passing Ship offers and other supported temporary trader offers may produce advisory notifications but do not satisfy readiness.
* Saved custom xenotype `.xtp` files can be used as plan-creation sources, but the mod does not manage or modify the global `.xtp` custom xenotype library.

## Template search limitation

Template creation searches deterministic combinations of current physical genepack compositions.

Every candidate shown by the Planner is valid for the selected planning goal and excludes redundant composition groups. The search also establishes a valid fallback candidate before extended enumeration.

To keep combinatorially large searches bounded, enumeration uses a deterministic node budget and retains a limited number of the best candidates found.

When the result is incomplete:

* additional valid alternatives may exist;
* the interface explicitly warns that not every alternative was examined or displayed;
* the Automatic candidate is the best candidate found by the bounded search;
* global optimality is not guaranteed.

When enumeration completes and all valid candidates fit within the retained limit, the Automatic candidate is globally best under the Planner's documented deterministic ordering.

## Performance limitation

Representative Planner workflows were profiled after introducing transient analysis-state caching. The tested open-window scenarios no longer showed a persistent Planner-specific performance regression. The main Planner UI also uses list virtualization, cached layout geometry, identity-based presentation projections and bounded template search.

Results depend on hardware, game state and mod configuration and are not a universal frame-rate guarantee.

## Support boundary summary

The declared supported configuration is RimWorld 1.6 with Biotech and the standard genetics boundaries described above.

Adding Xenogerm Planner to an existing save is supported. Removing it is also supported with the documented one-time missing-component log errors on the first load; saving once without the mod removes the stale Planner component records, and later loads no longer report those Planner components. Planner-owned plans are not preserved after that rewrite, while vanilla xenogerm templates created through the Planner remain vanilla save data.

Failures at supported integration boundaries are expected to degrade safely without preventing save loading, plan persistence, or access to the core Planner interface. Configurations that replace those boundaries remain unverified unless a later release explicitly expands the support policy.