using System;
using XenogermPlanner.Analysis;

namespace XenogermPlanner.UI
{
    internal enum GeneCoverageSortColumn
    {
        Gene,
        Availability,
        GenepackCount,
        PotentialDonorCount
    }

    internal enum GeneCoverageAvailabilityState
    {
        Available = 0,
        ExactPayloadConflict = 1,
        Missing = 2,
        Unavailable = 3
    }

    internal readonly struct GeneCoverageSortState
    {
        internal static GeneCoverageSortState Default { get; } =
            new GeneCoverageSortState(GeneCoverageSortColumn.Gene, descending: false);

        internal GeneCoverageSortColumn Column { get; }
        internal bool Descending { get; }

        internal GeneCoverageSortState(GeneCoverageSortColumn column, bool descending)
        {
            ValidateColumn(column);

            Column = column;
            Descending = descending;
        }

        internal GeneCoverageSortState Toggle(GeneCoverageSortColumn column)
        {
            ValidateColumn(column);

            return column == Column
                ? new GeneCoverageSortState(column, !Descending)
                : new GeneCoverageSortState(column, descending: false);
        }

        private static void ValidateColumn(GeneCoverageSortColumn column)
        {
            if (column != GeneCoverageSortColumn.Gene &&
                column != GeneCoverageSortColumn.Availability &&
                column != GeneCoverageSortColumn.GenepackCount &&
                column != GeneCoverageSortColumn.PotentialDonorCount)
            {
                throw new ArgumentOutOfRangeException(nameof(column), column, "Unsupported gene coverage sort column.");
            }
        }
    }

    internal sealed class GeneCoverageTableRow
    {
        private GeneCoverageTableRow(
            PlanGeneCoverageDiagnostic diagnostic,
            string unresolvedGeneDefName,
            string displayName,
            string stableKey,
            GeneCoverageAvailabilityState availabilityState,
            int sourceGenepackCount,
            int? potentialDonorCount)
        {
            if (diagnostic == null && string.IsNullOrWhiteSpace(unresolvedGeneDefName))
            {
                throw new ArgumentException(
                    "A gene coverage table row requires either a resolved diagnostic or an unresolved gene def name.");
            }

            if (diagnostic != null && unresolvedGeneDefName != null)
            {
                throw new ArgumentException(
                    "A gene coverage table row cannot contain both a resolved diagnostic and an unresolved " +
                    "gene def name.");
            }

            if (sourceGenepackCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sourceGenepackCount),
                    sourceGenepackCount,
                    "Source genepack count cannot be negative.");
            }

            if (potentialDonorCount is < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(potentialDonorCount),
                    potentialDonorCount,
                    "Potential donor count cannot be negative.");
            }

            Diagnostic = diagnostic;
            UnresolvedGeneDefName = unresolvedGeneDefName;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            StableKey = stableKey ?? throw new ArgumentNullException(nameof(stableKey));
            AvailabilityState = availabilityState;
            SourceGenepackCount = sourceGenepackCount;
            PotentialDonorCount = potentialDonorCount;
        }

        internal PlanGeneCoverageDiagnostic Diagnostic { get; }
        internal string UnresolvedGeneDefName { get; }
        internal string DisplayName { get; }
        internal string StableKey { get; }
        internal GeneCoverageAvailabilityState AvailabilityState { get; }
        internal int SourceGenepackCount { get; }
        internal int? PotentialDonorCount { get; }
        internal bool IsResolved => Diagnostic != null;

        internal static GeneCoverageTableRow CreateResolved(
            PlanGeneCoverageDiagnostic diagnostic,
            string displayName,
            string stableKey,
            int sourceGenepackCount,
            int? potentialDonorCount = null)
        {
            if (diagnostic == null)
                throw new ArgumentNullException(nameof(diagnostic));

            GeneCoverageAvailabilityState availabilityState;

            switch (diagnostic.State)
            {
                case PlanGeneCoverageState.Available:
                    availabilityState = GeneCoverageAvailabilityState.Available;
                    break;

                case PlanGeneCoverageState.ExactPayloadConflict:
                    availabilityState = GeneCoverageAvailabilityState.ExactPayloadConflict;
                    break;

                case PlanGeneCoverageState.Missing:
                    availabilityState = GeneCoverageAvailabilityState.Missing;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(diagnostic),
                        diagnostic.State,
                        "Unsupported gene coverage state.");
            }

            return new GeneCoverageTableRow(
                diagnostic,
                null,
                displayName,
                stableKey,
                availabilityState,
                sourceGenepackCount,
                potentialDonorCount);
        }

        internal static GeneCoverageTableRow CreateUnresolved(string geneDefName)
        {
            if (string.IsNullOrWhiteSpace(geneDefName))
                throw new ArgumentException("Gene def name cannot be null, empty or whitespace.", nameof(geneDefName));

            return new GeneCoverageTableRow(
                null,
                geneDefName,
                geneDefName,
                geneDefName,
                GeneCoverageAvailabilityState.Unavailable,
                0,
                null);
        }
    }
}