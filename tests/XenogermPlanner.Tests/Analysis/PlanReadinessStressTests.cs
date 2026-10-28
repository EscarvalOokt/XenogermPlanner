using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Analysis;

namespace XenogermPlanner.Tests.Analysis
{
    [TestFixture]
    public sealed class PlanReadinessStressTests
    {
        [Test]
        public void Analyze_LargeCoverageScenarioReturnsExpectedResult()
        {
            var scenario = PlanReadinessStressScenario.CreateCoverageReady();

            PlanReadinessResult result = Analyze(scenario);

            AssertScenarioResult(scenario, result);
        }

        [Test]
        public void Analyze_LargeExactReadyScenarioReturnsExpectedResult()
        {
            var scenario = PlanReadinessStressScenario.CreateExactReady();

            PlanReadinessResult result = Analyze(scenario);

            AssertScenarioResult(scenario, result);
        }

        [Test]
        public void Analyze_LargeExactConflictScenarioReturnsExpectedResult()
        {
            var scenario = PlanReadinessStressScenario.CreateExactConflict();

            PlanReadinessResult result = Analyze(scenario);

            AssertScenarioResult(scenario, result);
            Assert.That(result.CoveredGenes, Has.Count.EqualTo(512));
            Assert.That(result.MissingGenes, Is.Empty);
            Assert.That(result.HasExactPayloadConflict, Is.True);
            Assert.That(
                result.GeneCoverageDiagnostics.Count(diagnostic =>
                    diagnostic.State == PlanGeneCoverageState.ExactPayloadConflict),
                Is.EqualTo(128));
            Assert.That(
                result.GeneCoverageDiagnostics.Count(diagnostic => diagnostic.State == PlanGeneCoverageState.Available),
                Is.EqualTo(384));
        }

        [Test]
        public void Analyze_MediumTargetLargeHeterogeneousMissingScenarioReturnsExpectedResult()
        {
            var scenario = PlanReadinessStressScenario.CreateMediumTargetLargeHeterogeneousMissing();

            PlanReadinessResult result = Analyze(scenario);

            AssertScenarioResult(scenario, result);
            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.NotReady));
            Assert.That(result.CoveredGenes, Is.Empty);
            Assert.That(result.MissingGenes, Has.Count.EqualTo(128));
            Assert.That(result.GeneCoverageDiagnostics, Has.Count.EqualTo(128));
            Assert.That(result.HasExactPayloadConflict, Is.False);

