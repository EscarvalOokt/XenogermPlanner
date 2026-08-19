# Xenogerm Planner architecture

This document defines the accepted product architecture and implementation boundaries of Xenogerm Planner.

The current implementation defines the validated Xenogerm Planner `1.1.1` release candidate. It incorporates the validated performance-stabilization set without changing the accepted product, persistence, readiness, assembler, trader-advisory, UI, or public integration API boundaries. Representative runtime profiling and release-level regression validation did not identify a practically meaningful regression or an additional hotspot requiring architecture changes. Planning, readiness, assembler, donor, product-readiness notifications, template generation, localization, optional integration and the accepted UI architecture remain implemented. New-plan creation is unified behind `Create`: from scratch, from a runtime xenogerm template, or from a premade or saved xenotype, with every branch passing through the existing Plan Editor before a new independent `XenogermPlan` is saved. The Planner uses the external compile-time shared UI source for canonical generic controls, layouts, IMGUI state handling, variable-height layout caching and consumer-neutral icon tint application, with assembly ownership covered by integration tests. Display names follow the collection-level unique-name policy, and Exact payload readiness exposes target-gene `ExactPayloadConflict` diagnostics without changing the high-level readiness state machine. The separately versioned read-only integration API version `1`, its Planner-owned relevance query, public contract coverage and external integration guide are implemented and consumed optionally by Settlement Trade Overview. Built-in trader advisory analyzes current active-map orbital and visiting-caravan genepack offers through the same Planner-owned relevance semantics, maintains a transient source-lifetime notification ledger and delivers aggregated advisory `PositiveEvent` messages without changing product inventory or readiness. Window-owned transient analysis caching prevents repeated readiness and target analysis on unchanged IMGUI events, donor and assembler analysis is resolved lazily for the active tab with bounded live-state refresh, and presentation caches use stable identity keys. English, Russian and Ukrainian resources, the release baseline build and regression suite, the stabilized automated performance matrix, representative runtime profiling and final release-candidate validation have been validated successfully.

It is an architecture specification, not an implementation guide or a vanilla implementation reference.

## Sources of truth

The project uses the following source hierarchy:

1. The installed RimWorld 1.6 implementation and the corresponding locally decompiled assemblies define vanilla runtime behavior.
2. The current production implementation defines the behavior that is actually available in the built mod.
3. This document defines Xenogerm Planner product and architecture decisions built on top of verified vanilla behavior.
4. `docs/integration-api.md` defines the exact public integration API version `1` surface and consumer contract.
5. `docs/compatibility.md` defines the public support scope and known limitations.
6. `docs/testing.md` defines automated testing and runtime acceptance boundaries.

Vanilla behavior must not be inferred from this document when it contradicts verified game implementation.

Architecture decisions must not be inferred from vanilla behavior when this document intentionally defines a different product-level model.

Release support claims must remain consistent with `docs/compatibility.md`.

## Product model

The central planning entity is a mod-owned `XenogermPlan`.

The implemented creation and reuse flow is:

```text
NEW PLAN CREATION
──────────────────────────────────────────────────────────────
full runtime GeneDef catalog ─────────────┐
runtime-visible CustomXenogerm ───────────┤
runtime XenotypeDef ──────────────────────┤─→ unified Create source flow
saved custom xenotype .xtp ───────────────┘            ↓
                                                  Plan Editor
                                                       ↓
                                             independent XenogermPlan

REUSE
──────────────────────────────────────────────────────────────
existing XenogermPlan ───────────────────────────────→ duplicate
versioned clipboard transfer payload ────────────────→ paste
                                                       ↓
                                             independent XenogermPlan
                                                       ↓
                                    desired distinct physical payload
                                                       ↓
                                               plan readiness
                                                       ↓
                                     optional assembler readiness
```

The implemented template-output flow adds:

```text
XenogermPlan
+ current product inventory
+ deterministic semantic Genepack composition candidate
        ↓
verified physical Genepack representatives
        ↓
vanilla CustomXenogerm template
```

A `XenogermPlan` represents a desired distinct physical xenogerm gene payload.

The target records which distinct genes should be physically present in the resulting xenogerm. Mutually exclusive genes remain valid target members. Vanilla effective, active and overridden gene states are derived context-dependent results and are not normalized into plan-owned data.

It is independent from the physical genepacks currently present on the map and independent from the lifecycle of any vanilla creation source after source-based creation or template-generation operations have completed.

Vanilla `CustomXenogerm` remains relevant as:

* a verified part of the vanilla xenogerm production flow;
* a supported transient source for `From xenogerm template` creation;
* an implemented template output created from verified concrete physical genepack composition;
* an integration reference for Gene Assembler behavior.

It is not the persistent identity or target model of `XenogermPlan`.

## Architectural boundaries

Xenogerm Planner separates the following concerns:

```text
PLAN TARGET
────────────────────────
XenogermPlan
→ desired distinct physical payload
→ readiness mode
→ plan-owned settings


PRODUCT INVENTORY
────────────────────────
product-defined current-map physical Genepack scope
→ physical Genepacks
→ genes available through their GeneSets


PLAN READINESS
────────────────────────
desired physical payload
+ product inventory
→ coverage or exact payload result
→ per-gene availability semantics


READINESS NOTIFICATIONS
────────────────────────
all saved XenogermPlans
+ current product inventory snapshot
→ product-level readiness transitions
→ per-plan notification delivery cursor
→ optional PositiveEvent message


ASSEMBLER READINESS
────────────────────────
selected Building_GeneAssembler
+ assembler-visible physical inputs
+ live vanilla infrastructure state
→ can assemble through that infrastructure


ADVISORY GENE SOURCES
────────────────────────
potential donor pawns
→ planned acquisition hints

optional temporary trade offers
→ possible future acquisition hints


SHARED UI INFRASTRUCTURE
────────────────────────
external Escarval.RimWorld.UI source
→ generic IMGUI primitives and layouts
→ compiled into XenogermPlanner.dll


EXTERNAL INTEGRATION API
────────────────────────
runtime-free genepack composition input
+ Planner-owned readiness semantics
→ read-only matching plan identities


VANILLA INTEGRATIONS
────────────────────────
map indication and camera navigation
native gene and exact-genepack inspection
CustomXenogerm template generation
```

These concerns must remain separate.

In particular:

* plan readiness does not imply that a specific Gene Assembler can assemble the plan now;
* assembler connectivity does not define membership in the broader product inventory;
* a vanilla `CustomXenogerm` template's grouped `GeneSet` composition does not define `XenogermPlan` readiness;
* advisory potential gene sources do not count as current genepack inventory and do not satisfy readiness;
* readiness notifications consume product-level readiness and do not use assembler-specific readiness as their transition target;
* the persisted notification delivery cursor suppresses duplicate delivery but is not an authoritative persisted readiness result;
* map navigation resolves UI data to current runtime objects but does not make those objects persisted plan identity;
* presentation layers consume project-owned analysis results and must not define or recompute readiness semantics;
* generic shared UI code must not own genetics, plan, readiness or consumer lifecycle semantics;
* external API queries are read-only views over Planner-owned analysis and must not mutate plans, product inventory, notifications or readiness cursors;
* external consumers must not treat trade offers or other advisory data as physical product inventory merely because the API reports relevance.

## `XenogermPlan` data boundary

A `XenogermPlan` is a save-local mod-owned planning entity.

Its plan-owned data includes:

* a stable mod-owned ID;
* an independently editable display name;
* a desired distinct `GeneDef` target;
* a readiness mode;
* a per-plan readiness-notification enabled setting;
* a notification delivery cursor recording whether a determinate baseline exists and whether its last determinate state was ready.

The stable ID is the plan identity.

Display names are presentation and user-editable naming data. The implemented collection policy is:

```text
stored name = input.Trim()

uniqueness key =
    trimmed name
    compared through StringComparer.OrdinalIgnoreCase
```

Under the accepted policy:

* the trimmed name must contain at least one character;
* renaming a plan does not change its stable ID;
* plan collection operations that require identity use the stable ID rather than the display name;
* manual create and rename reject a conflicting name through localized validation rather than silently changing user input;
* duplication, source-based creation and clipboard paste allocate a deterministic numeric suffix when their preferred name is occupied;
* duplication keeps the localized `copy` form as its preferred candidate before numeric allocation;
* loading preserves the first occurrence of a name and deterministically renames later duplicates;
* an existing numeric suffix is continued as one sequence instead of producing names such as `Plan 2 2`;
* migration support for development-only empty names is not part of the compatibility boundary.

The stable ID remains the only programmatic identity even after display names become unique.

The desired target uses set-level physical-payload semantics.

Duplicate occurrences of the same `GeneDef` have no additional meaning. Distinct conflicting genes remain separate target requirements and are not silently removed, replaced or normalized through `NonOverriddenGenes`.

For example:

```text
A
A
B
C
```

normalizes conceptually to:

```text
{ A, B, C }
```

The plan does not model or promise a canonical effective gene set for a recipient pawn. Effective state can depend on vanilla ordering, `RandomChosen` behavior, gene layers, insertion order and runtime state.

The plan does not preserve:

