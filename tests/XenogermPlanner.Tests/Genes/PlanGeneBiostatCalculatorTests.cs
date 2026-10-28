using System;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Tests.Genes
{
    [TestFixture]
    public sealed class PlanGeneBiostatCalculatorTests
    {
        [Test]
        public void CalculateRaw_EmptyCollectionReturnsZeroValues()
        {
            PlanGeneBiostats result = PlanGeneBiostatCalculator.CalculateRaw([]);

            Assert.That(result.Complexity, Is.Zero);
            Assert.That(result.Metabolism, Is.Zero);
            Assert.That(result.ArchiteCapsules, Is.Zero);
        }

        [Test]
        public void CalculateRaw_SumsEveryGeneBiostat()
        {
            GeneDef firstGene = CreateGene("First", complexity: 3, metabolism: -2, archites: 1);
            GeneDef secondGene = CreateGene("Second", complexity: 5, metabolism: 4, archites: 2);

            PlanGeneBiostats result = PlanGeneBiostatCalculator.CalculateRaw([firstGene, secondGene]);

            Assert.That(result.Complexity, Is.EqualTo(8));
            Assert.That(result.Metabolism, Is.EqualTo(2));
            Assert.That(result.ArchiteCapsules, Is.EqualTo(3));
        }

        [Test]
        public void CalculateEffective_AppliesProjectionBeforeSumming()
        {
            GeneDef retainedGene = CreateGene("Retained", complexity: 4, metabolism: 2, archites: 1);
            GeneDef removedGene = CreateGene("Removed", complexity: 9, metabolism: -5, archites: 3);

            PlanGeneBiostats result = PlanGeneBiostatCalculator.CalculateEffective(
                [retainedGene, removedGene],
                _ => [retainedGene]);

            Assert.That(result.Complexity, Is.EqualTo(4));
            Assert.That(result.Metabolism, Is.EqualTo(2));
            Assert.That(result.ArchiteCapsules, Is.EqualTo(1));
        }

        [Test]
        public void CalculateEffective_DistinctsProjectedGenesBeforeSumming()
        {
            GeneDef firstGene = CreateGene("First", complexity: 3, metabolism: -2, archites: 1);
            GeneDef secondGene = CreateGene("Second", complexity: 5, metabolism: 4, archites: 2);

            PlanGeneBiostats result = PlanGeneBiostatCalculator.CalculateEffective(
                [firstGene, secondGene],
                _ => [firstGene, firstGene, secondGene]);

            Assert.That(result.Complexity, Is.EqualTo(8));
            Assert.That(result.Metabolism, Is.EqualTo(2));
            Assert.That(result.ArchiteCapsules, Is.EqualTo(3));
        }

        [Test]
        public void CalculateEffective_DuplicateInputRemainsSingleAfterProjectedDistinct()
        {
            GeneDef gene = CreateGene("Gene", complexity: 3, metabolism: -2, archites: 1);

            PlanGeneBiostats result = PlanGeneBiostatCalculator.CalculateEffective([gene, gene], genes => genes);

            Assert.That(result.Complexity, Is.EqualTo(3));
            Assert.That(result.Metabolism, Is.EqualTo(-2));
            Assert.That(result.ArchiteCapsules, Is.EqualTo(1));
        }

        [Test]
        public void CalculateRaw_RejectsNullCollection()
        {
            void Action() => PlanGeneBiostatCalculator.CalculateRaw(null);

            Assert.Throws<ArgumentNullException>((Action)Action);
        }

        [Test]
        public void CalculateRaw_RejectsNullGene()
        {
            void Action() => PlanGeneBiostatCalculator.CalculateRaw([null]);

            Assert.Throws<ArgumentException>((Action)Action);
        }

        [Test]
        public void CalculateEffective_RejectsNullProjection()
        {
            GeneDef gene = CreateGene("Gene", complexity: 1, metabolism: 0, archites: 0);

            void Action() => PlanGeneBiostatCalculator.CalculateEffective([gene], null);

            Assert.Throws<ArgumentNullException>((Action)Action);
        }

        [Test]
        public void CalculateEffective_RejectsNullProjectionResult()
        {
            GeneDef gene = CreateGene("Gene", complexity: 1, metabolism: 0, archites: 0);

            void Action() => PlanGeneBiostatCalculator.CalculateEffective([gene], _ => null);

            Assert.Throws<InvalidOperationException>((Action)Action);
        }

        [Test]
        public void CalculateEffective_RejectsNullProjectedGene()
        {
            GeneDef gene = CreateGene("Gene", complexity: 1, metabolism: 0, archites: 0);

            void Action() => PlanGeneBiostatCalculator.CalculateEffective([gene], _ => [null]);

            Assert.Throws<InvalidOperationException>((Action)Action);
        }

        private static GeneDef CreateGene(string defName, int complexity, int metabolism, int archites)
        {
            return new GeneDef
            {
                defName = defName,
                biostatCpx = complexity,
                biostatMet = metabolism,
                biostatArc = archites
            };
        }
    }
}