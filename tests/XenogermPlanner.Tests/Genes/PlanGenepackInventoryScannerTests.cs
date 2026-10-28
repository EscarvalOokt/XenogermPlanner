using System;
using System.Collections.Generic;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Tests.Genes
{
    [TestFixture]
    public sealed class PlanGenepackInventoryScannerTests
    {
        [Test]
        public void Scan_NullMapReturnsUnavailableEmptySnapshot()
        {
            PlanGenepackInventorySnapshot snapshot = PlanGenepackInventoryScanner.Scan(null);

            Assert.That(snapshot.IsAvailable, Is.False);
            Assert.That(snapshot.Genepacks, Is.Empty);
        }

        [Test]
        public void Scan_UsesDiscoveryBoundaryAndHolderTraversalPolicy()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            Genepack genepack = GenepackInventoryTestData.CreateGenepack();
            Map discoveredMap = null;
            Predicate<IThingHolder> holderPassCheck = null;

            PlanGenepackInventorySnapshot snapshot = PlanGenepackInventoryScanner.Scan(
                map,
                (currentMap, output, passCheck) =>
                {
                    discoveredMap = currentMap;
                    holderPassCheck = passCheck;
                    output.Add(genepack);
                },
                (_, _) => true);

            Assert.That(discoveredMap, Is.SameAs(map));
            Assert.That(holderPassCheck, Is.Not.Null);
            Assert.That(holderPassCheck(GenepackInventoryTestData.CreatePassingShip()), Is.False);
            Assert.That(holderPassCheck(GenepackInventoryTestData.CreatePawn()), Is.True);
            Assert.That(snapshot.Genepacks, Has.Count.EqualTo(1));
            Assert.That(snapshot.Genepacks[0], Is.SameAs(genepack));
        }

        [Test]
        public void Scan_IncludesAcceptedSpawnedAndHeldCandidates()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            Genepack spawnedGenepack = GenepackInventoryTestData.CreateGenepack();
            Genepack heldGenepack = GenepackInventoryTestData.CreateGenepack();

            PlanGenepackInventorySnapshot snapshot = GenepackInventoryTestData.Scan(
                map,
                [spawnedGenepack, heldGenepack],
                (_, _) => true);

            Assert.That(snapshot.IsAvailable, Is.True);
            Assert.That(snapshot.Genepacks, Has.Count.EqualTo(2));
            Assert.That(ContainsReference(snapshot.Genepacks, spawnedGenepack), Is.True);
            Assert.That(ContainsReference(snapshot.Genepacks, heldGenepack), Is.True);
        }

        [Test]
        public void Scan_ExcludesCandidateRejectedByProductPolicy()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            Genepack includedGenepack = GenepackInventoryTestData.CreateGenepack();
            Genepack excludedGenepack = GenepackInventoryTestData.CreateGenepack();

            PlanGenepackInventorySnapshot snapshot = GenepackInventoryTestData.Scan(
                map,
                [includedGenepack, excludedGenepack],
                (_, genepack) => !ReferenceEquals(genepack, excludedGenepack));

            Assert.That(snapshot.Genepacks, Has.Count.EqualTo(1));
            Assert.That(snapshot.Genepacks[0], Is.SameAs(includedGenepack));
        }

        [Test]
        public void Scan_DeduplicatesPhysicalGenepackReferences()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            Genepack genepack = GenepackInventoryTestData.CreateGenepack();

            PlanGenepackInventorySnapshot snapshot = GenepackInventoryTestData.Scan(
                map,
                [genepack, genepack],
                (_, _) => true);

            Assert.That(snapshot.Genepacks, Has.Count.EqualTo(1));
            Assert.That(snapshot.Genepacks[0], Is.SameAs(genepack));
        }

        [Test]
        public void Scan_PreservesDistinctPhysicalGenepackReferences()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            Genepack firstGenepack = GenepackInventoryTestData.CreateGenepack();
            Genepack secondGenepack = GenepackInventoryTestData.CreateGenepack();

            PlanGenepackInventorySnapshot snapshot = GenepackInventoryTestData.Scan(
                map,
                [firstGenepack, secondGenepack],
                (_, _) => true);

            Assert.That(snapshot.Genepacks, Has.Count.EqualTo(2));
            Assert.That(ContainsReference(snapshot.Genepacks, firstGenepack), Is.True);
            Assert.That(ContainsReference(snapshot.Genepacks, secondGenepack), Is.True);
        }

        [Test]
        public void Scan_IgnoresNullDiscoveredCandidate()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            Genepack genepack = GenepackInventoryTestData.CreateGenepack();
            var policyEvaluationCount = 0;

            PlanGenepackInventorySnapshot snapshot = GenepackInventoryTestData.Scan(
                map,
                [null, genepack],
                (_, _) =>
                {
                    policyEvaluationCount++;
                    return true;
                });

            Assert.That(policyEvaluationCount, Is.EqualTo(1));
            Assert.That(snapshot.Genepacks, Has.Count.EqualTo(1));
            Assert.That(snapshot.Genepacks[0], Is.SameAs(genepack));
        }

        [Test]
        public void Scan_NoDiscoveredCandidatesReturnsAvailableEmptySnapshot()
        {
            Map map = GenepackInventoryTestData.CreateMap();

            PlanGenepackInventorySnapshot snapshot = GenepackInventoryTestData.Scan(map, [], (_, _) => true);

            Assert.That(snapshot.IsAvailable, Is.True);
            Assert.That(snapshot.Genepacks, Is.Empty);
        }

        [Test]
        public void Scan_NullDiscoveryBoundaryThrows()
        {
            Map map = GenepackInventoryTestData.CreateMap();

            Assert.Throws<ArgumentNullException>(
                (Action)(() => PlanGenepackInventoryScanner.Scan(map, null, (_, _) => true)));
        }

        [Test]
        public void Scan_NullProductPolicyThrows()
        {
            Map map = GenepackInventoryTestData.CreateMap();

            Assert.Throws<ArgumentNullException>(
                (Action)(() => PlanGenepackInventoryScanner.Scan(map, (_, _, _) => { }, null)));
        }

        private static bool ContainsReference(IReadOnlyList<Genepack> genepacks, Genepack expectedGenepack)
        {
            foreach (Genepack t in genepacks)
            {
                if (ReferenceEquals(t, expectedGenepack))
                    return true;
            }

            return false;
        }
    }
}