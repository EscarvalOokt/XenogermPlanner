using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Verse;

namespace XenogermPlanner.Analysis
{
    public sealed class PlanGeneRandomChoiceGroupDiagnostic
    {
        private readonly ReadOnlyCollection<GeneDef> _genes;

        internal PlanGeneRandomChoiceGroupDiagnostic(IEnumerable<GeneDef> genes)
        {
            if (genes == null)
                throw new ArgumentNullException(nameof(genes));

            var copied = new List<GeneDef>();
            var distinct = new HashSet<GeneDef>();

            foreach (GeneDef gene in genes)
            {
                if (gene == null)
                {
                    throw new ArgumentException("Random-choice gene group cannot contain null values.", nameof(genes));
                }

                if (distinct.Add(gene))
                    copied.Add(gene);
            }

            if (copied.Count < 2)
            {
                throw new ArgumentException(
                    "Random-choice gene group must contain at least two distinct genes.",
                    nameof(genes));
            }

            copied.Sort(CompareGenes);
            _genes = copied.AsReadOnly();
        }

        public IReadOnlyList<GeneDef> Genes => _genes;

        private static int CompareGenes(GeneDef left, GeneDef right)
        {
            return StringComparer.Ordinal.Compare(left.defName ?? string.Empty, right.defName ?? string.Empty);
        }
    }
}