using System.Linq;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Plans
{
    [TestFixture]
    public sealed class XenotypePlanCreationSourceReaderTests
    {
        [Test]
        public void TryReadSource_PremadeCopiesDistinctGenes()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            var source = new XenotypeDef
            {
                defName = "PremadeDef",
                label = "Premade",
                genes = [geneA, geneA, geneB]
            };

            bool read = XenotypePlanCreationSourceReader.TryReadSource(
                source,
                out XenotypePlanCreationSourceData sourceData,
                out XenotypePlanCreationSourceFailure failure);

            Assert.That(read, Is.True);
            Assert.That(failure, Is.EqualTo(XenotypePlanCreationSourceFailure.None));
            Assert.That(sourceData.Name, Is.EqualTo("Premade"));
            Assert.That(sourceData.DesiredGenes.Select(gene => gene.defName), Is.EquivalentTo(["GeneA", "GeneB"]));
        }

        [Test]
        public void TryReadSource_PremadePreservesDistinctConflictingGenes()
        {
            GeneDef first = PlanTestData.CreateGene("First");
            GeneDef second = PlanTestData.CreateGene("Second");
            first.exclusionTags = ["Conflict"];
            second.exclusionTags = ["Conflict"];
            var source = new XenotypeDef
            {
                defName = "PremadeDef",
                label = "Premade",
                genes = [first, second]
            };

            bool read = XenotypePlanCreationSourceReader.TryReadSource(
                source,
                out XenotypePlanCreationSourceData sourceData,
                out XenotypePlanCreationSourceFailure failure);

            Assert.That(read, Is.True);
            Assert.That(failure, Is.EqualTo(XenotypePlanCreationSourceFailure.None));
            Assert.That(sourceData.DesiredGenes, Is.EquivalentTo([first, second]));
        }

        [Test]
        public void TryReadSource_CustomCopiesVisibleGeneComposition()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            var source = new CustomXenotype
            {
                name = "Custom",
                genes = [geneA]
            };

            bool read = XenotypePlanCreationSourceReader.TryReadSource(
                source,
                out XenotypePlanCreationSourceData sourceData,
                out XenotypePlanCreationSourceFailure failure);

            source.genes.Add(geneB);

            Assert.That(read, Is.True);
            Assert.That(failure, Is.EqualTo(XenotypePlanCreationSourceFailure.None));
            Assert.That(sourceData.Name, Is.EqualTo("Custom"));
            Assert.That(sourceData.DesiredGenes.Select(gene => gene.defName), Is.EquivalentTo(["GeneA"]));
            Assert.That(source.genes.Select(gene => gene.defName), Is.EquivalentTo(["GeneA", "GeneB"]));
        }

        [Test]
        public void TryReadSource_CustomTrimsNameAndNormalizesDuplicateGenes()
        {
            GeneDef gene = PlanTestData.CreateGene("GeneA");
            var source = new CustomXenotype
            {
                name = "  Custom  ",
                genes = [gene, gene]
            };

            bool read = XenotypePlanCreationSourceReader.TryReadSource(
                source,
                out XenotypePlanCreationSourceData sourceData,
                out XenotypePlanCreationSourceFailure failure);

            Assert.That(read, Is.True);
            Assert.That(failure, Is.EqualTo(XenotypePlanCreationSourceFailure.None));
            Assert.That(sourceData.Name, Is.EqualTo("Custom"));
            Assert.That(sourceData.DesiredGenes, Is.EquivalentTo([gene]));
        }

        [Test]
        public void TryReadSource_RejectsNullPremadeSource()
        {
            bool read = XenotypePlanCreationSourceReader.TryReadSource(
                (XenotypeDef)null,
                out XenotypePlanCreationSourceData sourceData,
                out XenotypePlanCreationSourceFailure failure);

            Assert.That(read, Is.False);
            Assert.That(sourceData, Is.Null);
            Assert.That(failure, Is.EqualTo(XenotypePlanCreationSourceFailure.SourceUnavailable));
        }

        [Test]
        public void TryReadSource_RejectsPremadeWithoutGeneCollection()
        {
            var source = new XenotypeDef
            {
                defName = "PremadeDef",
                label = "Premade",
                genes = null
            };

            AssertPremadeFailure(source, XenotypePlanCreationSourceFailure.InvalidSourceData);
        }

        [Test]
        public void TryReadSource_RejectsEmptyPremadeSource()
        {
            var source = new XenotypeDef
            {
                defName = "PremadeDef",
                label = "Premade",
                genes = []
            };

            AssertPremadeFailure(source, XenotypePlanCreationSourceFailure.EmptySource);
        }

        [Test]
        public void TryReadSource_RejectsPremadeNullGene()
        {
            var source = new XenotypeDef
            {
                defName = "PremadeDef",
                label = "Premade",
                genes = [PlanTestData.CreateGene("GeneA"), null]
            };

            AssertPremadeFailure(source, XenotypePlanCreationSourceFailure.InvalidSourceData);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void TryReadSource_RejectsInvalidCustomName(string name)
        {
            var source = new CustomXenotype
            {
                name = name,
                genes = [PlanTestData.CreateGene("GeneA")]
            };

            AssertCustomFailure(source, XenotypePlanCreationSourceFailure.InvalidSourceData);
        }

        [Test]
        public void TryReadSource_RejectsEmptyCustomSource()
        {
            var source = new CustomXenotype
            {
                name = "Custom",
                genes = []
            };

            AssertCustomFailure(source, XenotypePlanCreationSourceFailure.EmptySource);
        }

        [Test]
        public void TryReadSource_RejectsCustomNullGene()
        {
            var source = new CustomXenotype
            {
                name = "Custom",
                genes = [PlanTestData.CreateGene("GeneA"), null]
            };

            AssertCustomFailure(source, XenotypePlanCreationSourceFailure.InvalidSourceData);
        }

        private static void AssertPremadeFailure(XenotypeDef source, XenotypePlanCreationSourceFailure expectedFailure)
        {
            bool read = XenotypePlanCreationSourceReader.TryReadSource(
                source,
                out XenotypePlanCreationSourceData sourceData,
                out XenotypePlanCreationSourceFailure failure);

            Assert.That(read, Is.False);
            Assert.That(sourceData, Is.Null);
            Assert.That(failure, Is.EqualTo(expectedFailure));
        }

        private static void AssertCustomFailure(
            CustomXenotype source,
            XenotypePlanCreationSourceFailure expectedFailure)
        {
            bool read = XenotypePlanCreationSourceReader.TryReadSource(
                source,
                out XenotypePlanCreationSourceData sourceData,
                out XenotypePlanCreationSourceFailure failure);

            Assert.That(read, Is.False);
            Assert.That(sourceData, Is.Null);
            Assert.That(failure, Is.EqualTo(expectedFailure));
        }
    }
}