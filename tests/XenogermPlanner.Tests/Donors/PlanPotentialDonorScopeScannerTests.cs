using System;
using System.Collections.Generic;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Donors;

namespace XenogermPlanner.Tests.Donors
{
    [TestFixture]
    public sealed class PlanPotentialDonorScopeScannerTests
    {
        [Test]
        public void Scan_NullMapReturnsUnavailableEmptySnapshot()
        {
            PlanPotentialDonorScopeSnapshot snapshot = PlanPotentialDonorScopeScanner.Scan(null);

            Assert.That(snapshot.IsAvailable, Is.False);
            Assert.That(snapshot.Pawns, Is.Empty);
        }

        [Test]
        public void Scan_UsesDiscoveryAndScopeBoundaries()
        {
            Map map = PlanPotentialDonorTestData.CreateMap();
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();
            Map discoveredMap = null;
            Map policyMap = null;
            Pawn policyPawn = null;
            Pawn trackerPawn = null;

            PlanPotentialDonorScopeSnapshot snapshot = PlanPotentialDonorScopeScanner.Scan(
                map,
                currentMap =>
                {
                    discoveredMap = currentMap;
                    return [pawn];
                },
                (currentMap, candidate) =>
                {
                    policyMap = currentMap;
                    policyPawn = candidate;
                    return true;
                },
                candidate =>
                {
                    trackerPawn = candidate;
                    return true;
                });

            Assert.That(discoveredMap, Is.SameAs(map));
            Assert.That(policyMap, Is.SameAs(map));
            Assert.That(policyPawn, Is.SameAs(pawn));
            Assert.That(trackerPawn, Is.SameAs(pawn));
            Assert.That(snapshot.Pawns, Has.Count.EqualTo(1));
            Assert.That(snapshot.Pawns[0], Is.SameAs(pawn));
        }

        [Test]
        public void Scan_ExcludesPawnOutsideSpawnedCurrentMapBoundary()
        {
            Map map = PlanPotentialDonorTestData.CreateMap();
            Pawn includedPawn = PlanPotentialDonorTestData.CreatePawn();
            Pawn excludedPawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorScopeSnapshot snapshot = PlanPotentialDonorTestData.ScanScope(
                map,
                [includedPawn, excludedPawn],
                (_, pawn) => ReferenceEquals(pawn, includedPawn),
                _ => true);

            Assert.That(snapshot.Pawns, Has.Count.EqualTo(1));
            Assert.That(snapshot.Pawns[0], Is.SameAs(includedPawn));
        }

        [Test]
        public void Scan_ExcludesPawnWithoutGeneTracker()
        {
            Map map = PlanPotentialDonorTestData.CreateMap();
            Pawn includedPawn = PlanPotentialDonorTestData.CreatePawn();
            Pawn excludedPawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorScopeSnapshot snapshot = PlanPotentialDonorTestData.ScanScope(
                map,
                [includedPawn, excludedPawn],
                (_, _) => true,
                pawn => ReferenceEquals(pawn, includedPawn));

            Assert.That(snapshot.Pawns, Has.Count.EqualTo(1));
            Assert.That(snapshot.Pawns[0], Is.SameAs(includedPawn));
        }

        [Test]
        public void Scan_IncludesEveryPawnAcceptedBySpawnAndGeneTrackerBoundaries()
        {
            Map map = PlanPotentialDonorTestData.CreateMap();
            Pawn firstPawn = PlanPotentialDonorTestData.CreatePawn();
            Pawn secondPawn = PlanPotentialDonorTestData.CreatePawn();
            Pawn thirdPawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorScopeSnapshot snapshot = PlanPotentialDonorTestData.ScanScope(
                map,
                [firstPawn, secondPawn, thirdPawn],
                (_, _) => true,
                _ => true);

            Assert.That(snapshot.Pawns, Is.EqualTo([firstPawn, secondPawn, thirdPawn]));
        }

        [Test]
        public void Scan_IgnoresNullDiscoveredPawn()
        {
            Map map = PlanPotentialDonorTestData.CreateMap();
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();
            var spawnedPolicyCalls = 0;
            var trackerPolicyCalls = 0;

            PlanPotentialDonorScopeSnapshot snapshot = PlanPotentialDonorTestData.ScanScope(
                map,
                [null, pawn],
                (_, _) =>
                {
                    spawnedPolicyCalls++;
                    return true;
                },
                _ =>
                {
                    trackerPolicyCalls++;
                    return true;
                });

            Assert.That(spawnedPolicyCalls, Is.EqualTo(1));
            Assert.That(trackerPolicyCalls, Is.EqualTo(1));
            Assert.That(snapshot.Pawns, Has.Count.EqualTo(1));
            Assert.That(snapshot.Pawns[0], Is.SameAs(pawn));
        }

