# Xenogerm Planner testing policy

This document defines automated boundaries and reproducible acceptance scenarios for the current RimWorld 1.7 source and the complete 1.6/1.7 package. Build commands are in [README](../README.md#development), implementation contracts in [Architecture](architecture.md), and player-facing limitations in [Compatibility](compatibility.md).

## Testing principle

Protect meaningful project-owned behavior: target semantics, readiness decisions, persisted data, state transitions, deterministic search, failure isolation and presentation policies. Use the smallest existing seam that reproduces a realistic regression.

Do not reproduce RimWorld, Scribe, the engine or a third-party mod in a test host. Inspect external APIs when adapting an integration, and use the game for actual Def initialization, save/load, native input/rendering and gameplay lifecycles. Test counts and broad method coverage are not acceptance goals.

<a id="automated-boundaries"></a>

## Automated test boundary

The [NUnit project](../tests/XenogermPlanner.Tests/XenogermPlanner.Tests.csproj) runs on .NET 10 outside the complete game and references the production project.

| Area | Contracts to protect |
| --- | --- |
| Plans and names | Stable IDs, distinct physical targets, conflicting genes retained, Trim/OrdinalIgnoreCase uniqueness, rejection of manual conflicts, deterministic duplicate/source/paste/restore naming. |
| Persistence | Save-record round trips, unresolved requirements, invalid-record isolation, notification settings/cursors, defaults for absent optional fields and duplicate-name normalization. |
| Clipboard | Versioned round trips, new identity on paste, unresolved definition names, excluded source identity/notification state, malformed data and unsupported versions. |
| Product readiness | Coverage/Exact set semantics, special-state precedence, reference identity, composition grouping, Missing versus ExactPayloadConflict and deterministic ordering. |
| Inventory | Current-map/faction/holder policy, physical deduplication, unavailable state, invalidation, map changes, snapshot reuse and 600-tick fallback. |
| Assembler scope | Exact connected packs, facility identity/power, stable representative choice and independent product versus connected scope. |
| Assembler search | Requirement and helper combinations, full prerequisites, Coverage extras, Exact exclusion, duplicate composition handling, deterministic results and complete candidate evaluation. |
| Metabolism | Distinct effective physical payload, boundary values below/at/above -5, values above +5, overrides, duplicate occurrences, helper packs and unmet helper prerequisites. Target-only or raw-sum calculations must not determine the result. |
| Search budget | One shared 10,000-state budget across base/helper DFS; a ready last-permitted state succeeds; only refusal of an unvisited state marks exhaustion; exhaustion without Ready yields SearchIncomplete without final blocked-candidate diagnostics. |
| Assembler result | Every live blocker, deterministic blocked-candidate selection, SearchIncomplete distinct from Blocked/NotReady, and no assembler status leaking into product readiness, templates or API relevance. |
| Window cache | Structural live-input equality, copied gene sequences, bounded 30-tick polling, unchanged-input reuse, immediate plan/mode/map/assembler invalidation and explicit Refresh. |
| Target/donor analysis | Ordinary/random/mixed conflicts, prerequisites, extraction participation, spawned current-map gene-tracker scope and transient exact pawn references. |
| Templates | Valid fallback, bounded search, irredundancy/deduplication, deterministic scoring, complete/incomplete alternatives, current physical re-resolution and safe failed saves. |
| Sources | Template/xenotype transformation, distinct-gene editor state, lazy saved-file loading, unchanged-file reuse, changed-file reload, source revalidation and isolated failures. |
| Readiness notifications | Silent first determinate baseline, Unavailable handling, non-ready→ready transition, re-arming, disabled-delivery cursor updates and independent failures. |
| Trader advisory | 60-tick polling, source/reference identity, silent load/map baselines, lifetime deduplication, pending delivery, CanTradeNow gating, aggregation, relevance reuse and subsystem isolation. |
| API version 1 | Public reflection shape, DTO invariants, enum values, validation, batch correspondence, per-item isolation, ordering, optional context and read-only semantics. Derived snapshot refresh is allowed; physical inventory, plans and notification cursors are not mutated by a query. |
| Presentation | Diagnostic precedence, numeric formatting, applicability, meaningful sort order, safe fallbacks and cache keys. |
| Source composition | Shared UI types belong to XenogermPlanner.dll; no standalone UI or DTO runtime assembly is required. |

Generic fixed/variable-height layout, control and layout-cache tests belong to `Escarval.RimWorld.UI.Tests`. Planner owns its composition/integration tests and the in-game rendering scenarios below.

For helper-search regressions, include cases where a zero/negative-metabolism gene improves the effective result through an override, where a compensating pack introduces another prerequisite, and where reaching -5 still leaves a blocker removable by another pack. Do not assume metabolism, complexity or prerequisites improve monotonically as packs are added.

An exhausted search must not be tested as proof of impossibility. Keep the no-Ready/completed case separate from the no-Ready/incomplete case.

## Build and package checks

Use isolated output and deployment directories for build-target checks. Deployment to a clean external directory prepares the complete candidate; the source `mod/` directory has no current DLL.

1. Build and test with deployment disabled. Verify that neither the game installation nor `mod/` receives output.
2. Resolve UI source from a v1.0.0 archive or tagged checkout. Check the configured path and both supported automatic layouts. A missing manifest must produce a useful configuration error.
3. Inspect compile items: include the manifest's shared source once, exclude its bin/obj output, and retain Planner-owned source. A standalone UI build must not be needed.
4. Deploy with `DeployToGame=true` to an absolute external `Current/Assemblies/` destination. Compare all installed source-content and historical DLL hashes, and compare the current DLL to the build output. Verify that no current directory is created in source `mod/`.
5. Repeat deployment after adding stale files to managed directories and obsolete root `Assemblies/` and `1.7/` directories. They must disappear; unrelated root files and a local source-absent `About/PublishedFileId.txt` must survive. Reject missing, relative, historical or wrongly cased destinations, source-tree/ancestor/game-root destinations, another installed package ID, symbolic links/junctions, invalid current load mapping and missing/changed historical DLLs before modifying the destination.
6. Parse metadata, Defs, translations and `LoadFolders.xml`; check keys, parameters, main-button DefInjected placement and texture references. Inspect package contents for game/reference DLLs, test DLLs, a standalone UI DLL and generated build metadata.
7. Check actual assembly selection in both game versions using R01.

## Runtime acceptance setup

Record the candidate package path, both DLL hashes, source and shared-source revisions, game version/revision, OS, DLC/mod list and load order, language, resolution and UI scale. Confirm the installed assembly matches the candidate.

Use separate configurations for clean Biotech, representative data-driven gene mods, optional Settlement Trade Overview, multiple maps/world layers, and deliberately malformed/incompatible fixtures. Record a missing prerequisite as not run instead of treating an unavailable scenario as passed.

Use disposable save copies. Preserve starting saves and record map identities, plan IDs, relevant pawns/packs/facilities, gene definitions and game ticks. Separate first-load logs from logs after resaving. Keep diagnostics and instrumentation in an isolated candidate and remove them before final package checks.

Internal assertions require observation: use breakpoints/watches or temporary counters for scans, analysis calls, state visits, cache identities, delivery acknowledgements and actual selected assembly paths. A visual result alone does not establish those counts.

## Runtime scenarios

| ID | Setup | Action | Expected result |
| --- | --- | --- | --- |
| R01 | Same complete package in clean separate RimWorld 1.6 and 1.7 installations; Biotech enabled. | Start each game, inspect selected assembly path/hash, open Planner and perform a short create/edit/save-load check. | One Planner DLL loads: preserved DLL from `1.6/Assemblies/` or current DLL from `Current/Assemblies/`. Shared Defs, translations and textures resolve. No root-level, game/reference, test or standalone UI DLL is loaded from the mod. |
| R02 | Clean 1.7 Biotech colony without Planner-owned plans. | Open Planner, create from scratch, change mode/notification setting and save. | A new independent plan appears; the editor exposes the full runtime gene catalog and preserves chosen settings. |
| R03 | Runtime xenogerm template, premade xenotype and saved custom xenotype; separate missing/invalid saved file. | Open each source, preview, cancel, continue, edit and save. Change/remove a source between selection and Continue. | Cancel creates no plan. Continue opens an editor with default Coverage/notifications; saving creates one independent plan. Source changes revalidate or fail safely without damaging other plans. |
| R04 | Plans with colliding names, conflicting genes and unavailable definition names; valid and malformed clipboard text. | Rename, duplicate, copy/paste and save/reload. | Manual duplicates are rejected; automatic names are deterministic. IDs remain independent, conflicting genes remain targets, unresolved names stay degraded, malformed paste does not corrupt the collection. |
| R05 | Loose, Gene Bank, nested and pawn-carried packs; foreign-faction, ship, caravan, other-map and detached cases. | Open Planner and Refresh; move packs between supported and excluded scopes. | Only eligible current-map physical packs count. Forbidden state, bank power and assembler connection alone do not change product membership. |
| R06 | Stable plan/inventory; two maps or layers and controlled tick progression. | Reopen, Refresh, change the active map and advance through the inventory fallback interval. Observe scan/snapshot identity. | Opening/Refresh invalidates, map changes replace scope, fallback occurs at 600 ticks, and stable cached data is reused between refreshes. No previous-map packs remain authoritative. |
| R07 | Coverage and Exact plans with ready, missing, extra-gene-only and mixed compatible/incompatible source compositions. | Switch modes and inspect availability, status and tooltips. | Product readiness follows the selected payload relation. ExactPayloadConflict remains distinct from Missing; compatible sources restore Available. No assembler infrastructure requirement changes product readiness. |
| R08 | Selected assembler with overlapping facilities, powered/unpowered banks, available/unavailable research and capsule counts. | Change one live condition at a time, destroy/remove the selected assembler and switch maps. | Connected scope and candidate-specific blockers match current infrastructure; selection clears safely and is not silently replaced. Product readiness remains independent. |
| R09 | Controlled candidate fixtures with effective metabolism -6, -5 and above +5; duplicate and overriding genes. | Compare the 1.7 native assembly constraint with Planner's selected-assembler result and diagnostic. | MetabolismTooLow applies only below -5 and reports the full candidate's effective value/minimum. Duplicates are not counted twice for metabolism; +5 is not an upper rejection threshold. |
| R10 | Coverage target requiring separate compensating packs, with zero/negative raw metabolism overrides, helper prerequisites and an alternative fixing another live blocker. | Select the assembler and compare supported combinations; repeat in Exact mode. | Coverage can find a fully usable extended candidate without accepting unmet helper requirements. Exact rejects extras. Passing metabolism alone is not Ready. |
| R11 | Isolated large candidate fixture capable of exhausting the 10,000-state search; a separate completed blocked fixture and a ready-at-limit fixture. | Observe state visits, result and displayed diagnostics; press Refresh. | Unresolved exhaustion shows Check incomplete without asserted final blockers or candidate. Completed search may show Blocked; a ready last-permitted state is Ready. Refresh reruns the bounded analysis; there is no background continuation. |
| R12 | Selected assembler and unchanged live inputs; diagnostic scan/analysis counters. | Leave the assembler view open through several 30-tick intervals, then change pack/facility/power/gene sequence/scalars, plan mode and selection one at a time. Finally Refresh. | Polling can scan live state; expensive analysis is reused when structural inputs match and reruns for relevant changes. Copied gene data detects in-place mutation. Explicit Refresh invalidates immediately. |
| R13 | Modded gene conflicts/prerequisites, an unchanged degraded plan and several target edits. | Inspect Gene effects and editor totals; compare native effective-gene projection and metabolism-to-hunger conversion. | Diagnostics do not silently normalize the physical target. Degraded totals are partial; edits recalculate. Raw composition and effective total values are not conflated. |
| R14 | Spawned gene-bearing pawns with varying faction/status and extractor eligibility; a stale donor target. | Inspect donor counts, open details, navigate, change genes/spawn state and remove a donor. | Counts reflect extraction participation, not usable extractor infrastructure or guaranteed output. Exact rows navigate safely; stale references become no-ops. |
| R15 | Product-ready Coverage/Exact plans, small complete and large incomplete template candidate spaces. | Generate a template, inspect feedback/preview, select an alternative and save; remove a required physical composition before another save. | Feedback renders before search, candidates are valid, incomplete alternatives are labelled, and stale compositions fail safely. Native same-name replacement works without changing the source plan or starting assembly. |
| R16 | A plan crossing non-ready→ready, multiple plans and disabled notifications; disposable save. | Change inventory, toggle notification settings and save/reload. | First determinate evaluation is silent; only re-armed non-ready→ready transitions deliver. Unavailable does not advance the cursor, disabled delivery still tracks state, and the persisted cursor prevents duplicate ready messages. |
| R17 | Orbital trader and visiting trade caravan on the active map; controlled relevant offers. | Let traders arrive/depart, perform an actual trade, change plans and switch away/back. Observe stock polls and relevance. | 60-tick polling discovers current stock and releases departed sources. Plan/inventory changes can reevaluate known offers. Load/map baselines are silent and offers never satisfy physical readiness. |
| R18 | New post-baseline relevant offer while CanTradeNow is false; multiple offers and plans. | Make trade available, lose/regain relevance, rename a plan, then introduce a new physical pack or plan ID. | Pending current pairs deliver once when allowed, aggregated per trader. Acknowledged pairs stay suppressed within that source lifetime; new identities can deliver. Caravan messages navigate; orbital messages remain text-only. |
| R19 | Isolated failing trader/source/provider or unavailable integration context alongside healthy plans/sources. | Exercise refresh and notification delivery. | One failure is isolated. Other plans, sources, persistence and the Planner UI remain usable. |
| R20 | Compatible STO on 1.7, then Planner absent or an incompatible consumer fixture; known Coverage/Exact/ready/degraded plans. | Query/open both trade views, mutate plans and reopen; inspect API results. | API v1 preserves index/status/order contracts and excludes ready/degraded/prerequisite-only matches. Optional failure remains neutral. Queries may refresh derived inventory but do not mutate plans, physical packs or delivery cursors. |
| R21 | All Planner windows, long gene/plan lists, empty results, different UI scales and each [supplied language](../README.md#languages). | Search, sort, collapse groups, scroll, remove final visible selected genes, hover and use left/right clicks, open/close modal dialogs. | No stale index, unbalanced scroll state, stuck modal or raw translation key. Icons, clipping, tooltips and hit targets align; modal pause/input ownership and native window chrome work. |
| R22 | Exact loose/held packs and genes, plus targets destroyed while a context menu is open. | Open native info cards and navigate from the same surfaces. | Inspection uses the exact gene/pack; navigation uses the current-map holder. Right-click does not trigger left-click actions; stale targets are safe. |
| R23 | Save made without Planner and save with multiple plans, cursor state, degraded requirements and a vanilla generated template. | Add Planner to the first; remove it from a copy of the second, load, save under a new name and reload. | Addition initializes empty Planner state. Removal leaves the colony accessible with first-load component errors; resave removes Planner data/errors. Planner plans are lost; vanilla templates remain vanilla data. |
| R24 | Disposable 1.6 save with known plan IDs/names/modes/cursors, generated templates and modded gene requirements. | Load on 1.7 with the complete package, inspect data, save under a new name and reload. | Planner-owned fields and available definitions retain their meaning; unresolved requirements remain degraded. Derived state is rebuilt. No Planner-specific corruption or duplicate components. Record other mods' migration failures separately. |
| R25 | Plan referencing a modded gene and a vanilla template containing it; disposable save branches. | Remove the definition provider, load/resave, then restore it and reload each branch. | Planner preserves unresolved names for later resolution. Do not require vanilla template recovery after unresolved entries were removed; verify the documented distinction without reconstruction from labels. |
| R26 | Representative normal and large saves with fixed camera, UI scale and profiling method. | Measure initial inventory capture, each active tab, plan switching, source search, template generation and background trader polling separately. | Steady UI reuses stable analysis/projections, list drawing is virtualized and callbacks follow bounded refresh lifecycles. Record synchronous capture/search hitches separately from continuous rendering cost. |

<a id="candidate-validation"></a>

## Release candidate validation

A release candidate is the complete package, not just a build output. Identify both versioned DLLs, inspect shared content and install into clean locations. Run R01 for both supported versions and the applicable 1.7 cases above.

Record observed behavior, diagnostics, failures and unavailable fixtures separately. Retain a failing case's starting save and exact actions. After fixes, rerun affected cases and identify the final installed candidate. No source-level or automated result substitutes for actual runtime acceptance.

## Stress and performance checks

Keep deterministic stress tests in the ordinary suite when they protect search correctness, data-shape handling, determinism or cache contracts. Explicit performance fixtures are separate diagnostic runs.

From the repository root:

```text
dotnet test tests/XenogermPlanner.Tests/XenogermPlanner.Tests.csproj -c Release --settings performance.runsettings
```

The [run settings](../performance.runsettings) select the readiness, relevance, assembler candidate, Gene Coverage projection and effective-biostat fixtures with NUnit workers disabled. Other explicit fixtures may require separate selection.

Use Release builds, consistent workloads and several independent process runs when comparing substantive changes. Warm up before measurement. Keep timing and current-thread managed-allocation passes separate; report median duration, supporting P95, managed allocated bytes and supporting Gen 0 counts. Treat Max and higher-generation collections as diagnostics. Verify semantic correctness of measured results.

Compare only identical workloads across revisions/runtime environments. Record changed game references, runtime, fixtures and hardware instead of presenting unlike runs as an algorithm-only comparison. Do not impose workstation-specific pass/fail timing limits without a defined requirement.

## Adding tests

Add a focused regression when supplied inputs can reproduce a wrong decision, lost requirement, invalid candidate, stale cache, incorrect delivery or broken public contract. Use runtime cases for engine-dependent behavior.

Do not extract production abstractions solely to test literal labels, colors, spacing or direct API forwarding. Select invalid-input cases where invalid data can realistically cross a public, persistence or integration boundary. Update procedures when their observable contracts change.