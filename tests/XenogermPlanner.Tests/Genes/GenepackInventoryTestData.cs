using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Tests.Genes
{
    internal static class GenepackInventoryTestData
    {
        internal static Faction CreateFaction()
        {
            return CreateUninitialized<Faction>();
        }

        internal static Map CreateMap()
        {
            return CreateUninitialized<Map>();
        }

        internal static Genepack CreateGenepack()
        {
            return CreateUninitialized<Genepack>();
        }

        internal static GeneDef CreateGene()
        {
            return CreateUninitialized<GeneDef>();
        }

        internal static IThingHolder CreatePassingShip()
        {
            return CreateUninitialized<TradeShip>();
        }

        internal static Pawn CreatePawn()
        {
            return CreateUninitialized<Pawn>();
        }

        internal static CandidateFixture CreateCandidate(Map mapHeld, params GeneDef[] genes)
        {
            return CreateCandidate(mapHeld, true, genes);
        }

        internal static CandidateFixture CreateCandidate(
            Map mapHeld,
            bool isPhysicallyMapRooted,
            params GeneDef[] genes)
        {
            return new CandidateFixture(CreateGenepack(), mapHeld, isPhysicallyMapRooted, genes);
        }

        internal static bool EvaluatePolicy(Map map, CandidateFixture fixture)
        {
            if (fixture == null)
                throw new ArgumentNullException(nameof(fixture));

            return PlanGenepackInventoryPolicy.ShouldInclude(
                map,
                fixture.Genepack,
                _ => fixture.MapHeld,
                _ => fixture.IsPhysicallyMapRooted,
                _ => fixture.HasForeignFactionOwner,
                _ => fixture.Genes);
        }

        internal static PlanGenepackInventorySnapshot Scan(
            Map map,
            IEnumerable<Genepack> discoveredGenepacks,
            Func<Map, Genepack, bool> shouldInclude)
        {
            if (discoveredGenepacks == null)
                throw new ArgumentNullException(nameof(discoveredGenepacks));

            return PlanGenepackInventoryScanner.Scan(
                map,
                (_, output, _) =>
                {
                    foreach (Genepack genepack in discoveredGenepacks)
                        output.Add(genepack);
                },
                shouldInclude);
        }

        private static T CreateUninitialized<T>() where T : class
        {
            return (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        }

        internal sealed class CandidateFixture
        {
            internal CandidateFixture(
                Genepack genepack,
                Map mapHeld,
                bool isPhysicallyMapRooted,
                IEnumerable<GeneDef> genes)
            {
                Genepack = genepack ?? throw new ArgumentNullException(nameof(genepack));
                MapHeld = mapHeld;
                IsPhysicallyMapRooted = isPhysicallyMapRooted;
                HasForeignFactionOwner = false;
                Genes = genes == null ? null : new List<GeneDef>(genes);
            }

            internal Genepack Genepack { get; }
            internal Map MapHeld { get; set; }
            internal bool IsPhysicallyMapRooted { get; set; }
            internal bool HasForeignFactionOwner { get; set; }
            internal IEnumerable<GeneDef> Genes { get; set; }
        }
    }
}