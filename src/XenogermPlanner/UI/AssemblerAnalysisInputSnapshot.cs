using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using XenogermPlanner.Assemblers;
using XenogermPlanner.Genes;

namespace XenogermPlanner.UI
{
    internal sealed class AssemblerAnalysisInputSnapshot
    {
        private readonly int _availableArchiteCapsules;
        private readonly bool _isArchogeneticsFinished;

        private readonly bool _isAssemblerPowered;
        private readonly int _maxComplexity;
        private readonly Dictionary<Genepack, SourceSnapshot> _sources;

        internal AssemblerAnalysisInputSnapshot(
            PlanAssemblerLiveState state,
            Func<Genepack, IReadOnlyList<GeneDef>> getGenes)
        {
            _isAssemblerPowered = state.IsAssemblerPowered;
            _maxComplexity = state.MaxComplexity;
            _availableArchiteCapsules = state.AvailableArchiteCapsules;
            _isArchogeneticsFinished = state.IsArchogeneticsFinished;
            _sources = new Dictionary<Genepack, SourceSnapshot>(ReferenceEqualityComparer<Genepack>.Instance);

            foreach (PlanAssemblerGenepackSource source in state.Scope.Sources)
                _sources.Add(source.Genepack, new SourceSnapshot(source, getGenes(source.Genepack)));
        }

        internal bool Matches(PlanAssemblerLiveState state, Func<Genepack, IReadOnlyList<GeneDef>> getGenes)
        {
            if (_isAssemblerPowered != state.IsAssemblerPowered ||
                _maxComplexity != state.MaxComplexity ||
                _availableArchiteCapsules != state.AvailableArchiteCapsules ||
                _isArchogeneticsFinished != state.IsArchogeneticsFinished ||
                _sources.Count != state.Scope.Sources.Count)
            {
                return false;
            }

            foreach (PlanAssemblerGenepackSource source in state.Scope.Sources)
            {
                if (!_sources.TryGetValue(source.Genepack, out SourceSnapshot previous) ||
                    !previous.Matches(source, getGenes(source.Genepack)))
                {
                    return false;
                }
            }

            return true;
        }

        private sealed class SourceSnapshot
        {
            internal SourceSnapshot(PlanAssemblerGenepackSource source, IReadOnlyList<GeneDef> genes)
            {
                Facility = source.Facility;
                IsPowered = source.IsFacilityPowered;
                if (genes == null)
                    return;

                Genes = new GeneDef[genes.Count];
                for (var index = 0; index < genes.Count; index++)
                    Genes[index] = genes[index];
            }

            internal Thing Facility { get; }
            internal bool IsPowered { get; }
            internal GeneDef[] Genes { get; }

            internal bool Matches(PlanAssemblerGenepackSource source, IReadOnlyList<GeneDef> genes)
            {
                if (!ReferenceEquals(Facility, source.Facility) || IsPowered != source.IsFacilityPowered)
                    return false;

                if (Genes == null || genes == null)
                    return Genes == null && genes == null;

                if (Genes.Length != genes.Count)
                    return false;

                for (var index = 0; index < Genes.Length; index++)
                {
                    if (!ReferenceEquals(Genes[index], genes[index]))
                        return false;
                }

                return true;
            }
        }
    }
}