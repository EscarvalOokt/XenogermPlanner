using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Verse;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Templates
{
    internal sealed class PlanXenogermTemplateComposition
    {
        private readonly ReadOnlyCollection<GeneDef> _additionalGenes;
        private readonly ReadOnlyCollection<GeneDef> _genes;

        internal PlanXenogermTemplateComposition(
            IEnumerable<GeneDef> genes,
            IEnumerable<GeneDef> additionalGenes,
            int physicalPackCount)
        {
            if (physicalPackCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(physicalPackCount),
                    physicalPackCount,
                    "Physical pack count must be positive.");
            }

            HashSet<GeneDef> copiedGenes = GenepackCompositionUtility.CopyDistinctGenes(genes, nameof(genes));
            HashSet<GeneDef> copiedAdditionalGenes =
                GenepackCompositionUtility.CopyDistinctGenes(additionalGenes, nameof(additionalGenes));

            if (copiedGenes.Count == 0)
                throw new ArgumentException("Template composition cannot be empty.", nameof(genes));

            if (!copiedAdditionalGenes.IsSubsetOf(copiedGenes))
            {
                throw new ArgumentException(
                    "Additional genes must be part of the full composition.",
                    nameof(additionalGenes));
            }

            var orderedGenes = new List<GeneDef>(copiedGenes);
            orderedGenes.Sort(CompareGenes);

            var orderedAdditionalGenes = new List<GeneDef>(copiedAdditionalGenes);
            orderedAdditionalGenes.Sort(CompareGenes);

            _genes = orderedGenes.AsReadOnly();
            _additionalGenes = orderedAdditionalGenes.AsReadOnly();
            CompositionKey = GenepackCompositionUtility.CreateCompositionKey(orderedGenes);
            PhysicalPackCount = physicalPackCount;
        }

        internal IReadOnlyList<GeneDef> Genes => _genes;
        internal IReadOnlyList<GeneDef> AdditionalGenes => _additionalGenes;
        internal string CompositionKey { get; }
        internal int PhysicalPackCount { get; }

        private static int CompareGenes(GeneDef left, GeneDef right)
        {
            return StringComparer.Ordinal.Compare(left?.defName ?? string.Empty, right?.defName ?? string.Empty);
        }
    }
}