* vanilla `CustomXenogerm.genesets` grouping;
* duplicate template `GeneSet` requirements;
* physical source `Genepack` references;
* runtime `CustomXenogerm` object references.

Plan identity is not derived from desired gene composition, a source template or a display name.

## Plan sources

The implemented baseline separates new-plan creation from plan reuse.

New plans are created through one `Create` entry flow:

```text
Create
├─ From scratch
├─ From xenogerm template
└─ From xenotype
        ↓
existing Plan Editor
        ↓
independent XenogermPlan
```

Duplication and versioned clipboard paste remain separate reuse operations. Every path produces an independent mod-owned plan identity and applies the same desired-target rule: flatten where required, remove exact duplicate `GeneDef` occurrences and preserve all distinct conflicting genes as physical payload requirements.

The source selector is a transient UI boundary. It owns source discovery, source validation and a source gene preview; it does not own plan readiness mode, readiness-notification settings or target diagnostics. Those settings remain part of the existing Plan Editor.

### From scratch

`From scratch` opens the existing Plan Editor with an empty initial gene selection.

The complete runtime `GeneDef` catalog remains available for manual selection. Plan creation is independent from current physical genepack inventory, and a desired gene does not need to be present in any currently owned genepack.

### From xenogerm template

Runtime-visible vanilla `CustomXenogerm` entries are supported transient creation sources.

The source transformation is:

```text
CustomXenogerm
        ↓
genesets
        ↓
flatten runtime-visible GeneDef values
        ↓
distinct
        ↓
editable Plan Editor initial state
```

The transformation intentionally discards vanilla template grouping and multiplicity.

Example:

```text
CustomXenogerm.genesets:
    [A]
    [A, B]
    [C]

Plan Editor initial target:
    { A, B, C }
```

The source `CustomXenogerm` is used only while preparing the editor state. The Planner revalidates the selected runtime source before continuing. The source object is not retained by the saved plan, deleting or replacing the source does not mutate an already saved plan, and editing the plan does not modify the vanilla template.

The Planner uses only runtime-visible gene data that is actually available. It does not infer missing genes from stale labels, template names or other presentation metadata.

### From xenotype

`From xenotype` supports two confirmed source groups:

```text
Premade xenotypes
→ DefDatabase<XenotypeDef>.AllDefs
→ XenotypeDef.genes

Saved xenotypes
→ GenFilePaths.AllCustomXenotypeFiles
→ vanilla .xtp loading flow
→ CustomXenotype.genes
```

Premade vanilla and modded `XenotypeDef` entries remain in one `Premade xenotypes` group. The current implementation does not introduce a separate vanilla-versus-modded classification contract.

Saved `.xtp` sources are discovered through the vanilla global xenotype library and loaded lazily through the verified vanilla version/load boundary. Successfully resolved source data is treated as transient editor input and may be revalidated when the user continues.

Both xenotype source kinds provide only their current resolved `GeneDef` collection as editable Plan Editor initial state. Pawn phenotype reconstruction, effective/active gene state and `Current.Game.customXenotypeDatabase` are not source catalogs for this feature.

### Shared source-selection UI

`From xenogerm template` and `From xenotype` use one semantic source-selection UI. It presents source entries and a gene preview while delegating source discovery and loading to source-specific providers.

For xenotypes, `Premade xenotypes` and `Saved xenotypes` are independent collapsible categories. Category state is transient presentation state and does not affect the selected source or the resulting plan target.

The source selector does not expose readiness mode or readiness-notification controls. Continuing from a source opens the existing Plan Editor with source-neutral initial state; the normal new-plan defaults are Coverage mode with readiness notifications enabled, and the user can change plan settings and genes before saving.

### Duplication of an existing plan

Duplicating a plan creates a new independent `XenogermPlan`.

The duplicate:

* receives a new stable mod-owned ID;
* preserves the resolved desired genes;
* preserves unresolved desired-gene def names;
* preserves the readiness mode;
* preserves the readiness-notification enabled setting;
* starts with an uninitialized notification delivery cursor;
* does not share mutable identity or lifecycle with the source plan.

The current Planner UI creates and selects the duplicate immediately and assigns a localized display name based on the source name with a `copy` suffix. That candidate passes through the collection-level unique-name allocator, producing a numeric suffix when required. This naming behavior does not change the identity boundary.

### Paste from a clipboard transfer payload

Clipboard transfer is a portability boundary, not a persistence identity transfer.

The implemented transfer data consists of:

* a format version;
* the plan display name;
* the readiness mode;
* desired gene requirements represented by `GeneDef.defName`.

The source plan stable ID, readiness-notification setting and notification delivery cursor are not transferred.

Pasting a valid payload creates a new independent plan with a new ID, the default enabled notification setting and an uninitialized notification delivery cursor. A conflicting transferred display name receives a deterministic numeric suffix through the implemented collection policy. Def names unavailable in the destination game configuration remain unresolved requirements and produce the same degraded plan behavior as persisted plan data.

The clipboard representation must remain separate from the save-record schema. Its concrete encoding and version syntax are implementation details.

## Persistence boundary

All mod-owned plan persistence belongs to one save-local RimWorld `GameComponent`.

The component is the persistence owner for the `XenogermPlan` collection and plan-owned settings.

The architecture separates persisted state from derived runtime state.

### Persisted state

Persisted data consists of mod-owned planning state:

* the `XenogermPlan` collection;
* stable plan IDs and display names;
* desired gene requirements represented by `GeneDef.defName`;
* readiness mode;
* the per-plan readiness-notification enabled setting;
* the notification delivery cursor used to suppress duplicate delivery across save/load.

The concrete C# save-record types and Scribe keys remain implementation details.

Earlier development-only mod-owned save schemas are not supported migration targets. The current persisted format and implemented unique-name validation define the release compatibility baseline. Duplicate names found during restore are normalized deterministically without discarding otherwise valid plans. Development-only empty-name records are not a supported migration target.

### Derived runtime state

The following state is derived and must not be treated as authoritative persisted plan data:

* runtime caches and projections;
* genepack inventory snapshots;
* candidate physical genepack combinations;
* readiness analysis results;
* assembler-visible inventory projections;
* live assembler readiness results;
* composition-to-physical-instance navigation projections;
* potential gene source analysis results;

Derived state is rebuilt after loading and refreshed after relevant invalidation.

The persisted notification delivery cursor is delivery state rather than a cached `PlanReadinessResult`. It records only whether a determinate notification baseline exists and whether the last determinate state was ready. Current readiness is always recomputed from the current plan and product inventory before transition evaluation.

The architecture must not create a second persisted copy of derived readiness or inventory state as an alternative source of truth.

### Degraded persisted gene data

Persisted desired-gene requirements are retained by def name.

When a persisted def name cannot be resolved against the active `GeneDef` configuration:

* the unresolved def name remains a plan requirement;
* the plan is marked degraded;
* the unresolved requirement is not silently removed or treated as satisfied;
* other valid plans and records continue to load independently.

If the missing `GeneDef` becomes resolvable again on a later load, the preserved def name can resolve back into the desired gene target.

Structurally invalid plan records are isolated from the rest of the plan collection rather than failing the complete collection.

The following architecture rule remains mandatory:

> unresolved or structurally unreliable desired-gene data cannot be silently treated as satisfied plan requirements.

## Product inventory boundary

The product inventory is map-local derived runtime state.

### Implemented current-map policy

The current implementation discovers physical non-empty `Genepack` through the verified generic map-holder traversal:

```text
current active map
        ↓
spawned matching Genepacks
+ recursively published holder contents
        ↓
explicit traversal and inclusion policy
        ↓
physical non-empty Genepacks
```

The implemented policy:

* excludes `PassingShip` branches;
* requires the genepack to resolve to the current map;
* requires the genepack or one of its physical parents to be spawned;
* excludes a genepack when it or any published holder in its parent chain has an explicit non-player faction;
* allows player-faction and factionless physical roots;
* excludes empty or structurally unavailable gene collections;
* deduplicates physical genepacks by reference identity.

World-rooted contents such as caravans and `TravellingTransporters`, contents on another map and detached objects do not enter the current-map snapshot.

The current product policy does not require storage in a Gene Bank or connection to a Gene Assembler. Loose, held, nested and pawn-carried genepacks can participate when they satisfy the current-map physical and faction rules.

Container power does not determine product inventory membership.

Connection to a Gene Assembler does not determine product inventory membership.

Therefore:

```text
product inventory
!=
selected assembler visible inventory
```

This distinction is intentional.

The product inventory answers:

> Which currently recognized physical genepacks belong to Xenogerm Planner's product-level current-map analysis scope?

The assembler-specific scope answers:

> Which physical genepacks are visible to one selected vanilla Gene Assembler?

### Inventory lifecycle

The product inventory is owned by a separate game component rather than persisted plan data.

Its lifecycle:

* returns an unavailable snapshot when there is no active map;
* rebuilds when the active map identity changes;
* supports explicit invalidation;
* is invalidated whenever the Planner opens so the first snapshot request performs a current scan;
* can be invalidated manually through the Planner `Refresh` action;
* uses a 600-tick periodic fallback refresh so correctness does not depend on Harmony invalidation hooks;
* does not treat the snapshot as an authoritative persisted source of truth.

