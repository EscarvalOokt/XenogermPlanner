using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Templates
{
    internal static class PlanXenogermTemplateCandidateSearcher
    {
        private static readonly IEqualityComparer<HashSet<GeneDef>> _geneSetComparer =
            HashSet<GeneDef>.CreateSetComparer();

        internal static PlanXenogermTemplateCandidateSearchResult Search(
            XenogermPlan plan,
            PlanReadinessResult readinessResult,
            PlanGenepackInventorySnapshot inventorySnapshot)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (readinessResult == null)
                throw new ArgumentNullException(nameof(readinessResult));

            if (inventorySnapshot == null)
                throw new ArgumentNullException(nameof(inventorySnapshot));

            return Search(
                plan.DesiredGenes,
                plan.ReadinessMode,
                plan.IsDegraded,
                readinessResult.Status,
                inventorySnapshot.IsAvailable,
                inventorySnapshot.Genepacks,
                GetGenepackGenesOrNull,
                PlanXenogermTemplateCandidateSearchLimits.Default);
        }

        internal static PlanXenogermTemplateCandidateSearchResult Search(
            IReadOnlyCollection<GeneDef> targetGenes,
            PlanReadinessMode readinessMode,
            bool isDegraded,
            PlanReadinessStatus readinessStatus,
            bool inventoryAvailable,
            IReadOnlyList<Genepack> genepacks,
            Func<Genepack, IEnumerable<GeneDef>> getGenepackGenes)
        {
            return Search(
                targetGenes,
                readinessMode,
                isDegraded,
                readinessStatus,
                inventoryAvailable,
                genepacks,
                getGenepackGenes,
                PlanXenogermTemplateCandidateSearchLimits.Default);
        }

        internal static PlanXenogermTemplateCandidateSearchResult Search(
            IReadOnlyCollection<GeneDef> targetGenes,
            PlanReadinessMode readinessMode,
            bool isDegraded,
            PlanReadinessStatus readinessStatus,
            bool inventoryAvailable,
            IReadOnlyList<Genepack> genepacks,
            Func<Genepack, IEnumerable<GeneDef>> getGenepackGenes,
            PlanXenogermTemplateCandidateSearchLimits limits)
        {
            if (targetGenes == null)
                throw new ArgumentNullException(nameof(targetGenes));

            if (genepacks == null)
                throw new ArgumentNullException(nameof(genepacks));

            if (getGenepackGenes == null)
                throw new ArgumentNullException(nameof(getGenepackGenes));

            if (limits == null)
                throw new ArgumentNullException(nameof(limits));

            ValidateReadinessMode(readinessMode);

            HashSet<GeneDef> copiedTargetGenes =
                GenepackCompositionUtility.CopyDistinctGenes(targetGenes, nameof(targetGenes));

            if (!inventoryAvailable ||
                isDegraded ||
                readinessStatus != PlanReadinessStatus.Ready ||
                copiedTargetGenes.Count == 0)
            {
                return new PlanXenogermTemplateCandidateSearchResult(
                    Array.Empty<PlanXenogermTemplateCandidate>(),
                    copiedTargetGenes,
                    readinessMode);
            }

            List<CompositionGroup> compositionGroups = CreateCompositionGroups(
                copiedTargetGenes,
                readinessMode,
                genepacks,
                getGenepackGenes);
            Dictionary<GeneDef, List<CompositionGroup>> groupsByTargetGene =
                CreateGroupsByTargetGene(copiedTargetGenes, compositionGroups);
            var collector = new BoundedCandidateCollector(readinessMode, limits.MaxRetainedCandidates);

            PlanXenogermTemplateCandidate fallbackCandidate = CreateFallbackCandidate(
                copiedTargetGenes,
                groupsByTargetGene);

            if (fallbackCandidate != null)
                collector.Add(fallbackCandidate);

            var searchState = new CandidateSearchState(copiedTargetGenes, groupsByTargetGene, limits, collector);

            EnumerateCandidates(searchState);

            bool isComplete = !searchState.NodeBudgetExhausted && !collector.DiscardedAnyCandidate;

            return new PlanXenogermTemplateCandidateSearchResult(
                collector.Candidates,
                copiedTargetGenes,
                readinessMode,
                isComplete);
        }

        private static List<CompositionGroup> CreateCompositionGroups(
            HashSet<GeneDef> targetGenes,
            PlanReadinessMode readinessMode,
            IEnumerable<Genepack> genepacks,
            Func<Genepack, IEnumerable<GeneDef>> getGenepackGenes)
        {
            var groups = new List<CompositionGroup>();
            var groupsByGenes = new Dictionary<HashSet<GeneDef>, CompositionGroup>(_geneSetComparer);

            foreach (Genepack genepack in genepacks)
            {
                if (genepack == null)
                    continue;

                IEnumerable<GeneDef> genes = getGenepackGenes(genepack);

                if (genes == null)
                    continue;

                if (!GenepackCompositionUtility.TryCopyDistinctGenes(genes, out HashSet<GeneDef> packGenes))
                    continue;

                if (packGenes.Count == 0 || !packGenes.Overlaps(targetGenes))
                    continue;

                if (readinessMode == PlanReadinessMode.ExactPayload && !packGenes.IsSubsetOf(targetGenes))
                    continue;

                if (groupsByGenes.TryGetValue(packGenes, out CompositionGroup existingGroup))
                {
                    existingGroup.PhysicalPackCount++;
                    continue;
                }

                var group = new CompositionGroup(packGenes);
                groupsByGenes.Add(group.Genes, group);
                groups.Add(group);
            }

            groups.Sort((left, right) => StringComparer.Ordinal.Compare(left.CompositionKey, right.CompositionKey));

            return groups;
        }

        private static PlanXenogermTemplateCandidate CreateFallbackCandidate(
            HashSet<GeneDef> targetGenes,
            IReadOnlyDictionary<GeneDef, List<CompositionGroup>> groupsByTargetGene)
        {
            var selectedGroups = new List<CompositionGroup>();
            var selectedGroupSet = new HashSet<CompositionGroup>();
            Dictionary<GeneDef, int> coverageCounts = CreateCoverageCounts(targetGenes);
            var coveredTargetGeneCount = 0;

            while (coveredTargetGeneCount < targetGenes.Count)
            {
                GeneDef nextTargetGene = SelectNextTargetGene(
                    targetGenes,
                    coverageCounts,
                    groupsByTargetGene,
                    selectedGroupSet);

                if (nextTargetGene == null ||
                    !groupsByTargetGene.TryGetValue(nextTargetGene, out List<CompositionGroup> groups))
                {
                    return null;
                }

                CompositionGroup selectedGroup = null;

                foreach (CompositionGroup group in groups)
                {
                    if (!selectedGroupSet.Contains(group))
                    {
                        selectedGroup = group;
                        break;
                    }
                }

                if (selectedGroup == null)
                    return null;

                selectedGroupSet.Add(selectedGroup);
                selectedGroups.Add(selectedGroup);
                AddGroupCoverage(targetGenes, selectedGroup, coverageCounts, ref coveredTargetGeneCount);
            }

            selectedGroups.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.CompositionKey, right.CompositionKey));
            RemoveRedundantGroups(targetGenes, selectedGroups, coverageCounts);

            return CreateCandidate(targetGenes, selectedGroups);
        }

        private static void RemoveRedundantGroups(
            HashSet<GeneDef> targetGenes,
            IList<CompositionGroup> selectedGroups,
            Dictionary<GeneDef, int> coverageCounts)
        {
            for (int groupIndex = selectedGroups.Count - 1; groupIndex >= 0; groupIndex--)
            {
                CompositionGroup group = selectedGroups[groupIndex];

                if (!IsGroupRedundant(targetGenes, group, coverageCounts))
                    continue;

                selectedGroups.RemoveAt(groupIndex);

                foreach (GeneDef gene in group.Genes)
                {
                    if (targetGenes.Contains(gene))
                        coverageCounts[gene]--;
                }
            }
        }

        private static void EnumerateCandidates(CandidateSearchState state)
        {
            if (state.VisitedNodeCount >= state.Limits.MaxVisitedNodes)
            {
                state.NodeBudgetExhausted = true;
                return;
            }

            state.VisitedNodeCount++;

            if (state.CoveredTargetGeneCount == state.TargetGenes.Count)
            {
                if (IsTargetIrredundant(state.TargetGenes, state.SelectedGroups, state.CoverageCounts))
                {
                    state.Collector.Add(CreateCandidate(state.TargetGenes, state.SelectedGroups));
                }

                return;
            }

            GeneDef nextTargetGene = SelectNextTargetGene(
                state.TargetGenes,
                state.CoverageCounts,
                state.GroupsByTargetGene,
                state.SelectedGroupSet);

            if (nextTargetGene == null ||
                !state.GroupsByTargetGene.TryGetValue(nextTargetGene, out List<CompositionGroup> groups))
            {
                return;
            }

            foreach (CompositionGroup group in groups)
            {
                if (state.NodeBudgetExhausted)
                    return;

                if (!state.SelectedGroupSet.Add(group))
                    continue;

                if (!ContributesMissingTarget(state.TargetGenes, group, state.CoverageCounts))
                {
                    state.SelectedGroupSet.Remove(group);
                    continue;
                }

                state.SelectedGroups.Add(group);
                AddGroupCoverage(state.TargetGenes, group, state.CoverageCounts, ref state.CoveredTargetGeneCount);

                EnumerateCandidates(state);

                RemoveGroupCoverage(state.TargetGenes, group, state.CoverageCounts, ref state.CoveredTargetGeneCount);
                state.SelectedGroups.RemoveAt(state.SelectedGroups.Count - 1);
                state.SelectedGroupSet.Remove(group);
            }
        }

        private static PlanXenogermTemplateCandidate CreateCandidate(
            HashSet<GeneDef> targetGenes,
            IEnumerable<CompositionGroup> groups)
        {
            var compositions = new List<PlanXenogermTemplateComposition>();

            foreach (CompositionGroup group in groups)
            {
                var additionalGenes = new HashSet<GeneDef>(group.Genes);
                additionalGenes.ExceptWith(targetGenes);

                compositions.Add(
                    new PlanXenogermTemplateComposition(group.Genes, additionalGenes, group.PhysicalPackCount));
            }

            return new PlanXenogermTemplateCandidate(compositions, targetGenes);
        }

        private static Dictionary<GeneDef, List<CompositionGroup>> CreateGroupsByTargetGene(
            IEnumerable<GeneDef> targetGenes,
            IEnumerable<CompositionGroup> compositionGroups)
        {
            var result = new Dictionary<GeneDef, List<CompositionGroup>>();

            foreach (GeneDef targetGene in targetGenes)
                result.Add(targetGene, new List<CompositionGroup>());

            foreach (CompositionGroup group in compositionGroups)
            {
                foreach (GeneDef gene in group.Genes)
                {
                    if (result.TryGetValue(gene, out List<CompositionGroup> groups))
                        groups.Add(group);
                }
            }

            foreach (List<CompositionGroup> groups in result.Values)
            {
                groups.Sort((left, right) => StringComparer.Ordinal.Compare(left.CompositionKey, right.CompositionKey));
            }

            return result;
        }

        private static Dictionary<GeneDef, int> CreateCoverageCounts(IEnumerable<GeneDef> targetGenes)
        {
            var result = new Dictionary<GeneDef, int>();

            foreach (GeneDef targetGene in targetGenes)
                result.Add(targetGene, 0);

            return result;
        }

        private static GeneDef SelectNextTargetGene(
            IEnumerable<GeneDef> targetGenes,
            IReadOnlyDictionary<GeneDef, int> coverageCounts,
            IReadOnlyDictionary<GeneDef, List<CompositionGroup>> groupsByTargetGene,
            ISet<CompositionGroup> selectedGroups)
        {
            GeneDef selectedGene = null;
            int selectedAvailableGroupCount = int.MaxValue;

            foreach (GeneDef targetGene in targetGenes)
            {
                if (coverageCounts[targetGene] > 0)
                    continue;

                var availableGroupCount = 0;

                if (groupsByTargetGene.TryGetValue(targetGene, out List<CompositionGroup> groups))
                {
                    foreach (CompositionGroup group in groups)
                    {
                        if (!selectedGroups.Contains(group))
                            availableGroupCount++;
                    }
                }

                if (availableGroupCount < selectedAvailableGroupCount ||
                    (availableGroupCount == selectedAvailableGroupCount && CompareGenes(targetGene, selectedGene) < 0))
                {
                    selectedGene = targetGene;
                    selectedAvailableGroupCount = availableGroupCount;
                }
            }

            return selectedAvailableGroupCount == 0 ? null : selectedGene;
        }

        private static bool ContributesMissingTarget(
            HashSet<GeneDef> targetGenes,
            CompositionGroup group,
            IReadOnlyDictionary<GeneDef, int> coverageCounts)
        {
            foreach (GeneDef gene in group.Genes)
            {
                if (targetGenes.Contains(gene) && coverageCounts[gene] == 0)
                    return true;
            }

            return false;
        }

        private static void AddGroupCoverage(
            HashSet<GeneDef> targetGenes,
            CompositionGroup group,
            IDictionary<GeneDef, int> coverageCounts,
            ref int coveredTargetGeneCount)
        {
            foreach (GeneDef gene in group.Genes)
            {
                if (!targetGenes.Contains(gene))
                    continue;

                if (coverageCounts[gene] == 0)
                    coveredTargetGeneCount++;

                coverageCounts[gene]++;
            }
        }

        private static void RemoveGroupCoverage(
            HashSet<GeneDef> targetGenes,
            CompositionGroup group,
            IDictionary<GeneDef, int> coverageCounts,
            ref int coveredTargetGeneCount)
        {
            foreach (GeneDef gene in group.Genes)
            {
                if (!targetGenes.Contains(gene))
                    continue;

                coverageCounts[gene]--;

                if (coverageCounts[gene] == 0)
                    coveredTargetGeneCount--;
            }
        }

        private static bool IsTargetIrredundant(
            HashSet<GeneDef> targetGenes,
            IEnumerable<CompositionGroup> selectedGroups,
            IReadOnlyDictionary<GeneDef, int> coverageCounts)
        {
            foreach (CompositionGroup group in selectedGroups)
            {
                if (IsGroupRedundant(targetGenes, group, coverageCounts))
                    return false;
            }

            return true;
        }

        private static bool IsGroupRedundant(
            HashSet<GeneDef> targetGenes,
            CompositionGroup group,
            IReadOnlyDictionary<GeneDef, int> coverageCounts)
        {
            foreach (GeneDef gene in group.Genes)
            {
                if (targetGenes.Contains(gene) && coverageCounts[gene] == 1)
                    return false;
            }

            return true;
        }

        private static int CompareCandidates(
            PlanXenogermTemplateCandidate left,
            PlanXenogermTemplateCandidate right,
            PlanReadinessMode readinessMode)
        {
            if (readinessMode == PlanReadinessMode.Coverage)
            {
                int additionalGeneComparison = left.AdditionalGenes.Count.CompareTo(right.AdditionalGenes.Count);

                if (additionalGeneComparison != 0)
                    return additionalGeneComparison;
            }

            int geneSetComparison = left.GeneSetCount.CompareTo(right.GeneSetCount);

            if (geneSetComparison != 0)
                return geneSetComparison;

            int occurrenceComparison = left.TotalGeneOccurrences.CompareTo(right.TotalGeneOccurrences);

            if (occurrenceComparison != 0)
                return occurrenceComparison;

            return StringComparer.Ordinal.Compare(left.CandidateKey, right.CandidateKey);
        }

        private static int CompareGenes(GeneDef left, GeneDef right)
        {
            if (right == null)
                return -1;

            return StringComparer.Ordinal.Compare(left?.defName ?? string.Empty, right.defName ?? string.Empty);
        }

        private static IEnumerable<GeneDef> GetGenepackGenesOrNull(Genepack genepack)
        {
            return genepack?.GeneSet?.GenesListForReading;
        }

        private static void ValidateReadinessMode(PlanReadinessMode readinessMode)
        {
            if (readinessMode != PlanReadinessMode.Coverage && readinessMode != PlanReadinessMode.ExactPayload)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(readinessMode),
                    readinessMode,
                    "Unsupported plan readiness mode.");
            }
        }

        private sealed class CompositionGroup
        {
            internal CompositionGroup(HashSet<GeneDef> genes)
            {
                Genes = new HashSet<GeneDef>(genes);
                CompositionKey = GenepackCompositionUtility.CreateCompositionKey(Genes);
                PhysicalPackCount = 1;
            }

            internal HashSet<GeneDef> Genes { get; }
            internal string CompositionKey { get; }
            internal int PhysicalPackCount { get; set; }
        }

        private sealed class BoundedCandidateCollector
        {
            private readonly HashSet<string> _candidateKeys = new HashSet<string>(StringComparer.Ordinal);

            private readonly List<PlanXenogermTemplateCandidate> _candidates =
                new List<PlanXenogermTemplateCandidate>();

            private readonly int _maximumCandidateCount;
            private readonly PlanReadinessMode _readinessMode;

            internal BoundedCandidateCollector(PlanReadinessMode readinessMode, int maximumCandidateCount)
            {
                _readinessMode = readinessMode;
                _maximumCandidateCount = maximumCandidateCount;
            }

            internal IReadOnlyList<PlanXenogermTemplateCandidate> Candidates => _candidates;
            internal bool DiscardedAnyCandidate { get; private set; }

            internal void Add(PlanXenogermTemplateCandidate candidate)
            {
                if (candidate == null)
                    throw new ArgumentNullException(nameof(candidate));

                if (!_candidateKeys.Add(candidate.CandidateKey))
                    return;

                int insertionIndex = FindInsertionIndex(candidate);

                if (_candidates.Count < _maximumCandidateCount)
                {
                    _candidates.Insert(insertionIndex, candidate);
                    return;
                }

                DiscardedAnyCandidate = true;

                if (insertionIndex >= _maximumCandidateCount)
                    return;

                _candidates.Insert(insertionIndex, candidate);
                _candidates.RemoveAt(_candidates.Count - 1);
            }

            private int FindInsertionIndex(PlanXenogermTemplateCandidate candidate)
            {
                var lowerBound = 0;
                int upperBound = _candidates.Count;

                while (lowerBound < upperBound)
                {
                    int middle = lowerBound + (upperBound - lowerBound) / 2;

                    if (CompareCandidates(_candidates[middle], candidate, _readinessMode) <= 0)
                        lowerBound = middle + 1;
                    else
                        upperBound = middle;
                }

                return lowerBound;
            }
        }

        private sealed class CandidateSearchState
        {
            internal int CoveredTargetGeneCount;

            internal CandidateSearchState(
                HashSet<GeneDef> targetGenes,
                IReadOnlyDictionary<GeneDef, List<CompositionGroup>> groupsByTargetGene,
                PlanXenogermTemplateCandidateSearchLimits limits,
                BoundedCandidateCollector collector)
            {
                TargetGenes = targetGenes;
                GroupsByTargetGene = groupsByTargetGene;
                Limits = limits;
                Collector = collector;
                CoverageCounts = CreateCoverageCounts(targetGenes);
            }

            internal HashSet<GeneDef> TargetGenes { get; }
            internal IReadOnlyDictionary<GeneDef, List<CompositionGroup>> GroupsByTargetGene { get; }
            internal PlanXenogermTemplateCandidateSearchLimits Limits { get; }
            internal BoundedCandidateCollector Collector { get; }
            internal List<CompositionGroup> SelectedGroups { get; } = new List<CompositionGroup>();
            internal HashSet<CompositionGroup> SelectedGroupSet { get; } = new HashSet<CompositionGroup>();
            internal Dictionary<GeneDef, int> CoverageCounts { get; }
            internal int VisitedNodeCount { get; set; }
            internal bool NodeBudgetExhausted { get; set; }
        }
    }
}