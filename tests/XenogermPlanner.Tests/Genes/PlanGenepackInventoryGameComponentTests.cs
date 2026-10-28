using System;
using System.Collections.Generic;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Genes;

// Providers intentionally read state changed between component calls.
// ReSharper disable AccessToModifiedClosure

namespace XenogermPlanner.Tests.Genes
{
    [TestFixture]
    public sealed class PlanGenepackInventoryGameComponentTests
    {
        [Test]
        public void Snapshot_FirstReadBuildsCurrentMapSnapshot()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            Genepack genepack = GenepackInventoryTestData.CreateGenepack();
            var scanCount = 0;
            PlanGenepackInventoryGameComponent component = CreateComponent(
                () => map,
                () => 0,
                currentMap =>
                {
                    scanCount++;
                    return CreateSnapshotForMap(currentMap, map, genepack);
                });

            PlanGenepackInventorySnapshot snapshot = component.Snapshot;

            Assert.That(scanCount, Is.EqualTo(1));
            Assert.That(snapshot.IsAvailable, Is.True);
            Assert.That(snapshot.Genepacks, Has.Count.EqualTo(1));
            Assert.That(snapshot.Genepacks[0], Is.SameAs(genepack));
        }

        [Test]
        public void Snapshot_RepeatedReadBeforeFallbackDoesNotRescan()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            var currentTick = 0;
            var scanCount = 0;
            PlanGenepackInventoryGameComponent component = CreateComponent(
                () => map,
                () => currentTick,
                _ =>
                {
                    scanCount++;
                    return PlanGenepackInventorySnapshot.CreateAvailable([]);
                });

            _ = component.Snapshot;
            currentTick = PlanGenepackInventoryGameComponent.FallbackRefreshIntervalTicks - 1;
            _ = component.Snapshot;

