using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;
using XenogermPlanner.Tests.Performance;

namespace XenogermPlanner.Tests.Analysis
{
    [TestFixture]
    [Category("Performance")]
    [Explicit("Run locally in Release configuration to measure genepack relevance analysis performance.")]
    public sealed class PlanGenepackRelevancePerformanceTests
    {
        [Test]
        public void Benchmark_SingleOfferLifecycle()
        {
            var scenario = RelevancePerformanceScenario.Create();

            PerformanceMeasurement.Measure(
                "Relevance Single Offer Lifecycle",
                scenario.EvaluateSingleOfferLifecycle,
                result => AssertMatches(result, scenario.ExpectedSingleOfferMatches),
                $"Candidate plans: {scenario.PlanCount}",
                $"Expected matches: {scenario.ExpectedSingleOfferMatches.Count}",
                "Analyzer constructions per sample: 1",
                "Offer evaluations per sample: 1");
        }

        [Test]
        public void Benchmark_TwoOfferLifecycle()
        {
            var scenario = RelevancePerformanceScenario.Create();

            PerformanceMeasurement.Measure(
                "Relevance Two Offer Lifecycle",
                scenario.EvaluateTwoOfferLifecycle,
                result => AssertBatchResult(
                    result,
                    2,
                    scenario.ExpectedMatchesPerOffer,
                    scenario.ExpectedTwoOfferLastMatches),
                $"Candidate plans: {scenario.PlanCount}",
                "Offers per sample: 2",
                $"Expected matches per offer: {scenario.ExpectedMatchesPerOffer}",
                "Analyzer constructions per sample: 1");
        }

        [Test]
        public void Benchmark_RepeatedBatchLifecycle()
        {
            var scenario = RelevancePerformanceScenario.Create();

            PerformanceMeasurement.Measure(
                "Relevance Repeated Batch Lifecycle",
                scenario.EvaluateBatchLifecycle,
                result => AssertBatchResult(result, scenario),
                $"Candidate plans: {scenario.PlanCount}",
                $"Offers per sample: {scenario.BatchOfferCount}",
                $"Expected matches per offer: {scenario.ExpectedMatchesPerOffer}",
                "Analyzer constructions per sample: 1");
        }

        private static void AssertBatchResult(
            RelevanceBatchMeasurementResult result,
            RelevancePerformanceScenario scenario)
        {
            AssertBatchResult(
                result,
                scenario.BatchOfferCount,
                scenario.ExpectedMatchesPerOffer,
                scenario.ExpectedLastBatchMatches);
        }

