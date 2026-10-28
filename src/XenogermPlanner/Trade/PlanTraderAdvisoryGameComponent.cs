using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;
using XenogermPlanner.UI;

namespace XenogermPlanner.Trade
{
    public sealed class PlanTraderAdvisoryGameComponent : GameComponent
    {
        internal const int StockRefreshIntervalTicks = 60;
        private readonly Action<PlanTraderAdvisoryNotification> _announce;

        private readonly Func<ITrader, bool> _canTradeNow;

        private readonly Func<
            IReadOnlyList<XenogermPlan>, PlanGenepackInventorySnapshot,
            Func<IReadOnlyCollection<GeneDef>, IReadOnlyList<XenogermPlan>>> _createRelevanceEvaluator;

        private readonly Func<Map> _getCurrentMap;
        private readonly Func<int> _getCurrentTick;
        private readonly Func<PlanGenepackInventorySnapshot> _getInventorySnapshot;
        private readonly Func<IReadOnlyList<XenogermPlan>> _getPlans;
        private readonly Action<string> _reportError;
        private readonly Func<Map, PlanTraderAdvisoryStockSnapshot> _scanStock;

        private readonly Dictionary<ITrader, PlanTraderAdvisorySourceState> _sourceStates =
            new Dictionary<ITrader, PlanTraderAdvisorySourceState>(ReferenceEqualityComparer<ITrader>.Instance);

        private Map _activeMap;
        private PlanGenepackInventorySnapshot _lastAnalyzedInventorySnapshot;
        private int _nextStockRefreshTick;
        private bool _relevanceInvalidated = true;

        private ReadOnlyCollection<PlanTraderAdvisorySourceState> _sourceStatesForReading =
            new List<PlanTraderAdvisorySourceState>().AsReadOnly();

        private bool _stockPollInvalidated = true;
        private PlanTraderAdvisoryStockSnapshot _stockSnapshot = PlanTraderAdvisoryStockSnapshot.Unavailable;

        // ReSharper disable once UnusedParameter.Local
        public PlanTraderAdvisoryGameComponent(Game _) : this(
            () => Find.CurrentMap,
            GetCurrentGameTick,
            PlanTraderAdvisoryStockScanner.Scan,
            GetCurrentPlans,
            GetCurrentInventorySnapshot,
            CreateRelevanceEvaluator,
            CanTraderTradeNow,
            AnnounceTraderAdvisory,
            ReportError)
        {
        }

        internal PlanTraderAdvisoryGameComponent(
            Func<Map> getCurrentMap,
            Func<int> getCurrentTick,
            Func<Map, PlanTraderAdvisoryStockSnapshot> scanStock,
            Func<IReadOnlyList<XenogermPlan>> getPlans,
            Func<PlanGenepackInventorySnapshot> getInventorySnapshot,
            Func<IReadOnlyList<XenogermPlan>, PlanGenepackInventorySnapshot,
                Func<IReadOnlyCollection<GeneDef>, IReadOnlyList<XenogermPlan>>> createRelevanceEvaluator,
            Func<ITrader, bool> canTradeNow,
            Action<PlanTraderAdvisoryNotification> announce,
            Action<string> reportError)
        {
            _getCurrentMap = getCurrentMap ?? throw new ArgumentNullException(nameof(getCurrentMap));
            _getCurrentTick = getCurrentTick ?? throw new ArgumentNullException(nameof(getCurrentTick));
            _scanStock = scanStock ?? throw new ArgumentNullException(nameof(scanStock));
            _getPlans = getPlans ?? throw new ArgumentNullException(nameof(getPlans));
            _getInventorySnapshot = getInventorySnapshot ??
                                    throw new ArgumentNullException(nameof(getInventorySnapshot));
            _createRelevanceEvaluator = createRelevanceEvaluator ??
                                        throw new ArgumentNullException(nameof(createRelevanceEvaluator));
            _canTradeNow = canTradeNow ?? throw new ArgumentNullException(nameof(canTradeNow));
            _announce = announce ?? throw new ArgumentNullException(nameof(announce));
            _reportError = reportError ?? throw new ArgumentNullException(nameof(reportError));
        }

        internal IReadOnlyList<PlanTraderAdvisorySourceState> Sources => _sourceStatesForReading;
        internal bool HasDeterminateBaseline { get; private set; }

        public void Invalidate()
        {
            _relevanceInvalidated = true;
        }

