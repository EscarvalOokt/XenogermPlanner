using System.Collections.Generic;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Plans;
using XenogermPlanner.Templates;
using XenogermPlanner.Tests.Templates;
using XenogermPlanner.UI;

namespace XenogermPlanner.Tests.UI
{
    [TestFixture]
    public sealed class XenogermTemplatePresentationProjectionTests
    {
        [Test]
        public void Build_PreservesCandidateAndCompositionIdentityAndPreparesText()
        {
            GeneDef target = PlanXenogermTemplateTestData.CreateGene("Target");
            GeneDef extra = PlanXenogermTemplateTestData.CreateGene("Extra");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, target);
            PlanXenogermTemplateCandidate automatic = PlanXenogermTemplateTestData.CreateCandidate(
                plan,
                [target, extra]);
            PlanXenogermTemplateCandidate alternative = PlanXenogermTemplateTestData.CreateCandidate(plan, [target]);
            var searchResult = new PlanXenogermTemplateCandidateSearchResult(
                [automatic, alternative],
                plan.DesiredGenes,
                plan.ReadinessMode);

            var projection = XenogermTemplatePresentationProjection.Build(
                searchResult,
                (_, index) => "label:" + index,
                candidate => "summary:" + candidate.CandidateKey,
                SortGenesDescending);

            Assert.That(projection.Candidates, Has.Count.EqualTo(2));
            Assert.That(projection.Candidates[0].Candidate, Is.SameAs(automatic));
            Assert.That(projection.Candidates[1].Candidate, Is.SameAs(alternative));
            Assert.That(projection.Candidates[0].Index, Is.EqualTo(0));
            Assert.That(projection.Candidates[0].Label, Is.EqualTo("label:0"));
            Assert.That(projection.Candidates[0].Summary, Is.EqualTo("summary:" + automatic.CandidateKey));
            Assert.That(projection.Candidates[0].Compositions[0].Composition, Is.SameAs(automatic.Compositions[0]));
            Assert.That(projection.Candidates[1].SortedAdditionalGenes, Is.Empty);
        }

        [Test]
        public void Build_SortsCompositionAndAdditionalGenesAndCachesMembership()
        {
            GeneDef alpha = PlanXenogermTemplateTestData.CreateGene("Alpha");
            GeneDef zulu = PlanXenogermTemplateTestData.CreateGene("Zulu");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, alpha);
            PlanXenogermTemplateCandidate candidate = PlanXenogermTemplateTestData.CreateCandidate(plan, [alpha, zulu]);
            var searchResult = new PlanXenogermTemplateCandidateSearchResult(
                [candidate],
                plan.DesiredGenes,
                plan.ReadinessMode);

            var projection = XenogermTemplatePresentationProjection.Build(
                searchResult,
                (_, index) => index.ToString(),
                _ => "summary",
                SortGenesDescending);

            XenogermTemplateCandidatePresentation candidatePresentation = projection.Candidates[0];
            XenogermTemplateCompositionPresentation compositionPresentation = candidatePresentation.Compositions[0];

            Assert.That(compositionPresentation.SortedGenes, Is.EqualTo([zulu, alpha]));
            Assert.That(compositionPresentation.IsAdditional(zulu), Is.True);
            Assert.That(compositionPresentation.IsAdditional(alpha), Is.False);
            Assert.That(candidatePresentation.SortedAdditionalGenes, Is.EqualTo([zulu]));
        }

        [Test]
        public void Build_PreservesCandidateCompositionOrder()
        {
            GeneDef alpha = PlanXenogermTemplateTestData.CreateGene("Alpha");
            GeneDef beta = PlanXenogermTemplateTestData.CreateGene("Beta");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, alpha, beta);
            PlanXenogermTemplateCandidate candidate = PlanXenogermTemplateTestData.CreateCandidate(
                plan,
                [beta],
                [alpha]);
            var searchResult = new PlanXenogermTemplateCandidateSearchResult(
                [candidate],
                plan.DesiredGenes,
                plan.ReadinessMode);

            var projection = XenogermTemplatePresentationProjection.Build(
                searchResult,
                (_, index) => index.ToString(),
                _ => "summary",
                genes => [..genes]);

            Assert.That(projection.Candidates[0].Compositions, Has.Count.EqualTo(2));
            Assert.That(projection.Candidates[0].Compositions[0].Composition, Is.SameAs(candidate.Compositions[0]));
            Assert.That(projection.Candidates[0].Compositions[1].Composition, Is.SameAs(candidate.Compositions[1]));
        }

        private static List<GeneDef> SortGenesDescending(IEnumerable<GeneDef> genes)
        {
            var sorted = new List<GeneDef>(genes);
            sorted.Sort((left, right) => string.CompareOrdinal(right.defName, left.defName));
            return sorted;
        }
    }
}