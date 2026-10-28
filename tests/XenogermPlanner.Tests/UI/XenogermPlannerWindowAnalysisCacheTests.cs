using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Assemblers;
using XenogermPlanner.Donors;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;
using XenogermPlanner.UI;

namespace XenogermPlanner.Tests.UI
{
    [TestFixture]
    public sealed class XenogermPlannerWindowAnalysisCacheTests
    {
        [Test]
        public void ProductReadiness_ReusesResultForStableInputs()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();

            PlanReadinessResult first = cache.GetProductReadiness(fixture.Plan, fixture.InventorySnapshot);
            PlanReadinessResult second = cache.GetProductReadiness(fixture.Plan, fixture.InventorySnapshot);

            Assert.That(second, Is.SameAs(first));
            Assert.That(fixture.ProductReadinessCalls, Is.EqualTo(1));
        }

        [Test]
        public void ProductReadiness_RebuildsForInventoryGenesAndReadinessModeChanges()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();

            cache.GetProductReadiness(fixture.Plan, fixture.InventorySnapshot);

            var replacementInventory = PlanGenepackInventorySnapshot.CreateAvailable([]);
            cache.GetProductReadiness(fixture.Plan, replacementInventory);

            fixture.Plan.ReplaceDesiredGenes([fixture.Gene]);
            cache.GetProductReadiness(fixture.Plan, replacementInventory);

            fixture.Plan.ChangeReadinessMode(PlanReadinessMode.ExactPayload);
            cache.GetProductReadiness(fixture.Plan, replacementInventory);