        private static void AssertBatchResult(
            RelevanceBatchMeasurementResult result,
            int offerCount,
            int expectedMatchesPerOffer,
            IReadOnlyList<XenogermPlan> expectedLastMatches)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TotalMatchCount, Is.EqualTo(offerCount * expectedMatchesPerOffer));
            AssertMatches(result.LastMatches, expectedLastMatches);
        }

        private static void AssertMatches(IReadOnlyList<XenogermPlan> actual, IReadOnlyList<XenogermPlan> expected)
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual, Has.Count.EqualTo(expected.Count));
            Assert.That(actual, Is.EqualTo(expected));

            for (var index = 0; index < expected.Count; index++)
                Assert.That(actual[index], Is.SameAs(expected[index]));
        }

        private sealed class RelevancePerformanceScenario
        {
            private const int CandidatePlanCount = 512;
            private const int NeedGroupCount = 8;
            private const int OffersPerBatch = 64;
            private readonly Func<XenogermPlan, PlanGenepackInventorySnapshot, PlanReadinessResult> _analyzeReadiness;
            private readonly IReadOnlyList<IReadOnlyCollection<GeneDef>> _batchOffers;
            private readonly PlanGenepackInventorySnapshot _inventory;

            private readonly IReadOnlyList<XenogermPlan> _plans;
            private readonly IReadOnlyDictionary<XenogermPlan, PlanReadinessResult> _readinessByPlan;
            private readonly IReadOnlyCollection<GeneDef> _singleOffer;

            private RelevancePerformanceScenario(
                IReadOnlyList<XenogermPlan> plans,
                PlanGenepackInventorySnapshot inventory,
                IReadOnlyDictionary<XenogermPlan, PlanReadinessResult> readinessByPlan,
                IReadOnlyCollection<GeneDef> singleOffer,
                IReadOnlyList<IReadOnlyCollection<GeneDef>> batchOffers,
                int expectedMatchesPerOffer,
                IReadOnlyList<XenogermPlan> expectedSingleOfferMatches,
                IReadOnlyList<XenogermPlan> expectedTwoOfferLastMatches,
                IReadOnlyList<XenogermPlan> expectedLastBatchMatches)
            {
                _plans = plans ?? throw new ArgumentNullException(nameof(plans));
                _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
                _readinessByPlan = readinessByPlan ?? throw new ArgumentNullException(nameof(readinessByPlan));
                _singleOffer = singleOffer ?? throw new ArgumentNullException(nameof(singleOffer));
                _batchOffers = batchOffers ?? throw new ArgumentNullException(nameof(batchOffers));
                ExpectedMatchesPerOffer = expectedMatchesPerOffer;
                ExpectedSingleOfferMatches = expectedSingleOfferMatches ??
                                             throw new ArgumentNullException(nameof(expectedSingleOfferMatches));
                ExpectedTwoOfferLastMatches = expectedTwoOfferLastMatches ??
                                              throw new ArgumentNullException(nameof(expectedTwoOfferLastMatches));
                ExpectedLastBatchMatches = expectedLastBatchMatches ??
                                           throw new ArgumentNullException(nameof(expectedLastBatchMatches));
                _analyzeReadiness = AnalyzeReadiness;
            }

            internal int PlanCount => _plans.Count;
            internal int BatchOfferCount => _batchOffers.Count;
            internal int ExpectedMatchesPerOffer { get; }
            internal IReadOnlyList<XenogermPlan> ExpectedSingleOfferMatches { get; }
            internal IReadOnlyList<XenogermPlan> ExpectedTwoOfferLastMatches { get; }
            internal IReadOnlyList<XenogermPlan> ExpectedLastBatchMatches { get; }

            internal static RelevancePerformanceScenario Create()
            {
                GeneDef[] needGenes = CreateGenes("NeedGene", NeedGroupCount);
                GeneDef[] supportGenes = CreateGenes("SupportGene", NeedGroupCount);
                var plans = new List<XenogermPlan>(CandidatePlanCount);
                var readinessByPlan = new Dictionary<XenogermPlan, PlanReadinessResult>();

                for (var index = 0; index < CandidatePlanCount; index++)
                {
                    int descendingNameIndex = CandidatePlanCount - index - 1;
                    int groupIndex = index % NeedGroupCount;
                    GeneDef needGene = needGenes[groupIndex];
                    GeneDef supportGene = supportGenes[groupIndex];
                    PlanReadinessMode readinessMode = index % 2 == 0
                        ? PlanReadinessMode.Coverage
                        : PlanReadinessMode.ExactPayload;
                    var plan = new XenogermPlan(
                        $"plan-{index:D4}",
                        $"Plan {descendingNameIndex:D4}",
                        [needGene, supportGene],
                        [],
                        readinessMode);
                    var missingDiagnostic = new PlanGeneCoverageDiagnostic(needGene, PlanGeneCoverageState.Missing, []);
                    var supportComposition = new PlanGenepackCompositionDiagnostic(
                        [supportGene],
                        physicalPackCount: 1,
                        isExactPayloadEligible: true,
                        additionalGenes: []);
                    var availableDiagnostic = new PlanGeneCoverageDiagnostic(
                        supportGene,
                        PlanGeneCoverageState.Available,
                        [supportComposition]);
                    var readiness = PlanReadinessResult.CreateNotReady(
                        [supportGene],
                        [needGene],
                        hasExactPayloadConflict: false,
                        geneCoverageDiagnostics: [missingDiagnostic, availableDiagnostic]);

                    plans.Add(plan);
                    readinessByPlan.Add(plan, readiness);
                }

                var singleOfferGroup = 3;
                IReadOnlyCollection<GeneDef> singleOffer =
                [
                    needGenes[singleOfferGroup],
                    supportGenes[singleOfferGroup]
                ];

                var batchOffers = new List<IReadOnlyCollection<GeneDef>>(OffersPerBatch);

                for (var index = 0; index < OffersPerBatch; index++)
                {
                    int groupIndex = index % NeedGroupCount;

                    batchOffers.Add(
                    [
                        needGenes[groupIndex],
                        supportGenes[groupIndex]
                    ]);
                }

                var twoOfferLastGroup = 1;
                int lastBatchGroup = (OffersPerBatch - 1) % NeedGroupCount;
                IReadOnlyList<XenogermPlan> expectedSingleMatches = GetExpectedMatches(
                    plans,
                    needGenes[singleOfferGroup]);
                IReadOnlyList<XenogermPlan> expectedTwoOfferLastMatches = GetExpectedMatches(
                    plans,
                    needGenes[twoOfferLastGroup]);
                IReadOnlyList<XenogermPlan> expectedLastBatchMatches = GetExpectedMatches(
                    plans,
                    needGenes[lastBatchGroup]);

                return new RelevancePerformanceScenario(
                    plans.AsReadOnly(),
                    PlanGenepackInventorySnapshot.CreateAvailable([]),
                    readinessByPlan,
                    singleOffer,
                    batchOffers.AsReadOnly(),
                    CandidatePlanCount / NeedGroupCount,
                    expectedSingleMatches,
                    expectedTwoOfferLastMatches,
                    expectedLastBatchMatches);
            }

            internal IReadOnlyList<XenogermPlan> EvaluateSingleOfferLifecycle()
            {
                PlanGenepackRelevanceAnalyzer analyzer = CreateAnalyzer();
                return analyzer.Evaluate(_singleOffer);
            }

            internal RelevanceBatchMeasurementResult EvaluateTwoOfferLifecycle()
            {
                return EvaluateOffers(2);
            }

            internal RelevanceBatchMeasurementResult EvaluateBatchLifecycle()
            {
                return EvaluateOffers(_batchOffers.Count);
            }

            private RelevanceBatchMeasurementResult EvaluateOffers(int offerCount)
            {
                PlanGenepackRelevanceAnalyzer analyzer = CreateAnalyzer();
                IReadOnlyList<XenogermPlan> lastMatches = [];
                var totalMatchCount = 0;

                for (var index = 0; index < offerCount; index++)
                {
                    lastMatches = analyzer.Evaluate(_batchOffers[index]);
                    totalMatchCount += lastMatches.Count;
                }

                return new RelevanceBatchMeasurementResult(totalMatchCount, lastMatches);
            }

            private PlanGenepackRelevanceAnalyzer CreateAnalyzer()
            {
                return new PlanGenepackRelevanceAnalyzer(_plans, _inventory, _analyzeReadiness);
            }

            private PlanReadinessResult AnalyzeReadiness(XenogermPlan plan, PlanGenepackInventorySnapshot inventory)
            {
                return _readinessByPlan[plan];
            }

            private static IReadOnlyList<XenogermPlan> GetExpectedMatches(
                IEnumerable<XenogermPlan> plans,
                GeneDef neededGene)
            {
                return plans.Where(plan => plan.DesiredGenes.Contains(neededGene))
                    .OrderBy(plan => plan.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(plan => plan.Id, StringComparer.Ordinal)
                    .ToList()
                    .AsReadOnly();
            }

            private static GeneDef[] CreateGenes(string prefix, int count)
            {
                var genes = new GeneDef[count];

                for (var index = 0; index < count; index++)
                {
                    genes[index] = new GeneDef
                    {
                        defName = $"{prefix}{index:D4}",
                        label = $"{prefix} {index:D4}"
                    };
                }

                return genes;
            }
        }

        private sealed class RelevanceBatchMeasurementResult
        {
            internal RelevanceBatchMeasurementResult(int totalMatchCount, IReadOnlyList<XenogermPlan> lastMatches)
            {
                TotalMatchCount = totalMatchCount;
                LastMatches = lastMatches ?? throw new ArgumentNullException(nameof(lastMatches));
            }

            internal int TotalMatchCount { get; }
            internal IReadOnlyList<XenogermPlan> LastMatches { get; }
        }
    }
}