using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Verse;

namespace XenogermPlanner.Analysis
{
    public sealed class PlanReadinessResult
    {
        private readonly ReadOnlyCollection<GeneDef> _coveredGenes;
        private readonly ReadOnlyCollection<PlanGeneCoverageDiagnostic> _geneCoverageDiagnostics;
        private readonly ReadOnlyCollection<GeneDef> _missingGenes;

        private PlanReadinessResult(
            PlanReadinessStatus status,
            PlanReadinessUnavailableReason unavailableReason,
            IEnumerable<GeneDef> coveredGenes,
            IEnumerable<GeneDef> missingGenes,
            bool hasExactPayloadConflict,
            IEnumerable<PlanGeneCoverageDiagnostic> geneCoverageDiagnostics)
        {
            ValidateStatus(status);
            ValidateUnavailableReason(unavailableReason);

            List<GeneDef> copiedCoveredGenes = CopyDistinctGenes(coveredGenes, nameof(coveredGenes));

            List<GeneDef> copiedMissingGenes = CopyDistinctGenes(missingGenes, nameof(missingGenes));

            List<PlanGeneCoverageDiagnostic> copiedGeneCoverageDiagnostics =
                CopyGeneCoverageDiagnostics(geneCoverageDiagnostics);

            ValidateResult(
                status,
                unavailableReason,
                copiedCoveredGenes,
                copiedMissingGenes,
                hasExactPayloadConflict,
                copiedGeneCoverageDiagnostics);

            Status = status;
            UnavailableReason = unavailableReason;
            _coveredGenes = copiedCoveredGenes.AsReadOnly();
            _missingGenes = copiedMissingGenes.AsReadOnly();
            _geneCoverageDiagnostics = copiedGeneCoverageDiagnostics.AsReadOnly();
            HasExactPayloadConflict = hasExactPayloadConflict;
        }

        public PlanReadinessStatus Status { get; }
        public PlanReadinessUnavailableReason UnavailableReason { get; }
        public IReadOnlyCollection<GeneDef> CoveredGenes => _coveredGenes;
        public IReadOnlyCollection<GeneDef> MissingGenes => _missingGenes;
        public IReadOnlyList<PlanGeneCoverageDiagnostic> GeneCoverageDiagnostics => _geneCoverageDiagnostics;
        public bool HasExactPayloadConflict { get; }
        public bool IsReady => Status == PlanReadinessStatus.Ready;

        internal static PlanReadinessResult CreateReady(IEnumerable<GeneDef> coveredGenes)
        {
            return CreateReady(coveredGenes, Array.Empty<PlanGeneCoverageDiagnostic>());
        }

        internal static PlanReadinessResult CreateReady(
            IEnumerable<GeneDef> coveredGenes,
            IEnumerable<PlanGeneCoverageDiagnostic> geneCoverageDiagnostics)
        {
            return new PlanReadinessResult(
                PlanReadinessStatus.Ready,
                PlanReadinessUnavailableReason.None,
                coveredGenes,
                Array.Empty<GeneDef>(),
                false,
                geneCoverageDiagnostics);
        }

        internal static PlanReadinessResult CreateNotReady(
            IEnumerable<GeneDef> coveredGenes,
            IEnumerable<GeneDef> missingGenes,
            bool hasExactPayloadConflict)
        {
            return CreateNotReady(
                coveredGenes,
                missingGenes,
                hasExactPayloadConflict,
                Array.Empty<PlanGeneCoverageDiagnostic>());
        }

        internal static PlanReadinessResult CreateNotReady(
            IEnumerable<GeneDef> coveredGenes,
            IEnumerable<GeneDef> missingGenes,
            bool hasExactPayloadConflict,
            IEnumerable<PlanGeneCoverageDiagnostic> geneCoverageDiagnostics)
        {
            return new PlanReadinessResult(
                PlanReadinessStatus.NotReady,
                PlanReadinessUnavailableReason.None,
                coveredGenes,
                missingGenes,
                hasExactPayloadConflict,
                geneCoverageDiagnostics);
        }

        internal static PlanReadinessResult CreateEmptyTarget()
        {
            return new PlanReadinessResult(
                PlanReadinessStatus.EmptyTarget,
                PlanReadinessUnavailableReason.None,
                Array.Empty<GeneDef>(),
                Array.Empty<GeneDef>(),
                false,
                Array.Empty<PlanGeneCoverageDiagnostic>());
        }

        internal static PlanReadinessResult CreateDegraded(
            IEnumerable<GeneDef> coveredGenes,
            IEnumerable<GeneDef> missingGenes)
        {
            return CreateDegraded(coveredGenes, missingGenes, Array.Empty<PlanGeneCoverageDiagnostic>());
        }

        internal static PlanReadinessResult CreateDegraded(
            IEnumerable<GeneDef> coveredGenes,
            IEnumerable<GeneDef> missingGenes,
            IEnumerable<PlanGeneCoverageDiagnostic> geneCoverageDiagnostics)
        {
            return new PlanReadinessResult(
                PlanReadinessStatus.Degraded,
                PlanReadinessUnavailableReason.None,
                coveredGenes,
                missingGenes,
                false,
                geneCoverageDiagnostics);
        }

        internal static PlanReadinessResult CreateUnavailable(PlanReadinessUnavailableReason unavailableReason)
        {
            return new PlanReadinessResult(
                PlanReadinessStatus.Unavailable,
                unavailableReason,
                Array.Empty<GeneDef>(),
                Array.Empty<GeneDef>(),
                false,
                Array.Empty<PlanGeneCoverageDiagnostic>());
        }

