using System;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Tests.Genes
{
    [TestFixture]
    public sealed class PlanGenepackInventoryPolicyTests
    {
        [Test]
        public void ShouldTraverseHolder_PassingShipReturnsFalse()
        {
            bool shouldTraverse = PlanGenepackInventoryPolicy.ShouldTraverseHolder(
                GenepackInventoryTestData.CreatePassingShip());

            Assert.That(shouldTraverse, Is.False);
        }

        [Test]
        public void ShouldTraverseHolder_FactionlessHolderReturnsTrue()
        {
            IThingHolder holder = GenepackInventoryTestData.CreatePawn();
            Faction playerFaction = GenepackInventoryTestData.CreateFaction();

            bool shouldTraverse = PlanGenepackInventoryPolicy.ShouldTraverseHolder(holder, _ => null, playerFaction);

            Assert.That(shouldTraverse, Is.True);
        }

        [Test]
        public void ShouldTraverseHolder_PlayerFactionHolderReturnsTrue()
        {
            IThingHolder holder = GenepackInventoryTestData.CreatePawn();
            Faction playerFaction = GenepackInventoryTestData.CreateFaction();

            bool shouldTraverse =
                PlanGenepackInventoryPolicy.ShouldTraverseHolder(holder, _ => playerFaction, playerFaction);

            Assert.That(shouldTraverse, Is.True);
        }

        [Test]
        public void ShouldTraverseHolder_ForeignFactionHolderReturnsFalse()
        {
            IThingHolder holder = GenepackInventoryTestData.CreatePawn();
            Faction playerFaction = GenepackInventoryTestData.CreateFaction();
            Faction foreignFaction = GenepackInventoryTestData.CreateFaction();

            bool shouldTraverse = PlanGenepackInventoryPolicy.ShouldTraverseHolder(
                holder,
                _ => foreignFaction,
                playerFaction);

            Assert.That(shouldTraverse, Is.False);
        }

        [Test]
        public void ShouldInclude_CurrentMapPhysicalNonEmptyGenepackReturnsTrue()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            GenepackInventoryTestData.CandidateFixture fixture = GenepackInventoryTestData.CreateCandidate(
                map,
                GenepackInventoryTestData.CreateGene());

            bool shouldInclude = GenepackInventoryTestData.EvaluatePolicy(map, fixture);

            Assert.That(shouldInclude, Is.True);
        }

        [Test]
        public void ShouldInclude_LooseSpawnedGenepackReturnsTrue()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            GenepackInventoryTestData.CandidateFixture fixture = GenepackInventoryTestData.CreateCandidate(
                map,
                true,
                GenepackInventoryTestData.CreateGene());

            bool shouldInclude = GenepackInventoryTestData.EvaluatePolicy(map, fixture);

            Assert.That(shouldInclude, Is.True);
        }

        [Test]
        public void ShouldInclude_HeldGenepackWithSpawnedCurrentMapAncestorReturnsTrue()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            GenepackInventoryTestData.CandidateFixture fixture = GenepackInventoryTestData.CreateCandidate(
                map,
                true,
                GenepackInventoryTestData.CreateGene());

            bool shouldInclude = GenepackInventoryTestData.EvaluatePolicy(map, fixture);

            Assert.That(shouldInclude, Is.True);
        }

        [Test]
        public void ShouldInclude_ForeignFactionOwnerReturnsFalse()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            GenepackInventoryTestData.CandidateFixture fixture = GenepackInventoryTestData.CreateCandidate(
                map,
                GenepackInventoryTestData.CreateGene());

            fixture.HasForeignFactionOwner = true;

            bool shouldInclude = GenepackInventoryTestData.EvaluatePolicy(map, fixture);

            Assert.That(shouldInclude, Is.False);
        }

        [Test]
        public void ShouldInclude_GenepackOnAnotherMapReturnsFalse()
        {
            Map scannedMap = GenepackInventoryTestData.CreateMap();
            Map otherMap = GenepackInventoryTestData.CreateMap();
            GenepackInventoryTestData.CandidateFixture fixture = GenepackInventoryTestData.CreateCandidate(
                otherMap,
                GenepackInventoryTestData.CreateGene());

            bool shouldInclude = GenepackInventoryTestData.EvaluatePolicy(scannedMap, fixture);

            Assert.That(shouldInclude, Is.False);
        }

        [Test]
        public void ShouldInclude_WorldRootedOrDetachedGenepackReturnsFalse()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            GenepackInventoryTestData.CandidateFixture fixture = GenepackInventoryTestData.CreateCandidate(
                map,
                false,
                GenepackInventoryTestData.CreateGene());

            bool shouldInclude = GenepackInventoryTestData.EvaluatePolicy(map, fixture);

            Assert.That(shouldInclude, Is.False);
        }

        [Test]
        public void ShouldInclude_EmptyGenepackReturnsFalse()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            GenepackInventoryTestData.CandidateFixture fixture = GenepackInventoryTestData.CreateCandidate(map);

            bool shouldInclude = GenepackInventoryTestData.EvaluatePolicy(map, fixture);

            Assert.That(shouldInclude, Is.False);
        }

        [Test]
        public void ShouldInclude_UnavailableGeneCollectionReturnsFalse()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            GenepackInventoryTestData.CandidateFixture fixture = GenepackInventoryTestData.CreateCandidate(
                map,
                GenepackInventoryTestData.CreateGene());

            fixture.Genes = null;

            bool shouldInclude = GenepackInventoryTestData.EvaluatePolicy(map, fixture);

            Assert.That(shouldInclude, Is.False);
        }

        [Test]
        public void ShouldInclude_GeneCollectionContainingOnlyNullValuesReturnsFalse()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            GenepackInventoryTestData.CandidateFixture fixture = GenepackInventoryTestData.CreateCandidate(map, [null]);

            bool shouldInclude = GenepackInventoryTestData.EvaluatePolicy(map, fixture);

            Assert.That(shouldInclude, Is.False);
        }

        [Test]
        public void ShouldInclude_DuplicateGenesStillProduceNonEmptyCandidate()
        {
            Map map = GenepackInventoryTestData.CreateMap();
            GeneDef gene = GenepackInventoryTestData.CreateGene();
            GenepackInventoryTestData.CandidateFixture fixture =
                GenepackInventoryTestData.CreateCandidate(map, gene, gene);

            bool shouldInclude = GenepackInventoryTestData.EvaluatePolicy(map, fixture);

            Assert.That(shouldInclude, Is.True);
        }

        [Test]
        public void ShouldInclude_NullGenepackReturnsFalse()
        {
            Map map = GenepackInventoryTestData.CreateMap();

            bool shouldInclude = PlanGenepackInventoryPolicy.ShouldInclude(map, null);

            Assert.That(shouldInclude, Is.False);
        }

        [Test]
        public void ShouldInclude_NullMapThrows()
        {
            Genepack genepack = GenepackInventoryTestData.CreateGenepack();

            Assert.Throws<ArgumentNullException>(
                (Action)(() => PlanGenepackInventoryPolicy.ShouldInclude(null, genepack)));
        }
    }
}