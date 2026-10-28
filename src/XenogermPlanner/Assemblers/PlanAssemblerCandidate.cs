using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RimWorld;
using XenogermPlanner.Analysis;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Assemblers
{
    internal sealed class PlanAssemblerCandidate
    {
        private readonly ReadOnlyCollection<PlanGenePrerequisiteDiagnostic> _missingPrerequisites;
        private readonly ReadOnlyCollection<PlanAssemblerGenepackSource> _sources;

        internal PlanAssemblerCandidate(
            IEnumerable<PlanAssemblerGenepackSource> sources,
            IEnumerable<PlanGenePrerequisiteDiagnostic> missingPrerequisites)
        {
            _sources = CopySources(sources).AsReadOnly();
            _missingPrerequisites = CopyPrerequisites(missingPrerequisites).AsReadOnly();

            if (_sources.Count == 0)
            {
                throw new ArgumentException(
                    "Assembler candidate requires at least one physical source.",
                    nameof(sources));
            }
        }

        internal IReadOnlyList<PlanAssemblerGenepackSource> Sources => _sources;
        internal IReadOnlyList<PlanGenePrerequisiteDiagnostic> MissingPrerequisites => _missingPrerequisites;
        internal bool IsPrerequisiteComplete => _missingPrerequisites.Count == 0;

        private static List<PlanAssemblerGenepackSource> CopySources(IEnumerable<PlanAssemblerGenepackSource> sources)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            var copied = new List<PlanAssemblerGenepackSource>();
            var genepacks = new HashSet<Genepack>(ReferenceEqualityComparer<Genepack>.Instance);

            foreach (PlanAssemblerGenepackSource source in sources)
            {
                if (source == null)
                {
                    throw new ArgumentException(
                        "Assembler candidate source collection cannot contain null values.",
                        nameof(sources));
                }

                if (genepacks.Add(source.Genepack))
                    copied.Add(source);
            }

            return copied;
        }

        private static List<PlanGenePrerequisiteDiagnostic> CopyPrerequisites(
            IEnumerable<PlanGenePrerequisiteDiagnostic> prerequisites)
        {
            if (prerequisites == null)
                throw new ArgumentNullException(nameof(prerequisites));

            var copied = new List<PlanGenePrerequisiteDiagnostic>();
            var keys = new HashSet<string>(StringComparer.Ordinal);

            foreach (PlanGenePrerequisiteDiagnostic prerequisite in prerequisites)
            {
                if (prerequisite == null)
                {
                    throw new ArgumentException(
                        "Missing prerequisite collection cannot contain null values.",
                        nameof(prerequisites));
                }

                string key = (prerequisite.DependentGene.defName ?? string.Empty) +
                             "\u001f" +
                             (prerequisite.PrerequisiteGene.defName ?? string.Empty);

                if (keys.Add(key))
                    copied.Add(prerequisite);
            }

            return copied;
        }
    }
}