        public override void GameComponentTick()
        {
            Map currentMap = _getCurrentMap();
            EnsureActiveMapState(currentMap);

            int currentTick = _getCurrentTick();
            var stockPolled = false;

            if (_stockPollInvalidated || currentTick >= _nextStockRefreshTick)
                stockPolled = RefreshStock(currentMap, currentTick);

            if (!TryEnsureCurrentRelevance(out bool relevanceReevaluated))
                return;

            if (!HasDeterminateBaseline)
            {
                if (TryEstablishBaseline())
                    HasDeterminateBaseline = true;

                return;
            }

            if (relevanceReevaluated || stockPolled)
                ReconcileNotifications();
        }

        private static int GetCurrentGameTick()
        {
            return Find.TickManager?.TicksGame ?? 0;
        }

        private void EnsureActiveMapState(Map currentMap)
        {
            if (ReferenceEquals(_activeMap, currentMap))
                return;

            _activeMap = currentMap;
            _sourceStates.Clear();
            RefreshSourceStatesForReading();
            _stockSnapshot = PlanTraderAdvisoryStockSnapshot.Unavailable;
            _lastAnalyzedInventorySnapshot = null;
            _stockPollInvalidated = true;
            _relevanceInvalidated = true;
            _nextStockRefreshTick = 0;
            HasDeterminateBaseline = false;
        }

        private bool RefreshStock(Map currentMap, int currentTick)
        {
            PlanTraderAdvisoryStockSnapshot refreshedSnapshot;

            try
            {
                refreshedSnapshot = _scanStock(currentMap) ??
                                    throw new InvalidOperationException(
                                        "Trader advisory stock scanner returned a null snapshot.");
            }
            catch (Exception exception)
            {
                _reportError($"Failed to refresh current trader advisory stock: {exception}");
                _stockPollInvalidated = false;
                _nextStockRefreshTick = currentTick + StockRefreshIntervalTicks;
                return false;
            }

            _stockSnapshot = refreshedSnapshot;
            _stockPollInvalidated = false;
            _nextStockRefreshTick = currentTick + StockRefreshIntervalTicks;

            if (refreshedSnapshot.IsAvailable && ReconcileSources(refreshedSnapshot.Sources))
                _relevanceInvalidated = true;

            return true;
        }

        private bool ReconcileSources(IReadOnlyList<PlanTraderAdvisorySourceSnapshot> currentSources)
        {
            var changed = false;
            var seenSources = new HashSet<ITrader>(ReferenceEqualityComparer<ITrader>.Instance);

            foreach (PlanTraderAdvisorySourceSnapshot source in currentSources)
            {
                seenSources.Add(source.Source);

                if (_sourceStates.TryGetValue(source.Source, out PlanTraderAdvisorySourceState existingState))
                {
                    if (existingState.UpdateStock(source))
                        changed = true;
                }
                else
                {
                    _sourceStates.Add(source.Source, new PlanTraderAdvisorySourceState(source));
                    changed = true;
                }
            }

            if (_sourceStates.Count > seenSources.Count)
            {
                var removedSources = new List<ITrader>();

                foreach (ITrader source in _sourceStates.Keys)
                {
                    if (!seenSources.Contains(source))
                        removedSources.Add(source);
                }

                foreach (ITrader removedSource in removedSources)
                {
                    _sourceStates.Remove(removedSource);
                    changed = true;
                }
            }

            if (changed)
                RefreshSourceStatesForReading();

            return changed;
        }

        private bool TryEnsureCurrentRelevance(out bool reevaluated)
        {
            reevaluated = false;

            IReadOnlyList<XenogermPlan> plans = _getPlans();
            PlanGenepackInventorySnapshot inventorySnapshot = _getInventorySnapshot();

            if (plans == null ||
                inventorySnapshot == null ||
                !inventorySnapshot.IsAvailable ||
                _stockSnapshot == null ||
                !_stockSnapshot.IsAvailable ||
                _activeMap == null)
            {
                _relevanceInvalidated = true;
                return false;
            }

            if (!_relevanceInvalidated && ReferenceEquals(_lastAnalyzedInventorySnapshot, inventorySnapshot))
                return true;

            Func<IReadOnlyCollection<GeneDef>, IReadOnlyList<XenogermPlan>> evaluateComposition;

            try
            {
                evaluateComposition = _createRelevanceEvaluator(plans, inventorySnapshot) ??
                                      throw new InvalidOperationException(
                                          "Trader advisory relevance evaluator factory returned null.");
            }
            catch (Exception exception)
            {
                _reportError($"Failed to prepare current trader advisory relevance analysis: {exception}");
                return false;
            }

            foreach (PlanTraderAdvisorySourceState sourceState in _sourceStatesForReading)
            {
                var analyses = new List<PlanTraderAdvisoryOfferAnalysis>();

                foreach (PlanTraderAdvisoryOfferSnapshot offer in sourceState.Snapshot.Offers)
                {
                    try
                    {
                        IReadOnlyList<XenogermPlan> matchingPlans = evaluateComposition(offer.Genes) ??
                                                                    throw new InvalidOperationException(
                                                                        "Trader advisory relevance analyzer " +
                                                                        "returned null matches.");

                        analyses.Add(new PlanTraderAdvisoryOfferAnalysis(offer, matchingPlans));
                    }
                    catch (Exception exception)
                    {
                        _reportError($"Failed to analyze one current trader genepack offer: {exception}");
                    }
                }

                sourceState.ReplaceAnalysis(analyses);
            }

            _lastAnalyzedInventorySnapshot = inventorySnapshot;
            _relevanceInvalidated = false;
            reevaluated = true;
            return true;
        }

