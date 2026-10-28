using System;
using System.Collections.Generic;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Api;
using XenogermPlanner.Api.Internal;

namespace XenogermPlanner.Tests.Api
{
    [TestFixture]
    public sealed class XenogermPlannerRelevanceQueryContractTests
    {
        [Test]
        public void Query_NullBatch_ReturnsInvalidRequestWithoutReadingContext()
        {
            GenepackRelevanceBatchResult result = XenogermPlannerRelevanceQuery.Query(
                null,
                () => throw new InvalidOperationException(),
                () => throw new InvalidOperationException(),
                () => throw new InvalidOperationException(),
                _ => throw new InvalidOperationException());

            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceBatchStatus.InvalidRequest));
            Assert.That(result.Results, Is.Empty);
        }

        [Test]
        public void Query_EmptyBatch_ReturnsSuccessfulEmptyResultWithoutReadingContext()
        {
            GenepackRelevanceBatchResult result = XenogermPlannerRelevanceQuery.Query(
                [],
                () => throw new InvalidOperationException(),
                () => throw new InvalidOperationException(),
                () => throw new InvalidOperationException(),
                _ => throw new InvalidOperationException());

            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceBatchStatus.Success));
            Assert.That(result.Results, Is.Empty);
        }

        [Test]
        public void Query_NoGame_ReturnsUnavailable()
        {
            GenepackRelevanceBatchResult result = QueryWithContext(
                CreateValidRequests(),
                hasGame: false,
                hasActiveMap: true,
                hasPlannerState: true);

            AssertUnavailable(result, GenepackRelevanceUnavailableReason.NoGame);
        }

        [Test]
        public void Query_NoActiveMap_ReturnsUnavailable()
        {
            GenepackRelevanceBatchResult result = QueryWithContext(
                CreateValidRequests(),
                hasGame: true,
                hasActiveMap: false,
                hasPlannerState: true);

            AssertUnavailable(result, GenepackRelevanceUnavailableReason.NoActiveMap);
        }

        [Test]
        public void Query_PlannerStateUnavailable_ReturnsUnavailable()
        {
            GenepackRelevanceBatchResult result = QueryWithContext(
                CreateValidRequests(),
                hasGame: true,
                hasActiveMap: true,
                hasPlannerState: false);

            AssertUnavailable(result, GenepackRelevanceUnavailableReason.PlannerStateUnavailable);
        }

        [Test]
        public void Query_ContextFailure_ReturnsBatchFailed()
        {
            GenepackRelevanceBatchResult result = XenogermPlannerRelevanceQuery.Query(
                CreateValidRequests(),
                () => throw new InvalidOperationException(),
                () => true,
                CreateSuccessfulEvaluator,
                ResolveKnownGene);

            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceBatchStatus.Failed));
            Assert.That(result.Results, Is.Empty);
        }

        [Test]
        public void Query_EvaluatorFactoryFailure_ReturnsBatchFailed()
        {
            GenepackRelevanceBatchResult result = XenogermPlannerRelevanceQuery.Query(
                CreateValidRequests(),
                () => true,
                () => true,
                () => throw new InvalidOperationException(),
                ResolveKnownGene);

            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceBatchStatus.Failed));
            Assert.That(result.Results, Is.Empty);
        }

        [Test]
        public void Query_CreatesEvaluatorOnceForWholeBatch()
        {
            var factoryCallCount = 0;

            GenepackRelevanceBatchResult result = XenogermPlannerRelevanceQuery.Query(
                [
                    new GenepackRelevanceRequest(["GeneA"]),
                    new GenepackRelevanceRequest(["GeneB"]),
                    new GenepackRelevanceRequest(["GeneC"])
                ],
                () => true,
                () => true,
                () =>
                {
                    factoryCallCount++;
                    return _ => GenepackRelevanceItemResult.CreateSuccess([]);
                },
                ResolveKnownGene);

            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceBatchStatus.Success));
            Assert.That(factoryCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Query_NullItem_ReturnsInvalidInputAtSameIndex()
        {
            GenepackRelevanceBatchResult result = QueryWithEvaluator(
                [
                    new GenepackRelevanceRequest(["GeneA"]),
                    null,
                    new GenepackRelevanceRequest(["GeneB"])
                ],
                _ => GenepackRelevanceItemResult.CreateSuccess([]));

            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceBatchStatus.Success));
            Assert.That(result.Results, Has.Count.EqualTo(3));
            Assert.That(result.Results[0].Status, Is.EqualTo(GenepackRelevanceItemStatus.Success));
            Assert.That(result.Results[1].Status, Is.EqualTo(GenepackRelevanceItemStatus.InvalidInput));
            Assert.That(result.Results[2].Status, Is.EqualTo(GenepackRelevanceItemStatus.Success));
        }

        [Test]
        public void Query_EmptyComposition_ReturnsInvalidInput()
        {
            GenepackRelevanceBatchResult result = QueryWithEvaluator(
                [new GenepackRelevanceRequest([])],
                _ => throw new InvalidOperationException());

            AssertSingleItemStatus(result, GenepackRelevanceItemStatus.InvalidInput);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void Query_InvalidGeneDefName_ReturnsInvalidInput(string invalidGeneDefName)
        {
            GenepackRelevanceBatchResult result = QueryWithEvaluator(
                [new GenepackRelevanceRequest(["GeneA", invalidGeneDefName])],
                _ => throw new InvalidOperationException());

            AssertSingleItemStatus(result, GenepackRelevanceItemStatus.InvalidInput);
        }

        [Test]
        public void Query_DeduplicatesByOrdinalEqualityAndPreservesFirstOccurrenceOrder()
        {
            GeneDef geneA = CreateGene("GeneA");
            GeneDef lowerCaseGeneA = CreateGene("genea");
            GeneDef geneB = CreateGene("GeneB");
            IReadOnlyCollection<GeneDef> capturedComposition = null;

            GenepackRelevanceBatchResult result = XenogermPlannerRelevanceQuery.Query(
                [
                    new GenepackRelevanceRequest(["GeneA", "GeneA", "genea", "GeneB", "GeneA"])
                ],
                () => true,
                () => true,
                () => composition =>
                {
                    capturedComposition = composition;
                    return GenepackRelevanceItemResult.CreateSuccess([]);
                },
                geneDefName =>
                {
                    switch (geneDefName)
                    {
                        case "GeneA":
                            return geneA;
                        case "genea":
                            return lowerCaseGeneA;
                        case "GeneB":
                            return geneB;
                        default:
                            return null;
                    }
                });

            AssertSingleItemStatus(result, GenepackRelevanceItemStatus.Success);
            Assert.That(capturedComposition, Is.EqualTo([geneA, lowerCaseGeneA, geneB]));
        }

        [Test]
        public void Query_ResolverAliases_AreDeduplicatedBeforeEvaluation()
        {
            GeneDef sharedGene = CreateGene("CanonicalGene");
            IReadOnlyCollection<GeneDef> capturedComposition = null;

            GenepackRelevanceBatchResult result = XenogermPlannerRelevanceQuery.Query(
                [new GenepackRelevanceRequest(["GeneA", "GeneAlias"])],
                () => true,
                () => true,
                () => composition =>
                {
                    capturedComposition = composition;
                    return GenepackRelevanceItemResult.CreateSuccess([]);
                },
                _ => sharedGene);

            AssertSingleItemStatus(result, GenepackRelevanceItemStatus.Success);
            Assert.That(capturedComposition, Is.EqualTo([sharedGene]));
        }

        [Test]
        public void Query_UnknownGeneDef_ReturnsUnknownGeneDef()
        {
            GenepackRelevanceBatchResult result = XenogermPlannerRelevanceQuery.Query(
                [new GenepackRelevanceRequest(["UnknownGene"])],
                () => true,
                () => true,
                CreateSuccessfulEvaluator,
                _ => null);

            AssertSingleItemStatus(result, GenepackRelevanceItemStatus.UnknownGeneDef);
        }

        [Test]
        public void Query_UnknownGeneDef_DoesNotEvaluatePartialComposition()
        {
            var evaluationCount = 0;

            GenepackRelevanceBatchResult result = XenogermPlannerRelevanceQuery.Query(
                [new GenepackRelevanceRequest(["GeneA", "UnknownGene"])],
                () => true,
                () => true,
                () => _ =>
                {
                    evaluationCount++;
                    return GenepackRelevanceItemResult.CreateSuccess([]);
                },
                geneDefName => geneDefName == "UnknownGene" ? null : CreateGene(geneDefName));

            AssertSingleItemStatus(result, GenepackRelevanceItemStatus.UnknownGeneDef);
            Assert.That(evaluationCount, Is.Zero);
        }

        [Test]
        public void Query_UnknownGeneDef_IsolatedToMatchingItem()
        {
            GenepackRelevanceBatchResult result = XenogermPlannerRelevanceQuery.Query(
                [
                    new GenepackRelevanceRequest(["GeneA"]),
                    new GenepackRelevanceRequest(["UnknownGene"]),
                    new GenepackRelevanceRequest(["GeneB"])
                ],
                () => true,
                () => true,
                CreateSuccessfulEvaluator,
                geneDefName => geneDefName == "UnknownGene" ? null : CreateGene(geneDefName));

            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceBatchStatus.Success));
            Assert.That(result.Results[0].Status, Is.EqualTo(GenepackRelevanceItemStatus.Success));
            Assert.That(result.Results[1].Status, Is.EqualTo(GenepackRelevanceItemStatus.UnknownGeneDef));
            Assert.That(result.Results[2].Status, Is.EqualTo(GenepackRelevanceItemStatus.Success));
        }

        [Test]
        public void Query_ResolverFailure_IsolatedToFailedItem()
        {
            GenepackRelevanceBatchResult result = XenogermPlannerRelevanceQuery.Query(
                [
                    new GenepackRelevanceRequest(["GeneA"]),
                    new GenepackRelevanceRequest(["BrokenGene"]),
                    new GenepackRelevanceRequest(["GeneB"])
                ],
                () => true,
                () => true,
                CreateSuccessfulEvaluator,
                geneDefName =>
                {
                    if (geneDefName == "BrokenGene")
                        throw new InvalidOperationException();

                    return CreateGene(geneDefName);
                });

            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceBatchStatus.Success));
            Assert.That(result.Results[0].Status, Is.EqualTo(GenepackRelevanceItemStatus.Success));
            Assert.That(result.Results[1].Status, Is.EqualTo(GenepackRelevanceItemStatus.Failed));
            Assert.That(result.Results[2].Status, Is.EqualTo(GenepackRelevanceItemStatus.Success));
        }

        [Test]
        public void Query_EvaluatorFailure_IsolatedToFailedItem()
        {
            var evaluationIndex = 0;

            GenepackRelevanceBatchResult result = QueryWithEvaluator(
                [
                    new GenepackRelevanceRequest(["GeneA"]),
                    new GenepackRelevanceRequest(["GeneB"]),
                    new GenepackRelevanceRequest(["GeneC"])
                ],
                _ =>
                {
                    evaluationIndex++;

                    if (evaluationIndex == 2)
                        throw new InvalidOperationException();

                    return GenepackRelevanceItemResult.CreateSuccess([]);
                });

            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceBatchStatus.Success));
            Assert.That(result.Results[0].Status, Is.EqualTo(GenepackRelevanceItemStatus.Success));
            Assert.That(result.Results[1].Status, Is.EqualTo(GenepackRelevanceItemStatus.Failed));
            Assert.That(result.Results[2].Status, Is.EqualTo(GenepackRelevanceItemStatus.Success));
        }

        [Test]
        public void Query_NullEvaluatorResult_IsolatedToFailedItem()
        {
            GenepackRelevanceBatchResult result = QueryWithEvaluator(CreateValidRequests(), _ => null);

            AssertSingleItemStatus(result, GenepackRelevanceItemStatus.Failed);
        }

        [Test]
        public void Query_ValidKnownComposition_ReturnsEvaluatorResult()
        {
            var expectedMatch = new GenepackRelevancePlanMatch("plan-id", "Plan Name");

            GenepackRelevanceBatchResult result = QueryWithEvaluator(
                CreateValidRequests(),
                _ => GenepackRelevanceItemResult.CreateSuccess([expectedMatch]));

            AssertSingleItemStatus(result, GenepackRelevanceItemStatus.Success);
            Assert.That(result.Results[0].Matches, Is.EqualTo([expectedMatch]));
        }

        private static IReadOnlyList<GenepackRelevanceRequest> CreateValidRequests()
        {
            return [new GenepackRelevanceRequest(["GeneA"])];
        }

        private static GenepackRelevanceBatchResult QueryWithContext(
            IReadOnlyList<GenepackRelevanceRequest> requests,
            bool hasGame,
            bool hasActiveMap,
            bool hasPlannerState)
        {
            return XenogermPlannerRelevanceQuery.Query(
                requests,
                () => hasGame,
                () => hasActiveMap,
                () => hasPlannerState ? CreateSuccessfulEvaluator() : null,
                ResolveKnownGene);
        }

        private static GenepackRelevanceBatchResult QueryWithEvaluator(
            IReadOnlyList<GenepackRelevanceRequest> requests,
            Func<IReadOnlyCollection<GeneDef>, GenepackRelevanceItemResult> evaluateComposition)
        {
            return XenogermPlannerRelevanceQuery.Query(
                requests,
                () => true,
                () => true,
                () => evaluateComposition,
                ResolveKnownGene);
        }

        private static Func<IReadOnlyCollection<GeneDef>, GenepackRelevanceItemResult> CreateSuccessfulEvaluator()
        {
            return _ => GenepackRelevanceItemResult.CreateSuccess([]);
        }

        private static GeneDef ResolveKnownGene(string geneDefName)
        {
            return CreateGene(geneDefName);
        }

        private static GeneDef CreateGene(string defName)
        {
            return new GeneDef { defName = defName };
        }

        private static void AssertUnavailable(
            GenepackRelevanceBatchResult result,
            GenepackRelevanceUnavailableReason expectedReason)
        {
            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceBatchStatus.Unavailable));
            Assert.That(result.UnavailableReason, Is.EqualTo(expectedReason));
            Assert.That(result.Results, Is.Empty);
        }

        private static void AssertSingleItemStatus(
            GenepackRelevanceBatchResult result,
            GenepackRelevanceItemStatus expectedStatus)
        {
            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceBatchStatus.Success));
            Assert.That(result.Results, Has.Count.EqualTo(1));
            Assert.That(result.Results[0].Status, Is.EqualTo(expectedStatus));
        }
    }
}