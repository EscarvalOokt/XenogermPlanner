using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RimWorld;
using XenogermPlanner.Analysis;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Assemblers
{
    public sealed class PlanAssemblerReadinessResult
    {
        private readonly ReadOnlyCollection<PlanAssemblerBlockerReason> _blockerReasons;
        private readonly ReadOnlyCollection<Genepack> _candidateGenepacks;
        private readonly ReadOnlyCollection<PlanGenePrerequisiteDiagnostic> _missingPrerequisites;

        private PlanAssemblerReadinessResult(
            PlanAssemblerReadinessStatus status,
            PlanReadinessResult geneScopeResult,
            int visibleGenepackCount,
            IEnumerable<PlanAssemblerBlockerReason> blockerReasons,
            int requiredComplexity,
            int availableComplexity,
            int requiredArchiteCapsules,
            int availableArchiteCapsules,
            IEnumerable<Genepack> candidateGenepacks,
            IEnumerable<PlanGenePrerequisiteDiagnostic> missingPrerequisites,
            int actualMetabolism)
        {
            ValidateStatus(status);

            GeneScopeResult = geneScopeResult ?? throw new ArgumentNullException(nameof(geneScopeResult));

            if (visibleGenepackCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(visibleGenepackCount),
                    visibleGenepackCount,
                    "Visible genepack count cannot be negative.");
            }

            if (requiredComplexity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredComplexity),
                    requiredComplexity,
                    "Required complexity cannot be negative.");
            }

            if (availableComplexity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(availableComplexity),
                    availableComplexity,
                    "Available complexity cannot be negative.");
            }

            if (requiredArchiteCapsules < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredArchiteCapsules),
                    requiredArchiteCapsules,
                    "Required archite capsule count cannot be negative.");
            }

            if (availableArchiteCapsules < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(availableArchiteCapsules),
                    availableArchiteCapsules,
                    "Available archite capsule count cannot be negative.");
            }

            List<PlanAssemblerBlockerReason> copiedBlockerReasons = CopyDistinctBlockerReasons(blockerReasons);
            List<Genepack> copiedCandidateGenepacks = CopyDistinctGenepacks(candidateGenepacks);
            List<PlanGenePrerequisiteDiagnostic> copiedMissingPrerequisites =
                CopyMissingPrerequisites(missingPrerequisites);

            ValidateResult(
                status,
                geneScopeResult,
                copiedBlockerReasons,
                copiedCandidateGenepacks,
                copiedMissingPrerequisites);

            int minimumMetabolism = GeneTuning.BiostatRange.TrueMin;
            if (copiedCandidateGenepacks.Count > 0 &&
                ContainsBlocker(copiedBlockerReasons, PlanAssemblerBlockerReason.MetabolismTooLow) !=
                (actualMetabolism < minimumMetabolism))
            {
                throw new ArgumentException(
                    "Candidate metabolism must match its blocker diagnostics.",
                    nameof(actualMetabolism));
            }

            Status = status;
            VisibleGenepackCount = visibleGenepackCount;
            _blockerReasons = copiedBlockerReasons.AsReadOnly();
            RequiredComplexity = requiredComplexity;
            AvailableComplexity = availableComplexity;
            RequiredArchiteCapsules = requiredArchiteCapsules;
            AvailableArchiteCapsules = availableArchiteCapsules;
            ActualMetabolism = actualMetabolism;
            MinimumMetabolism = minimumMetabolism;
            _candidateGenepacks = copiedCandidateGenepacks.AsReadOnly();
            _missingPrerequisites = copiedMissingPrerequisites.AsReadOnly();
        }

        public PlanAssemblerReadinessStatus Status { get; }
        public PlanReadinessResult GeneScopeResult { get; }
        public int VisibleGenepackCount { get; }

        public IReadOnlyList<PlanAssemblerBlockerReason> BlockerReasons => _blockerReasons;

        public int RequiredComplexity { get; }
        public int AvailableComplexity { get; }
        public int RequiredArchiteCapsules { get; }
        public int AvailableArchiteCapsules { get; }
        public int ActualMetabolism { get; }
        public int MinimumMetabolism { get; }
        public int CandidatePackCount => _candidateGenepacks.Count;

        public IReadOnlyList<Genepack> CandidateGenepacks => _candidateGenepacks;

        public IReadOnlyList<PlanGenePrerequisiteDiagnostic> MissingPrerequisites => _missingPrerequisites;

        public bool IsReady => Status == PlanAssemblerReadinessStatus.Ready;

        internal static PlanAssemblerReadinessResult CreateReady(
            PlanReadinessResult geneScopeResult,
            int visibleGenepackCount,
            int requiredComplexity,
            int availableComplexity,
            int requiredArchiteCapsules,
            int availableArchiteCapsules,
            IEnumerable<Genepack> candidateGenepacks,
            int actualMetabolism)
        {
            return new PlanAssemblerReadinessResult(
                PlanAssemblerReadinessStatus.Ready,
                geneScopeResult,
                visibleGenepackCount,
                Array.Empty<PlanAssemblerBlockerReason>(),
                requiredComplexity,
                availableComplexity,
                requiredArchiteCapsules,
                availableArchiteCapsules,
                candidateGenepacks,
                Array.Empty<PlanGenePrerequisiteDiagnostic>(),
                actualMetabolism);
        }

        internal static PlanAssemblerReadinessResult CreateBlocked(
            PlanReadinessResult geneScopeResult,
            int visibleGenepackCount,
            IEnumerable<PlanAssemblerBlockerReason> blockerReasons,
            int requiredComplexity,
            int availableComplexity,
            int requiredArchiteCapsules,
            int availableArchiteCapsules,
            IEnumerable<Genepack> candidateGenepacks,
            IEnumerable<PlanGenePrerequisiteDiagnostic> missingPrerequisites,
            int actualMetabolism)
        {
            return new PlanAssemblerReadinessResult(
                PlanAssemblerReadinessStatus.Blocked,
                geneScopeResult,
                visibleGenepackCount,
                blockerReasons,
                requiredComplexity,
                availableComplexity,
                requiredArchiteCapsules,
                availableArchiteCapsules,
                candidateGenepacks,
                missingPrerequisites,
                actualMetabolism);
        }

        internal static PlanAssemblerReadinessResult CreateSearchIncomplete(
            PlanReadinessResult geneScopeResult,
            int visibleGenepackCount)
        {
            return CreateWithoutCandidate(
                PlanAssemblerReadinessStatus.SearchIncomplete,
                geneScopeResult,
                visibleGenepackCount);
        }

        internal static PlanAssemblerReadinessResult CreateNotReady(
            PlanReadinessResult geneScopeResult,
            int visibleGenepackCount)
        {
            return CreateWithoutCandidate(PlanAssemblerReadinessStatus.NotReady, geneScopeResult, visibleGenepackCount);
        }

        internal static PlanAssemblerReadinessResult CreateEmptyTarget(
            PlanReadinessResult geneScopeResult,
            int visibleGenepackCount)
        {
            return CreateWithoutCandidate(
                PlanAssemblerReadinessStatus.EmptyTarget,
                geneScopeResult,
                visibleGenepackCount);
        }

        internal static PlanAssemblerReadinessResult CreateDegraded(
            PlanReadinessResult geneScopeResult,
            int visibleGenepackCount)
        {
            return CreateWithoutCandidate(PlanAssemblerReadinessStatus.Degraded, geneScopeResult, visibleGenepackCount);
        }

        private static PlanAssemblerReadinessResult CreateWithoutCandidate(
            PlanAssemblerReadinessStatus status,
            PlanReadinessResult geneScopeResult,
            int visibleGenepackCount)
        {
            return new PlanAssemblerReadinessResult(
                status,
                geneScopeResult,
                visibleGenepackCount,
                Array.Empty<PlanAssemblerBlockerReason>(),
                0,
                0,
                0,
                0,
                Array.Empty<Genepack>(),
                Array.Empty<PlanGenePrerequisiteDiagnostic>(),
                0);
        }

        private static List<PlanAssemblerBlockerReason> CopyDistinctBlockerReasons(
            IEnumerable<PlanAssemblerBlockerReason> blockerReasons)
        {
            if (blockerReasons == null)
                throw new ArgumentNullException(nameof(blockerReasons));

            var copiedReasons = new List<PlanAssemblerBlockerReason>();
            var distinctReasons = new HashSet<PlanAssemblerBlockerReason>();

            foreach (PlanAssemblerBlockerReason blockerReason in blockerReasons)
            {
                ValidateBlockerReason(blockerReason);

                if (distinctReasons.Add(blockerReason))
                    copiedReasons.Add(blockerReason);
            }

            return copiedReasons;
        }

        private static List<Genepack> CopyDistinctGenepacks(IEnumerable<Genepack> candidateGenepacks)
        {
            if (candidateGenepacks == null)
                throw new ArgumentNullException(nameof(candidateGenepacks));

            var copiedGenepacks = new List<Genepack>();
            var distinctGenepacks = new HashSet<Genepack>(ReferenceEqualityComparer<Genepack>.Instance);

            foreach (Genepack genepack in candidateGenepacks)
            {
                if (genepack == null)
                {
                    throw new ArgumentException(
                        "Candidate genepack collection cannot contain null values.",
                        nameof(candidateGenepacks));
                }

                if (distinctGenepacks.Add(genepack))
                    copiedGenepacks.Add(genepack);
            }

            return copiedGenepacks;
        }

        private static List<PlanGenePrerequisiteDiagnostic> CopyMissingPrerequisites(
            IEnumerable<PlanGenePrerequisiteDiagnostic> missingPrerequisites)
        {
            if (missingPrerequisites == null)
                throw new ArgumentNullException(nameof(missingPrerequisites));

            var copied = new List<PlanGenePrerequisiteDiagnostic>();
            var keys = new HashSet<string>(StringComparer.Ordinal);

            foreach (PlanGenePrerequisiteDiagnostic diagnostic in missingPrerequisites)
            {
                if (diagnostic == null)
                {
                    throw new ArgumentException(
                        "Missing prerequisite collection cannot contain null values.",
                        nameof(missingPrerequisites));
                }

                string key = (diagnostic.DependentGene.defName ?? string.Empty) +
                             "\u001f" +
                             (diagnostic.PrerequisiteGene.defName ?? string.Empty);

                if (keys.Add(key))
                    copied.Add(diagnostic);
            }

            return copied;
        }

        private static void ValidateStatus(PlanAssemblerReadinessStatus status)
        {
            if (status != PlanAssemblerReadinessStatus.Ready &&
                status != PlanAssemblerReadinessStatus.NotReady &&
                status != PlanAssemblerReadinessStatus.Blocked &&
                status != PlanAssemblerReadinessStatus.EmptyTarget &&
                status != PlanAssemblerReadinessStatus.Degraded &&
                status != PlanAssemblerReadinessStatus.SearchIncomplete)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Unsupported assembler readiness status.");
            }
        }

        private static void ValidateBlockerReason(PlanAssemblerBlockerReason blockerReason)
        {
            if (blockerReason != PlanAssemblerBlockerReason.MissingPrerequisite &&
                blockerReason != PlanAssemblerBlockerReason.UsedGeneBankUnpowered &&
                blockerReason != PlanAssemblerBlockerReason.AssemblerUnpowered &&
                blockerReason != PlanAssemblerBlockerReason.InsufficientComplexity &&
                blockerReason != PlanAssemblerBlockerReason.ArchogeneticsResearchMissing &&
                blockerReason != PlanAssemblerBlockerReason.InsufficientArchiteCapsules &&
                blockerReason != PlanAssemblerBlockerReason.MetabolismTooLow)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(blockerReason),
                    blockerReason,
                    "Unsupported assembler blocker reason.");
            }
        }

        private static void ValidateResult(
            PlanAssemblerReadinessStatus status,
            PlanReadinessResult geneScopeResult,
            IReadOnlyCollection<PlanAssemblerBlockerReason> blockerReasons,
            IReadOnlyCollection<Genepack> candidateGenepacks,
            IReadOnlyCollection<PlanGenePrerequisiteDiagnostic> missingPrerequisites)
        {
            bool hasMissingPrerequisiteBlocker = ContainsBlocker(
                blockerReasons,
                PlanAssemblerBlockerReason.MissingPrerequisite);

            if (missingPrerequisites.Count > 0 && !hasMissingPrerequisiteBlocker)
            {
                throw new ArgumentException(
                    "Missing prerequisite diagnostics require the matching blocker reason.",
                    nameof(missingPrerequisites));
            }

            if (hasMissingPrerequisiteBlocker && missingPrerequisites.Count == 0)
            {
                throw new ArgumentException(
                    "Missing prerequisite blocker requires at least one diagnostic.",
                    nameof(blockerReasons));
            }

            if (status == PlanAssemblerReadinessStatus.Ready || status == PlanAssemblerReadinessStatus.Blocked)
            {
                if (geneScopeResult.Status != PlanReadinessStatus.Ready)
                {
                    throw new ArgumentException(
                        "Ready or blocked assembler readiness requires a ready gene scope.",
                        nameof(geneScopeResult));
                }

                if (candidateGenepacks.Count == 0)
                {
                    throw new ArgumentException(
                        "Ready or blocked assembler readiness requires a concrete physical candidate.",
                        nameof(candidateGenepacks));
                }

                if (status == PlanAssemblerReadinessStatus.Ready &&
                    (blockerReasons.Count != 0 || missingPrerequisites.Count != 0))
                {
                    throw new ArgumentException(
                        "Ready assembler readiness cannot contain blockers or missing prerequisites.",
                        nameof(blockerReasons));
                }

                if (status == PlanAssemblerReadinessStatus.Blocked && blockerReasons.Count == 0)
                {
                    throw new ArgumentException(
                        "Blocked assembler readiness requires at least one blocker.",
                        nameof(blockerReasons));
                }

                return;
            }

            if (blockerReasons.Count != 0 || candidateGenepacks.Count != 0 || missingPrerequisites.Count != 0)
            {
                throw new ArgumentException(
                    "Assembler readiness without a concrete candidate cannot contain candidate diagnostics.",
                    nameof(candidateGenepacks));
            }

            PlanReadinessStatus expectedGeneScopeStatus;

            switch (status)
            {
                case PlanAssemblerReadinessStatus.SearchIncomplete:
                    expectedGeneScopeStatus = PlanReadinessStatus.Ready;
                    break;

                case PlanAssemblerReadinessStatus.NotReady:
                    expectedGeneScopeStatus = PlanReadinessStatus.NotReady;
                    break;

                case PlanAssemblerReadinessStatus.EmptyTarget:
                    expectedGeneScopeStatus = PlanReadinessStatus.EmptyTarget;
                    break;

                case PlanAssemblerReadinessStatus.Degraded:
                    expectedGeneScopeStatus = PlanReadinessStatus.Degraded;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(status),
                        status,
                        "Unsupported assembler readiness status.");
            }

            if (geneScopeResult.Status != expectedGeneScopeStatus)
            {
                throw new ArgumentException(
                    "Assembler readiness status must match its gene-scope result.",
                    nameof(geneScopeResult));
            }
        }

        private static bool ContainsBlocker(
            IEnumerable<PlanAssemblerBlockerReason> blockers,
            PlanAssemblerBlockerReason expected)
        {
            foreach (PlanAssemblerBlockerReason blocker in blockers)
            {
                if (blocker == expected)
                    return true;
            }

            return false;
        }
    }
}