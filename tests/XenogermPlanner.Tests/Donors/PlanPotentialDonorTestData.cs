using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;
using XenogermPlanner.Donors;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Tests.Donors
{
    internal static class PlanPotentialDonorTestData
    {
        internal static GeneDef CreateGene(
            string defName,
            int metabolism = 0,
            int archites = 0,
            bool isMelanin = false,
            bool passOnDirectly = true,
            bool canGenerateInGeneSet = true)
        {
            if (string.IsNullOrWhiteSpace(defName))
            {
                throw new ArgumentException("Gene def name cannot be null, empty or whitespace.", nameof(defName));
            }

            return new GeneDef
            {
                defName = defName,
                biostatMet = metabolism,
                biostatArc = archites,
                endogeneCategory = isMelanin ? EndogeneCategory.Melanin : default(EndogeneCategory),
                passOnDirectly = passOnDirectly,
                canGenerateInGeneSet = canGenerateInGeneSet
            };
        }

        internal static Pawn CreatePawn()
        {
            return CreateUninitialized<Pawn>();
        }

        internal static Map CreateMap()
        {
            return CreateUninitialized<Map>();
        }

        internal static PlanPotentialDonorScopeSnapshot CreateScope(params Pawn[] pawns)
        {
            return PlanPotentialDonorScopeSnapshot.CreateAvailable(pawns);
        }

        internal static PlanPotentialDonorScopeSnapshot ScanScope(
            Map map,
            IEnumerable<Pawn> discoveredPawns,
            Func<Map, Pawn, bool> isSpawnedOnMap,
            Func<Pawn, bool> hasGeneTracker)
        {
            if (discoveredPawns == null)
                throw new ArgumentNullException(nameof(discoveredPawns));

            return PlanPotentialDonorScopeScanner.Scan(map, _ => discoveredPawns, isSpawnedOnMap, hasGeneTracker);
        }

        internal static PlanPotentialDonorAnalysisResult Analyze(
            IEnumerable<GeneDef> requestedGenes,
            PlanPotentialDonorScopeSnapshot scope,
            IReadOnlyDictionary<Pawn, IEnumerable<GeneDef>> genesByPawn)
        {
            if (genesByPawn == null)
                throw new ArgumentNullException(nameof(genesByPawn));

            return PlanPotentialDonorAnalyzer.Analyze(
                requestedGenes,
                scope,
                pawn =>
                {
                    genesByPawn.TryGetValue(pawn, out IEnumerable<GeneDef> genes);
                    return genes;
                });
        }

        internal static Dictionary<Pawn, IEnumerable<GeneDef>> CreatePawnGeneMap()
        {
            return new Dictionary<Pawn, IEnumerable<GeneDef>>(ReferenceEqualityComparer<Pawn>.Instance);
        }

        internal static Dictionary<Pawn, IEnumerable<GeneDef>> CreatePawnGeneMap(Pawn pawn, params GeneDef[] genes)
        {
            if (pawn == null)
                throw new ArgumentNullException(nameof(pawn));

            Dictionary<Pawn, IEnumerable<GeneDef>> genesByPawn = CreatePawnGeneMap();
            genesByPawn.Add(pawn, genes ?? throw new ArgumentNullException(nameof(genes)));

            return genesByPawn;
        }

        private static T CreateUninitialized<T>() where T : class
        {
            return (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        }
    }
}