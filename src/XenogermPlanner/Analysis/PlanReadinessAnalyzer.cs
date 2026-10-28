using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Analysis
{
    public static class PlanReadinessAnalyzer
    {
        public static PlanReadinessResult Analyze(XenogermPlan plan, PlanGenepackInventorySnapshot inventory)
        {
            return Analyze(plan, inventory, PlanGenepackCombinationSearcher.Search);
        }

        internal static PlanReadinessResult Analyze(
            XenogermPlan plan,
            PlanGenepackInventorySnapshot inventory,
            Func<IReadOnlyCollection<GeneDef>, PlanReadinessMode, IReadOnlyList<Genepack>,
                PlanGenepackCombinationSearchResult> searchCombinations)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));

            if (searchCombinations == null)
                throw new ArgumentNullException(nameof(searchCombinations));

            if (!inventory.IsAvailable)
            {
                return PlanReadinessResult.CreateUnavailable(PlanReadinessUnavailableReason.NoActiveMap);
            }

            return AnalyzeAvailableGenepacks(plan, inventory.Genepacks, searchCombinations);
        }

        internal static PlanReadinessResult AnalyzeAvailableGenepacks(
            XenogermPlan plan,
            IReadOnlyList<Genepack> genepacks)
        {
            return AnalyzeAvailableGenepacks(plan, genepacks, PlanGenepackCombinationSearcher.Search);
        }

        internal static PlanReadinessResult AnalyzeAvailableGenepacks(
            XenogermPlan plan,
            IReadOnlyList<Genepack> genepacks,
            Func<IReadOnlyCollection<GeneDef>, PlanReadinessMode, IReadOnlyList<Genepack>,
                PlanGenepackCombinationSearchResult> searchCombinations)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (genepacks == null)
                throw new ArgumentNullException(nameof(genepacks));

            if (searchCombinations == null)
                throw new ArgumentNullException(nameof(searchCombinations));

            PlanGenepackCombinationSearchResult searchResult =
                searchCombinations(plan.DesiredGenes, plan.ReadinessMode, genepacks) ??
                throw new InvalidOperationException("Genepack combination search returned a null result.");

            BuildGeneDiagnostics(
                plan.DesiredGenes,
                plan.ReadinessMode,
                searchResult.AvailableGenepackCompositions,
                out List<GeneDef> coveredGenes,
                out List<GeneDef> missingGenes,
                out List<PlanGeneCoverageDiagnostic> geneCoverageDiagnostics);

            if (plan.IsDegraded)
            {
                return PlanReadinessResult.CreateDegraded(coveredGenes, missingGenes, geneCoverageDiagnostics);
            }

            if (plan.DesiredGenes.Count == 0)
                return PlanReadinessResult.CreateEmptyTarget();

            if (searchResult.HasValidCombination)
            {
                return PlanReadinessResult.CreateReady(coveredGenes, geneCoverageDiagnostics);
            }

            bool hasExactPayloadConflict =
                plan.ReadinessMode == PlanReadinessMode.ExactPayload && missingGenes.Count == 0;

            return PlanReadinessResult.CreateNotReady(
                coveredGenes,
                missingGenes,
                hasExactPayloadConflict,
                geneCoverageDiagnostics);
        }

        private static void BuildGeneDiagnostics(
            IReadOnlyCollection<GeneDef> desiredGenes,
            PlanReadinessMode readinessMode,
            IReadOnlyList<PlanGenepackCompositionDiagnostic> availableGenepackCompositions,
            out List<GeneDef> coveredGenes,
            out List<GeneDef> missingGenes,
            out List<PlanGeneCoverageDiagnostic> geneCoverageDiagnostics)
        {
            coveredGenes = new List<GeneDef>(desiredGenes.Count);
            missingGenes = new List<GeneDef>(desiredGenes.Count);
            geneCoverageDiagnostics = new List<PlanGeneCoverageDiagnostic>(desiredGenes.Count);

            var compositionsByDesiredGene =
                new Dictionary<GeneDef, List<PlanGenepackCompositionDiagnostic>>(desiredGenes.Count);

            foreach (GeneDef desiredGene in desiredGenes)
            {
                compositionsByDesiredGene.TryAdd(desiredGene, null);
            }

            for (var index = 0; index < availableGenepackCompositions.Count; index++)
            {
                PlanGenepackCompositionDiagnostic composition = availableGenepackCompositions[index];

                if (composition.AdditionalGenes.Count == composition.Genes.Count)
                    continue;

                foreach (GeneDef gene in composition.Genes)
                {
                    if (!compositionsByDesiredGene.TryGetValue(
                            gene,
                            out List<PlanGenepackCompositionDiagnostic> matchingList))
                    {
                        continue;
                    }

                    if (matchingList == null)
                    {
                        matchingList = new List<PlanGenepackCompositionDiagnostic>();
                        compositionsByDesiredGene[gene] = matchingList;
                    }

                    matchingList.Add(composition);
                }
            }

            foreach (GeneDef desiredGene in desiredGenes)
            {
                List<PlanGenepackCompositionDiagnostic> matchingList = compositionsByDesiredGene[desiredGene];
                IReadOnlyList<PlanGenepackCompositionDiagnostic> sourceCompositions;

                if (matchingList != null)
                    sourceCompositions = matchingList;
                else
                    sourceCompositions = Array.Empty<PlanGenepackCompositionDiagnostic>();

                PlanGeneCoverageState state = ClassifyGeneCoverage(readinessMode, sourceCompositions);

                var diagnostic = new PlanGeneCoverageDiagnostic(desiredGene, state, sourceCompositions);

                geneCoverageDiagnostics.Add(diagnostic);

                if (state == PlanGeneCoverageState.Missing)
                    missingGenes.Add(desiredGene);
                else
                    coveredGenes.Add(desiredGene);
            }
        }

        private static PlanGeneCoverageState ClassifyGeneCoverage(
            PlanReadinessMode readinessMode,
            IReadOnlyList<PlanGenepackCompositionDiagnostic> sourceCompositions)
        {
            if (sourceCompositions.Count == 0)
                return PlanGeneCoverageState.Missing;

            switch (readinessMode)
            {
                case PlanReadinessMode.Coverage:
                    return PlanGeneCoverageState.Available;

                case PlanReadinessMode.ExactPayload:
                    foreach (PlanGenepackCompositionDiagnostic composition in sourceCompositions)
                    {
                        if (composition.IsExactPayloadEligible)
                            return PlanGeneCoverageState.Available;
                    }

                    return PlanGeneCoverageState.ExactPayloadConflict;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(readinessMode),
                        readinessMode,
                        "Unknown plan readiness mode.");
            }
        }
    }
}