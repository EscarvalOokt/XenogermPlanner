using System;
using System.Collections.Generic;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Donors;
using XenogermPlanner.Plans;
using XenogermPlanner.Tests.Donors;
using XenogermPlanner.Tests.Genes;
using XenogermPlanner.UI;

namespace XenogermPlanner.Tests.UI
{
    [TestFixture]
    public sealed class GeneCoverageTableProjectionTests
    {
        [Test]
        public void Build_PreparesSortedExactPhysicalSourceGroup()
        {
            GeneDef gene = CreateGene("Gene", "Gene");
            var plan = new XenogermPlan("Plan", [gene], PlanReadinessMode.Coverage);
            PlanGenepackCompositionDiagnostic composition = CreateComposition(gene, physicalPackCount: 2);
            var coverage = new PlanGeneCoverageDiagnostic(gene, PlanGeneCoverageState.Available, [composition]);
            var readiness = PlanReadinessResult.CreateReady([gene], [coverage]);
            Genepack later = GenepackInventoryTestData.CreateGenepack();
            Genepack earlier = GenepackInventoryTestData.CreateGenepack();
            var genesByPack = new Dictionary<Genepack, IReadOnlyList<GeneDef>>
            {
                { later, [gene] },
                { earlier, [gene] }
            };
            var physicalOrder = new Dictionary<Genepack, int>
            {
                { later, 2 },
                { earlier, 1 }
            };

            var projection = GeneCoverageTableProjection.Build(
                plan,
                readiness,
                [later, earlier],
                null,
                GeneCoverageSortState.Default,
                pack => genesByPack[pack],
                (left, right) => physicalOrder[left].CompareTo(physicalOrder[right]));

            Assert.That(projection.Rows, Has.Count.EqualTo(1));
            GeneCoverageTablePresentationRow row = projection.Rows[0];
            Assert.That(row.Row.Diagnostic, Is.SameAs(coverage));
            Assert.That(row.SourceGroups, Has.Count.EqualTo(1));
            Assert.That(row.SourceGroups[0].Composition, Is.SameAs(composition));
            Assert.That(row.SourceGroups[0].Genepacks, Is.EqualTo([earlier, later]));
            Assert.That(row.SourceGroups[0].Genepacks[0], Is.SameAs(earlier));
            Assert.That(row.SourceGroups[0].Genepacks[1], Is.SameAs(later));
        }

        [Test]
        public void Build_WithoutReferencedCompositionsDoesNotInspectSourceGenepacks()
        {
            GeneDef missing = CreateGene("Missing", "Missing");
            var plan = new XenogermPlan("Plan", [missing], PlanReadinessMode.Coverage);
            var coverage = new PlanGeneCoverageDiagnostic(missing, PlanGeneCoverageState.Missing, []);
            var readiness = PlanReadinessResult.CreateNotReady(
                [],
                [missing],
                hasExactPayloadConflict: false,
                geneCoverageDiagnostics: [coverage]);
            Genepack source = GenepackInventoryTestData.CreateGenepack();
            var getGenepackGenesCallCount = 0;

            var projection = GeneCoverageTableProjection.Build(
                plan,
                readiness,
                [source],
                null,
                GeneCoverageSortState.Default,
                _ =>
                {
                    getGenepackGenesCallCount++;
                    throw new InvalidOperationException(
                        "Source genepacks must not be inspected without referenced compositions.");
                },
                (_, _) => 0);

            Assert.That(getGenepackGenesCallCount, Is.Zero);
            Assert.That(projection.Rows, Has.Count.EqualTo(1));
            Assert.That(projection.Rows[0].Row.Diagnostic, Is.SameAs(coverage));
            Assert.That(projection.Rows[0].Row.AvailabilityState, Is.EqualTo(GeneCoverageAvailabilityState.Missing));
            Assert.That(projection.Rows[0].SourceGroups, Is.Empty);
        }

