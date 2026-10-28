# Xenogerm Planner compatibility and known limitations

This document describes supported configurations and player-facing limits. It applies to the complete package for RimWorld 1.6 and 1.7. Current source, assembler metabolism checking and bounded assembler analysis target 1.7; the 1.6 package entry uses the preserved released DLL.

## Supported configuration

- RimWorld 1.6 or 1.7 with the matching package DLL.
- Biotech enabled and loaded before Xenogerm Planner.
- Standard RimWorld genes, genepacks, map holders, Gene Assembler/Gene Extractor behavior, xenogerm templates and Scribe data.
- A language listed in [README](../README.md#languages).

Harmony and a separate UI framework mod are not required. Install the complete package so `LoadFolders.xml` can select shared content and exactly one DLL: `1.6/Assemblies/` for 1.6, or `Current/Assemblies/` for 1.7. Do not combine versioned assemblies with an older unversioned copy.

## Gene mods

Data-driven mods are expected to work when they add normal runtime `GeneDef` entries and continue to use vanilla-compatible genepacks, gene sets, holder traversal, connected facilities, extraction semantics and definition references. Such genes can be selected and saved by `defName` without a per-mod adapter.

There is no general guarantee for mods that replace these systems, hide items outside the published map-holder graph, alter Scribe resolution or substantially change conflict, prerequisite, extraction or assembly rules. Absence of a specific compatibility guarantee does not establish incompatibility.

## Save compatibility

### Adding the mod

Xenogerm Planner can be added to an existing save. It initializes an empty plan collection; pre-existing Planner data is not required.

### Moving from RimWorld 1.6 to 1.7

Use the complete package and retain a copy of the original 1.6 save. The 1.7 game selects the DLL in `Current/Assemblies/` and rebuilds derived inventory, analysis and UI state.

Planner-owned plan identity, names, gene requirements, planning mode and notification state use the same save contract. Check these fields and any generated templates after loading and again after resaving. Other mods and the game's own save migration have separate compatibility boundaries. Validation steps are in [Testing policy](testing.md#runtime-scenarios).

### Removing the mod

A save written with Planner contains its game-component records. The first load after disabling the mod can log missing-class and `GameComponent` deserialization errors. The colony remains accessible.

Save under a new name without Planner and reload that copy. Saving omits the stale component records, so later loads no longer produce those Planner component errors. Planner-owned plans, planning modes and notification state are lost after this rewrite.

Vanilla `CustomXenogerm` templates created through Planner remain in RimWorld's own template database. They do not preserve the independent Planner plan or its notification settings.

### Temporarily missing genes

A plan retains unresolved desired-gene definition names and becomes degraded. Missing requirements are not treated as satisfied, and other valid plans continue to work. A restored definition can resolve again on a later load.

Vanilla templates use a different lifecycle. RimWorld can remove unresolved genes from loaded GeneSets; recovery after restoring a source mod is not guaranteed, especially after resaving. Creating a plan from a template uses only the genes still present in that runtime template.

## Inventory and current-map scope

Product readiness uses physical nonempty genepacks on the active map. Eligible loose, stored, nested and pawn-carried packs can participate when rooted in a spawned current-map holder.

The scope excludes caravans, world-rooted travelling transporters, other maps, detached objects, passing-ship stock and explicitly foreign-faction holder branches. Forbidden state, bank power and assembler connectivity alone do not determine product membership.

A selected Gene Assembler sees only packs exposed by its connected facilities. It can therefore be blocked or not ready while the broader product plan is ready. Maps on different world layers remain distinct active-map scopes.

## Assembler metabolism and search

On RimWorld 1.7, a concrete assembly candidate must have effective metabolism of at least `-5`. The calculation includes all selected physical inputs, applies gene overrides and counts distinct effective genes for metabolism. There is no upper-`+5` rejection.

In **All planned genes**, additional connected packs may compensate metabolism. These packs can add genes and prerequisites, and the full candidate must still satisfy power, complexity, research, capsule and prerequisite requirements. **Exact gene set** forbids genes outside the target.

The assembler search is limited and may stop before checking every possible combination. **Check incomplete** means the Planner has not determined whether the assembler can assemble the plan. It is not a final Blocked or Not ready result.

The UI does not continue the search in the background. **Refresh** repeats the bounded analysis; unchanged inputs may reach the same limit. Altering available packs or the plan can change the search. The incomplete assembler result does not change product readiness, template eligibility or API relevance.

## Donors and trader offers

A potential donor is a spawned current-map pawn whose genes allow the desired gene to participate in a valid extraction sequence. The count does not require a currently usable extractor, hauling route, reservation or other logistics. It is not a probability or a guarantee of the next extraction result.

The Planner monitors orbital traders and visiting trade caravans on the active map and periodically checks their current offers. Offers do not count toward plan readiness until acquired and available on the active map.

Offers already present when you load a game or switch maps do not trigger notifications. Newly relevant offers can produce one combined message per trader. Visiting-caravan notifications can navigate to the trader pawn; orbital notifications are text-only. The mod does not automate purchases or open orbital trade UI.

[Settlement Trade Overview](https://steamcommunity.com/sharedfiles/filedetails/?id=3781528628) can use the optional [API version 1](integration-api.md) to display relevance in its own trade lists. Neither mod is required by the other.

## Template search

Template creation uses genepacks currently available on the active map. Every displayed option satisfies the selected planning mode.

If the search reaches its limit, the UI warns that additional valid alternatives may exist. You can use the automatic choice or select one of the alternatives shown; an incomplete search does not guarantee that the automatic choice is the best possible combination.

Template search has its own limits and eligibility. It does not require a selected assembler or import its live metabolism and infrastructure blockers. Saving a template does not start an assembly job.

## Other functional limits

- Plans describe a distinct physical gene payload, not a recipient pawn's final phenotype or complete active gene state.
- Conflicting genes remain target requirements even when one can suppress another after implantation.
- Packs are not reserved between plans; several plans can rely on the same physical packs.
- The mod does not automate extraction, trading, assembly, implantation or pawn assignment.
- Saved `.xtp` files are creation sources; Planner does not manage or modify the global custom xenotype library.
- Planner indicators belong to Planner-owned or optional consumer-owned surfaces, not vanilla genetics UI.

## UI and performance

The selected Gene Assembler's status updates automatically. Use **Refresh** to repeat the check immediately after changing inventory or infrastructure.

Inventory capture and candidate searches run synchronously. Large or complex data sets can still cause a pause, and template/assembler limits do not provide a universal frame-rate guarantee. Native input, rendering and integration behavior depend on the game, hardware and mod configuration.