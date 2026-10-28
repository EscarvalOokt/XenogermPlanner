using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Api;
using XenogermPlanner.Api.Internal;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;
using XenogermPlanner.Tests.Analysis;

namespace XenogermPlanner.Tests.Api
{
    [TestFixture]
    public sealed class XenogermPlannerApiSemanticContractTests
    {
        [Test]
        public void Query_MixedPlanStatesAndModes_ReturnsOnlyPlannerDefinedRelevantMatches()
        {
            GeneDef coverageMissingGene = PlanReadinessTestData.CreateGene("CoverageMissingGene");
            GeneDef coverageAdditionalGene = PlanReadinessTestData.CreateGene("CoverageAdditionalGene");
            GeneDef exactConflictGene = PlanReadinessTestData.CreateGene("ExactConflictGene");
            GeneDef exactOutsideGene = PlanReadinessTestData.CreateGene("ExactOutsideGene");
            GeneDef prerequisiteGene = PlanReadinessTestData.CreateGene("PrerequisiteGene");
            GeneDef dependentGene = PlanReadinessTestData.CreateGene("DependentGene");
            dependentGene.prerequisite = prerequisiteGene;

            XenogermPlan coveragePlan = CreatePlan(
                "coverage-plan",
                "  Coverage Need  ",
                PlanReadinessMode.Coverage,
                coverageMissingGene);

            XenogermPlan exactPlan = CreatePlan(
                "exact-plan",
                "Exact Conflict",
                PlanReadinessMode.ExactPayload,
                exactConflictGene);

            XenogermPlan readyPlan = CreatePlan(
                "ready-plan",
                "Ready Plan",
                PlanReadinessMode.Coverage,
                exactConflictGene);

            XenogermPlan emptyTargetPlan = CreatePlan("empty-plan", "Empty Plan", PlanReadinessMode.Coverage);

            var degradedPlan = new XenogermPlan(
                "degraded-plan",
                "Degraded Plan",
                [coverageMissingGene],
                ["UnavailableGene"],
                PlanReadinessMode.Coverage);

            XenogermPlan prerequisitePlan = CreatePlan(
                "prerequisite-plan",
                "Prerequisite Plan",
                PlanReadinessMode.Coverage,
                dependentGene);

            PlanReadinessTestData.PackFixture conflictPack =
                PlanReadinessTestData.CreatePack(exactConflictGene, exactOutsideGene);

            PlanGenepackInventorySnapshot inventory = PlanReadinessTestData.CreateInventory(conflictPack);

            XenogermPlan[] plans =
            [
                readyPlan,
                degradedPlan,
                exactPlan,
                emptyTargetPlan,
                prerequisitePlan,
                coveragePlan
            ];

            Dictionary<string, GeneDef> resolver = CreateResolver(
                coverageMissingGene,
                coverageAdditionalGene,
                exactConflictGene,
                exactOutsideGene,
                prerequisiteGene,
                dependentGene);

            GenepackRelevanceRequest[] requests =
            [
                new GenepackRelevanceRequest(
                [
                    coverageMissingGene.defName,
                    coverageAdditionalGene.defName
                ]),
                new GenepackRelevanceRequest([exactConflictGene.defName]),
                new GenepackRelevanceRequest(
                [
                    exactConflictGene.defName,
                    exactOutsideGene.defName
                ]),
                new GenepackRelevanceRequest([prerequisiteGene.defName])
            ];

            GenepackRelevanceBatchResult result = Query(requests, plans, inventory, resolver, conflictPack);

            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceBatchStatus.Success));
            Assert.That(result.Results, Has.Count.EqualTo(requests.Length));

            AssertSingleMatch(result.Results[0], coveragePlan.Id, coveragePlan.Name);
            AssertSingleMatch(result.Results[1], exactPlan.Id, exactPlan.Name);
            AssertSuccessfulWithoutMatches(result.Results[2]);
            AssertSuccessfulWithoutMatches(result.Results[3]);

            string[] returnedPlanIds = result.Results.SelectMany(item => item.Matches)
                .Select(match => match.PlanId)
                .ToArray();