        [Test]
        public void Build_MatchesDistinctPhysicalCompositionWithDifferentOrderAndDuplicates()
        {
            GeneDef geneA = CreateGene("A", "A");
            GeneDef geneB = CreateGene("B", "B");
            var plan = new XenogermPlan("Plan", [geneA, geneB], PlanReadinessMode.Coverage);
            var composition = new PlanGenepackCompositionDiagnostic(
                [geneA, geneB],
                physicalPackCount: 1,
                isExactPayloadEligible: true,
                additionalGenes: []);
            var coverageA = new PlanGeneCoverageDiagnostic(geneA, PlanGeneCoverageState.Available, [composition]);
            var coverageB = new PlanGeneCoverageDiagnostic(geneB, PlanGeneCoverageState.Available, [composition]);
            var readiness = PlanReadinessResult.CreateReady([geneA, geneB], [coverageA, coverageB]);
            Genepack pack = GenepackInventoryTestData.CreateGenepack();

            var projection = GeneCoverageTableProjection.Build(
                plan,
                readiness,
                [pack],
                null,
                GeneCoverageSortState.Default,
                _ => [geneB, geneA, geneA],
                (_, _) => 0);

            GeneCoverageTablePresentationRow rowA = FindRow(projection, geneA);
            GeneCoverageTablePresentationRow rowB = FindRow(projection, geneB);

            Assert.That(rowA.SourceGroups, Has.Count.EqualTo(1));
            Assert.That(rowA.SourceGroups[0].Composition, Is.SameAs(composition));
            Assert.That(rowA.SourceGroups[0].Genepacks, Is.EqualTo([pack]));
            Assert.That(rowB.SourceGroups, Has.Count.EqualTo(1));
            Assert.That(rowB.SourceGroups[0].Composition, Is.SameAs(composition));
            Assert.That(rowB.SourceGroups[0].Genepacks, Is.EqualTo([pack]));
        }

        [Test]
        public void Build_ExactPayloadConflictPreservesSourcesAndMapsAvailabilityState()
        {
            GeneDef gene = CreateGene("Gene", "Gene");
            GeneDef additionalGene = CreateGene("Additional", "Additional");
            var plan = new XenogermPlan("Plan", [gene], PlanReadinessMode.ExactPayload);
            PlanGenepackCompositionDiagnostic composition = CreateExactPayloadConflictComposition(
                gene,
                additionalGene,
                physicalPackCount: 1);
            var coverage = new PlanGeneCoverageDiagnostic(
                gene,
                PlanGeneCoverageState.ExactPayloadConflict,
                [composition]);
            var readiness = PlanReadinessResult.CreateNotReady(
                [gene],
                [],
                hasExactPayloadConflict: true,
                geneCoverageDiagnostics: [coverage]);
            Genepack pack = GenepackInventoryTestData.CreateGenepack();

            var projection = GeneCoverageTableProjection.Build(
                plan,
                readiness,
                [pack],
                null,
                GeneCoverageSortState.Default,
                _ => [gene, additionalGene],
                (_, _) => 0);

            Assert.That(projection.Rows, Has.Count.EqualTo(1));
            GeneCoverageTablePresentationRow row = projection.Rows[0];
            Assert.That(row.Row.Diagnostic, Is.SameAs(coverage));
            Assert.That(row.Row.Diagnostic.IsCovered, Is.True);
            Assert.That(row.Row.AvailabilityState, Is.EqualTo(GeneCoverageAvailabilityState.ExactPayloadConflict));
            Assert.That(row.Row.SourceGenepackCount, Is.EqualTo(1));
            Assert.That(row.SourceGroups, Has.Count.EqualTo(1));
            Assert.That(row.SourceGroups[0].Composition, Is.SameAs(composition));
            Assert.That(row.SourceGroups[0].Genepacks, Is.EqualTo([pack]));
        }

