
# Xenogerm Planner testing policy

This document defines the accepted automated testing and runtime acceptance boundaries of Xenogerm Planner. The current baseline includes the completed shared UI and layout-cache migration, tint-aware icon call-site migration, normalized primary and button assets, collection-level unique-name policy, target-gene `ExactPayloadConflict` analysis and presentation, separately versioned read-only integration API version `1`, window-owned analysis caching, the validated unified plan-creation flow and the implemented trader advisory lifecycle. The external shared source is compiled into the Planner assembly and its ownership boundary has consumer integration coverage. Public reflection shape, DTO invariants, batch orchestration, Planner-owned relevance semantics, semantic integration, read-only API behavior, analysis-cache reuse and invalidation, source transformation and revalidation, xenotype source projection, trader current-stock transformation, reference-identity reconciliation, notification deduplication, delivery gating and cross-subsystem regression boundaries are covered. The final SVG sources and regenerated packaged PNG textures are present under the normalized paths. The current Release build, complete NUnit suite, repeated UI profiling and runtime regression validation, including trader advisory runtime scenarios, have been validated successfully.

It is a testing policy, not a product architecture specification or vanilla implementation reference.

## Testing principle

Xenogerm Planner uses a risk-based, contract-oriented testing approach.

The primary question before adding or retaining an automated test is:

> Which meaningful Xenogerm Planner-owned contract does this test protect, and which realistic regression can it detect?

Automated tests are expected to protect behavior where a regression can silently change planning or readiness semantics, corrupt or lose mod-owned persisted data, break meaningful state transitions, or change a non-trivial deterministic algorithm.

Test count and broad method-level coverage are not goals by themselves.

A test that does not protect a concrete project contract should not be retained only because the corresponding class or method exists.

## What automated tests protect

Automated tests should focus on the following areas.

### Core semantics

Core project-owned semantics should be tested thoroughly.

This includes:

* `XenogermPlan` invariants;
* stable plan identity and collection-level display-name normalization through `Trim()` and `OrdinalIgnoreCase`;
* deterministic unique-name allocation for duplicate, source-based creation, paste and restore paths;
* manual create and rename conflict rejection without silent renaming;
* distinct desired-gene semantics;
* degraded unresolved requirements;
* independent identity and mutable state for duplicated and pasted plans;
* Coverage and Exact payload semantics;
* target-gene `Available`, `Missing` and Exact payload `ExactPayloadConflict` classification;
* exact-conflict-only `NotReady` results and preservation of the physical distinction between missing and incompatible availability;
* project-owned ordinary, random-choice-group, mixed-conflict and prerequisite diagnostics;
* deterministic diagnostic grouping and ordering where presentation semantics depend on them;
* product genepack combination search;
* assembler-specific physical candidate search;
* readiness state precedence;
* prerequisite-aware assembler candidate evaluation and other assembler blocker rules;
* deterministic candidate selection rules;
* reference-identity semantics where physical object identity is part of the project contract;
* potential-donor selection-participation semantics and exact transient pawn-reference results;
* template composition candidate validity for Coverage and Exact payload modes;
* template candidate deduplication, irredundancy, deterministic scoring and automatic-candidate selection;
* bounded template search limits, deterministic fallback validity, retained-candidate caps and complete/incomplete result semantics;
* semantic-composition-to-physical-pack resolution and stable representative selection;
* shared raw and effective plan-gene biostat projections, including vanilla-style override projection, duplicate removal and independent complexity, metabolism and Archite totals;
* candidate-level vanilla-style and per-composition raw genetic biostat calculations through the shared calculation boundary;
* product-level readiness-notification transition semantics, including determinate baseline initialization, `Unavailable` handling, non-ready-to-ready delivery, re-arming and disabled-setting tracking;
* source-neutral Plan Editor initial-state semantics for new plans, including defensive distinct-gene snapshots and the default Coverage / readiness-notification settings;
* unified source-list projection semantics, including independent collapsed-group state and deterministic source ordering.

A regression that can silently turn `Ready` into `NotReady`, hide a valid candidate, select a different candidate under deterministic rules, treat an unresolved requirement as satisfied, or miss a supported blocker requires automated protection.

### Persistence contracts

Mod-owned persistence behavior should be tested at the project's persistence boundary.

This includes:

