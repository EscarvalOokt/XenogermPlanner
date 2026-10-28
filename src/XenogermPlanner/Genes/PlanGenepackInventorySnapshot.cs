using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RimWorld;

namespace XenogermPlanner.Genes
{
    public sealed class PlanGenepackInventorySnapshot
    {
        private readonly ReadOnlyCollection<Genepack> _genepacks;

        private PlanGenepackInventorySnapshot(bool isAvailable, IEnumerable<Genepack> genepacks)
        {
            if (genepacks == null)
                throw new ArgumentNullException(nameof(genepacks));

            var copiedGenepacks = new List<Genepack>();
            var distinctGenepacks = new HashSet<Genepack>(ReferenceEqualityComparer<Genepack>.Instance);

            foreach (Genepack genepack in genepacks)
            {
                if (genepack == null)
                {
                    throw new ArgumentException("Genepack collection cannot contain null values.", nameof(genepacks));
                }

                if (distinctGenepacks.Add(genepack))
                    copiedGenepacks.Add(genepack);
            }

            if (!isAvailable && copiedGenepacks.Count > 0)
            {
                throw new ArgumentException(
                    "Unavailable inventory snapshot cannot contain genepacks.",
                    nameof(genepacks));
            }

            IsAvailable = isAvailable;
            _genepacks = copiedGenepacks.AsReadOnly();
        }

        public bool IsAvailable { get; }
        public IReadOnlyList<Genepack> Genepacks => _genepacks;

        internal static PlanGenepackInventorySnapshot Unavailable { get; } =
            new PlanGenepackInventorySnapshot(false, Array.Empty<Genepack>());

        internal static PlanGenepackInventorySnapshot CreateAvailable(IEnumerable<Genepack> genepacks)
        {
            return new PlanGenepackInventorySnapshot(true, genepacks);
        }
    }
}