        [Test]
        public void Cache_DiagnosticStateChangeForcesRebuild()
        {
            GeneDef gene = CreateGene("Gene", "Gene");
            GeneDef additionalGene = CreateGene("Additional", "Additional");
            var plan = new XenogermPlan("Plan", [gene], PlanReadinessMode.ExactPayload);
            PlanGenepackCompositionDiagnostic composition = CreateExactPayloadConflictComposition(
                gene,
                additionalGene,
                physicalPackCount: 1);
            var availableDiagnostic = new PlanGeneCoverageDiagnostic(
                gene,
                PlanGeneCoverageState.Available,
                [composition]);
            var conflictDiagnostic = new PlanGeneCoverageDiagnostic(
                gene,
                PlanGeneCoverageState.ExactPayloadConflict,
                [composition]);
            var availableReadiness = PlanReadinessResult.CreateNotReady(
                [gene],
                [],
                hasExactPayloadConflict: true,
                geneCoverageDiagnostics: [availableDiagnostic]);
            var conflictReadiness = PlanReadinessResult.CreateNotReady(
                [gene],
                [],
                hasExactPayloadConflict: true,
                geneCoverageDiagnostics: [conflictDiagnostic]);
            Genepack pack = GenepackInventoryTestData.CreateGenepack();
            IReadOnlyList<Genepack> sources = [pack];
            var cache = new GeneCoverageTableProjectionCache();

            GeneCoverageTableProjection first = cache.GetOrBuild(
                plan,
                availableReadiness,
                sources,
                null,
                GeneCoverageSortState.Default,
                "English",
                _ => [gene, additionalGene],
                (_, _) => 0);
            GeneCoverageTableProjection rebuilt = cache.GetOrBuild(
                plan,
                conflictReadiness,
                sources,
                null,
                GeneCoverageSortState.Default,
                "English",
                _ => [gene, additionalGene],
                (_, _) => 0);

            Assert.That(rebuilt, Is.Not.SameAs(first));
            Assert.That(first.Rows[0].Row.AvailabilityState, Is.EqualTo(GeneCoverageAvailabilityState.Available));
            Assert.That(
                rebuilt.Rows[0].Row.AvailabilityState,
                Is.EqualTo(GeneCoverageAvailabilityState.ExactPayloadConflict));
        }

        [Test]
        public void Build_IncludesUnresolvedRowsAndOmitsSourceGroupsWithoutMatchingPhysicalPacks()
        {
            GeneDef covered = CreateGene("Covered", "Covered");
            GeneDef other = CreateGene("Other", "Other");
            var plan = new XenogermPlan("plan-id", "Plan", [covered], ["MissingDef"], PlanReadinessMode.Coverage);
            PlanGenepackCompositionDiagnostic composition = CreateComposition(covered, physicalPackCount: 1);
            var coverage = new PlanGeneCoverageDiagnostic(covered, PlanGeneCoverageState.Available, [composition]);
            var readiness = PlanReadinessResult.CreateDegraded([covered], [], [coverage]);
            Genepack unrelatedPack = GenepackInventoryTestData.CreateGenepack();

            var projection = GeneCoverageTableProjection.Build(
                plan,
                readiness,
                [unrelatedPack],
                null,
                GeneCoverageSortState.Default,
                _ => [other],
                (_, _) => 0);

            Assert.That(projection.Rows, Has.Count.EqualTo(2));
            Assert.That(projection.Rows[0].Row.Diagnostic, Is.SameAs(coverage));
            Assert.That(projection.Rows[0].SourceGroups, Is.Empty);
            Assert.That(projection.Rows[1].Row.IsResolved, Is.False);
            Assert.That(projection.Rows[1].Row.UnresolvedGeneDefName, Is.EqualTo("MissingDef"));
        }

        [Test]
        public void Build_UsesExistingCoverageSortPolicy()
        {
            GeneDef alpha = CreateGene("Alpha", "Alpha");
            GeneDef beta = CreateGene("Beta", "Beta");
            var plan = new XenogermPlan("Plan", [alpha, beta], PlanReadinessMode.Coverage);
            var alphaCoverage = new PlanGeneCoverageDiagnostic(alpha, PlanGeneCoverageState.Missing, []);
            var betaCoverage = new PlanGeneCoverageDiagnostic(beta, PlanGeneCoverageState.Missing, []);
            var readiness = PlanReadinessResult.CreateNotReady(
                [],
                [alpha, beta],
                hasExactPayloadConflict: false,
                geneCoverageDiagnostics: [alphaCoverage, betaCoverage]);

            var projection = GeneCoverageTableProjection.Build(
                plan,
                readiness,
                [],
                null,
                new GeneCoverageSortState(GeneCoverageSortColumn.Gene, descending: true));

            Assert.That(projection.Rows[0].Row.Diagnostic, Is.SameAs(betaCoverage));
            Assert.That(projection.Rows[1].Row.Diagnostic, Is.SameAs(alphaCoverage));
        }