Physical genepacks in the snapshot use reference identity. Equivalent gene compositions do not merge physical objects at the inventory boundary.

## Plan readiness

Plan readiness is evaluated against the desired distinct physical gene payload and physical genepacks in the product inventory.

Let:

```text
T = distinct desired GeneDef set of the XenogermPlan
```

For a candidate combination of physical inventory genepacks, let:

```text
P = distinct union of GeneDef values in the candidate Genepack GeneSets
```

The selected readiness mode defines the required relation between `T` and `P`.

### Coverage mode

Coverage mode requires:

```text
T ⊆ P
```

Every desired gene must be present in a valid physical genepack combination.

Additional genes are allowed.

Example:

```text
Target:
    { A, B, C }

Inventory candidate:
    [A]
    [B, D]
    [C]

Candidate union:
    { A, B, C, D }

Result:
    Ready
```

The extra gene `D` does not block coverage readiness.

Coverage mode answers:

> Is there a physical genepack combination that provides every desired gene?

### Exact payload mode

Exact payload mode requires:

```text
T = P
```

Every desired gene must be present and a valid physical genepack combination must not add genes outside the target.

Using the same example:

```text
Target:
    { A, B, C }

Inventory candidate:
    [A]
    [B, D]
    [C]

Candidate union:
    { A, B, C, D }

Result:
    Not ready
```

The extra gene `D` prevents exact payload readiness.

Exact payload mode answers:

> Is there a physical genepack combination whose resulting unique gene payload exactly matches the desired genes?

### Set-level semantics

Both readiness modes use set-level gene semantics.

Duplicate genes in the plan do not change the target.

Duplicate occurrences of the same gene across different physical packs do not change the candidate union.

For example:

```text
Target:
    { A, B }

Candidate packs:
    [A]
    [A, B]

Candidate union:
    { A, B }
```

The repeated `A` does not create another requirement or another output gene.

This matches the relevant final-output behavior of vanilla `Xenogerm.Initialize`, where genes are added through `GeneSet.AddGene` and duplicate `GeneDef` entries are not preserved in the resulting `Xenogerm.GeneSet`.

Distinct conflicting genes remain part of both `T` and `P`. Product readiness does not replace them with a derived non-overridden set and does not predict final active genes on a particular pawn.

### Conflicts and prerequisites

Conflict and prerequisite information is implemented as derived analysis rather than plan normalization.

The implemented boundaries are:

* ordinary, `RandomChosen` and mixed conflicts may produce different effective runtime behavior but do not remove physical target requirements;
* project-owned target analysis produces deterministic ordinary, random-choice-group, mixed-conflict and missing-prerequisite diagnostics without changing readiness membership;
* ordinary diagnostics may expose a predicted winner only when the project can resolve one safely, while random and mixed results remain explicitly non-canonical;
* a missing prerequisite does not remove or replace the dependent target gene;
* plan-level prerequisite diagnostics explain the requirement according to the selected readiness mode;
* prerequisite satisfaction remains an assemblability constraint evaluated against each concrete physical candidate.

Coverage can satisfy a dependent gene through a physical candidate that also contains an additional prerequisite gene. Exact payload requires every physically necessary prerequisite to be part of the target because additional genes are not allowed.

### Readiness states

The current readiness result uses the following states:

```text
Ready
NotReady
EmptyTarget
Degraded
Unavailable
```

The accepted state precedence is:

```text
product inventory unavailable
→ Unavailable / NoActiveMap

plan contains unresolved desired-gene requirements
→ Degraded

resolved desired target is empty
→ EmptyTarget

otherwise
→ Ready or NotReady
```

An empty target is an incomplete planning state and is not treated as ready through a zero-genepack combination.

A degraded plan never produces `Ready` while unresolved desired-gene requirements remain.

When inventory is available, degraded results may still contain partial covered/missing diagnostics for the resolved desired genes. Unresolved def names remain separate persisted requirements and are not counted as satisfied.

### Readiness diagnostics

For available non-empty targets, readiness analysis derives:

* covered desired genes;
* missing desired genes;
* per-gene source genepack compositions;
* equivalent physical pack counts for the same composition;
* exact-payload eligibility of a composition;
* genes outside the target that make a composition exact-incompatible.

Equivalent physical genepacks are aggregated only for composition diagnostics. The product inventory continues to retain the individual physical pack objects.

Per-gene source diagnostics describe which available compositions contain a desired gene. They explain availability and exact-payload conflicts; they are not a physical candidate selection contract.

For exact payload mode, a not-ready result with every desired gene available can report an exact-payload conflict. This means the currently stored genepacks expose all desired genes, but no allowed combination can produce the exact target without adding genes outside it.

The implemented diagnostics refine this distinction at the target-gene level without adding another top-level plan state. Coverage retains `Available` and `Missing`. Exact payload uses:

```text
Available
ExactPayloadConflict
Missing
```

A desired gene is `Available` in Exact payload mode when at least one source composition contains it without genes outside the plan target. It is `ExactPayloadConflict` when source compositions exist but every one adds at least one gene outside the target. It is `Missing` only when no source composition contains the gene.

For external acquisition relevance, Exact payload needs are therefore:

```text
Missing
+ ExactPayloadConflict
```

`ExactPayloadConflict` must remain separate from the physical `MissingGenes` contract because the gene is present in product inventory. If both compatible and incompatible sources exist, the gene is `Available`. The top-level state set and precedence remain unchanged.

### Vanilla template grouping is not plan readiness

Vanilla `CustomXenogerm` template loading uses grouped `GeneSet` matching and preserves requirement multiplicity while selecting initial physical genepacks.

`XenogermPlan` readiness intentionally does not use those semantics.

For example:

```text
CustomXenogerm source:
    [A, B]
    [C]
```

loads into the Plan Editor as:

```text
XenogermPlan target:
    { A, B, C }
```

After source-based creation, the original pack grouping is no longer a plan requirement.

This is an intentional product distinction between:

* vanilla template loading fidelity;
* Xenogerm Planner desired final gene payload.

## Readiness result and physical combinations

Readiness analysis determines whether at least one physical genepack combination satisfies the selected readiness mode.

The product-level result contract intentionally does not retain a canonical physical genepack combination and does not expose a list of valid combinations.

The readiness result contains derived gene and composition diagnostics, but it does not contain physical `Genepack` references.

In particular:

* `Ready` means that at least one valid physical combination exists under the selected readiness mode;
* the result does not identify which physical pack combination proved readiness;
* equivalent composition diagnostics and physical pack counts are explanatory data, not a selected candidate set;
* product readiness remains independent from one selected Gene Assembler.

This policy keeps product-level readiness focused on the desired target and broader product inventory.

Concrete physical inputs required for live assembler blocker evaluation are obtained through a separate assembler-specific candidate search over assembler-visible physical packs.

Physical candidate references remain derived assembler-layer data. They are not added to `XenogermPlan` or `PlanReadinessResult` and are not treated as persisted or canonical product-level selection.

## Readiness notification boundary

Readiness notifications are a product-level delivery layer over the existing plan-readiness boundary.

The implemented flow is:

```text
all saved XenogermPlans
+ current product inventory snapshot
        ↓
PlanReadinessAnalyzer
        ↓
project-owned transition tracking
        ↓
per-plan notification delivery cursor
        ↓
optional RimWorld PositiveEvent message
```

The notification target is product-level readiness for every saved plan. A transient selected Gene Assembler and `PlanAssemblerReadinessResult` do not participate in this transition boundary.

The background notification game component reuses the existing product inventory snapshot and `PlanReadinessAnalyzer`. It evaluates all saved plans when the inventory snapshot reference changes or when plan mutations explicitly invalidate notification evaluation. It does not introduce a parallel inventory scan or a second readiness algorithm.

The determinate readiness family consists of every product-level state except `Unavailable`:

```text
Ready
NotReady
EmptyTarget
Degraded
```

The accepted transition semantics are:

* the first determinate evaluation initializes the notification delivery cursor without sending a message;
* `Unavailable` does not initialize or change the cursor;
* a transition from any determinate non-ready state to `Ready` is the only message-producing transition;
* repeated `Ready` evaluations and repeated non-ready evaluations do not send messages;
* a transition from `Ready` to a determinate non-ready state re-arms a later ready notification;
* disabling notifications suppresses delivery but does not stop cursor updates;
* re-enabling notifications while the plan is already ready does not create a retroactive message.

The enabled setting and notification delivery cursor are plan-owned persisted state. The cursor exists only to suppress duplicate delivery across repeated evaluation and save/load; it is not a persisted readiness result and cannot be used as a source of current readiness.

New plans created through the Plan Editor use the enabled setting by default and start with an uninitialized cursor. Duplicating a plan copies the setting but not the cursor. Clipboard transfer excludes both the setting and cursor, so a pasted plan receives the destination defaults and a new baseline lifecycle.

The Planner exposes the setting in the Plan Editor for new and existing plans through standard RimWorld checkbox controls. Source-selection dialogs do not own this setting. UI code edits the setting and invalidates notification evaluation but does not define transition semantics.

