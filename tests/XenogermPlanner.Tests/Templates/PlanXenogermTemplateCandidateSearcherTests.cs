using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Plans;
using XenogermPlanner.Templates;

namespace XenogermPlanner.Tests.Templates
{
    [TestFixture]
    public sealed class PlanXenogermTemplateCandidateSearcherTests
    {
        [Test]
        public void Search_ExactPayloadFindsSingleExactPack()
        {
            GeneDef geneA = PlanXenogermTemplateTestData.CreateGene("A");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.ExactPayload, geneA);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateTestData.Search(
                plan,
                PlanXenogermTemplateTestData.CreatePack("pack-a", geneA));

            Assert.That(result.HasCandidate, Is.True);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.AutomaticCandidate.GeneSetCount, Is.EqualTo(1));
            Assert.That(result.AutomaticCandidate.AdditionalGenes, Is.Empty);
        }

        [Test]
        public void Search_ExactPayloadExcludesCompositionWithAdditionalGene()
        {
            GeneDef target = PlanXenogermTemplateTestData.CreateGene("Target");
            GeneDef extra = PlanXenogermTemplateTestData.CreateGene("Extra");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.ExactPayload, target);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateTestData.Search(
                plan,
                PlanXenogermTemplateTestData.CreatePack("extra", target, extra),
                PlanXenogermTemplateTestData.CreatePack("exact", target));

            Assert.That(result.Candidates, Has.Count.EqualTo(1));
            Assert.That(result.AutomaticCandidate.UnionGenes, Is.EquivalentTo([target]));
        }

        [Test]
        public void Search_ExactPayloadPrefersFewerGeneSets()
        {
            GeneDef geneA = PlanXenogermTemplateTestData.CreateGene("A");
            GeneDef geneB = PlanXenogermTemplateTestData.CreateGene("B");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.ExactPayload, geneA, geneB);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateTestData.Search(
                plan,
                PlanXenogermTemplateTestData.CreatePack("a", geneA),
                PlanXenogermTemplateTestData.CreatePack("b", geneB),
                PlanXenogermTemplateTestData.CreatePack("ab", geneA, geneB));

            Assert.That(result.Candidates, Has.Count.EqualTo(2));
            Assert.That(result.AutomaticCandidate.GeneSetCount, Is.EqualTo(1));
            Assert.That(
                PlanXenogermTemplateTestData.ContainsComposition(result.AutomaticCandidate, geneA, geneB),
                Is.True);
        }

        [Test]
        public void Search_ExactPayloadUsesTotalOccurrencesAsSecondCriterion()
        {
            GeneDef geneA = PlanXenogermTemplateTestData.CreateGene("A");
            GeneDef geneB = PlanXenogermTemplateTestData.CreateGene("B");
            GeneDef geneC = PlanXenogermTemplateTestData.CreateGene("C");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(
                PlanReadinessMode.ExactPayload,
                geneA,
                geneB,
                geneC);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateTestData.Search(
                plan,
                PlanXenogermTemplateTestData.CreatePack("ab", geneA, geneB),
                PlanXenogermTemplateTestData.CreatePack("c", geneC),
                PlanXenogermTemplateTestData.CreatePack("bc", geneB, geneC));

            Assert.That(result.Candidates, Has.Count.EqualTo(2));
            Assert.That(result.AutomaticCandidate.TotalGeneOccurrences, Is.EqualTo(3));
            Assert.That(PlanXenogermTemplateTestData.ContainsComposition(result.AutomaticCandidate, geneC), Is.True);
        }

        [Test]
        public void Search_CoveragePrefersFewerAdditionalDistinctGenes()
        {
            GeneDef geneA = PlanXenogermTemplateTestData.CreateGene("A");
            GeneDef geneB = PlanXenogermTemplateTestData.CreateGene("B");
            GeneDef extraX = PlanXenogermTemplateTestData.CreateGene("X");
            GeneDef extraY = PlanXenogermTemplateTestData.CreateGene("Y");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateTestData.Search(
                plan,
                PlanXenogermTemplateTestData.CreatePack("wide", geneA, geneB, extraX, extraY),
                PlanXenogermTemplateTestData.CreatePack("ax", geneA, extraX),
                PlanXenogermTemplateTestData.CreatePack("b", geneB));

            Assert.That(result.AutomaticCandidate.AdditionalGenes, Is.EquivalentTo([extraX]));
            Assert.That(result.AutomaticCandidate.GeneSetCount, Is.EqualTo(2));
        }

        [Test]
        public void Search_CoverageUsesFewerGeneSetsWhenAdditionalGenesTie()
        {
            GeneDef geneA = PlanXenogermTemplateTestData.CreateGene("A");
            GeneDef geneB = PlanXenogermTemplateTestData.CreateGene("B");
            GeneDef extra = PlanXenogermTemplateTestData.CreateGene("X");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateTestData.Search(
                plan,
                PlanXenogermTemplateTestData.CreatePack("abx", geneA, geneB, extra),
                PlanXenogermTemplateTestData.CreatePack("ax", geneA, extra),
                PlanXenogermTemplateTestData.CreatePack("b", geneB));

            Assert.That(result.AutomaticCandidate.AdditionalGenes, Is.EquivalentTo([extra]));
            Assert.That(result.AutomaticCandidate.GeneSetCount, Is.EqualTo(1));
        }

        [Test]
        public void Search_UsesCandidateKeyAsFinalTieBreak()
        {
            GeneDef geneA = PlanXenogermTemplateTestData.CreateGene("A");
            GeneDef geneB = PlanXenogermTemplateTestData.CreateGene("B");
            GeneDef geneC = PlanXenogermTemplateTestData.CreateGene("C");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(
                PlanReadinessMode.ExactPayload,
                geneA,
                geneB,
                geneC);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateTestData.Search(
                plan,
                PlanXenogermTemplateTestData.CreatePack("a", geneA),
                PlanXenogermTemplateTestData.CreatePack("bc", geneB, geneC),
                PlanXenogermTemplateTestData.CreatePack("ab", geneA, geneB),
                PlanXenogermTemplateTestData.CreatePack("c", geneC));

            string minimumKey = result.Candidates.Select(candidate => candidate.CandidateKey)
                .OrderBy(key => key, StringComparer.Ordinal)
                .First();

            Assert.That(result.AutomaticCandidate.CandidateKey, Is.EqualTo(minimumKey));
        }

        [Test]
        public void Search_AggregatesEquivalentPhysicalPacks()
        {
            GeneDef target = PlanXenogermTemplateTestData.CreateGene("Target");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, target);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateTestData.Search(
                plan,
                PlanXenogermTemplateTestData.CreatePack("first", target),
                PlanXenogermTemplateTestData.CreatePack("second", target));

            Assert.That(result.Candidates, Has.Count.EqualTo(1));
            Assert.That(result.AutomaticCandidate.Compositions[0].PhysicalPackCount, Is.EqualTo(2));
        }

        [Test]
        public void Search_ExcludesCompositionWithoutTargetContribution()
        {
            GeneDef target = PlanXenogermTemplateTestData.CreateGene("Target");
            GeneDef unrelated = PlanXenogermTemplateTestData.CreateGene("Unrelated");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, target);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateTestData.Search(
                plan,
                PlanXenogermTemplateTestData.CreatePack("unrelated", unrelated),
                PlanXenogermTemplateTestData.CreatePack("target", target));

            Assert.That(result.Candidates, Has.Count.EqualTo(1));
            Assert.That(result.AutomaticCandidate.UnionGenes, Is.EquivalentTo([target]));
        }

        [Test]
        public void Search_EmitsOnlyTargetIrredundantCandidates()
        {
            GeneDef geneA = PlanXenogermTemplateTestData.CreateGene("A");
            GeneDef geneB = PlanXenogermTemplateTestData.CreateGene("B");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateTestData.Search(
                plan,
                PlanXenogermTemplateTestData.CreatePack("a", geneA),
                PlanXenogermTemplateTestData.CreatePack("ab", geneA, geneB));

            Assert.That(result.Candidates, Has.Count.EqualTo(1));
            Assert.That(result.AutomaticCandidate.GeneSetCount, Is.EqualTo(1));
        }

        [Test]
        public void Search_DoesNotEmitPermutationDuplicates()
        {
            GeneDef geneA = PlanXenogermTemplateTestData.CreateGene("A");
            GeneDef geneB = PlanXenogermTemplateTestData.CreateGene("B");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateTestData.Search(
                plan,
                PlanXenogermTemplateTestData.CreatePack("a", geneA),
                PlanXenogermTemplateTestData.CreatePack("b", geneB));

            Assert.That(result.Candidates, Has.Count.EqualTo(1));
        }

        [Test]
        public void Search_IsIndependentOfInventoryOrder()
        {
            GeneDef geneA = PlanXenogermTemplateTestData.CreateGene("A");
            GeneDef geneB = PlanXenogermTemplateTestData.CreateGene("B");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB);
            PlanXenogermTemplateTestData.PackFixture packA = PlanXenogermTemplateTestData.CreatePack("a", geneA);
            PlanXenogermTemplateTestData.PackFixture packB = PlanXenogermTemplateTestData.CreatePack("b", geneB);
            PlanXenogermTemplateTestData.PackFixture packAb =
                PlanXenogermTemplateTestData.CreatePack("ab", geneB, geneA);

            PlanXenogermTemplateCandidateSearchResult first = PlanXenogermTemplateTestData.Search(
                plan,
                packA,
                packB,
                packAb);
            PlanXenogermTemplateCandidateSearchResult second =
                PlanXenogermTemplateTestData.Search(plan, packAb, packB, packA);

            Assert.That(
                first.Candidates.Select(candidate => candidate.CandidateKey),
                Is.EqualTo(second.Candidates.Select(candidate => candidate.CandidateKey)));
        }

        [Test]
        public void Search_IsIndependentOfGeneOrderInsideComposition()
        {
            GeneDef geneA = PlanXenogermTemplateTestData.CreateGene("A");
            GeneDef geneB = PlanXenogermTemplateTestData.CreateGene("B");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.ExactPayload, geneA, geneB);

            PlanXenogermTemplateCandidateSearchResult first = PlanXenogermTemplateTestData.Search(
                plan,
                PlanXenogermTemplateTestData.CreatePack("first", geneA, geneB));
            PlanXenogermTemplateCandidateSearchResult second = PlanXenogermTemplateTestData.Search(
                plan,
                PlanXenogermTemplateTestData.CreatePack("second", geneB, geneA));

            Assert.That(first.AutomaticCandidate.CandidateKey, Is.EqualTo(second.AutomaticCandidate.CandidateKey));
        }

        [TestCase(PlanReadinessStatus.NotReady)]
        [TestCase(PlanReadinessStatus.EmptyTarget)]
        [TestCase(PlanReadinessStatus.Degraded)]
        [TestCase(PlanReadinessStatus.Unavailable)]
        public void Search_ReturnsNoCandidatesUnlessProductReadinessIsReady(PlanReadinessStatus status)
        {
            GeneDef target = PlanXenogermTemplateTestData.CreateGene("Target");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, target);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateTestData.Search(
                plan,
                status,
                inventoryAvailable: true,
                PlanXenogermTemplateTestData.CreatePack("pack", target));

            Assert.That(result.Candidates, Is.Empty);
        }

        [Test]
        public void Search_ReturnsNoCandidatesForDegradedPlan()
        {
            GeneDef target = PlanXenogermTemplateTestData.CreateGene("Target");
            var plan = new XenogermPlan("stable-id", "Plan", [target], ["MissingGene"], PlanReadinessMode.Coverage);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateTestData.Search(
                plan,
                PlanReadinessStatus.Ready,
                true,
                PlanXenogermTemplateTestData.CreatePack("pack", target));

            Assert.That(result.Candidates, Is.Empty);
        }

        [Test]
        public void Search_SkipsStructurallyInvalidComposition()
        {
            GeneDef target = PlanXenogermTemplateTestData.CreateGene("Target");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, target);
            PlanXenogermTemplateTestData.PackFixture invalid =
                PlanXenogermTemplateTestData.CreatePack("invalid", target);
            PlanXenogermTemplateTestData.PackFixture valid = PlanXenogermTemplateTestData.CreatePack("valid", target);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateCandidateSearcher.Search(
                plan.DesiredGenes,
                plan.ReadinessMode,
                false,
                PlanReadinessStatus.Ready,
                true,
                [invalid.Genepack, valid.Genepack],
                genepack => ReferenceEquals(genepack, invalid.Genepack) ? new[] { target, null } : new[] { target });

            Assert.That(result.Candidates, Has.Count.EqualTo(1));
            Assert.That(result.AutomaticCandidate.Compositions[0].PhysicalPackCount, Is.EqualTo(1));
        }

        [Test]
        public void Search_DoesNotMutateTargetOrInputGeneCollections()
        {
            GeneDef geneA = PlanXenogermTemplateTestData.CreateGene("A");
            GeneDef geneB = PlanXenogermTemplateTestData.CreateGene("B");
            GeneDef[] targetGenes = [geneA, geneB];
            GeneDef[] packGenes = [geneB, geneA];
            PlanXenogermTemplateTestData.PackFixture pack = PlanXenogermTemplateTestData.CreatePack("pack", packGenes);

            PlanXenogermTemplateCandidateSearcher.Search(
                targetGenes,
                PlanReadinessMode.ExactPayload,
                false,
                PlanReadinessStatus.Ready,
                true,
                [pack.Genepack],
                _ => packGenes);

            Assert.That(targetGenes, Is.EqualTo([geneA, geneB]));
            Assert.That(packGenes, Is.EqualTo([geneB, geneA]));
        }

        [Test]
        public void Search_ReturnsNoCandidatesWhenInventoryIsUnavailable()
        {
            GeneDef target = PlanXenogermTemplateTestData.CreateGene("Target");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, target);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateTestData.Search(
                plan,
                PlanReadinessStatus.Ready,
                inventoryAvailable: false,
                PlanXenogermTemplateTestData.CreatePack("pack", target));

            Assert.That(result.Candidates, Is.Empty);
        }

        [Test]
        public void Search_MinimalNodeBudgetStillReturnsIrredundantCoverageFallback()
        {
            CreateBinaryCoverageSpace(
                4,
                out GeneDef[] targetGenes,
                out PlanXenogermTemplateTestData.PackFixture[] packs);
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, targetGenes);
            var limits = new PlanXenogermTemplateCandidateSearchLimits(1, 4);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateTestData.Search(plan, limits, packs);

            Assert.That(result.HasCandidate, Is.True);
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.Candidates.Count, Is.LessThanOrEqualTo(4));
            AssertCandidateCoversTarget(result.AutomaticCandidate, targetGenes);
            AssertCandidateIsTargetIrredundant(result.AutomaticCandidate, targetGenes);
        }

        [Test]
        public void Search_MinimalNodeBudgetReturnsValidExactPayloadFallback()
        {
            GeneDef geneA = PlanXenogermTemplateTestData.CreateGene("A");
            GeneDef geneB = PlanXenogermTemplateTestData.CreateGene("B");
            GeneDef geneC = PlanXenogermTemplateTestData.CreateGene("C");
            GeneDef[] targetGenes = [geneA, geneB, geneC];
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.ExactPayload, targetGenes);
            var limits = new PlanXenogermTemplateCandidateSearchLimits(1, 4);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateTestData.Search(
                plan,
                limits,
                PlanXenogermTemplateTestData.CreatePack("a", geneA),
                PlanXenogermTemplateTestData.CreatePack("b", geneB),
                PlanXenogermTemplateTestData.CreatePack("c", geneC),
                PlanXenogermTemplateTestData.CreatePack("ab", geneA, geneB),
                PlanXenogermTemplateTestData.CreatePack("bc", geneB, geneC));

            Assert.That(result.HasCandidate, Is.True);
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.AutomaticCandidate.UnionGenes, Is.EquivalentTo(targetGenes));
            Assert.That(result.AutomaticCandidate.AdditionalGenes, Is.Empty);
            AssertCandidateIsTargetIrredundant(result.AutomaticCandidate, targetGenes);
        }

        [Test]
        public void Search_CandidateLimitRetainsBestCandidatesFromCompleteTraversal()
        {
            CreateBinaryCoverageSpace(
                3,
                out GeneDef[] targetGenes,
                out PlanXenogermTemplateTestData.PackFixture[] packs);
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, targetGenes);
            var completeLimits = new PlanXenogermTemplateCandidateSearchLimits(1000, 100);
            var limitedLimits = new PlanXenogermTemplateCandidateSearchLimits(1000, 3);

            PlanXenogermTemplateCandidateSearchResult complete =
                PlanXenogermTemplateTestData.Search(plan, completeLimits, packs);
            PlanXenogermTemplateCandidateSearchResult limited =
                PlanXenogermTemplateTestData.Search(plan, limitedLimits, packs);

            Assert.That(complete.IsComplete, Is.True);
            Assert.That(complete.Candidates, Has.Count.EqualTo(8));
            Assert.That(limited.IsComplete, Is.False);
            Assert.That(limited.Candidates, Has.Count.EqualTo(3));
            Assert.That(
                limited.Candidates.Select(candidate => candidate.CandidateKey),
                Is.EqualTo(complete.Candidates.Take(3).Select(candidate => candidate.CandidateKey)));
        }

        [Test]
        public void Search_NodeBudgetTruncationIsDeterministicAndInventoryOrderIndependent()
        {
            CreateBinaryCoverageSpace(
                8,
                out GeneDef[] targetGenes,
                out PlanXenogermTemplateTestData.PackFixture[] packs);
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, targetGenes);
            var limits = new PlanXenogermTemplateCandidateSearchLimits(30, 5);

            PlanXenogermTemplateCandidateSearchResult first = PlanXenogermTemplateTestData.Search(plan, limits, packs);
            PlanXenogermTemplateCandidateSearchResult repeated =
                PlanXenogermTemplateTestData.Search(plan, limits, packs);
            PlanXenogermTemplateCandidateSearchResult reversed =
                PlanXenogermTemplateTestData.Search(plan, limits, packs.Reverse().ToArray());

            Assert.That(first.IsComplete, Is.False);
            Assert.That(
                repeated.Candidates.Select(candidate => candidate.CandidateKey),
                Is.EqualTo(first.Candidates.Select(candidate => candidate.CandidateKey)));
            Assert.That(
                reversed.Candidates.Select(candidate => candidate.CandidateKey),
                Is.EqualTo(first.Candidates.Select(candidate => candidate.CandidateKey)));
        }

        [Test]
        public void Search_NodeBudgetTruncationIsIndependentOfGeneOrderInsideCompositions()
        {
            GeneDef targetA = PlanXenogermTemplateTestData.CreateGene("TargetA");
            GeneDef targetB = PlanXenogermTemplateTestData.CreateGene("TargetB");
            GeneDef targetC = PlanXenogermTemplateTestData.CreateGene("TargetC");
            GeneDef extraA = PlanXenogermTemplateTestData.CreateGene("ExtraA");
            GeneDef extraB = PlanXenogermTemplateTestData.CreateGene("ExtraB");
            GeneDef extraC = PlanXenogermTemplateTestData.CreateGene("ExtraC");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(
                PlanReadinessMode.Coverage,
                targetA,
                targetB,
                targetC);
            var limits = new PlanXenogermTemplateCandidateSearchLimits(4, 2);

            PlanXenogermTemplateCandidateSearchResult first = PlanXenogermTemplateTestData.Search(
                plan,
                limits,
                PlanXenogermTemplateTestData.CreatePack("a1", targetA, extraA),
                PlanXenogermTemplateTestData.CreatePack("a2", targetA),
                PlanXenogermTemplateTestData.CreatePack("b1", targetB, extraB),
                PlanXenogermTemplateTestData.CreatePack("b2", targetB),
                PlanXenogermTemplateTestData.CreatePack("c1", targetC, extraC),
                PlanXenogermTemplateTestData.CreatePack("c2", targetC));
            PlanXenogermTemplateCandidateSearchResult reordered = PlanXenogermTemplateTestData.Search(
                plan,
                limits,
                PlanXenogermTemplateTestData.CreatePack("a1", extraA, targetA),
                PlanXenogermTemplateTestData.CreatePack("a2", targetA),
                PlanXenogermTemplateTestData.CreatePack("b1", extraB, targetB),
                PlanXenogermTemplateTestData.CreatePack("b2", targetB),
                PlanXenogermTemplateTestData.CreatePack("c1", extraC, targetC),
                PlanXenogermTemplateTestData.CreatePack("c2", targetC));

            Assert.That(first.IsComplete, Is.False);
            Assert.That(reordered.IsComplete, Is.False);
            Assert.That(
                reordered.Candidates.Select(candidate => candidate.CandidateKey),
                Is.EqualTo(first.Candidates.Select(candidate => candidate.CandidateKey)));
        }

        [Test]
        public void Search_PathologicalCandidateSpaceStopsAtConfiguredLimits()
        {
            CreateBinaryCoverageSpace(
                12,
                out GeneDef[] targetGenes,
                out PlanXenogermTemplateTestData.PackFixture[] packs);
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, targetGenes);
            var limits = new PlanXenogermTemplateCandidateSearchLimits(50, 8);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateTestData.Search(plan, limits, packs);

            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.Candidates, Is.Not.Empty);
            Assert.That(result.Candidates.Count, Is.LessThanOrEqualTo(8));
            Assert.That(
                result.Candidates.Select(candidate => candidate.CandidateKey).Distinct().Count(),
                Is.EqualTo(result.Candidates.Count));
            AssertCandidateCoversTarget(result.AutomaticCandidate, targetGenes);
        }

        [Test]
        public void Search_InconsistentReadyInputWithoutCoverageReturnsEmptyCompleteResult()
        {
            GeneDef geneA = PlanXenogermTemplateTestData.CreateGene("A");
            GeneDef geneB = PlanXenogermTemplateTestData.CreateGene("B");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB);

            PlanXenogermTemplateCandidateSearchResult result = PlanXenogermTemplateTestData.Search(
                plan,
                PlanXenogermTemplateTestData.CreatePack("a", geneA));

            Assert.That(result.Candidates, Is.Empty);
            Assert.That(result.IsComplete, Is.True);
        }

        [Test]
        public void SearchLimits_NonPositiveNodeBudget_Throws()
        {
            Assert.That(
                (Action)(() => { _ = new PlanXenogermTemplateCandidateSearchLimits(0, 1); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void SearchLimits_NonPositiveCandidateLimit_Throws()
        {
            Assert.That(
                (Action)(() => { _ = new PlanXenogermTemplateCandidateSearchLimits(1, 0); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        private static void CreateBinaryCoverageSpace(
            int targetGeneCount,
            out GeneDef[] targetGenes,
            out PlanXenogermTemplateTestData.PackFixture[] packs)
        {
            targetGenes = new GeneDef[targetGeneCount];
            var createdPacks = new List<PlanXenogermTemplateTestData.PackFixture>(targetGeneCount * 2);

            for (var index = 0; index < targetGeneCount; index++)
            {
                GeneDef target = PlanXenogermTemplateTestData.CreateGene($"Target{index:D2}");
                GeneDef firstExtra = PlanXenogermTemplateTestData.CreateGene($"Extra{index:D2}A");
                GeneDef secondExtra = PlanXenogermTemplateTestData.CreateGene($"Extra{index:D2}B");
                targetGenes[index] = target;
                createdPacks.Add(PlanXenogermTemplateTestData.CreatePack($"pack-{index:D2}-a", target, firstExtra));
                createdPacks.Add(PlanXenogermTemplateTestData.CreatePack($"pack-{index:D2}-b", target, secondExtra));
            }

            packs = createdPacks.ToArray();
        }

        private static void AssertCandidateCoversTarget(
            PlanXenogermTemplateCandidate candidate,
            IEnumerable<GeneDef> targetGenes)
        {
            var unionGenes = new HashSet<GeneDef>(candidate.UnionGenes);
            Assert.That(unionGenes.IsSupersetOf(targetGenes), Is.True);
        }

        private static void AssertCandidateIsTargetIrredundant(
            PlanXenogermTemplateCandidate candidate,
            IEnumerable<GeneDef> targetGenes)
        {
            var target = new HashSet<GeneDef>(targetGenes);

            for (var removedIndex = 0; removedIndex < candidate.Compositions.Count; removedIndex++)
            {
                var remainingGenes = new HashSet<GeneDef>();

                for (var index = 0; index < candidate.Compositions.Count; index++)
                {
                    if (index != removedIndex)
                        remainingGenes.UnionWith(candidate.Compositions[index].Genes);
                }

                Assert.That(target.IsSubsetOf(remainingGenes), Is.False);
            }
        }
    }
}