            Assert.That(scanCount, Is.EqualTo(1));
        }

        [Test]
        public void Invalidate_DoesNotScanUntilSnapshotIsRequested()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            var scanCount = 0;
            PlanGenepackInventoryGameComponent component = CreateCountingComponent(
                () => map,
                () => 0,
                () => scanCount++);

            _ = component.Snapshot;

            component.Invalidate();

            Assert.That(scanCount, Is.EqualTo(1));

            _ = component.Snapshot;

            Assert.That(scanCount, Is.EqualTo(2));
        }

        [Test]
        public void GameComponentTick_RefreshesInvalidatedSnapshot()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            var scanCount = 0;
            PlanGenepackInventoryGameComponent component = CreateCountingComponent(
                () => map,
                () => 0,
                () => scanCount++);

            _ = component.Snapshot;
            component.Invalidate();

            component.GameComponentTick();

            Assert.That(scanCount, Is.EqualTo(2));
        }

        [Test]
        public void GameComponentTick_DoesNotRefreshBeforeFallbackBoundary()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            var currentTick = 0;
            var scanCount = 0;
            PlanGenepackInventoryGameComponent component = CreateCountingComponent(
                () => map,
                () => currentTick,
                () => scanCount++);

            _ = component.Snapshot;
            currentTick = PlanGenepackInventoryGameComponent.FallbackRefreshIntervalTicks - 1;

            component.GameComponentTick();

            Assert.That(scanCount, Is.EqualTo(1));
        }

        [Test]
        public void GameComponentTick_RefreshesAtFallbackBoundary()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            var currentTick = 0;
            var scanCount = 0;
            PlanGenepackInventoryGameComponent component = CreateCountingComponent(
                () => map,
                () => currentTick,
                () => scanCount++);

            _ = component.Snapshot;
            currentTick = PlanGenepackInventoryGameComponent.FallbackRefreshIntervalTicks;

            component.GameComponentTick();

            Assert.That(scanCount, Is.EqualTo(2));
        }

        [Test]
        public void FallbackRefresh_IsRescheduledFromLastRefresh()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            var currentTick = 0;
            var scanCount = 0;
            PlanGenepackInventoryGameComponent component = CreateCountingComponent(
                () => map,
                () => currentTick,
                () => scanCount++);

            _ = component.Snapshot;

            currentTick = PlanGenepackInventoryGameComponent.FallbackRefreshIntervalTicks;
            component.GameComponentTick();

            currentTick = PlanGenepackInventoryGameComponent.FallbackRefreshIntervalTicks * 2 - 1;
            component.GameComponentTick();

            Assert.That(scanCount, Is.EqualTo(2));

            currentTick++;
            component.GameComponentTick();

            Assert.That(scanCount, Is.EqualTo(3));
        }

        [Test]
        public void Snapshot_RefreshesAtFallbackBoundaryWithoutComponentTick()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            var currentTick = 0;
            var scanCount = 0;
            PlanGenepackInventoryGameComponent component = CreateCountingComponent(
                () => map,
                () => currentTick,
                () => scanCount++);

            _ = component.Snapshot;
            currentTick = PlanGenepackInventoryGameComponent.FallbackRefreshIntervalTicks;

            _ = component.Snapshot;

            Assert.That(scanCount, Is.EqualTo(2));
        }

        [Test]
        public void Snapshot_MapChangeRebuildsForNewMap()
        {
            Map firstMap = GenepackInventoryTestData.CreateMap();
            Map secondMap = GenepackInventoryTestData.CreateMap();
            Map currentMap = firstMap;
            Genepack firstGenepack = GenepackInventoryTestData.CreateGenepack();
            Genepack secondGenepack = GenepackInventoryTestData.CreateGenepack();
            var scanCount = 0;
            PlanGenepackInventoryGameComponent component = CreateComponent(
                () => currentMap,
                () => 0,
                map =>
                {
                    scanCount++;

                    if (ReferenceEquals(map, firstMap))
                    {
                        return PlanGenepackInventorySnapshot.CreateAvailable([firstGenepack]);
                    }

                    return PlanGenepackInventorySnapshot.CreateAvailable([secondGenepack]);
                });

            PlanGenepackInventorySnapshot firstSnapshot = component.Snapshot;
            currentMap = secondMap;
            PlanGenepackInventorySnapshot secondSnapshot = component.Snapshot;

            Assert.That(scanCount, Is.EqualTo(2));

            Assert.That(firstSnapshot.Genepacks, Has.Count.EqualTo(1));
            Assert.That(firstSnapshot.Genepacks[0], Is.SameAs(firstGenepack));

            Assert.That(secondSnapshot.Genepacks, Has.Count.EqualTo(1));
            Assert.That(secondSnapshot.Genepacks[0], Is.SameAs(secondGenepack));
        }

        [Test]
        public void Snapshot_MapToNullReplacesInventoryWithUnavailableSnapshot()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            Map currentMap = map;
            Genepack genepack = GenepackInventoryTestData.CreateGenepack();
            PlanGenepackInventoryGameComponent component = CreateComponent(
                () => currentMap,
                () => 0,
                scannedMap => CreateSnapshotForMap(scannedMap, map, genepack));

            PlanGenepackInventorySnapshot availableSnapshot = component.Snapshot;
            currentMap = null;
            PlanGenepackInventorySnapshot unavailableSnapshot = component.Snapshot;

            Assert.That(availableSnapshot.IsAvailable, Is.True);
            Assert.That(unavailableSnapshot.IsAvailable, Is.False);
            Assert.That(unavailableSnapshot.Genepacks, Is.Empty);
        }

        [Test]
        public void Snapshot_NullToMapBuildsAvailableSnapshot()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            Map currentMap = null;
            Genepack genepack = GenepackInventoryTestData.CreateGenepack();
            PlanGenepackInventoryGameComponent component = CreateComponent(
                () => currentMap,
                () => 0,
                scannedMap => CreateSnapshotForMap(scannedMap, map, genepack));

            PlanGenepackInventorySnapshot unavailableSnapshot = component.Snapshot;
            currentMap = map;
            PlanGenepackInventorySnapshot availableSnapshot = component.Snapshot;

            Assert.That(unavailableSnapshot.IsAvailable, Is.False);
            Assert.That(availableSnapshot.IsAvailable, Is.True);
            Assert.That(availableSnapshot.Genepacks, Has.Count.EqualTo(1));
            Assert.That(availableSnapshot.Genepacks[0], Is.SameAs(genepack));
        }

        [Test]
        public void Snapshot_RebuildReplacesStaleGenepacks()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            Genepack firstGenepack = GenepackInventoryTestData.CreateGenepack();
            Genepack secondGenepack = GenepackInventoryTestData.CreateGenepack();
            Genepack currentGenepack = firstGenepack;
            PlanGenepackInventoryGameComponent component = CreateComponent(
                () => map,
                () => 0,
                _ => PlanGenepackInventorySnapshot.CreateAvailable([currentGenepack]));

            PlanGenepackInventorySnapshot firstSnapshot = component.Snapshot;

            currentGenepack = secondGenepack;
            component.Invalidate();

            PlanGenepackInventorySnapshot secondSnapshot = component.Snapshot;

            Assert.That(firstSnapshot.Genepacks, Has.Count.EqualTo(1));
            Assert.That(firstSnapshot.Genepacks[0], Is.SameAs(firstGenepack));

            Assert.That(secondSnapshot.Genepacks, Has.Count.EqualTo(1));
            Assert.That(secondSnapshot.Genepacks[0], Is.SameAs(secondGenepack));
            Assert.That(ContainsReference(secondSnapshot.Genepacks, firstGenepack), Is.False);
        }

        [Test]
        public void NewComponent_RebuildsDerivedSnapshotWithoutPersistedState()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            var scanCount = 0;

            PlanGenepackInventorySnapshot ScanMap(Map _)
            {
                scanCount++;

                return PlanGenepackInventorySnapshot.CreateAvailable([]);
            }

            PlanGenepackInventoryGameComponent firstComponent = CreateComponent(() => map, () => 0, ScanMap);
            _ = firstComponent.Snapshot;

            PlanGenepackInventoryGameComponent secondComponent = CreateComponent(() => map, () => 0, ScanMap);
            _ = secondComponent.Snapshot;

            Assert.That(scanCount, Is.EqualTo(2));
        }

        private static PlanGenepackInventoryGameComponent CreateComponent(
            Func<Map> getCurrentMap,
            Func<int> getCurrentTick,
            Func<Map, PlanGenepackInventorySnapshot> scanMap)
        {
            return new PlanGenepackInventoryGameComponent(getCurrentMap, getCurrentTick, scanMap);
        }

        private static PlanGenepackInventoryGameComponent CreateCountingComponent(
            Func<Map> getCurrentMap,
            Func<int> getCurrentTick,
            Action onScan)
        {
            return CreateComponent(
                getCurrentMap,
                getCurrentTick,
                map =>
                {
                    onScan();

                    return map == null
                        ? PlanGenepackInventorySnapshot.Unavailable
                        : PlanGenepackInventorySnapshot.CreateAvailable([]);
                });
        }

        private static PlanGenepackInventorySnapshot CreateSnapshotForMap(
            Map actualMap,
            Map availableMap,
            Genepack genepack)
        {
            if (!ReferenceEquals(actualMap, availableMap))
            {
                return PlanGenepackInventorySnapshot.Unavailable;
            }

            return PlanGenepackInventorySnapshot.CreateAvailable([genepack]);
        }

        private static bool ContainsReference(IReadOnlyList<Genepack> genepacks, Genepack expectedGenepack)
        {
            foreach (Genepack t in genepacks)
            {
                if (ReferenceEquals(t, expectedGenepack))
                {
                    return true;
                }
            }

            return false;
        }
    }
}