* plan save-record round trips;
* stable identity and persisted plan-owned fields;
* degraded requirement restoration;
* invalid record isolation;
* per-plan readiness-notification settings and notification delivery cursor round trips;
* backward-compatible defaults for save records created before notification fields existed;
* deterministic duplicate-name normalization during restore;
* supported migration behavior after migrations are introduced.

Development-only empty names and earlier development save formats are not a compatibility target and do not require a legacy migration suite. Tests should instead protect the current accepted validation and the release save-compatibility baseline.

Automated tests should protect Xenogerm Planner's persistence transformations and validation rules.

They should not attempt to reproduce the complete RimWorld Scribe runtime when the behavior under test belongs to the game runtime rather than project-owned logic. In particular, adding the mod to an existing save, disabling it while Planner-owned `GameComponent` records are present, RimWorld's handling of missing component classes, save rewriting without the mod and preservation of vanilla `CustomXenogerm` data belong to runtime acceptance rather than the NUnit persistence suite.

### Portability contracts

Project-owned plan portability behavior should be tested independently from save-record persistence.

This includes:

* versioned clipboard transfer round trips;
* creation of a new stable identity for every pasted plan;
* deterministic numeric name allocation when pasted names conflict with the destination collection;
* exclusion of the source plan ID from transfer data;
* preservation and destination-side resolution of unresolved `GeneDef.defName` requirements;
* deterministic normalization of duplicate gene requirements;
* exclusion of local readiness-notification settings and notification delivery cursor state from the clipboard payload;
* isolation of malformed payloads and explicit rejection of unsupported versions.

These tests protect the transfer contract. They do not attempt to simulate the operating system clipboard or Unity input handling.

### Meaningful lifecycle and state transitions

Non-trivial lifecycle behavior should be tested when Xenogerm Planner owns the transition rules.

Examples include:

* derived inventory first-build and reuse behavior;
* explicit invalidation;
* active-map identity changes;
* periodic fallback refresh;
* readiness-notification determinate baseline initialization;
* suppression of repeated ready and non-ready evaluations;
* re-arming after a determinate ready-to-non-ready transition;
* preservation of the notification delivery cursor across save/load;
* continued cursor tracking while per-plan delivery is disabled;
* product readiness and target analysis reuse for unchanged plan and inventory identities;
* immediate analysis-cache rebuild after plan, inventory, map or assembler identity changes;
* bounded donor, selectable-assembler and assembler live-state refresh;
* complete analysis-cache invalidation on the explicit window lifecycle boundary.

Trader advisory automated lifecycle coverage protects the following project-owned contracts:

* 60-game-tick trader discovery/stock polling without coupling correctness to a vanilla arrival event;
* silent first determinate baseline after component initialization, save load and active-map change, with current relevance pairs acknowledged without delivery;
* unavailable game/map/Planner context not initializing or advancing that baseline;
* active-map changes discarding prior transient trader-source and delivery state;
* reference-identity reconciliation for concrete `TradeShip`, caravan trader `Pawn` and physical `Genepack` objects;
* delivery deduplication by trader-source lifetime, exact `Genepack` reference and stable plan ID;
* a baseline-acknowledged or successfully delivered pair remaining suppressed after temporary relevance loss and regain during the same source lifetime;
* disappearing trader sources releasing their transient delivery state so a later concrete source starts a new lifetime;
* one reconciliation aggregating newly deliverable pairs into at most one notification per trader;
* `ITrader.CanTradeNow` gating delivery without changing discovery or relevance classification;
* newly observed post-baseline pairs remaining pending while `CanTradeNow` is false, becoming deliverable when it later becomes true if still current and relevant, and being removed from pending state when no longer current/relevant;
* plan mutation or product-inventory snapshot identity change reevaluating current known offers without requiring another trader discovery scan;
* trader advisory state remaining non-persisted and separate from `XenogermPlan`, product inventory and readiness notification cursors;
* trader relevance reusing the same Planner-owned Coverage/Exact-payload semantics as public API version `1` without a parallel algorithm.

The test should target the project-owned transition contract rather than the existence of individual methods.

### Non-trivial deterministic algorithms

Search, filtering and transformation logic with project-owned rules should be tested for semantic correctness and determinism.

Relevant examples include:

