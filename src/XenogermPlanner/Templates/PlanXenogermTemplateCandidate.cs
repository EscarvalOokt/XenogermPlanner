using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Verse;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Templates
{
    internal sealed class PlanXenogermTemplateCandidate
    {
        private readonly ReadOnlyCollection<GeneDef> _additionalGenes;
        private readonly ReadOnlyCollection<PlanXenogermTemplateComposition> _compositions;
        private readonly ReadOnlyCollection<GeneDef> _unionGenes;

        internal PlanXenogermTemplateCandidate(
            IEnumerable<PlanXenogermTemplateComposition> compositions,
            IEnumerable<GeneDef> targetGenes)
        {
            if (compositions == null)
                throw new ArgumentNullException(nameof(compositions));

            HashSet<GeneDef> copiedTargetGenes =
                GenepackCompositionUtility.CopyDistinctGenes(targetGenes, nameof(targetGenes));

            var copiedCompositions = new List<PlanXenogermTemplateComposition>();
            var compositionKeys = new HashSet<string>(StringComparer.Ordinal);
            var unionGenes = new HashSet<GeneDef>();
            var totalGeneOccurrences = 0;

            foreach (PlanXenogermTemplateComposition composition in compositions)
            {
                if (composition == null)
                {
                    throw new ArgumentException(
                        "Template candidate cannot contain null compositions.",
                        nameof(compositions));
                }

                if (!compositionKeys.Add(composition.CompositionKey))
                {
                    throw new ArgumentException(
                        "Template candidate cannot contain duplicate compositions.",
                        nameof(compositions));
                }

                copiedCompositions.Add(composition);
                unionGenes.UnionWith(composition.Genes);
                totalGeneOccurrences += composition.Genes.Count;
            }

            if (copiedCompositions.Count == 0)
                throw new ArgumentException("Template candidate cannot be empty.", nameof(compositions));

            copiedCompositions.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.CompositionKey, right.CompositionKey));

            var additionalGenes = new HashSet<GeneDef>(unionGenes);
            additionalGenes.ExceptWith(copiedTargetGenes);

            var orderedUnionGenes = unionGenes.OrderBy(gene => gene.defName ?? string.Empty, StringComparer.Ordinal)
                .ToList();
            var orderedAdditionalGenes = additionalGenes.OrderBy(
                    gene => gene.defName ?? string.Empty,
                    StringComparer.Ordinal)
                .ToList();

            _compositions = copiedCompositions.AsReadOnly();
            _unionGenes = orderedUnionGenes.AsReadOnly();
            _additionalGenes = orderedAdditionalGenes.AsReadOnly();
            TotalGeneOccurrences = totalGeneOccurrences;
            CandidateKey = string.Join("\u001e", copiedCompositions.Select(composition => composition.CompositionKey));
        }

        internal IReadOnlyList<PlanXenogermTemplateComposition> Compositions => _compositions;
        internal IReadOnlyList<GeneDef> UnionGenes => _unionGenes;
        internal IReadOnlyList<GeneDef> AdditionalGenes => _additionalGenes;
        internal int GeneSetCount => _compositions.Count;
        internal int TotalGeneOccurrences { get; }
        internal string CandidateKey { get; }
    }
}