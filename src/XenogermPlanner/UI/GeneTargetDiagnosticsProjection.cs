using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Plans;

namespace XenogermPlanner.UI
{
    internal enum GeneTargetDiagnosticPresentationRowKind
    {
        Conflict,
        RandomChoiceGroup,
        Prerequisite
    }

    internal sealed class GeneTargetDiagnosticPresentationRow
    {
        private readonly ReadOnlyCollection<GeneDef> _genes;

        private GeneTargetDiagnosticPresentationRow(
            GeneTargetDiagnosticPresentationRowKind kind,
            PlanGeneConflictDiagnostic conflictDiagnostic,
            PlanGeneRandomChoiceGroupDiagnostic randomChoiceGroupDiagnostic,
            PlanGenePrerequisiteDiagnostic prerequisiteDiagnostic,
            GeneDef firstGene,
            GeneDef secondGene,
            IEnumerable<GeneDef> genes,
            string message)
        {
            Kind = kind;
            ConflictDiagnostic = conflictDiagnostic;
            RandomChoiceGroupDiagnostic = randomChoiceGroupDiagnostic;
            PrerequisiteDiagnostic = prerequisiteDiagnostic;
            FirstGene = firstGene;
            SecondGene = secondGene;
            Message = message ?? throw new ArgumentNullException(nameof(message));

            var copiedGenes = new List<GeneDef>();

            if (genes != null)
            {
                foreach (GeneDef gene in genes)
                {
                    if (gene == null)
                        throw new ArgumentException(
                            "Diagnostic gene collection cannot contain null values.",
                            nameof(genes));

                    copiedGenes.Add(gene);
                }
            }

            _genes = copiedGenes.AsReadOnly();
        }

        internal GeneTargetDiagnosticPresentationRowKind Kind { get; }
        internal PlanGeneConflictDiagnostic ConflictDiagnostic { get; }
        internal PlanGeneRandomChoiceGroupDiagnostic RandomChoiceGroupDiagnostic { get; }
        internal PlanGenePrerequisiteDiagnostic PrerequisiteDiagnostic { get; }
        internal GeneDef FirstGene { get; }
        internal GeneDef SecondGene { get; }
        internal IReadOnlyList<GeneDef> Genes => _genes;
        internal string Message { get; }

        internal static GeneTargetDiagnosticPresentationRow CreateConflict(
            PlanGeneConflictDiagnostic diagnostic,
            string message)
        {
            if (diagnostic == null)
                throw new ArgumentNullException(nameof(diagnostic));

            GeneDef firstGene = diagnostic.HasPredictedWinner ? diagnostic.OverridingGene : diagnostic.FirstGene;
            GeneDef secondGene = diagnostic.HasPredictedWinner ? diagnostic.OverriddenGene : diagnostic.SecondGene;

            return new GeneTargetDiagnosticPresentationRow(
                GeneTargetDiagnosticPresentationRowKind.Conflict,
                diagnostic,
                null,
                null,
                firstGene,
                secondGene,
                null,
                message);
        }

        internal static GeneTargetDiagnosticPresentationRow CreateRandomChoiceGroup(
            PlanGeneRandomChoiceGroupDiagnostic diagnostic,
            IEnumerable<GeneDef> sortedGenes,
            string message)
        {
            if (diagnostic == null)
                throw new ArgumentNullException(nameof(diagnostic));

            if (sortedGenes == null)
                throw new ArgumentNullException(nameof(sortedGenes));

            return new GeneTargetDiagnosticPresentationRow(
                GeneTargetDiagnosticPresentationRowKind.RandomChoiceGroup,
                null,
                diagnostic,
                null,
                null,
                null,
                sortedGenes,
                message);
        }

        internal static GeneTargetDiagnosticPresentationRow CreatePrerequisite(
            PlanGenePrerequisiteDiagnostic diagnostic,
            string message)
        {
            if (diagnostic == null)
                throw new ArgumentNullException(nameof(diagnostic));

            return new GeneTargetDiagnosticPresentationRow(
                GeneTargetDiagnosticPresentationRowKind.Prerequisite,
                null,
                null,
                diagnostic,
                diagnostic.DependentGene,
                diagnostic.PrerequisiteGene,
                null,
                message);
        }
    }

    internal sealed class GeneTargetDiagnosticsProjection
    {
        private readonly ReadOnlyCollection<GeneTargetDiagnosticPresentationRow> _rows;

