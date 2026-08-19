using System;
using System.Collections.Generic;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Genes;
using XenogermPlanner.Tests.Performance;

namespace XenogermPlanner.Tests.Genes
{
    [TestFixture]
    [Category("Performance")]
    [Explicit("Run locally in Release configuration to measure effective gene-biostat calculation performance.")]
    public sealed class PlanGeneBiostatPerformanceTests
    {
        [Test]
        public void Benchmark_EffectiveBiostatDistinctProjection()
        {
            var scenario = GeneBiostatPerformanceScenario.Create();

            PerformanceMeasurement.Measure(
                "Effective Gene Biostats / Distinct Projection",
                scenario.CalculateBatch,
                result => AssertResultMatchesScenario(scenario, result),
                $"Unique input genes: {scenario.UniqueGeneCount}",
                $"Projected gene entries: {scenario.ProjectedGeneCount}",
                $"Calculations per sample: {scenario.CalculationsPerSample}");
        }

        private static void AssertResultMatchesScenario(
            GeneBiostatPerformanceScenario scenario,
            PlanGeneBiostats result)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Complexity, Is.EqualTo(scenario.ExpectedComplexity));
            Assert.That(result.Metabolism, Is.EqualTo(scenario.ExpectedMetabolism));
            Assert.That(result.ArchiteCapsules, Is.EqualTo(scenario.ExpectedArchiteCapsules));
        }

        private sealed class GeneBiostatPerformanceScenario
        {
            private const int GeneCount = 512;
            private const int ProjectionCopiesPerGene = 2;
            private const int OperationsPerSample = 100;

            private readonly IReadOnlyList<GeneDef> _inputGenes;
            private readonly IReadOnlyList<GeneDef> _projectedGenes;
            private readonly Func<IEnumerable<GeneDef>, IEnumerable<GeneDef>> _getNonOverriddenGenes;

            internal int UniqueGeneCount => _inputGenes.Count;
            internal int ProjectedGeneCount => _projectedGenes.Count;
            internal int CalculationsPerSample => OperationsPerSample;
            internal int ExpectedComplexity { get; }
            internal int ExpectedMetabolism { get; }
            internal int ExpectedArchiteCapsules { get; }

            private GeneBiostatPerformanceScenario(
                IReadOnlyList<GeneDef> inputGenes,
                IReadOnlyList<GeneDef> projectedGenes,
                int expectedComplexity,
                int expectedMetabolism,
                int expectedArchiteCapsules)
            {
                _inputGenes = inputGenes ?? throw new ArgumentNullException(nameof(inputGenes));
                _projectedGenes = projectedGenes ?? throw new ArgumentNullException(nameof(projectedGenes));
                ExpectedComplexity = expectedComplexity;
                ExpectedMetabolism = expectedMetabolism;
                ExpectedArchiteCapsules = expectedArchiteCapsules;
                _getNonOverriddenGenes = GetProjectedGenes;
            }

            internal static GeneBiostatPerformanceScenario Create()
            {
                var inputGenes = new List<GeneDef>(GeneCount);
                var projectedGenes = new List<GeneDef>(GeneCount * ProjectionCopiesPerGene);
                var expectedComplexity = 0;
                var expectedMetabolism = 0;
                var expectedArchiteCapsules = 0;

                for (var index = 0; index < GeneCount; index++)
                {
                    var gene = new GeneDef
                    {
                        defName = $"BiostatGene{index:D4}",
                        label = $"Biostat Gene {index:D4}",
                        biostatCpx = index % 5 + 1,
                        biostatMet = index % 11 - 5,
                        biostatArc = index % 17 == 0 ? 1 : 0
                    };

                    inputGenes.Add(gene);
                    expectedComplexity += gene.biostatCpx;
                    expectedMetabolism += gene.biostatMet;
                    expectedArchiteCapsules += gene.biostatArc;

                    for (var copyIndex = 0; copyIndex < ProjectionCopiesPerGene; copyIndex++)
                        projectedGenes.Add(gene);
                }

                return new GeneBiostatPerformanceScenario(
                    inputGenes.AsReadOnly(),
                    projectedGenes.AsReadOnly(),
                    expectedComplexity,
                    expectedMetabolism,
                    expectedArchiteCapsules);
            }

            internal PlanGeneBiostats CalculateBatch()
            {
                PlanGeneBiostats result = null;

                for (var index = 0; index < OperationsPerSample; index++)
                {
                    result = PlanGeneBiostatCalculator.CalculateEffective(_inputGenes, _getNonOverriddenGenes);
                }

                return result;
            }

            private IEnumerable<GeneDef> GetProjectedGenes(IEnumerable<GeneDef> genes)
            {
                return _projectedGenes;
            }
        }
    }
}