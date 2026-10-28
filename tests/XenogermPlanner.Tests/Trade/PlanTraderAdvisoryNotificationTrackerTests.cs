using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Plans;
using XenogermPlanner.Trade;

namespace XenogermPlanner.Tests.Trade
{
    [TestFixture]
    public sealed class PlanTraderAdvisoryNotificationTrackerTests
    {
        [Test]
        public void EstablishBaselineAcknowledgesEveryCurrentRelevantPairWithoutNotification()
        {
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack firstPack = CreateUninitialized<Genepack>();
            Genepack secondPack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan firstPlan = CreatePlan("plan-a", "Alpha", gene);
            XenogermPlan secondPlan = CreatePlan("plan-b", "Beta", gene);
            PlanTraderAdvisoryOfferSnapshot firstOffer = Offer(firstPack, gene);
            PlanTraderAdvisoryOfferSnapshot secondOffer = Offer(secondPack, gene);
            PlanTraderAdvisorySourceState state = State(
                trader,
                [firstOffer, secondOffer],
                new PlanTraderAdvisoryOfferAnalysis(firstOffer, [firstPlan, secondPlan]),
                new PlanTraderAdvisoryOfferAnalysis(secondOffer, [firstPlan]));

            PlanTraderAdvisoryNotificationTracker.EstablishBaseline(state);

            Assert.That(state.IsAcknowledged(firstPack, firstPlan.Id), Is.True);
            Assert.That(state.IsAcknowledged(firstPack, secondPlan.Id), Is.True);
            Assert.That(state.IsAcknowledged(secondPack, firstPlan.Id), Is.True);
            Assert.That(state.IsPending(firstPack, firstPlan.Id), Is.False);
            Assert.That(PlanTraderAdvisoryNotificationTracker.Reconcile(state, canTradeNow: true), Is.Null);
        }

        [Test]
        public void Reconcile_WhenTraderCannotTradeKeepsCurrentUnacknowledgedPairsPending()
        {
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("plan-a", "Alpha", gene);
            PlanTraderAdvisoryOfferSnapshot offer = Offer(genepack, gene);
            PlanTraderAdvisorySourceState state = State(
                trader,
                [offer],
                new PlanTraderAdvisoryOfferAnalysis(offer, [plan]));

            PlanTraderAdvisoryNotification notification =
                PlanTraderAdvisoryNotificationTracker.Reconcile(state, canTradeNow: false);

            Assert.That(notification, Is.Null);
            Assert.That(state.IsPending(genepack, plan.Id), Is.True);
            Assert.That(state.IsAcknowledged(genepack, plan.Id), Is.False);
        }

        [Test]
        public void Reconcile_WhenTraderCanTradeAggregatesPhysicalOffersAndMarkDeliveredAcknowledgesPairs()
        {
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack firstPack = CreateUninitialized<Genepack>();
            Genepack secondPack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan firstPlan = CreatePlan("plan-a", "Alpha", gene);
            XenogermPlan secondPlan = CreatePlan("plan-b", "Beta", gene);
            PlanTraderAdvisoryOfferSnapshot firstOffer = Offer(firstPack, gene);
            PlanTraderAdvisoryOfferSnapshot secondOffer = Offer(secondPack, gene);
            PlanTraderAdvisorySourceState state = State(
                trader,
                [firstOffer, secondOffer],
                new PlanTraderAdvisoryOfferAnalysis(firstOffer, [firstPlan, secondPlan]),
                new PlanTraderAdvisoryOfferAnalysis(secondOffer, [firstPlan]));

            PlanTraderAdvisoryNotification notification =
                PlanTraderAdvisoryNotificationTracker.Reconcile(state, canTradeNow: true);

            Assert.That(notification, Is.Not.Null);
            Assert.That(notification.OfferCount, Is.EqualTo(2));
            Assert.That(notification.Offers[0].MatchingPlans, Is.EqualTo([firstPlan, secondPlan]));
            Assert.That(notification.Offers[1].MatchingPlans, Is.EqualTo([firstPlan]));
            Assert.That(state.IsPending(firstPack, firstPlan.Id), Is.True);
            Assert.That(state.IsPending(firstPack, secondPlan.Id), Is.True);
            Assert.That(state.IsPending(secondPack, firstPlan.Id), Is.True);

            PlanTraderAdvisoryNotificationTracker.MarkDelivered(state, notification);

            Assert.That(state.IsAcknowledged(firstPack, firstPlan.Id), Is.True);
            Assert.That(state.IsAcknowledged(firstPack, secondPlan.Id), Is.True);
            Assert.That(state.IsAcknowledged(secondPack, firstPlan.Id), Is.True);
            Assert.That(state.IsPending(firstPack, firstPlan.Id), Is.False);
            Assert.That(state.IsPending(firstPack, secondPlan.Id), Is.False);
            Assert.That(state.IsPending(secondPack, firstPlan.Id), Is.False);
            Assert.That(PlanTraderAdvisoryNotificationTracker.Reconcile(state, canTradeNow: true), Is.Null);
        }

