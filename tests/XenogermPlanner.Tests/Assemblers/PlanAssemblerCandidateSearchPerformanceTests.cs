using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Assemblers;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;
using XenogermPlanner.Tests.Performance;

namespace XenogermPlanner.Tests.Assemblers
{
    [TestFixture]
    [Category("Performance")]
    [Explicit("Run locally in Release configuration to measure assembler candidate-search performance.")]
    public sealed class PlanAssemblerCandidateSearchPerformanceTests
    {
        [Test]
        public void Benchmark_BranchingCoverageCandidateEnumeration()
        {
            var scenario = AssemblerCandidateSearchPerformanceScenario.Create();

            PerformanceMeasurement.Measure(
                "Assembler Branching Coverage Candidate Enumeration",
                scenario.Search,
                result => AssertResultMatchesScenario(scenario, result),
                $"Target genes: {scenario.TargetGeneCount}",
                $"Composition alternatives per target: {scenario.AlternativesPerTargetGene}",
                $"Physical genepacks: {scenario.PhysicalPackCount}",
                $"Expected candidates: {scenario.ExpectedCandidateCount}");
        }

        [Test]
        public void Benchmark_HelperHeavyReadinessStopsAtSharedBudget()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("A_Target", metabolism: -6);
            PlanAssemblerReadinessTestData.PackFixture[] packs = Enumerable.Range(0, 20)
                .Select(index => PlanAssemblerReadinessTestData.CreatePack(
                    "helper-" + index,
                    true,
                    PlanAssemblerReadinessTestData.CreateGene("Helper" + index.ToString("D2"))))
                .Prepend(PlanAssemblerReadinessTestData.CreatePack("target", true, target))
                .ToArray();
            XenogermPlan plan = PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target);
            PlanAssemblerLiveState liveState =
                PlanAssemblerReadinessTestData.CreateLiveState(true, 100, 100, true, packs);