        private static List<GeneDef> CopyDistinctGenes(IEnumerable<GeneDef> genes, string parameterName)
        {
            if (genes == null)
                throw new ArgumentNullException(parameterName);

            var copiedGenes = new List<GeneDef>();
            var distinctGenes = new HashSet<GeneDef>();

            foreach (GeneDef gene in genes)
            {
                if (gene == null)
                {
                    throw new ArgumentException("Gene collection cannot contain null values.", parameterName);
                }

                if (distinctGenes.Add(gene))
                    copiedGenes.Add(gene);
            }

            return copiedGenes;
        }

        private static List<PlanGeneCoverageDiagnostic> CopyGeneCoverageDiagnostics(
            IEnumerable<PlanGeneCoverageDiagnostic> geneCoverageDiagnostics)
        {
            if (geneCoverageDiagnostics == null)
            {
                throw new ArgumentNullException(nameof(geneCoverageDiagnostics));
            }

            var copiedDiagnostics = new List<PlanGeneCoverageDiagnostic>();
            var diagnosticGenes = new HashSet<GeneDef>();

            foreach (PlanGeneCoverageDiagnostic diagnostic in geneCoverageDiagnostics)
            {
                if (diagnostic == null)
                {
                    throw new ArgumentException(
                        "Gene coverage diagnostic collection cannot contain null values.",
                        nameof(geneCoverageDiagnostics));
                }

                if (!diagnosticGenes.Add(diagnostic.Gene))
                {
                    throw new ArgumentException(
                        "Gene coverage diagnostic collection cannot contain duplicate genes.",
                        nameof(geneCoverageDiagnostics));
                }

                copiedDiagnostics.Add(diagnostic);
            }

            return copiedDiagnostics;
        }

        private static void ValidateStatus(PlanReadinessStatus status)
        {
            if (status != PlanReadinessStatus.Ready &&
                status != PlanReadinessStatus.NotReady &&
                status != PlanReadinessStatus.EmptyTarget &&
                status != PlanReadinessStatus.Degraded &&
                status != PlanReadinessStatus.Unavailable)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported plan readiness status.");
            }
        }

        private static void ValidateUnavailableReason(PlanReadinessUnavailableReason unavailableReason)
        {
            if (unavailableReason != PlanReadinessUnavailableReason.None &&
                unavailableReason != PlanReadinessUnavailableReason.NoActiveMap)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unavailableReason),
                    unavailableReason,
                    "Unsupported plan readiness unavailable reason.");
            }
        }

        private static void ValidateResult(
            PlanReadinessStatus status,
            PlanReadinessUnavailableReason unavailableReason,
            IReadOnlyCollection<GeneDef> coveredGenes,
            IReadOnlyCollection<GeneDef> missingGenes,
            bool hasExactPayloadConflict,
            IReadOnlyCollection<PlanGeneCoverageDiagnostic> geneCoverageDiagnostics)
        {
            if (status == PlanReadinessStatus.Unavailable)
            {
                if (unavailableReason == PlanReadinessUnavailableReason.None)
                {
                    throw new ArgumentException(
                        "Unavailable readiness result requires an unavailable reason.",
                        nameof(unavailableReason));
                }
            }
            else if (unavailableReason != PlanReadinessUnavailableReason.None)
            {
                throw new ArgumentException(
                    "Only unavailable readiness result can have an unavailable reason.",
                    nameof(unavailableReason));
            }

            var coveredGeneSet = new HashSet<GeneDef>(coveredGenes);
            var missingGeneSet = new HashSet<GeneDef>(missingGenes);

            foreach (GeneDef missingGene in missingGenes)
            {
                if (coveredGeneSet.Contains(missingGene))
                {
                    throw new ArgumentException("Covered and missing gene collections cannot overlap.");
                }
            }

            if (status == PlanReadinessStatus.Ready && missingGenes.Count > 0)
            {
                throw new ArgumentException("Ready result cannot contain missing genes.", nameof(missingGenes));
            }

            if ((status == PlanReadinessStatus.EmptyTarget || status == PlanReadinessStatus.Unavailable) &&
                (coveredGenes.Count > 0 || missingGenes.Count > 0 || geneCoverageDiagnostics.Count > 0))
            {
                throw new ArgumentException("Empty-target and unavailable results cannot contain gene diagnostics.");
            }

            if (hasExactPayloadConflict && status != PlanReadinessStatus.NotReady)
            {
                throw new ArgumentException(
                    "Exact payload conflict is only valid for a not-ready result.",
                    nameof(hasExactPayloadConflict));
            }

            foreach (PlanGeneCoverageDiagnostic diagnostic in geneCoverageDiagnostics)
            {
                bool isCoveredGene = coveredGeneSet.Contains(diagnostic.Gene);
                bool isMissingGene = missingGeneSet.Contains(diagnostic.Gene);

                if (!isCoveredGene && !isMissingGene)
                {
                    throw new ArgumentException(
                        "Gene coverage diagnostic must correspond to a covered or missing gene.",
                        nameof(geneCoverageDiagnostics));
                }

                if (diagnostic.IsCovered != isCoveredGene)
                {
                    throw new ArgumentException(
                        "Gene coverage diagnostic state must match covered and missing collections.",
                        nameof(geneCoverageDiagnostics));
                }
            }

            if (geneCoverageDiagnostics.Count > 0 &&
                geneCoverageDiagnostics.Count != coveredGenes.Count + missingGenes.Count)
            {
                throw new ArgumentException(
                    "Gene coverage diagnostics must describe every covered and missing gene.",
                    nameof(geneCoverageDiagnostics));
            }
        }
    }
}