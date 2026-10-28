using System;

namespace XenogermPlanner.Templates
{
    internal sealed class PlanXenogermTemplateCandidateSearchLimits
    {
        private const int DefaultMaxVisitedNodes = 10000;
        private const int DefaultMaxRetainedCandidates = 64;

        internal PlanXenogermTemplateCandidateSearchLimits(int maxVisitedNodes, int maxRetainedCandidates)
        {
            if (maxVisitedNodes <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxVisitedNodes),
                    maxVisitedNodes,
                    "Maximum visited node count must be positive.");
            }

            if (maxRetainedCandidates <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxRetainedCandidates),
                    maxRetainedCandidates,
                    "Maximum retained candidate count must be positive.");
            }

            MaxVisitedNodes = maxVisitedNodes;
            MaxRetainedCandidates = maxRetainedCandidates;
        }

        internal static PlanXenogermTemplateCandidateSearchLimits Default { get; } =
            new PlanXenogermTemplateCandidateSearchLimits(DefaultMaxVisitedNodes, DefaultMaxRetainedCandidates);

        internal int MaxVisitedNodes { get; }
        internal int MaxRetainedCandidates { get; }
    }
}