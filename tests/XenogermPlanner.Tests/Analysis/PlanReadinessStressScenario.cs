using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Analysis
{
    internal sealed class PlanReadinessStressScenario
    {
        private const int LargePlannedGeneCount = 512;
        private const int MediumPlannedGeneCount = 128;
        private const int SmallPlannedGeneCount = 16;
        private const int ExtraGeneCount = 512;
        private const int MaximumGenesPerPack = 4;
        internal const int PhysicalPackCount = 2048;

        private readonly Func<Genepack, IEnumerable<GeneDef>> _getGenepackGenes;

        private readonly Func<IReadOnlyCollection<GeneDef>, PlanReadinessMode, IReadOnlyList<Genepack>,
            PlanGenepackCombinationSearchResult> _searchCombinations;

        internal XenogermPlan Plan { get; }
        internal PlanReadinessTestData.PackFixture[] Packs { get; }
        internal PlanGenepackInventorySnapshot Inventory { get; }
        internal PlanReadinessStatus ExpectedStatus { get; }
        internal int ExpectedDesiredGeneCount { get; }
        internal int ExpectedPhysicalPackCount { get; }
        internal int ExpectedCoveredGeneCount { get; }
        internal int ExpectedMissingGeneCount { get; }
        internal int ExpectedExactPayloadConflictGeneCount { get; }
        internal bool ExpectedExactPayloadConflict { get; }

        private PlanReadinessStressScenario(
            XenogermPlan plan,
            IEnumerable<PlanReadinessTestData.PackFixture> packs,
            PlanReadinessStatus expectedStatus,
            int expectedDesiredGeneCount,
            int expectedPhysicalPackCount,
            int expectedCoveredGeneCount,
            int expectedMissingGeneCount,
            int expectedExactPayloadConflictGeneCount,
            bool expectedExactPayloadConflict)
        {
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));

            if (packs == null)
                throw new ArgumentNullException(nameof(packs));

            Packs = packs.ToArray();
            Inventory = PlanReadinessTestData.CreateInventory(Packs);
            ExpectedStatus = expectedStatus;
            ExpectedDesiredGeneCount = expectedDesiredGeneCount;
            ExpectedPhysicalPackCount = expectedPhysicalPackCount;
            ExpectedCoveredGeneCount = expectedCoveredGeneCount;
            ExpectedMissingGeneCount = expectedMissingGeneCount;
            ExpectedExactPayloadConflictGeneCount = expectedExactPayloadConflictGeneCount;
            ExpectedExactPayloadConflict = expectedExactPayloadConflict;

            var genesByPack = new Dictionary<Genepack, IReadOnlyList<GeneDef>>(
                ReferenceEqualityComparer<Genepack>.Instance);

            foreach (PlanReadinessTestData.PackFixture pack in Packs)
                genesByPack.Add(pack.Genepack, pack.Genes);

            _getGenepackGenes = genepack => genesByPack[genepack];
            _searchCombinations = SearchCombinations;

            Validate();
        }

        internal static PlanReadinessStressScenario CreateCoverageReady()
        {
            GeneDef[] targetGenes = PlanReadinessTestData.CreateGenes("TargetGene", LargePlannedGeneCount);

            GeneDef[] extraGenes = PlanReadinessTestData.CreateGenes("ExtraGene", ExtraGeneCount);

            var baseCompositions = new List<GeneDef[]>(LargePlannedGeneCount);

            for (var index = 0; index < LargePlannedGeneCount; index++)
            {
                switch (index % 4)
                {
                    case 0:
                        baseCompositions.Add(
                            new[]
                            {
                                targetGenes[index]
                            });
                        break;

                    case 1:
                        baseCompositions.Add(
                            new[]
                            {
                                targetGenes[index],
                                extraGenes[index]
                            });
                        break;

                    case 2:
                        baseCompositions.Add(
                            new[]
                            {
                                targetGenes[index],
                                targetGenes[(index + 1) % LargePlannedGeneCount],
                                extraGenes[index]
                            });
                        break;

                    default:
                        baseCompositions.Add(
                            new[]
                            {
                                targetGenes[index],
                                extraGenes[index],
                                extraGenes[(index + 1) % ExtraGeneCount],
                                extraGenes[(index + 2) % ExtraGeneCount]
                            });
                        break;
                }
            }

            return new PlanReadinessStressScenario(
                PlanReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, targetGenes),
                RepeatCompositions(baseCompositions, 4),
                PlanReadinessStatus.Ready,
                LargePlannedGeneCount,
                PhysicalPackCount,
                LargePlannedGeneCount,
                0,
                0,
                false);
        }

        internal static PlanReadinessStressScenario CreateExactReady()
        {
            GeneDef[] targetGenes = PlanReadinessTestData.CreateGenes("TargetGene", LargePlannedGeneCount);

            GeneDef[] extraGenes = PlanReadinessTestData.CreateGenes("ExtraGene", ExtraGeneCount);

            var packs = new List<PlanReadinessTestData.PackFixture>(PhysicalPackCount);

            for (var index = 0; index < LargePlannedGeneCount; index++)
            {
                packs.Add(PlanReadinessTestData.CreatePack(targetGenes[index]));

                packs.Add(PlanReadinessTestData.CreatePack(targetGenes[index]));
            }

            for (var groupIndex = 0; groupIndex < LargePlannedGeneCount / 4; groupIndex++)
            {
                int geneIndex = groupIndex * 4;
                GeneDef[] composition =
                {
                    targetGenes[geneIndex],
                    targetGenes[geneIndex + 1],
                    targetGenes[geneIndex + 2],
                    targetGenes[geneIndex + 3]
                };

                AddCompositionCopies(packs, composition, 4);
            }

            for (var groupIndex = 0; groupIndex < LargePlannedGeneCount / 4; groupIndex++)
            {
                int geneIndex = groupIndex * 4;
                GeneDef[] composition =
                {
                    targetGenes[geneIndex],
                    targetGenes[geneIndex + 1],
                    extraGenes[groupIndex]
                };

                AddCompositionCopies(packs, composition, 4);
            }

            return new PlanReadinessStressScenario(
                PlanReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, targetGenes),
                packs,
                PlanReadinessStatus.Ready,
                LargePlannedGeneCount,
                PhysicalPackCount,
                LargePlannedGeneCount,
                0,
                0,
                false);
        }

        internal static PlanReadinessStressScenario CreateExactConflict()
        {
            GeneDef[] targetGenes = PlanReadinessTestData.CreateGenes("TargetGene", LargePlannedGeneCount);

            GeneDef[] extraGenes = PlanReadinessTestData.CreateGenes("ExtraGene", ExtraGeneCount);

            var baseCompositions = new List<GeneDef[]>(LargePlannedGeneCount);

            for (var index = 0; index < LargePlannedGeneCount; index++)
            {
                if (index < 384)
                {
                    baseCompositions.Add(
                        new[]
                        {
                            targetGenes[index]
                        });
                }
                else
                {
                    baseCompositions.Add(
                        new[]
                        {
                            targetGenes[index],
                            extraGenes[index - 384]
                        });
                }
            }

            return new PlanReadinessStressScenario(
                PlanReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, targetGenes),
                RepeatCompositions(baseCompositions, 4),
                PlanReadinessStatus.NotReady,
                LargePlannedGeneCount,
                PhysicalPackCount,
                LargePlannedGeneCount,
                0,
                128,
                true);
        }

        internal static PlanReadinessStressScenario CreateSmallTargetLargeHeterogeneousMissing()
        {
            return CreateLargeHeterogeneousMissing(SmallPlannedGeneCount, "SmallTargetGene");
        }

        internal static PlanReadinessStressScenario CreateMediumTargetLargeHeterogeneousMissing()
        {
            return CreateLargeHeterogeneousMissing(MediumPlannedGeneCount, "MediumTargetGene");
        }

        internal PlanReadinessResult Analyze()
        {
            return PlanReadinessAnalyzer.Analyze(Plan, Inventory, _searchCombinations);
        }

        private PlanGenepackCombinationSearchResult SearchCombinations(
            IReadOnlyCollection<GeneDef> desiredGenes,
            PlanReadinessMode readinessMode,
            IReadOnlyList<Genepack> genepacks)
        {
            return PlanGenepackCombinationSearcher.Search(desiredGenes, readinessMode, genepacks, _getGenepackGenes);
        }

        private static PlanReadinessStressScenario CreateLargeHeterogeneousMissing(
            int targetGeneCount,
            string targetGenePrefix)
        {
            GeneDef[] targetGenes = PlanReadinessTestData.CreateGenes(targetGenePrefix, targetGeneCount);
            GeneDef[] unrelatedGenes = PlanReadinessTestData.CreateGenes(
                "UnrelatedInventoryGene",
                PhysicalPackCount * 2);
            var packs = new List<PlanReadinessTestData.PackFixture>(PhysicalPackCount);

            for (var index = 0; index < PhysicalPackCount; index++)
            {
                packs.Add(PlanReadinessTestData.CreatePack(unrelatedGenes[index * 2], unrelatedGenes[index * 2 + 1]));
            }

            return new PlanReadinessStressScenario(
                PlanReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, targetGenes),
                packs,
                PlanReadinessStatus.NotReady,
                targetGeneCount,
                PhysicalPackCount,
                0,
                targetGeneCount,
                0,
                false);
        }

        private static List<PlanReadinessTestData.PackFixture> RepeatCompositions(
            IReadOnlyList<GeneDef[]> compositions,
            int copiesPerComposition)
        {
            var packs = new List<PlanReadinessTestData.PackFixture>(compositions.Count * copiesPerComposition);

            foreach (GeneDef[] composition in compositions)
            {
                AddCompositionCopies(packs, composition, copiesPerComposition);
            }

            return packs;
        }

        private static void AddCompositionCopies(
            ICollection<PlanReadinessTestData.PackFixture> packs,
            GeneDef[] composition,
            int copyCount)
        {
            for (var copyIndex = 0; copyIndex < copyCount; copyIndex++)
            {
                packs.Add(PlanReadinessTestData.CreatePack(composition));
            }
        }

        private void Validate()
        {
            if (ExpectedDesiredGeneCount < 0)
            {
                throw new InvalidOperationException("Expected desired-gene count cannot be negative.");
            }

            if (ExpectedPhysicalPackCount < 0)
            {
                throw new InvalidOperationException("Expected physical-pack count cannot be negative.");
            }

            if (Plan.DesiredGenes.Count != ExpectedDesiredGeneCount)
            {
                throw new InvalidOperationException(
                    $"Stress scenario must contain exactly {ExpectedDesiredGeneCount} planned genes.");
            }

            if (Packs.Length != ExpectedPhysicalPackCount)
            {
                throw new InvalidOperationException(
                    $"Stress scenario must contain exactly {ExpectedPhysicalPackCount} physical genepacks.");
            }

            if (ExpectedCoveredGeneCount + ExpectedMissingGeneCount != ExpectedDesiredGeneCount)
            {
                throw new InvalidOperationException(
                    "Expected covered and missing gene counts must describe every desired gene.");
            }

            if (ExpectedExactPayloadConflictGeneCount < 0 ||
                ExpectedExactPayloadConflictGeneCount > ExpectedCoveredGeneCount)
            {
                throw new InvalidOperationException(
                    "Expected exact-payload conflict count must fit within the covered-gene count.");
            }

            foreach (PlanReadinessTestData.PackFixture pack in Packs)
            {
                if (pack == null)
                {
                    throw new InvalidOperationException("Stress scenario cannot contain a null pack fixture.");
                }

                if (pack.Genes.Count < 1 || pack.Genes.Count > MaximumGenesPerPack)
                {
                    throw new InvalidOperationException(
                        "Every stress-scenario genepack must contain between one and four genes.");
                }

                foreach (GeneDef gene in pack.Genes)
                {
                    if (gene == null)
                    {
                        throw new InvalidOperationException("Stress-scenario genepacks cannot contain null genes.");
                    }
                }
            }
        }
    }
}