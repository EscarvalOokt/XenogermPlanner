using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Verse;

namespace XenogermPlanner.Analysis
{
    public sealed class PlanGeneTargetAnalysisResult
    {
        private readonly ReadOnlyCollection<PlanGeneConflictDiagnostic> _conflicts;
        private readonly ReadOnlyCollection<PlanGenePrerequisiteDiagnostic> _missingPrerequisites;
        private readonly ReadOnlyCollection<PlanGeneRandomChoiceGroupDiagnostic> _randomChoiceGroups;

        internal PlanGeneTargetAnalysisResult(
            IEnumerable<PlanGeneConflictDiagnostic> conflicts,
            IEnumerable<PlanGeneRandomChoiceGroupDiagnostic> randomChoiceGroups,
            IEnumerable<PlanGenePrerequisiteDiagnostic> missingPrerequisites)
        {
            _conflicts = CopyConflicts(conflicts).AsReadOnly();
            _randomChoiceGroups = CopyRandomChoiceGroups(randomChoiceGroups).AsReadOnly();
            _missingPrerequisites = CopyPrerequisites(missingPrerequisites).AsReadOnly();
        }

        public IReadOnlyList<PlanGeneConflictDiagnostic> Conflicts => _conflicts;
        public IReadOnlyList<PlanGeneRandomChoiceGroupDiagnostic> RandomChoiceGroups => _randomChoiceGroups;
        public IReadOnlyList<PlanGenePrerequisiteDiagnostic> MissingPrerequisites => _missingPrerequisites;
        public bool HasConflicts => _conflicts.Count > 0 || _randomChoiceGroups.Count > 0;
        public bool HasMissingPrerequisites => _missingPrerequisites.Count > 0;
        public bool HasDiagnostics => HasConflicts || HasMissingPrerequisites;
        public int DiagnosticCount => _conflicts.Count + _randomChoiceGroups.Count + _missingPrerequisites.Count;

        private static List<PlanGeneConflictDiagnostic> CopyConflicts(IEnumerable<PlanGeneConflictDiagnostic> conflicts)
        {
            if (conflicts == null)
                throw new ArgumentNullException(nameof(conflicts));

            var copied = new List<PlanGeneConflictDiagnostic>();
            var pairs = new HashSet<string>(StringComparer.Ordinal);

            foreach (PlanGeneConflictDiagnostic diagnostic in conflicts)
            {
                if (diagnostic == null)
                {
                    throw new ArgumentException(
                        "Conflict diagnostic collection cannot contain null values.",
                        nameof(conflicts));
                }

                string key = CreatePairKey(diagnostic.FirstGene.defName, diagnostic.SecondGene.defName);

                if (!pairs.Add(key))
                {
                    throw new ArgumentException(
                        "Conflict diagnostic collection cannot contain duplicate gene pairs.",
                        nameof(conflicts));
                }

                copied.Add(diagnostic);
            }

            return copied;
        }

        private static List<PlanGeneRandomChoiceGroupDiagnostic> CopyRandomChoiceGroups(
            IEnumerable<PlanGeneRandomChoiceGroupDiagnostic> groups)
        {
            if (groups == null)
                throw new ArgumentNullException(nameof(groups));

            var copied = new List<PlanGeneRandomChoiceGroupDiagnostic>();
            var groupKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (PlanGeneRandomChoiceGroupDiagnostic diagnostic in groups)
            {
                if (diagnostic == null)
                {
                    throw new ArgumentException(
                        "Random-choice group collection cannot contain null values.",
                        nameof(groups));
                }

                string key = CreateGroupKey(diagnostic);

                if (!groupKeys.Add(key))
                {
                    throw new ArgumentException(
                        "Random-choice group collection cannot contain duplicate gene groups.",
                        nameof(groups));
                }

                copied.Add(diagnostic);
            }

            return copied;
        }

        private static List<PlanGenePrerequisiteDiagnostic> CopyPrerequisites(
            IEnumerable<PlanGenePrerequisiteDiagnostic> prerequisites)
        {
            if (prerequisites == null)
                throw new ArgumentNullException(nameof(prerequisites));

            var copied = new List<PlanGenePrerequisiteDiagnostic>();
            var pairs = new HashSet<string>(StringComparer.Ordinal);

            foreach (PlanGenePrerequisiteDiagnostic diagnostic in prerequisites)
            {
                if (diagnostic == null)
                {
                    throw new ArgumentException(
                        "Prerequisite diagnostic collection cannot contain null values.",
                        nameof(prerequisites));
                }

                string key = (diagnostic.DependentGene.defName ?? string.Empty) +
                             "\u001f" +
                             (diagnostic.PrerequisiteGene.defName ?? string.Empty);

                if (!pairs.Add(key))
                {
                    throw new ArgumentException(
                        "Prerequisite diagnostic collection cannot contain duplicate gene pairs.",
                        nameof(prerequisites));
                }

                copied.Add(diagnostic);
            }

            return copied;
        }

        private static string CreatePairKey(string firstDefName, string secondDefName)
        {
            string first = firstDefName ?? string.Empty;
            string second = secondDefName ?? string.Empty;

            return StringComparer.Ordinal.Compare(first, second) <= 0
                ? first + "\u001f" + second
                : second + "\u001f" + first;
        }

        private static string CreateGroupKey(PlanGeneRandomChoiceGroupDiagnostic diagnostic)
        {
            var builder = new StringBuilder();

            foreach (GeneDef gene in diagnostic.Genes)
            {
                if (builder.Length > 0)
                    builder.Append('\u001f');

                builder.Append(gene.defName ?? string.Empty);
            }

            return builder.ToString();
        }
    }
}