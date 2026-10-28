using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Trade;

namespace XenogermPlanner.Tests.Trade
{
    [TestFixture]
    public sealed class PlanTraderAdvisoryStockScannerTests
    {
        [Test]
        public void Scan_NullMapReturnsUnavailableEmptySnapshot()
        {
            PlanTraderAdvisoryStockSnapshot snapshot = PlanTraderAdvisoryStockScanner.Scan(null);

            Assert.That(snapshot.IsAvailable, Is.False);
            Assert.That(snapshot.Sources, Is.Empty);
        }

        [Test]
        public void Scan_ActiveMapWithoutTradersReturnsAvailableEmptySnapshot()
        {
            Map map = CreateUninitialized<Map>();

            PlanTraderAdvisoryStockSnapshot snapshot = Scan(map, [], [], _ => [], _ => []);

            Assert.That(snapshot.IsAvailable, Is.True);
            Assert.That(snapshot.Sources, Is.Empty);
        }

        [Test]
        public void Scan_ProjectsOrbitalTraderAndConcreteGenepackOffer()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");

            PlanTraderAdvisoryStockSnapshot snapshot = Scan(map, [trader], [], _ => [genepack], _ => [gene]);

            Assert.That(snapshot.Sources, Has.Count.EqualTo(1));
            PlanTraderAdvisorySourceSnapshot source = snapshot.Sources[0];
            Assert.That(source.Source, Is.SameAs(trader));
            Assert.That(source.Kind, Is.EqualTo(PlanTraderAdvisorySourceKind.Orbital));
            Assert.That(source.NavigationPawn, Is.Null);
            Assert.That(source.Offers, Has.Count.EqualTo(1));
            Assert.That(source.Offers[0].Genepack, Is.SameAs(genepack));
            Assert.That(source.Offers[0].Genes, Is.EquivalentTo([gene]));
        }

        [Test]
        public void Scan_ProjectsCaravanTraderWithExactNavigationPawn()
        {
            Map map = CreateUninitialized<Map>();
            Pawn trader = CreateUninitialized<Pawn>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");

            PlanTraderAdvisoryStockSnapshot snapshot = Scan(map, [], [trader], _ => [genepack], _ => [gene]);

            Assert.That(snapshot.Sources, Has.Count.EqualTo(1));
            PlanTraderAdvisorySourceSnapshot source = snapshot.Sources[0];
            Assert.That(source.Source, Is.SameAs(trader));
            Assert.That(source.Kind, Is.EqualTo(PlanTraderAdvisorySourceKind.Caravan));
            Assert.That(source.NavigationPawn, Is.SameAs(trader));
        }

        [Test]
        public void Scan_IgnoresNonGenepackGoods()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Pawn nonGenepack = CreateUninitialized<Pawn>();
            var geneReads = 0;

            PlanTraderAdvisoryStockSnapshot snapshot = Scan(
                map,
                [trader],
                [],
                _ => [nonGenepack],
                _ =>
                {
                    geneReads++;
                    return [];
                });

