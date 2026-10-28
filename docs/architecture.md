# Xenogerm Planner architecture

This document describes the current RimWorld 1.7 implementation. Player instructions and build commands are in [README](../README.md); support boundaries are in [Compatibility](compatibility.md), runtime scenarios in [Testing policy](testing.md), and the public integration contract in [Integration API](integration-api.md).

## Runtime and package

The production project and tests target `net10.0`. RimWorld 1.7 uses Waylith; the project references the installed `Assembly-CSharp.dll` and `UnityStubs.dll`. The latter supplies the Unity-named compatibility types used by the UI.

The repository's `mod/` directory contains shared content and preserved historical DLLs, including `1.6/Assemblies/XenogermPlanner.dll`. Current sources build the RimWorld 1.7 DLL into `bin/`. Explicit deployment assembles the complete installed mod with the current DLL under `Current/Assemblies/`; no current binary is staged in the repository. `LoadFolders.xml` selects the shared root plus `1.6` on RimWorld 1.6, or the shared root plus `Current` on 1.7.

[Deployment targets](../build/ModDeployment.targets) run only with `DeployToGame=true`. They validate the destination, package identity, load mapping and historical checksums before replacing managed content directories. Shared metadata, Defs, translations and textures are deployed with the assemblies, and obsolete unversioned/current-numbered assembly layouts are removed. Files outside managed directories and a source-absent local `About/PublishedFileId.txt` are preserved. Ordinary builds and tests leave the source package and installation unchanged. The deployed directory is the complete package; deployment to a clean external staging directory prepares a release archive. See README for commands and the cleanup boundary.

The package ID is `escarval.xenogermplanner`; the assembly and root namespace are `XenogermPlanner`. Biotech is required. Harmony is neither required nor bundled.

`Escarval.RimWorld.UI` v1.0.0 is a source dependency. The project imports `Escarval.RimWorld.UI.Source.projitems`, which owns source inclusion, `bin/` and `obj/` exclusions and the `SharedUI` project-view links. Planner supplies compiler settings and game references. No standalone UI project build or separate UI DLL is part of the runtime package.

## Implementation map

| Area | Entry points and ownership |
| --- | --- |
| Plans and persistence | [XenogermPlan](../src/XenogermPlanner/Plans/XenogermPlan.cs), [plan component](../src/XenogermPlanner/Plans/XenogermPlanGameComponent.cs) and [save record](../src/XenogermPlanner/Plans/XenogermPlanSaveRecord.cs) own save-local planning state. |
| Physical inventory | [Inventory scanner](../src/XenogermPlanner/Genes/PlanGenepackInventoryScanner.cs), [policy](../src/XenogermPlanner/Genes/PlanGenepackInventoryPolicy.cs) and [inventory component](../src/XenogermPlanner/Genes/PlanGenepackInventoryGameComponent.cs) own current-map discovery and refresh. |
| Product readiness | [PlanReadinessAnalyzer](../src/XenogermPlanner/Analysis/PlanReadinessAnalyzer.cs) and [combination search](../src/XenogermPlanner/Analysis/PlanGenepackCombinationSearcher.cs) own Coverage and Exact payload decisions. |
| Assembler readiness | [Scope scanner](../src/XenogermPlanner/Assemblers/PlanAssemblerScopeScanner.cs), [live-state reader](../src/XenogermPlanner/Assemblers/PlanAssemblerLiveStateReader.cs), [candidate search](../src/XenogermPlanner/Assemblers/PlanAssemblerCandidateSearcher.cs) and [analyzer](../src/XenogermPlanner/Assemblers/PlanAssemblerReadinessAnalyzer.cs) own concrete connected inputs and live blockers. |
| Donors and templates | [Donor analyzer](../src/XenogermPlanner/Donors/PlanPotentialDonorAnalyzer.cs), [template search](../src/XenogermPlanner/Templates/PlanXenogermTemplateCandidateSearcher.cs) and [template saver](../src/XenogermPlanner/Templates/PlanXenogermTemplateSaver.cs) own their separate workflows. |
| Notifications | [Readiness component](../src/XenogermPlanner/Notifications/PlanReadinessNotificationGameComponent.cs) owns plan transitions; [trader advisory component](../src/XenogermPlanner/Trade/PlanTraderAdvisoryGameComponent.cs) owns transient offers and delivery. |
| UI | [Planner window](../src/XenogermPlanner/UI/XenogermPlannerWindow.cs), [analysis cache](../src/XenogermPlanner/UI/XenogermPlannerWindowAnalysisCache.cs), projections and Planner widgets own presentation and interaction. |
| External API | [XenogermPlannerApi](../src/XenogermPlanner/Api/XenogermPlannerApi.cs) exposes optional read-only API version 1. |

