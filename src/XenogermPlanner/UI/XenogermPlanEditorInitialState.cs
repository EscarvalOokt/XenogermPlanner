using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Verse;
using XenogermPlanner.Plans;

namespace XenogermPlanner.UI
{
    internal sealed class XenogermPlanEditorInitialState
    {
        private readonly ReadOnlyCollection<GeneDef> _desiredGenes;

        internal XenogermPlanEditorInitialState(
            string planName,
            IEnumerable<GeneDef> desiredGenes,
            PlanReadinessMode readinessMode,
            bool readinessNotificationsEnabled)
        {
            if (planName == null)
                throw new ArgumentNullException(nameof(planName));

            if (desiredGenes == null)
                throw new ArgumentNullException(nameof(desiredGenes));

            var distinctGenes = new HashSet<GeneDef>();

            foreach (GeneDef gene in desiredGenes)
            {
                if (gene == null)
                    throw new ArgumentException(
                        "Initial gene collection cannot contain null values.",
                        nameof(desiredGenes));

                distinctGenes.Add(gene);
            }

            PlanName = planName;
            _desiredGenes = new List<GeneDef>(distinctGenes).AsReadOnly();
            ReadinessMode = readinessMode;
            ReadinessNotificationsEnabled = readinessNotificationsEnabled;
        }

        internal string PlanName { get; }
        internal IReadOnlyCollection<GeneDef> DesiredGenes => _desiredGenes;
        internal PlanReadinessMode ReadinessMode { get; }
        internal bool ReadinessNotificationsEnabled { get; }

        internal static XenogermPlanEditorInitialState CreateEmpty()
        {
            return CreateFromSource(string.Empty, Array.Empty<GeneDef>());
        }

        internal static XenogermPlanEditorInitialState CreateFromSource(
            string planName,
            IEnumerable<GeneDef> desiredGenes)
        {
            return new XenogermPlanEditorInitialState(
                planName,
                desiredGenes,
                PlanReadinessMode.Coverage,
                readinessNotificationsEnabled: true);
        }
    }
}