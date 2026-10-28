using System;

namespace XenogermPlanner.Api
{
    public sealed class GenepackRelevancePlanMatch
    {
        public GenepackRelevancePlanMatch(string planId, string displayName)
        {
            if (string.IsNullOrWhiteSpace(planId))
                throw new ArgumentException("Plan ID cannot be null, empty or whitespace.", nameof(planId));

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "Plan display name cannot be null, empty or whitespace.",
                    nameof(displayName));
            }

            PlanId = planId;
            DisplayName = displayName;
        }

        public string PlanId { get; }
        public string DisplayName { get; }
    }
}