using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;
using XenogermPlanner.UI;

namespace XenogermPlanner.Notifications
{
    public sealed class PlanReadinessNotificationGameComponent : GameComponent
    {
        private readonly Func<XenogermPlan, PlanGenepackInventorySnapshot, PlanReadinessResult> _analyze;
        private readonly Action<XenogermPlan> _announce;
        private readonly Func<PlanGenepackInventorySnapshot> _getInventorySnapshot;
        private readonly Func<IReadOnlyList<XenogermPlan>> _getPlans;
        private readonly Action<string> _reportError;
        private bool _invalidated = true;

        private PlanGenepackInventorySnapshot _lastEvaluatedInventorySnapshot;

        // ReSharper disable once UnusedParameter.Local
        public PlanReadinessNotificationGameComponent(Game _) : this(
            GetCurrentPlans,
            GetCurrentInventorySnapshot,
            PlanReadinessAnalyzer.Analyze,
            AnnouncePlanReady,
            ReportError)
        {
        }

        internal PlanReadinessNotificationGameComponent(
            Func<IReadOnlyList<XenogermPlan>> getPlans,
            Func<PlanGenepackInventorySnapshot> getInventorySnapshot,
            Func<XenogermPlan, PlanGenepackInventorySnapshot, PlanReadinessResult> analyze,
            Action<XenogermPlan> announce,
            Action<string> reportError)
        {
            _getPlans = getPlans ?? throw new ArgumentNullException(nameof(getPlans));
            _getInventorySnapshot = getInventorySnapshot ??
                                    throw new ArgumentNullException(nameof(getInventorySnapshot));
            _analyze = analyze ?? throw new ArgumentNullException(nameof(analyze));
            _announce = announce ?? throw new ArgumentNullException(nameof(announce));
            _reportError = reportError ?? throw new ArgumentNullException(nameof(reportError));
        }

        public void Invalidate()
        {
            _invalidated = true;
        }

        public override void GameComponentTick()
        {
            IReadOnlyList<XenogermPlan> plans = _getPlans();
            PlanGenepackInventorySnapshot inventorySnapshot = _getInventorySnapshot();

            if (plans == null || inventorySnapshot == null)
                return;

            if (!_invalidated && ReferenceEquals(_lastEvaluatedInventorySnapshot, inventorySnapshot))
                return;

            _lastEvaluatedInventorySnapshot = inventorySnapshot;
            _invalidated = false;

            foreach (XenogermPlan plan in plans)
            {
                try
                {
                    PlanReadinessResult readinessResult = _analyze(plan, inventorySnapshot) ??
                                                          throw new InvalidOperationException(
                                                              "Readiness analyzer returned a null result.");

                    if (PlanReadinessNotificationTracker.Update(plan, readinessResult))
                        _announce(plan);
                }
                catch (Exception exception)
                {
                    string planId = plan?.Id ?? "<null>";

                    _reportError(
                        $"Failed to evaluate readiness notification for xenogerm plan '{planId}': {exception}");
                }
            }
        }

        private static IReadOnlyList<XenogermPlan> GetCurrentPlans()
        {
            return Current.Game?.GetComponent<XenogermPlanGameComponent>()?.Plans;
        }

        private static PlanGenepackInventorySnapshot GetCurrentInventorySnapshot()
        {
            return Current.Game?.GetComponent<PlanGenepackInventoryGameComponent>()?.Snapshot;
        }

        private static void AnnouncePlanReady(XenogermPlan plan)
        {
            string message = XenogermPlannerPresentation.GetReadinessReadyNotificationMessage(plan);

            Messages.Message(message, MessageTypeDefOf.PositiveEvent, historical: false);
        }

        private static void ReportError(string message)
        {
            Log.Error($"{XenogermPlannerMod.LogPrefix} {message}");
        }
    }
}