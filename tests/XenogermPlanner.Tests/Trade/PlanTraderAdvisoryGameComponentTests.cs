using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;
using XenogermPlanner.Trade;

// Providers intentionally read state changed between component calls.
// ReSharper disable AccessToModifiedClosure

namespace XenogermPlanner.Tests.Trade
{
    [TestFixture]
    public sealed class PlanTraderAdvisoryGameComponentTests
    {
        [Test]
        public void FirstTickPollsImmediatelyAndRepeatedTickBeforeIntervalDoesNotPollAgain()
        {
            Map map = CreateUninitialized<Map>();
            var tick = 0;
            var scanCount = 0;
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ =>
                {
                    scanCount++;
                    return EmptyStock();
                });

            component.GameComponentTick();
            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks - 1;
            component.GameComponentTick();
            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();

            Assert.That(scanCount, Is.EqualTo(2));
        }

        [Test]
        public void PollCadenceStartsFromActualRefreshTick()
        {
            Map map = CreateUninitialized<Map>();
            var tick = 17;
            var scanCount = 0;
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ =>
                {
                    scanCount++;
                    return EmptyStock();
                });

            component.GameComponentTick();
            tick = 76;
            component.GameComponentTick();
            tick = 77;
            component.GameComponentTick();

            Assert.That(scanCount, Is.EqualTo(2));
        }

        [Test]
        public void MapChangeClearsOldSourcesAndForcesImmediatePoll()
        {
            Map firstMap = CreateUninitialized<Map>();
            Map secondMap = CreateUninitialized<Map>();
            TradeShip firstTrader = CreateUninitialized<TradeShip>();
            TradeShip secondTrader = CreateUninitialized<TradeShip>();
            GeneDef gene = CreateGene("GeneA");
            Map currentMap = firstMap;
            var tick = 0;
            var scanCount = 0;
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => currentMap,
                () => tick,
                map =>
                {
                    scanCount++;
                    return ReferenceEquals(map, firstMap)
                        ? Stock(Source(firstTrader, Offer(gene)))
                        : Stock(Source(secondTrader, Offer(gene)));
                });

            component.GameComponentTick();
            PlanTraderAdvisorySourceState firstState = component.Sources[0];

            tick = 1;
            currentMap = secondMap;
            component.GameComponentTick();

            Assert.That(scanCount, Is.EqualTo(2));
            Assert.That(component.Sources, Has.Count.EqualTo(1));
            Assert.That(component.Sources[0], Is.Not.SameAs(firstState));
            Assert.That(component.Sources[0].Snapshot.Source, Is.SameAs(secondTrader));
        }

        [Test]
        public void ReturningToPreviousMapStartsNewSourceLifetime()
        {
            Map firstMap = CreateUninitialized<Map>();
            Map secondMap = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            GeneDef gene = CreateGene("GeneA");
            Map currentMap = firstMap;
            var tick = 0;
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => currentMap,
                () => tick,
                map => ReferenceEquals(map, firstMap) ? Stock(Source(trader, Offer(gene))) : EmptyStock());

            component.GameComponentTick();
            PlanTraderAdvisorySourceState originalState = component.Sources[0];

            currentMap = secondMap;
            tick = 1;
            component.GameComponentTick();

            currentMap = firstMap;
            tick = 2;
            component.GameComponentTick();

            Assert.That(component.Sources, Has.Count.EqualTo(1));
            Assert.That(component.Sources[0], Is.Not.SameAs(originalState));
            Assert.That(component.Sources[0].Snapshot.Source, Is.SameAs(trader));
        }

        [Test]
        public void RepeatedPollWithSameExactTraderAndStockPreservesSourceLifetimeAndAnalysis()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("Plan", gene);
            var tick = 0;
            var evaluatorFactoryCalls = 0;
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => Stock(Source(trader, Offer(genepack, gene))),
                getPlans: () => [plan],
                createEvaluator: (_, _) =>
                {
                    evaluatorFactoryCalls++;
                    return _ => [plan];
                });

            component.GameComponentTick();
            PlanTraderAdvisorySourceState state = component.Sources[0];
            PlanTraderAdvisoryOfferAnalysis analysis = state.OfferAnalyses[0];

            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();

            Assert.That(component.Sources[0], Is.SameAs(state));
            Assert.That(component.Sources[0].OfferAnalyses[0], Is.SameAs(analysis));
            Assert.That(evaluatorFactoryCalls, Is.EqualTo(1));
        }

        [Test]
        public void NewExactTraderReferenceStartsNewSourceLifetimeEvenWithEquivalentStock()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip firstTrader = CreateUninitialized<TradeShip>();
            TradeShip secondTrader = CreateUninitialized<TradeShip>();
            GeneDef gene = CreateGene("GeneA");
            TradeShip currentTrader = firstTrader;
            var tick = 0;
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => Stock(Source(currentTrader, Offer(gene))));

            component.GameComponentTick();
            PlanTraderAdvisorySourceState firstState = component.Sources[0];

            currentTrader = secondTrader;
            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();

            Assert.That(component.Sources, Has.Count.EqualTo(1));
            Assert.That(component.Sources[0], Is.Not.SameAs(firstState));
            Assert.That(component.Sources[0].Snapshot.Source, Is.SameAs(secondTrader));
        }

        [Test]
        public void DisappearedTraderIsRemovedDuringReconciliation()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            GeneDef gene = CreateGene("GeneA");
            var includeTrader = true;
            var tick = 0;
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => includeTrader ? Stock(Source(trader, Offer(gene))) : EmptyStock());

            component.GameComponentTick();
            Assert.That(component.Sources, Has.Count.EqualTo(1));

            includeTrader = false;
            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();

            Assert.That(component.Sources, Is.Empty);
        }

        [Test]
        public void ReplacedPhysicalGenepackUpdatesCurrentStockAndReevaluates()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack firstGenepack = CreateUninitialized<Genepack>();
            Genepack secondGenepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            Genepack currentGenepack = firstGenepack;
            var tick = 0;
            var evaluatorCalls = 0;
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => Stock(Source(trader, Offer(currentGenepack, gene))),
                createEvaluator: (_, _) => _ =>
                {
                    evaluatorCalls++;
                    return [];
                });

            component.GameComponentTick();

            currentGenepack = secondGenepack;
            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();

            Assert.That(evaluatorCalls, Is.EqualTo(2));
            Assert.That(component.Sources[0].Snapshot.Offers[0].Genepack, Is.SameAs(secondGenepack));
        }

        [Test]
        public void ChangedCompositionOnSamePhysicalGenepackTriggersReevaluation()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef firstGene = CreateGene("FirstGene");
            GeneDef secondGene = CreateGene("SecondGene");
            GeneDef currentGene = firstGene;
            var tick = 0;
            var evaluatorCalls = 0;
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => Stock(Source(trader, Offer(genepack, currentGene))),
                createEvaluator: (_, _) => _ =>
                {
                    evaluatorCalls++;
                    return [];
                });

            component.GameComponentTick();

            currentGene = secondGene;
            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();

            Assert.That(evaluatorCalls, Is.EqualTo(2));
            Assert.That(component.Sources[0].Snapshot.Offers[0].Genes, Is.EquivalentTo([secondGene]));
        }

        [Test]
        public void ExplicitInvalidationReevaluatesKnownOffersWithoutStockPoll()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            GeneDef gene = CreateGene("GeneA");
            var tick = 0;
            var scanCount = 0;
            var evaluatorFactoryCalls = 0;
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ =>
                {
                    scanCount++;
                    return Stock(Source(trader, Offer(gene)));
                },
                createEvaluator: (_, _) =>
                {
                    evaluatorFactoryCalls++;
                    return _ => [];
                });

            component.GameComponentTick();
            component.Invalidate();
            tick = 1;
            component.GameComponentTick();

            Assert.That(scanCount, Is.EqualTo(1));
            Assert.That(evaluatorFactoryCalls, Is.EqualTo(2));
        }

        [Test]
        public void NewProductInventorySnapshotReferenceReevaluatesWithoutStockPoll()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            GeneDef gene = CreateGene("GeneA");
            var tick = 0;
            var scanCount = 0;
            var evaluatorFactoryCalls = 0;
            PlanGenepackInventorySnapshot inventory = EmptyInventory();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ =>
                {
                    scanCount++;
                    return Stock(Source(trader, Offer(gene)));
                },
                getInventory: () => inventory,
                createEvaluator: (_, _) =>
                {
                    evaluatorFactoryCalls++;
                    return _ => [];
                });

            component.GameComponentTick();
            inventory = EmptyInventory();
            tick = 1;
            component.GameComponentTick();

            Assert.That(scanCount, Is.EqualTo(1));
            Assert.That(evaluatorFactoryCalls, Is.EqualTo(2));
        }

        [Test]
        public void UnchangedInventoryAndStockDoNotRepeatAnalysis()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            var tick = 0;
            var evaluatorFactoryCalls = 0;
            PlanGenepackInventorySnapshot inventory = EmptyInventory();

            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => Stock(Source(trader, Offer(genepack, gene))),
                getInventory: () => inventory,
                createEvaluator: (_, _) =>
                {
                    evaluatorFactoryCalls++;
                    return _ => [];
                });

            component.GameComponentTick();

            tick = 1;
            component.GameComponentTick();

            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();

            Assert.That(evaluatorFactoryCalls, Is.EqualTo(1));
        }

        [Test]
        public void CurrentOfferAnalysisCanMatchMultiplePlans()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan firstPlan = CreatePlan("First", gene);
            XenogermPlan secondPlan = CreatePlan("Second", gene);
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => 0,
                _ => Stock(Source(trader, Offer(gene))),
                getPlans: () => [firstPlan, secondPlan],
                createEvaluator: (_, _) => _ => [firstPlan, secondPlan]);

            component.GameComponentTick();

            PlanTraderAdvisoryOfferAnalysis analysis = component.Sources[0].OfferAnalyses[0];
            Assert.That(analysis.IsRelevant, Is.True);
            Assert.That(analysis.MatchingPlans, Is.EqualTo([firstPlan, secondPlan]));
        }

        [Test]
        public void IrrelevantCurrentOfferRemainsInStockWithEmptyMatches()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            GeneDef gene = CreateGene("GeneA");
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => 0,
                _ => Stock(Source(trader, Offer(gene))));

            component.GameComponentTick();

            Assert.That(component.Sources[0].Snapshot.Offers, Has.Count.EqualTo(1));
            Assert.That(component.Sources[0].OfferAnalyses, Has.Count.EqualTo(1));
            Assert.That(component.Sources[0].OfferAnalyses[0].IsRelevant, Is.False);
        }

        [Test]
        public void FirstSuccessfulAnalysisEstablishesDeterminateBaselineEvenWithoutTraders()
        {
            Map map = CreateUninitialized<Map>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(() => map, () => 0, _ => EmptyStock());

            component.GameComponentTick();

            Assert.That(component.HasDeterminateBaseline, Is.True);
        }

        [Test]
        public void NoActiveMapDoesNotEstablishDeterminateBaseline()
        {
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => null,
                () => 0,
                _ => PlanTraderAdvisoryStockSnapshot.Unavailable);

            component.GameComponentTick();

            Assert.That(component.HasDeterminateBaseline, Is.False);
        }

        [Test]
        public void MissingPlanCollectionDoesNotEstablishDeterminateBaseline()
        {
            Map map = CreateUninitialized<Map>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => 0,
                _ => EmptyStock(),
                getPlans: () => null);

            component.GameComponentTick();

            Assert.That(component.HasDeterminateBaseline, Is.False);
        }

        [Test]
        public void UnavailableInventoryDoesNotEstablishDeterminateBaseline()
        {
            Map map = CreateUninitialized<Map>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => 0,
                _ => EmptyStock(),
                getInventory: () => PlanGenepackInventorySnapshot.Unavailable);

            component.GameComponentTick();

            Assert.That(component.HasDeterminateBaseline, Is.False);
        }

        [Test]
        public void PreviouslyDeterminateAnalysisReevaluatesAfterTemporaryContextUnavailability()
        {
            Map map = CreateUninitialized<Map>();
            PlanGenepackInventorySnapshot availableInventory = EmptyInventory();
            PlanGenepackInventorySnapshot currentInventory = availableInventory;
            var tick = 0;
            var evaluatorFactoryCalls = 0;
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => EmptyStock(),
                getInventory: () => currentInventory,
                createEvaluator: (_, _) =>
                {
                    evaluatorFactoryCalls++;
                    return _ => [];
                });

            component.GameComponentTick();
            Assert.That(evaluatorFactoryCalls, Is.EqualTo(1));

            currentInventory = PlanGenepackInventorySnapshot.Unavailable;
            tick = 1;
            component.GameComponentTick();

            currentInventory = availableInventory;
            tick = 2;
            component.GameComponentTick();

            Assert.That(evaluatorFactoryCalls, Is.EqualTo(2));
            Assert.That(component.HasDeterminateBaseline, Is.True);
        }

        [Test]
        public void BaselineEstablishesWhenPreviouslyUnavailableContextBecomesAvailable()
        {
            Map map = CreateUninitialized<Map>();
            PlanGenepackInventorySnapshot inventory = PlanGenepackInventorySnapshot.Unavailable;
            var tick = 0;
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => EmptyStock(),
                getInventory: () => inventory);

            component.GameComponentTick();
            Assert.That(component.HasDeterminateBaseline, Is.False);

            inventory = EmptyInventory();
            tick = 1;
            component.GameComponentTick();

            Assert.That(component.HasDeterminateBaseline, Is.True);
        }

        [Test]
        public void OfferAnalysisFailureDoesNotBlockOtherOffersOrEstablishFalseMatch()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            GeneDef failingGene = CreateGene("FailingGene");
            GeneDef healthyGene = CreateGene("HealthyGene");
            XenogermPlan plan = CreatePlan("Plan", healthyGene);
            var errors = new List<string>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => 0,
                _ => Stock(Source(trader, Offer(failingGene), Offer(healthyGene))),
                createEvaluator: (_, _) => composition =>
                {
                    if (ContainsReference(composition, failingGene))
                        throw new InvalidOperationException("Expected offer failure.");

                    return [plan];
                },
                reportError: errors.Add);

            component.GameComponentTick();

            Assert.That(component.Sources[0].Snapshot.Offers, Has.Count.EqualTo(2));
            Assert.That(component.Sources[0].OfferAnalyses, Has.Count.EqualTo(1));
            Assert.That(component.Sources[0].OfferAnalyses[0].MatchingPlans, Is.EqualTo([plan]));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(component.HasDeterminateBaseline, Is.True);
        }

        [Test]
        public void RelevanceEvaluatorFactoryFailureLeavesBaselineUndeterminedAndRetriesLater()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            GeneDef gene = CreateGene("GeneA");
            var failFactory = true;
            var factoryCalls = 0;
            var tick = 0;
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => Stock(Source(trader, Offer(gene))),
                createEvaluator: (_, _) =>
                {
                    factoryCalls++;

                    if (failFactory)
                        throw new InvalidOperationException("Expected factory failure.");

                    return _ => [];
                });

            component.GameComponentTick();
            Assert.That(component.HasDeterminateBaseline, Is.False);

            failFactory = false;
            tick = 1;
            component.GameComponentTick();

            Assert.That(factoryCalls, Is.EqualTo(2));
            Assert.That(component.HasDeterminateBaseline, Is.True);
        }

        [Test]
        public void StockScannerFailureDoesNotDiscardExistingSourceStateAndRetriesOnNextPollBoundary()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            GeneDef gene = CreateGene("GeneA");
            var failScan = false;
            var tick = 0;
            var errors = new List<string>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ =>
                {
                    if (failScan)
                        throw new InvalidOperationException("Expected scan failure.");

                    return Stock(Source(trader, Offer(gene)));
                },
                reportError: errors.Add);

            component.GameComponentTick();
            PlanTraderAdvisorySourceState originalState = component.Sources[0];

            failScan = true;
            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();

            Assert.That(component.Sources[0], Is.SameAs(originalState));
            Assert.That(errors, Has.Count.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void InitialRelevantOfferEstablishesSilentAcknowledgedBaseline(bool canTradeNow)
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("Plan", gene);
            var notifications = new List<PlanTraderAdvisoryNotification>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => 0,
                _ => Stock(Source(trader, Offer(genepack, gene))),
                createEvaluator: (_, _) => _ => [plan],
                canTradeNow: _ => canTradeNow,
                announce: notifications.Add);

            component.GameComponentTick();

            Assert.That(component.HasDeterminateBaseline, Is.True);
            Assert.That(component.Sources[0].IsAcknowledged(genepack, plan.Id), Is.True);
            Assert.That(component.Sources[0].IsPending(genepack, plan.Id), Is.False);
            Assert.That(notifications, Is.Empty);
        }

        [Test]
        public void NewPhysicalOfferAfterBaselineProducesOneNotificationForThatOffer()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack baselinePack = CreateUninitialized<Genepack>();
            Genepack newPack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("Plan", gene);
            var tick = 0;
            var notifications = new List<PlanTraderAdvisoryNotification>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => tick < PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks
                    ? Stock(Source(trader, Offer(baselinePack, gene)))
                    : Stock(Source(trader, Offer(baselinePack, gene), Offer(newPack, gene))),
                createEvaluator: (_, _) => _ => [plan],
                announce: notifications.Add);

            component.GameComponentTick();
            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();

            Assert.That(notifications, Has.Count.EqualTo(1));
            Assert.That(notifications[0].OfferCount, Is.EqualTo(1));
            Assert.That(notifications[0].Offers[0].Offer.Genepack, Is.SameAs(newPack));
            Assert.That(component.Sources[0].IsAcknowledged(baselinePack, plan.Id), Is.True);
            Assert.That(component.Sources[0].IsAcknowledged(newPack, plan.Id), Is.True);
        }

        [Test]
        public void NewRelevantPairsForOneTraderAreAggregatedIntoOneNotification()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack firstPack = CreateUninitialized<Genepack>();
            Genepack secondPack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan firstPlan = CreatePlan("First", gene);
            XenogermPlan secondPlan = CreatePlan("Second", gene);
            var tick = 0;
            var notifications = new List<PlanTraderAdvisoryNotification>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => tick < PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks
                    ? EmptyStock()
                    : Stock(Source(trader, Offer(firstPack, gene), Offer(secondPack, gene))),
                createEvaluator: (_, _) => _ => [firstPlan, secondPlan],
                announce: notifications.Add);

            component.GameComponentTick();
            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();

            Assert.That(notifications, Has.Count.EqualTo(1));
            Assert.That(notifications[0].OfferCount, Is.EqualTo(2));
            Assert.That(notifications[0].Offers[0].MatchingPlans, Has.Count.EqualTo(2));
            Assert.That(notifications[0].Offers[1].MatchingPlans, Has.Count.EqualTo(2));
        }

        [Test]
        public void NewPlanIdForExistingOfferAfterBaselineProducesNotification()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan firstPlan = CreatePlan("First", gene);
            XenogermPlan secondPlan = CreatePlan("Second", gene);
            IReadOnlyList<XenogermPlan> currentPlans = [firstPlan];
            var tick = 0;
            var notifications = new List<PlanTraderAdvisoryNotification>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => Stock(Source(trader, Offer(genepack, gene))),
                getPlans: () => currentPlans,
                createEvaluator: (plans, _) => _ => plans,
                announce: notifications.Add);

            component.GameComponentTick();

            currentPlans = [firstPlan, secondPlan];
            tick = 1;
            component.Invalidate();
            component.GameComponentTick();

            Assert.That(notifications, Has.Count.EqualTo(1));
            Assert.That(notifications[0].Offers[0].MatchingPlans, Is.EqualTo([secondPlan]));
            Assert.That(component.Sources[0].IsAcknowledged(genepack, firstPlan.Id), Is.True);
            Assert.That(component.Sources[0].IsAcknowledged(genepack, secondPlan.Id), Is.True);
        }

        [Test]
        public void RenamingAcknowledgedPlanDoesNotCreateNewNotificationIdentity()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("Before rename", gene);
            var tick = 0;
            var notifications = new List<PlanTraderAdvisoryNotification>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => Stock(Source(trader, Offer(genepack, gene))),
                createEvaluator: (_, _) => _ => [plan],
                announce: notifications.Add);

            component.GameComponentTick();

            plan.Rename("After rename");
            tick = 1;
            component.Invalidate();
            component.GameComponentTick();

            Assert.That(notifications, Is.Empty);
            Assert.That(component.Sources[0].IsAcknowledged(genepack, plan.Id), Is.True);
        }

        [Test]
        public void PendingPairDeliversWhenLaterPollFindsTraderTradeableWithoutReanalysis()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("Plan", gene);
            var tick = 0;
            var relevant = false;
            var tradeable = false;
            var evaluatorFactoryCalls = 0;
            var notifications = new List<PlanTraderAdvisoryNotification>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => Stock(Source(trader, Offer(genepack, gene))),
                createEvaluator: (_, _) =>
                {
                    evaluatorFactoryCalls++;
                    return _ => relevant ? new[] { plan } : Array.Empty<XenogermPlan>();
                },
                canTradeNow: _ => tradeable,
                announce: notifications.Add);

            component.GameComponentTick();

            relevant = true;
            tick = 1;
            component.Invalidate();
            component.GameComponentTick();

            Assert.That(component.Sources[0].IsPending(genepack, plan.Id), Is.True);
            Assert.That(notifications, Is.Empty);
            Assert.That(evaluatorFactoryCalls, Is.EqualTo(2));

            tradeable = true;
            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();

            Assert.That(notifications, Has.Count.EqualTo(1));
            Assert.That(evaluatorFactoryCalls, Is.EqualTo(2));
            Assert.That(component.Sources[0].IsAcknowledged(genepack, plan.Id), Is.True);
            Assert.That(component.Sources[0].IsPending(genepack, plan.Id), Is.False);
        }

        [Test]
        public void DeliveredPairDoesNotRepeatAfterLaterCanTradeNowTransitions()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("Plan", gene);
            var tick = 0;
            var relevant = false;
            var tradeable = true;
            var notifications = new List<PlanTraderAdvisoryNotification>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => Stock(Source(trader, Offer(genepack, gene))),
                createEvaluator: (_, _) => _ => relevant ? new[] { plan } : Array.Empty<XenogermPlan>(),
                canTradeNow: _ => tradeable,
                announce: notifications.Add);

            component.GameComponentTick();

            relevant = true;
            tick = 1;
            component.Invalidate();
            component.GameComponentTick();
            Assert.That(notifications, Has.Count.EqualTo(1));

            tradeable = false;
            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();

            tradeable = true;
            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks * 2;
            component.GameComponentTick();

            Assert.That(notifications, Has.Count.EqualTo(1));
        }

        [Test]
        public void DeliveryFailureKeepsPairPendingAndRetriesOnLaterPoll()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("Plan", gene);
            var tick = 0;
            var relevant = false;
            var deliveryAttempts = 0;
            var errors = new List<string>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => Stock(Source(trader, Offer(genepack, gene))),
                createEvaluator: (_, _) => _ => relevant ? new[] { plan } : Array.Empty<XenogermPlan>(),
                announce: _ =>
                {
                    deliveryAttempts++;

                    if (deliveryAttempts == 1)
                        throw new InvalidOperationException("Expected delivery failure.");
                },
                reportError: errors.Add);

            component.GameComponentTick();

            relevant = true;
            tick = 1;
            component.Invalidate();
            component.GameComponentTick();

            Assert.That(deliveryAttempts, Is.EqualTo(1));
            Assert.That(component.Sources[0].IsPending(genepack, plan.Id), Is.True);
            Assert.That(component.Sources[0].IsAcknowledged(genepack, plan.Id), Is.False);

            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();

            Assert.That(deliveryAttempts, Is.EqualTo(2));
            Assert.That(component.Sources[0].IsAcknowledged(genepack, plan.Id), Is.True);
            Assert.That(errors, Has.Count.EqualTo(1));
        }

        [Test]
        public void CanTradeNowFailureKeepsCurrentPairPendingAndRetriesLater()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("Plan", gene);
            var tick = 0;
            var relevant = false;
            var failCanTradeNow = true;
            var notifications = new List<PlanTraderAdvisoryNotification>();
            var errors = new List<string>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => Stock(Source(trader, Offer(genepack, gene))),
                createEvaluator: (_, _) => _ => relevant ? new[] { plan } : Array.Empty<XenogermPlan>(),
                canTradeNow: _ =>
                {
                    if (failCanTradeNow)
                        throw new InvalidOperationException("Expected CanTradeNow failure.");

                    return true;
                },
                announce: notifications.Add,
                reportError: errors.Add);

            component.GameComponentTick();

            relevant = true;
            tick = 1;
            component.Invalidate();
            component.GameComponentTick();

            Assert.That(component.Sources[0].IsPending(genepack, plan.Id), Is.True);
            Assert.That(notifications, Is.Empty);
            Assert.That(errors, Has.Count.EqualTo(1));

            failCanTradeNow = false;
            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();

            Assert.That(notifications, Has.Count.EqualTo(1));
            Assert.That(component.Sources[0].IsAcknowledged(genepack, plan.Id), Is.True);
        }

        [Test]
        public void DeliveryFailureForOneTraderDoesNotBlockAnotherTrader()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip failingTrader = CreateUninitialized<TradeShip>();
            TradeShip healthyTrader = CreateUninitialized<TradeShip>();
            Genepack failingPack = CreateUninitialized<Genepack>();
            Genepack healthyPack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("Plan", gene);
            var tick = 0;
            var relevant = false;
            var healthyDeliveries = 0;
            var errors = new List<string>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => Stock(
                    Source(failingTrader, Offer(failingPack, gene)),
                    Source(healthyTrader, Offer(healthyPack, gene))),
                createEvaluator: (_, _) => _ => relevant ? new[] { plan } : Array.Empty<XenogermPlan>(),
                // Simulate a delivery failure only for the selected trader.
                // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
                announce: notification =>
                {
                    if (ReferenceEquals(notification.Source.Source, failingTrader))
                        throw new InvalidOperationException("Expected first trader failure.");

                    healthyDeliveries++;
                },
                reportError: errors.Add);

            component.GameComponentTick();

            relevant = true;
            tick = 1;
            component.Invalidate();
            component.GameComponentTick();

            Assert.That(healthyDeliveries, Is.EqualTo(1));
            Assert.That(errors, Has.Count.EqualTo(1));

            PlanTraderAdvisorySourceState failingState = FindSource(component, failingTrader);
            PlanTraderAdvisorySourceState healthyState = FindSource(component, healthyTrader);
            Assert.That(failingState.IsPending(failingPack, plan.Id), Is.True);
            Assert.That(failingState.IsAcknowledged(failingPack, plan.Id), Is.False);
            Assert.That(healthyState.IsAcknowledged(healthyPack, plan.Id), Is.True);
        }

        [Test]
        public void PendingPairRemovedAfterRelevanceLossDoesNotDeliverWhenTraderLaterBecomesTradeable()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("Plan", gene);
            var tick = 0;
            var relevant = false;
            var tradeable = false;
            var notifications = new List<PlanTraderAdvisoryNotification>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => Stock(Source(trader, Offer(genepack, gene))),
                createEvaluator: (_, _) => _ => relevant ? new[] { plan } : Array.Empty<XenogermPlan>(),
                canTradeNow: _ => tradeable,
                announce: notifications.Add);

            component.GameComponentTick();

            relevant = true;
            tick = 1;
            component.Invalidate();
            component.GameComponentTick();
            Assert.That(component.Sources[0].IsPending(genepack, plan.Id), Is.True);

            relevant = false;
            tick = 2;
            component.Invalidate();
            component.GameComponentTick();
            Assert.That(component.Sources[0].IsPending(genepack, plan.Id), Is.False);

            tradeable = true;
            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();

            Assert.That(notifications, Is.Empty);
        }

        [Test]
        public void ActiveMapChangeEstablishesNewSilentBaselineForExistingRelevantTrader()
        {
            Map firstMap = CreateUninitialized<Map>();
            Map secondMap = CreateUninitialized<Map>();
            TradeShip firstTrader = CreateUninitialized<TradeShip>();
            TradeShip secondTrader = CreateUninitialized<TradeShip>();
            Genepack firstPack = CreateUninitialized<Genepack>();
            Genepack secondPack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("Plan", gene);
            Map currentMap = firstMap;
            var tick = 0;
            var notifications = new List<PlanTraderAdvisoryNotification>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => currentMap,
                () => tick,
                map => ReferenceEquals(map, firstMap)
                    ? Stock(Source(firstTrader, Offer(firstPack, gene)))
                    : Stock(Source(secondTrader, Offer(secondPack, gene))),
                createEvaluator: (_, _) => _ => [plan],
                announce: notifications.Add);

            component.GameComponentTick();
            Assert.That(component.Sources[0].IsAcknowledged(firstPack, plan.Id), Is.True);

            currentMap = secondMap;
            tick = 1;
            component.GameComponentTick();

            Assert.That(component.HasDeterminateBaseline, Is.True);
            Assert.That(component.Sources, Has.Count.EqualTo(1));
            Assert.That(component.Sources[0].Snapshot.Source, Is.SameAs(secondTrader));
            Assert.That(component.Sources[0].IsAcknowledged(secondPack, plan.Id), Is.True);
            Assert.That(notifications, Is.Empty);
        }

        [Test]
        public void NewTraderAppearingAfterEmptyBaselineCanNotifyImmediately()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("Plan", gene);
            var tick = 0;
            var notifications = new List<PlanTraderAdvisoryNotification>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => tick < PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks
                    ? EmptyStock()
                    : Stock(Source(trader, Offer(genepack, gene))),
                createEvaluator: (_, _) => _ => [plan],
                announce: notifications.Add);

            component.GameComponentTick();
            Assert.That(component.HasDeterminateBaseline, Is.True);

            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();

            Assert.That(notifications, Has.Count.EqualTo(1));
            Assert.That(notifications[0].Source.Source, Is.SameAs(trader));
        }

        [Test]
        public void NewConcreteTraderSourceAfterPreviousLifetimeEndedCanNotifyForSamePhysicalPair()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip firstTrader = CreateUninitialized<TradeShip>();
            TradeShip secondTrader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("Plan", gene);
            var tick = 0;
            var notifications = new List<PlanTraderAdvisoryNotification>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ =>
                {
                    if (tick < PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks)
                        return Stock(Source(firstTrader, Offer(genepack, gene)));

                    if (tick < PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks * 2)
                        return EmptyStock();

                    return Stock(Source(secondTrader, Offer(genepack, gene)));
                },
                createEvaluator: (_, _) => _ => [plan],
                announce: notifications.Add);

            component.GameComponentTick();

            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();
            Assert.That(component.Sources, Is.Empty);

            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks * 2;
            component.GameComponentTick();

            Assert.That(notifications, Has.Count.EqualTo(1));
            Assert.That(notifications[0].Source.Source, Is.SameAs(secondTrader));
        }

        [Test]
        public void RecreatedComponentWithExistingRelevantTraderUsesSilentBaseline()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("Plan", gene);
            var tick = 0;
            var offerPresent = false;
            var firstNotifications = new List<PlanTraderAdvisoryNotification>();
            PlanTraderAdvisoryGameComponent firstComponent = CreateComponent(
                () => map,
                () => tick,
                _ => offerPresent ? Stock(Source(trader, Offer(genepack, gene))) : EmptyStock(),
                createEvaluator: (_, _) => _ => [plan],
                announce: firstNotifications.Add);

            firstComponent.GameComponentTick();

            offerPresent = true;
            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            firstComponent.GameComponentTick();

            Assert.That(firstNotifications, Has.Count.EqualTo(1));
            Assert.That(firstComponent.Sources[0].IsAcknowledged(genepack, plan.Id), Is.True);

            var reloadedNotifications = new List<PlanTraderAdvisoryNotification>();
            PlanTraderAdvisoryGameComponent recreatedComponent = CreateComponent(
                () => map,
                () => tick,
                _ => Stock(Source(trader, Offer(genepack, gene))),
                createEvaluator: (_, _) => _ => [plan],
                announce: reloadedNotifications.Add);

            recreatedComponent.GameComponentTick();

            Assert.That(recreatedComponent.HasDeterminateBaseline, Is.True);
            Assert.That(recreatedComponent.Sources, Has.Count.EqualTo(1));
            Assert.That(recreatedComponent.Sources[0].IsAcknowledged(genepack, plan.Id), Is.True);
            Assert.That(recreatedComponent.Sources[0].IsPending(genepack, plan.Id), Is.False);
            Assert.That(reloadedNotifications, Is.Empty);
        }

        [Test]
        public void DeliveredPairDoesNotRepeatAfterRelevanceLossAndRegainDuringSameSourceLifetime()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("Plan", gene);
            var tick = 0;
            var relevant = false;
            var notifications = new List<PlanTraderAdvisoryNotification>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => Stock(Source(trader, Offer(genepack, gene))),
                createEvaluator: (_, _) => _ => relevant ? new[] { plan } : Array.Empty<XenogermPlan>(),
                announce: notifications.Add);

            component.GameComponentTick();

            relevant = true;
            tick = 1;
            component.Invalidate();
            component.GameComponentTick();

            Assert.That(notifications, Has.Count.EqualTo(1));
            Assert.That(component.Sources[0].IsAcknowledged(genepack, plan.Id), Is.True);

            relevant = false;
            tick = 2;
            component.Invalidate();
            component.GameComponentTick();

            relevant = true;
            tick = 3;
            component.Invalidate();
            component.GameComponentTick();

            Assert.That(notifications, Has.Count.EqualTo(1));
            Assert.That(component.Sources[0].IsAcknowledged(genepack, plan.Id), Is.True);
            Assert.That(component.Sources[0].IsPending(genepack, plan.Id), Is.False);
        }

        [Test]
        public void CaravanNotificationPreservesExactTraderPawnAsSourceAndNavigationTarget()
        {
            Map map = CreateUninitialized<Map>();
            Pawn traderPawn = CreateUninitialized<Pawn>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("Plan", gene);
            var tick = 0;
            var notifications = new List<PlanTraderAdvisoryNotification>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => tick < PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks
                    ? EmptyStock()
                    : Stock(
                        Source(traderPawn, PlanTraderAdvisorySourceKind.Caravan, traderPawn, Offer(genepack, gene))),
                createEvaluator: (_, _) => _ => [plan],
                announce: notifications.Add);

            component.GameComponentTick();

            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();

            Assert.That(notifications, Has.Count.EqualTo(1));
            Assert.That(notifications[0].Source.Source, Is.SameAs(traderPawn));
            Assert.That(notifications[0].Source.Kind, Is.EqualTo(PlanTraderAdvisorySourceKind.Caravan));
            Assert.That(notifications[0].Source.NavigationPawn, Is.SameAs(traderPawn));
        }

        [Test]
        public void NotificationDeliveryDoesNotMutatePlanOwnedState()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            var plan = new XenogermPlan(
                "plan-id",
                "Plan Name",
                [gene],
                [],
                PlanReadinessMode.Coverage,
                readinessNotificationsEnabled: false,
                hasReadinessNotificationBaseline: true,
                lastReadinessNotificationStateWasReady: true);
            var tick = 0;
            var notifications = new List<PlanTraderAdvisoryNotification>();
            PlanTraderAdvisoryGameComponent component = CreateComponent(
                () => map,
                () => tick,
                _ => tick < PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks
                    ? EmptyStock()
                    : Stock(Source(trader, Offer(genepack, gene))),
                getPlans: () => [plan],
                createEvaluator: (_, _) => _ => [plan],
                announce: notifications.Add);

            component.GameComponentTick();

            tick = PlanTraderAdvisoryGameComponent.StockRefreshIntervalTicks;
            component.GameComponentTick();

            Assert.That(notifications, Has.Count.EqualTo(1));
            Assert.That(plan.Id, Is.EqualTo("plan-id"));
            Assert.That(plan.Name, Is.EqualTo("Plan Name"));
            Assert.That(plan.ReadinessMode, Is.EqualTo(PlanReadinessMode.Coverage));
            Assert.That(plan.ReadinessNotificationsEnabled, Is.False);
            Assert.That(plan.HasReadinessNotificationBaseline, Is.True);
            Assert.That(plan.LastReadinessNotificationStateWasReady, Is.True);
            Assert.That(plan.DesiredGenes, Is.EquivalentTo([gene]));
        }

        private static PlanTraderAdvisoryGameComponent CreateComponent(
            Func<Map> getMap,
            Func<int> getTick,
            Func<Map, PlanTraderAdvisoryStockSnapshot> scanStock,
            Func<IReadOnlyList<XenogermPlan>> getPlans = null,
            Func<PlanGenepackInventorySnapshot> getInventory = null,
            Func<IReadOnlyList<XenogermPlan>, PlanGenepackInventorySnapshot,
                Func<IReadOnlyCollection<GeneDef>, IReadOnlyList<XenogermPlan>>> createEvaluator = null,
            Func<ITrader, bool> canTradeNow = null,
            Action<PlanTraderAdvisoryNotification> announce = null,
            Action<string> reportError = null)
        {
            PlanGenepackInventorySnapshot defaultInventory = getInventory == null ? EmptyInventory() : null;

            return new PlanTraderAdvisoryGameComponent(
                getMap,
                getTick,
                scanStock,
                getPlans ?? (Array.Empty<XenogermPlan>),
                getInventory ?? (() => defaultInventory),
                createEvaluator ?? ((_, _) => _ => []),
                canTradeNow ?? (_ => true),
                announce ?? (_ => { }),
                reportError ?? (_ => { }));
        }

        private static PlanGenepackInventorySnapshot EmptyInventory()
        {
            return PlanGenepackInventorySnapshot.CreateAvailable([]);
        }

        private static PlanTraderAdvisoryStockSnapshot EmptyStock()
        {
            return PlanTraderAdvisoryStockSnapshot.CreateAvailable([]);
        }

        private static PlanTraderAdvisoryStockSnapshot Stock(params PlanTraderAdvisorySourceSnapshot[] sources)
        {
            return PlanTraderAdvisoryStockSnapshot.CreateAvailable(sources);
        }

        private static PlanTraderAdvisorySourceSnapshot Source(
            ITrader trader,
            params PlanTraderAdvisoryOfferSnapshot[] offers)
        {
            return Source(trader, PlanTraderAdvisorySourceKind.Orbital, null, offers);
        }

        private static PlanTraderAdvisorySourceSnapshot Source(
            ITrader trader,
            PlanTraderAdvisorySourceKind kind,
            Pawn navigationPawn,
            params PlanTraderAdvisoryOfferSnapshot[] offers)
        {
            return new PlanTraderAdvisorySourceSnapshot(trader, kind, navigationPawn, offers);
        }

        private static PlanTraderAdvisoryOfferSnapshot Offer(GeneDef gene)
        {
            return Offer(CreateUninitialized<Genepack>(), gene);
        }

        private static PlanTraderAdvisoryOfferSnapshot Offer(Genepack genepack, params GeneDef[] genes)
        {
            return new PlanTraderAdvisoryOfferSnapshot(genepack, genes);
        }

        private static XenogermPlan CreatePlan(string name, params GeneDef[] genes)
        {
            return new XenogermPlan(name, genes, PlanReadinessMode.Coverage);
        }

        private static PlanTraderAdvisorySourceState FindSource(
            PlanTraderAdvisoryGameComponent component,
            ITrader trader)
        {
            foreach (PlanTraderAdvisorySourceState source in component.Sources)
            {
                if (ReferenceEquals(source.Snapshot.Source, trader))
                    return source;
            }

            Assert.Fail("Expected trader source was not found.");
            return null;
        }

        private static bool ContainsReference(IEnumerable<GeneDef> genes, GeneDef expected)
        {
            foreach (GeneDef gene in genes)
            {
                if (ReferenceEquals(gene, expected))
                    return true;
            }

            return false;
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