        [Test]
        public void Scan_DeduplicatesRepeatedPawnReference()
        {
            Map map = PlanPotentialDonorTestData.CreateMap();
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorScopeSnapshot snapshot = PlanPotentialDonorTestData.ScanScope(
                map,
                [pawn, pawn],
                (_, _) => true,
                _ => true);

            Assert.That(snapshot.Pawns, Has.Count.EqualTo(1));
            Assert.That(snapshot.Pawns[0], Is.SameAs(pawn));
        }

        [Test]
        public void Scan_PreservesDistinctPawnInstances()
        {
            Map map = PlanPotentialDonorTestData.CreateMap();
            Pawn firstPawn = PlanPotentialDonorTestData.CreatePawn();
            Pawn secondPawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorScopeSnapshot snapshot = PlanPotentialDonorTestData.ScanScope(
                map,
                [firstPawn, secondPawn],
                (_, _) => true,
                _ => true);

            Assert.That(snapshot.Pawns, Has.Count.EqualTo(2));
            Assert.That(snapshot.Pawns[0], Is.SameAs(firstPawn));
            Assert.That(snapshot.Pawns[1], Is.SameAs(secondPawn));
        }

        [Test]
        public void Scan_NullDiscoveryResultReturnsAvailableEmptySnapshot()
        {
            Map map = PlanPotentialDonorTestData.CreateMap();

            PlanPotentialDonorScopeSnapshot snapshot = PlanPotentialDonorScopeScanner.Scan(
                map,
                _ => null,
                (_, _) => true,
                _ => true);

            Assert.That(snapshot.IsAvailable, Is.True);
            Assert.That(snapshot.Pawns, Is.Empty);
        }

        [Test]
        public void Scan_NoDiscoveredPawnsReturnsAvailableEmptySnapshot()
        {
            Map map = PlanPotentialDonorTestData.CreateMap();

            PlanPotentialDonorScopeSnapshot snapshot = PlanPotentialDonorTestData.ScanScope(
                map,
                [],
                (_, _) => true,
                _ => true);

            Assert.That(snapshot.IsAvailable, Is.True);
            Assert.That(snapshot.Pawns, Is.Empty);
        }

        [Test]
        public void Snapshot_CopiesInputCollection()
        {
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();
            var source = new List<Pawn> { pawn };

            var snapshot = PlanPotentialDonorScopeSnapshot.CreateAvailable(source);
            source.Clear();

            Assert.That(snapshot.Pawns, Has.Count.EqualTo(1));
            Assert.That(snapshot.Pawns[0], Is.SameAs(pawn));
        }

        [Test]
        public void Snapshot_NullCollectionThrows()
        {
            Assert.Throws<ArgumentNullException>((Action)(() => PlanPotentialDonorScopeSnapshot.CreateAvailable(null)));
        }

        [Test]
        public void Snapshot_NullPawnThrows()
        {
            Assert.Throws<ArgumentException>((Action)(() => PlanPotentialDonorScopeSnapshot.CreateAvailable([null])));
        }

        [Test]
        public void Scan_NullDiscoveryBoundaryThrows()
        {
            Map map = PlanPotentialDonorTestData.CreateMap();

            Assert.Throws<ArgumentNullException>(
                (Action)(() => PlanPotentialDonorScopeScanner.Scan(map, null, (_, _) => true, _ => true)));
        }

        [Test]
        public void Scan_NullSpawnedMapBoundaryThrows()
        {
            Map map = PlanPotentialDonorTestData.CreateMap();

            Assert.Throws<ArgumentNullException>(
                (Action)(() => PlanPotentialDonorScopeScanner.Scan(map, _ => [], null, _ => true)));
        }

        [Test]
        public void Scan_NullGeneTrackerBoundaryThrows()
        {
            Map map = PlanPotentialDonorTestData.CreateMap();

            Assert.Throws<ArgumentNullException>(
                (Action)(() => PlanPotentialDonorScopeScanner.Scan(map, _ => [], (_, _) => true, null)));
        }
    }
}