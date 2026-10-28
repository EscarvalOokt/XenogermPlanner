using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Verse;

namespace XenogermPlanner.Donors
{
    public sealed class PlanPotentialDonorAnalysisResult
    {
        private readonly Dictionary<GeneDef, PlanPotentialDonorGeneDiagnostic> _diagnosticsByGene;
        private readonly ReadOnlyCollection<PlanPotentialDonorGeneDiagnostic> _geneDiagnostics;

        private PlanPotentialDonorAnalysisResult(
            bool isAvailable,
            IEnumerable<PlanPotentialDonorGeneDiagnostic> geneDiagnostics)
        {
            if (geneDiagnostics == null)
                throw new ArgumentNullException(nameof(geneDiagnostics));

            var copiedDiagnostics = new List<PlanPotentialDonorGeneDiagnostic>();
            _diagnosticsByGene = new Dictionary<GeneDef, PlanPotentialDonorGeneDiagnostic>();

            foreach (PlanPotentialDonorGeneDiagnostic diagnostic in geneDiagnostics)
            {
                if (diagnostic == null)
                {
                    throw new ArgumentException(
                        "Potential donor diagnostic collection cannot contain null values.",
                        nameof(geneDiagnostics));
                }

                if (!_diagnosticsByGene.TryAdd(diagnostic.Gene, diagnostic))
                {
                    throw new ArgumentException(
                        "Potential donor diagnostic collection cannot contain duplicate genes.",
                        nameof(geneDiagnostics));
                }

                copiedDiagnostics.Add(diagnostic);
            }

            if (!isAvailable && copiedDiagnostics.Count > 0)
            {
                throw new ArgumentException(
                    "Unavailable potential donor analysis cannot contain diagnostics.",
                    nameof(geneDiagnostics));
            }

            copiedDiagnostics.Sort(CompareDiagnostics);

            IsAvailable = isAvailable;
            _geneDiagnostics = copiedDiagnostics.AsReadOnly();
        }

        public bool IsAvailable { get; }
        public IReadOnlyList<PlanPotentialDonorGeneDiagnostic> GeneDiagnostics => _geneDiagnostics;

        internal static PlanPotentialDonorAnalysisResult Unavailable { get; } = new PlanPotentialDonorAnalysisResult(
            false,
            Array.Empty<PlanPotentialDonorGeneDiagnostic>());

        internal static PlanPotentialDonorAnalysisResult CreateAvailable(
            IEnumerable<PlanPotentialDonorGeneDiagnostic> geneDiagnostics)
        {
            return new PlanPotentialDonorAnalysisResult(true, geneDiagnostics);
        }

        public bool TryGetDiagnostic(GeneDef gene, out PlanPotentialDonorGeneDiagnostic diagnostic)
        {
            if (gene == null)
                throw new ArgumentNullException(nameof(gene));

            return _diagnosticsByGene.TryGetValue(gene, out diagnostic);
        }

        private static int CompareDiagnostics(
            PlanPotentialDonorGeneDiagnostic left,
            PlanPotentialDonorGeneDiagnostic right)
        {
            return StringComparer.Ordinal.Compare(
                left.Gene.defName ?? string.Empty,
                right.Gene.defName ?? string.Empty);
        }
    }
}