        [Test]
        public void Cache_ReusesProjectionAndInvalidatesForSortSourceIdentityAndLanguageChanges()
        {
            GeneDef gene = CreateGene("Gene", "Gene");
            var plan = new XenogermPlan("Plan", [gene], PlanReadinessMode.Coverage);
            PlanGenepackCompositionDiagnostic composition = CreateComposition(gene, physicalPackCount: 1);
            var coverage = new PlanGeneCoverageDiagnostic(gene, PlanGeneCoverageState.Available, [composition]);
            var readiness = PlanReadinessResult.CreateReady([gene], [coverage]);
            Genepack pack = GenepackInventoryTestData.CreateGenepack();
            IReadOnlyList<Genepack> sources = [pack];
            IReadOnlyList<Genepack> replacementSources = [pack];
            var cache = new GeneCoverageTableProjectionCache();

            GeneCoverageTableProjection first = cache.GetOrBuild(
                plan,
                readiness,
                sources,
                null,
                GeneCoverageSortState.Default,
                "English",
                _ => [gene],
                (_, _) => 0);
            GeneCoverageTableProjection second = cache.GetOrBuild(
                plan,
                readiness,
                sources,
                null,
                GeneCoverageSortState.Default,
                "English",
                _ => [gene],
                (_, _) => 0);
            GeneCoverageTableProjection otherSort = cache.GetOrBuild(
                plan,
                readiness,
                sources,
                null,
                new GeneCoverageSortState(GeneCoverageSortColumn.Availability, descending: false),
                "English",
                _ => [gene],
                (_, _) => 0);
            GeneCoverageTableProjection otherLanguage = cache.GetOrBuild(
                plan,
                readiness,
                sources,
                null,
                new GeneCoverageSortState(GeneCoverageSortColumn.Availability, descending: false),
                "Other",
                _ => [gene],
                (_, _) => 0);
            GeneCoverageTableProjection changedSource = cache.GetOrBuild(
                plan,
                readiness,
                replacementSources,
                null,
                new GeneCoverageSortState(GeneCoverageSortColumn.Availability, descending: false),
                "Other",
                _ => [gene],
                (_, _) => 0);

            Assert.That(second, Is.SameAs(first));
            Assert.That(otherSort, Is.Not.SameAs(first));
            Assert.That(otherLanguage, Is.Not.SameAs(otherSort));
            Assert.That(changedSource, Is.Not.SameAs(otherLanguage));
        }