## Plans, creation and persistence

A `XenogermPlan` represents a desired distinct physical gene payload. Its stable ID is its programmatic identity; its display name is editable presentation data. Conflicting genes remain valid target members. The plan does not predict the effective phenotype of a recipient pawn or normalize its target through `NonOverriddenGenes`.

The plan stores its ID, name, desired gene definition names, readiness mode, readiness-notification setting and notification delivery cursor. It does not store physical genepacks, source templates, inventory snapshots, selected assemblers, donor references or analysis results.

Names are trimmed, nonempty and unique using `StringComparer.OrdinalIgnoreCase`. Manual create/rename rejects conflicts. Duplicate, source-based creation and paste allocate a deterministic numeric suffix; duplication first uses the localized copy name. Restore preserves the first name and renames later duplicates deterministically. Name changes do not change plan IDs.

All new-plan sources lead through the Plan Editor:

| Source | Transformation and lifetime |
| --- | --- |
| From scratch | Starts with an empty target and the runtime `GeneDef` catalog. Physical ownership of a gene is not required. |
| Xenogerm template | Flattens the runtime `CustomXenogerm.genesets`, removes exact duplicates and preserves distinct conflicting genes. Revalidates the source before continuing. Template grouping and multiplicity are not plan requirements. |
| Premade xenotype | Reads runtime `XenotypeDef.genes`; vanilla and modded Defs share the same source group. |
| Saved xenotype | Discovers global `.xtp` files, loads through the native version/load boundary, caches unchanged file data and isolates missing or failed sources. |

The source selector owns discovery, validation and preview. It does not expose plan settings. The editor receives source-neutral state, defaults to Coverage with notifications enabled, and creates a plan only when saved. A source is not retained by the saved plan; later source changes do not mutate it.

Duplication immediately creates an independent ID, preserves resolved and unresolved requirements, mode and notification setting, and resets the delivery cursor. Clipboard transfer contains a format version, name, mode and gene definition names. Paste creates a new ID with default notifications and a fresh cursor; it does not transfer save identity or notification state. Unsupported formats and malformed payloads are rejected.

The plan game component owns persistence through Scribe. Unresolved definition names remain requirements, produce degraded plans and can resolve on a later load. They are never silently counted as satisfied. Structurally invalid records are isolated. Derived state is rebuilt after loading.

A save written with Planner includes its game-component records. Without the mod, missing-class and component-deserialization errors can occur on the first load. Saving without Planner omits those records; a subsequent load has no Planner component errors. Planner-owned plans and notification state are lost after that rewrite. Vanilla xenogerm templates remain in the game's template database.

## Product inventory

Product inventory is a derived snapshot of physical nonempty genepacks on the active map. Discovery uses spawned things and recursively published holder contents.

A pack must resolve to the active map and have a spawned physical root. Player-faction and factionless roots are allowed; an explicit foreign faction on the pack or its holder chain excludes it. Passing ships, world-rooted caravans and travelling transporters, other maps, detached objects and structurally unavailable gene collections are excluded. Physical packs are deduplicated by reference identity.

Loose, stored, nested and pawn-carried packs can participate. Forbidden state, container power and assembler connectivity do not define product membership. Equivalent compositions remain separate physical objects in inventory.

The inventory component returns unavailable state without an active map, refreshes on active-map identity change or explicit invalidation, and uses a 600-game-tick fallback. Opening Planner and pressing Refresh invalidate it. Reading `Snapshot` can perform a pending refresh; it is not a passive guarantee that the cached reference remains unchanged.

## Product readiness and diagnostics

Let `T` be the distinct desired gene set and `P` the distinct union of a candidate's physical genepack genes.

- Coverage requires `T` to be a subset of `P`; extras are allowed.
- Exact payload requires `T = P`; every outside gene makes that composition incompatible.
- Repeated occurrences do not create additional target requirements.
- Conflicting genes remain part of both sets.

Product readiness is independent of a selected assembler, its power, complexity capacity, research, capsules and metabolism constraints. It establishes physical payload availability, not immediate assemblability.

State precedence is unavailable inventory → `Unavailable`, unresolved requirements → `Degraded`, empty resolved target → `EmptyTarget`, otherwise `Ready` or `NotReady`. Degraded plans can expose partial diagnostics but never become ready while requirements remain unresolved.

Diagnostics group equivalent compositions for explanations while retaining physical copy counts. Coverage classifies target genes as `Available` or `Missing`. Exact payload adds `ExactPayloadConflict`: sources exist, but all include genes outside the target. A compatible source makes the gene available even when incompatible sources also exist. `ExactPayloadConflict` is distinct from the physical missing-gene set.

