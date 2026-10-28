using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Verse;

namespace XenogermPlanner.Plans
{
    public sealed class XenogermPlanGameComponent : GameComponent
    {
        private readonly List<XenogermPlan> _plans = new List<XenogermPlan>();
        private ReadOnlyCollection<XenogermPlan> _plansForReading;
        private List<XenogermPlanSaveRecord> _saveRecords;

        // ReSharper disable once UnusedParameter.Local
        public XenogermPlanGameComponent(Game _)
        {
            RefreshPlansForReading();
        }

        public IReadOnlyList<XenogermPlan> Plans => _plansForReading;

        public void AddPlan(XenogermPlan plan)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (ContainsPlanId(plan.Id))
                throw new InvalidOperationException($"A xenogerm plan with ID '{plan.Id}' already exists.");

            if (!TryValidatePlanName(
                    plan.Name,
                    null,
                    out string normalizedName,
                    out XenogermPlanNameValidationFailure failure))
            {
                if (failure == XenogermPlanNameValidationFailure.InvalidName)
                {
                    throw new ArgumentException("Plan name cannot be null, empty or whitespace.", nameof(plan));
                }

                throw new InvalidOperationException($"A xenogerm plan named '{normalizedName}' already exists.");
            }

            if (!string.Equals(plan.Name, normalizedName, StringComparison.Ordinal))
                plan.Rename(normalizedName);

            _plans.Add(plan);
            RefreshPlansForReading();
        }

        internal void AddPlanWithAllocatedName(XenogermPlan plan)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (ContainsPlanId(plan.Id))
                throw new InvalidOperationException($"A xenogerm plan with ID '{plan.Id}' already exists.");

            string allocatedName = AllocateUniquePlanName(plan.Name);

            if (!string.Equals(plan.Name, allocatedName, StringComparison.Ordinal))
                plan.Rename(allocatedName);

            AddPlan(plan);
        }

        public bool RemovePlan(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Plan ID cannot be null, empty or whitespace.", nameof(id));

            for (var index = 0; index < _plans.Count; index++)
            {
                if (!string.Equals(_plans[index].Id, id, StringComparison.Ordinal))
                    continue;

                _plans.RemoveAt(index);
                RefreshPlansForReading();

                return true;
            }

            return false;
        }

        internal bool TryValidatePlanName(
            string requestedName,
            string excludedPlanId,
            out string normalizedName,
            out XenogermPlanNameValidationFailure failure)
        {
            return XenogermPlanNameAllocator.TryValidate(
                _plans,
                requestedName,
                excludedPlanId,
                out normalizedName,
                out failure);
        }

        internal string AllocateUniquePlanName(string preferredName, string excludedPlanId = null)
        {
            return XenogermPlanNameAllocator.Allocate(_plans, preferredName, excludedPlanId);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
                _saveRecords = CreateSaveRecords();

            Scribe_Collections.Look(ref _saveRecords, "plans", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RestorePlans();
                _saveRecords = null;
            }
        }

        internal void RestorePlans(
            IEnumerable<XenogermPlanSaveRecord> records,
            Func<string, GeneDef> resolveGeneDef,
            Action<string> reportError)
        {
            if (resolveGeneDef == null)
                throw new ArgumentNullException(nameof(resolveGeneDef));

            if (reportError == null)
                throw new ArgumentNullException(nameof(reportError));

            _plans.Clear();

            if (records == null)
            {
                RefreshPlansForReading();
                return;
            }

            var restoredPlanIds = new HashSet<string>(StringComparer.Ordinal);
            var index = 0;

            foreach (XenogermPlanSaveRecord saveRecord in records)
            {
                if (saveRecord == null)
                {
                    reportError($"Failed to restore xenogerm plan record at index {index}: " + "record is null.");

                    index++;
                    continue;
                }

                try
                {
                    if (!saveRecord.TryCreatePlan(resolveGeneDef, out XenogermPlan plan, out string failureReason))
                    {
                        reportError($"Failed to restore xenogerm plan record at index {index}: " + failureReason);

                        index++;
                        continue;
                    }

                    if (!restoredPlanIds.Add(plan.Id))
                    {
                        reportError(
                            $"Failed to restore xenogerm plan record at index {index}: " +
                            $"duplicate plan ID '{plan.Id}'.");

                        index++;
                        continue;
                    }

                    string allocatedName = AllocateUniquePlanName(plan.Name);

                    if (!string.Equals(plan.Name, allocatedName, StringComparison.Ordinal))
                        plan.Rename(allocatedName);

                    _plans.Add(plan);
                }
                catch (Exception exception)
                {
                    reportError($"Failed to restore xenogerm plan record at index {index}: " + exception);
                }

                index++;
            }

            RefreshPlansForReading();
        }

        private List<XenogermPlanSaveRecord> CreateSaveRecords()
        {
            var records = new List<XenogermPlanSaveRecord>(_plans.Count);

            foreach (XenogermPlan plan in _plans)
                records.Add(XenogermPlanSaveRecord.FromPlan(plan));

            return records;
        }

        private void RestorePlans()
        {
            RestorePlans(
                _saveRecords,
                DefDatabase<GeneDef>.GetNamedSilentFail,
                message => Log.Error($"{XenogermPlannerMod.LogPrefix} {message}"));
        }

        private bool ContainsPlanId(string id)
        {
            foreach (XenogermPlan plan in _plans)
            {
                if (string.Equals(plan.Id, id, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void RefreshPlansForReading()
        {
            _plansForReading = new List<XenogermPlan>(_plans).AsReadOnly();
        }
    }
}