* combination search;
* exact-payload filtering;
* assembler composition grouping;
* physical source preference rules;
* candidate ordering;
* best blocked candidate selection;
* bounded potential-donor extraction-sequence search;
* template semantic composition grouping and bounded enumeration;
* template alternative ordering, candidate-key determinism and truncated-result determinism;
* fixed-height and variable-height visible-range calculations supplied by the external shared UI source and protected by the canonical shared test project;
* variable-height cumulative row geometry and canonical shared layout-cache invalidation;
* the existing consumer assembly-boundary contract proving that shared types are compiled into `XenogermPlanner.dll`;
* consumer integration with shared generic controls without duplicating Planner-specific semantics;
* stable filtered, sorted, grouped and formatted presentation projections;
* identity-based gene-coverage projection-cache compatibility and explicit invalidation;
* pack-order independence where the contract requires it.

Fast deterministic stress scenarios may remain in the regular test suite when they protect algorithmic behavior that ordinary fixtures do not exercise adequately.

## External integration API contracts

The separately versioned read-only API version `1` is a public project boundary with direct automated protection.

The implemented coverage protects:

* facade discovery by the stable assembly-qualified name;
* `ApiVersion` discovery independently from the mod version and the exact version `1` public enum values;
* the public batch method signature, DTO constructors and read-only property surface;
* runtime-free request validation and ordinal duplicate normalization;
* documented handling of unknown definitions and structurally invalid requests;
* explicit unavailable behavior without an active game, active map or usable Planner state;
* composition-to-result correspondence, request order and item-level failure isolation;
* deterministic match ordering by normalized display name and then stable plan ID;
* response identity containing both `PlanId` and `DisplayName`;
* Coverage relevance when an offered composition intersects `Missing` target genes, including additional offered genes;
* Exact payload relevance only when the offered composition has no genes outside the target and intersects `Missing` or `ExactPayloadConflict`;
* inclusion of exact-conflict-only `NotReady` plans;
* exclusion of `Ready`, `EmptyTarget`, `Degraded` and `Unavailable`;
* exclusion of prerequisite-only offers;
* repeated equivalent queries producing equivalent public results;
* queries not mutating plans, product inventory, notification settings or delivery cursors;
* absence of forbidden runtime or internal Planner types from the public API surface.

API tests target the documented public surface and public soft-binding sequence rather than reflection over private implementation types. Focused internal seams remain limited to deterministic orchestration and semantic integration that cannot construct the full game runtime in the test host.

The Planner suite does not prove that an external consumer handles optional discovery correctly. Consumer-specific absence, unsupported-version and projection behavior belongs to that consumer's test policy and runtime acceptance.

## Vanilla-facing boundaries

Vanilla-facing adapters may be tested through minimal injected seams when Xenogerm Planner applies its own filtering, identity or transformation rules to data obtained from RimWorld.

Examples include:

* product inventory inclusion and exclusion rules;
* assembler-visible scope projection;
* physical reference deduplication;
* preservation of facility power metadata;
* spawned current-map potential-donor scope, gene-tracker filtering and exact pawn-reference deduplication;
* `CustomXenogerm` source flattening, distinct-gene transformation and runtime-source revalidation;
* premade `XenotypeDef` and saved `CustomXenotype` source transformation into source-neutral editor state;
* saved `.xtp` source discovery, lazy native loading, unchanged-file cache reuse, metadata-driven reload and missing/load-failure isolation through injected vanilla-facing seams;
* template physical re-resolution from current inventory;
* validation of a selected template candidate against the current plan mode and target;
* transformation of the vanilla save helper result into project-owned success or failure states.

These tests answer:

> Given boundary data with state X, does Xenogerm Planner transform or classify it according to project rule Y?

They do not prove that RimWorld itself produces state X or implements the corresponding vanilla behavior in a particular way.

Verified vanilla behavior remains grounded in the installed RimWorld implementation, source-level verification and runtime validation.

A fake object or injected delegate must not be described as evidence of vanilla implementation behavior.

Production architecture should not be expanded with a parallel abstraction solely to make a trivial vanilla API call unit-testable.

## Presentation policy

Presentation tests are appropriate when they protect non-trivial user-facing policy.

Examples include:

