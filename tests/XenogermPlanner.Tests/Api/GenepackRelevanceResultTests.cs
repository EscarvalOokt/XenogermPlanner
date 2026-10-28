using System;
using System.Collections.Generic;
using NUnit.Framework;
using XenogermPlanner.Api;

namespace XenogermPlanner.Tests.Api
{
    [TestFixture]
    public sealed class GenepackRelevanceResultTests
    {
        [Test]
        public void PlanMatch_ValidIdentity_PreservesValues()
        {
            var match = new GenepackRelevancePlanMatch("plan-id", "Plan Name");

            Assert.That(match.PlanId, Is.EqualTo("plan-id"));
            Assert.That(match.DisplayName, Is.EqualTo("Plan Name"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void PlanMatch_InvalidPlanId_Throws(string planId)
        {
            Assert.Throws<ArgumentException>(
                (Action)(() => { _ = new GenepackRelevancePlanMatch(planId, "Plan Name"); }));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void PlanMatch_InvalidDisplayName_Throws(string displayName)
        {
            Assert.Throws<ArgumentException>(
                (Action)(() => { _ = new GenepackRelevancePlanMatch("plan-id", displayName); }));
        }

        [Test]
        public void ItemSuccess_CopiesMatchesAndPreservesOrder()
        {
            var first = new GenepackRelevancePlanMatch("first", "First");
            var second = new GenepackRelevancePlanMatch("second", "Second");
            var source = new List<GenepackRelevancePlanMatch> { first, second };

            var result = GenepackRelevanceItemResult.CreateSuccess(source);
            source.Clear();

            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceItemStatus.Success));
            Assert.That(result.Matches, Is.EqualTo([first, second]));
        }

        [Test]
        public void ItemErrorFactories_ReturnEmptyMatches()
        {
            GenepackRelevanceItemResult[] results =
            [
                GenepackRelevanceItemResult.CreateInvalidInput(),
                GenepackRelevanceItemResult.CreateUnknownGeneDef(),
                GenepackRelevanceItemResult.CreateFailed()
            ];

            Assert.That(results[0].Status, Is.EqualTo(GenepackRelevanceItemStatus.InvalidInput));
            Assert.That(results[1].Status, Is.EqualTo(GenepackRelevanceItemStatus.UnknownGeneDef));
            Assert.That(results[2].Status, Is.EqualTo(GenepackRelevanceItemStatus.Failed));

            foreach (GenepackRelevanceItemResult result in results)
                Assert.That(result.Matches, Is.Empty);
        }

        [Test]
        public void BatchSuccess_CopiesResultsAndPreservesOrder()
        {
            var first = GenepackRelevanceItemResult.CreateSuccess([]);
            var second = GenepackRelevanceItemResult.CreateInvalidInput();
            var source = new List<GenepackRelevanceItemResult> { first, second };

            var result = GenepackRelevanceBatchResult.CreateSuccess(source);
            source.Clear();

            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceBatchStatus.Success));
            Assert.That(result.UnavailableReason, Is.EqualTo(GenepackRelevanceUnavailableReason.None));
            Assert.That(result.Results, Is.EqualTo([first, second]));
        }

        [Test]
        public void BatchNonSuccessFactories_ReturnNoPartialResults()
        {
            var invalid = GenepackRelevanceBatchResult.CreateInvalidRequest();
            var unavailable = GenepackRelevanceBatchResult.CreateUnavailable(
                GenepackRelevanceUnavailableReason.NoActiveMap);
            var failed = GenepackRelevanceBatchResult.CreateFailed();

            Assert.That(invalid.Status, Is.EqualTo(GenepackRelevanceBatchStatus.InvalidRequest));
            Assert.That(invalid.UnavailableReason, Is.EqualTo(GenepackRelevanceUnavailableReason.None));
            Assert.That(invalid.Results, Is.Empty);

            Assert.That(unavailable.Status, Is.EqualTo(GenepackRelevanceBatchStatus.Unavailable));
            Assert.That(unavailable.UnavailableReason, Is.EqualTo(GenepackRelevanceUnavailableReason.NoActiveMap));
            Assert.That(unavailable.Results, Is.Empty);

            Assert.That(failed.Status, Is.EqualTo(GenepackRelevanceBatchStatus.Failed));
            Assert.That(failed.UnavailableReason, Is.EqualTo(GenepackRelevanceUnavailableReason.None));
            Assert.That(failed.Results, Is.Empty);
        }

        [Test]
        public void BatchUnavailable_NoneReason_Throws()
        {
            Assert.Throws<ArgumentException>(
                (Action)(() =>
                {
                    _ = GenepackRelevanceBatchResult.CreateUnavailable(GenepackRelevanceUnavailableReason.None);
                }));
        }
    }
}