Message delivery uses the current plan display name through the shared Planner presentation boundary and sends a standard non-historical `PositiveEvent`. Failure while evaluating or delivering one plan is isolated and logged without preventing evaluation of other plans, save loading, plan persistence or core Planner access.

## Assembler readiness boundary

Assembler readiness is a separate derived layer evaluated for one selected `Building_GeneAssembler`.

The implemented boundary is:

```text
XenogermPlan
        +
selected Building_GeneAssembler
        ↓
assembler-visible physical scope
        +
live vanilla infrastructure state
        ↓
assembler-specific physical candidate search
        ↓
candidate-specific blocker evaluation
        ↓
PlanAssemblerReadinessResult
```

Product-level plan readiness remains available as a separate result:

```text
desired target
+ map-level product inventory
→ PlanReadinessResult
```

Assembler readiness does not redefine the desired plan target, product inventory scope or product-readiness result.

### Selected assembler lifecycle

The selected Gene Assembler is transient Planner UI state.

The selection:

* is a runtime reference to a current-map `Building_GeneAssembler`;
* is shared while the current Planner session switches between plans;
* is not stored in `XenogermPlan`;
* is not persisted in save data;
* is cleared when the active map identity changes;
* is cleared when the exact selected assembler is no longer in the current selectable assembler scope;
* is not automatically replaced by another assembler;
* can be explicitly cleared by the user.

Missing or stale assembler selection is therefore handled by the transient selection lifecycle rather than degraded persisted-plan handling.

### Assembler-visible physical scope

Assembler-visible inventory is derived independently for the selected assembler.

The scope is read through:

```text
Building_GeneAssembler.ConnectedFacilities
        ↓
facility with CompGenepackContainer
        ↓
ContainedGenepacks
        ↓
exact physical Genepack source
```

Each derived source retains:

* the exact physical `Genepack` reference;
* the containing connected facility;
* the current facility power state.

Physical genepacks are deduplicated by reference identity.

Equivalent gene compositions do not merge physical objects at the scope boundary.

Power does not remove a connected pack from the visible scope. Facility power is retained as live source metadata for candidate-specific blocker evaluation.

Vanilla facility scopes may overlap between assemblers.

A physical genepack can therefore belong to the product inventory and be visible to:

* no assembler;
* one assembler;
* multiple assemblers.

Each selected assembler is evaluated against its own current scope.

### Assembler physical candidate search

Concrete physical candidates are obtained through a dedicated assembler-specific search.

The product-level `PlanGenepackCombinationSearcher` and `PlanReadinessResult` remain unchanged and do not expose canonical physical inputs.

The assembler candidate search uses the desired distinct target and current assembler scope.

For Coverage mode:

* a source may contain genes outside the desired target;
* search first covers missing desired genes and then attempts to satisfy prerequisites discovered from the selected physical composition;
* a prerequisite-only composition group may therefore be selected after the desired target is already covered when it reduces the candidate's missing-prerequisite set.

For Exact payload mode:

* a source with any gene outside the desired target is excluded;
* a prerequisite can be supplied only when it is also present in the desired target.

Candidate search uses requirement-contributing, target-and-prerequisite-irredundant combinations:

* before target coverage is complete, a source is added when it satisfies the selected missing target requirement;
* after target coverage is complete, a source may be added when it satisfies the selected missing prerequisite requirement;
* a complete candidate must cover the complete target;
* removing a selected composition group must either break target coverage or leave a strictly worse missing-prerequisite set.

Equivalent full gene compositions are grouped to avoid duplicate search branches.

Within one equivalent composition group, a source from a powered facility is preferred over a source from an unpowered facility. Remaining physical ties use a stable physical key.

Candidate enumeration is deterministic. It selects the next missing target gene with the fewest remaining source composition groups, then applies the same rule to missing prerequisites after target coverage is complete.

The search does not make a candidate canonical at the product layer.

### Live assembler readiness

Live assembler state is derived fresh for each assembler readiness evaluation.

The current live state includes:

* the current assembler-visible physical scope;
* Gene Assembler power;
* live `Building_GeneAssembler.MaxComplexity()`;
* current Archogenetics research completion;
* current non-fogged Archite Capsule count on the assembler map.

For each concrete candidate, blocker evaluation uses the candidate's exact physical sources.

Supported blocker reasons are:

```text
MissingPrerequisite
UsedGeneBankUnpowered
AssemblerUnpowered
InsufficientComplexity
ArchogeneticsResearchMissing
InsufficientArchiteCapsules
```

Each concrete candidate is checked against `GeneDef.prerequisite` using the raw flattened genes provided by that candidate's physical genepacks. Missing required genes produce `MissingPrerequisite` blockers for that candidate without mutating the plan target or replacing the dependent gene.

Only facilities containing candidate physical inputs participate in the used-Gene-Bank power blocker.

Candidate complexity follows the verified active assembler boundary:

```text
flatten candidate physical pack genes
→ xenogene-typed NonOverriddenGenes
→ sum biostatCpx without Distinct
```

Archite requirements use distinct effective genes and sum `biostatArc`.

The analyzer stops when it finds a blocker-free candidate.

When all valid candidates are blocked, the returned blocked result uses the deterministic best evaluated candidate by:

1. prerequisite-complete candidate before an incomplete fallback candidate;
2. fewer blocker reasons;
3. smaller complexity deficit;
4. smaller Archite Capsule deficit;
5. fewer physical candidate packs.

Assembler readiness states are:

```text
Ready
NotReady
Blocked
EmptyTarget
Degraded
```

The accepted state precedence is:

```text
plan contains unresolved desired-gene requirements
→ Degraded

resolved desired target is empty
→ EmptyTarget

assembler-visible gene scope cannot satisfy the target
→ NotReady

valid concrete candidate exists, but supported live blockers remain
→ Blocked

blocker-free concrete candidate exists
→ Ready
```

`Ready` and `Blocked` results retain the selected derived physical candidate references and numeric complexity and archite diagnostics.

These references remain transient assembler-layer data and are rebuilt from current live state.

## `CustomXenogerm` template generation boundary

Generating a vanilla `CustomXenogerm` template is an integration output, not a conversion of plan identity.

A plan contains a flat distinct desired-gene target and does not retain vanilla `GeneSet` grouping. The implemented workflow therefore derives template candidates from current physical `Genepack` compositions and never synthesizes artificial `GeneSet` grouping directly from `XenogermPlan.DesiredGenes`.

The implemented boundary is:

```text
product-level Ready XenogermPlan
+ current product inventory
        ↓
bounded deterministic semantic composition search
        ↓
valid fallback candidate
+ best retained alternatives
+ explicit complete / incomplete result
        ↓
generation feedback
+ name / icon / grouped GeneSet preview
        ↓
re-resolve each semantic composition to a current physical Genepack
        ↓
CustomXenogermUtility.SaveXenogermTemplate
        ↓
independent save-local CustomXenogerm template
```

The workflow supports both readiness modes:

* Coverage candidates must cover every desired gene and may contain additional genes;
* Exact payload candidates must have a distinct union equal to the desired target.

Template generation is available only when current product-level readiness is `Ready`. This requirement establishes that at least one supported physical composition exists; it does not import assembler power, research, complexity, prerequisite, Archite Capsule or connected-facility blockers into template-save eligibility.

Candidate search operates on unique full `GeneSet` compositions rather than physical object permutations. It:

* uses all physical genepacks in the current product inventory;
* groups equivalent compositions and records their physical copy counts;
* builds a deterministic valid fallback candidate before extended enumeration;
* keeps only target-contributing, target-irredundant complete combinations;
* removes semantic and permutation duplicates;
* limits extended enumeration through a deterministic node budget;
* retains only a bounded set of the best candidates under the existing comparator;
* reports whether the returned candidate set is complete.

The automatic candidate order is:

```text
Coverage:
additional distinct genes
→ GeneSet count
→ total gene occurrences
→ stable candidate key

Exact payload:
GeneSet count
→ total gene occurrences
→ stable candidate key
```

When enumeration completes within the node budget and the number of valid candidates does not exceed the retained limit, the automatic candidate is globally best under this order and all valid alternatives are returned. When the node budget is exhausted, every returned candidate remains valid and target-irredundant, but the automatic candidate is only the best candidate found by the bounded deterministic search. When more valid candidates exist than the retained limit, only the best retained subset is exposed. In either incomplete case the Planner warns the player that other valid alternatives may exist.

The dialog stores semantic compositions rather than long-lived physical references. Before saving, each selected composition is resolved again against the current inventory. Equivalent physical packs are ordered by a stable physical key based on `ThingID`, and one representative is selected for the vanilla helper. If the selected composition is no longer available, the save operation fails safely instead of silently changing the template.

The dedicated Planner workflow:

* opens a small modal generation-feedback window before starting synchronous candidate search;
* starts the search only after the feedback window has rendered at least one frame;
* uses the plan display name as the initial template name;
* uses a vanilla `XenotypeIconDef` selector and a vanilla default icon;
* always shows the grouped template preview;
* offers the deterministic automatic candidate and the retained valid alternatives;
* explicitly warns when the returned alternative set is incomplete;
* does not allow arbitrary synthetic gene or `GeneSet` editing;
* displays the concrete additional-gene summary above the scrollable `GeneSet` list, with bounded icon rows and an overflow indicator;
* displays matching physical-pack counts and candidate/per-`GeneSet` biostats;
* uses public vanilla genetic biostat icons and colors.