        [Test]
        public void Cache_InvalidatesForPlanCollectionPackReferenceAndDonorCountChanges()
        {
            GeneDef covered = CreateGene("Covered", "Covered");
            GeneDef missing = CreateGene("Missing", "Missing");
            var plan = new XenogermPlan("Plan", [covered, missing], PlanReadinessMode.Coverage);
            PlanGenepackCompositionDiagnostic composition = CreateComposition(covered, physicalPackCount: 1);
            var coveredDiagnostic = new PlanGeneCoverageDiagnostic(
                covered,
                PlanGeneCoverageState.Available,
                [composition]);
            var missingDiagnostic = new PlanGeneCoverageDiagnostic(missing, PlanGeneCoverageState.Missing, []);
            var readiness = PlanReadinessResult.CreateNotReady(
                [covered],
                [missing],
                hasExactPayloadConflict: false,
                geneCoverageDiagnostics: [coveredDiagnostic, missingDiagnostic]);
            Genepack firstPack = GenepackInventoryTestData.CreateGenepack();
            Genepack replacementPack = GenepackInventoryTestData.CreateGenepack();
            Pawn firstDonor = PlanPotentialDonorTestData.CreatePawn();
            Pawn secondDonor = PlanPotentialDonorTestData.CreatePawn();
            var oneDonor = PlanPotentialDonorAnalysisResult.CreateAvailable(
            [
                new PlanPotentialDonorGeneDiagnostic(missing, [firstDonor])
            ]);
            var twoDonors = PlanPotentialDonorAnalysisResult.CreateAvailable(
            [
                new PlanPotentialDonorGeneDiagnostic(missing, [firstDonor, secondDonor])
            ]);
            var cache = new GeneCoverageTableProjectionCache();
            IReadOnlyList<Genepack> firstSources = [firstPack];
            IReadOnlyList<Genepack> replacementSources = [replacementPack];

            GeneCoverageTableProjection first = cache.GetOrBuild(
                plan,
                readiness,
                firstSources,
                oneDonor,
                GeneCoverageSortState.Default,
                "English",
                _ => [covered],
                (_, _) => 0);

            plan.ReplaceDesiredGenes([covered, missing]);
            GeneCoverageTableProjection changedPlanCollection = cache.GetOrBuild(
                plan,
                readiness,
                firstSources,
                oneDonor,
                GeneCoverageSortState.Default,
                "English",
                _ => [covered],
                (_, _) => 0);
            GeneCoverageTableProjection changedPackReference = cache.GetOrBuild(
                plan,
                readiness,
                replacementSources,
                oneDonor,
                GeneCoverageSortState.Default,
                "English",
                _ => [covered],
                (_, _) => 0);
            GeneCoverageTableProjection changedDonorCount = cache.GetOrBuild(
                plan,
                readiness,
                replacementSources,
                twoDonors,
                GeneCoverageSortState.Default,
                "English",
                _ => [covered],
                (_, _) => 0);

            Assert.That(changedPlanCollection, Is.Not.SameAs(first));
            Assert.That(changedPackReference, Is.Not.SameAs(changedPlanCollection));
            Assert.That(changedDonorCount, Is.Not.SameAs(changedPackReference));
            GeneCoverageTablePresentationRow missingRow = FindRow(changedDonorCount, missing);
            Assert.That(missingRow.Row.PotentialDonorCount, Is.EqualTo(2));
        }

        [Test]
        public void Cache_InvalidateForcesRebuild()
        {
            GeneDef gene = CreateGene("Gene", "Gene");
            var plan = new XenogermPlan("Plan", [gene], PlanReadinessMode.Coverage);
            PlanGenepackCompositionDiagnostic composition = CreateComposition(gene, physicalPackCount: 1);
            var coverage = new PlanGeneCoverageDiagnostic(gene, PlanGeneCoverageState.Available, [composition]);
            var readiness = PlanReadinessResult.CreateReady([gene], [coverage]);
            Genepack pack = GenepackInventoryTestData.CreateGenepack();
            var cache = new GeneCoverageTableProjectionCache();

            GeneCoverageTableProjection first = cache.GetOrBuild(
                plan,
                readiness,
                [pack],
                null,
                GeneCoverageSortState.Default,
                "English",
                _ => [gene],
                (_, _) => 0);

            cache.Invalidate();

            GeneCoverageTableProjection rebuilt = cache.GetOrBuild(
                plan,
                readiness,
                [pack],
                null,
                GeneCoverageSortState.Default,
                "English",
                _ => [gene],
                (_, _) => 0);

            Assert.That(rebuilt, Is.Not.SameAs(first));
        }

        private static GeneCoverageTablePresentationRow FindRow(GeneCoverageTableProjection projection, GeneDef gene)
        {
            foreach (GeneCoverageTablePresentationRow row in projection.Rows)
            {
                if (ReferenceEquals(row.Row.Diagnostic?.Gene, gene))
                    return row;
            }

            Assert.Fail("Expected coverage row was not found.");
            return null;
        }

        private static GeneDef CreateGene(string defName, string label)
        {
            return new GeneDef
            {
                defName = defName,
                label = label
            };
        }

        private static PlanGenepackCompositionDiagnostic CreateExactPayloadConflictComposition(
            GeneDef gene,
            GeneDef additionalGene,
            int physicalPackCount)
        {
            return new PlanGenepackCompositionDiagnostic(
                [gene, additionalGene],
                physicalPackCount,
                isExactPayloadEligible: false,
                additionalGenes: [additionalGene]);
        }

        private static PlanGenepackCompositionDiagnostic CreateComposition(GeneDef gene, int physicalPackCount)
        {
            return new PlanGenepackCompositionDiagnostic(
                [gene],
                physicalPackCount,
                isExactPayloadEligible: true,
                additionalGenes: []);
        }
    }
}