using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Verse;

namespace XenogermPlanner.UI
{
    internal enum XenogermPlanSourceFailure
    {
        None,
        SourceUnavailable,
        InvalidSourceData,
        EmptySource,
        LoadFailed
    }

    internal sealed class XenogermPlanSourceSelection
    {
        private readonly ReadOnlyCollection<GeneDef> _desiredGenes;

        internal XenogermPlanSourceSelection(string name, IEnumerable<GeneDef> desiredGenes)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (desiredGenes == null)
                throw new ArgumentNullException(nameof(desiredGenes));

            var distinctGenes = new HashSet<GeneDef>();

            foreach (GeneDef gene in desiredGenes)
            {
                if (gene == null)
                {
                    throw new ArgumentException(
                        "Plan source gene collection cannot contain null values.",
                        nameof(desiredGenes));
                }

                distinctGenes.Add(gene);
            }

            if (distinctGenes.Count == 0)
                throw new ArgumentException("Plan source gene collection cannot be empty.", nameof(desiredGenes));

            Name = name;
            _desiredGenes = new List<GeneDef>(distinctGenes).AsReadOnly();
        }

        internal string Name { get; }
        internal IReadOnlyCollection<GeneDef> DesiredGenes => _desiredGenes;
    }

    internal sealed class XenogermPlanSourceResolveResult
    {
        private XenogermPlanSourceResolveResult(
            XenogermPlanSourceSelection selection,
            XenogermPlanSourceFailure failure)
        {
            Selection = selection;
            Failure = failure;
        }

        internal XenogermPlanSourceSelection Selection { get; }
        internal XenogermPlanSourceFailure Failure { get; }

        internal bool IsSuccess => Failure == XenogermPlanSourceFailure.None && Selection != null;

        internal static XenogermPlanSourceResolveResult Success(XenogermPlanSourceSelection selection)
        {
            if (selection == null)
                throw new ArgumentNullException(nameof(selection));

            return new XenogermPlanSourceResolveResult(selection, XenogermPlanSourceFailure.None);
        }

        internal static XenogermPlanSourceResolveResult Failed(XenogermPlanSourceFailure failure)
        {
            if (failure == XenogermPlanSourceFailure.None)
                throw new ArgumentOutOfRangeException(nameof(failure));

            return new XenogermPlanSourceResolveResult(null, failure);
        }
    }

    internal sealed class XenogermPlanSourceEntry
    {
        internal XenogermPlanSourceEntry(
            object stableKey,
            string displayName,
            string metadataKey,
            object sourceToken,
            XenogermPlanSourceResolveResult initialResult = null,
            params NamedArgument[] metadataArguments)
        {
            StableKey = stableKey ?? throw new ArgumentNullException(nameof(stableKey));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            MetadataKey = metadataKey ?? throw new ArgumentNullException(nameof(metadataKey));
            MetadataArguments = new ReadOnlyCollection<NamedArgument>(
                metadataArguments ?? Array.Empty<NamedArgument>());
            SourceToken = sourceToken ?? throw new ArgumentNullException(nameof(sourceToken));
            InitialResult = initialResult;
        }

        internal object StableKey { get; }
        internal string DisplayName { get; }
        internal string MetadataKey { get; }
        internal IReadOnlyList<NamedArgument> MetadataArguments { get; }
        internal object SourceToken { get; }
        internal XenogermPlanSourceResolveResult InitialResult { get; }

        internal bool IsKnownInvalid => InitialResult is { IsSuccess: false };
    }

    internal sealed class XenogermPlanSourceGroup
    {
        private readonly ReadOnlyCollection<XenogermPlanSourceEntry> _sources;

        internal XenogermPlanSourceGroup(
            string key,
            string labelKey,
            bool isCollapsible,
            IEnumerable<XenogermPlanSourceEntry> sources)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Source group key cannot be null, empty or whitespace.", nameof(key));

            if (isCollapsible && string.IsNullOrWhiteSpace(labelKey))
            {
                throw new ArgumentException("Collapsible source groups require a localization key.", nameof(labelKey));
            }

            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            var sourceList = new List<XenogermPlanSourceEntry>();

            foreach (XenogermPlanSourceEntry source in sources)
            {
                if (source == null)
                    throw new ArgumentException("Source group cannot contain null entries.", nameof(sources));

                sourceList.Add(source);
            }

            Key = key;
            LabelKey = labelKey;
            IsCollapsible = isCollapsible;
            _sources = sourceList.AsReadOnly();
        }

        internal string Key { get; }
        internal string LabelKey { get; }
        internal bool IsCollapsible { get; }
        internal IReadOnlyList<XenogermPlanSourceEntry> Sources => _sources;
    }

    internal interface IXenogermPlanSourceProvider
    {
        string TitleKey { get; }
        string EmptySourcesKey { get; }
        IReadOnlyList<XenogermPlanSourceGroup> Groups { get; }

        void Refresh();

        void Resolve(
            XenogermPlanSourceEntry source,
            bool revalidate,
            Action<XenogermPlanSourceResolveResult> onResolved);
    }
}