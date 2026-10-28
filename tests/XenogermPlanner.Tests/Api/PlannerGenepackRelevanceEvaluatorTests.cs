using System.Collections.Generic;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Api;
using XenogermPlanner.Api.Internal;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Api
{
    [TestFixture]
    public sealed class PlannerGenepackRelevanceEvaluatorTests
    {
        [Test]
        public void Evaluate_ConvertsAnalyzerMatchesToPublicPlanIdentityDtos()
        {
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan lastByName = CreatePlan("plan-c", "Zulu", gene);
            XenogermPlan secondById = CreatePlan("plan-b", "alpha", gene);
            XenogermPlan firstById = CreatePlan("plan-a", "Alpha", gene);
            var readinessByPlan = new Dictionary<XenogermPlan, PlanReadinessResult>
            {
                [lastByName] = CreateMissingResult(gene),
                [secondById] = CreateMissingResult(gene),
                [firstById] = CreateMissingResult(gene)
            };
            var evaluator = new PlannerGenepackRelevanceEvaluator(
                [lastByName, secondById, firstById],
                PlanGenepackInventorySnapshot.CreateAvailable([]),
                (plan, _) => readinessByPlan[plan]);

            GenepackRelevanceItemResult result = evaluator.Evaluate([gene]);

            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceItemStatus.Success));
            Assert.That(result.Matches, Has.Count.EqualTo(3));
            Assert.That(result.Matches[0].PlanId, Is.EqualTo("plan-a"));
            Assert.That(result.Matches[0].DisplayName, Is.EqualTo("Alpha"));
            Assert.That(result.Matches[1].PlanId, Is.EqualTo("plan-b"));
            Assert.That(result.Matches[1].DisplayName, Is.EqualTo("alpha"));
            Assert.That(result.Matches[2].PlanId, Is.EqualTo("plan-c"));
            Assert.That(result.Matches[2].DisplayName, Is.EqualTo("Zulu"));
        }

        [Test]
        public void Evaluate_NoAnalyzerMatches_ReturnsSuccessfulEmptyItem()
        {
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("plan-id", "Plan Name", gene);
            var evaluator = new PlannerGenepackRelevanceEvaluator(
                [plan],
                PlanGenepackInventorySnapshot.CreateAvailable([]),
                (_, _) => PlanReadinessResult.CreateReady([gene]));

            GenepackRelevanceItemResult result = evaluator.Evaluate([gene]);

            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceItemStatus.Success));
            Assert.That(result.Matches, Is.Empty);
        }

        [Test]
        public void Constructor_StillAnalyzesPlansOnlyOnceForMultipleApiEvaluations()
        {
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("plan-id", "Plan Name", gene);
            var analyzeCount = 0;
            var evaluator = new PlannerGenepackRelevanceEvaluator(
                [plan],
                PlanGenepackInventorySnapshot.CreateAvailable([]),
                (_, _) =>
                {
                    analyzeCount++;
                    return CreateMissingResult(gene);
                });

            evaluator.Evaluate([gene]);
            evaluator.Evaluate([gene]);

            Assert.That(analyzeCount, Is.EqualTo(1));
        }

        private static XenogermPlan CreatePlan(string id, string name, GeneDef gene)
        {
            return new XenogermPlan(id, name, [gene], [], PlanReadinessMode.Coverage);
        }

        private static PlanReadinessResult CreateMissingResult(GeneDef gene)
        {
            return PlanReadinessResult.CreateNotReady(
                [],
                [gene],
                hasExactPayloadConflict: false,
                [
                    new PlanGeneCoverageDiagnostic(gene, PlanGeneCoverageState.Missing, [])
                ]);
        }

        private static GeneDef CreateGene(string defName)
        {
            return new GeneDef { defName = defName };
        }
    }
}