using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Assemblers
{
    internal static class PlanAssemblerScopeScanner
    {
        internal static IReadOnlyList<Building_GeneAssembler> GetSelectableAssemblers(Map map)
        {
            if (map == null)
                return Array.Empty<Building_GeneAssembler>();

            return GetSelectableAssemblers(map.listerBuildings.allBuildingsColonist);
        }

        internal static IReadOnlyList<Building_GeneAssembler> GetSelectableAssemblers(IEnumerable<Building> buildings)
        {
            if (buildings == null)
                throw new ArgumentNullException(nameof(buildings));

            var assemblers = new List<Building_GeneAssembler>();

            foreach (Building building in buildings)
            {
                if (building is Building_GeneAssembler assembler)
                    assemblers.Add(assembler);
            }

            return assemblers.AsReadOnly();
        }

        internal static PlanAssemblerScopeSnapshot Scan(Building_GeneAssembler assembler)
        {
            return Scan(assembler, value => value.ConnectedFacilities, GetContainedGenepacks, IsFacilityPowered);
        }

        internal static PlanAssemblerScopeSnapshot Scan(
            Building_GeneAssembler assembler,
            Func<Building_GeneAssembler, IEnumerable<Thing>> getConnectedFacilities,
            Func<Thing, IEnumerable<Genepack>> getContainedGenepacks,
            Func<Thing, bool> isFacilityPowered)
        {
            if (assembler == null)
                throw new ArgumentNullException(nameof(assembler));

            if (getConnectedFacilities == null)
            {
                throw new ArgumentNullException(nameof(getConnectedFacilities));
            }

            if (getContainedGenepacks == null)
            {
                throw new ArgumentNullException(nameof(getContainedGenepacks));
            }

            if (isFacilityPowered == null)
                throw new ArgumentNullException(nameof(isFacilityPowered));

            var sources = new List<PlanAssemblerGenepackSource>();
            var seenGenepacks = new HashSet<Genepack>(ReferenceEqualityComparer<Genepack>.Instance);

            IEnumerable<Thing> connectedFacilities = getConnectedFacilities(assembler);

            if (connectedFacilities == null)
                return new PlanAssemblerScopeSnapshot(sources);

            foreach (Thing facility in connectedFacilities)
            {
                if (facility == null)
                    continue;

                IEnumerable<Genepack> containedGenepacks = getContainedGenepacks(facility);

                if (containedGenepacks == null)
                    continue;

                bool facilityPowered = isFacilityPowered(facility);

                foreach (Genepack genepack in containedGenepacks)
                {
                    if (genepack == null || !seenGenepacks.Add(genepack))
                    {
                        continue;
                    }

                    sources.Add(new PlanAssemblerGenepackSource(genepack, facility, facilityPowered));
                }
            }

            return new PlanAssemblerScopeSnapshot(sources);
        }

        internal static IReadOnlyList<Genepack> GetVisibleGenepacks(Building_GeneAssembler assembler)
        {
            return Scan(assembler).VisibleGenepacks;
        }

        internal static IReadOnlyList<Genepack> GetVisibleGenepacks(
            Building_GeneAssembler assembler,
            Func<Building_GeneAssembler, IEnumerable<Thing>> getConnectedFacilities,
            Func<Thing, IEnumerable<Genepack>> getContainedGenepacks)
        {
            return Scan(assembler, getConnectedFacilities, getContainedGenepacks, _ => true).VisibleGenepacks;
        }

        private static IEnumerable<Genepack> GetContainedGenepacks(Thing facility)
        {
            var parent = facility as ThingWithComps;
            CompGenepackContainer container = parent?.GetComp<CompGenepackContainer>();

            return container?.ContainedGenepacks;
        }

        private static bool IsFacilityPowered(Thing facility)
        {
            var parent = facility as ThingWithComps;
            CompPowerTrader power = parent?.GetComp<CompPowerTrader>();

            return power == null || power.PowerOn;
        }
    }
}