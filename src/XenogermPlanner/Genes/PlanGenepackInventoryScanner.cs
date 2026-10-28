using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace XenogermPlanner.Genes
{
    internal static class PlanGenepackInventoryScanner
    {
        internal static PlanGenepackInventorySnapshot Scan(Map map)
        {
            if (map == null)
                return PlanGenepackInventorySnapshot.Unavailable;

            return Scan(map, DiscoverGenepacks, PlanGenepackInventoryPolicy.ShouldInclude);
        }

        internal static PlanGenepackInventorySnapshot Scan(
            Map map,
            Action<Map, List<Genepack>, Predicate<IThingHolder>> discoverGenepacks,
            Func<Map, Genepack, bool> shouldInclude)
        {
            if (map == null)
                return PlanGenepackInventorySnapshot.Unavailable;

            if (discoverGenepacks == null)
                throw new ArgumentNullException(nameof(discoverGenepacks));

            if (shouldInclude == null)
                throw new ArgumentNullException(nameof(shouldInclude));

            var discoveredGenepacks = new List<Genepack>();

            discoverGenepacks(map, discoveredGenepacks, PlanGenepackInventoryPolicy.ShouldTraverseHolder);

            var includedGenepacks = new HashSet<Genepack>(ReferenceEqualityComparer<Genepack>.Instance);

            foreach (Genepack genepack in discoveredGenepacks)
            {
                if (genepack != null && shouldInclude(map, genepack))
                    includedGenepacks.Add(genepack);
            }

            return PlanGenepackInventorySnapshot.CreateAvailable(includedGenepacks);
        }

        private static void DiscoverGenepacks(
            Map map,
            List<Genepack> discoveredGenepacks,
            Predicate<IThingHolder> passCheck)
        {
            ThingOwnerUtility.GetAllThingsRecursively(
                map,
                ThingRequest.ForDef(ThingDefOf.Genepack),
                discoveredGenepacks,
                true,
                passCheck);
        }
    }
}