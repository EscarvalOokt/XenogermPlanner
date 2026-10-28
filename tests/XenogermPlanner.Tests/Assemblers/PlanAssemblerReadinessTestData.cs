using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using XenogermPlanner.Assemblers;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;
using XenogermPlanner.Tests.Genes;
using XenogermPlanner.Tests.Plans;

namespace XenogermPlanner.Tests.Assemblers
{
    internal static class PlanAssemblerReadinessTestData
    {
        internal static GeneDef CreateGene(
            string defName,
            int complexity = 0,
            int archite = 0,
            GeneDef prerequisite = null,
            int metabolism = 0)
        {
            GeneDef gene = PlanTestData.CreateGene(defName);
            gene.biostatCpx = complexity;
            gene.biostatArc = archite;
            gene.prerequisite = prerequisite;
            gene.biostatMet = metabolism;
            return gene;
        }

        internal static XenogermPlan CreatePlan(PlanReadinessMode readinessMode, params GeneDef[] desiredGenes)
        {
            return new XenogermPlan("Plan", desiredGenes, readinessMode);
        }

        internal static XenogermPlan CreateDegradedPlan(
            PlanReadinessMode readinessMode,
            IEnumerable<GeneDef> resolvedGenes,
            params string[] unresolvedGeneDefNames)
        {
            return new XenogermPlan("stable-id", "Plan", resolvedGenes, unresolvedGeneDefNames, readinessMode);
        }

        internal static PackFixture CreatePack(string physicalKey, bool facilityPowerOn, params GeneDef[] genes)
        {
            return new PackFixture(physicalKey, facilityPowerOn, genes);
        }

        internal static PlanAssemblerScopeSnapshot CreateScope(params PackFixture[] packs)
        {
            ValidatePacks(packs);

            return new PlanAssemblerScopeSnapshot(packs.Select(pack => pack.Source));
        }

        internal static PlanAssemblerLiveState CreateLiveState(
            bool assemblerPowerOn,
            int maxComplexity,
            int availableArchiteCapsules,
            bool archogeneticsFinished,
            params PackFixture[] packs)
        {
            return new PlanAssemblerLiveState(
                CreateScope(packs),
                assemblerPowerOn,
                maxComplexity,
                availableArchiteCapsules,
                archogeneticsFinished);
        }

        internal static IReadOnlyList<PlanAssemblerCandidate> Search(XenogermPlan plan, params PackFixture[] packs)
        {
            return Search(plan, new PlanAssemblerSearchBudget(), false, packs);
        }

        internal static IReadOnlyList<PlanAssemblerCandidate> Search(
            XenogermPlan plan,
            PlanAssemblerSearchBudget budget,
            bool includeHelpers,
            params PackFixture[] packs)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            ValidatePacks(packs);
            Dictionary<Genepack, PackFixture> fixturesByPack = CreatePackLookup(packs);

            return PlanAssemblerCandidateSearcher.Search(
                    plan.DesiredGenes,
                    plan.ReadinessMode,
                    CreateScope(packs),
                    genepack => FindPack(fixturesByPack, genepack).Genes,
                    genepack => FindPack(fixturesByPack, genepack).PhysicalKey,
                    budget,
                    includeHelpers)
                .ToList()
                .AsReadOnly();
        }

        internal static PlanAssemblerReadinessResult Analyze(
            XenogermPlan plan,
            PlanAssemblerLiveState liveState,
            params PackFixture[] packs)
        {
            return Analyze(plan, liveState, genes => genes, packs);
        }

        internal static PlanAssemblerReadinessResult Analyze(
            XenogermPlan plan,
            PlanAssemblerLiveState liveState,
            Func<IEnumerable<GeneDef>, IEnumerable<GeneDef>> getNonOverriddenGenes,
            params PackFixture[] packs)
        {
            return Analyze(plan, liveState, getNonOverriddenGenes, null, packs);
        }

