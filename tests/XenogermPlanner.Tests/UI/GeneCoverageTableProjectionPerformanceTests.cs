using System;
using System.Collections.Generic;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;
using XenogermPlanner.Tests.Genes;
using XenogermPlanner.Tests.Performance;
using XenogermPlanner.UI;

namespace XenogermPlanner.Tests.UI
{
    [TestFixture]
    [Category("Performance")]
    [Explicit("Run locally in Release configuration to measure Gene Coverage projection performance.")]
    public sealed class GeneCoverageTableProjectionPerformanceTests
    {
        [Test]
        public void Benchmark_ManyReferencedSourceCompositions()
        {
            var scenario = GeneCoverageProjectionPerformanceScenario.CreateManyReferencedSourceCompositions();

            PerformanceMeasurement.Measure(
                "Gene Coverage Projection / Many Referenced Source Compositions",
                scenario.Build,
                result => AssertManySourceResult(scenario, result),
                $"Desired genes: {scenario.ExpectedRowCount}",
                $"Physical genepacks: {scenario.SourceGenepackCount}",
                $"Referenced compositions: {scenario.ExpectedReferencedCompositionCount}");
        }

        [Test]
        public void Benchmark_FewReferencedSourceCompositions()
        {
            var scenario = GeneCoverageProjectionPerformanceScenario.CreateFewReferencedSourceCompositions();

            PerformanceMeasurement.Measure(
                "Gene Coverage Projection / Few Referenced Source Compositions",
                scenario.Build,
                result => AssertFewSourceResult(scenario, result),
                $"Desired genes: {scenario.ExpectedRowCount}",
                $"Physical genepacks: {scenario.SourceGenepackCount}",
                $"Referenced compositions: {scenario.ExpectedReferencedCompositionCount}",
                $"Copies per referenced composition: {scenario.ExpectedCopiesPerReferencedComposition}");
        }

        [Test]
        public void Benchmark_MissingOnlyWithoutSourceCompositions()
        {
            var scenario = GeneCoverageProjectionPerformanceScenario.CreateMissingOnlyWithoutSourceCompositions();

            PerformanceMeasurement.Measure(
                "Gene Coverage Projection / Missing Only / No Source Compositions",
                scenario.Build,
                result => AssertMissingOnlyResult(scenario, result),
                $"Desired genes: {scenario.ExpectedRowCount}",
                $"Physical genepacks: {scenario.SourceGenepackCount}",
                "Referenced compositions: 0");
        }

        private static void AssertManySourceResult(
            GeneCoverageProjectionPerformanceScenario scenario,
            GeneCoverageTableProjection result)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Rows, Has.Count.EqualTo(scenario.ExpectedRowCount));
            Assert.That(result.ShowPotentialDonors, Is.False);

            var totalSourceGroups = 0;
            var totalPhysicalPacks = 0;

            foreach (GeneCoverageTablePresentationRow row in result.Rows)
            {
                Assert.That(row.IsResolved, Is.True);
                Assert.That(row.Row.AvailabilityState, Is.EqualTo(GeneCoverageAvailabilityState.Available));
                Assert.That(row.SourceGroups, Has.Count.EqualTo(1));
                Assert.That(row.Row.SourceGenepackCount, Is.EqualTo(scenario.ExpectedCopiesPerReferencedComposition));

                GeneCoverageTableSourceGroup sourceGroup = row.SourceGroups[0];
                Assert.That(sourceGroup.Genepacks, Has.Count.EqualTo(scenario.ExpectedCopiesPerReferencedComposition));
                Assert.That(scenario.IsInStablePhysicalOrder(sourceGroup.Genepacks), Is.True);

                totalSourceGroups++;
                totalPhysicalPacks += sourceGroup.Genepacks.Count;
            }

