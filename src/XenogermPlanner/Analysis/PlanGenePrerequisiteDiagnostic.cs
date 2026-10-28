using System;
using Verse;

namespace XenogermPlanner.Analysis
{
    public sealed class PlanGenePrerequisiteDiagnostic
    {
        internal PlanGenePrerequisiteDiagnostic(GeneDef dependentGene, GeneDef prerequisiteGene)
        {
            DependentGene = dependentGene ?? throw new ArgumentNullException(nameof(dependentGene));
            PrerequisiteGene = prerequisiteGene ?? throw new ArgumentNullException(nameof(prerequisiteGene));
        }

        public GeneDef DependentGene { get; }
        public GeneDef PrerequisiteGene { get; }
    }
}