Candidate-level biostats follow the vanilla dialog projection:

```text
flatten genes from all selected compositions
→ xenogene-typed NonOverriddenGenes
→ Distinct
→ sum complexity / metabolism / Archites
```

Per-`GeneSet` biostats are raw sums for that physical composition and are presented as explanatory values. They are not assumed to add up to the candidate-level result when duplicates or overrides are involved.

`CustomXenogermUtility.SaveXenogermTemplate` creates the independent save-local template, applies vanilla same-name replacement and sends vanilla save feedback. The Planner does not retain the resulting runtime `CustomXenogerm` reference. Creating, replacing or deleting the template does not mutate the source plan.

The feature does not automatically start assembly and does not manage the global `.xtp` custom xenotype library.

## Shared plan biostat calculation boundary

Plan Editor and template generation use one shared project-owned gene-biostat calculation boundary.

`PlanGeneBiostatCalculator` accepts `GeneDef` collections and exposes two projections:

* raw composition totals sum `biostatCpx`, `biostatMet` and `biostatArc` directly;
* effective plan totals apply the vanilla xenogene-typed `NonOverriddenGenes` projection, remove duplicate `GeneDef` values and then sum the same fields.

`PlanXenogermTemplateBiostatCalculator` remains a template-specific adapter. It extracts genes from template compositions and converts the shared result into the template result type instead of owning a parallel implementation.

Plan Editor stores the current effective result as derived UI state and refreshes it together with selected-gene analysis. It displays:

* complexity;
* metabolic efficiency;
* the corresponding hunger-rate factor through `GeneTuning.MetabolismToFoodConsumptionFactorCurve`;
* required Archite Capsules.

An unchanged degraded plan can calculate only the resolved subset of its target. The UI marks that result as partial until the selected target is changed and the unresolved requirements are explicitly removed through the existing degraded-plan workflow.

Biostat values are not persisted in `XenogermPlan`, do not alter readiness semantics and do not create a second source of truth for the desired physical payload.

## Degraded data policy

Xenogerm Planner must fail safely when plan or creation-source data is incomplete or structurally unreliable.

The following rules apply:

* missing `GeneDef` values must not be reconstructed from labels;
* template, gene or plan presentation text is not a composition source;
* unresolved desired-gene data must not be silently counted as satisfied;
* structurally unreliable plan data must not produce a `Ready` result;
* one degraded plan must not prevent other valid plans from loading or being analyzed;
* derived readiness state must be rebuilt from the currently valid plan and inventory data;
* runtime `CustomXenogerm` source-based creation uses only the composition visible in the current runtime object;
* the Planner does not claim to restore genes already removed from vanilla runtime data.

The verified vanilla source-level flow shows that unresolved `GeneDef` references in `GeneSet` can become `null` and be removed during `PostLoadInit`.

The complete vanilla save/load/resave/restore scenario for a temporarily missing modded `GeneDef` remains unverified.

This remains a vanilla-behavior and compatibility uncertainty.

It is not an architecture assumption that vanilla data can recover after the missing definition is restored.

## UI boundary

Core Xenogerm Planner workflows use a separate Planner interface.

The current Planner UI is localized in English, Russian and Ukrainian through the existing Keyed and DefInjected resources. All languages use the same project-owned presentation, shared widget, style and layout boundaries.

The implemented Planner UI is responsible for:

* displaying the plan collection;
* filtering the visible plan collection by display name without changing plan identity, order or persisted state;
* selecting a plan by its stable identity;
* creating a plan through the unified `Create` flow from scratch, a runtime-visible vanilla `CustomXenogerm`, a premade `XenotypeDef` or a saved custom xenotype `.xtp`;
* presenting one shared source-selection UI for xenogerm-template and xenotype creation, with source-specific discovery/loading and editable gene preview;
* editing desired genes and the display name;
* displaying live effective complexity, metabolic efficiency, hunger rate and required Archite Capsules for the current Plan Editor target;
* duplicating a plan and copying or pasting a plan through the clipboard boundary;
* presenting collection actions, selected-plan actions and inventory refresh as shared contextual icon controls with localized tooltips and disabled states;
* selecting the readiness mode;
* configuring the per-plan product-readiness notification setting in the Plan Editor for new and existing plans;
* displaying product-level readiness and player-facing availability diagnostics;
* selecting or clearing one transient current-map Gene Assembler;
* displaying separate assembler readiness, supported blocking reasons and secondary assembler diagnostics;
* presenting details through the dedicated `Overview`, `Gene assembler` and `Gene effects` tabs;
* presenting derived conflict and prerequisite diagnostics for existing plans and Plan Editor targets;
* providing the agreed Plan Editor bulk actions as contextual icon controls in the fixed `Gene catalog` and `Selected genes` headers, with localized tooltips and disabled states;
* opening native info cards for displayed genes and exact loose or held genepacks through a shared RMB context-menu boundary while preserving existing left-click actions;
* presenting aligned product and assembler statuses, native complexity and Archite blocker assets, and adaptive non-archite/archite genepack tooltips;
* refreshing the current-map product inventory on Planner open or through an explicit icon-based `Refresh` action;
* locating selected and selectable Gene Assemblers through native map indication and camera navigation;
* displaying exact physical genepack instances in the `Gene availability` table and locating or inspecting them through the shared interaction boundaries;
* sorting the `Gene availability` tables in `Overview` and `Gene assembler` by gene, availability or the number of exact physical genepacks in the active presentation scope;
* displaying a sortable potential-donor count for resolved product-level missing genes in `Overview`;
* opening a separate potential-donor details window with a gene icon, a live exact-pawn list and full-row map indication and camera navigation;
* starting the `CustomXenogerm` template workflow for product-level `Ready` plans in Coverage and Exact payload modes;
* presenting modal generation feedback before synchronous bounded template search begins;
* presenting deterministic automatic and retained alternative template compositions with complete/incomplete search-state feedback;
* showing the concrete additional-gene summary above the scrollable grouped `GeneSet` preview, together with physical-copy counts and genetic biostats;
* collecting template name and icon through a dedicated modal dialog and saving through the verified vanilla helper;
* invalidating background notification evaluation after plan mutations without recomputing transition semantics in the UI;
* applying shared `RimWorldUiStyle` colors and generic UI metrics together with Planner-owned metrics and semantic presentation through reusable Planner widgets across the main tab and modal dialogs;
* preserving native RimWorld `Window` ownership of outer window chrome while applying Planner styling to project-owned content surfaces;
* force-pausing the game while modal Plan Editor, source-selection, potential-donor details, template-generation and template-creation windows absorb surrounding input.

The Plan Editor selected-gene list treats mutation as a post-render action. A row may request removal while it is drawn, but the selected collection and derived target analysis are updated only after visible-row traversal has ended. The scroll view is closed through a guaranteed `finally` boundary before the mutation is applied, preventing stale visible indices and an unbalanced IMGUI mouse-position stack.

The readiness UI consumes derived analysis results rather than recomputing readiness semantics locally.

Product readiness presentation shows a compact goal/status summary evaluated against the full product inventory.

The `Gene availability` table uses an active presentation scope:

```text
no selected Gene Assembler
→ exact physical genepacks from the product inventory

selected Gene Assembler
→ exact physical genepacks from that assembler's visible scope
```

Changing the table scope does not replace or redefine the product-level readiness result. Assembler readiness remains a separate live result with its own blockers.

Composition-level diagnostics remain explanatory metadata for gene availability and exact-payload compatibility. The table resolves those diagnostics to exact current physical genepacks only at the UI boundary.

Assembler readiness presentation keeps status and actionable blockers at the primary level. Secondary assembler diagnostics are exposed through transient collapsible details. Candidate genepacks are not rendered as a separate assembler-details block because the active-scope availability table already presents exact physical packs.

Product and assembler readiness statuses use shared semantic Planner color mappings. Presentation labels and messages remain centralized in the shared Planner presentation boundary.

The current sorting boundary remains project-owned. RimWorld's native `PawnTable` and `PawnColumnWorker` infrastructure is pawn-specific and is not adapted for `XenogermPlan`, `GeneDef` or gene-coverage rows. The active sort column and direction are transient per-table UI state, while deterministic row projection and comparison remain in the shared Planner presentation boundary. Existing custom scrolling, variable-height rows, wrapped exact-genepack icons and cell interactions remain unchanged.

### UI performance and projection boundary

The implemented Planner UI uses project-owned transient analysis, presentation and geometry caches so unchanged IMGUI events consume stable derived state instead of rebuilding domain analysis.

The accepted boundary is:

```text
plan state + product inventory snapshot + active-map live state
        ↓
window-owned transient analysis cache
        ↓
tab-specific derived analysis resolved only when needed
        ↓
transient presentation projection keyed by stable result identity
        ↓
cached row geometry
        ↓
viewport-visible row range
        ↓
IMGUI rendering and interaction
```

