using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Plans
{
    [TestFixture]
    public sealed class CustomXenogermPlanImporterTests
    {
        [Test]
        public void TryReadSource_FlattensGeneSetsAndNormalizesDuplicateGenes()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            GeneDef geneC = PlanTestData.CreateGene("GeneC");
            CustomXenogerm source = PlanTestData.CreateCustomXenogerm(
                "Template",
                PlanTestData.CreateGeneSet(geneA),
                PlanTestData.CreateGeneSet(geneA, geneB),
                PlanTestData.CreateGeneSet(geneC));

            bool imported = CustomXenogermPlanImporter.TryReadSource(
                source,
                out CustomXenogermPlanImportData importData,
                out CustomXenogermPlanImportFailure failure);

            Assert.That(imported, Is.True);
            Assert.That(failure, Is.EqualTo(CustomXenogermPlanImportFailure.None));
            Assert.That(importData, Is.Not.Null);
            Assert.That(importData.Name, Is.EqualTo("Template"));
            Assert.That(importData.DesiredGenes.Count, Is.EqualTo(3));
            Assert.That(
                importData.DesiredGenes.Select(gene => gene.defName),
                Is.EquivalentTo(["GeneA", "GeneB", "GeneC"]));
        }

        [Test]
        public void TryReadSource_TrimsSourceName()
        {
            CustomXenogerm source = PlanTestData.CreateCustomXenogerm(
                "  Template  ",
                PlanTestData.CreateGeneSet(PlanTestData.CreateGene("GeneA")));

            bool imported = CustomXenogermPlanImporter.TryReadSource(
                source,
                out CustomXenogermPlanImportData importData,
                out CustomXenogermPlanImportFailure failure);

            Assert.That(imported, Is.True);
            Assert.That(failure, Is.EqualTo(CustomXenogermPlanImportFailure.None));
            Assert.That(importData.Name, Is.EqualTo("Template"));
        }

        [Test]
        public void TryReadSource_PreservesConflictingGenesAcrossGeneSets()
        {
            GeneDef first = PlanTestData.CreateGene("First");
            GeneDef second = PlanTestData.CreateGene("Second");
            first.exclusionTags = ["Conflict"];
            second.exclusionTags = ["Conflict"];
            CustomXenogerm source = PlanTestData.CreateCustomXenogerm(
                "Template",
                PlanTestData.CreateGeneSet(first, second),
                PlanTestData.CreateGeneSet(first));

            bool imported = CustomXenogermPlanImporter.TryReadSource(
                source,
                out CustomXenogermPlanImportData importData,
                out CustomXenogermPlanImportFailure failure);

            Assert.That(imported, Is.True);
            Assert.That(failure, Is.EqualTo(CustomXenogermPlanImportFailure.None));
            Assert.That(importData.DesiredGenes, Is.EquivalentTo([first, second]));
        }

        [Test]
        public void TryReadSource_CopiesVisibleGeneComposition()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            GeneSet sourceGeneSet = PlanTestData.CreateGeneSet(geneA);
            CustomXenogerm source = PlanTestData.CreateCustomXenogerm("Template", sourceGeneSet);

            bool imported = CustomXenogermPlanImporter.TryReadSource(
                source,
                out CustomXenogermPlanImportData importData,
                out CustomXenogermPlanImportFailure failure);

            sourceGeneSet.AddGene(geneB);

            Assert.That(imported, Is.True);
            Assert.That(failure, Is.EqualTo(CustomXenogermPlanImportFailure.None));
            Assert.That(importData.DesiredGenes.Select(gene => gene.defName), Is.EquivalentTo(["GeneA"]));
            Assert.That(
                sourceGeneSet.GenesListForReading.Select(gene => gene.defName),
                Is.EquivalentTo(["GeneA", "GeneB"]));
        }

        [Test]
        public void TryReadSource_DataRemainsIndependentAfterSourceReplacementAndRemoval()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            GeneDef geneC = PlanTestData.CreateGene("GeneC");
            CustomXenogerm source = PlanTestData.CreateCustomXenogerm(
                "Template",
                PlanTestData.CreateGeneSet(geneA, geneB));
            CustomXenogerm replacement = PlanTestData.CreateCustomXenogerm(
                "Template",
                PlanTestData.CreateGeneSet(geneC));
            var runtimeSources = new List<CustomXenogerm>
            {
                source
            };

            bool imported = CustomXenogermPlanImporter.TryReadSource(
                runtimeSources[0],
                out CustomXenogermPlanImportData importData,
                out CustomXenogermPlanImportFailure failure);

            runtimeSources[0] = replacement;

            Assert.That(imported, Is.True);
            Assert.That(failure, Is.EqualTo(CustomXenogermPlanImportFailure.None));
            Assert.That(importData.Name, Is.EqualTo("Template"));
            Assert.That(importData.DesiredGenes.Select(gene => gene.defName), Is.EquivalentTo(["GeneA", "GeneB"]));

            runtimeSources.Clear();

            Assert.That(importData.Name, Is.EqualTo("Template"));
            Assert.That(importData.DesiredGenes.Select(gene => gene.defName), Is.EquivalentTo(["GeneA", "GeneB"]));
        }

        [Test]
        public void TryReadSource_RejectsNullSource()
        {
            AssertImportFailure(null, CustomXenogermPlanImportFailure.SourceUnavailable);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void TryReadSource_RejectsInvalidSourceName(string name)
        {
            var source = new CustomXenogerm
            {
                name = name,
                genesets = [PlanTestData.CreateGeneSet(PlanTestData.CreateGene("GeneA"))]
            };

            AssertImportFailure(source, CustomXenogermPlanImportFailure.InvalidSourceData);
        }

        [Test]
        public void TryReadSource_RejectsMissingGeneSetCollection()
        {
            var source = new CustomXenogerm
            {
                name = "Template",
                genesets = null
            };

            AssertImportFailure(source, CustomXenogermPlanImportFailure.InvalidSourceData);
        }

        [Test]
        public void TryReadSource_RejectsEmptyGeneSetCollection()
        {
            var source = new CustomXenogerm
            {
                name = "Template",
                genesets = []
            };

            AssertImportFailure(source, CustomXenogermPlanImportFailure.EmptySource);
        }

        [Test]
        public void TryReadSource_RejectsNullGeneSet()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            var source = new CustomXenogerm
            {
                name = "Template",
                genesets =
                [
                    PlanTestData.CreateGeneSet(geneA),
                    null,
                    PlanTestData.CreateGeneSet(geneB)
                ]
            };

            AssertImportFailure(source, CustomXenogermPlanImportFailure.InvalidSourceData);
        }

        [Test]
        public void TryReadSource_RejectsEmptyGeneSet()
        {
            CustomXenogerm source = PlanTestData.CreateCustomXenogerm("Template", PlanTestData.CreateGeneSet());

            AssertImportFailure(source, CustomXenogermPlanImportFailure.EmptySource);
        }

        [Test]
        public void TryReadSource_RejectsNullGene()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneSet geneSet = PlanTestData.CreateGeneSet(geneA);

            geneSet.GenesListForReading.Add(null);

            CustomXenogerm source = PlanTestData.CreateCustomXenogerm("Template", geneSet);

            AssertImportFailure(source, CustomXenogermPlanImportFailure.InvalidSourceData);
        }

        private static void AssertImportFailure(CustomXenogerm source, CustomXenogermPlanImportFailure expectedFailure)
        {
            bool imported = CustomXenogermPlanImporter.TryReadSource(
                source,
                out CustomXenogermPlanImportData importData,
                out CustomXenogermPlanImportFailure failure);

            Assert.That(imported, Is.False);
            Assert.That(importData, Is.Null);
            Assert.That(failure, Is.EqualTo(expectedFailure));
        }
    }
}