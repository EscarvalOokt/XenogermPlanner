using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RimWorld;
using Verse;
using XenogermPlanner.Plans;

namespace XenogermPlanner.UI
{
    internal sealed class CustomXenogermPlanSourceProvider : IXenogermPlanSourceProvider
    {
        private const string GroupKey = "xenogerm-templates";

        private readonly Func<IReadOnlyList<CustomXenogerm>> _getRuntimeSources;
        private ReadOnlyCollection<XenogermPlanSourceGroup> _groups;

        internal CustomXenogermPlanSourceProvider() : this(GetRuntimeSources)
        {
        }

        internal CustomXenogermPlanSourceProvider(Func<IReadOnlyList<CustomXenogerm>> getRuntimeSources)
        {
            _getRuntimeSources = getRuntimeSources ?? throw new ArgumentNullException(nameof(getRuntimeSources));
            _groups = new List<XenogermPlanSourceGroup>().AsReadOnly();
            Refresh();
        }

        public string TitleKey => "XenogermPlanner.PlanSource.XenogermTemplate.Title";

        public string EmptySourcesKey => "XenogermPlanner.PlanSource.XenogermTemplate.EmptySources";

        public IReadOnlyList<XenogermPlanSourceGroup> Groups => _groups;

        public void Refresh()
        {
            var entries = new List<XenogermPlanSourceEntry>();
            IReadOnlyList<CustomXenogerm> runtimeSources = null;

            try
            {
                runtimeSources = _getRuntimeSources();
            }
            catch (Exception exception)
            {
                Log.Warning($"[Xenogerm Planner] Failed to enumerate xenogerm template sources: {exception}");
            }

            if (runtimeSources != null)
            {
                for (var index = 0; index < runtimeSources.Count; index++)
                {
                    CustomXenogerm source = runtimeSources[index];
                    var token = new SourceToken(source, index);

                    XenogermPlanSourceResolveResult initialResult = ReadSource(source);
                    string displayName = GetSourceDisplayName(source);
                    string metadataKey = initialResult.IsSuccess
                        ? "XenogermPlanner.PlanSource.GeneCount"
                        : "XenogermPlanner.PlanSource.CannotUse";

                    XenogermPlanSourceEntry entry = initialResult.IsSuccess
                        ? new XenogermPlanSourceEntry(
                            (object)source ?? token,
                            displayName,
                            metadataKey,
                            token,
                            initialResult,
                            initialResult.Selection.DesiredGenes.Count)
                        : new XenogermPlanSourceEntry(
                            (object)source ?? token,
                            displayName,
                            metadataKey,
                            token,
                            initialResult);

                    entries.Add(entry);
                }
            }

            entries.Sort(CompareEntries);

            _groups = new List<XenogermPlanSourceGroup>
            {
                new XenogermPlanSourceGroup(GroupKey, null, false, entries)
            }.AsReadOnly();
        }

        public void Resolve(
            XenogermPlanSourceEntry source,
            bool revalidate,
            Action<XenogermPlanSourceResolveResult> onResolved)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (onResolved == null)
                throw new ArgumentNullException(nameof(onResolved));

            if (!revalidate && source.InitialResult != null)
            {
                onResolved(source.InitialResult);
                return;
            }

            if (!(source.SourceToken is SourceToken token) || token.Source == null)
            {
                onResolved(XenogermPlanSourceResolveResult.Failed(XenogermPlanSourceFailure.SourceUnavailable));
                return;
            }

            IReadOnlyList<CustomXenogerm> runtimeSources;

            try
            {
                runtimeSources = _getRuntimeSources();
            }
            catch (Exception exception)
            {
                Log.Warning($"[Xenogerm Planner] Failed to revalidate xenogerm template source: {exception}");
                onResolved(XenogermPlanSourceResolveResult.Failed(XenogermPlanSourceFailure.SourceUnavailable));
                return;
            }

            if (runtimeSources == null || !ContainsSourceReference(runtimeSources, token.Source))
            {
                onResolved(XenogermPlanSourceResolveResult.Failed(XenogermPlanSourceFailure.SourceUnavailable));
                return;
            }

            onResolved(ReadSource(token.Source));
        }

        private static XenogermPlanSourceResolveResult ReadSource(CustomXenogerm source)
        {
            if (!CustomXenogermPlanImporter.TryReadSource(
                    source,
                    out CustomXenogermPlanImportData importData,
                    out CustomXenogermPlanImportFailure failure))
            {
                return XenogermPlanSourceResolveResult.Failed(ConvertFailure(failure));
            }

            if (importData == null)
                return XenogermPlanSourceResolveResult.Failed(XenogermPlanSourceFailure.InvalidSourceData);

            try
            {
                return XenogermPlanSourceResolveResult.Success(
                    new XenogermPlanSourceSelection(importData.Name, importData.DesiredGenes));
            }
            catch (ArgumentException)
            {
                return XenogermPlanSourceResolveResult.Failed(XenogermPlanSourceFailure.InvalidSourceData);
            }
        }

        private static XenogermPlanSourceFailure ConvertFailure(CustomXenogermPlanImportFailure failure)
        {
            switch (failure)
            {
                case CustomXenogermPlanImportFailure.SourceUnavailable:
                    return XenogermPlanSourceFailure.SourceUnavailable;

                case CustomXenogermPlanImportFailure.EmptySource:
                    return XenogermPlanSourceFailure.EmptySource;

                case CustomXenogermPlanImportFailure.InvalidSourceData:
                default:
                    return XenogermPlanSourceFailure.InvalidSourceData;
            }
        }

        private static bool ContainsSourceReference(IReadOnlyList<CustomXenogerm> sources, CustomXenogerm source)
        {
            foreach (CustomXenogerm current in sources)
            {
                if (ReferenceEquals(current, source))
                    return true;
            }

            return false;
        }

        private static int CompareEntries(XenogermPlanSourceEntry left, XenogermPlanSourceEntry right)
        {
            int nameComparison = StringComparer.CurrentCultureIgnoreCase.Compare(left.DisplayName, right.DisplayName);

            if (nameComparison != 0)
                return nameComparison;

            var leftToken = (SourceToken)left.SourceToken;
            var rightToken = (SourceToken)right.SourceToken;

            return leftToken.SourceIndex.CompareTo(rightToken.SourceIndex);
        }

        private static string GetSourceDisplayName(CustomXenogerm source)
        {
            if (source == null || string.IsNullOrWhiteSpace(source.name))
            {
                return "XenogermPlanner.PlanSource.XenogermTemplate.UnnamedSource".Translate().ToString();
            }

            return source.name;
        }

        private static IReadOnlyList<CustomXenogerm> GetRuntimeSources()
        {
            return Current.Game?.customXenogermDatabase?.CustomXenogermsForReading;
        }

        private sealed class SourceToken
        {
            internal SourceToken(CustomXenogerm source, int sourceIndex)
            {
                Source = source;
                SourceIndex = sourceIndex;
            }

            internal CustomXenogerm Source { get; }
            internal int SourceIndex { get; }
        }
    }
}