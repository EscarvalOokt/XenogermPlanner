using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Assemblers
{
    internal static class PlanAssemblerCandidateSearcher
    {
        private static readonly IEqualityComparer<HashSet<GeneDef>> _geneSetComparer =
            HashSet<GeneDef>.CreateSetComparer();

        internal static IEnumerable<PlanAssemblerCandidate> Search(
            IReadOnlyCollection<GeneDef> desiredGenes,
            PlanReadinessMode readinessMode,
            PlanAssemblerScopeSnapshot scope)
        {
            return Search(
                desiredGenes,
                readinessMode,
                scope,
                GenepackCompositionUtility.GetGenes,
                GenepackCompositionUtility.GetStablePhysicalKey);
        }

        internal static IEnumerable<PlanAssemblerCandidate> Search(
            IReadOnlyCollection<GeneDef> desiredGenes,
            PlanReadinessMode readinessMode,
            PlanAssemblerScopeSnapshot scope,
            Func<Genepack, IEnumerable<GeneDef>> getGenepackGenes,
            Func<Genepack, string> getPhysicalKey,
            PlanAssemblerSearchBudget budget = null,
            bool includeHelpers = false)
        {
            if (desiredGenes == null)
                throw new ArgumentNullException(nameof(desiredGenes));

            if (scope == null)
                throw new ArgumentNullException(nameof(scope));

            if (getGenepackGenes == null)
                throw new ArgumentNullException(nameof(getGenepackGenes));

            if (getPhysicalKey == null)
                throw new ArgumentNullException(nameof(getPhysicalKey));

            ValidateReadinessMode(readinessMode);

            HashSet<GeneDef> targetGenes = GenepackCompositionUtility.CopyDistinctGenes(
                desiredGenes,
                nameof(desiredGenes));

            if (targetGenes.Count == 0)
                return Array.Empty<PlanAssemblerCandidate>();

            List<CompositionGroup> compositionGroups = CreateCompositionGroups(
                targetGenes,
                readinessMode,
                scope,
                getGenepackGenes,
                getPhysicalKey);

            return SearchCandidates(
                targetGenes,
                compositionGroups,
                budget ?? new PlanAssemblerSearchBudget(),
                includeHelpers && readinessMode == PlanReadinessMode.Coverage);
        }

        private static IEnumerable<PlanAssemblerCandidate> SearchCandidates(
            HashSet<GeneDef> targetGenes,
            IReadOnlyList<CompositionGroup> compositionGroups,
            PlanAssemblerSearchBudget budget,
            bool includeHelpers)
        {
            Dictionary<GeneDef, List<CompositionGroup>> groupsByGene = CreateGroupsByGene(compositionGroups);
            var selectedGroups = new List<CompositionGroup>();
            var selectedGroupSet = new HashSet<CompositionGroup>();
            var selectedGenes = new HashSet<GeneDef>();
            var emittedCandidateKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (PlanAssemblerCandidate candidate in SearchCandidates(
                         targetGenes,
                         compositionGroups,
                         groupsByGene,
                         selectedGroups,
                         selectedGroupSet,
                         selectedGenes,
                         emittedCandidateKeys,
                         budget))
            {
                yield return candidate;
            }

            if (budget.IsExhausted || !includeHelpers || compositionGroups.Count <= 1)
                yield break;

            // The first pass preserves the small requirement-contributing search. The fallback
            // also admits packs needed only for metabolism or an override of another live blocker.
            selectedGroups.Clear();
            selectedGenes.Clear();
            foreach (PlanAssemblerCandidate candidate in SearchHelperCandidates(
                         targetGenes,
                         compositionGroups,
                         selectedGroups,
                         selectedGenes,
                         0,
                         emittedCandidateKeys,
                         budget))
            {
                yield return candidate;
            }
        }

        private static IEnumerable<PlanAssemblerCandidate> SearchCandidates(
            HashSet<GeneDef> targetGenes,
            IReadOnlyList<CompositionGroup> compositionGroups,
            IReadOnlyDictionary<GeneDef, List<CompositionGroup>> groupsByGene,
            List<CompositionGroup> selectedGroups,
            HashSet<CompositionGroup> selectedGroupSet,
            HashSet<GeneDef> selectedGenes,
            HashSet<string> emittedCandidateKeys,
            PlanAssemblerSearchBudget budget)
        {
            if (!budget.TryVisitState())
                yield break;

            List<GeneDef> missingTargetGenes = GetMissingGenes(targetGenes, selectedGenes);
            List<PlanGenePrerequisiteDiagnostic> missingPrerequisites =
                PlanGeneTargetAnalyzer.FindMissingPrerequisites(selectedGenes);

            if (missingTargetGenes.Count == 0)
            {
                if (missingPrerequisites.Count == 0)
                {
                    PlanAssemblerCandidate completeCandidate = TryCreateCandidate(
                        targetGenes,
                        selectedGroups,
                        missingPrerequisites,
                        emittedCandidateKeys);

                    if (completeCandidate != null)
                        yield return completeCandidate;

                    yield break;
                }

                GeneDef nextPrerequisite = SelectNextRequirement(
                    missingPrerequisites.Select(diagnostic => diagnostic.PrerequisiteGene),
                    groupsByGene,
                    selectedGroupSet);

                if (nextPrerequisite == null)
                {
                    PlanAssemblerCandidate fallbackCandidate = TryCreateCandidate(
                        targetGenes,
                        selectedGroups,
                        missingPrerequisites,
                        emittedCandidateKeys);

                    if (fallbackCandidate != null)
                        yield return fallbackCandidate;

                    yield break;
                }

                foreach (PlanAssemblerCandidate candidate in AddRequirementGroups(
                             nextPrerequisite,
                             targetGenes,
                             compositionGroups,
                             groupsByGene,
                             selectedGroups,
                             selectedGroupSet,
                             selectedGenes,
                             emittedCandidateKeys,
                             budget))
                {
                    yield return candidate;
                }

                yield break;
            }

            GeneDef nextTargetGene = SelectNextRequirement(missingTargetGenes, groupsByGene, selectedGroupSet);

            if (nextTargetGene == null)
                yield break;

            foreach (PlanAssemblerCandidate candidate in AddRequirementGroups(
                         nextTargetGene,
                         targetGenes,
                         compositionGroups,
                         groupsByGene,
                         selectedGroups,
                         selectedGroupSet,
                         selectedGenes,
                         emittedCandidateKeys,
                         budget))
            {
                yield return candidate;
            }
        }

        private static IEnumerable<PlanAssemblerCandidate> AddRequirementGroups(
            GeneDef requirement,
            HashSet<GeneDef> targetGenes,
            IReadOnlyList<CompositionGroup> compositionGroups,
            IReadOnlyDictionary<GeneDef, List<CompositionGroup>> groupsByGene,
            List<CompositionGroup> selectedGroups,
            HashSet<CompositionGroup> selectedGroupSet,
            HashSet<GeneDef> selectedGenes,
            HashSet<string> emittedCandidateKeys,
            PlanAssemblerSearchBudget budget)
        {
            if (!groupsByGene.TryGetValue(requirement, out List<CompositionGroup> groups))
                yield break;

            foreach (CompositionGroup group in groups)
            {
                if (budget.IsExhausted)
                    yield break;

                if (selectedGroupSet.Contains(group))
                    continue;

                var newlyAddedGenes = new List<GeneDef>();

                foreach (GeneDef gene in group.Genes)
                {
                    if (selectedGenes.Add(gene))
                        newlyAddedGenes.Add(gene);
                }

                if (newlyAddedGenes.Count == 0)
                    continue;

                selectedGroups.Add(group);
                selectedGroupSet.Add(group);

                foreach (PlanAssemblerCandidate candidate in SearchCandidates(
                             targetGenes,
                             compositionGroups,
                             groupsByGene,
                             selectedGroups,
                             selectedGroupSet,
                             selectedGenes,
                             emittedCandidateKeys,
                             budget))
                {
                    yield return candidate;
                }

                selectedGroupSet.Remove(group);
                selectedGroups.RemoveAt(selectedGroups.Count - 1);

                foreach (GeneDef gene in newlyAddedGenes)
                    selectedGenes.Remove(gene);

                RebuildSelectedGenes(selectedGroups, selectedGenes);
            }
        }

        private static PlanAssemblerCandidate TryCreateCandidate(
            HashSet<GeneDef> targetGenes,
            IReadOnlyList<CompositionGroup> selectedGroups,
            IReadOnlyList<PlanGenePrerequisiteDiagnostic> missingPrerequisites,
            ISet<string> emittedCandidateKeys,
            bool requireIrredundant = true)
        {
            if (selectedGroups.Count == 0 ||
                (requireIrredundant && !IsCandidateIrredundant(targetGenes, selectedGroups)))
                return null;

            var orderedGroups = new List<CompositionGroup>(selectedGroups);
            orderedGroups.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.CompositionKey, right.CompositionKey));

            var compositionKeys = new string[orderedGroups.Count];
            var representativeSources = new PlanAssemblerGenepackSource[orderedGroups.Count];

            for (var index = 0; index < orderedGroups.Count; index++)
            {
                compositionKeys[index] = orderedGroups[index].CompositionKey;
                representativeSources[index] = orderedGroups[index].RepresentativeSource;
            }

            var candidateKey = string.Join("\u001e", compositionKeys);

            if (!emittedCandidateKeys.Add(candidateKey))
                return null;

            return new PlanAssemblerCandidate(representativeSources, missingPrerequisites);
        }

        private static IEnumerable<PlanAssemblerCandidate> SearchHelperCandidates(
            HashSet<GeneDef> targetGenes,
            IReadOnlyList<CompositionGroup> groups,
            List<CompositionGroup> selectedGroups,
            HashSet<GeneDef> selectedGenes,
            int nextIndex,
            ISet<string> emittedCandidateKeys,
            PlanAssemblerSearchBudget budget)
        {
            if (!budget.TryVisitState())
                yield break;

            List<PlanGenePrerequisiteDiagnostic> missingPrerequisites =
                PlanGeneTargetAnalyzer.FindMissingPrerequisites(selectedGenes);

            if (targetGenes.IsSubsetOf(selectedGenes))
            {
                PlanAssemblerCandidate candidate = TryCreateCandidate(
                    targetGenes,
                    selectedGroups,
                    missingPrerequisites,
                    emittedCandidateKeys,
                    requireIrredundant: false);

                if (candidate != null)
                    yield return candidate;
            }

            // Raw membership is monotonic. Effective biostats are not: a later pack can
            // override genes, so current metabolism/complexity cannot prune this branch.
            var reachableGenes = new HashSet<GeneDef>(selectedGenes);
            for (int index = nextIndex; index < groups.Count; index++)
                reachableGenes.UnionWith(groups[index].Genes);

            if (!targetGenes.IsSubsetOf(reachableGenes) ||
                missingPrerequisites.Any(diagnostic => !reachableGenes.Contains(diagnostic.PrerequisiteGene)))
            {
                yield break;
            }

            for (int index = nextIndex; index < groups.Count; index++)
            {
                if (budget.IsExhausted)
                    yield break;

                selectedGroups.Add(groups[index]);
                selectedGenes.UnionWith(groups[index].Genes);

                foreach (PlanAssemblerCandidate candidate in SearchHelperCandidates(
                             targetGenes,
                             groups,
                             selectedGroups,
                             selectedGenes,
                             index + 1,
                             emittedCandidateKeys,
                             budget))
                {
                    yield return candidate;
                }

                selectedGroups.RemoveAt(selectedGroups.Count - 1);
                RebuildSelectedGenes(selectedGroups, selectedGenes);
            }
        }

        private static bool IsCandidateIrredundant(
            HashSet<GeneDef> targetGenes,
            IReadOnlyList<CompositionGroup> selectedGroups)
        {
            HashSet<GeneDef> selectedGenes = CreateGeneUnion(selectedGroups);
            List<PlanGenePrerequisiteDiagnostic> currentMissing =
                PlanGeneTargetAnalyzer.FindMissingPrerequisites(selectedGenes);
            HashSet<string> currentMissingKeys = CreatePrerequisiteKeys(currentMissing);

            for (var removedIndex = 0; removedIndex < selectedGroups.Count; removedIndex++)
            {
                HashSet<GeneDef> reducedGenes = CreateGeneUnionExcept(selectedGroups, removedIndex);

                if (!targetGenes.IsSubsetOf(reducedGenes))
                    continue;

                HashSet<string> reducedMissingKeys = CreatePrerequisiteKeys(
                    PlanGeneTargetAnalyzer.FindMissingPrerequisites(reducedGenes));

                if (reducedMissingKeys.IsSubsetOf(currentMissingKeys))
                    return false;
            }

            return targetGenes.IsSubsetOf(selectedGenes);
        }

        private static List<CompositionGroup> CreateCompositionGroups(
            HashSet<GeneDef> targetGenes,
            PlanReadinessMode readinessMode,
            PlanAssemblerScopeSnapshot scope,
            Func<Genepack, IEnumerable<GeneDef>> getGenepackGenes,
            Func<Genepack, string> getPhysicalKey)
        {
            var compositionGroups = new List<CompositionGroup>();
            var groupsByGenes = new Dictionary<HashSet<GeneDef>, CompositionGroup>(_geneSetComparer);

            foreach (PlanAssemblerGenepackSource source in scope.Sources)
            {
                HashSet<GeneDef> packGenes = GenepackCompositionUtility.CopyDistinctGenes(
                    getGenepackGenes(source.Genepack) ??
                    throw new InvalidOperationException("Genepack gene collection is unavailable."),
                    nameof(getGenepackGenes));

                if (packGenes.Count == 0)
                    continue;

                if (readinessMode == PlanReadinessMode.ExactPayload && !packGenes.IsSubsetOf(targetGenes))
                    continue;

                if (!groupsByGenes.TryGetValue(packGenes, out CompositionGroup group))
                {
                    group = new CompositionGroup(packGenes, GenepackCompositionUtility.CreateCompositionKey(packGenes));

                    groupsByGenes.Add(group.Genes, group);
                    compositionGroups.Add(group);
                }

                group.AddSource(source);
            }

            foreach (CompositionGroup group in compositionGroups)
                group.SortSources(getPhysicalKey);

            compositionGroups.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.CompositionKey, right.CompositionKey));

            return compositionGroups;
        }

        private static Dictionary<GeneDef, List<CompositionGroup>> CreateGroupsByGene(
            IEnumerable<CompositionGroup> compositionGroups)
        {
            var groupsByGene = new Dictionary<GeneDef, List<CompositionGroup>>();

            foreach (CompositionGroup group in compositionGroups)
            {
                foreach (GeneDef gene in group.Genes)
                {
                    if (!groupsByGene.TryGetValue(gene, out List<CompositionGroup> groups))
                    {
                        groups = new List<CompositionGroup>();
                        groupsByGene.Add(gene, groups);
                    }

                    groups.Add(group);
                }
            }

            foreach (List<CompositionGroup> groups in groupsByGene.Values)
            {
                groups.Sort((left, right) => StringComparer.Ordinal.Compare(left.CompositionKey, right.CompositionKey));
            }

            return groupsByGene;
        }

        private static GeneDef SelectNextRequirement(
            IEnumerable<GeneDef> requirements,
            IReadOnlyDictionary<GeneDef, List<CompositionGroup>> groupsByGene,
            ISet<CompositionGroup> selectedGroups)
        {
            GeneDef selectedRequirement = null;
            int selectedSourceCount = int.MaxValue;
            var distinctRequirements = new HashSet<GeneDef>();

            foreach (GeneDef requirement in requirements)
            {
                if (requirement == null || !distinctRequirements.Add(requirement))
                    continue;

                var sourceCount = 0;

                if (groupsByGene.TryGetValue(requirement, out List<CompositionGroup> groups))
                {
                    foreach (CompositionGroup group in groups)
                    {
                        if (!selectedGroups.Contains(group))
                            sourceCount++;
                    }
                }

                if (sourceCount < selectedSourceCount ||
                    (sourceCount == selectedSourceCount && CompareGenes(requirement, selectedRequirement) < 0))
                {
                    selectedRequirement = requirement;
                    selectedSourceCount = sourceCount;
                }
            }

            return selectedSourceCount == 0 ? null : selectedRequirement;
        }

        private static List<GeneDef> GetMissingGenes(IEnumerable<GeneDef> requiredGenes, ISet<GeneDef> selectedGenes)
        {
            var missing = new List<GeneDef>();

            foreach (GeneDef requiredGene in requiredGenes)
            {
                if (!selectedGenes.Contains(requiredGene))
                    missing.Add(requiredGene);
            }

            missing.Sort(CompareGenes);
            return missing;
        }

        private static HashSet<GeneDef> CreateGeneUnion(IEnumerable<CompositionGroup> groups)
        {
            var genes = new HashSet<GeneDef>();

            foreach (CompositionGroup group in groups)
                genes.UnionWith(group.Genes);

            return genes;
        }

        private static HashSet<GeneDef> CreateGeneUnionExcept(IReadOnlyList<CompositionGroup> groups, int excludedIndex)
        {
            var genes = new HashSet<GeneDef>();

            for (var index = 0; index < groups.Count; index++)
            {
                if (index != excludedIndex)
                    genes.UnionWith(groups[index].Genes);
            }

            return genes;
        }

        private static HashSet<string> CreatePrerequisiteKeys(IEnumerable<PlanGenePrerequisiteDiagnostic> prerequisites)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);

            foreach (PlanGenePrerequisiteDiagnostic prerequisite in prerequisites)
            {
                keys.Add(
                    (prerequisite.DependentGene.defName ?? string.Empty) +
                    "\u001f" +
                    (prerequisite.PrerequisiteGene.defName ?? string.Empty));
            }

            return keys;
        }

        private static void RebuildSelectedGenes(
            IEnumerable<CompositionGroup> selectedGroups,
            ISet<GeneDef> selectedGenes)
        {
            selectedGenes.Clear();

            foreach (CompositionGroup group in selectedGroups)
                selectedGenes.UnionWith(group.Genes);
        }

        private static int CompareSources(
            PlanAssemblerGenepackSource left,
            PlanAssemblerGenepackSource right,
            Func<Genepack, string> getPhysicalKey)
        {
            int powerComparison = right.IsFacilityPowered.CompareTo(left.IsFacilityPowered);

            if (powerComparison != 0)
                return powerComparison;

            string leftKey = getPhysicalKey(left.Genepack) ?? string.Empty;
            string rightKey = getPhysicalKey(right.Genepack) ?? string.Empty;

            return StringComparer.Ordinal.Compare(leftKey, rightKey);
        }

        private static int CompareGenes(GeneDef left, GeneDef right)
        {
            if (right == null)
                return -1;

            return StringComparer.Ordinal.Compare(left.defName ?? string.Empty, right.defName ?? string.Empty);
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
            private readonly List<PlanAssemblerGenepackSource> _sources;

            internal CompositionGroup(HashSet<GeneDef> genes, string compositionKey)
            {
                Genes = new HashSet<GeneDef>(genes);
                CompositionKey = compositionKey;
                _sources = new List<PlanAssemblerGenepackSource>();
            }

            internal HashSet<GeneDef> Genes { get; }
            internal string CompositionKey { get; }

            internal PlanAssemblerGenepackSource RepresentativeSource => _sources[0];

            internal void AddSource(PlanAssemblerGenepackSource source)
            {
                _sources.Add(source);
            }

            internal void SortSources(Func<Genepack, string> getPhysicalKey)
            {
                _sources.Sort((left, right) => CompareSources(left, right, getPhysicalKey));
            }
        }
    }
}