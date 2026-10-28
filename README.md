# [Xenogerm Planner](https://steamcommunity.com/sharedfiles/filedetails/?id=3781523927)

*Plan your xenogerms before you own every genepack.*

Xenogerm Planner lets you create plans for RimWorld's Biotech genetics system, choose the genes you want and track them as your colony collects genepacks. Plans are saved with your game independently from vanilla xenogerm templates.

## Features

- Create plans from scratch, existing xenogerm templates, premade xenotypes or saved custom xenotypes.
- Edit, duplicate, copy and paste plans.
- Track available and missing genes on the active map.
- Inspect exact genepacks, locate them on the map and find potential pawn donors.
- Compare product-level readiness with the readiness of a selected Gene Assembler.
- Inspect conflicts, prerequisites, complexity, metabolism, hunger rate and Archite Capsule requirements.
- Create vanilla xenogerm templates from ready plans.
- Receive optional plan-readiness notifications and advisory notifications about useful trader offers.

## Requirements and installation

- RimWorld 1.6 or 1.7.
- Biotech, loaded before Xenogerm Planner.
- No Harmony or separate UI framework mod required.

Subscribe on [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3781523927), enable **Xenogerm Planner** in the Mods menu and restart as requested.

For a manual installation, place the complete mod package in `RimWorld/Mods/XenogermPlanner/`, with `About/` directly inside that folder. When building from source, use [deployment](#deploying-a-development-build) to prepare a complete installation. The repository's `mod/` directory and GitHub source archives contain shared content and historical DLLs, but no current DLL.

To update manually, close RimWorld and replace the installed mod folder with the complete package. This prevents duplicate assemblies from an older layout. To remove the mod, disable it and restart, then delete the manual installation or unsubscribe. Read [Save compatibility](#save-compatibility) before changing an existing save.

## Usage

1. Load a colony and open the **Xenogerm Planner** main tab.
2. Choose **Create**, select a source, edit the genes, choose **All planned genes** or **Exact gene set**, and save the plan.
3. Review the plan's status and gene availability table.
4. Select a Gene Assembler when you want to check its connected inputs and live assembly requirements.
5. Use **Refresh** after changing inventory or infrastructure when you need an immediate update.

Hover over controls for explanations. Right-click a gene or genepack to inspect its native info card; click an available physical target to navigate to it.

## Planning modes and readiness

**All planned genes** requires every planned gene to be available. The genepacks may also contain extra genes.

**Exact gene set** requires a combination containing exactly the planned genes, without extras. A gene can be physically present but available only in packs containing unwanted genes; the Planner distinguishes this conflict from a missing gene.

Product readiness uses physical genepacks on the active map. Loose, stored and pawn-carried packs can count when their holders belong to the supported scope. Genepacks on another map, in a caravan or still offered by a trader do not count.

A selected Gene Assembler has a separate connected input scope and live requirements. On RimWorld 1.7, the check includes a minimum effective metabolism of `-5` for the complete candidate. A value above `+5` is not a metabolism blocker. In All planned genes mode, additional connected packs can compensate metabolism; Exact gene set still forbids genes outside the target.

On RimWorld 1.7, **Check incomplete** means the assembler check reached its search limit without determining whether assembly is possible. Product-level readiness and template generation remain separate from that assembler result.

## Notifications and integrations

Per-plan readiness notifications announce when a plan that was not ready becomes ready. The first check that can determine readiness does not send a notification. While readiness cannot be determined, no notification is sent. Repeated ready evaluations and enabling notifications for an already ready plan do not produce another message. Saving and loading the game alone do not repeat an already delivered readiness notification.

The Planner also monitors active-map orbital traders and visiting trader caravans. It can notify you about genepacks relevant to unfinished plans. Offers already present when you load a game or switch maps do not trigger notifications. Visiting-caravan notifications can navigate to the trader pawn; orbital notifications are text-only.

With [Settlement Trade Overview](https://steamcommunity.com/sharedfiles/filedetails/?id=3781528628), offered genepacks can show Planner relevance in its trade lists. Neither mod requires the other. Offers remain advisory until acquired and included in the active-map inventory.

Other mods can use the optional read-only [integration API version 1](docs/integration-api.md). Its version is independent from the mod version and game version.

## Save compatibility

The mod can be added to an existing save; the colony starts with no Planner-owned plans.

When moving a save from RimWorld 1.6 to 1.7, install the complete package so the game selects the matching DLL. Keep a copy of the original save while checking the migrated colony, plans and templates.

Removing the mod discards Planner-owned plans, planning modes and notification state when the save is rewritten without it. The first load without the mod can log missing-class and `GameComponent` deserialization errors. The colony remains accessible. Save under a new name without the mod, then reload that copy to remove the stale component entries and their errors. Vanilla xenogerm templates created through the Planner remain vanilla save data.

If a planned modded gene is temporarily unavailable, its definition name is preserved and the plan becomes degraded. Vanilla templates have different missing-definition behavior and may lose unresolved genes after resaving. See [Compatibility and known limitations](docs/compatibility.md).

## Compatibility

The Planner supports standard RimWorld/Biotech genes, genepacks, map holders, Gene Assembler facilities, Gene Extractor rules and xenogerm templates. Data-driven gene mods using those boundaries are expected to work. Mods that replace those systems or change their semantics may need separate compatibility work.

Planning covers the active map. Donors and trader offers provide acquisition hints; they do not satisfy readiness. The mod does not automate extraction, trading, assembly or implantation.

## Languages

- English
- Russian
- Ukrainian

## Development

Current sources and tests target RimWorld 1.7 and `net10.0`. The package also contains the released RimWorld 1.6 DLL, which is preserved unchanged and is not rebuilt from current sources.

### Prerequisites

- A local RimWorld 1.7 installation containing `Assembly-CSharp.dll` and `UnityStubs.dll`.
- A .NET 10 SDK, or a compatible SDK capable of targeting `net10.0`, and a .NET 10 runtime for running tests.
- [Escarval.RimWorld.UI v1.0.0](https://github.com/EscarvalOokt/Escarval.RimWorld.UI/releases/tag/v1.0.0) source, obtained as described below.

The UI source must include `src/Escarval.RimWorld.UI/Escarval.RimWorld.UI.Source.projitems`. The production project imports that manifest and compiles its source into `XenogermPlanner.dll`, excluding the library's `bin/` and `obj/` directories. No standalone UI build, UI local configuration or extra runtime DLL is required. Xenogerm Planner supplies the game references and compiler settings.

### Shared UI source

Download **Source code (zip)** or **Source code (tar.gz)** from the [v1.0.0 release](https://github.com/EscarvalOokt/Escarval.RimWorld.UI/releases/tag/v1.0.0) and extract the complete archive to a directory that will remain available during Xenogerm Planner builds. The extracted source root is `Escarval.RimWorld.UI-1.0.0`; it contains the required file at `src/Escarval.RimWorld.UI/Escarval.RimWorld.UI.Source.projitems`.

For example, if the source root is `C:\Dependencies\Escarval.RimWorld.UI-1.0.0`, set this property in `src/XenogermPlanner.Local.props` after copying the [configuration example](docs/XenogermPlanner.Local.props.example):

```xml
<EscarvalRimWorldUiProjectDir>C:\Dependencies\Escarval.RimWorld.UI-1.0.0\src\Escarval.RimWorld.UI</EscarvalRimWorldUiProjectDir>
```

As an alternative to an archive, run this command from the Xenogerm Planner repository root to clone the same release tag into a new sibling directory:

```text
git clone --branch v1.0.0 --depth 1 https://github.com/EscarvalOokt/Escarval.RimWorld.UI.git ../Escarval.RimWorld.UI
```

Use the release tag for this setup. Xenogerm Planner compiles the files currently present at `EscarvalRimWorldUiProjectDir`; the build does not download the library or enforce its version. For the library's integration contract and API, see the versioned [Consumer Integration](https://github.com/EscarvalOokt/Escarval.RimWorld.UI/blob/v1.0.0/docs/integration.md) and [Public API Reference](https://github.com/EscarvalOokt/Escarval.RimWorld.UI/blob/v1.0.0/docs/api.md).

### Local configuration

Copy [the configuration example](docs/XenogermPlanner.Local.props.example) to `src/XenogermPlanner.Local.props`, which is ignored by Git.

- Set `RimWorldManagedDir` to the RimWorld 1.7 directory containing the two game DLLs. On Windows, this is the game installation root.
- Set `EscarvalRimWorldUiProjectDir` to the absolute path of the directory containing `Escarval.RimWorld.UI.Source.projitems`, as in the archive example above. Specify the inner `src/Escarval.RimWorld.UI/` directory, not the source root or the `.projitems` file itself. For a checkout named `Escarval.RimWorld.UI`, the build can also discover `../Escarval.RimWorld.UI/src/Escarval.RimWorld.UI/` or `../../Escarval.RimWorld.UI/src/Escarval.RimWorld.UI/`, relative to this repository root. The versioned archive folder is not discovered automatically.
- Leave `DeployToGame=false` for ordinary development. Configure `RimWorldModAssembliesDir` only for explicit deployment; it must identify the intended development installation's `Current/Assemblies/` directory.

### Build

From the repository root:

```text
dotnet build src/XenogermPlanner.sln -c Release
```

The current DLL is written under `src/XenogermPlanner/bin/Release/`. Ordinary builds and tests leave `mod/` and the game installation unchanged. The repository contains no generated current assembly directory.

### Deploying a development build

Set `RimWorldModAssembliesDir` to the absolute destination `<mod directory>/Current/Assemblies/`, using this casing. Close RimWorld before updating its installed mod, then run:

```text
dotnet build src/XenogermPlanner.sln -c Release -p:DeployToGame=true
```

Deployment creates or updates the complete mod in the directory above `Current`. It copies shared content and the preserved historical DLLs from `mod/`, writes the built `XenogermPlanner.dll` to `Current/Assemblies/`, and installs `LoadFolders.xml` with the matching version selection:

```text
XenogermPlanner/
  About/
  LoadFolders.xml
  Defs/, Languages/, Textures/   Shared content
  1.6/Assemblies/XenogermPlanner.dll   Preserved RimWorld 1.6 assembly
  Current/Assemblies/XenogermPlanner.dll   Current build for RimWorld 1.7
```

The deployment rules are in [ModDeployment.targets](build/ModDeployment.targets). Before changing the destination, they validate its location, an existing package ID, the current load-folder mapping and the historical DLL checksums. A destination inside the source repository, its ancestors, the game root, another mod, or a path through symbolic links/junctions is rejected.

Deployment replaces the managed `About`, `Defs`, `Patches`, `Languages`, `Textures`, `Sounds`, `Resources`, `Common`, historical-version and `Current` directories. It also removes obsolete root `Assemblies/` and the numbered directory of the current game version (`1.7/` today). Files outside these directories are preserved; keep personal files there. A local `About/PublishedFileId.txt` is preserved when the source package does not supply it.

Use the deployed directory as the installable package. For a release archive, deploy to a clean external directory by overriding `-p:RimWorldModAssembliesDir=<absolute staging directory>/Current/Assemblies/`, then archive the resulting mod directory. A clean destination avoids including unrelated files preserved from a development installation. Game/reference DLLs, test assemblies and a standalone shared UI DLL are not copied.

`LoadFolders.xml` selects the shared root plus `1.6` for RimWorld 1.6, or the shared root plus `Current` for 1.7. When a future game version needs a different DLL, preserve the last compatible DLL in its numbered directory under `mod/`, add its `HistoricalModVersion` entry and SHA-256 in the production project, update `CurrentRimWorldVersion`, and update `LoadFolders.xml` and `About.xml` so the new version uses `Current`. Historical DLLs are never rebuilt from current source.

### Tests

```text
dotnet test tests/XenogermPlanner.Tests/XenogermPlanner.Tests.csproj -c Release
```

Tests build the production project using the same local configuration. Leave deployment disabled. The suite runs on .NET 10 outside the full game and protects project-owned semantics; actual saves, native UI and runtime integrations require the procedures in [Testing policy](docs/testing.md).

Generic UI layout and control tests belong to the UI library. Planner tests retain the shared-source assembly contract and Planner-specific integration behavior.

## Technical documentation

- [Architecture](docs/architecture.md)
- [Testing policy](docs/testing.md)
- [Compatibility and known limitations](docs/compatibility.md)
- [Integration API](docs/integration-api.md)