using System.Collections.Generic;
using NUnit.Framework;
using Verse;
using XenogermPlanner.UI;

namespace XenogermPlanner.Tests.UI
{
    [TestFixture]
    public sealed class GeneCatalogProjectionTests
    {
        [Test]
        public void Build_ExpandedCategories_PreservesCategoryAndGeneOrder()
        {
            GeneCategoryDef categoryA = CreateCategory("CategoryA");
            GeneCategoryDef categoryB = CreateCategory("CategoryB");
            GeneDef geneA1 = CreateGene("GeneA1", categoryA);
            GeneDef geneA2 = CreateGene("GeneA2", categoryA);
            GeneDef geneB1 = CreateGene("GeneB1", categoryB);
            var collapsedCategories = new Dictionary<GeneCategoryDef, bool>
            {
                { categoryA, false },
                { categoryB, false }
            };

            List<GeneCatalogRow> rows = GeneCatalogProjection.Build(
                [geneA1, geneA2, geneB1],
                collapsedCategories,
                forceExpanded: false);

            Assert.That(rows, Has.Count.EqualTo(5));
            AssertCategoryRow(rows[0], categoryA, isExpanded: true);
            AssertGeneRow(rows[1], geneA1);
            AssertGeneRow(rows[2], geneA2);
            AssertCategoryRow(rows[3], categoryB, isExpanded: true);
            AssertGeneRow(rows[4], geneB1);
        }

        [Test]
        public void Build_CollapsedCategory_LeavesOnlyItsCategoryRow()
        {
            GeneCategoryDef categoryA = CreateCategory("CategoryA");
            GeneCategoryDef categoryB = CreateCategory("CategoryB");
            GeneDef geneA1 = CreateGene("GeneA1", categoryA);
            GeneDef geneA2 = CreateGene("GeneA2", categoryA);
            GeneDef geneB1 = CreateGene("GeneB1", categoryB);
            var collapsedCategories = new Dictionary<GeneCategoryDef, bool>
            {
                { categoryA, true },
                { categoryB, false }
            };

            List<GeneCatalogRow> rows = GeneCatalogProjection.Build(
                [geneA1, geneA2, geneB1],
                collapsedCategories,
                forceExpanded: false);

            Assert.That(rows, Has.Count.EqualTo(3));
            AssertCategoryRow(rows[0], categoryA, isExpanded: false);
            AssertCategoryRow(rows[1], categoryB, isExpanded: true);
            AssertGeneRow(rows[2], geneB1);
        }

        [Test]
        public void Build_MultipleCategoryStates_DoNotAffectNeighboringCategories()
        {
            GeneCategoryDef categoryA = CreateCategory("CategoryA");
            GeneCategoryDef categoryB = CreateCategory("CategoryB");
            GeneCategoryDef categoryC = CreateCategory("CategoryC");
            GeneDef geneA = CreateGene("GeneA", categoryA);
            GeneDef geneB = CreateGene("GeneB", categoryB);
            GeneDef geneC = CreateGene("GeneC", categoryC);
            var collapsedCategories = new Dictionary<GeneCategoryDef, bool>
            {
                { categoryA, true },
                { categoryB, false },
                { categoryC, true }
            };

            List<GeneCatalogRow> rows = GeneCatalogProjection.Build(
                [geneA, geneB, geneC],
                collapsedCategories,
                forceExpanded: false);

            Assert.That(rows, Has.Count.EqualTo(4));
            AssertCategoryRow(rows[0], categoryA, isExpanded: false);
            AssertCategoryRow(rows[1], categoryB, isExpanded: true);
            AssertGeneRow(rows[2], geneB);
            AssertCategoryRow(rows[3], categoryC, isExpanded: false);
        }

        [Test]
        public void Build_ForceExpanded_ShowsCollapsedCategoryWithoutChangingStoredState()
        {
            GeneCategoryDef category = CreateCategory("Category");
            GeneDef gene = CreateGene("Gene", category);
            var collapsedCategories = new Dictionary<GeneCategoryDef, bool>
            {
                { category, true }
            };

            List<GeneCatalogRow> rows = GeneCatalogProjection.Build([gene], collapsedCategories, forceExpanded: true);

            Assert.That(rows, Has.Count.EqualTo(2));
            AssertCategoryRow(rows[0], category, isExpanded: true);
            AssertGeneRow(rows[1], gene);
            Assert.That(collapsedCategories[category], Is.True);
        }