* whether diagnostics are shown for a specific readiness state;
* which diagnostic wins when several semantic states could apply;
* meaningful fallback behavior for unsupported values;
* user-facing ordering when ordering is part of the presentation contract;
* formatting logic that combines several semantic values and is easy to misrepresent;
* donor-count applicability and deterministic donor-count or donor-name ordering where those rules affect user interpretation;
* metabolic-efficiency sign formatting and vanilla-style hunger-rate percentage formatting;
* template candidate summaries, complete/incomplete search-state policy, disabled reasons and save-failure presentation when they combine multiple semantic values;
* source-list grouping and source metadata projection where collapse state, source kind or failure state changes user interpretation.

Automated tests are generally not required for:

* a trivial `enum → translation key` mapping;
* a trivial `enum → color` mapping;
* literal labels;
* spacing or fixed layout constants;
* the presence of a visual arrow or similar glyph;
* direct `Widgets` calls;
* tooltip text that only forwards one localization key.

A simple presentation helper should not be extracted into a new pure abstraction only to make a trivial mapping testable.

## Runtime acceptance

Scenarios that require the actual RimWorld, Verse or Unity runtime should be checked through runtime acceptance or compatibility testing rather than simulated in the NUnit host.

This includes:

* continued successful loading and use of the connected sibling `Escarval.RimWorld.UI` source without a separate runtime UI DLL;
* IMGUI layout and visual hierarchy;
* actual colors;
* clipping and scrolling;
* tabbed Planner interaction and layout;
* `Create` FloatMenu interaction and routing to `From scratch`, `From xenogerm template` and `From xenotype`;
* semantic UI parity between xenogerm-template and xenotype source selection, including source list, gene preview, Cancel/Continue behavior and absence of plan settings from the source selector;
* actual system clipboard integration;
* potential-donor dialog layout, scrolling, full-row hover indication and camera navigation;
* template-generation feedback dialog lifecycle and deferred search after the first rendered frame;
* template-creation dialog layout, automatic/custom mode switching and alternative selection;
* complete and incomplete template-search warnings and Automatic/Customize behavior;
* template grouped preview, additional-gene summary above the scroll area, overflow marker, full-list tooltip, scrolling and dynamic candidate-card layout;
* template icon selector, name validation and modal lifecycle;
* candidate-level and per-`GeneSet` genetic biostat rendering with public vanilla assets;
* live Plan Editor complexity, metabolic efficiency, hunger rate and Archite Capsule rendering in create and edit modes;
* manual name validation, source-based creation / duplicate / paste allocation and restored duplicate-name presentation;
* Exact payload conflict status, labels, colors and tooltips in English, Russian and Ukrainian;
* partial biostat presentation for an unchanged degraded plan and complete recalculation after the selected target changes;
* parity with the vanilla metabolism-to-hunger-rate conversion and percentage presentation;
* direct vanilla template save, same-name replacement and subsequent load through vanilla UI;
* English, Russian and Ukrainian layouts in the supported release configuration;
* modal force-pause behavior for Planner dialogs that absorb surrounding input;
* button interaction;
* project-owned texture loading, icon rendering and disabled visual states;
* `DefDatabase` initialization;
* actual Scribe lifecycle;
* actual connected facility graphs;
* actual power component behavior;
* actual game-map state;
* actual RimWorld readiness-notification message presentation and lifecycle;
* real Scribe save/load suppression behavior and pre-feature save defaults for notification fields;
* readiness-mode and readiness-notification controls in the Plan Editor for new and existing plans, with those controls absent from source-selection dialogs;
* collapsible premade/saved xenotype groups, including repeated collapse/expand and scrolling without stale visible indices or an unbalanced scroll-view mouse-position stack;
* actual saved `.xtp` discovery and vanilla version/load behavior, including source failure recovery and transition to editable Plan Editor state;
* confirmation that selecting a source does not create a plan before the Plan Editor is saved;
* independent delivery for multiple plans and active-map unavailable/restore scenarios;
* implemented vanilla integration boundaries;
* public API availability with an active map, without an active map and across new-game/load transitions;
* absence of additional runtime assemblies for shared UI or API DTOs;
* dependency metadata and absence of unexpected runtime assemblies;
* consistency between public support claims and the configurations actually accepted for support;
* Harmony integration only when a future separately approved production or research patch exists;
* parity with vanilla dialogs or other vanilla runtime flows;
* unchanged open-window analysis reuse across Overview, Gene assembler and Gene effects scenarios;
* repeated Plan Editor selected-gene removal, including the final visible rows, without `ArgumentOutOfRangeException` or an unbalanced scroll-view mouse-position stack;
* adding Xenogerm Planner to an existing save without pre-existing Planner-owned state;
* removing Xenogerm Planner from a save that contains persisted plans and vanilla xenogerm templates created through the Planner, including first-load missing-component logging, continued game loading, resave cleanup, a subsequent clean reload without Planner component errors, loss of Planner-owned plan state and preservation of vanilla `CustomXenogerm` template data.

