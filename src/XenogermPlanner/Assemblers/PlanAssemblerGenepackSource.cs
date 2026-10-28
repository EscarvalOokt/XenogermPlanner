using System;
using RimWorld;
using Verse;

namespace XenogermPlanner.Assemblers
{
    internal sealed class PlanAssemblerGenepackSource
    {
        internal PlanAssemblerGenepackSource(Genepack genepack, Thing facility, bool isFacilityPowered)
        {
            Genepack = genepack ?? throw new ArgumentNullException(nameof(genepack));
            Facility = facility ?? throw new ArgumentNullException(nameof(facility));
            IsFacilityPowered = isFacilityPowered;
        }

        internal Genepack Genepack { get; }
        internal Thing Facility { get; }
        internal bool IsFacilityPowered { get; }
    }
}