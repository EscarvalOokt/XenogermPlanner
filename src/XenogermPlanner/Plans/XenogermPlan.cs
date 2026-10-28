using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Verse;

namespace XenogermPlanner.Plans
{
    public sealed class XenogermPlan
    {
        private readonly HashSet<GeneDef> _desiredGenes;
        private readonly HashSet<string> _unresolvedDesiredGeneDefNames;
        private ReadOnlyCollection<GeneDef> _desiredGenesForReading;
        private bool _hasReadinessNotificationBaseline;
        private bool _lastReadinessNotificationStateWasReady;
        private ReadOnlyCollection<string> _unresolvedDesiredGeneDefNamesForReading;

        public XenogermPlan(
            string name,
            IEnumerable<GeneDef> desiredGenes,
            PlanReadinessMode readinessMode,
            bool readinessNotificationsEnabled = true) : this(
            CreateId(),
            name,
            desiredGenes,
            Array.Empty<string>(),
            readinessMode,
            readinessNotificationsEnabled)
        {
        }

        internal XenogermPlan(
            string id,
            string name,
            IEnumerable<GeneDef> desiredGenes,
            IEnumerable<string> unresolvedDesiredGeneDefNames,
            PlanReadinessMode readinessMode,
            bool readinessNotificationsEnabled = true,
            bool hasReadinessNotificationBaseline = false,
            bool lastReadinessNotificationStateWasReady = false)
        {
            ValidateId(id);
            string normalizedName = NormalizeName(name);
            ValidateReadinessMode(readinessMode);

            Id = id;
            Name = normalizedName;
            _desiredGenes = NormalizeDesiredGenes(desiredGenes);
            _unresolvedDesiredGeneDefNames = NormalizeUnresolvedDesiredGeneDefNames(unresolvedDesiredGeneDefNames);
            ReadinessMode = readinessMode;
            ReadinessNotificationsEnabled = readinessNotificationsEnabled;
            _hasReadinessNotificationBaseline = hasReadinessNotificationBaseline;
            _lastReadinessNotificationStateWasReady =
                hasReadinessNotificationBaseline && lastReadinessNotificationStateWasReady;

            RemoveResolvedGeneDefNamesFromUnresolved();

            RefreshDesiredGenesForReading();
            RefreshUnresolvedDesiredGeneDefNamesForReading();
        }

        public string Id { get; }
        public string Name { get; private set; }
        public IReadOnlyCollection<GeneDef> DesiredGenes => _desiredGenesForReading;

        public IReadOnlyCollection<string> UnresolvedDesiredGeneDefNames => _unresolvedDesiredGeneDefNamesForReading;

        public bool IsDegraded => _unresolvedDesiredGeneDefNames.Count > 0;
        public PlanReadinessMode ReadinessMode { get; private set; }
        public bool ReadinessNotificationsEnabled { get; private set; }

        internal bool HasReadinessNotificationBaseline => _hasReadinessNotificationBaseline;
        internal bool LastReadinessNotificationStateWasReady => _lastReadinessNotificationStateWasReady;

        public XenogermPlan CreateDuplicate()
        {
            return CreateIndependent(
                Name,
                DesiredGenes,
                UnresolvedDesiredGeneDefNames,
                ReadinessMode,
                ReadinessNotificationsEnabled);
        }

        internal static XenogermPlan CreateIndependent(
            string name,
            IEnumerable<GeneDef> desiredGenes,
            IEnumerable<string> unresolvedDesiredGeneDefNames,
            PlanReadinessMode readinessMode,
            bool readinessNotificationsEnabled = true)
        {
            return new XenogermPlan(
                CreateId(),
                name,
                desiredGenes,
                unresolvedDesiredGeneDefNames,
                readinessMode,
                readinessNotificationsEnabled);
        }

        public void Rename(string name)
        {
            Name = NormalizeName(name);
        }

