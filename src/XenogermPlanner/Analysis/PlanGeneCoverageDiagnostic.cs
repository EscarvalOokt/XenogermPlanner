using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Verse;

namespace XenogermPlanner.Analysis
{
    public sealed class PlanGeneCoverageDiagnostic
    {
        private readonly ReadOnlyCollection<PlanGenepackCompositionDiagnostic> _sourceGenepackCompositions;

        internal PlanGeneCoverageDiagnostic(
            GeneDef gene,
            PlanGeneCoverageState state,
            IEnumerable<PlanGenepackCompositionDiagnostic> sourceGenepackCompositions)
        {
            Gene = gene ?? throw new ArgumentNullException(nameof(gene));

            if (!Enum.IsDefined(typeof(PlanGeneCoverageState), state))
                throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown gene coverage state.");

            if (sourceGenepackCompositions == null)
                throw new ArgumentNullException(nameof(sourceGenepackCompositions));

            var copiedCompositions = new List<PlanGenepackCompositionDiagnostic>();

            foreach (PlanGenepackCompositionDiagnostic composition in sourceGenepackCompositions)
            {
                if (composition == null)
                {
                    throw new ArgumentException(
                        "Source genepack composition collection cannot contain null values.",
                        nameof(sourceGenepackCompositions));
                }

                if (!ContainsGene(composition.Genes, gene))
                {
                    throw new ArgumentException(
                        "Source genepack composition must contain the covered gene.",
                        nameof(sourceGenepackCompositions));
                }

                copiedCompositions.Add(composition);
            }

            ValidateState(state, copiedCompositions, nameof(sourceGenepackCompositions));

            State = state;
            _sourceGenepackCompositions = copiedCompositions.AsReadOnly();
        }

        public GeneDef Gene { get; }
        public PlanGeneCoverageState State { get; }
        public bool IsCovered => State != PlanGeneCoverageState.Missing;

        public IReadOnlyList<PlanGenepackCompositionDiagnostic> SourceGenepackCompositions =>
            _sourceGenepackCompositions;

        private static void ValidateState(
            PlanGeneCoverageState state,
            IReadOnlyCollection<PlanGenepackCompositionDiagnostic> sourceCompositions,
            string parameterName)
        {
            switch (state)
            {
                case PlanGeneCoverageState.Available:
                    if (sourceCompositions.Count == 0)
                    {
                        throw new ArgumentException(
                            "Available gene coverage must contain at least one source genepack composition.",
                            parameterName);
                    }

                    return;

                case PlanGeneCoverageState.ExactPayloadConflict:
                    if (sourceCompositions.Count == 0)
                    {
                        throw new ArgumentException(
                            "Exact-payload conflict coverage must contain at least one source genepack composition.",
                            parameterName);
                    }

                    foreach (PlanGenepackCompositionDiagnostic composition in sourceCompositions)
                    {
                        if (composition.IsExactPayloadEligible)
                        {
                            throw new ArgumentException(
                                "Exact-payload conflict coverage cannot contain an exact-payload eligible composition.",
                                parameterName);
                        }
                    }

                    return;

                case PlanGeneCoverageState.Missing:
                    if (sourceCompositions.Count > 0)
                    {
                        throw new ArgumentException(
                            "Missing gene coverage cannot contain source genepack compositions.",
                            parameterName);
                    }

                    return;

                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown gene coverage state.");
            }
        }

        private static bool ContainsGene(IEnumerable<GeneDef> genes, GeneDef expectedGene)
        {
            foreach (GeneDef gene in genes)
            {
                if (gene == expectedGene)
                    return true;
            }

            return false;
        }
    }
}