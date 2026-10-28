using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using RimWorld;
using Verse;
using XenogermPlanner.Plans;

namespace XenogermPlanner.UI
{
    internal sealed class XenotypePlanSourceProvider : IXenogermPlanSourceProvider
    {
        private const string PremadeGroupKey = "premade-xenotypes";
        private const string SavedGroupKey = "saved-xenotypes";
        private readonly Action<string, Action> _checkVersionAndLoad;

        private readonly Func<IEnumerable<XenotypeDef>> _getPremadeSources;
        private readonly Func<IEnumerable<FileInfo>> _getSavedSources;

        private readonly Dictionary<string, SavedSourceCacheEntry> _savedSourceCache =
            new Dictionary<string, SavedSourceCacheEntry>(StringComparer.OrdinalIgnoreCase);

        private readonly TryLoadCustomXenotype _tryLoadCustomXenotype;

        private ReadOnlyCollection<XenogermPlanSourceGroup> _groups;

        internal XenotypePlanSourceProvider() : this(
            () => DefDatabase<XenotypeDef>.AllDefs,
            () => GenFilePaths.AllCustomXenotypeFiles,
            CheckVersionAndLoad,
            TryLoadCustomXenotypeFile)
        {
        }

        internal XenotypePlanSourceProvider(
            Func<IEnumerable<XenotypeDef>> getPremadeSources,
            Func<IEnumerable<FileInfo>> getSavedSources,
            Action<string, Action> checkVersionAndLoad,
            TryLoadCustomXenotype tryLoadCustomXenotype)
        {
            _getPremadeSources = getPremadeSources ?? throw new ArgumentNullException(nameof(getPremadeSources));
            _getSavedSources = getSavedSources ?? throw new ArgumentNullException(nameof(getSavedSources));
            _checkVersionAndLoad = checkVersionAndLoad ?? throw new ArgumentNullException(nameof(checkVersionAndLoad));
            _tryLoadCustomXenotype = tryLoadCustomXenotype ??
                                     throw new ArgumentNullException(nameof(tryLoadCustomXenotype));
            _groups = new List<XenogermPlanSourceGroup>().AsReadOnly();
            Refresh();
        }

        public string TitleKey => "XenogermPlanner.PlanSource.Xenotype.Title";

        public string EmptySourcesKey => "XenogermPlanner.PlanSource.Xenotype.EmptySources";

        public IReadOnlyList<XenogermPlanSourceGroup> Groups => _groups;

