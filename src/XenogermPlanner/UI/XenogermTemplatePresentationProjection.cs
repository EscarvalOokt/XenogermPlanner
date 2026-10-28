using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Verse;
using XenogermPlanner.Templates;

namespace XenogermPlanner.UI
{
    internal sealed class XenogermTemplateCompositionPresentation
    {
        private readonly HashSet<GeneDef> _additionalGenes;
        private readonly ReadOnlyCollection<GeneDef> _sortedGenes;

        internal XenogermTemplateCompositionPresentation(
            PlanXenogermTemplateComposition composition,
            IEnumerable<GeneDef> sortedGenes)
        {
            Composition = composition ?? throw new ArgumentNullException(nameof(composition));

            if (sortedGenes == null)
                throw new ArgumentNullException(nameof(sortedGenes));

            var copiedGenes = new List<GeneDef>();

            foreach (GeneDef gene in sortedGenes)
            {
                if (gene == null)
                    throw new ArgumentException("Composition genes cannot contain null values.", nameof(sortedGenes));

                copiedGenes.Add(gene);
            }

            _sortedGenes = copiedGenes.AsReadOnly();
            _additionalGenes = new HashSet<GeneDef>(composition.AdditionalGenes);
        }

        internal PlanXenogermTemplateComposition Composition { get; }
        internal IReadOnlyList<GeneDef> SortedGenes => _sortedGenes;

        internal bool IsAdditional(GeneDef gene)
        {
            if (gene == null)
                throw new ArgumentNullException(nameof(gene));

            return _additionalGenes.Contains(gene);
        }
    }

    internal sealed class XenogermTemplateCandidatePresentation
    {
        private readonly ReadOnlyCollection<XenogermTemplateCompositionPresentation> _compositions;
        private readonly ReadOnlyCollection<GeneDef> _sortedAdditionalGenes;

        internal XenogermTemplateCandidatePresentation(
            PlanXenogermTemplateCandidate candidate,
            int index,
            string label,
            string summary,
            IEnumerable<XenogermTemplateCompositionPresentation> compositions,
            IEnumerable<GeneDef> sortedAdditionalGenes)
        {
            Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Candidate index cannot be negative.");

            if (compositions == null)
                throw new ArgumentNullException(nameof(compositions));

            if (sortedAdditionalGenes == null)
                throw new ArgumentNullException(nameof(sortedAdditionalGenes));

            Index = index;
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));

            var copiedCompositions = new List<XenogermTemplateCompositionPresentation>();

            foreach (XenogermTemplateCompositionPresentation composition in compositions)
            {
                if (composition == null)
                    throw new ArgumentException(
                        "Candidate compositions cannot contain null values.",
                        nameof(compositions));

                copiedCompositions.Add(composition);
            }

            var copiedAdditionalGenes = new List<GeneDef>();

            foreach (GeneDef gene in sortedAdditionalGenes)
            {
                if (gene == null)
                {
                    throw new ArgumentException(
                        "Candidate additional genes cannot contain null values.",
                        nameof(sortedAdditionalGenes));
                }

                copiedAdditionalGenes.Add(gene);
            }

            _compositions = copiedCompositions.AsReadOnly();
            _sortedAdditionalGenes = copiedAdditionalGenes.AsReadOnly();
        }

        internal PlanXenogermTemplateCandidate Candidate { get; }
        internal int Index { get; }
        internal string Label { get; }
        internal string Summary { get; }
        internal IReadOnlyList<XenogermTemplateCompositionPresentation> Compositions => _compositions;
        internal IReadOnlyList<GeneDef> SortedAdditionalGenes => _sortedAdditionalGenes;
    }

    internal sealed class XenogermTemplatePresentationProjection
    {
        private readonly ReadOnlyCollection<XenogermTemplateCandidatePresentation> _candidates;

        private XenogermTemplatePresentationProjection(IEnumerable<XenogermTemplateCandidatePresentation> candidates)
        {
            if (candidates == null)
                throw new ArgumentNullException(nameof(candidates));

            var copiedCandidates = new List<XenogermTemplateCandidatePresentation>();

            foreach (XenogermTemplateCandidatePresentation candidate in candidates)
            {
                if (candidate == null)
                    throw new ArgumentException("Template candidates cannot contain null values.", nameof(candidates));

                copiedCandidates.Add(candidate);
            }

            _candidates = copiedCandidates.AsReadOnly();
        }

        internal IReadOnlyList<XenogermTemplateCandidatePresentation> Candidates => _candidates;

        internal static XenogermTemplatePresentationProjection Build(
            PlanXenogermTemplateCandidateSearchResult searchResult)
        {
            return Build(
                searchResult,
                (_, index) => XenogermPlannerPresentation.GetTemplateCandidateLabel(index, index == 0),
                XenogermPlannerPresentation.GetTemplateCandidateSummary,
                XenogermPlannerPresentation.GetSortedGenes);
        }

        internal static XenogermTemplatePresentationProjection Build(
            PlanXenogermTemplateCandidateSearchResult searchResult,
            Func<PlanXenogermTemplateCandidate, int, string> getLabel,
            Func<PlanXenogermTemplateCandidate, string> getSummary,
            Func<IEnumerable<GeneDef>, List<GeneDef>> sortGenes)
        {
            if (searchResult == null)
                throw new ArgumentNullException(nameof(searchResult));

            if (getLabel == null)
                throw new ArgumentNullException(nameof(getLabel));

            if (getSummary == null)
                throw new ArgumentNullException(nameof(getSummary));

            if (sortGenes == null)
                throw new ArgumentNullException(nameof(sortGenes));

            var candidates = new List<XenogermTemplateCandidatePresentation>(searchResult.Candidates.Count);

            for (var index = 0; index < searchResult.Candidates.Count; index++)
            {
                PlanXenogermTemplateCandidate candidate = searchResult.Candidates[index];
                var compositions = new List<XenogermTemplateCompositionPresentation>(candidate.Compositions.Count);

                foreach (PlanXenogermTemplateComposition composition in candidate.Compositions)
                {
                    compositions.Add(
                        new XenogermTemplateCompositionPresentation(composition, sortGenes(composition.Genes)));
                }

                candidates.Add(
                    new XenogermTemplateCandidatePresentation(
                        candidate,
                        index,
                        getLabel(candidate, index),
                        getSummary(candidate),
                        compositions,
                        sortGenes(candidate.AdditionalGenes)));
            }

            return new XenogermTemplatePresentationProjection(candidates);
        }
    }
}