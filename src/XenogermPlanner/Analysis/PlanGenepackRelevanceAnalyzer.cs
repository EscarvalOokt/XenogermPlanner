using System;
using System.Collections.Generic;
using Verse;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Analysis
{
    internal sealed class PlanGenepackRelevanceAnalyzer
    {
        private readonly List<PlanCandidate> _candidates;
        private bool _candidatesSorted;
        private bool _hasEvaluated;

        internal PlanGenepackRelevanceAnalyzer(
            IReadOnlyList<XenogermPlan> plans,
            PlanGenepackInventorySnapshot inventory) : this(plans, inventory, PlanReadinessAnalyzer.Analyze)
        {
        }

        internal PlanGenepackRelevanceAnalyzer(
            IReadOnlyList<XenogermPlan> plans,
            PlanGenepackInventorySnapshot inventory,
            Func<XenogermPlan, PlanGenepackInventorySnapshot, PlanReadinessResult> analyzeReadiness)
        {
            if (plans == null)
                throw new ArgumentNullException(nameof(plans));

            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));

            if (analyzeReadiness == null)
                throw new ArgumentNullException(nameof(analyzeReadiness));

            _candidates = BuildCandidates(plans, inventory, analyzeReadiness);
        }

        internal IReadOnlyList<XenogermPlan> Evaluate(IReadOnlyCollection<GeneDef> offeredGenes)
        {
            HashSet<GeneDef> offeredGeneSet = CreateOfferedGeneSet(offeredGenes);

            if (_hasEvaluated && !_candidatesSorted)
            {
                _candidates.Sort(CompareCandidates);
                _candidatesSorted = true;
            }

            var matchingPlans = new List<XenogermPlan>();

            for (var index = 0; index < _candidates.Count; index++)
            {
                PlanCandidate candidate = _candidates[index];

                if (candidate.IsRelevant(offeredGeneSet))
                    matchingPlans.Add(candidate.Plan);
            }

            if (!_candidatesSorted)
                matchingPlans.Sort(ComparePlans);

            _hasEvaluated = true;

            return matchingPlans.AsReadOnly();
        }

        private static HashSet<GeneDef> CreateOfferedGeneSet(IReadOnlyCollection<GeneDef> offeredGenes)
        {
            if (offeredGenes == null)
                throw new ArgumentNullException(nameof(offeredGenes));

            var offeredGeneSet = new HashSet<GeneDef>();

            foreach (GeneDef offeredGene in offeredGenes)
            {
                if (offeredGene == null)
                {
                    throw new ArgumentException(
                        "Offered gene collection cannot contain null values.",
                        nameof(offeredGenes));
                }

                offeredGeneSet.Add(offeredGene);
            }

            if (offeredGeneSet.Count == 0)
                throw new ArgumentException("Offered gene collection cannot be empty.", nameof(offeredGenes));

            return offeredGeneSet;
        }

        private static List<PlanCandidate> BuildCandidates(
            IReadOnlyList<XenogermPlan> plans,
            PlanGenepackInventorySnapshot inventory,
            Func<XenogermPlan, PlanGenepackInventorySnapshot, PlanReadinessResult> analyzeReadiness)
        {
            var candidates = new List<PlanCandidate>();

            foreach (XenogermPlan plan in plans)
            {
                if (plan == null)
                    throw new ArgumentException("Plan collection cannot contain null values.", nameof(plans));

                PlanReadinessResult readiness = analyzeReadiness(plan, inventory) ??
                                                throw new InvalidOperationException(
                                                    "Plan readiness analyzer returned a null result.");

                if (readiness.Status != PlanReadinessStatus.NotReady)
                    continue;

                var targetGenes = new HashSet<GeneDef>(plan.DesiredGenes);
                var neededGenes = new HashSet<GeneDef>();

                foreach (PlanGeneCoverageDiagnostic diagnostic in readiness.GeneCoverageDiagnostics)
                {
                    if (IsRelevantNeed(plan.ReadinessMode, diagnostic.State))
                        neededGenes.Add(diagnostic.Gene);
                }

                if (neededGenes.Count == 0)
                    continue;

                candidates.Add(new PlanCandidate(plan, targetGenes, neededGenes));
            }

            return candidates;
        }

        private static bool IsRelevantNeed(PlanReadinessMode readinessMode, PlanGeneCoverageState state)
        {
            switch (readinessMode)
            {
                case PlanReadinessMode.Coverage:
                    return state == PlanGeneCoverageState.Missing;

                case PlanReadinessMode.ExactPayload:
                    return state == PlanGeneCoverageState.Missing ||
                           state == PlanGeneCoverageState.ExactPayloadConflict;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(readinessMode),
                        readinessMode,
                        "Unknown plan readiness mode.");
            }
        }

        private static int CompareCandidates(PlanCandidate left, PlanCandidate right)
        {
            return ComparePlans(left.Plan, right.Plan);
        }

        private static int ComparePlans(XenogermPlan left, XenogermPlan right)
        {
            int nameComparison = StringComparer.OrdinalIgnoreCase.Compare(left.Name, right.Name);

            return nameComparison != 0 ? nameComparison : StringComparer.Ordinal.Compare(left.Id, right.Id);
        }

        private sealed class PlanCandidate
        {
            private readonly HashSet<GeneDef> _neededGenes;
            private readonly HashSet<GeneDef> _targetGenes;

            internal PlanCandidate(XenogermPlan plan, HashSet<GeneDef> targetGenes, HashSet<GeneDef> neededGenes)
            {
                Plan = plan ?? throw new ArgumentNullException(nameof(plan));
                ReadinessMode = plan.ReadinessMode;
                _targetGenes = targetGenes ?? throw new ArgumentNullException(nameof(targetGenes));
                _neededGenes = neededGenes ?? throw new ArgumentNullException(nameof(neededGenes));
            }

            internal XenogermPlan Plan { get; }
            internal PlanReadinessMode ReadinessMode { get; }

            internal bool IsRelevant(HashSet<GeneDef> offeredGenes)
            {
                if (ReadinessMode == PlanReadinessMode.ExactPayload)
                {
                    foreach (GeneDef offeredGene in offeredGenes)
                    {
                        if (!_targetGenes.Contains(offeredGene))
                            return false;
                    }
                }

                foreach (GeneDef offeredGene in offeredGenes)
                {
                    if (_neededGenes.Contains(offeredGene))
                        return true;
                }

                return false;
            }
        }
    }
}