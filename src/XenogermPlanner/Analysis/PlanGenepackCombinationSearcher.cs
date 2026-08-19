using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Analysis
{
    internal static class PlanGenepackCombinationSearcher
    {
        private static readonly IEqualityComparer<HashSet<GeneDef>> _geneSetComparer =
            HashSet<GeneDef>.CreateSetComparer();

        private sealed class CompositionGroup
        {
            internal HashSet<GeneDef> Genes { get; }
            internal HashSet<GeneDef> AdditionalGenes { get; }
            internal bool IsExactPayloadEligible { get; }
            internal int PhysicalPackCount { get; set; }

            internal CompositionGroup(HashSet<GeneDef> ownedGenes, HashSet<GeneDef> ownedAdditionalGenes)
            {
                Genes = ownedGenes ?? throw new ArgumentNullException(nameof(ownedGenes));
                AdditionalGenes = ownedAdditionalGenes ?? throw new ArgumentNullException(nameof(ownedAdditionalGenes));
                IsExactPayloadEligible = AdditionalGenes.Count == 0;
                PhysicalPackCount = 1;
            }
        }

        internal static PlanGenepackCombinationSearchResult Search(
            IReadOnlyCollection<GeneDef> desiredGenes,
            PlanReadinessMode readinessMode,
            IReadOnlyList<Genepack> genepacks)
        {
            return Search(desiredGenes, readinessMode, genepacks, GetGenepackGenes);
        }

        internal static PlanGenepackCombinationSearchResult Search(
            IReadOnlyCollection<GeneDef> desiredGenes,
            PlanReadinessMode readinessMode,
            IReadOnlyList<Genepack> genepacks,
            Func<Genepack, IEnumerable<GeneDef>> getGenepackGenes)
        {
            if (desiredGenes == null)
                throw new ArgumentNullException(nameof(desiredGenes));

            if (genepacks == null)
                throw new ArgumentNullException(nameof(genepacks));

            if (getGenepackGenes == null)
                throw new ArgumentNullException(nameof(getGenepackGenes));

            ValidateReadinessMode(readinessMode);

            HashSet<GeneDef> targetGenes = GenepackCompositionUtility.CopyDistinctGenes(
                desiredGenes,
                nameof(desiredGenes));

            var availableGenes = new HashSet<GeneDef>();
            var exactEligibleGenes = new HashSet<GeneDef>();
            var compositionGroups = new List<CompositionGroup>();
            var compositionGroupsByGenes = new Dictionary<HashSet<GeneDef>, CompositionGroup>(_geneSetComparer);

            foreach (Genepack t in genepacks)
            {
                Genepack genepack = t ?? throw new ArgumentException(
                    "Genepack collection cannot contain null values.",
                    nameof(genepacks));

                IEnumerable<GeneDef> genepackGenes = getGenepackGenes(genepack) ??
                                                     throw new InvalidOperationException(
                                                         "Genepack gene collection is unavailable.");

                var packGenes = new HashSet<GeneDef>();
                var additionalGenes = new HashSet<GeneDef>();

                foreach (GeneDef gene in genepackGenes)
                {
                    if (gene == null)
                    {
                        throw new ArgumentException(
                            "Gene collection cannot contain null values.",
                            nameof(genepackGenes));
                    }

                    if (!packGenes.Add(gene))
                        continue;

                    availableGenes.Add(gene);

                    if (!targetGenes.Contains(gene))
                        additionalGenes.Add(gene);
                }

                if (compositionGroupsByGenes.TryGetValue(packGenes, out CompositionGroup compositionGroup))
                {
                    compositionGroup.PhysicalPackCount++;
                }
                else
                {
                    compositionGroup = new CompositionGroup(packGenes, additionalGenes);

                    compositionGroups.Add(compositionGroup);
                    compositionGroupsByGenes.Add(compositionGroup.Genes, compositionGroup);
                }

                if (readinessMode == PlanReadinessMode.ExactPayload && additionalGenes.Count == 0)
                {
                    foreach (GeneDef gene in packGenes)
                        exactEligibleGenes.Add(gene);
                }
            }

            bool hasValidCombination = readinessMode == PlanReadinessMode.Coverage
                ? targetGenes.IsSubsetOf(availableGenes)
                : exactEligibleGenes.SetEquals(targetGenes);

            return new PlanGenepackCombinationSearchResult(
                hasValidCombination,
                availableGenes,
                CreateCompositionDiagnostics(compositionGroups));
        }

        private static IEnumerable<GeneDef> GetGenepackGenes(Genepack genepack)
        {
            GeneSet geneSet = genepack.GeneSet ??
                              throw new InvalidOperationException("Genepack does not have a gene set.");

            List<GeneDef> genes = geneSet.GenesListForReading;

            return genes == null
                ? throw new InvalidOperationException("Genepack gene collection is unavailable.")
                : (IEnumerable<GeneDef>)genes;
        }

        private static List<PlanGenepackCompositionDiagnostic> CreateCompositionDiagnostics(
            IEnumerable<CompositionGroup> compositionGroups)
        {
            var diagnostics = new List<PlanGenepackCompositionDiagnostic>();

            foreach (CompositionGroup compositionGroup in compositionGroups)
            {
                diagnostics.Add(
                    new PlanGenepackCompositionDiagnostic(
                        compositionGroup.Genes,
                        compositionGroup.PhysicalPackCount,
                        compositionGroup.IsExactPayloadEligible,
                        compositionGroup.AdditionalGenes));
            }

            return diagnostics;
        }

        private static void ValidateReadinessMode(PlanReadinessMode readinessMode)
        {
            if (readinessMode != PlanReadinessMode.Coverage && readinessMode != PlanReadinessMode.ExactPayload)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(readinessMode),
                    readinessMode,
                    "Unsupported plan readiness mode.");
            }
        }
    }
}