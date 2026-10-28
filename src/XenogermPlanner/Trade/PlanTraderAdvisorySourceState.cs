using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RimWorld;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Trade
{
    internal sealed class PlanTraderAdvisorySourceState
    {
        private readonly Dictionary<Genepack, HashSet<string>> _acknowledgedPlanIdsByGenepack =
            new Dictionary<Genepack, HashSet<string>>(ReferenceEqualityComparer<Genepack>.Instance);

        private readonly Dictionary<Genepack, HashSet<string>> _pendingPlanIdsByGenepack =
            new Dictionary<Genepack, HashSet<string>>(ReferenceEqualityComparer<Genepack>.Instance);

        private ReadOnlyCollection<PlanTraderAdvisoryOfferAnalysis> _offerAnalyses =
            new List<PlanTraderAdvisoryOfferAnalysis>().AsReadOnly();

        internal PlanTraderAdvisorySourceState(PlanTraderAdvisorySourceSnapshot snapshot)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        internal PlanTraderAdvisorySourceSnapshot Snapshot { get; private set; }
        internal IReadOnlyList<PlanTraderAdvisoryOfferAnalysis> OfferAnalyses => _offerAnalyses;

        internal bool UpdateStock(PlanTraderAdvisorySourceSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            if (!ReferenceEquals(Snapshot.Source, snapshot.Source))
                throw new ArgumentException("Trader source state cannot change source identity.", nameof(snapshot));

            if (StockMatches(Snapshot, snapshot))
                return false;

            Snapshot = snapshot;
            _offerAnalyses = new List<PlanTraderAdvisoryOfferAnalysis>().AsReadOnly();
            return true;
        }

        internal void ReplaceAnalysis(IEnumerable<PlanTraderAdvisoryOfferAnalysis> analyses)
        {
            if (analyses == null)
                throw new ArgumentNullException(nameof(analyses));

            var copiedAnalyses = new List<PlanTraderAdvisoryOfferAnalysis>();
            var analyzedGenepacks = new HashSet<Genepack>(ReferenceEqualityComparer<Genepack>.Instance);
            var currentGenepacks = new HashSet<Genepack>(ReferenceEqualityComparer<Genepack>.Instance);

            foreach (PlanTraderAdvisoryOfferSnapshot offer in Snapshot.Offers)
                currentGenepacks.Add(offer.Genepack);

            foreach (PlanTraderAdvisoryOfferAnalysis analysis in analyses)
            {
                if (analysis == null)
                {
                    throw new ArgumentException(
                        "Trader advisory analysis collection cannot contain null values.",
                        nameof(analyses));
                }

                if (!currentGenepacks.Contains(analysis.Offer.Genepack))
                {
                    throw new ArgumentException(
                        "Trader advisory analysis must refer to a current source offer.",
                        nameof(analyses));
                }

                if (analyzedGenepacks.Add(analysis.Offer.Genepack))
                    copiedAnalyses.Add(analysis);
            }

            _offerAnalyses = copiedAnalyses.AsReadOnly();
        }

        internal bool IsAcknowledged(Genepack genepack, string planId)
        {
            ValidatePair(genepack, planId);

            return _acknowledgedPlanIdsByGenepack.TryGetValue(genepack, out HashSet<string> planIds) &&
                   planIds.Contains(planId);
        }

        internal bool IsPending(Genepack genepack, string planId)
        {
            ValidatePair(genepack, planId);

            return _pendingPlanIdsByGenepack.TryGetValue(genepack, out HashSet<string> planIds) &&
                   planIds.Contains(planId);
        }

        internal void MarkAcknowledged(Genepack genepack, string planId)
        {
            ValidatePair(genepack, planId);

            GetOrCreatePlanIds(_acknowledgedPlanIdsByGenepack, genepack).Add(planId);
            RemovePending(genepack, planId);
        }

        internal void MarkPending(Genepack genepack, string planId)
        {
            ValidatePair(genepack, planId);

            if (IsAcknowledged(genepack, planId))
                return;

            GetOrCreatePlanIds(_pendingPlanIdsByGenepack, genepack).Add(planId);
        }

        internal void ClearPending()
        {
            _pendingPlanIdsByGenepack.Clear();
        }

        private static HashSet<string> GetOrCreatePlanIds(
            Dictionary<Genepack, HashSet<string>> lookup,
            Genepack genepack)
        {
            if (lookup.TryGetValue(genepack, out HashSet<string> planIds))
                return planIds;

            planIds = new HashSet<string>(StringComparer.Ordinal);
            lookup.Add(genepack, planIds);
            return planIds;
        }

        private void RemovePending(Genepack genepack, string planId)
        {
            if (!_pendingPlanIdsByGenepack.TryGetValue(genepack, out HashSet<string> planIds))
                return;

            planIds.Remove(planId);

            if (planIds.Count == 0)
                _pendingPlanIdsByGenepack.Remove(genepack);
        }

        private static void ValidatePair(Genepack genepack, string planId)
        {
            if (genepack == null)
                throw new ArgumentNullException(nameof(genepack));

            if (string.IsNullOrWhiteSpace(planId))
                throw new ArgumentException("Trader advisory plan identity cannot be empty.", nameof(planId));
        }

        private static bool StockMatches(PlanTraderAdvisorySourceSnapshot left, PlanTraderAdvisorySourceSnapshot right)
        {
            if (left.Kind != right.Kind ||
                !ReferenceEquals(left.NavigationPawn, right.NavigationPawn) ||
                left.Offers.Count != right.Offers.Count)
            {
                return false;
            }

            var rightOffers = new Dictionary<Genepack, PlanTraderAdvisoryOfferSnapshot>(
                ReferenceEqualityComparer<Genepack>.Instance);

            foreach (PlanTraderAdvisoryOfferSnapshot offer in right.Offers)
                rightOffers[offer.Genepack] = offer;

            foreach (PlanTraderAdvisoryOfferSnapshot leftOffer in left.Offers)
            {
                if (!rightOffers.TryGetValue(leftOffer.Genepack, out PlanTraderAdvisoryOfferSnapshot rightOffer))
                    return false;

                if (!GenepackCompositionUtility.CompositionsMatch(leftOffer.Genes, rightOffer.Genes))
                    return false;
            }

            return true;
        }
    }
}