using System.Linq;
using NUnit.Framework;
using XenogermPlanner.Analysis;
using XenogermPlanner.Tests.Performance;

namespace XenogermPlanner.Tests.Analysis
{
    [TestFixture]
    [Category("Performance")]
    [Explicit("Run locally in Release configuration to measure readiness analysis performance.")]
    public sealed class PlanReadinessPerformanceTests
    {
        [Test]
        public void Benchmark_LargeCoverageAnalysis()
        {
            Measure("Large Coverage Ready", PlanReadinessStressScenario.CreateCoverageReady());
        }

        [Test]
        public void Benchmark_LargeExactReadyAnalysis()
        {
            Measure("Large Exact Ready", PlanReadinessStressScenario.CreateExactReady());
        }

        [Test]
        public void Benchmark_LargeExactConflictAnalysis()
        {
            Measure("Large Exact Conflict", PlanReadinessStressScenario.CreateExactConflict());
        }

        [Test]
        public void Benchmark_SmallTargetLargeHeterogeneousMissingAnalysis()
        {
            Measure(
                "Small Target / Large Heterogeneous Inventory / Missing",
                PlanReadinessStressScenario.CreateSmallTargetLargeHeterogeneousMissing());
        }

        [Test]
        public void Benchmark_MediumTargetLargeHeterogeneousMissingAnalysis()
        {
            Measure(
                "Medium Target / Large Heterogeneous Inventory / Missing",
                PlanReadinessStressScenario.CreateMediumTargetLargeHeterogeneousMissing());
        }

        private static void Measure(string scenarioName, PlanReadinessStressScenario scenario)
        {
            PerformanceMeasurement.Measure(
                scenarioName,
                scenario.Analyze,
                result => AssertResultMatchesScenario(scenario, result),
                $"Desired genes: {scenario.Plan.DesiredGenes.Count}",
                $"Physical genepacks: {scenario.Packs.Length}");
        }

        private static void AssertResultMatchesScenario(
            PlanReadinessStressScenario scenario,
            PlanReadinessResult result)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo(scenario.ExpectedStatus));
            Assert.That(result.CoveredGenes, Has.Count.EqualTo(scenario.ExpectedCoveredGeneCount));
            Assert.That(result.MissingGenes, Has.Count.EqualTo(scenario.ExpectedMissingGeneCount));
            Assert.That(result.HasExactPayloadConflict, Is.EqualTo(scenario.ExpectedExactPayloadConflict));
            Assert.That(
                result.GeneCoverageDiagnostics.Count(diagnostic =>
                    diagnostic.State == PlanGeneCoverageState.ExactPayloadConflict),
                Is.EqualTo(scenario.ExpectedExactPayloadConflictGeneCount));
            Assert.That(result.GeneCoverageDiagnostics, Has.Count.EqualTo(scenario.ExpectedDesiredGeneCount));
        }
    }
}