            Assert.That(returnedPlanIds, Does.Not.Contain(readyPlan.Id));
            Assert.That(returnedPlanIds, Does.Not.Contain(emptyTargetPlan.Id));
            Assert.That(returnedPlanIds, Does.Not.Contain(degradedPlan.Id));
            Assert.That(returnedPlanIds, Does.Not.Contain(prerequisitePlan.Id));
            Assert.That(result.Results[0].Matches[0].DisplayName, Is.EqualTo("Coverage Need"));
        }

        [Test]
        public void Query_RelevantMatches_AreOrderedByNormalizedNameThenStablePlanId()
        {
            GeneDef missingGene = PlanReadinessTestData.CreateGene("MissingGene");
            XenogermPlan lastByName = CreatePlan("plan-c", "Zulu", PlanReadinessMode.Coverage, missingGene);
            XenogermPlan secondById = CreatePlan("plan-b", "alpha", PlanReadinessMode.Coverage, missingGene);
            XenogermPlan firstById = CreatePlan("plan-a", "  Alpha  ", PlanReadinessMode.Coverage, missingGene);

            XenogermPlan[] plans = [lastByName, secondById, firstById];
            PlanGenepackInventorySnapshot inventory = PlanReadinessTestData.CreateInventory();
            Dictionary<string, GeneDef> resolver = CreateResolver(missingGene);

            GenepackRelevanceBatchResult result = Query(
                [new GenepackRelevanceRequest([missingGene.defName])],
                plans,
                inventory,
                resolver);

            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceBatchStatus.Success));
            Assert.That(result.Results, Has.Count.EqualTo(1));
            Assert.That(result.Results[0].Status, Is.EqualTo(GenepackRelevanceItemStatus.Success));
            Assert.That(
                result.Results[0].Matches.Select(match => match.PlanId),
                Is.EqualTo(["plan-a", "plan-b", "plan-c"]));
            Assert.That(
                result.Results[0].Matches.Select(match => match.DisplayName),
                Is.EqualTo(["Alpha", "alpha", "Zulu"]));
        }

        [Test]
        public void Query_EquivalentDefNameSets_ReturnDeterministicallyEquivalentPublicResults()
        {
            GeneDef missingGene = PlanReadinessTestData.CreateGene("MissingGene");
            GeneDef additionalGene = PlanReadinessTestData.CreateGene("AdditionalGene");
            XenogermPlan plan = CreatePlan("plan-id", "Plan Name", PlanReadinessMode.Coverage, missingGene);

            PlanGenepackInventorySnapshot inventory = PlanReadinessTestData.CreateInventory();
            Dictionary<string, GeneDef> resolver = CreateResolver(missingGene, additionalGene);

            GenepackRelevanceBatchResult firstResult = Query(
                [
                    new GenepackRelevanceRequest(
                    [
                        missingGene.defName,
                        additionalGene.defName,
                        missingGene.defName
                    ])
                ],
                [plan],
                inventory,
                resolver);

            GenepackRelevanceBatchResult secondResult = Query(
                [
                    new GenepackRelevanceRequest(
                    [
                        additionalGene.defName,
                        missingGene.defName
                    ])
                ],
                [plan],
                inventory,
                resolver);

            AssertEquivalentPublicResults(firstResult, secondResult);
        }

        [Test]
        public void Query_DoesNotMutatePlansInventoryOrNotificationDeliveryState()
        {
            GeneDef missingGene = PlanReadinessTestData.CreateGene("MissingGene");
            GeneDef inventoryGene = PlanReadinessTestData.CreateGene("InventoryGene");

            var relevantPlan = new XenogermPlan(
                "relevant-plan",
                "  Relevant Plan  ",
                [missingGene],
                [],
                PlanReadinessMode.Coverage,
                readinessNotificationsEnabled: false,
                hasReadinessNotificationBaseline: true,
                lastReadinessNotificationStateWasReady: true);

            XenogermPlan otherPlan = CreatePlan("other-plan", "Other Plan", PlanReadinessMode.Coverage, inventoryGene);

            var plans = new List<XenogermPlan> { otherPlan, relevantPlan };
            PlanReadinessTestData.PackFixture inventoryPack = PlanReadinessTestData.CreatePack(inventoryGene);
            PlanGenepackInventorySnapshot inventory = PlanReadinessTestData.CreateInventory(inventoryPack);
            Dictionary<string, GeneDef> resolver = CreateResolver(missingGene, inventoryGene);

            XenogermPlan[] originalPlanReferences = plans.ToArray();
            string originalId = relevantPlan.Id;
            string originalName = relevantPlan.Name;
            GeneDef[] originalDesiredGenes = relevantPlan.DesiredGenes.ToArray();
            string[] originalUnresolvedGeneDefNames = relevantPlan.UnresolvedDesiredGeneDefNames.ToArray();
            PlanReadinessMode originalMode = relevantPlan.ReadinessMode;
            bool originalNotificationsEnabled = relevantPlan.ReadinessNotificationsEnabled;
            bool originalHasBaseline = relevantPlan.HasReadinessNotificationBaseline;
            bool originalLastStateWasReady = relevantPlan.LastReadinessNotificationStateWasReady;
            PlanGenepackInventorySnapshot originalInventoryReference = inventory;
            bool originalInventoryAvailability = inventory.IsAvailable;
            Genepack[] originalGenepackReferences = inventory.Genepacks.ToArray();

            GenepackRelevanceBatchResult result = Query(
                [
                    new GenepackRelevanceRequest([missingGene.defName]),
                    new GenepackRelevanceRequest([inventoryGene.defName])
                ],
                plans,
                inventory,
                resolver,
                inventoryPack);

            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceBatchStatus.Success));
            Assert.That(plans, Is.EqualTo(originalPlanReferences));
            Assert.That(relevantPlan.Id, Is.EqualTo(originalId));
            Assert.That(relevantPlan.Name, Is.EqualTo(originalName));
            Assert.That(relevantPlan.DesiredGenes, Is.EqualTo(originalDesiredGenes));
            Assert.That(relevantPlan.UnresolvedDesiredGeneDefNames, Is.EqualTo(originalUnresolvedGeneDefNames));
            Assert.That(relevantPlan.ReadinessMode, Is.EqualTo(originalMode));
            Assert.That(relevantPlan.ReadinessNotificationsEnabled, Is.EqualTo(originalNotificationsEnabled));
            Assert.That(relevantPlan.HasReadinessNotificationBaseline, Is.EqualTo(originalHasBaseline));
            Assert.That(relevantPlan.LastReadinessNotificationStateWasReady, Is.EqualTo(originalLastStateWasReady));
            Assert.That(inventory, Is.SameAs(originalInventoryReference));
            Assert.That(inventory.IsAvailable, Is.EqualTo(originalInventoryAvailability));
            Assert.That(inventory.Genepacks, Is.EqualTo(originalGenepackReferences));
        }

        private static GenepackRelevanceBatchResult Query(
            IReadOnlyList<GenepackRelevanceRequest> requests,
            IReadOnlyList<XenogermPlan> plans,
            PlanGenepackInventorySnapshot inventory,
            IReadOnlyDictionary<string, GeneDef> resolver,
            params PlanReadinessTestData.PackFixture[] packs)
        {
            Func<IReadOnlyCollection<GeneDef>, GenepackRelevanceItemResult> CreateEvaluator()
            {
                var evaluator = new PlannerGenepackRelevanceEvaluator(
                    plans,
                    inventory,
                    (plan, snapshot) => PlanReadinessTestData.Analyze(plan, snapshot, packs));

                return evaluator.Evaluate;
            }

            GeneDef ResolveGeneDef(string defName)
            {
                return resolver.GetValueOrDefault(defName);
            }

            return XenogermPlannerRelevanceQuery.Query(
                requests,
                () => true,
                () => true,
                CreateEvaluator,
                ResolveGeneDef);
        }

        private static XenogermPlan CreatePlan(
            string id,
            string name,
            PlanReadinessMode readinessMode,
            params GeneDef[] desiredGenes)
        {
            return new XenogermPlan(id, name, desiredGenes, [], readinessMode);
        }

        private static Dictionary<string, GeneDef> CreateResolver(params GeneDef[] genes)
        {
            var resolver = new Dictionary<string, GeneDef>(StringComparer.Ordinal);

            foreach (GeneDef gene in genes)
            {
                if (gene == null)
                    throw new ArgumentException("Gene collection cannot contain null values.", nameof(genes));

                resolver.Add(gene.defName, gene);
            }

            return resolver;
        }

        private static void AssertSingleMatch(
            GenepackRelevanceItemResult result,
            string expectedPlanId,
            string expectedDisplayName)
        {
            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceItemStatus.Success));
            Assert.That(result.Matches, Has.Count.EqualTo(1));
            Assert.That(result.Matches[0].PlanId, Is.EqualTo(expectedPlanId));
            Assert.That(result.Matches[0].DisplayName, Is.EqualTo(expectedDisplayName));
        }

        private static void AssertSuccessfulWithoutMatches(GenepackRelevanceItemResult result)
        {
            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceItemStatus.Success));
            Assert.That(result.Matches, Is.Empty);
        }

        private static void AssertEquivalentPublicResults(
            GenepackRelevanceBatchResult first,
            GenepackRelevanceBatchResult second)
        {
            Assert.That(second.Status, Is.EqualTo(first.Status));
            Assert.That(second.UnavailableReason, Is.EqualTo(first.UnavailableReason));
            Assert.That(second.Results, Has.Count.EqualTo(first.Results.Count));

            for (var itemIndex = 0; itemIndex < first.Results.Count; itemIndex++)
            {
                GenepackRelevanceItemResult firstItem = first.Results[itemIndex];
                GenepackRelevanceItemResult secondItem = second.Results[itemIndex];

                Assert.That(secondItem.Status, Is.EqualTo(firstItem.Status));
                Assert.That(secondItem.Matches, Has.Count.EqualTo(firstItem.Matches.Count));

                for (var matchIndex = 0; matchIndex < firstItem.Matches.Count; matchIndex++)
                {
                    Assert.That(
                        secondItem.Matches[matchIndex].PlanId,
                        Is.EqualTo(firstItem.Matches[matchIndex].PlanId));
                    Assert.That(
                        secondItem.Matches[matchIndex].DisplayName,
                        Is.EqualTo(firstItem.Matches[matchIndex].DisplayName));
                }
            }
        }
    }
}