using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Verse;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Templates
{
    internal sealed class PlanXenogermTemplateCandidateSearchResult
    {
        private readonly ReadOnlyCollection<PlanXenogermTemplateCandidate> _candidates;
        private readonly ReadOnlyCollection<GeneDef> _targetGenes;

        internal PlanXenogermTemplateCandidateSearchResult(
            IEnumerable<PlanXenogermTemplateCandidate> candidates,
            IEnumerable<GeneDef> targetGenes,
            PlanReadinessMode readinessMode,
            bool isComplete = true)
        {
            if (candidates == null)
                throw new ArgumentNullException(nameof(candidates));

            ValidateReadinessMode(readinessMode);

            HashSet<GeneDef> copiedTargetGenes =
                GenepackCompositionUtility.CopyDistinctGenes(targetGenes, nameof(targetGenes));
            var copiedCandidates = new List<PlanXenogermTemplateCandidate>();
            var candidateKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (PlanXenogermTemplateCandidate candidate in candidates)
            {
                if (candidate == null)
                    throw new ArgumentException("Candidate collection cannot contain null values.", nameof(candidates));

                if (!candidateKeys.Add(candidate.CandidateKey))
                {
                    throw new ArgumentException(
                        "Candidate collection cannot contain duplicate keys.",
                        nameof(candidates));
                }

                copiedCandidates.Add(candidate);
            }

            _candidates = copiedCandidates.AsReadOnly();
            _targetGenes = copiedTargetGenes.OrderBy(gene => gene.defName ?? string.Empty, StringComparer.Ordinal)
                .ToList()
                .AsReadOnly();
            IsComplete = isComplete;
        }

        internal IReadOnlyList<PlanXenogermTemplateCandidate> Candidates => _candidates;
        internal IReadOnlyList<GeneDef> TargetGenes => _targetGenes;
        internal bool IsComplete { get; }

        internal PlanXenogermTemplateCandidate AutomaticCandidate => _candidates.Count == 0 ? null : _candidates[0];

        internal bool HasCandidate => AutomaticCandidate != null;

        private static void ValidateReadinessMode(PlanReadinessMode readinessMode)
        {
            if (readinessMode != PlanReadinessMode.Coverage && readinessMode != PlanReadinessMode.ExactPayload)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(readinessMode),
                    readinessMode,
                    "Unsupported plan readiness mode.");
            }
        }
    }
}