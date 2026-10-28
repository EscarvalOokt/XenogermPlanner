using System;

namespace XenogermPlanner.Assemblers
{
    internal sealed class PlanAssemblerSearchBudget
    {
        internal const int DefaultMaxVisitedStates = 10000;

        internal PlanAssemblerSearchBudget(int maxVisitedStates = DefaultMaxVisitedStates)
        {
            if (maxVisitedStates <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxVisitedStates));

            MaxVisitedStates = maxVisitedStates;
        }

        internal int MaxVisitedStates { get; }
        internal int VisitedStates { get; private set; }
        internal bool IsExhausted { get; private set; }

        internal bool TryVisitState()
        {
            if (VisitedStates == MaxVisitedStates)
            {
                IsExhausted = true;
                return false;
            }

            VisitedStates++;
            return true;
        }
    }
}