Product-level `PlanReadinessResult` is reused while the plan target, readiness mode and inventory snapshot identity remain unchanged. `PlanGeneTargetAnalysisResult` is reused while the resolved desired-gene collection identity remains unchanged. Potential-donor, selectable-assembler and selected-assembler live analysis is requested only for the active tab and is refreshed immediately when its semantic inputs change or after the bounded 30-tick live-state interval. Opening the Planner and saving a plan explicitly invalidate the window-owned analysis cache.

The gene-coverage projection cache uses the stable plan collections, readiness result, source collection, donor result, sorting state and language identity as its compatibility key. It does not repeat deep diagnostic, gene, source-pack or donor comparisons on every IMGUI event.

Fixed-height lists derive their visible index range arithmetically. Variable-height lists cache measured row heights and cumulative offsets, then use binary search to determine the rows intersecting the current viewport. Hierarchical gene-catalog data is flattened into a cached presentation projection before applying the fixed-height path. Stable filtered, sorted, grouped and formatted presentation projections are reused until their explicit semantic, language, sorting or width inputs change.

These caches and projections:

* remain derived transient UI state owned by the current window;
* are not persisted in `XenogermPlan`, save records, inventory components or readiness results;
* do not define product, readiness, donor, assembler or template semantics;
* must be invalidated when their declared inputs change;
* may throttle only live presentation refresh and must update immediately when a semantic identity key changes;
* must reuse the existing shared Planner layout, style and interaction boundaries rather than introduce parallel rendering systems.

The optional potential-donor column belongs only to the product-level `Overview` table and supports the same shared sortable-header mechanism. Missing genes have a numeric donor count, while covered and unresolved rows retain non-applicable presentation values. The donor-details view uses a simple custom scroll list rather than `PawnTable`; it does not add a second table framework or user-controlled donor-list sorting.

### Physical target indication and navigation

Planner map interaction is implemented through one stateless shared boundary:

```text
hover exact Thing
→ adjust to a current-map target
→ TargetHighlighter.Highlight

click exact Thing
→ adjust to a current-map target
→ CameraJumper.CanJump
→ CameraJumper.TryJump
```

The implemented interaction applies to:

* the selected Gene Assembler;
* Gene Assemblers listed by the selector;
* exact physical genepacks displayed in the active-scope availability table;
* exact potential donor pawns displayed in the donor-details window.

Held things are passed through vanilla target adjustment, which resolves them to an appropriate spawned holder. Camera jump and selection remain separate operations, and shared Planner navigation does not require selection by default.

The shared boundary:

* accepts exact transient runtime references;
* restricts interaction to adjusted targets on `Find.CurrentMap`;
* safely ignores null, destroyed, detached and stale references;
* does not persist navigation targets;
* does not create a parallel arrow renderer;
* does not require Harmony patches.

The donor-details list makes the complete pawn row interactive. Hover state is detected inside the scroll view, while the shared target highlight is queued after leaving the scroll-view GUI context. A left click anywhere in the row uses the same shared navigation boundary.

### Native Planner inspection and genetic biostat assets

Source-level research against `Assembly-CSharp, Version=1.6.9676.17735` confirms the supported native inspection boundaries:

```text
GeneDef
→ new Dialog_InfoCard(geneDef)
→ Find.WindowStack.Add

exact loose or held Genepack
→ Widgets.InfoCardButton(..., genepack) in vanilla Gene Assembler UI
→ new Dialog_InfoCard(genepack)
→ Find.WindowStack.Add
```

The implemented Planner integration uses one shared RMB context-menu boundary for `GeneDef` and exact `Genepack` references. The context action is processed before the existing left-click branch, so native inspection does not trigger gene selection, removal or genepack navigation.

Inspection and navigation intentionally retain different target semantics:

```text
native info card
→ exact GeneDef or exact Genepack

map indication / navigation
→ adjusted current-map holder target
```

For an exact genepack, the shared inspector revalidates the physical reference before opening the menu and again before adding `Dialog_InfoCard`, so a pack destroyed while the menu is open becomes a safe no-op. The integration does not require Harmony, reflection or a project-owned inspector.

The verified native genetic biostat assets are public `CachedTexture` fields:

```text
GeneUtility.GCXTex
→ UI/Icons/Biostats/Complexity

GeneUtility.METTex
→ UI/Icons/Biostats/Metabolism

GeneUtility.ARCTex
→ UI/Icons/Biostats/ArchiteCapsuleRequired
```

Planner biostat and blocker presentation uses these public vanilla textures and their associated colors for complexity, metabolism and required Archite Capsules. Hunger rate is derived through the public vanilla metabolism curve rather than a project-owned conversion table. The mod does not ship local copies of vanilla biostat assets.

Runtime acceptance confirmed the core Planner RMB event ordering, exact held-pack inspection, info-card lifecycle, status and blocker rendering, and continued usability of the underlying Planner surfaces. Extended stale or destroyed target combinations remain compatibility-validation scenarios rather than unfinished feature work.

### Exact physical genepack presentation

Product readiness diagnostics continue to aggregate equivalent gene compositions. That aggregation does not define a canonical physical pack.

The `Gene availability` table presents every matching physical genepack as a separate exact icon within the current active presentation scope.

```text
one physical instance
→ one exact icon
→ hover indicates that instance
→ click navigates to that instance or its spawned holder
→ RMB opens the native info card for that exact pack

multiple equivalent physical instances
→ one icon per physical instance
→ each icon has independent indication, navigation and inspection
```

A separate equivalent-instance popup is not required for the current table because equivalent physical copies are already visible individually.

The tooltip may use the matching aggregate composition diagnostic for gene and exact-payload explanations, but the interactive control represents the exact physical `Genepack` reference.

Physical references remain outside `PlanGenepackCompositionDiagnostic`, `PlanReadinessResult` and persisted `XenogermPlan` state.

### Potential donor presentation

Potential donor information remains visually distinct from stored genepack availability.

Potential donor analysis is performed for resolved product-level missing genes shown in the `Overview` table. It does not run as assembler-specific availability and does not add donors to product inventory or readiness.

The implemented pawn source scope is:

```text
current active Map
→ map.mapPawns.AllPawnsSpawned
→ pawn is spawned on that exact Map
→ pawn has a Pawn_GeneTracker
```

The scope does not filter by faction, colonist/prisoner/slave/visitor/enemy status, humanlike classification or current Gene Extractor infrastructure and logistics. Without an active map, donor data is unavailable.

The main donor count uses the verified Gene Extractor selection-participation rule: a pawn counts as a potential donor for a desired `GeneDef` when that gene can participate in at least one valid vanilla extraction sequence for the pawn's current genes. Current Gene Extractor power, occupancy, reachability, hauling and other infrastructure or logistics conditions do not define this count.

The optional `Potential donors` column uses the shared sortable-header and deterministic presentation sorting. A positive count opens a separate details window. The details window re-runs analysis for the selected gene, shows its icon in the title, presents exact donor pawns in a deterministic custom scroll list and uses shared full-row indication and camera navigation.

Donor results and exact pawn references remain derived and transient. The UI uses the term `potential donor` and does not imply that the desired gene is currently available as a genepack or guaranteed to appear in the next extraction result.

### Vanilla dialog and template integration

The core Planner workflow does not use `Dialog_CreateXenogerm` or `Dialog_XenogermList_Load` as its UI host.

The verified vanilla boundary used by the implemented template workflow is:

```text
Planner-owned semantic candidate
→ current physical Genepack representatives
→ CustomXenogermUtility.SaveXenogermTemplate
```

`Dialog_CreateXenogerm` remains a research and behavior reference for template loading, gene selection, validation and biostat presentation. It is not opened or prefilled by the Planner because the vanilla dialog does not expose a supported public boundary for assigning the private selected-genepack collection, and its selection remains user-mutable.

The dedicated Planner dialog owns only Planner presentation and input collection. Vanilla continues to own creation, same-name replacement, save-local database mutation and success feedback through `CustomXenogermUtility.SaveXenogermTemplate`.

The implemented baseline uses standard RimWorld UI mechanisms and the external `Escarval.RimWorld.UI` source for generic styling, controls, fixed- and variable-height layouts, IMGUI state restoration and variable-height layout caching. The shared `.cs` files are compiled directly into `XenogermPlanner.dll`; no separate runtime UI assembly or user-installed framework mod is introduced. Generic deterministic layout and cache tests belong to `Escarval.RimWorld.UI.Tests`.

Planner-owned code remains responsible for gene and genepack rendering, readiness diagnostics, biostats, donors, templates, native inspection, map target interaction, genetics-specific tooltips and all plan presentation semantics. `XenogermPlannerWidgets` remains a Planner-specific facade where it delegates generic operations to shared primitives and supplies genetics-specific presentation.

The accepted icon-color boundary distinguishes three categories:

* code-rendered monochrome icons use white source textures and explicit consumer-defined runtime tint after the shared tint-aware helper contract is implemented;
* multicolor Planner assets retain source-defined colors and are rendered with a neutral white tint when Planner code owns the draw call;
* icons rendered from RimWorld gameplay data, Defs or metadata remain under their native presentation paths.

Planner-specific semantic tint values remain in Planner-owned style or presentation code. The shared UI project owns generic tint application and disabled behavior, but it does not define genetics-specific colors. The final Planner ModIcon and MainButton designs are integrated, the SVG source library and corresponding packaged textures are normalized, and multicolor assets are not converted to white.

