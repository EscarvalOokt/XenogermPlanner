using System.Linq;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Analysis
{
    [TestFixture]
    public sealed class PlanGenepackCombinationSearcherTests
    {
        [Test]
        public void Search_CoverageFindsSingleExactPack()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");

            PlanGenepackCombinationSearchResult result = PlanReadinessTestData.Search(
                [geneA, geneB],
                PlanReadinessMode.Coverage,
                PlanReadinessTestData.CreatePack(geneA, geneB));

            Assert.That(result.HasValidCombination, Is.True);
        }

        [Test]
        public void Search_CoverageFindsMultiplePackCombination()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");

            PlanGenepackCombinationSearchResult result = PlanReadinessTestData.Search(
                [geneA, geneB],
                PlanReadinessMode.Coverage,
                PlanReadinessTestData.CreatePack(geneA),
                PlanReadinessTestData.CreatePack(geneB));

            Assert.That(result.HasValidCombination, Is.True);
        }

        [Test]
        public void Search_CoverageAllowsAdditionalGenes()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            GeneDef geneX = PlanReadinessTestData.CreateGene("GeneX");

            PlanGenepackCombinationSearchResult result = PlanReadinessTestData.Search(
                [geneA, geneB],
                PlanReadinessMode.Coverage,
                PlanReadinessTestData.CreatePack(geneA, geneX),
                PlanReadinessTestData.CreatePack(geneB));

            Assert.That(result.HasValidCombination, Is.True);
        }

        [Test]
        public void Search_CoverageFailsWhenDesiredGeneIsMissing()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");

            PlanGenepackCombinationSearchResult result = PlanReadinessTestData.Search(
                [geneA, geneB],
                PlanReadinessMode.Coverage,
                PlanReadinessTestData.CreatePack(geneA));

            Assert.That(result.HasValidCombination, Is.False);
        }

        [Test]
        public void Search_ExactPayloadFindsSingleExactPack()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");

            PlanGenepackCombinationSearchResult result = PlanReadinessTestData.Search(
                [geneA, geneB],
                PlanReadinessMode.ExactPayload,
                PlanReadinessTestData.CreatePack(geneA, geneB));

            Assert.That(result.HasValidCombination, Is.True);
        }

        [Test]
        public void Search_ExactPayloadFindsMultipleTargetOnlyPacks()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");

            PlanGenepackCombinationSearchResult result = PlanReadinessTestData.Search(
                [geneA, geneB],
                PlanReadinessMode.ExactPayload,
                PlanReadinessTestData.CreatePack(geneA),
                PlanReadinessTestData.CreatePack(geneB));

            Assert.That(result.HasValidCombination, Is.True);
        }

        [Test]
        public void Search_ExactPayloadIgnoresExtraBearingPackWhenExactCombinationExists()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            GeneDef geneX = PlanReadinessTestData.CreateGene("GeneX");

            PlanGenepackCombinationSearchResult result = PlanReadinessTestData.Search(
                [geneA, geneB],
                PlanReadinessMode.ExactPayload,
                PlanReadinessTestData.CreatePack(geneA),
                PlanReadinessTestData.CreatePack(geneB),
                PlanReadinessTestData.CreatePack(geneA, geneX));

            Assert.That(result.HasValidCombination, Is.True);
        }

        [Test]
        public void Search_ExactPayloadFailsWhenRequiredGeneOnlyComesWithExtraGene()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            GeneDef geneX = PlanReadinessTestData.CreateGene("GeneX");

            PlanGenepackCombinationSearchResult result = PlanReadinessTestData.Search(
                [geneA, geneB],
                PlanReadinessMode.ExactPayload,
                PlanReadinessTestData.CreatePack(geneA, geneX),
                PlanReadinessTestData.CreatePack(geneB));

            Assert.That(result.HasValidCombination, Is.False);
        }

        [Test]
        public void Search_ExactPayloadFailsWhenDesiredGeneIsMissing()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");

            PlanGenepackCombinationSearchResult result = PlanReadinessTestData.Search(
                [geneA, geneB],
                PlanReadinessMode.ExactPayload,
                PlanReadinessTestData.CreatePack(geneA));

            Assert.That(result.HasValidCombination, Is.False);
        }

        [Test]
        public void Search_DuplicateGenesAcrossPacksDoNotChangeResult()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");

            PlanGenepackCombinationSearchResult result = PlanReadinessTestData.Search(
                [geneA, geneB, geneA],
                PlanReadinessMode.ExactPayload,
                PlanReadinessTestData.CreatePack(geneA),
                PlanReadinessTestData.CreatePack(geneA, geneB));

            Assert.That(result.HasValidCombination, Is.True);
            Assert.That(PlanReadinessTestData.GetGeneDefNames(result.AvailableGenes), Is.EqualTo(["GeneA", "GeneB"]));
        }

        [Test]
        public void Search_DifferentPackGroupingsWithSameUnionHaveSameReadiness()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            GeneDef geneC = PlanReadinessTestData.CreateGene("GeneC");

            PlanGenepackCombinationSearchResult groupedResult = PlanReadinessTestData.Search(
                [geneA, geneB, geneC],
                PlanReadinessMode.ExactPayload,
                PlanReadinessTestData.CreatePack(geneA, geneB),
                PlanReadinessTestData.CreatePack(geneC));

            PlanGenepackCombinationSearchResult splitResult = PlanReadinessTestData.Search(
                [geneA, geneB, geneC],
                PlanReadinessMode.ExactPayload,
                PlanReadinessTestData.CreatePack(geneA),
                PlanReadinessTestData.CreatePack(geneB, geneC));

            Assert.That(groupedResult.HasValidCombination, Is.EqualTo(splitResult.HasValidCombination));
            Assert.That(groupedResult.HasValidCombination, Is.True);
        }

        [Test]
        public void Search_MultipleValidCombinationsReturnReadinessWithoutSelection()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");

            PlanGenepackCombinationSearchResult result = PlanReadinessTestData.Search(
                [geneA, geneB],
                PlanReadinessMode.ExactPayload,
                PlanReadinessTestData.CreatePack(geneA),
                PlanReadinessTestData.CreatePack(geneB),
                PlanReadinessTestData.CreatePack(geneA, geneB));

            Assert.That(result.HasValidCombination, Is.True);
            Assert.That(PlanReadinessTestData.GetGeneDefNames(result.AvailableGenes), Is.EqualTo(["GeneA", "GeneB"]));
        }


        [Test]
        public void Search_AggregatesEquivalentPhysicalPackCompositions()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");

            PlanGenepackCombinationSearchResult result = PlanReadinessTestData.Search(
                [geneA, geneB],
                PlanReadinessMode.Coverage,
                PlanReadinessTestData.CreatePack(geneA, geneB),
                PlanReadinessTestData.CreatePack(geneB, geneA));

            Assert.That(result.AvailableGenepackCompositions, Has.Count.EqualTo(1));
            Assert.That(result.AvailableGenepackCompositions[0].PhysicalPackCount, Is.EqualTo(2));
            Assert.That(
                PlanReadinessTestData.GetGeneDefNames(result.AvailableGenepackCompositions[0].Genes),
                Is.EqualTo(["GeneA", "GeneB"]));
        }

        [Test]
        public void Search_CompositionWithoutAdditionalGenesIsExactPayloadEligible()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");

            PlanGenepackCombinationSearchResult result = PlanReadinessTestData.Search(
                [geneA, geneB],
                PlanReadinessMode.ExactPayload,
                PlanReadinessTestData.CreatePack(geneA));

            PlanGenepackCompositionDiagnostic composition = result.AvailableGenepackCompositions.Single();

            Assert.That(composition.IsExactPayloadEligible, Is.True);
            Assert.That(composition.AdditionalGenes, Is.Empty);
        }

        [Test]
        public void Search_CompositionWithAdditionalGenesIsExactPayloadIneligible()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneX = PlanReadinessTestData.CreateGene("GeneX");

            PlanGenepackCombinationSearchResult result = PlanReadinessTestData.Search(
                [geneA],
                PlanReadinessMode.ExactPayload,
                PlanReadinessTestData.CreatePack(geneA, geneX));

            PlanGenepackCompositionDiagnostic composition = result.AvailableGenepackCompositions.Single();

            Assert.That(composition.IsExactPayloadEligible, Is.False);
            Assert.That(PlanReadinessTestData.GetGeneDefNames(composition.AdditionalGenes), Is.EqualTo(["GeneX"]));
        }

        [Test]
        public void Search_CompositionDiagnosticsRemainSnapshotsAfterPackMutation()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            PlanReadinessTestData.PackFixture pack = PlanReadinessTestData.CreatePack(geneA);

            PlanGenepackCombinationSearchResult result = PlanReadinessTestData.Search(
                [geneA],
                PlanReadinessMode.ExactPayload,
                pack);

            pack.Genes.Add(geneB);

            PlanGenepackCompositionDiagnostic composition = result.AvailableGenepackCompositions.Single();

            Assert.That(PlanReadinessTestData.GetGeneDefNames(composition.Genes), Is.EqualTo(["GeneA"]));
            Assert.That(composition.IsExactPayloadEligible, Is.True);
            Assert.That(composition.AdditionalGenes, Is.Empty);
        }

        [Test]
        public void Search_RepeatedCallsReturnSameSetSemantics()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            GeneDef geneX = PlanReadinessTestData.CreateGene("GeneX");
            PlanReadinessTestData.PackFixture[] packs =
            [
                PlanReadinessTestData.CreatePack(geneA, geneX),
                PlanReadinessTestData.CreatePack(geneB)
            ];

            PlanGenepackCombinationSearchResult firstResult = PlanReadinessTestData.Search(
                [geneA, geneB],
                PlanReadinessMode.ExactPayload,
                packs);

            PlanGenepackCombinationSearchResult secondResult = PlanReadinessTestData.Search(
                [geneA, geneB],
                PlanReadinessMode.ExactPayload,
                packs);

            Assert.That(firstResult.HasValidCombination, Is.EqualTo(secondResult.HasValidCombination));
            Assert.That(
                PlanReadinessTestData.GetGeneDefNames(firstResult.AvailableGenes),
                Is.EqualTo(PlanReadinessTestData.GetGeneDefNames(secondResult.AvailableGenes)));
        }
    }
}