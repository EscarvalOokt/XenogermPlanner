using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace XenogermPlanner.Api
{
    public sealed class GenepackRelevanceItemResult
    {
        private readonly ReadOnlyCollection<GenepackRelevancePlanMatch> _matches;

        private GenepackRelevanceItemResult(
            GenepackRelevanceItemStatus status,
            IEnumerable<GenepackRelevancePlanMatch> matches)
        {
            ValidateStatus(status);

            List<GenepackRelevancePlanMatch> copiedMatches = CopyMatches(matches);

            if (status != GenepackRelevanceItemStatus.Success && copiedMatches.Count > 0)
            {
                throw new ArgumentException(
                    "Only a successful relevance item result can contain plan matches.",
                    nameof(matches));
            }

            Status = status;
            _matches = copiedMatches.AsReadOnly();
        }

        public GenepackRelevanceItemStatus Status { get; }
        public IReadOnlyList<GenepackRelevancePlanMatch> Matches => _matches;

        internal static GenepackRelevanceItemResult CreateSuccess(IEnumerable<GenepackRelevancePlanMatch> matches)
        {
            return new GenepackRelevanceItemResult(GenepackRelevanceItemStatus.Success, matches);
        }

        internal static GenepackRelevanceItemResult CreateInvalidInput()
        {
            return new GenepackRelevanceItemResult(
                GenepackRelevanceItemStatus.InvalidInput,
                Array.Empty<GenepackRelevancePlanMatch>());
        }

        internal static GenepackRelevanceItemResult CreateUnknownGeneDef()
        {
            return new GenepackRelevanceItemResult(
                GenepackRelevanceItemStatus.UnknownGeneDef,
                Array.Empty<GenepackRelevancePlanMatch>());
        }

        internal static GenepackRelevanceItemResult CreateFailed()
        {
            return new GenepackRelevanceItemResult(
                GenepackRelevanceItemStatus.Failed,
                Array.Empty<GenepackRelevancePlanMatch>());
        }

        private static List<GenepackRelevancePlanMatch> CopyMatches(IEnumerable<GenepackRelevancePlanMatch> matches)
        {
            if (matches == null)
                throw new ArgumentNullException(nameof(matches));

            var copiedMatches = new List<GenepackRelevancePlanMatch>();

            foreach (GenepackRelevancePlanMatch match in matches)
            {
                if (match == null)
                {
                    throw new ArgumentException("Plan match collection cannot contain null values.", nameof(matches));
                }

                copiedMatches.Add(match);
            }

            return copiedMatches;
        }

        private static void ValidateStatus(GenepackRelevanceItemStatus status)
        {
            if (status != GenepackRelevanceItemStatus.Success &&
                status != GenepackRelevanceItemStatus.InvalidInput &&
                status != GenepackRelevanceItemStatus.UnknownGeneDef &&
                status != GenepackRelevanceItemStatus.Failed)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported relevance item status.");
            }
        }
    }
}