Future UI work should:

* continue to use standard RimWorld UI mechanisms and preserve native ownership of outer window chrome;
* reuse the external generic shared UI source and existing Planner-specific presentation boundaries when equivalent behavior already exists;
* use the accepted monochrome, multicolor and RimWorld-owned icon classifications rather than applying one tint policy to every asset;
* continue to use the existing shared context-menu boundary for native info-card actions rather than duplicating RMB behavior;
* keep pawn-specific native table infrastructure separate from current non-pawn Planner rows;
* avoid duplicated local styling, target renderers or parallel UI helpers for equivalent behavior;
* preserve the agreed sibling-directory build structure until a separately approved post-release repository strategy replaces it.

The existing Planner main tab remains the core UI entry point.

## Harmony and integration policy

New production functionality must first be designed through public or otherwise supported native RimWorld data, UI and component boundaries.

The preferred integration order is:

```text
vanilla/public data access
        ↓
standard RimWorld components and extension mechanisms
        ↓
existing Planner-owned periodic or explicit refresh boundaries
        ↓
separate architecture decision when the feature cannot be implemented safely without lifecycle patching
```

Harmony prefix, postfix or transpiler integration is not an automatic fallback. A feature that cannot be implemented reliably through supported native boundaries must be deferred, excluded from the current release scope or approved through a separate architecture decision that records necessity, compatibility risk and graceful-failure behavior.

Core plan, persistence, inventory, readiness, notifications, assembler, donor, template and Planner UI workflows do not require Harmony patches.

The release code and dependency audit is complete. It confirmed that all discovered Harmony patches, runtime debug actions and associated services belonged to temporary gene-semantics research. No production workflow dependency on patched lifecycle behavior was identified.

The confirmed cleanup has been applied:

* the temporary research-only patches, debug actions, output/session mechanisms and corresponding research tests have been removed;
* confirmed redundant and stale code has been removed while required persistence, portability and compatibility contracts remain intact;
* production and test projects no longer reference `0Harmony`;
* the bootstrap does not create a Harmony instance or call `PatchAll`;
* mod metadata declares Biotech as the only required mod dependency.

The current production baseline therefore operates without Harmony. Any future Harmony use still requires a separate architecture decision under the policy above.

Failures at implemented vanilla integration or notification-delivery boundaries must never prevent:

* save loading;
* plan persistence;
* core Planner UI access;
* explicit or periodic refresh of derived state;
* independent notification evaluation for other plans.

Derived correctness must not depend solely on an integration lifecycle hook when an explicit or periodic fallback is appropriate.

## Native genepack indicator scope

Patch-free research did not establish a sufficiently reliable public/native composition boundary for adding plan-relevance indicators to the existing vanilla genepack presentation surfaces that were inspected.

Native or vanilla UI indicators for plan-relevant genepacks therefore remain outside the accepted Xenogerm Planner surface scope. Harmony integration is not introduced as a fallback for this feature.

This exclusion does not apply to a separate consumer-owned interface that already owns its row data and asks the documented Planner API for read-only relevance. In particular, Settlement Trade Overview may display an indicator in its own global and settlement-specific trade lists without adding an indicator to vanilla genetics UI or changing Planner readiness.

Existing exact-genepack presentation inside Planner remains limited to Planner-owned surfaces and the verified native inspection, indication and navigation boundaries already used by the project.

## External integration API boundary

The implemented architecture provides a separately versioned, read-only and consumer-neutral integration API owned by Xenogerm Planner. Its API version is independent from the mod version.

The stable public facade is:

```text
XenogermPlanner.Api.XenogermPlannerApi, XenogermPlanner
```

It exposes:

```text
ApiVersion = 1

QueryGenepackRelevance(
    IReadOnlyList<GenepackRelevanceRequest>)
    → GenepackRelevanceBatchResult
```

Version `1` is the current public integration contract. The API version changes only as part of a released Planner build. Optional consumers support only explicitly known versions and must not assume that a value greater than `1` is compatible.

The external query boundary is:

```text
batch of runtime-free GeneDef.defName compositions
        ↓
Planner-owned plan and current product-inventory analysis
        ↓
batch and item statuses
+ ordered matching plan stable IDs
+ ordered matching plan display names
```

The public request and response surface does not accept live `Thing`, `Genepack`, `GeneDef`, `GeneSet` or `Tradeable` objects and does not expose internal readiness, persistence or UI types. Structural validation, unknown-definition handling, unavailable-context reasons, batch correspondence and item-level failure isolation are part of the public version `1` contract. Results are ordered deterministically by normalized display name and then stable plan ID.

Relevance semantics are owned by Planner:

* only plans whose top-level result is `NotReady` are considered;
* `Ready`, `EmptyTarget`, `Degraded` and `Unavailable` are excluded;
* in Coverage mode, an offered composition is relevant when it contains at least one `Missing` target gene; additional genes are allowed;
* in Exact payload mode, an offered composition must contain no genes outside the target and must contain at least one `Missing` or `ExactPayloadConflict` target gene;
* prerequisite-only genepacks are not considered relevant;
* a plan already ready through current product inventory is not returned.

The query uses only current Planner readiness and its derived target-gene diagnostics. It does not add the offered composition to product inventory, recompute a hypothetical persisted state, mutate any plan, alter notification cursors or establish a reservation. External trade offers remain advisory and do not satisfy readiness.

Optional consumers discover and invoke the public facade through soft binding. Planner does not reference Settlement Trade Overview, and absence of any consumer does not affect Planner behavior. The exact public contract, statuses and minimal consumer flow are documented in [`docs/integration-api.md`](integration-api.md).

## Potential gene sources boundary

Potential gene sources are a separate advisory concept.

The accepted boundary is:

```text
physical Genepack inventory
→ participates in plan readiness

potential pawn gene sources
→ implemented informational acquisition hints

active orbital and visiting caravan trader genepack offers
→ implemented advisory notifications
→ native current-state polling and reconciliation
→ transient deduplicated delivery lifecycle
```

No advisory source satisfies missing plan requirements or changes a plan from not ready to ready. Trader stock remains outside physical product inventory; `PassingShip` stock is specifically excluded even though generic map-holder traversal can discover it.

The implemented trader advisory uses the confirmed native current-state boundaries: active `TradeShip` instances from `Map.passingShipManager.passingShips` and visiting trader caravans from `Map.lordManager.lords` with `LordJob_TradeWithColony` and `TraderCaravanUtility.FindTrader`. Concrete current stock is read through `ITrader.Goods`, and concrete `Genepack` composition is read through its native `GeneSet` boundary.

The confirmed lifecycle result does not provide one suitable public stock-ready arrival event for both supported trader branches. The implemented trader advisory therefore uses Planner-owned current-state refresh and reconciliation rather than lifecycle patching.

### Trader advisory refresh, reconciliation and notification lifecycle

Trader advisory state belongs to a dedicated Planner-owned game-level runtime component. It remains separate from `XenogermPlan`, `PlanGenepackInventorySnapshot`, `PlanReadinessResult`, assembler readiness and the existing product-readiness notification cursor. The component owns only derived current-map trader advisory state and delivery bookkeeping for active trader-source lifetimes.

The implemented runtime boundary is:

```text
current active Map
        ↓
supported current ITrader sources
        ↓
current concrete Genepack offers
        ↓
transient trader advisory snapshot
        +
current XenogermPlans
        +
current product-inventory snapshot
        ↓
Planner-owned relevance semantics
        ↓
reconciliation + transient notification ledger
        ↓
optional advisory PositiveEvent message
```

Trader discovery and stock refresh use a 60-game-tick polling cadence. The polling boundary discovers the supported traders from current native map state and reads their current concrete `ITrader.Goods`. This cadence is intentionally independent from the 600-tick product-inventory fallback refresh because trader stock is temporary advisory state and notification usefulness requires a more responsive current-state check.

A plan mutation or a change in current product-inventory snapshot identity invalidates trader relevance analysis immediately. The current known trader offers may then be reevaluated without waiting for the next trader discovery/stock poll. Stock appearance, disappearance and mutation that have no common reliable public lifecycle event are detected by the periodic trader poll.

The active map is part of the advisory lifecycle identity. When the active map changes, all transient trader advisory and delivery state is discarded and the newly active map starts a new baseline lifecycle. Trader activity that occurred while another map was active does not generate retroactive notifications when the map later becomes active.

The first determinate reconciliation after component initialization, save load or active-map change establishes a silent baseline. Existing traders and offers are recorded, and every relevance pair that exists in that baseline is marked acknowledged without delivering a message. An unavailable game, map or required Planner analysis context does not establish or advance that baseline. No trader advisory stock, relevance result or notification ledger is persisted across save/load.

Runtime identity uses exact object identity:

* an orbital trader source is the concrete active `TradeShip`;
* a visiting caravan source is the concrete trader `Pawn` resolved through `TraderCaravanUtility.FindTrader`;
* an offered item is the concrete `Genepack`;
* plan identity remains the stable `XenogermPlan` ID.

The delivery-deduplication unit is:

```text
trader source lifetime
+ exact Genepack reference
+ XenogermPlan stable ID
```

