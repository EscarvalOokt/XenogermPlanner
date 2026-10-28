using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Assemblers
{
    internal static class PlanAssemblerReadinessAnalyzer
    {
        internal static PlanAssemblerReadinessResult Analyze(XenogermPlan plan, Building_GeneAssembler assembler)
        {
            if (assembler == null)
                throw new ArgumentNullException(nameof(assembler));

            return Analyze(plan, PlanAssemblerLiveStateReader.Read(assembler));
        }

        internal static PlanAssemblerReadinessResult Analyze(XenogermPlan plan, PlanAssemblerLiveState liveState)
        {
            return Analyze(plan, liveState, GetGenepackGenes, GetNonOverriddenGenes, GetStablePhysicalKey);
        }

        internal static PlanAssemblerReadinessResult Analyze(
            XenogermPlan plan,
            PlanAssemblerLiveState liveState,
            Func<Genepack, IEnumerable<GeneDef>> getGenepackGenes,
            Func<IEnumerable<GeneDef>, IEnumerable<GeneDef>> getNonOverriddenGenes,
            Func<Genepack, string> getPhysicalKey,
            PlanAssemblerSearchBudget searchBudget = null)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (liveState == null)
                throw new ArgumentNullException(nameof(liveState));

            if (getGenepackGenes == null)
                throw new ArgumentNullException(nameof(getGenepackGenes));

            if (getNonOverriddenGenes == null)
                throw new ArgumentNullException(nameof(getNonOverriddenGenes));

            if (getPhysicalKey == null)
                throw new ArgumentNullException(nameof(getPhysicalKey));

            PlanReadinessResult geneScopeResult = PlanReadinessAnalyzer.AnalyzeAvailableGenepacks(
                plan,
                liveState.Scope.VisibleGenepacks,
                (desiredGenes, readinessMode, genepacks) => PlanGenepackCombinationSearcher.Search(
                    desiredGenes,
                    readinessMode,
                    genepacks,
                    getGenepackGenes));

            int visibleGenepackCount = liveState.Scope.VisibleGenepacks.Count;

            switch (geneScopeResult.Status)
            {
                case PlanReadinessStatus.Degraded:
                    return PlanAssemblerReadinessResult.CreateDegraded(geneScopeResult, visibleGenepackCount);

                case PlanReadinessStatus.EmptyTarget:
                    return PlanAssemblerReadinessResult.CreateEmptyTarget(geneScopeResult, visibleGenepackCount);

                case PlanReadinessStatus.NotReady:
                    return PlanAssemblerReadinessResult.CreateNotReady(geneScopeResult, visibleGenepackCount);

                case PlanReadinessStatus.Ready:
                    break;

                default:
                    throw new InvalidOperationException(
                        "Assembler gene scope returned an unsupported readiness status.");
            }

            CandidateEvaluation bestBlockedCandidate = null;
            var foundCandidate = false;
            PlanAssemblerSearchBudget budget = searchBudget ?? new PlanAssemblerSearchBudget();

            IEnumerable<PlanAssemblerCandidate> candidates = PlanAssemblerCandidateSearcher.Search(
                plan.DesiredGenes,
                plan.ReadinessMode,
                liveState.Scope,
                getGenepackGenes,
                getPhysicalKey,
                budget,
                includeHelpers: true);

            foreach (PlanAssemblerCandidate candidate in candidates)
            {
                foundCandidate = true;

                CandidateEvaluation evaluation = EvaluateCandidate(
                    candidate,
                    liveState,
                    getGenepackGenes,
                    getNonOverriddenGenes);

                if (evaluation.IsReady)
                {
                    return CreateReadyResult(geneScopeResult, visibleGenepackCount, liveState, evaluation);
                }

                if (bestBlockedCandidate == null || IsBetterBlockedCandidate(evaluation, bestBlockedCandidate))
                {
                    bestBlockedCandidate = evaluation;
                }
            }

            if (budget.IsExhausted)
                return PlanAssemblerReadinessResult.CreateSearchIncomplete(geneScopeResult, visibleGenepackCount);

            if (!foundCandidate || bestBlockedCandidate == null)
            {
                throw new InvalidOperationException(
                    "Ready assembler gene scope did not produce a concrete physical candidate.");
            }

            return PlanAssemblerReadinessResult.CreateBlocked(
                geneScopeResult,
                visibleGenepackCount,
                bestBlockedCandidate.Blockers,
                bestBlockedCandidate.RequiredComplexity,
                liveState.MaxComplexity,
                bestBlockedCandidate.RequiredArchiteCapsules,
                liveState.AvailableArchiteCapsules,
                bestBlockedCandidate.Candidate.Sources.Select(source => source.Genepack),
                bestBlockedCandidate.Candidate.MissingPrerequisites,
                bestBlockedCandidate.ActualMetabolism);
        }

        private static CandidateEvaluation EvaluateCandidate(
            PlanAssemblerCandidate candidate,
            PlanAssemblerLiveState liveState,
            Func<Genepack, IEnumerable<GeneDef>> getGenepackGenes,
            Func<IEnumerable<GeneDef>, IEnumerable<GeneDef>> getNonOverriddenGenes)
        {
            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));

            var flattenedGenes = new List<GeneDef>();

            foreach (PlanAssemblerGenepackSource source in candidate.Sources)
            {
                if (source == null)
                {
                    throw new ArgumentException("Assembler candidate cannot contain null sources.", nameof(candidate));
                }

                IEnumerable<GeneDef> genes = getGenepackGenes(source.Genepack) ??
                                             throw new InvalidOperationException(
                                                 "Genepack gene collection is unavailable.");

                foreach (GeneDef gene in genes)
                {
                    if (gene == null)
                    {
                        throw new ArgumentException(
                            "Gene collection cannot contain null values.",
                            nameof(getGenepackGenes));
                    }

                    flattenedGenes.Add(gene);
                }
            }

            IEnumerable<GeneDef> nonOverriddenGenes = getNonOverriddenGenes(flattenedGenes) ??
                                                      throw new InvalidOperationException(
                                                          "Non-overridden gene analysis returned a null collection.");

            var effectiveGenes = new List<GeneDef>();

            foreach (GeneDef gene in nonOverriddenGenes)
            {
                if (gene == null)
                {
                    throw new InvalidOperationException("Non-overridden gene analysis returned a null gene.");
                }

                effectiveGenes.Add(gene);
            }

            var requiredComplexity = 0;

            foreach (GeneDef gene in effectiveGenes)
                requiredComplexity += gene.biostatCpx;

            var distinctEffectiveGenes = new HashSet<GeneDef>(effectiveGenes);
            var requiredArchiteCapsules = 0;
            var actualMetabolism = 0;

            foreach (GeneDef gene in distinctEffectiveGenes)
            {
                requiredArchiteCapsules += gene.biostatArc;
                actualMetabolism += gene.biostatMet;
            }

            var blockers = new List<PlanAssemblerBlockerReason>();

            if (!candidate.IsPrerequisiteComplete)
                blockers.Add(PlanAssemblerBlockerReason.MissingPrerequisite);

            if (!liveState.IsAssemblerPowered)
                blockers.Add(PlanAssemblerBlockerReason.AssemblerUnpowered);

            if (candidate.Sources.Any(source => !source.IsFacilityPowered))
                blockers.Add(PlanAssemblerBlockerReason.UsedGeneBankUnpowered);

            if (requiredComplexity > liveState.MaxComplexity)
                blockers.Add(PlanAssemblerBlockerReason.InsufficientComplexity);

            if (requiredArchiteCapsules > 0 && !liveState.IsArchogeneticsFinished)
                blockers.Add(PlanAssemblerBlockerReason.ArchogeneticsResearchMissing);

            if (requiredArchiteCapsules > liveState.AvailableArchiteCapsules)
                blockers.Add(PlanAssemblerBlockerReason.InsufficientArchiteCapsules);

            if (actualMetabolism < GeneTuning.BiostatRange.TrueMin)
                blockers.Add(PlanAssemblerBlockerReason.MetabolismTooLow);

            return new CandidateEvaluation(
                candidate,
                blockers.AsReadOnly(),
                requiredComplexity,
                requiredArchiteCapsules,
                liveState.MaxComplexity,
                liveState.AvailableArchiteCapsules,
                actualMetabolism);
        }

        private static bool IsBetterBlockedCandidate(CandidateEvaluation candidate, CandidateEvaluation currentBest)
        {
            int comparison = currentBest.IsPrerequisiteComplete.CompareTo(candidate.IsPrerequisiteComplete);

            if (comparison != 0)
                return comparison < 0;

            comparison = candidate.Blockers.Count.CompareTo(currentBest.Blockers.Count);

            if (comparison != 0)
                return comparison < 0;

            comparison = candidate.ComplexityDeficit.CompareTo(currentBest.ComplexityDeficit);

            if (comparison != 0)
                return comparison < 0;

            comparison = candidate.ArchiteCapsuleDeficit.CompareTo(currentBest.ArchiteCapsuleDeficit);

            if (comparison != 0)
                return comparison < 0;

            comparison = candidate.MetabolismDeficit.CompareTo(currentBest.MetabolismDeficit);

            if (comparison != 0)
                return comparison < 0;

            return candidate.Candidate.Sources.Count < currentBest.Candidate.Sources.Count;
        }

        private static PlanAssemblerReadinessResult CreateReadyResult(
            PlanReadinessResult geneScopeResult,
            int visibleGenepackCount,
            PlanAssemblerLiveState liveState,
            CandidateEvaluation evaluation)
        {
            return PlanAssemblerReadinessResult.CreateReady(
                geneScopeResult,
                visibleGenepackCount,
                evaluation.RequiredComplexity,
                liveState.MaxComplexity,
                evaluation.RequiredArchiteCapsules,
                liveState.AvailableArchiteCapsules,
                evaluation.Candidate.Sources.Select(source => source.Genepack),
                evaluation.ActualMetabolism);
        }

        private static IEnumerable<GeneDef> GetNonOverriddenGenes(IEnumerable<GeneDef> genes)
        {
            if (genes == null)
                throw new ArgumentNullException(nameof(genes));

            var genesWithType = new List<GeneDefWithType>();

            foreach (GeneDef gene in genes)
            {
                if (gene == null)
                {
                    throw new ArgumentException("Gene collection cannot contain null values.", nameof(genes));
                }

                genesWithType.Add(new GeneDefWithType(gene, true));
            }

            return genesWithType.NonOverriddenGenes();
        }

        private static IEnumerable<GeneDef> GetGenepackGenes(Genepack genepack)
        {
            GeneSet geneSet = genepack.GeneSet ??
                              throw new InvalidOperationException("Genepack does not have a gene set.");

            List<GeneDef> genes = geneSet.GenesListForReading;

            return genes == null
                ? throw new InvalidOperationException("Genepack gene collection is unavailable.")
                : (IEnumerable<GeneDef>)genes;
        }

        private static string GetStablePhysicalKey(Genepack genepack)
        {
            return genepack.ThingID ?? string.Empty;
        }

        private sealed class CandidateEvaluation
        {
            internal CandidateEvaluation(
                PlanAssemblerCandidate candidate,
                IReadOnlyList<PlanAssemblerBlockerReason> blockers,
                int requiredComplexity,
                int requiredArchiteCapsules,
                int availableComplexity,
                int availableArchiteCapsules,
                int actualMetabolism)
            {
                Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
                Blockers = blockers ?? throw new ArgumentNullException(nameof(blockers));
                RequiredComplexity = requiredComplexity;
                RequiredArchiteCapsules = requiredArchiteCapsules;
                ComplexityDeficit = Math.Max(0, requiredComplexity - availableComplexity);
                ArchiteCapsuleDeficit = Math.Max(0, requiredArchiteCapsules - availableArchiteCapsules);
                ActualMetabolism = actualMetabolism;
                MetabolismDeficit = Math.Max(0, GeneTuning.BiostatRange.TrueMin - actualMetabolism);
            }

            internal PlanAssemblerCandidate Candidate { get; }
            internal IReadOnlyList<PlanAssemblerBlockerReason> Blockers { get; }
            internal int RequiredComplexity { get; }
            internal int RequiredArchiteCapsules { get; }
            internal int ComplexityDeficit { get; }
            internal int ArchiteCapsuleDeficit { get; }
            internal int ActualMetabolism { get; }
            internal int MetabolismDeficit { get; }
            internal bool IsPrerequisiteComplete => Candidate.IsPrerequisiteComplete;
            internal bool IsReady => Blockers.Count == 0;
        }
    }
}