        public void ReplaceDesiredGenes(IEnumerable<GeneDef> genes)
        {
            HashSet<GeneDef> normalizedGenes = NormalizeDesiredGenes(genes);

            _desiredGenes.Clear();
            _desiredGenes.UnionWith(normalizedGenes);
            _unresolvedDesiredGeneDefNames.Clear();

            RefreshDesiredGenesForReading();
            RefreshUnresolvedDesiredGeneDefNamesForReading();
        }

        public bool AddDesiredGene(GeneDef gene)
        {
            ValidateGene(gene);

            bool added = _desiredGenes.Add(gene);
            bool resolvedUnresolvedRequirement =
                gene.defName != null && _unresolvedDesiredGeneDefNames.Remove(gene.defName);

            if (added)
                RefreshDesiredGenesForReading();

            if (resolvedUnresolvedRequirement)
                RefreshUnresolvedDesiredGeneDefNamesForReading();

            return added;
        }

        public bool RemoveDesiredGene(GeneDef gene)
        {
            ValidateGene(gene);

            bool removed = _desiredGenes.Remove(gene);
            if (removed)
                RefreshDesiredGenesForReading();

            return removed;
        }

        public bool ContainsDesiredGene(GeneDef gene)
        {
            ValidateGene(gene);
            return _desiredGenes.Contains(gene);
        }

        public void ChangeReadinessMode(PlanReadinessMode readinessMode)
        {
            ValidateReadinessMode(readinessMode);
            ReadinessMode = readinessMode;
        }

        public void ChangeReadinessNotificationsEnabled(bool enabled)
        {
            ReadinessNotificationsEnabled = enabled;
        }

        internal void UpdateReadinessNotificationState(bool isReady)
        {
            _hasReadinessNotificationBaseline = true;
            _lastReadinessNotificationStateWasReady = isReady;
        }

        private static string CreateId()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static HashSet<GeneDef> NormalizeDesiredGenes(IEnumerable<GeneDef> genes)
        {
            if (genes == null)
                throw new ArgumentNullException(nameof(genes));

            var normalizedGenes = new HashSet<GeneDef>();

            foreach (GeneDef gene in genes)
            {
                ValidateGene(gene);
                normalizedGenes.Add(gene);
            }

            return normalizedGenes;
        }

        private static HashSet<string> NormalizeUnresolvedDesiredGeneDefNames(IEnumerable<string> geneDefNames)
        {
            if (geneDefNames == null)
                throw new ArgumentNullException(nameof(geneDefNames));

            var normalizedGeneDefNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (string geneDefName in geneDefNames)
            {
                ValidateGeneDefName(geneDefName);
                normalizedGeneDefNames.Add(geneDefName);
            }

            return normalizedGeneDefNames;
        }

        private static void ValidateId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Plan ID cannot be null, empty or whitespace.", nameof(id));
        }

        private static string NormalizeName(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (!XenogermPlanNameAllocator.TryNormalize(name, out string normalizedName))
            {
                throw new ArgumentException("Plan name cannot be empty or whitespace.", nameof(name));
            }

            return normalizedName;
        }

        private static void ValidateGene(GeneDef gene)
        {
            if (gene == null)
                throw new ArgumentNullException(nameof(gene));
        }

        private static void ValidateGeneDefName(string geneDefName)
        {
            if (string.IsNullOrWhiteSpace(geneDefName))
                throw new ArgumentException("Gene def name cannot be null, empty or whitespace.", nameof(geneDefName));
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

        private void RemoveResolvedGeneDefNamesFromUnresolved()
        {
            foreach (GeneDef gene in _desiredGenes)
            {
                if (gene.defName != null)
                    _unresolvedDesiredGeneDefNames.Remove(gene.defName);
            }
        }

        private void RefreshDesiredGenesForReading()
        {
            _desiredGenesForReading = new List<GeneDef>(_desiredGenes).AsReadOnly();
        }

        private void RefreshUnresolvedDesiredGeneDefNamesForReading()
        {
            _unresolvedDesiredGeneDefNamesForReading = new List<string>(_unresolvedDesiredGeneDefNames).AsReadOnly();
        }
    }
}