            PerformanceMeasurement.Measure(
                "Assembler Helper-Heavy Bounded Readiness",
                () =>
                {
                    var budget = new PlanAssemblerSearchBudget();
                    PlanAssemblerReadinessResult result = PlanAssemblerReadinessTestData.Analyze(
                        plan,
                        liveState,
                        genes => genes,
                        budget,
                        packs);
                    return Tuple.Create(result, budget.VisitedStates, budget.IsExhausted);
                },
                measured =>
                {
                    Assert.That(measured.Item1.Status, Is.EqualTo(PlanAssemblerReadinessStatus.SearchIncomplete));
                    Assert.That(measured.Item1.CandidateGenepacks, Is.Empty);
                    Assert.That(measured.Item1.BlockerReasons, Is.Empty);
                    Assert.That(measured.Item2, Is.EqualTo(PlanAssemblerSearchBudget.DefaultMaxVisitedStates));
                    Assert.That(measured.Item3, Is.True);
                },
                "Composition groups: 21",
                "Shared base + helper state budget: 10000");
        }

        private static void AssertResultMatchesScenario(
            AssemblerCandidateSearchPerformanceScenario scenario,
            IReadOnlyList<PlanAssemblerCandidate> result)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(scenario.ExpectedCandidateCount));
            Assert.That(scenario.LastVisitedStates, Is.EqualTo(2047));
            Assert.That(scenario.LastSearchExhausted, Is.False);

            foreach (PlanAssemblerCandidate candidate in result)
            {
                Assert.That(candidate.IsPrerequisiteComplete, Is.True);
                Assert.That(candidate.Sources, Has.Count.EqualTo(scenario.TargetGeneCount));
                Assert.That(scenario.CandidateCoversTarget(candidate), Is.True);
            }

            IReadOnlyList<PlanAssemblerCandidate> repeatedResult = scenario.Search();

            Assert.That(
                scenario.CreateCandidateSignatures(repeatedResult),
                Is.EqualTo(scenario.CreateCandidateSignatures(result)));
        }

        private sealed class AssemblerCandidateSearchPerformanceScenario
        {
            private const int DesiredGeneCount = 10;
            private const int AlternativesPerGene = 2;
            private readonly IReadOnlyDictionary<Genepack, IReadOnlyList<GeneDef>> _genesByPack;
            private readonly Func<Genepack, IEnumerable<GeneDef>> _getGenepackGenes;
            private readonly Func<Genepack, string> _getPhysicalKey;
            private readonly IReadOnlyDictionary<Genepack, string> _physicalKeysByPack;

            private readonly XenogermPlan _plan;
            private readonly PlanAssemblerScopeSnapshot _scope;
            private readonly HashSet<GeneDef> _targetGenes;

            private AssemblerCandidateSearchPerformanceScenario(
                XenogermPlan plan,
                PlanAssemblerScopeSnapshot scope,
                IReadOnlyDictionary<Genepack, IReadOnlyList<GeneDef>> genesByPack,
                IReadOnlyDictionary<Genepack, string> physicalKeysByPack,
                IEnumerable<GeneDef> targetGenes,
                int expectedCandidateCount)
            {
                _plan = plan ?? throw new ArgumentNullException(nameof(plan));
                _scope = scope ?? throw new ArgumentNullException(nameof(scope));
                _genesByPack = genesByPack ?? throw new ArgumentNullException(nameof(genesByPack));
                _physicalKeysByPack = physicalKeysByPack ?? throw new ArgumentNullException(nameof(physicalKeysByPack));
                _targetGenes = new HashSet<GeneDef>(
                    targetGenes ?? throw new ArgumentNullException(nameof(targetGenes)));
                ExpectedCandidateCount = expectedCandidateCount;
                _getGenepackGenes = GetGenepackGenes;
                _getPhysicalKey = GetPhysicalKey;
            }

            internal int TargetGeneCount => _targetGenes.Count;
            internal int AlternativesPerTargetGene => AlternativesPerGene;
            internal int PhysicalPackCount => _scope.Sources.Count;
            internal int ExpectedCandidateCount { get; }
            internal int LastVisitedStates { get; private set; }
            internal bool LastSearchExhausted { get; private set; }

            internal static AssemblerCandidateSearchPerformanceScenario Create()
            {
                var targetGenes = new GeneDef[DesiredGeneCount];
                var packs = new List<PlanAssemblerReadinessTestData.PackFixture>(
                    DesiredGeneCount * AlternativesPerGene);

                for (var targetIndex = 0; targetIndex < DesiredGeneCount; targetIndex++)
                {
                    GeneDef targetGene = CreateGene($"TargetGene{targetIndex:D2}");
                    targetGenes[targetIndex] = targetGene;

                    for (var alternativeIndex = 0; alternativeIndex < AlternativesPerGene; alternativeIndex++)
                    {
                        GeneDef extraGene = CreateGene($"ExtraGene{targetIndex:D2}_{alternativeIndex:D2}");
                        var physicalKey = $"Pack{targetIndex:D2}_{alternativeIndex:D2}";

                        packs.Add(
                            PlanAssemblerReadinessTestData.CreatePack(
                                physicalKey,
                                facilityPowerOn: true,
                                targetGene,
                                extraGene));
                    }
                }

                PlanAssemblerReadinessTestData.PackFixture[] packArray = packs.ToArray();
                var genesByPack = new Dictionary<Genepack, IReadOnlyList<GeneDef>>(
                    ReferenceEqualityComparer<Genepack>.Instance);
                var physicalKeysByPack = new Dictionary<Genepack, string>(ReferenceEqualityComparer<Genepack>.Instance);

                foreach (PlanAssemblerReadinessTestData.PackFixture pack in packArray)
                {
                    genesByPack.Add(pack.Genepack, pack.Genes);
                    physicalKeysByPack.Add(pack.Genepack, pack.PhysicalKey);
                }

                return new AssemblerCandidateSearchPerformanceScenario(
                    PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, targetGenes),
                    PlanAssemblerReadinessTestData.CreateScope(packArray),
                    genesByPack,
                    physicalKeysByPack,
                    targetGenes,
                    expectedCandidateCount: 1 << DesiredGeneCount);
            }

            internal IReadOnlyList<PlanAssemblerCandidate> Search()
            {
                var budget = new PlanAssemblerSearchBudget();
                var candidates = PlanAssemblerCandidateSearcher.Search(
                        _plan.DesiredGenes,
                        _plan.ReadinessMode,
                        _scope,
                        _getGenepackGenes,
                        _getPhysicalKey,
                        budget)
                    .ToList();
                LastVisitedStates = budget.VisitedStates;
                LastSearchExhausted = budget.IsExhausted;
                return candidates;
            }

            internal bool CandidateCoversTarget(PlanAssemblerCandidate candidate)
            {
                if (candidate == null)
                    throw new ArgumentNullException(nameof(candidate));

                var providedGenes = new HashSet<GeneDef>();

                foreach (PlanAssemblerGenepackSource source in candidate.Sources)
                    providedGenes.UnionWith(_genesByPack[source.Genepack]);

                return _targetGenes.IsSubsetOf(providedGenes);
            }

            internal string[] CreateCandidateSignatures(IEnumerable<PlanAssemblerCandidate> candidates)
            {
                if (candidates == null)
                    throw new ArgumentNullException(nameof(candidates));

                return candidates.Select(candidate => string.Join(
                        "|",
                        candidate.Sources.Select(source => _physicalKeysByPack[source.Genepack])))
                    .ToArray();
            }

            private IEnumerable<GeneDef> GetGenepackGenes(Genepack genepack)
            {
                return _genesByPack[genepack];
            }

            private string GetPhysicalKey(Genepack genepack)
            {
                return _physicalKeysByPack[genepack];
            }

            private static GeneDef CreateGene(string defName)
            {
                return new GeneDef
                {
                    defName = defName,
                    label = defName
                };
            }
        }
    }
}