An acknowledged relevance pair is not delivered during the same concrete trader-source lifetime even if it temporarily becomes irrelevant and later relevant again. A pair becomes acknowledged either because it belonged to the silent baseline or because a notification containing it was successfully delivered. Renaming the plan does not change the pair identity. A new plan ID, a new physical `Genepack`, or a new trader-source lifetime can create a new deliverable pair. When a trader source disappears from current native state, its transient source state is removed completely.

All newly deliverable pairs for one trader discovered during one reconciliation are aggregated into at most one notification for that trader. After successful delivery, those pairs become acknowledged. Baseline acknowledgement is separate from successful delivery so initial/load/map-change state never becomes a delayed retroactive notification.

`ITrader.CanTradeNow` is a delivery gate, not a discovery or relevance rule. Current trader stock can be discovered and analyzed while `CanTradeNow` is false. A newly relevant pair first observed after the silent baseline remains pending and unacknowledged while the trader cannot trade; if it is still present and relevant when a later reconciliation observes `CanTradeNow == true`, it may then be delivered and acknowledged. A pending pair that is no longer current or relevant is removed from pending state. A later `true → false → true` transition does not re-deliver pairs already acknowledged during that source lifetime.

Trader relevance reuses one Planner-owned semantic implementation shared with the existing public API version `1` behavior. The trade subsystem does not depend on `XenogermPlanner.Api.Internal` as an architectural layer and does not introduce a second Coverage/Exact-payload algorithm. The shared evaluator remains in a consumer-neutral Planner-owned analysis boundary while the public API version `1` surface and results remain unchanged.

Trader notification presentation uses a localized standard non-historical `PositiveEvent` message and the existing shared Planner presentation boundary. One notification identifies the trader, summarizes the newly relevant physical genepack offers and the affected plan display names in deterministic presentation order; stable plan IDs remain the internal identity.

For a visiting trader caravan, the exact trader pawn is the navigation target and uses the existing current-map target interaction boundary. An orbital `TradeShip` has no equivalent verified map target, so its implemented notification is text-only. The feature does not invent or patch a communications-console or trade-dialog navigation path solely for presentation symmetry.

Failures while scanning, evaluating or delivering one trader source must be isolated so they do not block other sources, plan persistence, product readiness, the existing readiness-notification lifecycle or core Planner access. The lifecycle does not require Harmony and does not change public integration API version `1`.

The main pawn donor rule is based on verified Gene Extractor output-selection semantics:

```text
pawn carries desired GeneDef
+ desired gene can occur in at least one valid extraction sequence
→ potential donor
```

At the architecture level, the verified vanilla sequence rules require the following distinctions:

* merely carrying a desired gene is not sufficient when vanilla selection can never reach it;
* current Gene Extractor infrastructure and logistics eligibility do not determine the main donor count;
* a potential extraction outcome is not a guaranteed result.

Donor analysis returns derived current-runtime information and exact pawn references. It is not persisted in `XenogermPlan`.

The implemented source collection contains all spawned pawns on the current active map that have a gene tracker. It does not apply faction, colony-status or Gene Extractor logistics filters and must not be inferred from `Building_GeneExtractor.CanAcceptPawn`.

The Planner presents donor information for resolved product-level missing genes through a sortable count control and a separate details view, with shared map indication and navigation for exact pawns.

Pawn-specific architecture and UI terminology uses `potential donor`. The umbrella term `potential gene sources` is reserved for advisory source kinds and includes the implemented trader-offer advisory boundary.

## Release support and known limitations

Compatibility and resilience validation for the current release baseline is complete. The localization wording audit, shared icon-tint migration, ModIcon and MainButton replacement, SVG normalization, packaged texture regeneration, analysis-cache optimization, Plan Editor mutation fix and current automated builds are part of the implemented baseline. The public support policy is defined in `docs/compatibility.md`; this section records the architecture implications of that policy.

### Supported integration boundary

The declared baseline is RimWorld 1.6 with Biotech and the standard vanilla genetics boundaries used throughout this document. Clean Biotech and representative data-driven modded-gene configurations were validated before release documentation was finalized.

Confirmed failures at supported integration boundaries are handled so they do not prevent save loading, plan persistence or access to the core Planner interface. Stale or destroyed transient targets remain safe no-ops where an exact runtime reference can no longer be resolved.

Third-party patches or replacement systems that change `GeneDef`, `Genepack`, holder traversal, Gene Assembler, Gene Extractor, `CustomXenogerm` or Scribe semantics remain unverified unless a later release policy explicitly expands support. This is a support boundary, not unfinished feature work.

### Extreme UI data sets

Runtime profiling and corrective optimization are complete for the current release baseline. Representative open-window scenarios no longer show a persistent Planner-specific performance regression on the measured system after list virtualization, layout caching, window-owned analysis caching, lazy tab-specific analysis and stable presentation projections.

The measurements apply to the tested hardware, save state and mod configuration and are not a workstation-independent frame-rate guarantee. New optimization work requires a newly confirmed hotspot rather than speculative changes to accepted architecture.

### Temporarily missing modded `GeneDef`

Independent `XenogermPlan` persistence preserves unresolved desired-gene def names and exposes a degraded plan state under the rules in this document.

Vanilla `CustomXenogerm.GeneSet` follows a different lifecycle: unresolved `GeneDef` references can become `null` and be removed during `PostLoadInit`. Recovery of those removed vanilla template entries after restoring the source definition is not guaranteed, especially after the affected save is written again. Source-based creation uses only the composition visible in the current runtime object.

The source-level evidence leaves vanilla recovery after a temporarily unavailable definition uncertain. That uncertainty does not authorize reconstruction from labels or other presentation metadata.

Gene Extractor selection semantics required for potential-donor analysis are verified for the accepted boolean participation rule. Evidence limitations around exact probabilities, duplicate cross-layer instances and third-party patched extractors do not expand the declared support scope.

## Implementation dependency flow

The implemented baseline is:

```text
XenogermPlan model, persistence and unified creation/reuse flows
        ↓
independent plan duplication and versioned clipboard transfer
        ↓
current-map product inventory policy and refresh lifecycle
        ↓
coverage / exact payload readiness and special result states
        ↓
per-gene source composition diagnostics
        ↓
selected Gene Assembler lifecycle and visible physical scope
        ↓
assembler-specific physical candidate search
        ↓
live assembler readiness, prerequisite-aware candidate checks and blocking reasons
        ↓
active-scope Gene availability with exact physical genepacks
        ↓
shared current-map indication and camera navigation
        ↓
verified conflicting-gene semantics and accepted physical-payload target policy
        ↓
project-owned conflict, random-choice-group and prerequisite diagnostics
        ↓
shared raw / effective plan biostat calculation and live Plan Editor biostat presentation
        ↓
searchable compact Planner details tabs, shared style registry, shared icon actions and completed diagnostics presentation
        ↓
project-owned sortable Gene availability tables with deterministic presentation ordering
        ↓
native info-card integration for genes and exact genepacks using verified public boundaries
        ↓
verified Gene Extractor potential-donor semantics
        ↓
spawned current-map potential-donor scope and bounded donor analysis
        ↓
sortable donor counts and exact-pawn donor-details UI with shared target interaction
        ↓
save-local CustomXenogerm template generation from bounded deterministic product-inventory candidates
        ↓
generation feedback, complete/incomplete alternative state and mandatory grouped preview
        ↓
product-level readiness notifications with per-plan setting and persisted delivery cursor
        ↓
release cleanup and Harmony-free Biotech-only integration baseline
        ↓
fixed-height and variable-height UI virtualization and cached layouts
        ↓
window-owned analysis caching, lazy tab-specific analysis and identity-based presentation projections
        ↓
safe post-render Plan Editor list mutation
        ↓
validated automated contract suite, explicit performance profiling and completed runtime regression acceptance
```

The Planner integration dependency flow is complete:

```text
separately versioned read-only integration API design
        ↓
Planner-owned relevance query
        ↓
public API contract and semantic integration tests
        ↓
docs/integration-api.md for API version 1
```

The trader-advisory implementation dependency flow is complete:

```text
completed native trader current-state research
        ↓
accepted refresh / reconciliation / deduplication / delivery lifecycle design
        ↓
current supported trader-stock analysis and transient reconciliation
        ↓
advisory trader notification delivery
        ↓
automated lifecycle coverage and runtime acceptance
```

Trader stock analysis, transient reconciliation, notification delivery, automated lifecycle coverage and runtime acceptance are part of the validated release baseline.

The generic UI migration, unique display-name policy, target-gene `ExactPayloadConflict` analysis and presentation, API implementation, optional STO consumer integration, localization audit, tint-aware icon migration, approved visual asset replacement, category-specific SVG normalization, packaged texture regeneration, analysis-cache optimization, Plan Editor mutation fix, automated regression coverage and current-build validation are complete baseline work. They are not repeated as future stages. Native or vanilla Planner genepack indicators remain excluded.

The architecture decisions in this document define the boundaries of future changes. No unresolved product or architecture decision blocks the current release baseline.

The testing policy in `docs/testing.md` defines which project contracts are protected through automated tests and which scenarios require runtime acceptance.