            Assert.That(geneReads, Is.Zero);
            Assert.That(snapshot.Sources, Has.Count.EqualTo(1));
            Assert.That(snapshot.Sources[0].Offers, Is.Empty);
        }

        [Test]
        public void Scan_DeduplicatesRepeatedExactGenepackReference()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            var geneReads = 0;

            PlanTraderAdvisoryStockSnapshot snapshot = Scan(
                map,
                [trader],
                [],
                _ => [genepack, genepack],
                _ =>
                {
                    geneReads++;
                    return [gene];
                });

            Assert.That(geneReads, Is.EqualTo(1));
            Assert.That(snapshot.Sources[0].Offers, Has.Count.EqualTo(1));
        }

        [Test]
        public void Scan_PreservesDistinctPhysicalGenepacksWithEquivalentComposition()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack firstGenepack = CreateUninitialized<Genepack>();
            Genepack secondGenepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");

            PlanTraderAdvisoryStockSnapshot snapshot = Scan(
                map,
                [trader],
                [],
                _ => [firstGenepack, secondGenepack],
                _ => [gene]);

            Assert.That(snapshot.Sources[0].Offers, Has.Count.EqualTo(2));
            Assert.That(snapshot.Sources[0].Offers[0].Genepack, Is.SameAs(firstGenepack));
            Assert.That(snapshot.Sources[0].Offers[1].Genepack, Is.SameAs(secondGenepack));
        }

        [Test]
        public void Scan_CopiesDistinctGeneComposition()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack genepack = CreateUninitialized<Genepack>();
            GeneDef firstGene = CreateGene("FirstGene");
            GeneDef secondGene = CreateGene("SecondGene");
            var sourceGenes = new List<GeneDef> { firstGene, firstGene, secondGene };

            PlanTraderAdvisoryStockSnapshot snapshot = Scan(map, [trader], [], _ => [genepack], _ => sourceGenes);

            sourceGenes.Clear();

            Assert.That(snapshot.Sources[0].Offers[0].Genes, Is.EquivalentTo([firstGene, secondGene]));
        }

        [Test]
        public void Scan_DeduplicatesRepeatedTraderReference()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            var goodsReads = 0;

            PlanTraderAdvisoryStockSnapshot snapshot = Scan(
                map,
                [trader, trader],
                [],
                _ =>
                {
                    goodsReads++;
                    return [];
                },
                _ => []);

            Assert.That(goodsReads, Is.EqualTo(1));
            Assert.That(snapshot.Sources, Has.Count.EqualTo(1));
        }

        [Test]
        public void Scan_SourceFailureDoesNotBlockOtherTrader()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip failingTrader = CreateUninitialized<TradeShip>();
            TradeShip healthyTrader = CreateUninitialized<TradeShip>();
            Genepack healthyGenepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            var errors = new List<string>();

            PlanTraderAdvisoryStockSnapshot snapshot = Scan(
                map,
                [failingTrader, healthyTrader],
                [],
                // Fail one source while leaving the other trader readable.
                // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
                trader =>
                {
                    if (ReferenceEquals(trader, failingTrader))
                        throw new InvalidOperationException("Expected source failure.");

                    return [healthyGenepack];
                },
                _ => [gene],
                errors.Add);

            Assert.That(snapshot.Sources, Has.Count.EqualTo(1));
            Assert.That(snapshot.Sources[0].Source, Is.SameAs(healthyTrader));
            Assert.That(errors, Has.Count.EqualTo(1));
        }

        [Test]
        public void Scan_MalformedOfferDoesNotFabricateCompositionOrBlockOtherOffer()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack malformedGenepack = CreateUninitialized<Genepack>();
            Genepack healthyGenepack = CreateUninitialized<Genepack>();
            GeneDef gene = CreateGene("GeneA");
            var errors = new List<string>();

            PlanTraderAdvisoryStockSnapshot snapshot = Scan(
                map,
                [trader],
                [],
                _ => [malformedGenepack, healthyGenepack],
                // Fail one offer while leaving the other genepack readable.
                // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
                genepack =>
                {
                    if (ReferenceEquals(genepack, malformedGenepack))
                        throw new InvalidOperationException("Expected malformed offer.");

                    return [gene];
                },
                errors.Add);

            Assert.That(snapshot.Sources, Has.Count.EqualTo(1));
            Assert.That(snapshot.Sources[0].Offers, Has.Count.EqualTo(1));
            Assert.That(snapshot.Sources[0].Offers[0].Genepack, Is.SameAs(healthyGenepack));
            Assert.That(errors, Has.Count.EqualTo(1));
        }

        [Test]
        public void Scan_EmptyOrNullGeneCompositionIsRejectedPerOffer()
        {
            Map map = CreateUninitialized<Map>();
            TradeShip trader = CreateUninitialized<TradeShip>();
            Genepack emptyGenepack = CreateUninitialized<Genepack>();
            Genepack nullGenepack = CreateUninitialized<Genepack>();
            var errors = new List<string>();

            PlanTraderAdvisoryStockSnapshot snapshot = Scan(
                map,
                [trader],
                [],
                _ => [emptyGenepack, nullGenepack],
                genepack => ReferenceEquals(genepack, emptyGenepack) ? Array.Empty<GeneDef>() : null,
                errors.Add);

            Assert.That(snapshot.Sources[0].Offers, Is.Empty);
            Assert.That(errors, Has.Count.EqualTo(2));
        }

        [Test]
        public void Scan_NullDependenciesThrowForAvailableMap()
        {
            Map map = CreateUninitialized<Map>();
            Func<Map, IEnumerable<TradeShip>> orbital = _ => [];
            Func<Map, IEnumerable<Pawn>> caravan = _ => [];
            Func<ITrader, IEnumerable<Thing>> goods = _ => [];
            Func<Genepack, IReadOnlyCollection<GeneDef>> genes = _ => [];
            Action<string> errors = _ => { };

            Assert.Throws<ArgumentNullException>(
                (Action)(() => PlanTraderAdvisoryStockScanner.Scan(map, null, caravan, goods, genes, errors)));
            Assert.Throws<ArgumentNullException>(
                (Action)(() => PlanTraderAdvisoryStockScanner.Scan(map, orbital, null, goods, genes, errors)));
            Assert.Throws<ArgumentNullException>(
                (Action)(() => PlanTraderAdvisoryStockScanner.Scan(map, orbital, caravan, null, genes, errors)));
            Assert.Throws<ArgumentNullException>(
                (Action)(() => PlanTraderAdvisoryStockScanner.Scan(map, orbital, caravan, goods, null, errors)));
            Assert.Throws<ArgumentNullException>(
                (Action)(() => PlanTraderAdvisoryStockScanner.Scan(map, orbital, caravan, goods, genes, null)));
        }

        private static PlanTraderAdvisoryStockSnapshot Scan(
            Map map,
            IEnumerable<TradeShip> orbitalTraders,
            IEnumerable<Pawn> caravanTraders,
            Func<ITrader, IEnumerable<Thing>> getGoods,
            Func<Genepack, IReadOnlyCollection<GeneDef>> getGenes,
            Action<string> reportError = null)
        {
            return PlanTraderAdvisoryStockScanner.Scan(
                map,
                _ => orbitalTraders,
                _ => caravanTraders,
                getGoods,
                getGenes,
                reportError ?? (_ => { }));
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