        private GeneTargetDiagnosticsProjection(IEnumerable<GeneTargetDiagnosticPresentationRow> rows)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));

            var copiedRows = new List<GeneTargetDiagnosticPresentationRow>();

            foreach (GeneTargetDiagnosticPresentationRow row in rows)
            {
                if (row == null)
                    throw new ArgumentException(
                        "Diagnostic presentation rows cannot contain null values.",
                        nameof(rows));

                copiedRows.Add(row);
            }

            _rows = copiedRows.AsReadOnly();
        }

        internal IReadOnlyList<GeneTargetDiagnosticPresentationRow> Rows => _rows;
        internal bool HasDiagnostics => _rows.Count > 0;

        internal static GeneTargetDiagnosticsProjection Build(
            PlanGeneTargetAnalysisResult analysis,
            PlanReadinessMode readinessMode)
        {
            return Build(
                analysis,
                readinessMode,
                XenogermPlannerPresentation.GetSortedGeneConflictDiagnostics,
                XenogermPlannerPresentation.GetSortedGeneRandomChoiceGroupDiagnostics,
                XenogermPlannerPresentation.GetSortedGenePrerequisiteDiagnostics,
                XenogermPlannerPresentation.GetSortedGenes,
                XenogermPlannerPresentation.GetGeneConflictMessage,
                XenogermPlannerPresentation.GetGeneRandomChoiceGroupMessage,
                diagnostic => XenogermPlannerPresentation.GetGenePrerequisiteMessage(diagnostic, readinessMode));
        }

        internal static GeneTargetDiagnosticsProjection Build(
            PlanGeneTargetAnalysisResult analysis,
            PlanReadinessMode readinessMode,
            Func<IEnumerable<PlanGeneConflictDiagnostic>, List<PlanGeneConflictDiagnostic>> sortConflicts,
            Func<IEnumerable<PlanGeneRandomChoiceGroupDiagnostic>, List<PlanGeneRandomChoiceGroupDiagnostic>>
                sortRandomChoiceGroups,
            Func<IEnumerable<PlanGenePrerequisiteDiagnostic>, List<PlanGenePrerequisiteDiagnostic>> sortPrerequisites,
            Func<IEnumerable<GeneDef>, List<GeneDef>> sortGenes,
            Func<PlanGeneConflictDiagnostic, string> getConflictMessage,
            Func<PlanGeneRandomChoiceGroupDiagnostic, string> getRandomChoiceGroupMessage,
            Func<PlanGenePrerequisiteDiagnostic, string> getPrerequisiteMessage)
        {
            if (analysis == null)
                throw new ArgumentNullException(nameof(analysis));

            if (readinessMode != PlanReadinessMode.Coverage && readinessMode != PlanReadinessMode.ExactPayload)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(readinessMode),
                    readinessMode,
                    "Unsupported plan readiness mode.");
            }

            if (sortConflicts == null)
                throw new ArgumentNullException(nameof(sortConflicts));

            if (sortRandomChoiceGroups == null)
                throw new ArgumentNullException(nameof(sortRandomChoiceGroups));

            if (sortPrerequisites == null)
                throw new ArgumentNullException(nameof(sortPrerequisites));

            if (sortGenes == null)
                throw new ArgumentNullException(nameof(sortGenes));

            if (getConflictMessage == null)
                throw new ArgumentNullException(nameof(getConflictMessage));

            if (getRandomChoiceGroupMessage == null)
                throw new ArgumentNullException(nameof(getRandomChoiceGroupMessage));

            if (getPrerequisiteMessage == null)
                throw new ArgumentNullException(nameof(getPrerequisiteMessage));

            var rows = new List<GeneTargetDiagnosticPresentationRow>(analysis.DiagnosticCount);

            foreach (PlanGeneConflictDiagnostic diagnostic in sortConflicts(analysis.Conflicts))
            {
                rows.Add(
                    GeneTargetDiagnosticPresentationRow.CreateConflict(diagnostic, getConflictMessage(diagnostic)));
            }

            foreach (PlanGeneRandomChoiceGroupDiagnostic diagnostic in sortRandomChoiceGroups(
                         analysis.RandomChoiceGroups))
            {
                rows.Add(
                    GeneTargetDiagnosticPresentationRow.CreateRandomChoiceGroup(
                        diagnostic,
                        sortGenes(diagnostic.Genes),
                        getRandomChoiceGroupMessage(diagnostic)));
            }

            foreach (PlanGenePrerequisiteDiagnostic diagnostic in sortPrerequisites(analysis.MissingPrerequisites))
            {
                rows.Add(
                    GeneTargetDiagnosticPresentationRow.CreatePrerequisite(
                        diagnostic,
                        getPrerequisiteMessage(diagnostic)));
            }

            return new GeneTargetDiagnosticsProjection(rows);
        }
    }
}