            Assert.That(fixture.ProductReadinessCalls, Is.EqualTo(4));
        }

        [Test]
        public void ProductReadiness_IgnoresNameAndNotificationChanges()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();

            PlanReadinessResult first = cache.GetProductReadiness(fixture.Plan, fixture.InventorySnapshot);
            fixture.Plan.Rename("Renamed");
            fixture.Plan.ChangeReadinessNotificationsEnabled(false);
            PlanReadinessResult second = cache.GetProductReadiness(fixture.Plan, fixture.InventorySnapshot);

            Assert.That(second, Is.SameAs(first));
            Assert.That(fixture.ProductReadinessCalls, Is.EqualTo(1));
        }

        [Test]
        public void TargetAnalysis_DependsOnlyOnDesiredGeneCollectionIdentity()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();

            PlanGeneTargetAnalysisResult first = cache.GetTargetAnalysis(fixture.Plan);
            fixture.Plan.Rename("Renamed");
            fixture.Plan.ChangeReadinessMode(PlanReadinessMode.ExactPayload);
            PlanGeneTargetAnalysisResult second = cache.GetTargetAnalysis(fixture.Plan);

            fixture.Plan.ReplaceDesiredGenes([fixture.Gene]);
            PlanGeneTargetAnalysisResult rebuilt = cache.GetTargetAnalysis(fixture.Plan);

            Assert.That(second, Is.SameAs(first));
            Assert.That(rebuilt, Is.Not.SameAs(first));
            Assert.That(fixture.TargetAnalysisCalls, Is.EqualTo(2));
        }

        [Test]
        public void PotentialDonors_DoesNotScanWithoutMissingGenes()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();
            var ready = PlanReadinessResult.CreateReady([fixture.Gene]);

            PlanPotentialDonorAnalysisResult result = cache.GetPotentialDonorAnalysis(ready, fixture.Map);

            Assert.That(result, Is.Null);
            Assert.That(fixture.PotentialDonorScopeCalls, Is.Zero);
            Assert.That(fixture.PotentialDonorAnalysisCalls, Is.Zero);
        }

        [Test]
        public void PotentialDonors_RefreshesByIntervalAndImmediatelyForInputChanges()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();

            PlanPotentialDonorAnalysisResult first = cache.GetPotentialDonorAnalysis(
                fixture.ReadinessResult,
                fixture.Map);
            fixture.CurrentTick = XenogermPlannerWindowAnalysisCache.LiveStateRefreshIntervalTicks - 1;
            PlanPotentialDonorAnalysisResult cached = cache.GetPotentialDonorAnalysis(
                fixture.ReadinessResult,
                fixture.Map);

            fixture.CurrentTick = XenogermPlannerWindowAnalysisCache.LiveStateRefreshIntervalTicks;
            cache.GetPotentialDonorAnalysis(fixture.ReadinessResult, fixture.Map);

            var replacementReadiness = PlanReadinessResult.CreateNotReady(
                [],
                [fixture.Gene],
                hasExactPayloadConflict: false);
            cache.GetPotentialDonorAnalysis(replacementReadiness, fixture.Map);

            Map replacementMap = CreateUninitialized<Map>();
            cache.GetPotentialDonorAnalysis(replacementReadiness, replacementMap);

            Assert.That(cached, Is.SameAs(first));
            Assert.That(fixture.PotentialDonorScopeCalls, Is.EqualTo(4));
            Assert.That(fixture.PotentialDonorAnalysisCalls, Is.EqualTo(4));
        }

        [Test]
        public void SelectableAssemblers_RefreshesByIntervalAndMapIdentity()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();

            IReadOnlyList<Building_GeneAssembler> first = cache.GetSelectableAssemblers(fixture.Map);
            IReadOnlyList<Building_GeneAssembler> cached = cache.GetSelectableAssemblers(fixture.Map);

            fixture.CurrentTick = XenogermPlannerWindowAnalysisCache.LiveStateRefreshIntervalTicks;
            cache.GetSelectableAssemblers(fixture.Map);

            cache.GetSelectableAssemblers(CreateUninitialized<Map>());

            Assert.That(cached, Is.SameAs(first));
            Assert.That(fixture.SelectableAssemblerCalls, Is.EqualTo(3));
        }

        [Test]
        public void AssemblerAnalysis_RescansByIntervalAndReanalyzesChangedRequests()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();

            XenogermPlannerWindowAssemblerAnalysis first = cache.GetAssemblerAnalysis(
                fixture.Plan,
                fixture.Assembler,
                fixture.Map);
            XenogermPlannerWindowAssemblerAnalysis cached = cache.GetAssemblerAnalysis(
                fixture.Plan,
                fixture.Assembler,
                fixture.Map);

            fixture.CurrentTick = XenogermPlannerWindowAnalysisCache.LiveStateRefreshIntervalTicks;
            cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler, fixture.Map);

            Building_GeneAssembler replacementAssembler = CreateUninitialized<Building_GeneAssembler>();
            cache.GetAssemblerAnalysis(fixture.Plan, replacementAssembler, fixture.Map);

            fixture.Plan.ReplaceDesiredGenes([fixture.Gene]);
            cache.GetAssemblerAnalysis(fixture.Plan, replacementAssembler, fixture.Map);

            fixture.Plan.ChangeReadinessMode(PlanReadinessMode.ExactPayload);
            cache.GetAssemblerAnalysis(fixture.Plan, replacementAssembler, fixture.Map);

            Assert.That(cached, Is.SameAs(first));
            Assert.That(fixture.AssemblerLiveStateCalls, Is.EqualTo(5));
            Assert.That(fixture.AssemblerReadinessCalls, Is.EqualTo(4));
        }

        [Test]
        public void LiveStateCaches_RefreshWhenTickMovesBackwards()
        {
            var fixture = new CacheFixture { CurrentTick = 100 };
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();

            cache.GetPotentialDonorAnalysis(fixture.ReadinessResult, fixture.Map);
            cache.GetSelectableAssemblers(fixture.Map);
            cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler, fixture.Map);

            fixture.CurrentTick = 10;

            cache.GetPotentialDonorAnalysis(fixture.ReadinessResult, fixture.Map);
            cache.GetSelectableAssemblers(fixture.Map);
            cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler, fixture.Map);

            Assert.That(fixture.PotentialDonorScopeCalls, Is.EqualTo(2));
            Assert.That(fixture.SelectableAssemblerCalls, Is.EqualTo(2));
            Assert.That(fixture.AssemblerLiveStateCalls, Is.EqualTo(2));
            Assert.That(fixture.AssemblerReadinessCalls, Is.EqualTo(1));
        }

        [Test]
        public void Invalidate_ForcesAllCachedResultsToRebuild()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();

            cache.GetProductReadiness(fixture.Plan, fixture.InventorySnapshot);
            cache.GetTargetAnalysis(fixture.Plan);
            cache.GetPotentialDonorAnalysis(fixture.ReadinessResult, fixture.Map);
            cache.GetSelectableAssemblers(fixture.Map);
            cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler, fixture.Map);

            cache.Invalidate();

            cache.GetProductReadiness(fixture.Plan, fixture.InventorySnapshot);
            cache.GetTargetAnalysis(fixture.Plan);
            cache.GetPotentialDonorAnalysis(fixture.ReadinessResult, fixture.Map);
            cache.GetSelectableAssemblers(fixture.Map);
            cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler, fixture.Map);

            Assert.That(fixture.ProductReadinessCalls, Is.EqualTo(2));
            Assert.That(fixture.TargetAnalysisCalls, Is.EqualTo(2));
            Assert.That(fixture.PotentialDonorScopeCalls, Is.EqualTo(2));
            Assert.That(fixture.SelectableAssemblerCalls, Is.EqualTo(2));
            Assert.That(fixture.AssemblerLiveStateCalls, Is.EqualTo(2));
            Assert.That(fixture.AssemblerReadinessCalls, Is.EqualTo(2));
        }

        [Test]
        public void AssemblerAnalysis_ReusesIncompleteResultForEquivalentNewSnapshots()
        {
            var fixture = new CacheFixture();
            Genepack firstPack = CreateUninitialized<Genepack>();
            Genepack secondPack = CreateUninitialized<Genepack>();
            ThingWithComps facility = CreateUninitialized<ThingWithComps>();
            fixture.PackGenes.Add(firstPack, [fixture.Gene]);
            fixture.PackGenes.Add(secondPack, [fixture.Gene]);
            fixture.AssemblerLiveState = CreateLiveState(
                new PlanAssemblerGenepackSource(firstPack, facility, true),
                new PlanAssemblerGenepackSource(secondPack, facility, true));
            fixture.AssemblerReadinessResult = PlanAssemblerReadinessResult.CreateSearchIncomplete(
                PlanReadinessResult.CreateReady([fixture.Gene]),
                2);
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();
            XenogermPlannerWindowAssemblerAnalysis first = cache.GetAssemblerAnalysis(
                fixture.Plan,
                fixture.Assembler,
                fixture.Map);

            fixture.CurrentTick = XenogermPlannerWindowAnalysisCache.LiveStateRefreshIntervalTicks;
            fixture.AssemblerLiveState = CreateLiveState(
                new PlanAssemblerGenepackSource(secondPack, facility, true),
                new PlanAssemblerGenepackSource(firstPack, facility, true));
            fixture.PackGenes[firstPack] = [fixture.Gene];
            XenogermPlannerWindowAssemblerAnalysis refreshed = cache.GetAssemblerAnalysis(
                fixture.Plan,
                fixture.Assembler,
                fixture.Map);
            fixture.CurrentTick++;
            cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler, fixture.Map);

            Assert.That(refreshed, Is.SameAs(first));
            Assert.That(refreshed.ReadinessResult.Status, Is.EqualTo(PlanAssemblerReadinessStatus.SearchIncomplete));
            Assert.That(fixture.AssemblerLiveStateCalls, Is.EqualTo(2));
            Assert.That(fixture.AssemblerReadinessCalls, Is.EqualTo(1));
        }

        [TestCase("AssemblerPower")]
        [TestCase("Complexity")]
        [TestCase("Capsules")]
        [TestCase("Research")]
        [TestCase("PhysicalPack")]
        [TestCase("Facility")]
        [TestCase("FacilityPower")]
        [TestCase("AddedPack")]
        [TestCase("RemovedPack")]
        [TestCase("ReplacedGene")]
        [TestCase("AddedGene")]
        [TestCase("RemovedGene")]
        [TestCase("GeneOrder")]
        public void AssemblerAnalysis_ReanalyzesChangedSnapshotInput(string changedInput)
        {
            var fixture = new CacheFixture();
            Genepack pack = CreateUninitialized<Genepack>();
            ThingWithComps facility = CreateUninitialized<ThingWithComps>();
            var secondGene = new GeneDef { defName = "Second" };
            var genes = new List<GeneDef> { fixture.Gene, secondGene };
            fixture.PackGenes.Add(pack, genes);
            var sources = new List<PlanAssemblerGenepackSource>
            {
                new PlanAssemblerGenepackSource(pack, facility, true)
            };
            fixture.AssemblerLiveState = CreateLiveState(sources.ToArray());
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();
            XenogermPlannerWindowAssemblerAnalysis first = cache.GetAssemblerAnalysis(
                fixture.Plan,
                fixture.Assembler,
                fixture.Map);

            var powered = true;
            var complexity = 10;
            var capsules = 2;
            var researchFinished = true;
            switch (changedInput)
            {
                case "AssemblerPower": powered = false; break;
                case "Complexity": complexity++; break;
                case "Capsules": capsules++; break;
                case "Research": researchFinished = false; break;
                case "PhysicalPack":
                    Genepack replacement = CreateUninitialized<Genepack>();
                    fixture.PackGenes.Add(replacement, [..genes]);
                    sources[0] = new PlanAssemblerGenepackSource(replacement, facility, true);
                    break;
                case "Facility":
                    sources[0] = new PlanAssemblerGenepackSource(pack, CreateUninitialized<ThingWithComps>(), true);
                    break;
                case "FacilityPower":
                    sources[0] = new PlanAssemblerGenepackSource(pack, facility, false);
                    break;
                case "AddedPack":
                    Genepack additional = CreateUninitialized<Genepack>();
                    fixture.PackGenes.Add(additional, [..genes]);
                    sources.Add(new PlanAssemblerGenepackSource(additional, facility, true));
                    break;
                case "RemovedPack": sources.Clear(); break;
                case "ReplacedGene": genes[0] = new GeneDef { defName = "Replacement" }; break;
                case "AddedGene": genes.Add(new GeneDef { defName = "Additional" }); break;
                case "RemovedGene": genes.RemoveAt(0); break;
                case "GeneOrder": genes.Reverse(); break;
                default: throw new ArgumentOutOfRangeException(nameof(changedInput));
            }

            fixture.AssemblerLiveState = new PlanAssemblerLiveState(
                new PlanAssemblerScopeSnapshot(sources),
                powered,
                complexity,
                capsules,
                researchFinished);
            fixture.CurrentTick = XenogermPlannerWindowAnalysisCache.LiveStateRefreshIntervalTicks;
            XenogermPlannerWindowAssemblerAnalysis changed = cache.GetAssemblerAnalysis(
                fixture.Plan,
                fixture.Assembler,
                fixture.Map);

            Assert.That(changed, Is.Not.SameAs(first));
            Assert.That(fixture.AssemblerLiveStateCalls, Is.EqualTo(2));
            Assert.That(fixture.AssemblerReadinessCalls, Is.EqualTo(2));
        }

        [Test]
        public void AssemblerAnalysis_ReanalyzesImmediatelyForMapAndPlanIdentityChanges()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();
            cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler, fixture.Map);
            Map replacementMap = CreateUninitialized<Map>();
            cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler, replacementMap);
            cache.GetAssemblerAnalysis(fixture.Plan.CreateDuplicate(), fixture.Assembler, replacementMap);

            Assert.That(fixture.AssemblerLiveStateCalls, Is.EqualTo(3));
            Assert.That(fixture.AssemblerReadinessCalls, Is.EqualTo(3));
        }

        [Test]
        public void AssemblerAnalysis_NameAndNotificationChangesDoNotRepeatSearch()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();
            cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler, fixture.Map);
            fixture.Plan.Rename("Renamed");
            fixture.Plan.ChangeReadinessNotificationsEnabled(false);
            fixture.CurrentTick = XenogermPlannerWindowAnalysisCache.LiveStateRefreshIntervalTicks;
            cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler, fixture.Map);

            Assert.That(fixture.AssemblerLiveStateCalls, Is.EqualTo(2));
            Assert.That(fixture.AssemblerReadinessCalls, Is.EqualTo(1));
        }

        [Test]
        public void InvalidateAssemblerAnalysis_ForcesRetryWithoutClearingProductReadiness()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();
            cache.GetProductReadiness(fixture.Plan, fixture.InventorySnapshot);
            cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler, fixture.Map);

            cache.InvalidateAssemblerAnalysis();
            cache.GetProductReadiness(fixture.Plan, fixture.InventorySnapshot);
            cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler, fixture.Map);

            Assert.That(fixture.ProductReadinessCalls, Is.EqualTo(1));
            Assert.That(fixture.AssemblerLiveStateCalls, Is.EqualTo(2));
            Assert.That(fixture.AssemblerReadinessCalls, Is.EqualTo(2));
        }

        private static PlanAssemblerLiveState CreateLiveState(params PlanAssemblerGenepackSource[] sources)
        {
            return new PlanAssemblerLiveState(new PlanAssemblerScopeSnapshot(sources), true, 10, 2, true);
        }

        private static T CreateUninitialized<T>() where T : class
        {
            return (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        }

        private sealed class CacheFixture
        {
            internal CacheFixture()
            {
                Gene = new GeneDef { defName = "Gene" };
                Plan = new XenogermPlan("Plan", [Gene], PlanReadinessMode.Coverage);
                InventorySnapshot = PlanGenepackInventorySnapshot.CreateAvailable([]);
                ReadinessResult = PlanReadinessResult.CreateNotReady([], [Gene], hasExactPayloadConflict: false);
                PotentialDonorScope = PlanPotentialDonorScopeSnapshot.CreateAvailable([]);
                PotentialDonorAnalysisResult = PlanPotentialDonorAnalysisResult.CreateAvailable(
                [
                    new PlanPotentialDonorGeneDiagnostic(Gene, [])
                ]);
                Map = CreateUninitialized<Map>();
                Assembler = CreateUninitialized<Building_GeneAssembler>();
                SelectableAssemblers = [Assembler];
                AssemblerLiveState = new PlanAssemblerLiveState(
                    new PlanAssemblerScopeSnapshot([]),
                    isAssemblerPowered: true,
                    maxComplexity: 0,
                    availableArchiteCapsules: 0,
                    isArchogeneticsFinished: true);
                AssemblerReadinessResult = PlanAssemblerReadinessResult.CreateEmptyTarget(
                    PlanReadinessResult.CreateEmptyTarget(),
                    visibleGenepackCount: 0);
            }

            internal GeneDef Gene { get; }
            internal XenogermPlan Plan { get; }
            internal PlanGenepackInventorySnapshot InventorySnapshot { get; }
            internal PlanReadinessResult ReadinessResult { get; }
            internal PlanPotentialDonorScopeSnapshot PotentialDonorScope { get; }
            internal PlanPotentialDonorAnalysisResult PotentialDonorAnalysisResult { get; }
            internal Map Map { get; }
            internal Building_GeneAssembler Assembler { get; }
            internal PlanAssemblerLiveState AssemblerLiveState { get; set; }
            internal PlanAssemblerReadinessResult AssemblerReadinessResult { get; set; }

            internal Dictionary<Genepack, List<GeneDef>> PackGenes { get; } = new Dictionary<Genepack, List<GeneDef>>();

            internal IReadOnlyList<Building_GeneAssembler> SelectableAssemblers { get; }

            internal int CurrentTick { get; set; }
            internal int ProductReadinessCalls { get; private set; }
            internal int TargetAnalysisCalls { get; private set; }
            internal int PotentialDonorScopeCalls { get; private set; }
            internal int PotentialDonorAnalysisCalls { get; private set; }
            internal int SelectableAssemblerCalls { get; private set; }
            internal int AssemblerLiveStateCalls { get; private set; }
            internal int AssemblerReadinessCalls { get; private set; }

            internal XenogermPlannerWindowAnalysisCache CreateCache()
            {
                return new XenogermPlannerWindowAnalysisCache(
                    () => CurrentTick,
                    (_, _) =>
                    {
                        ProductReadinessCalls++;
                        return ReadinessResult;
                    },
                    _ =>
                    {
                        TargetAnalysisCalls++;
                        return new PlanGeneTargetAnalysisResult([], [], []);
                    },
                    _ =>
                    {
                        PotentialDonorScopeCalls++;
                        return PotentialDonorScope;
                    },
                    (_, _) =>
                    {
                        PotentialDonorAnalysisCalls++;
                        return PotentialDonorAnalysisResult;
                    },
                    _ =>
                    {
                        SelectableAssemblerCalls++;
                        return SelectableAssemblers;
                    },
                    _ =>
                    {
                        AssemblerLiveStateCalls++;
                        return AssemblerLiveState;
                    },
                    (_, _) =>
                    {
                        AssemblerReadinessCalls++;
                        return AssemblerReadinessResult;
                    },
                    pack => PackGenes[pack]);
            }
        }
    }
}