            foreach (PlanGeneCoverageDiagnostic diagnostic in result.GeneCoverageDiagnostics)
            {
                Assert.That(diagnostic.State, Is.EqualTo(PlanGeneCoverageState.Missing));
                Assert.That(diagnostic.SourceGenepackCompositions, Is.Empty);
            }
        }

        [Test]
        public void Analyze_LargeCoverageScenarioDescribesEveryDesiredGene()
        {
            var scenario = PlanReadinessStressScenario.CreateCoverageReady();

            PlanReadinessResult result = Analyze(scenario);

            Assert.That(result.GeneCoverageDiagnostics, Has.Count.EqualTo(scenario.Plan.DesiredGenes.Count));
            Assert.That(
                PlanReadinessTestData.GetGeneDefNames(
                    result.GeneCoverageDiagnostics.Select(diagnostic => diagnostic.Gene)),
                Is.EqualTo(PlanReadinessTestData.GetGeneDefNames(scenario.Plan.DesiredGenes)));
        }

        [Test]
        public void Analyze_LargeExactScenarioPreservesCompositionAggregation()
        {
            var scenario = PlanReadinessStressScenario.CreateExactReady();

            PlanReadinessResult result = Analyze(scenario);

            PlanGenepackCompositionDiagnostic[] compositions = GetDistinctCompositions(result);

            Assert.That(
                compositions.Sum(composition => composition.PhysicalPackCount),
                Is.EqualTo(PlanReadinessStressScenario.PhysicalPackCount));
            Assert.That(compositions.Any(composition => composition.PhysicalPackCount > 1), Is.True);
            Assert.That(compositions.Any(composition => composition.IsExactPayloadEligible), Is.True);
            Assert.That(compositions.Any(composition => !composition.IsExactPayloadEligible), Is.True);
        }

        [Test]
        public void Analyze_RepeatedLargeScenarioReturnsSameSemanticResult()
        {
            var scenario = PlanReadinessStressScenario.CreateExactConflict();

            string expectedSignature = CreateSemanticSignature(Analyze(scenario));

            for (var iteration = 0; iteration < 100; iteration++)
            {
                string actualSignature = CreateSemanticSignature(Analyze(scenario));

                Assert.That(
                    actualSignature,
                    Is.EqualTo(expectedSignature),
                    $"Semantic result changed at repeated analysis {iteration}.");
            }
        }

        [Test]
        public void Analyze_ReorderedPhysicalPacksReturnsSameSemanticResult()
        {
            var scenario = PlanReadinessStressScenario.CreateExactReady();

            string expectedSignature = CreateSemanticSignature(Analyze(scenario));

            PlanReadinessTestData.PackFixture[] reversedPacks = scenario.Packs.Reverse().ToArray();

            PlanReadinessResult reorderedResult = PlanReadinessTestData.Analyze(scenario.Plan, reversedPacks);

            Assert.That(CreateSemanticSignature(reorderedResult), Is.EqualTo(expectedSignature));
        }

        [Test]
        public void Analyze_DifferentDeterministicPackPermutationsReturnSameSemanticResult()
        {
            var scenario = PlanReadinessStressScenario.CreateCoverageReady();

            string expectedSignature = CreateSemanticSignature(Analyze(scenario));

            for (var seed = 1; seed <= 20; seed++)
            {
                PlanReadinessTestData.PackFixture[] permutedPacks = CreatePermutation(scenario.Packs, seed);

                PlanReadinessResult result = PlanReadinessTestData.Analyze(scenario.Plan, permutedPacks);

                Assert.That(
                    CreateSemanticSignature(result),
                    Is.EqualTo(expectedSignature),
                    $"Semantic result changed for pack permutation seed {seed}.");
            }
        }

        [Test]
        public void Analyze_LargeScenarioDoesNotMutatePackGenes()
        {
            var scenario = PlanReadinessStressScenario.CreateExactReady();

            string[] expectedPackSignatures = scenario.Packs.Select(CreatePackSignature).ToArray();

            _ = Analyze(scenario);

            string[] actualPackSignatures = scenario.Packs.Select(CreatePackSignature).ToArray();

            Assert.That(actualPackSignatures, Is.EqualTo(expectedPackSignatures));
        }

        [Test]
        public void Analyze_LargeScenarioDoesNotMutatePlanTarget()
        {
            var scenario = PlanReadinessStressScenario.CreateExactConflict();

            string[] expectedGeneDefNames = PlanReadinessTestData.GetGeneDefNames(scenario.Plan.DesiredGenes);

            _ = Analyze(scenario);

            Assert.That(
                PlanReadinessTestData.GetGeneDefNames(scenario.Plan.DesiredGenes),
                Is.EqualTo(expectedGeneDefNames));
        }

        private static PlanReadinessResult Analyze(PlanReadinessStressScenario scenario)
        {
            return PlanReadinessTestData.Analyze(scenario.Plan, scenario.Packs);
        }

        private static void AssertScenarioResult(PlanReadinessStressScenario scenario, PlanReadinessResult result)
        {
            Assert.That(result.Status, Is.EqualTo(scenario.ExpectedStatus));
            Assert.That(result.CoveredGenes, Has.Count.EqualTo(scenario.ExpectedCoveredGeneCount));
            Assert.That(result.MissingGenes, Has.Count.EqualTo(scenario.ExpectedMissingGeneCount));
            Assert.That(
                result.GeneCoverageDiagnostics.Count(diagnostic =>
                    diagnostic.State == PlanGeneCoverageState.ExactPayloadConflict),
                Is.EqualTo(scenario.ExpectedExactPayloadConflictGeneCount));
            Assert.That(result.HasExactPayloadConflict, Is.EqualTo(scenario.ExpectedExactPayloadConflict));
            Assert.That(
                result.GeneCoverageDiagnostics,
                Has.Count.EqualTo(result.CoveredGenes.Count + result.MissingGenes.Count));
        }

        private static PlanGenepackCompositionDiagnostic[] GetDistinctCompositions(PlanReadinessResult result)
        {
            return result.GeneCoverageDiagnostics.SelectMany(diagnostic => diagnostic.SourceGenepackCompositions)
                .GroupBy(CreateCompositionSignature)
                .Select(group => group.First())
                .ToArray();
        }

        private static string CreateSemanticSignature(PlanReadinessResult result)
        {
            var signature = new StringBuilder();

            signature.Append("Status:").Append(result.Status).Append('\n');
            signature.Append("UnavailableReason:").Append(result.UnavailableReason).Append('\n');
            signature.Append("IsReady:").Append(result.IsReady).Append('\n');
            signature.Append("HasExactPayloadConflict:").Append(result.HasExactPayloadConflict).Append('\n');
            signature.Append("Covered:").Append(JoinGeneDefNames(result.CoveredGenes)).Append('\n');
            signature.Append("Missing:").Append(JoinGeneDefNames(result.MissingGenes)).Append('\n');

            foreach (PlanGeneCoverageDiagnostic diagnostic in result.GeneCoverageDiagnostics.OrderBy(
                         current => current.Gene.defName,
                         StringComparer.Ordinal))
            {
                signature.Append("Gene:")
                    .Append(diagnostic.Gene.defName)
                    .Append('|')
                    .Append(diagnostic.State)
                    .Append('|')
                    .Append(diagnostic.IsCovered)
                    .Append('|');

                string[] compositionSignatures = diagnostic.SourceGenepackCompositions
                    .Select(CreateCompositionSignature)
                    .OrderBy(current => current, StringComparer.Ordinal)
                    .ToArray();

                signature.Append(string.Join(";", compositionSignatures));
                signature.Append('\n');
            }

            return signature.ToString();
        }

        private static string CreateCompositionSignature(PlanGenepackCompositionDiagnostic composition)
        {
            return JoinGeneDefNames(composition.Genes) +
                   "|Count=" +
                   composition.PhysicalPackCount +
                   "|Exact=" +
                   composition.IsExactPayloadEligible +
                   "|Additional=" +
                   JoinGeneDefNames(composition.AdditionalGenes);
        }

        private static string CreatePackSignature(PlanReadinessTestData.PackFixture pack)
        {
            return JoinGeneDefNames(pack.Genes);
        }

        private static string JoinGeneDefNames(IEnumerable<GeneDef> genes)
        {
            return string.Join(",", PlanReadinessTestData.GetGeneDefNames(genes));
        }

        private static PlanReadinessTestData.PackFixture[] CreatePermutation(
            IReadOnlyList<PlanReadinessTestData.PackFixture> packs,
            int seed)
        {
            var random = new Random(seed);
            PlanReadinessTestData.PackFixture[] permutation = packs.ToArray();

            for (int index = permutation.Length - 1; index > 0; index--)
            {
                int swapIndex = random.Next(index + 1);

                (permutation[index], permutation[swapIndex]) = (permutation[swapIndex], permutation[index]);
            }

            return permutation;
        }
    }
}