Trader advisory runtime acceptance covers:

* orbital trader arrival, generated-stock discovery through current `Map.passingShipManager.passingShips`, departure and source cleanup;
* visiting trader caravan arrival, trader-pawn resolution through `LordJob_TradeWithColony` / `TraderCaravanUtility.FindTrader`, departure and source cleanup;
* trade-stock mutation after an actual trade becoming visible through a later current-state refresh;
* save/load with an already present trader establishing a silent baseline rather than producing a retroactive notification;
* switching away from and back to a map establishing the accepted per-active-map silent baseline;
* a newly relevant post-baseline offer observed while `ITrader.CanTradeNow` is false remaining undelivered until the trader becomes tradeable, when still current and relevant;
* standard localized non-historical `PositiveEvent` presentation with aggregation of new relevant pairs per trader;
* visiting-caravan notification navigation to the exact current trader pawn through the shared target-interaction boundary;
* orbital notifications remaining text-only rather than opening an unverified communications or trade UI path;
* no trader offer entering the physical product inventory, changing `PlanReadinessResult`, mutating `XenogermPlan` or altering existing readiness-notification cursors;
* failures for one trader source not preventing other trader evaluation, plan persistence, existing readiness notifications or core Planner access.

Runtime acceptance should verify the visible or integration behavior that cannot be established reliably through project-owned deterministic logic alone.

The completed current-build acceptance verified the migrated shared UI boundary, tint-aware icon call sites, normalized source and packaged asset paths, unique-name workflows, target-gene `ExactPayloadConflict` behavior, English/Russian/Ukrainian presentation, API version `1`, window analysis-cache lifecycle, the Plan Editor selected-gene removal regression, the unified creation flow, the trader advisory lifecycle and the documented save-removal behavior. The current Release build and complete NUnit suite passed successfully, including source transformation, source-list projection, template-source provider, xenotype-source provider, editor-initial-state, API, semantic integration, analysis-cache, projection-cache, trader stock scanner, trader lifecycle, notification tracker and trader cross-subsystem regression fixtures. Runtime validation completed without identified problems in normal continued play and covered the accepted trader advisory behavior alongside the existing creation, readiness, notification, integration and UI regression matrix.

Save-removal acceptance used a save containing multiple persisted Planner plans and a vanilla xenogerm template created from a ready plan. The first load with Xenogerm Planner disabled produced the expected missing-class / abstract `GameComponent` deserialization errors for Planner-owned components in the development log, but the save loaded and continued. Saving once without the mod removed all `XenogermPlanner` component records from the rewritten save; the subsequent load produced no Planner component errors. The Planner plans were no longer present, while the vanilla `CustomXenogerm` template and its gene composition remained unchanged. This runtime behavior defines the current removal compatibility claim; it should not be replaced by a simulated NUnit assertion about RimWorld Scribe internals.

Consumer integration tests prove that shared types are compiled into `XenogermPlanner.dll`, while API surface tests prove that no separate runtime DTO assembly is required. Repeated runtime profiling confirmed that the tested open-window scenarios no longer exhibit a persistent Planner-specific regression; those measurements are diagnostic evidence rather than a universal frame-rate guarantee.

## Regression tests

A bug does not automatically require a new automated test.

Add a regression test when:

* the defect belongs to deterministic Xenogerm Planner-owned logic;
* the original failure can be reproduced through the normal contract of the affected layer;
* the test protects a meaningful behavior from realistic recurrence.

Good regression-test targets include:

* an unresolved desired gene being counted as satisfied;
* Exact payload accepting an additional gene;
* a blocked first assembler candidate hiding a usable alternative;
* derived inventory failing to rebuild after a relevant active-map transition;
* duplicate display names surviving a supported mutation path;
* an exact-incompatible source being presented as Exact payload `Available`;
* the API returning a ready, degraded or prerequisite-only match.

