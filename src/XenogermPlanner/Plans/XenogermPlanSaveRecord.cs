using System;
using System.Collections.Generic;
using Verse;

namespace XenogermPlanner.Plans
{
    internal sealed class XenogermPlanSaveRecord : IExposable
    {
        private List<string> _desiredGeneDefNames;
        private bool _hasReadinessNotificationBaseline;
        private string _id;
        private bool _lastReadinessNotificationStateWasReady;
        private string _name;
        private int _readinessMode = -1;
        private bool _readinessNotificationsEnabled = true;

        public XenogermPlanSaveRecord()
        {
        }

        internal XenogermPlanSaveRecord(
            string id,
            string name,
            IEnumerable<string> desiredGeneDefNames,
            int readinessMode,
            bool readinessNotificationsEnabled = true,
            bool hasReadinessNotificationBaseline = false,
            bool lastReadinessNotificationStateWasReady = false)
        {
            _id = id;
            _name = name;
            _desiredGeneDefNames = desiredGeneDefNames == null ? null : [..desiredGeneDefNames];
            _readinessMode = readinessMode;
            _readinessNotificationsEnabled = readinessNotificationsEnabled;
            _hasReadinessNotificationBaseline = hasReadinessNotificationBaseline;
            _lastReadinessNotificationStateWasReady =
                hasReadinessNotificationBaseline && lastReadinessNotificationStateWasReady;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref _id, "id");
            Scribe_Values.Look(ref _name, "name");
            Scribe_Collections.Look(ref _desiredGeneDefNames, "desiredGenes", LookMode.Value);
            Scribe_Values.Look(ref _readinessMode, "readinessMode", -1);
            Scribe_Values.Look(ref _readinessNotificationsEnabled, "readinessNotificationsEnabled", true);
            Scribe_Values.Look(ref _hasReadinessNotificationBaseline, "hasReadinessNotificationBaseline");
            Scribe_Values.Look(ref _lastReadinessNotificationStateWasReady, "lastReadinessNotificationStateWasReady");
        }

        internal static XenogermPlanSaveRecord FromPlan(XenogermPlan plan)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            return new XenogermPlanSaveRecord(
                plan.Id,
                plan.Name,
                CollectDesiredGeneDefNames(plan),
                (int)plan.ReadinessMode,
                plan.ReadinessNotificationsEnabled,
                plan.HasReadinessNotificationBaseline,
                plan.LastReadinessNotificationStateWasReady);
        }

        internal bool TryCreatePlan(out XenogermPlan plan, out string failureReason)
        {
            return TryCreatePlan(DefDatabase<GeneDef>.GetNamedSilentFail, out plan, out failureReason);
        }

        internal bool TryCreatePlan(
            Func<string, GeneDef> resolveGeneDef,
            out XenogermPlan plan,
            out string failureReason)
        {
            if (resolveGeneDef == null)
                throw new ArgumentNullException(nameof(resolveGeneDef));

            plan = null;
            failureReason = null;

            if (string.IsNullOrWhiteSpace(_id))
            {
                failureReason = "Plan ID is missing or invalid.";
                return false;
            }

            if (!XenogermPlanNameAllocator.TryNormalize(_name, out string normalizedName))
            {
                failureReason = "Plan name is missing or invalid.";
                return false;
            }

            if (_desiredGeneDefNames == null)
            {
                failureReason = "Desired gene collection is missing.";
                return false;
            }

            if (_readinessMode != (int)PlanReadinessMode.Coverage &&
                _readinessMode != (int)PlanReadinessMode.ExactPayload)
            {
                failureReason = $"Unsupported readiness mode value '{_readinessMode}'.";
                return false;
            }

            var distinctGeneDefNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (string geneDefName in _desiredGeneDefNames)
            {
                if (string.IsNullOrWhiteSpace(geneDefName))
                {
                    failureReason = "Desired gene collection contains an invalid def name.";
                    return false;
                }

                distinctGeneDefNames.Add(geneDefName);
            }

            var resolvedGenes = new List<GeneDef>();
            var unresolvedGeneDefNames = new List<string>();

            foreach (string geneDefName in distinctGeneDefNames)
            {
                GeneDef gene = resolveGeneDef(geneDefName);

                if (gene == null)
                    unresolvedGeneDefNames.Add(geneDefName);
                else
                    resolvedGenes.Add(gene);
            }

            try
            {
                plan = new XenogermPlan(
                    _id,
                    normalizedName,
                    resolvedGenes,
                    unresolvedGeneDefNames,
                    (PlanReadinessMode)_readinessMode,
                    _readinessNotificationsEnabled,
                    _hasReadinessNotificationBaseline,
                    _lastReadinessNotificationStateWasReady);

                return true;
            }
            catch (ArgumentException exception)
            {
                failureReason = exception.Message;
                return false;
            }
        }

        private static List<string> CollectDesiredGeneDefNames(XenogermPlan plan)
        {
            var geneDefNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (GeneDef gene in plan.DesiredGenes)
            {
                if (gene == null)
                    throw new InvalidOperationException("Plan contains a null desired gene.");

                AddGeneDefName(geneDefNames, gene.defName);
            }

            foreach (string geneDefName in plan.UnresolvedDesiredGeneDefNames)
                AddGeneDefName(geneDefNames, geneDefName);

            var sortedGeneDefNames = new List<string>(geneDefNames);
            sortedGeneDefNames.Sort(StringComparer.Ordinal);

            return sortedGeneDefNames;
        }

        private static void AddGeneDefName(HashSet<string> geneDefNames, string geneDefName)
        {
            if (string.IsNullOrWhiteSpace(geneDefName))
                throw new InvalidOperationException("Plan contains a desired gene with an invalid def name.");

            geneDefNames.Add(geneDefName);
        }
    }
}