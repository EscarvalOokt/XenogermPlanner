using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Verse;

namespace XenogermPlanner.Analysis
{
    internal sealed class PlanGenepackCombinationSearchResult
    {
        private readonly ReadOnlyCollection<PlanGenepackCompositionDiagnostic> _availableGenepackCompositions;
        private readonly ReadOnlyCollection<GeneDef> _availableGenes;

        internal PlanGenepackCombinationSearchResult(
            bool hasValidCombination,
            IEnumerable<GeneDef> availableGenes,
            IEnumerable<PlanGenepackCompositionDiagnostic> availableGenepackCompositions)
        {
            if (availableGenes == null)
                throw new ArgumentNullException(nameof(availableGenes));

            if (availableGenepackCompositions == null)
            {
                throw new ArgumentNullException(nameof(availableGenepackCompositions));
            }

            var copiedGenes = new List<GeneDef>();
            var distinctGenes = new HashSet<GeneDef>();

            foreach (GeneDef gene in availableGenes)
            {
                if (gene == null)
                {
                    throw new ArgumentException(
                        "Available gene collection cannot contain null values.",
                        nameof(availableGenes));
                }

                if (distinctGenes.Add(gene))
                    copiedGenes.Add(gene);
            }

            var copiedCompositions = new List<PlanGenepackCompositionDiagnostic>();
            var compositionGenes = new HashSet<GeneDef>();

            foreach (PlanGenepackCompositionDiagnostic composition in availableGenepackCompositions)
            {
                if (composition == null)
                {
                    throw new ArgumentException(
                        "Available genepack composition collection cannot contain null values.",
                        nameof(availableGenepackCompositions));
                }

                copiedCompositions.Add(composition);
                compositionGenes.UnionWith(composition.Genes);
            }

            if (!compositionGenes.SetEquals(distinctGenes))
            {
                throw new ArgumentException(
                    "Available genes must match the union of available genepack compositions.",
                    nameof(availableGenepackCompositions));
            }

            HasValidCombination = hasValidCombination;
            _availableGenes = copiedGenes.AsReadOnly();
            _availableGenepackCompositions = copiedCompositions.AsReadOnly();
        }

        internal bool HasValidCombination { get; }

        internal IReadOnlyCollection<GeneDef> AvailableGenes => _availableGenes;

        internal IReadOnlyList<PlanGenepackCompositionDiagnostic> AvailableGenepackCompositions =>
            _availableGenepackCompositions;
    }
}