A test is usually not justified only to protect the test host from artificial fixture behavior, RimWorld initialization absent from NUnit, or a visual layout detail.

## Guard clauses and invalid input

Null and invalid-input tests should be selective.

They are valuable when:

* the tested method is a public or important internal boundary;
* invalid data can realistically arrive from persistence or runtime integration;
* safe degradation or the type of failure is part of the contract.

Tests should not be added mechanically for every `ArgumentNullException` or private/helper guard clause.

The presence of defensive code alone does not require one test per guard.

## Stress and performance testing

Fast deterministic stress tests may run as regular tests when they protect:

* larger gene or genepack sets;
* repeat determinism;
* pack-order independence;
* pathological exact-payload or candidate-search scenarios;
* large template candidate spaces, bounded completion, valid fallback candidates and repeat deterministic alternative ordering;
* large fixed-height and variable-height layout inputs without workstation-specific timing assertions;
* large sets of API query compositions when a batch contract is selected, without workstation-specific timing assertions.

Performance benchmarks are diagnostic tools and remain separate from the regular regression suite.

They should:

* be explicit;
* run locally in Release configuration;
* use the same workload and measurement boundary for direct before/after comparisons;
* keep timing and managed-allocation measurement in separate passes so allocation instrumentation does not become part of the timing boundary;
* report timing Median as the primary duration metric, P95 as supporting evidence and Max as diagnostic data;
* report managed allocated bytes as the primary allocation metric;
* use Gen 0 collection counts as supporting allocation-pressure evidence and Gen 1 / Gen 2 only as diagnostics;
* verify semantic correctness of every measured result in addition to reporting performance data.

The current shared performance helper performs warm-up before the measured passes, forces collection before each measured pass, measures repeated timing samples with `Stopwatch`, and measures current-thread managed allocation separately. GC collection counts are recorded around the complete allocation pass rather than summed per operation.

Acceptance decisions for substantive performance work should use several independent process-level runs when timing stability matters. Run-level Median is the primary timing comparison. P95 helps identify tail behavior, while Max is too sensitive to isolated system outliers to serve as an acceptance metric by itself. Managed allocated bytes have proven more stable than higher-generation GC counts and should carry more weight in allocation comparisons.

The current `performance.runsettings` selected matrix contains 13 scenarios across readiness, relevance, assembler candidate search, Gene Coverage projection and effective biostat calculation. Additional explicit benchmarks may exist outside that selected validation matrix.

Performance comparison history must preserve workload chronology:

* direct before/after comparisons should use only scenarios that exist on both measured revisions;
* scenarios introduced later establish regression baselines from the revision where they first appear;
* later scenarios do not need to be backported to older revisions only to produce an equal benchmark count;
* cumulative validation should compare each workload against the appropriate historical or introduction baseline.

Template candidate benchmarks should verify bounded completion and semantic validity in addition to reporting diagnostic timings. Runtime UI profiling should use representative normal and extreme data sets. The completed analysis-cache profiling used stable baseline, large-plan, small-plan and tab-specific scenarios; measurements from one machine must not be treated as a workstation-independent FPS guarantee.

Performance benchmarks should not use arbitrary workstation-specific pass/fail timing thresholds without a separately justified requirement. Functional equivalence remains mandatory even when a performance result is favorable.

## Test decision checklist

Before adding or retaining an automated test, use the following decision sequence:

```text
Can the regression change gameplay or readiness semantics?
→ Yes: test it.

Can the regression lose or corrupt mod-owned persisted data or produce ambiguous plan names?
→ Yes: test it.

Is this a non-trivial project-owned lifecycle or state transition?
→ Yes: test it.

Is this deterministic search, filtering or transformation with project-owned rules?
→ Yes: test it.

Is this a public integration API contract or a vanilla-facing adapter where Xenogerm Planner applies its own rule?
→ Test the project-owned rule through the smallest practical seam.

Is this only a literal mapping, color, label, spacing or direct Widgets call?
→ Usually do not unit-test it.

Does reliable verification require the actual RimWorld runtime?
→ Use runtime acceptance or compatibility testing.

Would production architecture need to change only to make a simple detail unit-testable?
→ The test is probably not justified.
```

The regular automated suite should remain focused on meaningful project contracts.

Runtime acceptance and compatibility testing complement the suite; they are not substitutes for project-owned semantic tests, and the NUnit suite is not a substitute for the game runtime.