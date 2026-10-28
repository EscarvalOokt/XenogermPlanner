using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;
using XenogermPlanner.Tests.Genes;
using XenogermPlanner.Tests.Plans;

namespace XenogermPlanner.Tests.Analysis
{
    internal static class PlanReadinessTestData
    {
        internal static GeneDef CreateGene(string defName)
        {
            return PlanTestData.CreateGene(defName);
        }

        internal static GeneDef[] CreateGenes(string defNamePrefix, int count)
        {
            if (string.IsNullOrWhiteSpace(defNamePrefix))
            {
                throw new ArgumentException(
                    "Gene def name prefix cannot be null, empty or whitespace.",
                    nameof(defNamePrefix));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Gene count cannot be negative.");
            }

            var genes = new GeneDef[count];

            for (var index = 0; index < count; index++)
            {
                genes[index] = CreateGene($"{defNamePrefix}{index:D4}");
            }

            return genes;
        }

        internal static XenogermPlan CreatePlan(PlanReadinessMode readinessMode, params GeneDef[] desiredGenes)
        {
            return new XenogermPlan("Plan", desiredGenes, readinessMode);
        }

        internal static XenogermPlan CreateDegradedPlan(
            PlanReadinessMode readinessMode,
            IEnumerable<GeneDef> resolvedGenes,
            params string[] unresolvedGeneDefNames)
        {
            return new XenogermPlan("stable-id", "Plan", resolvedGenes, unresolvedGeneDefNames, readinessMode);
        }

        internal static PackFixture CreatePack(params GeneDef[] genes)
        {
            return new PackFixture(GenepackInventoryTestData.CreateGenepack(), genes);
        }

        internal static PlanGenepackInventorySnapshot CreateInventory(params PackFixture[] packs)
        {
            if (packs == null)
                throw new ArgumentNullException(nameof(packs));

            return PlanGenepackInventorySnapshot.CreateAvailable(packs.Select(pack => pack.Genepack));
        }

        internal static PlanGenepackInventorySnapshot CreateUnavailableInventory()
        {
            return PlanGenepackInventorySnapshot.Unavailable;
        }

        internal static PlanGenepackCombinationSearchResult Search(
            IEnumerable<GeneDef> desiredGenes,
            PlanReadinessMode readinessMode,
            params PackFixture[] packs)
        {
            if (desiredGenes == null)
                throw new ArgumentNullException(nameof(desiredGenes));

            if (packs == null)
                throw new ArgumentNullException(nameof(packs));

            IReadOnlyCollection<GeneDef> copiedDesiredGenes = new List<GeneDef>(desiredGenes).AsReadOnly();

            IReadOnlyList<Genepack> genepacks = packs.Select(pack => pack.Genepack).ToList().AsReadOnly();

            Dictionary<Genepack, PackFixture> packsByGenepack = CreatePackLookup(packs);

            return PlanGenepackCombinationSearcher.Search(
                copiedDesiredGenes,
                readinessMode,
                genepacks,
                genepack => FindPack(packsByGenepack, genepack).Genes);
        }

        internal static PlanReadinessResult Analyze(XenogermPlan plan, params PackFixture[] packs)
        {
            return Analyze(plan, CreateInventory(packs), packs);
        }

        internal static PlanReadinessResult AnalyzeAvailableGenepacks(XenogermPlan plan, params PackFixture[] packs)
        {
            if (packs == null)
                throw new ArgumentNullException(nameof(packs));

            IReadOnlyList<Genepack> genepacks = packs.Select(pack => pack.Genepack).ToList().AsReadOnly();

            Dictionary<Genepack, PackFixture> packsByGenepack = CreatePackLookup(packs);

            return PlanReadinessAnalyzer.AnalyzeAvailableGenepacks(
                plan,
                genepacks,
                (desiredGenes, readinessMode, availableGenepacks) => PlanGenepackCombinationSearcher.Search(
                    desiredGenes,
                    readinessMode,
                    availableGenepacks,
                    genepack => FindPack(packsByGenepack, genepack).Genes));
        }

        internal static PlanReadinessResult Analyze(
            XenogermPlan plan,
            PlanGenepackInventorySnapshot inventory,
            params PackFixture[] packs)
        {
            if (packs == null)
                throw new ArgumentNullException(nameof(packs));

            Dictionary<Genepack, PackFixture> packsByGenepack = CreatePackLookup(packs);

            return PlanReadinessAnalyzer.Analyze(
                plan,
                inventory,
                (desiredGenes, readinessMode, genepacks) => PlanGenepackCombinationSearcher.Search(
                    desiredGenes,
                    readinessMode,
                    genepacks,
                    genepack => FindPack(packsByGenepack, genepack).Genes));
        }

        internal static string[] GetGeneDefNames(IEnumerable<GeneDef> genes)
        {
            return genes.Select(gene => gene.defName).OrderBy(defName => defName, StringComparer.Ordinal).ToArray();
        }

        private static Dictionary<Genepack, PackFixture> CreatePackLookup(IEnumerable<PackFixture> packs)
        {
            var packsByGenepack = new Dictionary<Genepack, PackFixture>(ReferenceEqualityComparer<Genepack>.Instance);

            foreach (PackFixture pack in packs)
            {
                if (pack == null)
                    throw new ArgumentException("Pack fixture collection cannot contain null values.", nameof(packs));

                packsByGenepack.TryAdd(pack.Genepack, pack);
            }

            return packsByGenepack;
        }

        private static PackFixture FindPack(
            IReadOnlyDictionary<Genepack, PackFixture> packsByGenepack,
            Genepack genepack)
        {
            if (packsByGenepack.TryGetValue(genepack, out PackFixture pack))
            {
                return pack;
            }

            throw new InvalidOperationException("Genepack is not associated with an analysis test fixture.");
        }

        internal sealed class PackFixture
        {
            internal PackFixture(Genepack genepack, IEnumerable<GeneDef> genes)
            {
                Genepack = genepack ?? throw new ArgumentNullException(nameof(genepack));

                if (genes == null)
                    throw new ArgumentNullException(nameof(genes));

                Genes = new List<GeneDef>();

                foreach (GeneDef gene in genes)
                {
                    if (gene == null)
                        throw new ArgumentNullException(nameof(genes));

                    Genes.Add(gene);
                }
            }

            internal Genepack Genepack { get; }
            internal List<GeneDef> Genes { get; }
        }
    }
}