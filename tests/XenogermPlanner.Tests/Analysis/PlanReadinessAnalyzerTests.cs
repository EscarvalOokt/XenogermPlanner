using System.Linq;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Analysis
{
    [TestFixture]
    public sealed class PlanReadinessAnalyzerTests
    {
        [Test]
        public void Analyze_UnavailableInventoryReturnsUnavailable()
        {
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(
                PlanReadinessMode.Coverage,
                PlanReadinessTestData.CreateGene("GeneA"));

            PlanReadinessResult result = PlanReadinessTestData.Analyze(
                plan,
                PlanReadinessTestData.CreateUnavailableInventory());

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.Unavailable));
            Assert.That(result.IsReady, Is.False);
        }

        [Test]
        public void Analyze_UnavailableInventoryUsesNoActiveMapReason()
        {
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(
                PlanReadinessMode.Coverage,
                PlanReadinessTestData.CreateGene("GeneA"));

            PlanReadinessResult result = PlanReadinessTestData.Analyze(
                plan,
                PlanReadinessTestData.CreateUnavailableInventory());

            Assert.That(result.UnavailableReason, Is.EqualTo(PlanReadinessUnavailableReason.NoActiveMap));
        }

        [Test]
        public void Analyze_UnavailableInventoryTakesPrecedenceOverEmptyTarget()
        {
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.Coverage);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(
                plan,
                PlanReadinessTestData.CreateUnavailableInventory());

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.Unavailable));
        }

        [Test]
        public void Analyze_UnavailableInventoryTakesPrecedenceOverDegradedPlan()
        {
            XenogermPlan plan = PlanReadinessTestData.CreateDegradedPlan(PlanReadinessMode.Coverage, [], "MissingGene");

            PlanReadinessResult result = PlanReadinessTestData.Analyze(
                plan,
                PlanReadinessTestData.CreateUnavailableInventory());

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.Unavailable));
        }

        [TestCase(PlanReadinessMode.Coverage)]
        [TestCase(PlanReadinessMode.ExactPayload)]
        public void Analyze_AvailableEmptyPlanReturnsEmptyTarget(PlanReadinessMode readinessMode)
        {
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(readinessMode);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(plan);

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.EmptyTarget));
            Assert.That(result.UnavailableReason, Is.EqualTo(PlanReadinessUnavailableReason.None));
            Assert.That(result.CoveredGenes, Is.Empty);
            Assert.That(result.MissingGenes, Is.Empty);
            Assert.That(result.IsReady, Is.False);
        }

        [Test]
        public void Analyze_DegradedCoveragePlanNeverReturnsReady()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            XenogermPlan plan = PlanReadinessTestData.CreateDegradedPlan(
                PlanReadinessMode.Coverage,
                [geneA],
                "MissingGene");

            PlanReadinessResult result = PlanReadinessTestData.Analyze(plan, PlanReadinessTestData.CreatePack(geneA));

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.Degraded));
            Assert.That(result.IsReady, Is.False);
        }

        [Test]
        public void Analyze_DegradedExactPlanNeverReturnsReady()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneX = PlanReadinessTestData.CreateGene("GeneX");
            XenogermPlan plan = PlanReadinessTestData.CreateDegradedPlan(
                PlanReadinessMode.ExactPayload,
                [geneA],
                "MissingGene");

            PlanReadinessResult result = PlanReadinessTestData.Analyze(
                plan,
                PlanReadinessTestData.CreatePack(geneA, geneX));

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.Degraded));
            Assert.That(result.IsReady, Is.False);
            Assert.That(result.HasExactPayloadConflict, Is.False);
            Assert.That(
                result.GeneCoverageDiagnostics.Single().State,
                Is.EqualTo(PlanGeneCoverageState.ExactPayloadConflict));
            Assert.That(result.CoveredGenes, Is.EquivalentTo([geneA]));
            Assert.That(result.MissingGenes, Is.Empty);
        }

        [Test]
        public void Analyze_DegradedPlanReturnsPartialCoveredDiagnostics()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            XenogermPlan plan = PlanReadinessTestData.CreateDegradedPlan(
                PlanReadinessMode.Coverage,
                [geneA, geneB],
                "MissingGene");

            PlanReadinessResult result = PlanReadinessTestData.Analyze(plan, PlanReadinessTestData.CreatePack(geneA));

            Assert.That(PlanReadinessTestData.GetGeneDefNames(result.CoveredGenes), Is.EqualTo(["GeneA"]));
        }

        [Test]
        public void Analyze_DegradedPlanReturnsPartialMissingDiagnostics()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            XenogermPlan plan = PlanReadinessTestData.CreateDegradedPlan(
                PlanReadinessMode.Coverage,
                [geneA, geneB],
                "MissingGene");

            PlanReadinessResult result = PlanReadinessTestData.Analyze(plan, PlanReadinessTestData.CreatePack(geneA));

            Assert.That(PlanReadinessTestData.GetGeneDefNames(result.MissingGenes), Is.EqualTo(["GeneB"]));
        }

        [Test]
        public void Analyze_CoverageValidCombinationReturnsReady()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            GeneDef geneX = PlanReadinessTestData.CreateGene("GeneX");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(
                plan,
                PlanReadinessTestData.CreatePack(geneA, geneX),
                PlanReadinessTestData.CreatePack(geneB));

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.Ready));
            Assert.That(result.IsReady, Is.True);
            Assert.That(
                result.GeneCoverageDiagnostics.Select(diagnostic => diagnostic.State),
                Is.All.EqualTo(PlanGeneCoverageState.Available));
        }

        [Test]
        public void Analyze_CoverageMissingGeneReturnsNotReady()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(plan, PlanReadinessTestData.CreatePack(geneA));

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.NotReady));
            Assert.That(result.IsReady, Is.False);
            Assert.That(
                result.GeneCoverageDiagnostics.Single(diagnostic => ReferenceEquals(diagnostic.Gene, geneA)).State,
                Is.EqualTo(PlanGeneCoverageState.Available));
            Assert.That(
                result.GeneCoverageDiagnostics.Single(diagnostic => ReferenceEquals(diagnostic.Gene, geneB)).State,
                Is.EqualTo(PlanGeneCoverageState.Missing));
        }

        [Test]
        public void Analyze_ExactValidCombinationReturnsReady()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, geneA, geneB);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(
                plan,
                PlanReadinessTestData.CreatePack(geneA),
                PlanReadinessTestData.CreatePack(geneB));

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.Ready));
            Assert.That(result.IsReady, Is.True);
            Assert.That(
                result.GeneCoverageDiagnostics.Select(diagnostic => diagnostic.State),
                Is.All.EqualTo(PlanGeneCoverageState.Available));
        }

        [Test]
        public void Analyze_ExactMissingGeneReturnsNotReady()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, geneA, geneB);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(plan, PlanReadinessTestData.CreatePack(geneA));

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.NotReady));
            Assert.That(
                result.GeneCoverageDiagnostics.Single(diagnostic => ReferenceEquals(diagnostic.Gene, geneA)).State,
                Is.EqualTo(PlanGeneCoverageState.Available));
            Assert.That(
                result.GeneCoverageDiagnostics.Single(diagnostic => ReferenceEquals(diagnostic.Gene, geneB)).State,
                Is.EqualTo(PlanGeneCoverageState.Missing));
        }

        [Test]
        public void Analyze_ExactWithoutCombinationAndWithFullCoverageReturnsConflict()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            GeneDef geneX = PlanReadinessTestData.CreateGene("GeneX");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, geneA, geneB);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(
                plan,
                PlanReadinessTestData.CreatePack(geneA, geneX),
                PlanReadinessTestData.CreatePack(geneB));

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.NotReady));
            Assert.That(result.MissingGenes, Is.Empty);
            Assert.That(result.HasExactPayloadConflict, Is.True);
            Assert.That(
                result.GeneCoverageDiagnostics.Single(diagnostic => ReferenceEquals(diagnostic.Gene, geneA)).State,
                Is.EqualTo(PlanGeneCoverageState.ExactPayloadConflict));
            Assert.That(
                result.GeneCoverageDiagnostics.Single(diagnostic => ReferenceEquals(diagnostic.Gene, geneB)).State,
                Is.EqualTo(PlanGeneCoverageState.Available));
        }

        [Test]
        public void Analyze_ExactOnlyConflictGeneReturnsTargetConflict()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneX = PlanReadinessTestData.CreateGene("GeneX");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, geneA);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(
                plan,
                PlanReadinessTestData.CreatePack(geneA, geneX));

            PlanGeneCoverageDiagnostic diagnostic = result.GeneCoverageDiagnostics.Single();

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.NotReady));
            Assert.That(result.HasExactPayloadConflict, Is.True);
            Assert.That(result.CoveredGenes, Is.EquivalentTo([geneA]));
            Assert.That(result.MissingGenes, Is.Empty);
            Assert.That(diagnostic.State, Is.EqualTo(PlanGeneCoverageState.ExactPayloadConflict));
            Assert.That(diagnostic.IsCovered, Is.True);
        }

        [Test]
        public void Analyze_ExactWithMissingGenesDoesNotReturnConflict()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            GeneDef geneX = PlanReadinessTestData.CreateGene("GeneX");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, geneA, geneB);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(
                plan,
                PlanReadinessTestData.CreatePack(geneA, geneX));

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.NotReady));
            Assert.That(result.HasExactPayloadConflict, Is.False);
            Assert.That(result.CoveredGenes, Is.EquivalentTo([geneA]));
            Assert.That(result.MissingGenes, Is.EquivalentTo([geneB]));
            Assert.That(
                result.GeneCoverageDiagnostics.Single(diagnostic => ReferenceEquals(diagnostic.Gene, geneA)).State,
                Is.EqualTo(PlanGeneCoverageState.ExactPayloadConflict));
            Assert.That(
                result.GeneCoverageDiagnostics.Single(diagnostic => ReferenceEquals(diagnostic.Gene, geneB)).State,
                Is.EqualTo(PlanGeneCoverageState.Missing));
        }

        [Test]
        public void Analyze_ReadyResultContainsAllDesiredGenesAsCovered()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(
                plan,
                PlanReadinessTestData.CreatePack(geneA),
                PlanReadinessTestData.CreatePack(geneB));

            Assert.That(PlanReadinessTestData.GetGeneDefNames(result.CoveredGenes), Is.EqualTo(["GeneA", "GeneB"]));
        }

        [Test]
        public void Analyze_ReadyResultHasNoMissingGenes()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(plan, PlanReadinessTestData.CreatePack(geneA));

            Assert.That(result.MissingGenes, Is.Empty);
        }

        [Test]
        public void Analyze_DiagnosticsUseDistinctGeneSemantics()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneA, geneB);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(
                plan,
                PlanReadinessTestData.CreatePack(geneA, geneA),
                PlanReadinessTestData.CreatePack(geneA));

            Assert.That(PlanReadinessTestData.GetGeneDefNames(result.CoveredGenes), Is.EqualTo(["GeneA"]));
            Assert.That(PlanReadinessTestData.GetGeneDefNames(result.MissingGenes), Is.EqualTo(["GeneB"]));
        }

        [Test]
        public void Analyze_ResultCollectionsRemainSnapshotsAfterSourceMutation()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB);
            PlanReadinessTestData.PackFixture pack = PlanReadinessTestData.CreatePack(geneA);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(plan, pack);

            pack.Genes.Add(geneB);

            Assert.That(PlanReadinessTestData.GetGeneDefNames(result.CoveredGenes), Is.EqualTo(["GeneA"]));
            Assert.That(PlanReadinessTestData.GetGeneDefNames(result.MissingGenes), Is.EqualTo(["GeneB"]));
        }


        [Test]
        public void Analyze_GeneCoverageMapsSourceCompositionsPerDesiredGene()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            GeneDef geneX = PlanReadinessTestData.CreateGene("GeneX");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(
                plan,
                PlanReadinessTestData.CreatePack(geneA),
                PlanReadinessTestData.CreatePack(geneA, geneX),
                PlanReadinessTestData.CreatePack(geneB));

            PlanGeneCoverageDiagnostic geneADiagnostic =
                result.GeneCoverageDiagnostics.Single(diagnostic => ReferenceEquals(diagnostic.Gene, geneA));
            PlanGeneCoverageDiagnostic geneBDiagnostic =
                result.GeneCoverageDiagnostics.Single(diagnostic => ReferenceEquals(diagnostic.Gene, geneB));

            Assert.That(geneADiagnostic.State, Is.EqualTo(PlanGeneCoverageState.Available));
            Assert.That(geneADiagnostic.IsCovered, Is.True);
            Assert.That(geneADiagnostic.SourceGenepackCompositions, Has.Count.EqualTo(2));
            Assert.That(geneBDiagnostic.State, Is.EqualTo(PlanGeneCoverageState.Available));
            Assert.That(geneBDiagnostic.IsCovered, Is.True);
            Assert.That(geneBDiagnostic.SourceGenepackCompositions, Has.Count.EqualTo(1));
        }

        [Test]
        public void Analyze_MissingGeneCoverageHasNoSourceCompositions()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(plan, PlanReadinessTestData.CreatePack(geneA));

            PlanGeneCoverageDiagnostic diagnostic =
                result.GeneCoverageDiagnostics.Single(item => ReferenceEquals(item.Gene, geneB));

            Assert.That(diagnostic.State, Is.EqualTo(PlanGeneCoverageState.Missing));
            Assert.That(diagnostic.IsCovered, Is.False);
            Assert.That(diagnostic.SourceGenepackCompositions, Is.Empty);
        }

        [Test]
        public void Analyze_GeneCoverageAggregatesEquivalentPhysicalPacks()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(
                plan,
                PlanReadinessTestData.CreatePack(geneA),
                PlanReadinessTestData.CreatePack(geneA));

            PlanGeneCoverageDiagnostic diagnostic = result.GeneCoverageDiagnostics.Single();
            PlanGenepackCompositionDiagnostic composition = diagnostic.SourceGenepackCompositions.Single();

            Assert.That(composition.PhysicalPackCount, Is.EqualTo(2));
        }

        [Test]
        public void Analyze_ExactGeneCoverageExposesCompositionEligibility()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            GeneDef geneX = PlanReadinessTestData.CreateGene("GeneX");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, geneA, geneB);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(
                plan,
                PlanReadinessTestData.CreatePack(geneA, geneX),
                PlanReadinessTestData.CreatePack(geneB));

            PlanGeneCoverageDiagnostic geneADiagnostic =
                result.GeneCoverageDiagnostics.Single(diagnostic => ReferenceEquals(diagnostic.Gene, geneA));
            PlanGeneCoverageDiagnostic geneBDiagnostic =
                result.GeneCoverageDiagnostics.Single(diagnostic => ReferenceEquals(diagnostic.Gene, geneB));

            Assert.That(geneADiagnostic.State, Is.EqualTo(PlanGeneCoverageState.ExactPayloadConflict));
            Assert.That(geneADiagnostic.SourceGenepackCompositions.Single().IsExactPayloadEligible, Is.False);
            Assert.That(
                PlanReadinessTestData.GetGeneDefNames(
                    geneADiagnostic.SourceGenepackCompositions.Single().AdditionalGenes),
                Is.EqualTo(["GeneX"]));
            Assert.That(geneBDiagnostic.State, Is.EqualTo(PlanGeneCoverageState.Available));
            Assert.That(geneBDiagnostic.SourceGenepackCompositions.Single().IsExactPayloadEligible, Is.True);
        }

        [Test]
        public void Analyze_ExactGeneWithCompatibleAndIncompatibleSourcesReturnsAvailable()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneX = PlanReadinessTestData.CreateGene("GeneX");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, geneA);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(
                plan,
                PlanReadinessTestData.CreatePack(geneA, geneX),
                PlanReadinessTestData.CreatePack(geneA));

            PlanGeneCoverageDiagnostic diagnostic = result.GeneCoverageDiagnostics.Single();

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.Ready));
            Assert.That(diagnostic.State, Is.EqualTo(PlanGeneCoverageState.Available));
            Assert.That(diagnostic.SourceGenepackCompositions, Has.Count.EqualTo(2));
            Assert.That(
                diagnostic.SourceGenepackCompositions.Any(composition => composition.IsExactPayloadEligible),
                Is.True);
            Assert.That(
                diagnostic.SourceGenepackCompositions.Any(composition => !composition.IsExactPayloadEligible),
                Is.True);
        }

        [Test]
        public void Analyze_GeneCoverageDiagnosticsRemainSnapshotsAfterSourceMutation()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, geneA);
            PlanReadinessTestData.PackFixture pack = PlanReadinessTestData.CreatePack(geneA);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(plan, pack);

            pack.Genes.Add(geneB);

            PlanGenepackCompositionDiagnostic composition =
                result.GeneCoverageDiagnostics.Single().SourceGenepackCompositions.Single();

            Assert.That(PlanReadinessTestData.GetGeneDefNames(composition.Genes), Is.EqualTo(["GeneA"]));
            Assert.That(composition.IsExactPayloadEligible, Is.True);
            Assert.That(composition.AdditionalGenes, Is.Empty);
        }

        [Test]
        public void Analyze_GeneCoverageDiagnosticsDescribeEveryResolvedDesiredGene()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(plan, PlanReadinessTestData.CreatePack(geneA));

            Assert.That(
                result.GeneCoverageDiagnostics.Count,
                Is.EqualTo(result.CoveredGenes.Count + result.MissingGenes.Count));
            Assert.That(
                PlanReadinessTestData.GetGeneDefNames(
                    result.GeneCoverageDiagnostics.Select(diagnostic => diagnostic.Gene)),
                Is.EqualTo(["GeneA", "GeneB"]));
        }

        [Test]
        public void Analyze_ConflictingGenesRemainSeparatePhysicalRequirements()
        {
            GeneDef first = PlanReadinessTestData.CreateGene("First");
            GeneDef second = PlanReadinessTestData.CreateGene("Second");
            first.exclusionTags = ["Conflict"];
            second.exclusionTags = ["Conflict"];
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, first, second);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(
                plan,
                PlanReadinessTestData.CreatePack(first, second));

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.Ready));
            Assert.That(result.CoveredGenes, Is.EquivalentTo([first, second]));
        }

        [Test]
        public void Analyze_MissingPrerequisiteDoesNotChangeProductCoverageReadiness()
        {
            GeneDef prerequisite = PlanReadinessTestData.CreateGene("Prerequisite");
            GeneDef dependent = PlanReadinessTestData.CreateGene("Dependent");
            dependent.prerequisite = prerequisite;
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, dependent);

            PlanReadinessResult result = PlanReadinessTestData.Analyze(
                plan,
                PlanReadinessTestData.CreatePack(dependent));

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.Ready));
            Assert.That(result.CoveredGenes, Is.EquivalentTo([dependent]));
        }

        [Test]
        public void AnalyzeAvailableGenepacks_EmptyScopeReturnsNotReady()
        {
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(
                PlanReadinessMode.Coverage,
                PlanReadinessTestData.CreateGene("GeneA"));

            PlanReadinessResult result = PlanReadinessTestData.AnalyzeAvailableGenepacks(plan);

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.NotReady));
            Assert.That(result.CoveredGenes, Is.Empty);
            Assert.That(PlanReadinessTestData.GetGeneDefNames(result.MissingGenes), Is.EqualTo(["GeneA"]));
        }

        [Test]
        public void AnalyzeAvailableGenepacks_CoverageMatchesInventoryAnalysis()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB);
            PlanReadinessTestData.PackFixture pack = PlanReadinessTestData.CreatePack(geneA, geneB);

            PlanReadinessResult inventoryResult = PlanReadinessTestData.Analyze(plan, pack);
            PlanReadinessResult availableScopeResult = PlanReadinessTestData.AnalyzeAvailableGenepacks(plan, pack);

            Assert.That(availableScopeResult.Status, Is.EqualTo(inventoryResult.Status));
            Assert.That(
                PlanReadinessTestData.GetGeneDefNames(availableScopeResult.CoveredGenes),
                Is.EqualTo(PlanReadinessTestData.GetGeneDefNames(inventoryResult.CoveredGenes)));
            Assert.That(
                PlanReadinessTestData.GetGeneDefNames(availableScopeResult.MissingGenes),
                Is.EqualTo(PlanReadinessTestData.GetGeneDefNames(inventoryResult.MissingGenes)));
        }

        [Test]
        public void AnalyzeAvailableGenepacks_ExactPayloadMatchesInventoryAnalysis()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            GeneDef geneB = PlanReadinessTestData.CreateGene("GeneB");
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, geneA);
            PlanReadinessTestData.PackFixture pack = PlanReadinessTestData.CreatePack(geneA, geneB);

            PlanReadinessResult inventoryResult = PlanReadinessTestData.Analyze(plan, pack);
            PlanReadinessResult availableScopeResult = PlanReadinessTestData.AnalyzeAvailableGenepacks(plan, pack);

            Assert.That(availableScopeResult.Status, Is.EqualTo(inventoryResult.Status));
            Assert.That(
                availableScopeResult.HasExactPayloadConflict,
                Is.EqualTo(inventoryResult.HasExactPayloadConflict));
            Assert.That(
                PlanReadinessTestData.GetGeneDefNames(availableScopeResult.CoveredGenes),
                Is.EqualTo(PlanReadinessTestData.GetGeneDefNames(inventoryResult.CoveredGenes)));
            Assert.That(
                PlanReadinessTestData.GetGeneDefNames(availableScopeResult.MissingGenes),
                Is.EqualTo(PlanReadinessTestData.GetGeneDefNames(inventoryResult.MissingGenes)));
        }

        [Test]
        public void AnalyzeAvailableGenepacks_DegradedPlanReturnsDegraded()
        {
            GeneDef geneA = PlanReadinessTestData.CreateGene("GeneA");
            XenogermPlan plan = PlanReadinessTestData.CreateDegradedPlan(
                PlanReadinessMode.Coverage,
                [geneA],
                "MissingGene");

            PlanReadinessResult result = PlanReadinessTestData.AnalyzeAvailableGenepacks(
                plan,
                PlanReadinessTestData.CreatePack(geneA));

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.Degraded));
            Assert.That(PlanReadinessTestData.GetGeneDefNames(result.CoveredGenes), Is.EqualTo(["GeneA"]));
        }

        [Test]
        public void AnalyzeAvailableGenepacks_EmptyTargetReturnsEmptyTarget()
        {
            XenogermPlan plan = PlanReadinessTestData.CreatePlan(PlanReadinessMode.Coverage);

            PlanReadinessResult result = PlanReadinessTestData.AnalyzeAvailableGenepacks(plan);

            Assert.That(result.Status, Is.EqualTo(PlanReadinessStatus.EmptyTarget));
        }
    }
}