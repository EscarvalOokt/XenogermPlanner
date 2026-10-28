using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace XenogermPlanner.Api
{
    public sealed class GenepackRelevanceBatchResult
    {
        private readonly ReadOnlyCollection<GenepackRelevanceItemResult> _results;

        private GenepackRelevanceBatchResult(
            GenepackRelevanceBatchStatus status,
            GenepackRelevanceUnavailableReason unavailableReason,
            IEnumerable<GenepackRelevanceItemResult> results)
        {
            ValidateStatus(status);
            ValidateUnavailableReason(unavailableReason);

            List<GenepackRelevanceItemResult> copiedResults = CopyResults(results);

            if (status == GenepackRelevanceBatchStatus.Unavailable)
            {
                if (unavailableReason == GenepackRelevanceUnavailableReason.None)
                {
                    throw new ArgumentException(
                        "An unavailable relevance batch result requires an unavailable reason.",
                        nameof(unavailableReason));
                }
            }
            else if (unavailableReason != GenepackRelevanceUnavailableReason.None)
            {
                throw new ArgumentException(
                    "Only an unavailable relevance batch result can contain an unavailable reason.",
                    nameof(unavailableReason));
            }

            if (status != GenepackRelevanceBatchStatus.Success && copiedResults.Count > 0)
            {
                throw new ArgumentException(
                    "Only a successful relevance batch result can contain item results.",
                    nameof(results));
            }

            Status = status;
            UnavailableReason = unavailableReason;
            _results = copiedResults.AsReadOnly();
        }

        public GenepackRelevanceBatchStatus Status { get; }
        public GenepackRelevanceUnavailableReason UnavailableReason { get; }
        public IReadOnlyList<GenepackRelevanceItemResult> Results => _results;

        internal static GenepackRelevanceBatchResult CreateSuccess(IEnumerable<GenepackRelevanceItemResult> results)
        {
            return new GenepackRelevanceBatchResult(
                GenepackRelevanceBatchStatus.Success,
                GenepackRelevanceUnavailableReason.None,
                results);
        }

        internal static GenepackRelevanceBatchResult CreateInvalidRequest()
        {
            return new GenepackRelevanceBatchResult(
                GenepackRelevanceBatchStatus.InvalidRequest,
                GenepackRelevanceUnavailableReason.None,
                Array.Empty<GenepackRelevanceItemResult>());
        }

        internal static GenepackRelevanceBatchResult CreateUnavailable(
            GenepackRelevanceUnavailableReason unavailableReason)
        {
            return new GenepackRelevanceBatchResult(
                GenepackRelevanceBatchStatus.Unavailable,
                unavailableReason,
                Array.Empty<GenepackRelevanceItemResult>());
        }

        internal static GenepackRelevanceBatchResult CreateFailed()
        {
            return new GenepackRelevanceBatchResult(
                GenepackRelevanceBatchStatus.Failed,
                GenepackRelevanceUnavailableReason.None,
                Array.Empty<GenepackRelevanceItemResult>());
        }

        private static List<GenepackRelevanceItemResult> CopyResults(IEnumerable<GenepackRelevanceItemResult> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            var copiedResults = new List<GenepackRelevanceItemResult>();

            foreach (GenepackRelevanceItemResult result in results)
            {
                if (result == null)
                {
                    throw new ArgumentException(
                        "Relevance item result collection cannot contain null values.",
                        nameof(results));
                }

                copiedResults.Add(result);
            }

            return copiedResults;
        }

        private static void ValidateStatus(GenepackRelevanceBatchStatus status)
        {
            if (status != GenepackRelevanceBatchStatus.Success &&
                status != GenepackRelevanceBatchStatus.InvalidRequest &&
                status != GenepackRelevanceBatchStatus.Unavailable &&
                status != GenepackRelevanceBatchStatus.Failed)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported relevance batch status.");
            }
        }

        private static void ValidateUnavailableReason(GenepackRelevanceUnavailableReason unavailableReason)
        {
            if (unavailableReason != GenepackRelevanceUnavailableReason.None &&
                unavailableReason != GenepackRelevanceUnavailableReason.NoGame &&
                unavailableReason != GenepackRelevanceUnavailableReason.NoActiveMap &&
                unavailableReason != GenepackRelevanceUnavailableReason.PlannerStateUnavailable)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unavailableReason),
                    unavailableReason,
                    "Unsupported relevance unavailable reason.");
            }
        }
    }
}