        [Test]
        public void Reconcile_UsesStablePlanIdRatherThanPlanReferenceOrDisplayName()
        {
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan baselinePlan = CreatePlan("stable-plan", "Before rename", gene);
            XenogermPlan replacementPlan = CreatePlan("stable-plan", "After rename", gene);
            PlanTraderAdvisoryOfferSnapshot offer = Offer(genepack, gene);
            PlanTraderAdvisorySourceState state = State(
                trader,
                [offer],
                new PlanTraderAdvisoryOfferAnalysis(offer, [baselinePlan]));

            PlanTraderAdvisoryNotificationTracker.EstablishBaseline(state);
            state.ReplaceAnalysis([new PlanTraderAdvisoryOfferAnalysis(offer, [replacementPlan])]);

            PlanTraderAdvisoryNotification notification =
                PlanTraderAdvisoryNotificationTracker.Reconcile(state, canTradeNow: true);

            Assert.That(notification, Is.Null);
            Assert.That(state.IsAcknowledged(genepack, replacementPlan.Id), Is.True);
        }

        [Test]
        public void Reconcile_AcknowledgedPairStaysSuppressedAfterRelevanceLossAndRegain()
        {
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("plan-a", "Alpha", gene);
            PlanTraderAdvisoryOfferSnapshot offer = Offer(genepack, gene);
            PlanTraderAdvisorySourceState state = State(
                trader,
                [offer],
                new PlanTraderAdvisoryOfferAnalysis(offer, [plan]));

            PlanTraderAdvisoryNotificationTracker.EstablishBaseline(state);
            state.ReplaceAnalysis([new PlanTraderAdvisoryOfferAnalysis(offer, [])]);
            PlanTraderAdvisoryNotificationTracker.Reconcile(state, canTradeNow: true);
            state.ReplaceAnalysis([new PlanTraderAdvisoryOfferAnalysis(offer, [plan])]);

            PlanTraderAdvisoryNotification notification =
                PlanTraderAdvisoryNotificationTracker.Reconcile(state, canTradeNow: true);

            Assert.That(notification, Is.Null);
            Assert.That(state.IsAcknowledged(genepack, plan.Id), Is.True);
        }

        [Test]
        public void Reconcile_RemovesPendingPairWhenItIsNoLongerCurrentOrRelevant()
        {
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("plan-a", "Alpha", gene);
            PlanTraderAdvisoryOfferSnapshot offer = Offer(genepack, gene);
            PlanTraderAdvisorySourceState state = State(
                trader,
                [offer],
                new PlanTraderAdvisoryOfferAnalysis(offer, [plan]));

            PlanTraderAdvisoryNotificationTracker.Reconcile(state, canTradeNow: false);
            Assert.That(state.IsPending(genepack, plan.Id), Is.True);

            state.ReplaceAnalysis([new PlanTraderAdvisoryOfferAnalysis(offer, [])]);
            PlanTraderAdvisoryNotification notification =
                PlanTraderAdvisoryNotificationTracker.Reconcile(state, canTradeNow: true);

            Assert.That(notification, Is.Null);
            Assert.That(state.IsPending(genepack, plan.Id), Is.False);
        }

