using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;
using XenogermPlanner.Templates;
using XenogermPlanner.Tests.Genes;
using XenogermPlanner.Tests.Plans;

namespace XenogermPlanner.Tests.Templates
{
    internal static class PlanXenogermTemplateTestData
    {
        internal static GeneDef CreateGene(string defName)
        {
            return PlanTestData.CreateGene(defName);
        }

        internal static XenogermPlan CreatePlan(PlanReadinessMode mode, params GeneDef[] genes)
        {
            return new XenogermPlan("Template plan", genes, mode);
        }

        internal static PackFixture CreatePack(string physicalKey, params GeneDef[] genes)
        {
            return new PackFixture(physicalKey, genes);
        }

        internal static PlanXenogermTemplateCandidateSearchResult Search(XenogermPlan plan, params PackFixture[] packs)
        {
            return Search(
                plan,
                PlanReadinessStatus.Ready,
                true,
                PlanXenogermTemplateCandidateSearchLimits.Default,
                packs);
        }

        internal static PlanXenogermTemplateCandidateSearchResult Search(
            XenogermPlan plan,
            PlanXenogermTemplateCandidateSearchLimits limits,
            params PackFixture[] packs)
        {
            return Search(plan, PlanReadinessStatus.Ready, true, limits, packs);
        }

        internal static PlanXenogermTemplateCandidateSearchResult Search(
            XenogermPlan plan,
            PlanReadinessStatus readinessStatus,
            bool inventoryAvailable,
            params PackFixture[] packs)
        {
            return Search(
                plan,
                readinessStatus,
                inventoryAvailable,
                PlanXenogermTemplateCandidateSearchLimits.Default,
                packs);
        }

        internal static PlanXenogermTemplateCandidateSearchResult Search(
            XenogermPlan plan,
            PlanReadinessStatus readinessStatus,
            bool inventoryAvailable,
            PlanXenogermTemplateCandidateSearchLimits limits,
            params PackFixture[] packs)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            Dictionary<Genepack, PackFixture> lookup = CreateLookup(packs);

            return PlanXenogermTemplateCandidateSearcher.Search(
                plan.DesiredGenes,
                plan.ReadinessMode,
                plan.IsDegraded,
                readinessStatus,
                inventoryAvailable,
                packs.Select(pack => pack.Genepack).ToList().AsReadOnly(),
                genepack => Find(lookup, genepack).Genes,
                limits);
        }

        internal static PlanXenogermTemplateCandidate CreateCandidate(
            XenogermPlan plan,
            params GeneDef[][] compositions)
        {
            var candidateCompositions = new List<PlanXenogermTemplateComposition>();
            var targetGenes = new HashSet<GeneDef>(plan.DesiredGenes);

            foreach (GeneDef[] genes in compositions)
            {
                var additionalGenes = new HashSet<GeneDef>(genes);
                additionalGenes.ExceptWith(targetGenes);
                candidateCompositions.Add(new PlanXenogermTemplateComposition(genes, additionalGenes, 1));
            }

            return new PlanXenogermTemplateCandidate(candidateCompositions, plan.DesiredGenes);
        }

        internal static PlanXenogermTemplateSaveResult Save(
            XenogermPlan plan,
            PlanXenogermTemplateCandidate candidate,
            bool inventoryAvailable,
            IEnumerable<PackFixture> packs,
            Func<string, XenotypeIconDef, List<Genepack>, AcceptanceReport> saveTemplate)
        {
            return Save(plan, candidate, "Template", null, inventoryAvailable, packs, saveTemplate);
        }

        internal static PlanXenogermTemplateSaveResult Save(
            XenogermPlan plan,
            PlanXenogermTemplateCandidate candidate,
            string templateName,
            XenotypeIconDef iconDef,
            bool inventoryAvailable,
            IEnumerable<PackFixture> packs,
            Func<string, XenotypeIconDef, List<Genepack>, AcceptanceReport> saveTemplate)
        {
            var copiedPacks = packs.ToList();
            Dictionary<Genepack, PackFixture> lookup = CreateLookup(copiedPacks);

            return PlanXenogermTemplateSaver.Save(
                plan,
                candidate,
                templateName,
                iconDef,
                inventoryAvailable,
                copiedPacks.Select(pack => pack.Genepack).ToList().AsReadOnly(),
                genepack => Find(lookup, genepack).Genes,
                genepack => Find(lookup, genepack).PhysicalKey,
                saveTemplate);
        }

        internal static bool ContainsComposition(PlanXenogermTemplateCandidate candidate, params GeneDef[] genes)
        {
            var expected = new HashSet<GeneDef>(genes);

            return candidate.Compositions.Any(composition => expected.SetEquals(composition.Genes));
        }

        private static Dictionary<Genepack, PackFixture> CreateLookup(IEnumerable<PackFixture> packs)
        {
            var lookup = new Dictionary<Genepack, PackFixture>(ReferenceEqualityComparer<Genepack>.Instance);

            foreach (PackFixture pack in packs)
                lookup.Add(pack.Genepack, pack);

            return lookup;
        }

        private static PackFixture Find(IReadOnlyDictionary<Genepack, PackFixture> lookup, Genepack genepack)
        {
            return lookup[genepack];
        }

        internal sealed class PackFixture
        {
            internal PackFixture(string physicalKey, IEnumerable<GeneDef> genes)
            {
                PhysicalKey = physicalKey ?? throw new ArgumentNullException(nameof(physicalKey));
                Genepack = GenepackInventoryTestData.CreateGenepack();
                Genes = new List<GeneDef>(genes ?? throw new ArgumentNullException(nameof(genes))).AsReadOnly();
            }

            internal Genepack Genepack { get; }
            internal IReadOnlyList<GeneDef> Genes { get; }
            internal string PhysicalKey { get; }
        }
    }
}