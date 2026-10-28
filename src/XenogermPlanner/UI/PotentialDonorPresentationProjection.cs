using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Verse;

namespace XenogermPlanner.UI
{
    internal sealed class PotentialDonorPresentationRow
    {
        internal PotentialDonorPresentationRow(Pawn donor, string displayName, int stableKey)
        {
            Donor = donor ?? throw new ArgumentNullException(nameof(donor));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            StableKey = stableKey;
        }

        internal Pawn Donor { get; }
        internal string DisplayName { get; }
        internal int StableKey { get; }
    }

    internal sealed class PotentialDonorPresentationProjection
    {
        private readonly ReadOnlyCollection<PotentialDonorPresentationRow> _rows;

        internal PotentialDonorPresentationProjection(IEnumerable<PotentialDonorPresentationRow> rows)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));

            var copiedRows = new List<PotentialDonorPresentationRow>();

            foreach (PotentialDonorPresentationRow row in rows)
            {
                if (row == null)
                    throw new ArgumentException("Potential donor rows cannot contain null values.", nameof(rows));

                copiedRows.Add(row);
            }

            _rows = copiedRows.AsReadOnly();
        }

        internal IReadOnlyList<PotentialDonorPresentationRow> Rows => _rows;
    }

    internal sealed class PotentialDonorPresentationProjectionCache
    {
        private object _languageKey;
        private PotentialDonorPresentationProjection _projection;
        private SourceDescriptor[] _sourceDescriptors;

        internal PotentialDonorPresentationProjection GetOrBuild(IReadOnlyList<Pawn> donors, object languageKey)
        {
            return GetOrBuild(
                donors,
                languageKey,
                XenogermPlannerPresentation.GetPotentialDonorDisplayName,
                donor => donor.thingIDNumber);
        }

        internal PotentialDonorPresentationProjection GetOrBuild(
            IReadOnlyList<Pawn> donors,
            object languageKey,
            Func<Pawn, string> getDisplayName,
            Func<Pawn, int> getStableKey)
        {
            if (donors == null)
                throw new ArgumentNullException(nameof(donors));

            if (getDisplayName == null)
                throw new ArgumentNullException(nameof(getDisplayName));

            if (getStableKey == null)
                throw new ArgumentNullException(nameof(getStableKey));

            if (_projection != null &&
                Equals(_languageKey, languageKey) &&
                IsCompatible(donors, getDisplayName, getStableKey))
            {
                return _projection;
            }

            var descriptors = new SourceDescriptor[donors.Count];
            var rows = new List<PotentialDonorPresentationRow>(donors.Count);

            for (var index = 0; index < donors.Count; index++)
            {
                Pawn donor = donors[index];

                if (donor == null)
                    throw new ArgumentException(
                        "Potential donor collection cannot contain null values.",
                        nameof(donors));

                string displayName = getDisplayName(donor) ?? string.Empty;
                int stableKey = getStableKey(donor);
                descriptors[index] = new SourceDescriptor(donor, displayName, stableKey);
                rows.Add(new PotentialDonorPresentationRow(donor, displayName, stableKey));
            }

            rows.Sort(CompareRows);

            _languageKey = languageKey;
            _sourceDescriptors = descriptors;
            _projection = new PotentialDonorPresentationProjection(rows);
            return _projection;
        }

        internal void Invalidate()
        {
            _languageKey = null;
            _sourceDescriptors = null;
            _projection = null;
        }

        private bool IsCompatible(
            IReadOnlyList<Pawn> donors,
            Func<Pawn, string> getDisplayName,
            Func<Pawn, int> getStableKey)
        {
            if (_sourceDescriptors == null || _sourceDescriptors.Length != donors.Count)
                return false;

            for (var index = 0; index < donors.Count; index++)
            {
                Pawn donor = donors[index];

                if (donor == null)
                    throw new ArgumentException(
                        "Potential donor collection cannot contain null values.",
                        nameof(donors));

                SourceDescriptor cached = _sourceDescriptors[index];
                string displayName = getDisplayName(donor) ?? string.Empty;
                int stableKey = getStableKey(donor);

                if (!ReferenceEquals(cached.Donor, donor) ||
                    !string.Equals(cached.DisplayName, displayName, StringComparison.Ordinal) ||
                    cached.StableKey != stableKey)
                {
                    return false;
                }
            }

            return true;
        }

        private static int CompareRows(PotentialDonorPresentationRow left, PotentialDonorPresentationRow right)
        {
            int comparison = StringComparer.CurrentCultureIgnoreCase.Compare(left.DisplayName, right.DisplayName);

            return comparison != 0 ? comparison : left.StableKey.CompareTo(right.StableKey);
        }

        private sealed class SourceDescriptor
        {
            internal SourceDescriptor(Pawn donor, string displayName, int stableKey)
            {
                Donor = donor;
                DisplayName = displayName;
                StableKey = stableKey;
            }

            internal Pawn Donor { get; }
            internal string DisplayName { get; }
            internal int StableKey { get; }
        }
    }
}