        [Test]
        public void Reconcile_EquivalentCompositionOnNewPhysicalGenepackCreatesNewPair()
        {
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack baselinePack = CreateUninitialized<Genepack>();
            Genepack replacementPack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("plan-a", "Alpha", gene);
            PlanTraderAdvisoryOfferSnapshot baselineOffer = Offer(baselinePack, gene);
            PlanTraderAdvisorySourceState state = State(
                trader,
                [baselineOffer],
                new PlanTraderAdvisoryOfferAnalysis(baselineOffer, [plan]));

            PlanTraderAdvisoryNotificationTracker.EstablishBaseline(state);

            PlanTraderAdvisoryOfferSnapshot replacementOffer = Offer(replacementPack, gene);
            state.UpdateStock(Source(trader, replacementOffer));
            state.ReplaceAnalysis([new PlanTraderAdvisoryOfferAnalysis(replacementOffer, [plan])]);

            PlanTraderAdvisoryNotification notification =
                PlanTraderAdvisoryNotificationTracker.Reconcile(state, canTradeNow: true);

            Assert.That(notification, Is.Not.Null);
            Assert.That(notification.OfferCount, Is.EqualTo(1));
            Assert.That(notification.Offers[0].Offer.Genepack, Is.SameAs(replacementPack));
            Assert.That(state.IsAcknowledged(baselinePack, plan.Id), Is.True);
            Assert.That(state.IsAcknowledged(replacementPack, plan.Id), Is.False);
        }

        [Test]
        public void MarkDelivered_NotificationFromDifferentSourceLifetimeIsRejectedWithoutAcknowledgement()
        {
            TradeShip firstTrader = CreateUninitialized<TradeShip>();
            TradeShip secondTrader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("plan-a", "Alpha", gene);
            PlanTraderAdvisoryOfferSnapshot offer = Offer(genepack, gene);
            PlanTraderAdvisorySourceState firstState = State(
                firstTrader,
                [offer],
                new PlanTraderAdvisoryOfferAnalysis(offer, [plan]));
            PlanTraderAdvisorySourceState secondState = State(
                secondTrader,
                [offer],
                new PlanTraderAdvisoryOfferAnalysis(offer, [plan]));
            PlanTraderAdvisoryNotification notification =
                PlanTraderAdvisoryNotificationTracker.Reconcile(firstState, canTradeNow: true);

            Assert.That(notification, Is.Not.Null);
            Assert.Throws<ArgumentException>(
                (Action)(() => PlanTraderAdvisoryNotificationTracker.MarkDelivered(secondState, notification)));
            Assert.That(firstState.IsAcknowledged(genepack, plan.Id), Is.False);
            Assert.That(firstState.IsPending(genepack, plan.Id), Is.True);
            Assert.That(secondState.IsAcknowledged(genepack, plan.Id), Is.False);
        }

        private static PlanTraderAdvisorySourceState State(
            ITrader trader,
            PlanTraderAdvisoryOfferSnapshot[] offers,
            params PlanTraderAdvisoryOfferAnalysis[] analyses)
        {
            var state = new PlanTraderAdvisorySourceState(
                new PlanTraderAdvisorySourceSnapshot(trader, PlanTraderAdvisorySourceKind.Orbital, null, offers));

            state.ReplaceAnalysis(analyses);
            return state;
        }

        private static PlanTraderAdvisorySourceSnapshot Source(
            ITrader trader,
            params PlanTraderAdvisoryOfferSnapshot[] offers)
        {
            return new PlanTraderAdvisorySourceSnapshot(trader, PlanTraderAdvisorySourceKind.Orbital, null, offers);
        }

        private static PlanTraderAdvisoryOfferSnapshot Offer(Genepack genepack, params GeneDef[] genes)
        {
            return new PlanTraderAdvisoryOfferSnapshot(genepack, genes);
        }

        private static XenogermPlan CreatePlan(string id, string name, params GeneDef[] genes)
        {
            return new XenogermPlan(id, name, genes, [], PlanReadinessMode.Coverage);
        }

        private static GeneDef CreateGene(string defName)
        {
            return new GeneDef { defName = defName };
        }

        private static T CreateUninitialized<T>() where T : class
        {
            return (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        }
    }
}