using System;

namespace XenogermPlanner.Assemblers
{
    internal sealed class PlanAssemblerLiveState
    {
        internal PlanAssemblerLiveState(
            PlanAssemblerScopeSnapshot scope,
            bool isAssemblerPowered,
            int maxComplexity,
            int availableArchiteCapsules,
            bool isArchogeneticsFinished)
        {
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));

            if (maxComplexity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxComplexity),
                    maxComplexity,
                    "Maximum complexity cannot be negative.");
            }

            if (availableArchiteCapsules < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(availableArchiteCapsules),
                    availableArchiteCapsules,
                    "Available archite capsule count cannot be negative.");
            }

            IsAssemblerPowered = isAssemblerPowered;
            MaxComplexity = maxComplexity;
            AvailableArchiteCapsules = availableArchiteCapsules;
            IsArchogeneticsFinished = isArchogeneticsFinished;
        }

        internal PlanAssemblerScopeSnapshot Scope { get; }
        internal bool IsAssemblerPowered { get; }
        internal int MaxComplexity { get; }
        internal int AvailableArchiteCapsules { get; }
        internal bool IsArchogeneticsFinished { get; }
    }
}