        public void Refresh()
        {
            List<XenogermPlanSourceEntry> premadeEntries = BuildPremadeEntries();
            List<XenogermPlanSourceEntry> savedEntries = BuildSavedEntries();

            _groups = new List<XenogermPlanSourceGroup>
            {
                new XenogermPlanSourceGroup(
                    PremadeGroupKey,
                    "XenogermPlanner.PlanSource.Xenotype.PremadeGroup",
                    true,
                    premadeEntries),
                new XenogermPlanSourceGroup(
                    SavedGroupKey,
                    "XenogermPlanner.PlanSource.Xenotype.SavedGroup",
                    true,
                    savedEntries)
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

            if (source.SourceToken is PremadeSourceToken premadeToken)
            {
                ResolvePremadeSource(premadeToken, onResolved);
                return;
            }

            if (source.SourceToken is SavedSourceToken savedToken)
            {
                ResolveSavedSource(savedToken, onResolved);
                return;
            }

            onResolved(XenogermPlanSourceResolveResult.Failed(XenogermPlanSourceFailure.SourceUnavailable));
        }

        private List<XenogermPlanSourceEntry> BuildPremadeEntries()
        {
            var entries = new List<XenogermPlanSourceEntry>();
            IEnumerable<XenotypeDef> sources = null;

            try
            {
                sources = _getPremadeSources();
            }
            catch (Exception exception)
            {
                Log.Warning($"[Xenogerm Planner] Failed to enumerate premade xenotypes: {exception}");
            }

            if (sources == null)
                return entries;

            var sourceIndex = 0;

            foreach (XenotypeDef source in sources)
            {
                var token = new PremadeSourceToken(source, sourceIndex++);
                XenogermPlanSourceResolveResult initialResult = ReadPremadeSource(source);
                string displayName = GetPremadeDisplayName(source);
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

            entries.Sort(ComparePremadeEntries);
            return entries;
        }

        private List<XenogermPlanSourceEntry> BuildSavedEntries()
        {
            var entries = new List<XenogermPlanSourceEntry>();
            IEnumerable<FileInfo> files = null;

            try
            {
                files = _getSavedSources();
            }
            catch (Exception exception)
            {
                Log.Warning($"[Xenogerm Planner] Failed to enumerate saved xenotypes: {exception}");
            }

            if (files == null)
                return entries;

            foreach (FileInfo file in files)
            {
                if (file == null || string.IsNullOrWhiteSpace(file.FullName))
                    continue;

                string path = file.FullName;
                var token = new SavedSourceToken(path);

                entries.Add(
                    new XenogermPlanSourceEntry(
                        path,
                        Path.GetFileNameWithoutExtension(file.Name),
                        "XenogermPlanner.PlanSource.Xenotype.SavedCustom",
                        token));
            }

            return entries;
        }

        private void ResolvePremadeSource(PremadeSourceToken token, Action<XenogermPlanSourceResolveResult> onResolved)
        {
            XenotypeDef source = token.Source;

            if (source == null || !ContainsPremadeSourceReference(source))
            {
                onResolved(XenogermPlanSourceResolveResult.Failed(XenogermPlanSourceFailure.SourceUnavailable));
                return;
            }

            onResolved(ReadPremadeSource(source));
        }

        private void ResolveSavedSource(SavedSourceToken token, Action<XenogermPlanSourceResolveResult> onResolved)
        {
            if (!TryGetCurrentFile(token.Path, out FileInfo file))
            {
                _savedSourceCache.Remove(token.Path);
                onResolved(XenogermPlanSourceResolveResult.Failed(XenogermPlanSourceFailure.SourceUnavailable));
                return;
            }

            if (_savedSourceCache.TryGetValue(token.Path, out SavedSourceCacheEntry cached) &&
                cached.Length == file.Length &&
                cached.LastWriteTimeUtc == file.LastWriteTimeUtc)
            {
                onResolved(cached.Result);
                return;
            }

            try
            {
                _checkVersionAndLoad(token.Path, () => CompleteSavedSourceLoad(token.Path, onResolved));
            }
            catch (Exception exception)
            {
                Log.Warning($"[Xenogerm Planner] Failed to load saved xenotype '{token.Path}': {exception}");
                onResolved(XenogermPlanSourceResolveResult.Failed(XenogermPlanSourceFailure.LoadFailed));
            }
        }

        private void CompleteSavedSourceLoad(string path, Action<XenogermPlanSourceResolveResult> onResolved)
        {
            try
            {
                if (!_tryLoadCustomXenotype(path, out CustomXenotype source) || source == null)
                {
                    onResolved(XenogermPlanSourceResolveResult.Failed(XenogermPlanSourceFailure.LoadFailed));
                    return;
                }

                XenogermPlanSourceResolveResult result = ReadCustomSource(source);

                if (result.IsSuccess && TryGetCurrentFile(path, out FileInfo currentFile))
                {
                    _savedSourceCache[path] = new SavedSourceCacheEntry(
                        currentFile.Length,
                        currentFile.LastWriteTimeUtc,
                        result);
                }

                onResolved(result);
            }
            catch (Exception exception)
            {
                Log.Warning($"[Xenogerm Planner] Failed to read saved xenotype '{path}': {exception}");
                onResolved(XenogermPlanSourceResolveResult.Failed(XenogermPlanSourceFailure.LoadFailed));
            }
        }

        private bool ContainsPremadeSourceReference(XenotypeDef source)
        {
            IEnumerable<XenotypeDef> currentSources;

            try
            {
                currentSources = _getPremadeSources();
            }
            catch (Exception exception)
            {
                Log.Warning($"[Xenogerm Planner] Failed to revalidate premade xenotype source: {exception}");
                return false;
            }

            if (currentSources == null)
                return false;

            foreach (XenotypeDef current in currentSources)
            {
                if (ReferenceEquals(current, source))
                    return true;
            }

            return false;
        }

        private static XenogermPlanSourceResolveResult ReadPremadeSource(XenotypeDef source)
        {
            if (!XenotypePlanCreationSourceReader.TryReadSource(
                    source,
                    out XenotypePlanCreationSourceData sourceData,
                    out XenotypePlanCreationSourceFailure failure))
            {
                return XenogermPlanSourceResolveResult.Failed(ConvertFailure(failure));
            }

            return CreateSuccessResult(sourceData);
        }

        private static XenogermPlanSourceResolveResult ReadCustomSource(CustomXenotype source)
        {
            if (!XenotypePlanCreationSourceReader.TryReadSource(
                    source,
                    out XenotypePlanCreationSourceData sourceData,
                    out XenotypePlanCreationSourceFailure failure))
            {
                return XenogermPlanSourceResolveResult.Failed(ConvertFailure(failure));
            }

            return CreateSuccessResult(sourceData);
        }

        private static XenogermPlanSourceResolveResult CreateSuccessResult(XenotypePlanCreationSourceData sourceData)
        {
            if (sourceData == null)
                return XenogermPlanSourceResolveResult.Failed(XenogermPlanSourceFailure.InvalidSourceData);

            try
            {
                return XenogermPlanSourceResolveResult.Success(
                    new XenogermPlanSourceSelection(sourceData.Name, sourceData.DesiredGenes));
            }
            catch (ArgumentException)
            {
                return XenogermPlanSourceResolveResult.Failed(XenogermPlanSourceFailure.InvalidSourceData);
            }
        }

        private static XenogermPlanSourceFailure ConvertFailure(XenotypePlanCreationSourceFailure failure)
        {
            switch (failure)
            {
                case XenotypePlanCreationSourceFailure.SourceUnavailable:
                    return XenogermPlanSourceFailure.SourceUnavailable;

                case XenotypePlanCreationSourceFailure.EmptySource:
                    return XenogermPlanSourceFailure.EmptySource;

                case XenotypePlanCreationSourceFailure.InvalidSourceData:
                default:
                    return XenogermPlanSourceFailure.InvalidSourceData;
            }
        }

        private static int ComparePremadeEntries(XenogermPlanSourceEntry left, XenogermPlanSourceEntry right)
        {
            var leftToken = (PremadeSourceToken)left.SourceToken;
            var rightToken = (PremadeSourceToken)right.SourceToken;

            float leftPriority = leftToken.Source?.displayPriority ?? 0f;
            float rightPriority = rightToken.Source?.displayPriority ?? 0f;
            int priorityComparison = rightPriority.CompareTo(leftPriority);

            if (priorityComparison != 0)
                return priorityComparison;

            int nameComparison = StringComparer.CurrentCultureIgnoreCase.Compare(left.DisplayName, right.DisplayName);

            if (nameComparison != 0)
                return nameComparison;

            return leftToken.SourceIndex.CompareTo(rightToken.SourceIndex);
        }

        private static string GetPremadeDisplayName(XenotypeDef source)
        {
            var displayName = source?.LabelCap.ToString();

            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;

            if (!string.IsNullOrWhiteSpace(source?.defName))
                return source.defName;

            return "XenogermPlanner.PlanSource.Xenotype.UnnamedSource".Translate().ToString();
        }

        private static bool TryGetCurrentFile(string path, out FileInfo file)
        {
            file = null;

            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                var currentFile = new FileInfo(path);
                currentFile.Refresh();

                if (!currentFile.Exists)
                    return false;

                file = currentFile;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void CheckVersionAndLoad(string path, Action loadAction)
        {
            PreLoadUtility.CheckVersionAndLoad(path, ScribeMetaHeaderUtility.ScribeHeaderMode.Xenotype, loadAction);
        }

        private static bool TryLoadCustomXenotypeFile(string path, out CustomXenotype source)
        {
            return GameDataSaveLoader.TryLoadXenotype(path, out source);
        }

        internal delegate bool TryLoadCustomXenotype(string path, out CustomXenotype source);

        private sealed class PremadeSourceToken
        {
            internal PremadeSourceToken(XenotypeDef source, int sourceIndex)
            {
                Source = source;
                SourceIndex = sourceIndex;
            }

            internal XenotypeDef Source { get; }
            internal int SourceIndex { get; }
        }

        private sealed class SavedSourceToken
        {
            internal SavedSourceToken(string path)
            {
                Path = path;
            }

            internal string Path { get; }
        }

        private sealed class SavedSourceCacheEntry
        {
            internal SavedSourceCacheEntry(
                long length,
                DateTime lastWriteTimeUtc,
                XenogermPlanSourceResolveResult result)
            {
                Length = length;
                LastWriteTimeUtc = lastWriteTimeUtc;
                Result = result;
            }

            internal long Length { get; }
            internal DateTime LastWriteTimeUtc { get; }
            internal XenogermPlanSourceResolveResult Result { get; }
        }
    }
}