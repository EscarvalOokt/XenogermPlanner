using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RimWorld;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Assemblers
{
    internal sealed class PlanAssemblerScopeSnapshot
    {
        private readonly ReadOnlyCollection<PlanAssemblerGenepackSource> _sources;
        private readonly Dictionary<Genepack, PlanAssemblerGenepackSource> _sourcesByGenepack;
        private readonly ReadOnlyCollection<Genepack> _visibleGenepacks;

        internal PlanAssemblerScopeSnapshot(IEnumerable<PlanAssemblerGenepackSource> sources)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            var copiedSources = new List<PlanAssemblerGenepackSource>();
            var copiedGenepacks = new List<Genepack>();
            _sourcesByGenepack =
                new Dictionary<Genepack, PlanAssemblerGenepackSource>(ReferenceEqualityComparer<Genepack>.Instance);

            foreach (PlanAssemblerGenepackSource source in sources)
            {
                if (source == null)
                {
                    throw new ArgumentException(
                        "Assembler genepack source collection cannot contain null values.",
                        nameof(sources));
                }

                if (!_sourcesByGenepack.TryAdd(source.Genepack, source))
                {
                    throw new ArgumentException(
                        "Assembler genepack source collection cannot contain duplicate physical genepack references.",
                        nameof(sources));
                }

                copiedSources.Add(source);
                copiedGenepacks.Add(source.Genepack);
            }

            _sources = copiedSources.AsReadOnly();
            _visibleGenepacks = copiedGenepacks.AsReadOnly();
        }

        internal IReadOnlyList<PlanAssemblerGenepackSource> Sources => _sources;
        internal IReadOnlyList<Genepack> VisibleGenepacks => _visibleGenepacks;

        internal bool TryGetSource(Genepack genepack, out PlanAssemblerGenepackSource source)
        {
            if (genepack == null)
                throw new ArgumentNullException(nameof(genepack));

            return _sourcesByGenepack.TryGetValue(genepack, out source);
        }
    }
}