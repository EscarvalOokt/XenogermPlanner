using System;
using Verse;

namespace XenogermPlanner.Analysis
{
    public sealed class PlanGeneConflictDiagnostic
    {
        internal PlanGeneConflictDiagnostic(
            GeneDef firstGene,
            GeneDef secondGene,
            PlanGeneConflictKind kind,
            GeneDef overridingGene,
            GeneDef overriddenGene)
        {
            FirstGene = firstGene ?? throw new ArgumentNullException(nameof(firstGene));
            SecondGene = secondGene ?? throw new ArgumentNullException(nameof(secondGene));

            if (ReferenceEquals(firstGene, secondGene))
            {
                throw new ArgumentException("Conflict diagnostic requires two distinct genes.", nameof(secondGene));
            }

            ValidateKind(kind);
            ValidatePrediction(firstGene, secondGene, kind, overridingGene, overriddenGene);

            Kind = kind;
            OverridingGene = overridingGene;
            OverriddenGene = overriddenGene;
        }

        public GeneDef FirstGene { get; }
        public GeneDef SecondGene { get; }
        public PlanGeneConflictKind Kind { get; }
        public GeneDef OverridingGene { get; }
        public GeneDef OverriddenGene { get; }
        public bool HasPredictedWinner => OverridingGene != null;

        private static void ValidateKind(PlanGeneConflictKind kind)
        {
            if (kind != PlanGeneConflictKind.Ordinary &&
                kind != PlanGeneConflictKind.RandomChosen &&
                kind != PlanGeneConflictKind.Mixed)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported gene conflict kind.");
            }
        }

        private static void ValidatePrediction(
            GeneDef firstGene,
            GeneDef secondGene,
            PlanGeneConflictKind kind,
            GeneDef overridingGene,
            GeneDef overriddenGene)
        {
            if ((overridingGene == null) != (overriddenGene == null))
            {
                throw new ArgumentException("Override prediction must contain both winner and loser.");
            }

            if (overridingGene == null)
                return;

            if (kind != PlanGeneConflictKind.Ordinary)
            {
                throw new ArgumentException("Only ordinary conflicts can contain a predicted winner.");
            }

            bool predictionMatchesPair =
                (ReferenceEquals(overridingGene, firstGene) && ReferenceEquals(overriddenGene, secondGene)) ||
                (ReferenceEquals(overridingGene, secondGene) && ReferenceEquals(overriddenGene, firstGene));

            if (!predictionMatchesPair)
            {
                throw new ArgumentException("Override prediction must refer to the diagnostic gene pair.");
            }
        }
    }
}