Readiness results do not retain a canonical physical candidate. Ordinary conflicts, random-choice groups, mixed conflicts and prerequisites are separate target diagnostics. They neither remove genes nor change target identity.

## Selected assembler and candidate search

Assembler selection is transient window state. It persists while switching plans in the same window, clears on map change or loss of the exact selectable assembler, and is never automatically replaced.

Assembler scope comes from the selected `Building_GeneAssembler.ConnectedFacilities` and their `CompGenepackContainer` contents. Each source retains its exact pack, facility and power state. Unpowered facilities remain visible; only facilities supplying a selected candidate contribute the used-Gene-Bank power blocker. Physical objects are deduplicated by reference identity, and scopes may overlap across assemblers.

Candidate search groups equivalent full compositions. It prefers powered physical representatives, then stable physical keys. Required target and prerequisite genes guide the base search. Coverage can extend a base candidate with additional connected compositions, including metabolism-compensating packs. Exact payload never permits genes outside the target.

Effective metabolism and other biostats depend on the complete candidate, including overrides. A helper is not selected solely because its raw metabolism is positive. Added genes can introduce prerequisites or change which genes are effective. Every accepted candidate must be evaluated against all live constraints.

One assembler analysis has a shared budget of 10,000 DFS state visits across base and helper search. Attempting to visit another unexplored state after that budget marks the search incomplete; merely using the last permitted state does not. A fully usable candidate wins even when found on the last permitted visit.

The assembler result distinguishes:

| Result | Meaning |
| --- | --- |
| `Degraded` / `EmptyTarget` | The plan cannot define a complete nonempty target. |
| `NotReady` | The connected physical gene scope cannot satisfy the planning mode. |
| `Ready` | A concrete candidate passes every supported live requirement. |
| `Blocked` | Search completed without a ready candidate; the result carries a deterministic best blocked candidate. |
| `SearchIncomplete` | The budget prevented completion and no ready candidate was found. Readiness is undetermined; partial best-candidate diagnostics are not presented as a final blocker result. |

A `SearchIncomplete` result does not change product readiness, template eligibility or API relevance. It does not schedule background continuation. Explicit Refresh starts a new analysis; unchanged inputs can reach the same limit again.

## Live assembler requirements

Each candidate is checked using its actual physical packs:

- Every raw gene prerequisite must be present in the candidate.
- The assembler and facilities supplying selected packs must be powered.
- Effective complexity must fit `Building_GeneAssembler.MaxComplexity()`.
- Archite requirements need Archogenetics research and enough non-fogged capsules on the assembler map.
- Effective metabolism must be at least the game's true minimum, `-5`. There is no upper-`+5` rejection.

The blocker reasons are `MissingPrerequisite`, `UsedGeneBankUnpowered`, `AssemblerUnpowered`, `InsufficientComplexity`, `ArchogeneticsResearchMissing`, `InsufficientArchiteCapsules` and `MetabolismTooLow`.

The assembler calculation flattens selected physical inputs and applies the xenogene-typed `NonOverriddenGenes` projection. Complexity sums effective occurrences; metabolism and Archite requirements use distinct effective genes. The result's metabolism diagnostic describes the complete candidate, not the target alone.

When a completed search finds no ready candidate, blocked-candidate ordering prefers prerequisite completion, fewer blocker reasons, smaller complexity deficit, smaller Archite deficit, smaller metabolism deficit and then fewer packs. Remaining ties retain deterministic search order. An incomplete search does not expose this intermediate best candidate as a final answer.

## Template generation and biostats

Template generation requires product-level `Ready`. It uses product inventory rather than selected-assembler inputs and does not require a usable assembler.

The template search operates on unique full compositions, retains physical copy counts, builds a valid fallback before extended enumeration, removes semantic/permutation duplicates and uses its own deterministic node and retained-candidate limits. These limits are separate from the assembler's 10,000-state budget.

Coverage ordering prefers fewer additional distinct genes, then fewer GeneSets, fewer total gene occurrences and a stable key. Exact ordering uses the latter three keys. A complete search returns all valid retained alternatives with a globally best automatic choice. An incomplete result warns that alternatives may be missing and that automatic selection is only the best found.

The generation feedback window renders before synchronous search starts. The creation dialog collects name and icon, shows grouped GeneSets, extra genes, physical-copy counts and biostats, and does not permit synthetic GeneSet editing. Before saving, semantic compositions are resolved again to current physical representatives. A vanished composition fails safely.

`CustomXenogermUtility.SaveXenogermTemplate` owns save-local template creation, same-name replacement and success feedback. The workflow neither starts assembly nor manages the global `.xtp` library.

`PlanGeneBiostatCalculator` provides raw composition sums and distinct effective plan totals. The Plan Editor and template preview share that boundary. Hunger rate uses the native metabolism curve. An unchanged degraded plan's totals cover only resolved genes and are marked partial.