        [Test]
        public void Build_MissingCategoryState_TreatsCategoryAsExpanded()
        {
            GeneCategoryDef category = CreateCategory("Category");
            GeneDef gene = CreateGene("Gene", category);

            List<GeneCatalogRow> rows = GeneCatalogProjection.Build(
                [gene],
                new Dictionary<GeneCategoryDef, bool>(),
                forceExpanded: false);

            Assert.That(rows, Has.Count.EqualTo(2));
            AssertCategoryRow(rows[0], category, isExpanded: true);
            AssertGeneRow(rows[1], gene);
        }

        [Test]
        public void Build_EmptyInput_ReturnsEmptyProjection()
        {
            List<GeneCatalogRow> rows = GeneCatalogProjection.Build(
                [],
                new Dictionary<GeneCategoryDef, bool>(),
                forceExpanded: false);

            Assert.That(rows, Is.Empty);
        }

        [Test]
        public void Build_RebuildAfterStateChange_DoesNotMutatePreviousSnapshot()
        {
            GeneCategoryDef category = CreateCategory("Category");
            GeneDef gene = CreateGene("Gene", category);
            var collapsedCategories = new Dictionary<GeneCategoryDef, bool>
            {
                { category, false }
            };

            List<GeneCatalogRow> expandedRows = GeneCatalogProjection.Build(
                [gene],
                collapsedCategories,
                forceExpanded: false);

            collapsedCategories[category] = true;

            List<GeneCatalogRow> collapsedRows = GeneCatalogProjection.Build(
                [gene],
                collapsedCategories,
                forceExpanded: false);

            Assert.That(expandedRows, Has.Count.EqualTo(2));
            AssertCategoryRow(expandedRows[0], category, isExpanded: true);
            AssertGeneRow(expandedRows[1], gene);

            Assert.That(collapsedRows, Has.Count.EqualTo(1));
            AssertCategoryRow(collapsedRows[0], category, isExpanded: false);
        }

        [Test]
        public void Build_CategoryTransitions_PreserveInputSequenceWithoutRegrouping()
        {
            GeneCategoryDef categoryA = CreateCategory("CategoryA");
            GeneCategoryDef categoryB = CreateCategory("CategoryB");
            GeneDef geneA1 = CreateGene("GeneA1", categoryA);
            GeneDef geneB = CreateGene("GeneB", categoryB);
            GeneDef geneA2 = CreateGene("GeneA2", categoryA);
            var collapsedCategories = new Dictionary<GeneCategoryDef, bool>
            {
                { categoryA, false },
                { categoryB, false }
            };

            List<GeneCatalogRow> rows = GeneCatalogProjection.Build(
                [geneA1, geneB, geneA2],
                collapsedCategories,
                forceExpanded: false);

            Assert.That(rows, Has.Count.EqualTo(6));
            AssertCategoryRow(rows[0], categoryA, isExpanded: true);
            AssertGeneRow(rows[1], geneA1);
            AssertCategoryRow(rows[2], categoryB, isExpanded: true);
            AssertGeneRow(rows[3], geneB);
            AssertCategoryRow(rows[4], categoryA, isExpanded: true);
            AssertGeneRow(rows[5], geneA2);
        }

        private static void AssertCategoryRow(GeneCatalogRow row, GeneCategoryDef expectedCategory, bool isExpanded)
        {
            Assert.That(row.Kind, Is.EqualTo(GeneCatalogRowKind.Category));
            Assert.That(row.Category, Is.SameAs(expectedCategory));
            Assert.That(row.Gene, Is.Null);
            Assert.That(row.IsCategoryExpanded, Is.EqualTo(isExpanded));
        }

        private static void AssertGeneRow(GeneCatalogRow row, GeneDef expectedGene)
        {
            Assert.That(row.Kind, Is.EqualTo(GeneCatalogRowKind.Gene));
            Assert.That(row.Category, Is.Null);
            Assert.That(row.Gene, Is.SameAs(expectedGene));
            Assert.That(row.IsCategoryExpanded, Is.False);
        }

        private static GeneCategoryDef CreateCategory(string defName)
        {
            return new GeneCategoryDef
            {
                defName = defName,
                label = defName
            };
        }

        private static GeneDef CreateGene(string defName, GeneCategoryDef category)
        {
            return new GeneDef
            {
                defName = defName,
                displayCategory = category
            };
        }
    }
}