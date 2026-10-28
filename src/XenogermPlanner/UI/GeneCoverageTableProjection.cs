using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Donors;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.UI
{
    internal sealed class GeneCoverageTableSourceGroup
    {
        private readonly ReadOnlyCollection<Genepack> _genepacks;

        internal GeneCoverageTableSourceGroup(
            PlanGenepackCompositionDiagnostic composition,
            IEnumerable<Genepack> genepacks)
        {
            Composition = composition ?? throw new ArgumentNullException(nameof(composition));

            if (genepacks == null)
                throw new ArgumentNullException(nameof(genepacks));

            var copiedGenepacks = new List<Genepack>();

            foreach (Genepack genepack in genepacks)
            {
                if (genepack == null)
                    throw new ArgumentException("Source genepacks cannot contain null values.", nameof(genepacks));

                copiedGenepacks.Add(genepack);
            }

            _genepacks = copiedGenepacks.AsReadOnly();
        }

        internal PlanGenepackCompositionDiagnostic Composition { get; }
        internal IReadOnlyList<Genepack> Genepacks => _genepacks;
    }

    internal sealed class GeneCoverageTablePresentationRow
    {
        private readonly ReadOnlyCollection<GeneCoverageTableSourceGroup> _sourceGroups;

        internal GeneCoverageTablePresentationRow(
            GeneCoverageTableRow row,
            IEnumerable<GeneCoverageTableSourceGroup> sourceGroups)
        {
            Row = row ?? throw new ArgumentNullException(nameof(row));

            if (sourceGroups == null)
                throw new ArgumentNullException(nameof(sourceGroups));

            var copiedSourceGroups = new List<GeneCoverageTableSourceGroup>();

            foreach (GeneCoverageTableSourceGroup sourceGroup in sourceGroups)
            {
                if (sourceGroup == null)
                    throw new ArgumentException("Source groups cannot contain null values.", nameof(sourceGroups));

                copiedSourceGroups.Add(sourceGroup);
            }

            _sourceGroups = copiedSourceGroups.AsReadOnly();
        }

        internal GeneCoverageTableRow Row { get; }
        internal IReadOnlyList<GeneCoverageTableSourceGroup> SourceGroups => _sourceGroups;
        internal bool IsResolved => Row.IsResolved;
    }

    internal sealed class GeneCoverageTableProjection
    {
        private readonly ReadOnlyCollection<GeneCoverageTablePresentationRow> _rows;

        private GeneCoverageTableProjection(
            IEnumerable<GeneCoverageTablePresentationRow> rows,
            bool showPotentialDonors)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));

            var copiedRows = new List<GeneCoverageTablePresentationRow>();

            foreach (GeneCoverageTablePresentationRow row in rows)
            {
                if (row == null)
                    throw new ArgumentException("Coverage presentation rows cannot contain null values.", nameof(rows));

                copiedRows.Add(row);
            }

            _rows = copiedRows.AsReadOnly();
            ShowPotentialDonors = showPotentialDonors;
        }

        internal IReadOnlyList<GeneCoverageTablePresentationRow> Rows => _rows;
        internal bool ShowPotentialDonors { get; }

        internal static GeneCoverageTableProjection Build(
            XenogermPlan plan,
            PlanReadinessResult readinessResult,
            IReadOnlyList<Genepack> sourceGenepacks,
            PlanPotentialDonorAnalysisResult potentialDonorAnalysis,
            GeneCoverageSortState sortState)
        {
            return Build(
                plan,
                readinessResult,
                sourceGenepacks,
                potentialDonorAnalysis,
                sortState,
                GetGenepackGenes,
                GenepackCompositionUtility.ComparePhysicalGenepacks);
        }

        internal static GeneCoverageTableProjection Build(
            XenogermPlan plan,
            PlanReadinessResult readinessResult,
            IReadOnlyList<Genepack> sourceGenepacks,
            PlanPotentialDonorAnalysisResult potentialDonorAnalysis,
            GeneCoverageSortState sortState,
            Func<Genepack, IReadOnlyList<GeneDef>> getGenepackGenes,
            Comparison<Genepack> comparePhysicalGenepacks)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (readinessResult == null)
                throw new ArgumentNullException(nameof(readinessResult));

            if (sourceGenepacks == null)
                throw new ArgumentNullException(nameof(sourceGenepacks));

            if (getGenepackGenes == null)
                throw new ArgumentNullException(nameof(getGenepackGenes));

            if (comparePhysicalGenepacks == null)
                throw new ArgumentNullException(nameof(comparePhysicalGenepacks));

            Dictionary<PlanGenepackCompositionDiagnostic, IReadOnlyList<Genepack>> lookup =
                CreateGenepacksByCompositionLookup(
                    readinessResult,
                    sourceGenepacks,
                    getGenepackGenes,
                    comparePhysicalGenepacks);

            List<GeneCoverageTableRow> rows = XenogermPlannerPresentation.GetSortedGeneCoverageRows(
                readinessResult.GeneCoverageDiagnostics,
                plan.UnresolvedDesiredGeneDefNames,
                lookup,
                potentialDonorAnalysis,
                sortState);

            var presentationRows = new List<GeneCoverageTablePresentationRow>(rows.Count);

            foreach (GeneCoverageTableRow row in rows)
            {
                var sourceGroups = new List<GeneCoverageTableSourceGroup>();

                if (row.IsResolved)
                {
                    foreach (PlanGenepackCompositionDiagnostic composition in XenogermPlannerPresentation
                                 .GetSortedGenepackCompositions(row.Diagnostic.SourceGenepackCompositions))
                    {
                        if (lookup.TryGetValue(composition, out IReadOnlyList<Genepack> genepacks) &&
                            genepacks.Count > 0)
                        {
                            sourceGroups.Add(new GeneCoverageTableSourceGroup(composition, genepacks));
                        }
                    }
                }

                presentationRows.Add(new GeneCoverageTablePresentationRow(row, sourceGroups));
            }

            return new GeneCoverageTableProjection(presentationRows, potentialDonorAnalysis != null);
        }

        private static Dictionary<PlanGenepackCompositionDiagnostic, IReadOnlyList<Genepack>>
            CreateGenepacksByCompositionLookup(
                PlanReadinessResult readinessResult,
                IReadOnlyList<Genepack> genepacks,
                Func<Genepack, IReadOnlyList<GeneDef>> getGenepackGenes,
                Comparison<Genepack> comparePhysicalGenepacks)
        {
            var lookup = new Dictionary<PlanGenepackCompositionDiagnostic, IReadOnlyList<Genepack>>();
            var matchingGenepacksByComposition = new Dictionary<PlanGenepackCompositionDiagnostic, List<Genepack>>();
            var compositionsByGene = new Dictionary<GeneDef, List<PlanGenepackCompositionDiagnostic>>();

            foreach (PlanGeneCoverageDiagnostic diagnostic in readinessResult.GeneCoverageDiagnostics)
            {
                foreach (PlanGenepackCompositionDiagnostic composition in diagnostic.SourceGenepackCompositions)
                {
                    if (matchingGenepacksByComposition.ContainsKey(composition))
                        continue;

                    matchingGenepacksByComposition.Add(composition, new List<Genepack>(composition.PhysicalPackCount));

                    foreach (GeneDef gene in composition.Genes)
                    {
                        if (!compositionsByGene.TryGetValue(
                                gene,
                                out List<PlanGenepackCompositionDiagnostic> compositions))
                        {
                            compositions = new List<PlanGenepackCompositionDiagnostic>();
                            compositionsByGene.Add(gene, compositions);
                        }

                        compositions.Add(composition);
                    }
                }
            }

            if (matchingGenepacksByComposition.Count == 0)
                return lookup;

            for (var index = 0; index < genepacks.Count; index++)
            {
                Genepack genepack = genepacks[index];

                if (genepack == null)
                    continue;

                IReadOnlyList<GeneDef> genes = getGenepackGenes(genepack);

                if (genes == null || genes.Count == 0)
                    continue;

                List<PlanGenepackCompositionDiagnostic> candidateCompositions = null;
                var canMatchReferencedComposition = true;

                for (var geneIndex = 0; geneIndex < genes.Count; geneIndex++)
                {
                    GeneDef gene = genes[geneIndex];

                    if (gene == null ||
                        !compositionsByGene.TryGetValue(
                            gene,
                            out List<PlanGenepackCompositionDiagnostic> geneCompositions))
                    {
                        canMatchReferencedComposition = false;
                        break;
                    }

                    if (candidateCompositions == null || geneCompositions.Count < candidateCompositions.Count)
                        candidateCompositions = geneCompositions;
                }

                if (!canMatchReferencedComposition || candidateCompositions == null)
                    continue;

                if (!GenepackCompositionUtility.TryCopyDistinctGenes(genes, out HashSet<GeneDef> packGenes) ||
                    packGenes.Count == 0)
                    continue;

                for (var candidateIndex = 0; candidateIndex < candidateCompositions.Count; candidateIndex++)
                {
                    PlanGenepackCompositionDiagnostic composition = candidateCompositions[candidateIndex];

                    if (packGenes.Count != composition.Genes.Count || !packGenes.SetEquals(composition.Genes))
                        continue;

                    matchingGenepacksByComposition[composition].Add(genepack);
                }
            }

            foreach (KeyValuePair<PlanGenepackCompositionDiagnostic, List<Genepack>> pair in
                     matchingGenepacksByComposition)
            {
                if (pair.Value.Count == 0)
                    continue;

                pair.Value.Sort(comparePhysicalGenepacks);
                lookup.Add(pair.Key, pair.Value.AsReadOnly());
            }

            return lookup;
        }

        private static IReadOnlyList<GeneDef> GetGenepackGenes(Genepack genepack)
        {
            return genepack?.GeneSet?.GenesListForReading;
        }
    }

    internal sealed class GeneCoverageTableProjectionCache
    {
        private object _desiredGenesKey;
        private object _languageKey;
        private XenogermPlan _plan;
        private PlanPotentialDonorAnalysisResult _potentialDonorAnalysis;
        private GeneCoverageTableProjection _projection;
        private PlanReadinessResult _readinessResult;
        private GeneCoverageSortState _sortState;
        private IReadOnlyList<Genepack> _sourceGenepacks;
        private object _unresolvedGenesKey;

        internal GeneCoverageTableProjection GetOrBuild(
            XenogermPlan plan,
            PlanReadinessResult readinessResult,
            IReadOnlyList<Genepack> sourceGenepacks,
            PlanPotentialDonorAnalysisResult potentialDonorAnalysis,
            GeneCoverageSortState sortState,
            object languageKey)
        {
            return GetOrBuild(
                plan,
                readinessResult,
                sourceGenepacks,
                potentialDonorAnalysis,
                sortState,
                languageKey,
                genepack => genepack?.GeneSet?.GenesListForReading,
                GenepackCompositionUtility.ComparePhysicalGenepacks);
        }

        internal GeneCoverageTableProjection GetOrBuild(
            XenogermPlan plan,
            PlanReadinessResult readinessResult,
            IReadOnlyList<Genepack> sourceGenepacks,
            PlanPotentialDonorAnalysisResult potentialDonorAnalysis,
            GeneCoverageSortState sortState,
            object languageKey,
            Func<Genepack, IReadOnlyList<GeneDef>> getGenepackGenes,
            Comparison<Genepack> comparePhysicalGenepacks)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (readinessResult == null)
                throw new ArgumentNullException(nameof(readinessResult));

            if (sourceGenepacks == null)
                throw new ArgumentNullException(nameof(sourceGenepacks));

            if (getGenepackGenes == null)
                throw new ArgumentNullException(nameof(getGenepackGenes));

            if (comparePhysicalGenepacks == null)
                throw new ArgumentNullException(nameof(comparePhysicalGenepacks));

            if (IsCompatible(plan, readinessResult, sourceGenepacks, potentialDonorAnalysis, sortState, languageKey))
            {
                return _projection;
            }

            var projection = GeneCoverageTableProjection.Build(
                plan,
                readinessResult,
                sourceGenepacks,
                potentialDonorAnalysis,
                sortState,
                getGenepackGenes,
                comparePhysicalGenepacks);

            Capture(plan, readinessResult, sourceGenepacks, potentialDonorAnalysis, sortState, languageKey, projection);

            return projection;
        }

        internal void Invalidate()
        {
            _plan = null;
            _desiredGenesKey = null;
            _unresolvedGenesKey = null;
            _readinessResult = null;
            _sourceGenepacks = null;
            _potentialDonorAnalysis = null;
            _languageKey = null;
            _projection = null;
        }

        private bool IsCompatible(
            XenogermPlan plan,
            PlanReadinessResult readinessResult,
            IReadOnlyList<Genepack> sourceGenepacks,
            PlanPotentialDonorAnalysisResult potentialDonorAnalysis,
            GeneCoverageSortState sortState,
            object languageKey)
        {
            return _projection != null &&
                   ReferenceEquals(_plan, plan) &&
                   ReferenceEquals(_desiredGenesKey, plan.DesiredGenes) &&
                   ReferenceEquals(_unresolvedGenesKey, plan.UnresolvedDesiredGeneDefNames) &&
                   ReferenceEquals(_readinessResult, readinessResult) &&
                   ReferenceEquals(_sourceGenepacks, sourceGenepacks) &&
                   ReferenceEquals(_potentialDonorAnalysis, potentialDonorAnalysis) &&
                   _sortState.Column == sortState.Column &&
                   _sortState.Descending == sortState.Descending &&
                   Equals(_languageKey, languageKey);
        }

        private void Capture(
            XenogermPlan plan,
            PlanReadinessResult readinessResult,
            IReadOnlyList<Genepack> sourceGenepacks,
            PlanPotentialDonorAnalysisResult potentialDonorAnalysis,
            GeneCoverageSortState sortState,
            object languageKey,
            GeneCoverageTableProjection projection)
        {
            _plan = plan;
            _desiredGenesKey = plan.DesiredGenes;
            _unresolvedGenesKey = plan.UnresolvedDesiredGeneDefNames;
            _readinessResult = readinessResult;
            _sourceGenepacks = sourceGenepacks;
            _potentialDonorAnalysis = potentialDonorAnalysis;
            _sortState = sortState;
            _languageKey = languageKey;
            _projection = projection;
        }
    }
}