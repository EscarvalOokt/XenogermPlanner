using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Verse;

namespace XenogermPlanner.Analysis
{
    public sealed class PlanGenepackCompositionDiagnostic
    {
        private readonly ReadOnlyCollection<GeneDef> _additionalGenes;
        private readonly ReadOnlyCollection<GeneDef> _genes;

        internal PlanGenepackCompositionDiagnostic(
            IEnumerable<GeneDef> genes,
            int physicalPackCount,
            bool isExactPayloadEligible,
            IEnumerable<GeneDef> additionalGenes)
        {
            List<GeneDef> copiedGenes = CopyDistinctGenes(genes, nameof(genes));

            List<GeneDef> copiedAdditionalGenes = CopyDistinctGenes(additionalGenes, nameof(additionalGenes));

            if (physicalPackCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(physicalPackCount),
                    physicalPackCount,
                    "Physical genepack count must be positive.");
            }

            var geneSet = new HashSet<GeneDef>(copiedGenes);

            foreach (GeneDef additionalGene in copiedAdditionalGenes)
            {
                if (!geneSet.Contains(additionalGene))
                {
                    throw new ArgumentException(
                        "Additional genes must belong to the genepack composition.",
                        nameof(additionalGenes));
                }
            }

            if (isExactPayloadEligible != (copiedAdditionalGenes.Count == 0))
            {
                throw new ArgumentException(
                    "Exact-payload eligibility must match the additional-gene diagnostics.",
                    nameof(isExactPayloadEligible));
            }

            _genes = copiedGenes.AsReadOnly();
            PhysicalPackCount = physicalPackCount;
            IsExactPayloadEligible = isExactPayloadEligible;
            _additionalGenes = copiedAdditionalGenes.AsReadOnly();
        }

        public IReadOnlyCollection<GeneDef> Genes => _genes;
        public int PhysicalPackCount { get; }
        public bool IsExactPayloadEligible { get; }

        public IReadOnlyCollection<GeneDef> AdditionalGenes => _additionalGenes;

        private static List<GeneDef> CopyDistinctGenes(IEnumerable<GeneDef> genes, string parameterName)
        {
            if (genes == null)
                throw new ArgumentNullException(parameterName);

            var copiedGenes = new List<GeneDef>();
            var distinctGenes = new HashSet<GeneDef>();

            foreach (GeneDef gene in genes)
            {
                if (gene == null)
                {
                    throw new ArgumentException("Gene collection cannot contain null values.", parameterName);
                }

                if (distinctGenes.Add(gene))
                    copiedGenes.Add(gene);
            }

            return copiedGenes;
        }
    }
}