Vanilla templates can lose unresolved genes during their load/resave lifecycle. Source-based creation uses only current runtime data; it does not reconstruct missing composition from labels.

## Readiness and trader notifications

Readiness notifications evaluate saved plans against the current product snapshot, independently from assembler readiness. A first determinate result establishes a silent cursor; `Unavailable` does not advance it. Only a determinate non-ready → ready transition delivers a message. Returning to non-ready re-arms delivery. Disabled notifications still advance the cursor, preventing retroactive delivery when enabled.

The per-plan setting and delivery cursor persist; current readiness does not. Evaluation is invalidated by plan mutation or a new inventory snapshot. One plan's failure must not prevent other evaluations, persistence or core UI access.

Trader advisory owns separate transient state. It discovers active `TradeShip` objects and visiting caravans through current-map trade Lords and `TraderCaravanUtility.FindTrader`, then reads `ITrader.Goods`. Discovery/stock polling runs every 60 game ticks. Plan mutation or inventory identity changes can reevaluate known offers without another stock scan.

Initialization, load and map changes establish a silent first determinate baseline. The deduplication key is concrete trader-source lifetime + exact genepack reference + stable plan ID. Baseline or successfully delivered pairs remain acknowledged through temporary relevance loss. A new plan, pack or source can create a new pair; disappearing sources release their state.

New pairs are aggregated into at most one message per trader per reconciliation. `CanTradeNow` gates delivery, not discovery: a new relevant pair remains pending while unavailable and can deliver later if still current. Orbital messages are text-only; visiting-caravan messages use the current trader pawn for navigation. Stock, relevance and delivery ledgers are not persisted.

Trade relevance is shared with API version 1: only `NotReady` plans match. Coverage offers must intersect missing target genes; Exact offers must contain no extras and intersect `Missing` or `ExactPayloadConflict`. Prerequisite-only offers do not match. Offers never enter physical product inventory.

## Donors, presentation and cache lifetime

Potential donors are spawned pawns on the active map with a gene tracker. The scope does not filter faction or colony status. A donor counts when the desired gene can participate in a valid extraction sequence; current extractor power, occupancy, reachability and hauling do not define that count. Donor results are advisory, transient and not a probability guarantee.

The Planner window owns selection, search, tabs, sorting and scroll state. Product readiness remains visible independently from the active gene-availability table, which can show product or selected-assembler physical packs. Each icon represents one exact physical pack. Composition diagnostics do not replace that identity.

Native inspection opens an info card for the exact gene or pack. Navigation adjusts held targets to a spawned holder on the active map. Null, destroyed, detached or stale references become safe no-ops. Right-click inspection is processed separately from left-click selection or navigation.

The window caches product readiness by plan and inventory identity and target diagnostics by target identity. Assembler live state is scanned at a bounded 30-game-tick cadence. Expensive analysis is repeated when plan/mode, assembler/map or structural inputs change: pack and facility identities, facility power, copied gene sequences and live scalar requirements. An unchanged poll reuses the analysis. Explicit Refresh invalidates it immediately.

Presentation projections depend on their semantic inputs, language, sorting and width. Fixed-height lists calculate visible ranges arithmetically; variable-height lists cache row geometry. Selected-gene removal is applied after row traversal and after closing the scroll view, keeping indices and IMGUI state balanced.

Shared UI owns generic controls, geometry, style primitives and state restoration. Planner owns genetics, labels, tooltips, semantic colors and lifecycle. Code-rendered monochrome icons use white textures and explicit tint; multicolor and native assets retain their respective presentation paths. Native windows own outer chrome, and modal Planner dialogs pause the game and absorb surrounding input.

Languages are listed in [README](../README.md#languages). Keyed and main-button DefInjected resources must stay consistent with those languages.

## Public API and project boundaries

The stable facade is `XenogermPlanner.Api.XenogermPlannerApi, XenogermPlanner`, with `ApiVersion == 1`. Public requests contain gene definition names; results contain statuses and ordered plan IDs/names. No runtime Thing, GeneDef, inventory, readiness or UI types cross that contract.

A query does not mutate physical inventory, plan state, save data or notification cursors. Obtaining the current inventory snapshot can refresh its derived cache. Consumers use optional soft binding, accept only known API versions, preserve their core behavior on absence/failure and decide when to query again. There are no change events or cross-mod mutation calls.

RimWorld owns genetics runtime, actual trading, extraction, assembly, Def initialization and saves. Planner owns target semantics, derived analysis and safe boundary handling. It does not automate those gameplay operations, reserve packs, include other maps in product inventory or add relevance indicators to vanilla genetics UI.