        private bool TryEstablishBaseline()
        {
            var succeeded = true;

            foreach (PlanTraderAdvisorySourceState sourceState in _sourceStatesForReading)
            {
                try
                {
                    PlanTraderAdvisoryNotificationTracker.EstablishBaseline(sourceState);
                }
                catch (Exception exception)
                {
                    succeeded = false;
                    _reportError($"Failed to establish one current trader advisory baseline: {exception}");
                }
            }

            return succeeded;
        }

        private void ReconcileNotifications()
        {
            foreach (PlanTraderAdvisorySourceState sourceState in _sourceStatesForReading)
            {
                try
                {
                    bool canTradeNow;

                    try
                    {
                        canTradeNow = _canTradeNow(sourceState.Snapshot.Source);
                    }
                    catch (Exception exception)
                    {
                        PlanTraderAdvisoryNotificationTracker.Reconcile(sourceState, canTradeNow: false);
                        _reportError($"Failed to read current trader availability for advisory delivery: {exception}");
                        continue;
                    }

                    PlanTraderAdvisoryNotification notification =
                        PlanTraderAdvisoryNotificationTracker.Reconcile(sourceState, canTradeNow);

                    if (notification == null)
                        continue;

                    try
                    {
                        _announce(notification);
                        PlanTraderAdvisoryNotificationTracker.MarkDelivered(sourceState, notification);
                    }
                    catch (Exception exception)
                    {
                        _reportError($"Failed to deliver one current trader advisory notification: {exception}");
                    }
                }
                catch (Exception exception)
                {
                    _reportError($"Failed to reconcile one current trader advisory notification state: {exception}");
                }
            }
        }

        private void RefreshSourceStatesForReading()
        {
            _sourceStatesForReading = new List<PlanTraderAdvisorySourceState>(_sourceStates.Values).AsReadOnly();
        }

        private static IReadOnlyList<XenogermPlan> GetCurrentPlans()
        {
            return Current.Game?.GetComponent<XenogermPlanGameComponent>()?.Plans;
        }

        private static PlanGenepackInventorySnapshot GetCurrentInventorySnapshot()
        {
            return Current.Game?.GetComponent<PlanGenepackInventoryGameComponent>()?.Snapshot;
        }

        private static Func<IReadOnlyCollection<GeneDef>, IReadOnlyList<XenogermPlan>> CreateRelevanceEvaluator(
            IReadOnlyList<XenogermPlan> plans,
            PlanGenepackInventorySnapshot inventorySnapshot)
        {
            var analyzer = new PlanGenepackRelevanceAnalyzer(plans, inventorySnapshot);
            return analyzer.Evaluate;
        }

        private static bool CanTraderTradeNow(ITrader trader)
        {
            return trader.CanTradeNow;
        }

        private static void AnnounceTraderAdvisory(PlanTraderAdvisoryNotification notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            string message = XenogermPlannerPresentation.GetTraderAdvisoryNotificationMessage(
                notification,
                notification.Source.Source.TraderName);

            Pawn navigationPawn = notification.Source.NavigationPawn;

            if (navigationPawn != null && XenogermPlannerTargetInteraction.CanNavigate(navigationPawn))
            {
                Messages.Message(message, navigationPawn, MessageTypeDefOf.PositiveEvent, historical: false);
                return;
            }

            Messages.Message(message, MessageTypeDefOf.PositiveEvent, historical: false);
        }

        private static void ReportError(string message)
        {
            Log.Error($"{XenogermPlannerMod.LogPrefix} {message}");
        }
    }
}