            Assert.That(totalSourceGroups, Is.EqualTo(scenario.ExpectedReferencedCompositionCount));
            Assert.That(totalPhysicalPacks, Is.EqualTo(scenario.ExpectedDisplayedPhysicalPackCount));
        }

        private static void AssertFewSourceResult(
            GeneCoverageProjectionPerformanceScenario scenario,
            GeneCoverageTableProjection result)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Rows, Has.Count.EqualTo(scenario.ExpectedRowCount));
            Assert.That(result.ShowPotentialDonors, Is.False);

            var availableRows = 0;
            var missingRows = 0;
            var totalSourceGroups = 0;
            var totalPhysicalPacks = 0;

            foreach (GeneCoverageTablePresentationRow row in result.Rows)
            {
                Assert.That(row.IsResolved, Is.True);

                if (row.Row.AvailabilityState == GeneCoverageAvailabilityState.Available)
                {
                    Assert.That(row.SourceGroups, Has.Count.EqualTo(1));
                    Assert.That(
                        row.Row.SourceGenepackCount,
                        Is.EqualTo(scenario.ExpectedCopiesPerReferencedComposition));

                    GeneCoverageTableSourceGroup sourceGroup = row.SourceGroups[0];
                    Assert.That(
                        sourceGroup.Genepacks,
                        Has.Count.EqualTo(scenario.ExpectedCopiesPerReferencedComposition));
                    Assert.That(scenario.IsInStablePhysicalOrder(sourceGroup.Genepacks), Is.True);

                    availableRows++;
                    totalSourceGroups++;
                    totalPhysicalPacks += sourceGroup.Genepacks.Count;
                    continue;
                }

                Assert.That(row.Row.AvailabilityState, Is.EqualTo(GeneCoverageAvailabilityState.Missing));
                Assert.That(row.Row.SourceGenepackCount, Is.Zero);
                Assert.That(row.SourceGroups, Is.Empty);
                missingRows++;
            }

            Assert.That(availableRows, Is.EqualTo(scenario.ExpectedReferencedCompositionCount));
            Assert.That(
                missingRows,
                Is.EqualTo(scenario.ExpectedRowCount - scenario.ExpectedReferencedCompositionCount));
            Assert.That(totalSourceGroups, Is.EqualTo(scenario.ExpectedReferencedCompositionCount));
            Assert.That(totalPhysicalPacks, Is.EqualTo(scenario.ExpectedDisplayedPhysicalPackCount));
        }

        private static void AssertMissingOnlyResult(
            GeneCoverageProjectionPerformanceScenario scenario,
            GeneCoverageTableProjection result)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Rows, Has.Count.EqualTo(scenario.ExpectedRowCount));
            Assert.That(result.ShowPotentialDonors, Is.False);

            foreach (GeneCoverageTablePresentationRow row in result.Rows)
            {
                Assert.That(row.IsResolved, Is.True);
                Assert.That(row.Row.AvailabilityState, Is.EqualTo(GeneCoverageAvailabilityState.Missing));
                Assert.That(row.Row.SourceGenepackCount, Is.Zero);
                Assert.That(row.SourceGroups, Is.Empty);
            }
        }

        private sealed class GeneCoverageProjectionPerformanceScenario
        {
            private const int SourcePackCount = 2048;
            private const int ManySourceDesiredGeneCount = 128;
            private const int FewSourceDesiredGeneCount = 16;
            private const int FewSourceReferencedCompositionCount = 4;
            private const int MissingOnlyDesiredGeneCount = 16;
            private const int CopiesPerComposition = 4;

            private readonly XenogermPlan _plan;
            private readonly PlanReadinessResult _readiness;
            private readonly IReadOnlyList<Genepack> _sourceGenepacks;
            private readonly IReadOnlyDictionary<Genepack, IReadOnlyList<GeneDef>> _genesByPack;
            private readonly IReadOnlyDictionary<Genepack, int> _physicalOrderByPack;
            private readonly Func<Genepack, IReadOnlyList<GeneDef>> _getGenepackGenes;
            private readonly Comparison<Genepack> _comparePhysicalGenepacks;

            internal int SourceGenepackCount => _sourceGenepacks.Count;
            internal int ExpectedRowCount { get; }
            internal int ExpectedReferencedCompositionCount { get; }
            internal int ExpectedCopiesPerReferencedComposition { get; }
            internal int ExpectedDisplayedPhysicalPackCount { get; }

            private GeneCoverageProjectionPerformanceScenario(
                XenogermPlan plan,
                PlanReadinessResult readiness,
                IReadOnlyList<Genepack> sourceGenepacks,
                IReadOnlyDictionary<Genepack, IReadOnlyList<GeneDef>> genesByPack,
                IReadOnlyDictionary<Genepack, int> physicalOrderByPack,
                int expectedRowCount,
                int expectedReferencedCompositionCount,
                int expectedCopiesPerReferencedComposition,
                int expectedDisplayedPhysicalPackCount)
            {
                _plan = plan ?? throw new ArgumentNullException(nameof(plan));
                _readiness = readiness ?? throw new ArgumentNullException(nameof(readiness));
                _sourceGenepacks = sourceGenepacks ?? throw new ArgumentNullException(nameof(sourceGenepacks));
                _genesByPack = genesByPack ?? throw new ArgumentNullException(nameof(genesByPack));
                _physicalOrderByPack = physicalOrderByPack ??
                                       throw new ArgumentNullException(nameof(physicalOrderByPack));
                ExpectedRowCount = expectedRowCount;
                ExpectedReferencedCompositionCount = expectedReferencedCompositionCount;
                ExpectedCopiesPerReferencedComposition = expectedCopiesPerReferencedComposition;
                ExpectedDisplayedPhysicalPackCount = expectedDisplayedPhysicalPackCount;
                _getGenepackGenes = GetGenepackGenes;
                _comparePhysicalGenepacks = ComparePhysicalGenepacks;
            }

            internal static GeneCoverageProjectionPerformanceScenario CreateManyReferencedSourceCompositions()
            {
                GeneDef[] targetGenes = CreateGenes("TargetGene", ManySourceDesiredGeneCount);
                GeneDef[] unrelatedGenes = CreateGenes(
                    "UnrelatedGene",
                    SourcePackCount - ManySourceDesiredGeneCount * CopiesPerComposition);
                var diagnostics = new List<PlanGeneCoverageDiagnostic>(ManySourceDesiredGeneCount);
                var sourceGenepacks = new List<Genepack>(SourcePackCount);
                var genesByPack = new Dictionary<Genepack, IReadOnlyList<GeneDef>>(
                    ReferenceEqualityComparer<Genepack>.Instance);
                var physicalOrderByPack = new Dictionary<Genepack, int>(ReferenceEqualityComparer<Genepack>.Instance);
                var physicalOrder = 0;

                for (var geneIndex = 0; geneIndex < targetGenes.Length; geneIndex++)
                {
                    GeneDef targetGene = targetGenes[geneIndex];
                    var composition = new PlanGenepackCompositionDiagnostic(
                        new[] { targetGene },
                        CopiesPerComposition,
                        isExactPayloadEligible: true,
                        additionalGenes: Array.Empty<GeneDef>());
                    diagnostics.Add(
                        new PlanGeneCoverageDiagnostic(
                            targetGene,
                            PlanGeneCoverageState.Available,
                            new[] { composition }));

                    for (var copyIndex = 0; copyIndex < CopiesPerComposition; copyIndex++)
                    {
                        AddPack(
                            sourceGenepacks,
                            genesByPack,
                            physicalOrderByPack,
                            ref physicalOrder,
                            new[] { targetGene });
                    }
                }

                foreach (GeneDef unrelatedGene in unrelatedGenes)
                {
                    AddPack(
                        sourceGenepacks,
                        genesByPack,
                        physicalOrderByPack,
                        ref physicalOrder,
                        new[] { unrelatedGene });
                }

                var plan = new XenogermPlan("Projection Plan", targetGenes, PlanReadinessMode.Coverage);
                var readiness = PlanReadinessResult.CreateReady(targetGenes, diagnostics);

                return new GeneCoverageProjectionPerformanceScenario(
                    plan,
                    readiness,
                    sourceGenepacks.AsReadOnly(),
                    genesByPack,
                    physicalOrderByPack,
                    ManySourceDesiredGeneCount,
                    ManySourceDesiredGeneCount,
                    CopiesPerComposition,
                    ManySourceDesiredGeneCount * CopiesPerComposition);
            }

            internal static GeneCoverageProjectionPerformanceScenario CreateFewReferencedSourceCompositions()
            {
                GeneDef[] targetGenes = CreateGenes("FewSourceTargetGene", FewSourceDesiredGeneCount);
                GeneDef[] unrelatedGenes = CreateGenes(
                    "FewSourceUnrelatedGene",
                    SourcePackCount - FewSourceReferencedCompositionCount * CopiesPerComposition);
                var availableGenes = new List<GeneDef>(FewSourceReferencedCompositionCount);
                var missingGenes = new List<GeneDef>(FewSourceDesiredGeneCount - FewSourceReferencedCompositionCount);
                var diagnostics = new List<PlanGeneCoverageDiagnostic>(FewSourceDesiredGeneCount);
                var sourceGenepacks = new List<Genepack>(SourcePackCount);
                var genesByPack = new Dictionary<Genepack, IReadOnlyList<GeneDef>>(
                    ReferenceEqualityComparer<Genepack>.Instance);
                var physicalOrderByPack = new Dictionary<Genepack, int>(ReferenceEqualityComparer<Genepack>.Instance);
                var physicalOrder = 0;

                for (var geneIndex = 0; geneIndex < targetGenes.Length; geneIndex++)
                {
                    GeneDef targetGene = targetGenes[geneIndex];

                    if (geneIndex < FewSourceReferencedCompositionCount)
                    {
                        var composition = new PlanGenepackCompositionDiagnostic(
                            new[] { targetGene },
                            CopiesPerComposition,
                            isExactPayloadEligible: true,
                            additionalGenes: Array.Empty<GeneDef>());
                        diagnostics.Add(
                            new PlanGeneCoverageDiagnostic(
                                targetGene,
                                PlanGeneCoverageState.Available,
                                new[] { composition }));
                        availableGenes.Add(targetGene);

                        for (var copyIndex = 0; copyIndex < CopiesPerComposition; copyIndex++)
                        {
                            AddPack(
                                sourceGenepacks,
                                genesByPack,
                                physicalOrderByPack,
                                ref physicalOrder,
                                new[] { targetGene });
                        }

                        continue;
                    }

                    diagnostics.Add(
                        new PlanGeneCoverageDiagnostic(
                            targetGene,
                            PlanGeneCoverageState.Missing,
                            Array.Empty<PlanGenepackCompositionDiagnostic>()));
                    missingGenes.Add(targetGene);
                }

                foreach (GeneDef unrelatedGene in unrelatedGenes)
                {
                    AddPack(
                        sourceGenepacks,
                        genesByPack,
                        physicalOrderByPack,
                        ref physicalOrder,
                        new[] { unrelatedGene });
                }

                var plan = new XenogermPlan("Few Source Projection Plan", targetGenes, PlanReadinessMode.Coverage);
                var readiness = PlanReadinessResult.CreateNotReady(
                    availableGenes,
                    missingGenes,
                    hasExactPayloadConflict: false,
                    geneCoverageDiagnostics: diagnostics);

                return new GeneCoverageProjectionPerformanceScenario(
                    plan,
                    readiness,
                    sourceGenepacks.AsReadOnly(),
                    genesByPack,
                    physicalOrderByPack,
                    FewSourceDesiredGeneCount,
                    FewSourceReferencedCompositionCount,
                    CopiesPerComposition,
                    FewSourceReferencedCompositionCount * CopiesPerComposition);
            }

            internal static GeneCoverageProjectionPerformanceScenario CreateMissingOnlyWithoutSourceCompositions()
            {
                GeneDef[] targetGenes = CreateGenes("MissingTargetGene", MissingOnlyDesiredGeneCount);
                GeneDef[] unrelatedGenes = CreateGenes("UnrelatedInventoryGene", SourcePackCount);
                var diagnostics = new List<PlanGeneCoverageDiagnostic>(MissingOnlyDesiredGeneCount);
                var sourceGenepacks = new List<Genepack>(SourcePackCount);
                var genesByPack = new Dictionary<Genepack, IReadOnlyList<GeneDef>>(
                    ReferenceEqualityComparer<Genepack>.Instance);
                var physicalOrderByPack = new Dictionary<Genepack, int>(ReferenceEqualityComparer<Genepack>.Instance);
                var physicalOrder = 0;

                foreach (GeneDef targetGene in targetGenes)
                {
                    diagnostics.Add(
                        new PlanGeneCoverageDiagnostic(
                            targetGene,
                            PlanGeneCoverageState.Missing,
                            Array.Empty<PlanGenepackCompositionDiagnostic>()));
                }

                foreach (GeneDef unrelatedGene in unrelatedGenes)
                {
                    AddPack(
                        sourceGenepacks,
                        genesByPack,
                        physicalOrderByPack,
                        ref physicalOrder,
                        new[] { unrelatedGene });
                }

                var plan = new XenogermPlan("Missing Projection Plan", targetGenes, PlanReadinessMode.Coverage);
                var readiness = PlanReadinessResult.CreateNotReady(
                    Array.Empty<GeneDef>(),
                    targetGenes,
                    hasExactPayloadConflict: false,
                    geneCoverageDiagnostics: diagnostics);

                return new GeneCoverageProjectionPerformanceScenario(
                    plan,
                    readiness,
                    sourceGenepacks.AsReadOnly(),
                    genesByPack,
                    physicalOrderByPack,
                    MissingOnlyDesiredGeneCount,
                    0,
                    0,
                    0);
            }

            internal GeneCoverageTableProjection Build()
            {
                return GeneCoverageTableProjection.Build(
                    _plan,
                    _readiness,
                    _sourceGenepacks,
                    potentialDonorAnalysis: null,
                    GeneCoverageSortState.Default,
                    _getGenepackGenes,
                    _comparePhysicalGenepacks);
            }

            internal bool IsInStablePhysicalOrder(IReadOnlyList<Genepack> genepacks)
            {
                if (genepacks == null)
                    throw new ArgumentNullException(nameof(genepacks));

                for (var index = 1; index < genepacks.Count; index++)
                {
                    if (ComparePhysicalGenepacks(genepacks[index - 1], genepacks[index]) > 0)
                        return false;
                }

                return true;
            }

            private IReadOnlyList<GeneDef> GetGenepackGenes(Genepack genepack)
            {
                return _genesByPack[genepack];
            }

            private int ComparePhysicalGenepacks(Genepack left, Genepack right)
            {
                return _physicalOrderByPack[left].CompareTo(_physicalOrderByPack[right]);
            }

            private static void AddPack(
                ICollection<Genepack> sourceGenepacks,
                IDictionary<Genepack, IReadOnlyList<GeneDef>> genesByPack,
                IDictionary<Genepack, int> physicalOrderByPack,
                ref int physicalOrder,
                IReadOnlyList<GeneDef> genes)
            {
                Genepack pack = GenepackInventoryTestData.CreateGenepack();
                sourceGenepacks.Add(pack);
                genesByPack.Add(pack, genes);
                physicalOrderByPack.Add(pack, physicalOrder);
                physicalOrder++;
            }

            private static GeneDef[] CreateGenes(string prefix, int count)
            {
                var genes = new GeneDef[count];

                for (var index = 0; index < count; index++)
                {
                    genes[index] = new GeneDef
                    {
                        defName = $"{prefix}{index:D4}",
                        label = $"{prefix} {index:D4}"
                    };
                }

                return genes;
            }
        }
    }
}