using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Verse;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Donors
{
    public sealed class PlanPotentialDonorGeneDiagnostic
    {
        private readonly ReadOnlyCollection<Pawn> _donors;

        internal PlanPotentialDonorGeneDiagnostic(GeneDef gene, IEnumerable<Pawn> donors)
        {
            Gene = gene ?? throw new ArgumentNullException(nameof(gene));

            if (donors == null)
                throw new ArgumentNullException(nameof(donors));

            var copiedDonors = new List<Pawn>();
            var distinctDonors = new HashSet<Pawn>(ReferenceEqualityComparer<Pawn>.Instance);

            foreach (Pawn donor in donors)
            {
                if (donor == null)
                {
                    throw new ArgumentException("Donor collection cannot contain null values.", nameof(donors));
                }

                if (distinctDonors.Add(donor))
                    copiedDonors.Add(donor);
            }

            _donors = copiedDonors.AsReadOnly();
        }

        public GeneDef Gene { get; }
        public IReadOnlyList<Pawn> Donors => _donors;
        public int DonorCount => _donors.Count;
        public bool HasDonors => _donors.Count > 0;
    }
}