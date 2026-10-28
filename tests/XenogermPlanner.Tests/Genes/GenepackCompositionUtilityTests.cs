using System;
using System.Collections.Generic;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Genes;
using XenogermPlanner.Tests.Plans;

namespace XenogermPlanner.Tests.Genes
{
    [TestFixture]
    public sealed class GenepackCompositionUtilityTests
    {
        [Test]
        public void CreateCompositionKey_IgnoresGeneOrderAndDuplicates()
        {
            GeneDef geneA = PlanTestData.CreateGene("A");
            GeneDef geneB = PlanTestData.CreateGene("B");

            string first = GenepackCompositionUtility.CreateCompositionKey([geneB, geneA, geneA]);
            string second = GenepackCompositionUtility.CreateCompositionKey([geneA, geneB]);

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void CreateCompositionKey_UsesOrdinalDefNameOrder()
        {
            GeneDef lower = PlanTestData.CreateGene("a");
            GeneDef upper = PlanTestData.CreateGene("B");

            string key = GenepackCompositionUtility.CreateCompositionKey([lower, upper]);

            Assert.That(key, Is.EqualTo("B\u001fa"));
        }

        [Test]
        public void CompositionsMatch_UsesDistinctSetEquality()
        {
            GeneDef geneA = PlanTestData.CreateGene("A");
            GeneDef geneB = PlanTestData.CreateGene("B");

            bool matches = GenepackCompositionUtility.CompositionsMatch([geneA, geneB, geneA], [geneB, geneA]);

            Assert.That(matches, Is.True);
        }

        [Test]
        public void CompositionsMatch_ReturnsFalseForDifferentSets()
        {
            GeneDef geneA = PlanTestData.CreateGene("A");
            GeneDef geneB = PlanTestData.CreateGene("B");

            Assert.That(GenepackCompositionUtility.CompositionsMatch([geneA], [geneA, geneB]), Is.False);
        }

        [Test]
        public void TryCopyDistinctGenes_ReturnsFalseForNullGene()
        {
            bool copied = GenepackCompositionUtility.TryCopyDistinctGenes(
                new List<GeneDef> { PlanTestData.CreateGene("A"), null },
                out HashSet<GeneDef> genes);

            Assert.That(copied, Is.False);
            Assert.That(genes, Is.Empty);
        }

        [Test]
        public void TryCompositionsMatch_ReturnsFalseForInvalidComposition()
        {
            GeneDef gene = PlanTestData.CreateGene("A");

            Assert.That(GenepackCompositionUtility.TryCompositionsMatch([gene], [gene, null]), Is.False);
        }

        [Test]
        public void CopyDistinctGenes_RejectsNullGene()
        {
            void Action()
            {
                GenepackCompositionUtility.CopyDistinctGenes(new List<GeneDef> { null }, "genes");
            }

            Assert.Throws<ArgumentException>((Action)Action);
        }

        [Test]
        public void ComparePhysicalGenepacks_SortsNullLast()
        {
            Genepack genepack = GenepackInventoryTestData.CreateGenepack();

            Assert.That(GenepackCompositionUtility.ComparePhysicalGenepacks(genepack, null), Is.LessThan(0));
            Assert.That(GenepackCompositionUtility.ComparePhysicalGenepacks(null, genepack), Is.GreaterThan(0));
        }

        [Test]
        public void ComparePhysicalGenepacks_ReturnsZeroForSameReference()
        {
            Genepack genepack = GenepackInventoryTestData.CreateGenepack();

            Assert.That(GenepackCompositionUtility.ComparePhysicalGenepacks(genepack, genepack), Is.Zero);
        }
    }
}