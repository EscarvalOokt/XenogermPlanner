using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Verse;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Donors
{
    public sealed class PlanPotentialDonorScopeSnapshot
    {
        private readonly ReadOnlyCollection<Pawn> _pawns;

        private PlanPotentialDonorScopeSnapshot(bool isAvailable, IEnumerable<Pawn> pawns)
        {
            if (pawns == null)
                throw new ArgumentNullException(nameof(pawns));

            var copiedPawns = new List<Pawn>();
            var distinctPawns = new HashSet<Pawn>(ReferenceEqualityComparer<Pawn>.Instance);

            foreach (Pawn pawn in pawns)
            {
                if (pawn == null)
                {
                    throw new ArgumentException("Pawn collection cannot contain null values.", nameof(pawns));
                }

                if (distinctPawns.Add(pawn))
                    copiedPawns.Add(pawn);
            }

            if (!isAvailable && copiedPawns.Count > 0)
            {
                throw new ArgumentException("Unavailable potential donor scope cannot contain pawns.", nameof(pawns));
            }

            IsAvailable = isAvailable;
            _pawns = copiedPawns.AsReadOnly();
        }

        public bool IsAvailable { get; }
        public IReadOnlyList<Pawn> Pawns => _pawns;

        internal static PlanPotentialDonorScopeSnapshot Unavailable { get; } =
            new PlanPotentialDonorScopeSnapshot(false, Array.Empty<Pawn>());

        internal static PlanPotentialDonorScopeSnapshot CreateAvailable(IEnumerable<Pawn> pawns)
        {
            return new PlanPotentialDonorScopeSnapshot(true, pawns);
        }
    }
}