        internal static PlanAssemblerReadinessResult Analyze(
            XenogermPlan plan,
            PlanAssemblerLiveState liveState,
            Func<IEnumerable<GeneDef>, IEnumerable<GeneDef>> getNonOverriddenGenes,
            PlanAssemblerSearchBudget budget,
            params PackFixture[] packs)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (liveState == null)
                throw new ArgumentNullException(nameof(liveState));

            if (getNonOverriddenGenes == null)
            {
                throw new ArgumentNullException(nameof(getNonOverriddenGenes));
            }

            ValidatePacks(packs);
            Dictionary<Genepack, PackFixture> fixturesByPack = CreatePackLookup(packs);

            return PlanAssemblerReadinessAnalyzer.Analyze(
                plan,
                liveState,
                genepack => FindPack(fixturesByPack, genepack).Genes,
                getNonOverriddenGenes,
                genepack => FindPack(fixturesByPack, genepack).PhysicalKey,
                budget);
        }

        internal static bool CandidateContains(PlanAssemblerCandidate candidate, PackFixture pack)
        {
            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));

            if (pack == null)
                throw new ArgumentNullException(nameof(pack));

            return candidate.Sources.Any(source => ReferenceEquals(source.Genepack, pack.Genepack));
        }

        internal static bool ResultContainsCandidatePack(PlanAssemblerReadinessResult result, PackFixture pack)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (pack == null)
                throw new ArgumentNullException(nameof(pack));

            return result.CandidateGenepacks.Any(genepack => ReferenceEquals(genepack, pack.Genepack));
        }

        private static Dictionary<Genepack, PackFixture> CreatePackLookup(IEnumerable<PackFixture> packs)
        {
            var fixturesByPack = new Dictionary<Genepack, PackFixture>(ReferenceEqualityComparer<Genepack>.Instance);

            foreach (PackFixture pack in packs)
            {
                fixturesByPack.TryAdd(pack.Genepack, pack);
            }

            return fixturesByPack;
        }

        private static PackFixture FindPack(
            IReadOnlyDictionary<Genepack, PackFixture> fixturesByPack,
            Genepack genepack)
        {
            if (fixturesByPack.TryGetValue(genepack, out PackFixture pack))
            {
                return pack;
            }

            throw new InvalidOperationException("Genepack is not associated with an assembler readiness test fixture.");
        }

        private static void ValidatePacks(IEnumerable<PackFixture> packs)
        {
            if (packs == null)
                throw new ArgumentNullException(nameof(packs));

            foreach (PackFixture pack in packs)
            {
                if (pack == null)
                {
                    throw new ArgumentException("Pack fixture collection cannot contain null values.", nameof(packs));
                }
            }
        }

        private static T CreateUninitialized<T>() where T : class
        {
            return (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        }

        internal sealed class PackFixture
        {
            internal PackFixture(string physicalKey, bool facilityPowerOn, IEnumerable<GeneDef> genes)
            {
                if (string.IsNullOrWhiteSpace(physicalKey))
                {
                    throw new ArgumentException(
                        "Physical key cannot be null, empty or whitespace.",
                        nameof(physicalKey));
                }

                if (genes == null)
                    throw new ArgumentNullException(nameof(genes));

                Genepack = GenepackInventoryTestData.CreateGenepack();
                PhysicalKey = physicalKey;
                Genes = new List<GeneDef>();

                foreach (GeneDef gene in genes)
                {
                    if (gene == null)
                        throw new ArgumentNullException(nameof(genes));

                    Genes.Add(gene);
                }

                Source = new PlanAssemblerGenepackSource(
                    Genepack,
                    CreateUninitialized<ThingWithComps>(),
                    facilityPowerOn);
            }

            internal Genepack Genepack { get; }
            internal PlanAssemblerGenepackSource Source { get; }
            internal List<GeneDef> Genes { get; }
            internal string PhysicalKey { get; }
        }
    }
}