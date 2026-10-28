using System;
using System.Collections.Generic;
using Escarval.RimWorld.UI;
using UnityEngine;
using Verse;

namespace XenogermPlanner.UI
{
    internal sealed class XenogermPlanSourceDialog : Window
    {
        private const float DialogWidth = 1000f;
        private const float DialogHeight = 700f;
        private const float TitleHeight = 34f;
        private const float ColumnGap = RimWorldUiStyle.Metrics.SectionGap;
        private const float LeftColumnWidth = 340f;
        private const float SectionPadding = RimWorldUiStyle.Metrics.PanelPadding;
        private const float SectionTitleHeight = RimWorldUiStyle.Metrics.HeaderHeight;
        private const float GroupRowHeight = RimWorldUiStyle.Metrics.CompactRowHeight;
        private const float SourceRowHeight = RimWorldUiStyle.Metrics.TwoLineRowHeight;
        private const float MetadataLineHeight = 24f;
        private const float GeneRowHeight = RimWorldUiStyle.Metrics.CompactRowHeight;
        private const float FooterHeight = 40f;
        private const float FooterButtonWidth = 120f;
        private const float FooterButtonHeight = 35f;
        private const float FooterButtonGap = 8f;
        private const float ContentGap = RimWorldUiStyle.Metrics.SectionGap;
        private readonly HashSet<string> _collapsedGroupKeys = new HashSet<string>(StringComparer.Ordinal);
        private readonly Action<XenogermPlanSourceSelection> _onSourceSelected;

        private readonly IXenogermPlanSourceProvider _provider;

        private readonly VariableHeightScrollListLayoutCache _sourceLayoutCache =
            new VariableHeightScrollListLayoutCache();

        private bool _isResolving;
        private object _presentationLanguageKey;
        private List<GeneDef> _previewGenes = new List<GeneDef>();
        private Vector2 _previewGenesScrollPosition;
        private int _resolveVersion;
        private XenogermPlanSourceResolveResult _selectedResult;
        private XenogermPlanSourceEntry _selectedSource;
        private Vector2 _sourceListScrollPosition;

        private List<XenogermPlanSourceListRow> _sourceRows = new List<XenogermPlanSourceListRow>();

        internal XenogermPlanSourceDialog(
            IXenogermPlanSourceProvider provider,
            Action<XenogermPlanSourceSelection> onSourceSelected)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _onSourceSelected = onSourceSelected;

            RefreshSources(preferredStableKey: null);

            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            forcePause = true;
        }

        public override Vector2 InitialSize => new Vector2(DialogWidth, DialogHeight);

        public override void DoWindowContents(Rect inRect)
        {
            using (ImGuiStateScope.Capture())
            {
                RefreshLanguageDependentPresentationIfNeeded();

                DrawTitle(new Rect(inRect.x, inRect.y, inRect.width, TitleHeight));

                var footerRect = new Rect(inRect.x, inRect.yMax - FooterHeight, inRect.width, FooterHeight);

                var contentRect = new Rect(
                    inRect.x,
                    inRect.y + TitleHeight,
                    inRect.width,
                    footerRect.y - inRect.y - TitleHeight - ContentGap);

                float leftColumnWidth = Mathf.Min(LeftColumnWidth, contentRect.width * 0.4f);
                var sourcesRect = new Rect(contentRect.x, contentRect.y, leftColumnWidth, contentRect.height);

                var previewRect = new Rect(
                    sourcesRect.xMax + ColumnGap,
                    contentRect.y,
                    Mathf.Max(0f, contentRect.width - leftColumnWidth - ColumnGap),
                    contentRect.height);

                DrawSourceList(sourcesRect);
                DrawPreview(previewRect);
                DrawFooter(footerRect);
            }
        }

        private void DrawTitle(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;

            Widgets.Label(rect, _provider.TitleKey.Translate());
        }

        private void DrawSourceList(Rect rect)
        {
            RimWorldUiWidgets.DrawPanel(rect);

            Rect innerRect = rect.ContractedBy(SectionPadding);
            var headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width, SectionTitleHeight);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;

            Widgets.Label(headerRect, "XenogermPlanner.PlanSource.Sources".Translate());

            var listRect = new Rect(
                innerRect.x,
                headerRect.yMax,
                innerRect.width,
                innerRect.height - SectionTitleHeight);

            if (!HasAnySources())
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;

                Widgets.Label(listRect, _provider.EmptySourcesKey.Translate());
                return;
            }

            float measuredWidth = Mathf.Max(0f, listRect.width - RimWorldUiStyle.Metrics.ScrollbarWidth);

            List<XenogermPlanSourceListRow> sourceRows = _sourceRows;
            VariableHeightScrollListLayout layout = GetSourceLayout(measuredWidth);

            VariableHeightScrollListVisibleRange visibleRange = RimWorldUiWidgets.BeginVariableHeightScrollView(
                listRect,
                ref _sourceListScrollPosition,
                layout,
                out Rect viewRect);

            string groupKeyToToggle = null;

            try
            {
                for (int index = visibleRange.FirstVisibleIndex;
                     index < visibleRange.LastVisibleIndexExclusive;
                     index++)
                {
                    XenogermPlanSourceListRow row = sourceRows[index];
                    var rowRect = new Rect(
                        viewRect.x,
                        viewRect.y + layout.GetRowOffset(index),
                        viewRect.width,
                        layout.GetRowHeight(index));

                    if (row.Kind == XenogermPlanSourceListRowKind.Group)
                    {
                        if (DrawGroupRow(rowRect, row, index))
                            groupKeyToToggle = row.Group.Key;
                    }
                    else
                    {
                        DrawSourceRow(rowRect, row.Source, index);
                    }
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }

            if (groupKeyToToggle != null)
                ToggleSourceGroup(groupKeyToToggle);
        }

        private VariableHeightScrollListLayout GetSourceLayout(float measuredWidth)
        {
            if (_sourceLayoutCache.TryGet(_sourceRows, 0, measuredWidth, out VariableHeightScrollListLayout layout))
            {
                return layout;
            }

            var measurements = new List<VariableHeightScrollListRowMeasurement>(_sourceRows.Count);

            foreach (XenogermPlanSourceListRow row in _sourceRows)
            {
                measurements.Add(
                    new VariableHeightScrollListRowMeasurement(
                        row.Kind == XenogermPlanSourceListRowKind.Group ? GroupRowHeight : SourceRowHeight));
            }

            layout = VariableHeightScrollListLayout.Create(measuredWidth, measurements);
            _sourceLayoutCache.Store(_sourceRows, 0, layout);
            return layout;
        }

        private static bool DrawGroupRow(Rect rect, XenogermPlanSourceListRow row, int rowIndex)
        {
            var label = row.Group.LabelKey.Translate().ToString();

            return XenogermPlannerWidgets.DrawCollapsibleSectionRow(rect, label, row.IsGroupExpanded, rowIndex);
        }

        private void ToggleSourceGroup(string groupKey)
        {
            if (!_collapsedGroupKeys.Add(groupKey))
                _collapsedGroupKeys.Remove(groupKey);

            RefreshSourceProjection();
        }

        private void DrawSourceRow(Rect rect, XenogermPlanSourceEntry source, int rowIndex)
        {
            bool selected = ReferenceEquals(_selectedSource, source);

            RimWorldUiWidgets.DrawSelectableRowBackground(rect, rowIndex, selected, Mouse.IsOver(rect));

            Rect contentRect = rect.ContractedBy(RimWorldUiStyle.Metrics.ControlGap);
            float nameHeight = contentRect.height * 0.56f;
            var nameRect = new Rect(contentRect.x, contentRect.y, contentRect.width, nameHeight);
            var metadataRect = new Rect(
                contentRect.x,
                nameRect.yMax,
                contentRect.width,
                Mathf.Max(0f, contentRect.yMax - nameRect.yMax));

            RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                nameRect,
                source.DisplayName,
                GameFont.Small,
                TextAnchor.MiddleLeft);

            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = source.IsKnownInvalid ? RimWorldUiStyle.Colors.Warning : RimWorldUiStyle.Colors.MutedText;

                Widgets.Label(metadataRect, GetSourceMetadata(source));
            }

            if (Event.current.button == 0 && Widgets.ButtonInvisible(rect))
                SelectSource(source);
        }

        private void DrawPreview(Rect rect)
        {
            RimWorldUiWidgets.DrawPanel(rect);

            Rect contentRect = rect.ContractedBy(SectionPadding);

            if (_selectedSource == null)
            {
                DrawCenteredMessage(contentRect, "XenogermPlanner.PlanSource.NoSelection".Translate().ToString());
                return;
            }

            var sourceTitleRect = new Rect(contentRect.x, contentRect.y, contentRect.width, SectionTitleHeight);
            string previewTitle = _selectedResult?.IsSuccess == true
                ? _selectedResult.Selection.Name
                : _selectedSource.DisplayName;

            RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                sourceTitleRect,
                previewTitle,
                GameFont.Medium,
                TextAnchor.MiddleLeft);

            var bodyRect = new Rect(
                contentRect.x,
                sourceTitleRect.yMax + RimWorldUiStyle.Metrics.SmallGap,
                contentRect.width,
                Mathf.Max(0f, contentRect.yMax - sourceTitleRect.yMax - RimWorldUiStyle.Metrics.SmallGap));

            if (_isResolving)
            {
                DrawCenteredMessage(bodyRect, "XenogermPlanner.PlanSource.Loading".Translate().ToString());
                return;
            }

            if (_selectedResult == null)
            {
                DrawCenteredMessage(bodyRect, "XenogermPlanner.PlanSource.PreviewUnavailable".Translate().ToString());
                return;
            }

            if (!_selectedResult.IsSuccess)
            {
                DrawWarningText(bodyRect, GetFailureMessage(_selectedResult.Failure));
                return;
            }

            DrawSuccessfulPreview(bodyRect);
        }

        private void DrawSuccessfulPreview(Rect rect)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            var metadataRect = new Rect(rect.x, rect.y, rect.width, MetadataLineHeight);
            Widgets.Label(metadataRect, "XenogermPlanner.PlanSource.GeneCount".Translate(_previewGenes.Count));

            var genesRect = new Rect(
                rect.x,
                metadataRect.yMax + ContentGap,
                rect.width,
                Mathf.Max(0f, rect.yMax - metadataRect.yMax - ContentGap));

            FixedHeightScrollListLayout layout = RimWorldUiWidgets.BeginFixedHeightScrollView(
                genesRect,
                ref _previewGenesScrollPosition,
                _previewGenes.Count,
                GeneRowHeight,
                out float viewWidth);

            for (int index = layout.FirstVisibleIndex; index < layout.LastVisibleIndexExclusive; index++)
            {
                GeneDef gene = _previewGenes[index];
                var geneRect = new Rect(0f, index * GeneRowHeight, viewWidth, GeneRowHeight);

                RimWorldUiWidgets.DrawSelectableRowBackground(
                    geneRect,
                    index,
                    selected: false,
                    hovered: Mouse.IsOver(geneRect),
                    drawAccent: false);

                XenogermPlannerWidgets.DrawGeneLabel(geneRect, gene);
                XenogermPlannerWidgets.AddGeneTooltip(geneRect, gene);
                XenogermPlannerNativeInspector.TryOpenContextMenu(geneRect, gene);
            }

            Widgets.EndScrollView();
        }

        private void DrawFooter(Rect rect)
        {
            var continueButtonRect = new Rect(
                rect.xMax - FooterButtonWidth,
                rect.y + (rect.height - FooterButtonHeight) * 0.5f,
                FooterButtonWidth,
                FooterButtonHeight);

            var cancelButtonRect = new Rect(
                continueButtonRect.x - FooterButtonGap - FooterButtonWidth,
                continueButtonRect.y,
                FooterButtonWidth,
                FooterButtonHeight);

            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Small;

                if (Widgets.ButtonText(cancelButtonRect, "XenogermPlanner.PlanSource.Cancel".Translate()))
                    Close();

                bool previousEnabled = GUI.enabled;

                try
                {
                    GUI.enabled = !_isResolving && _selectedSource != null && _selectedResult?.IsSuccess == true;

                    if (Widgets.ButtonText(continueButtonRect, "XenogermPlanner.PlanSource.Continue".Translate()))
                    {
                        ContinueWithSelectedSource();
                    }
                }
                finally
                {
                    GUI.enabled = previousEnabled;
                }
            }
        }

        private void SelectSource(XenogermPlanSourceEntry source)
        {
            if (source == null)
                return;

            _selectedSource = source;
            _selectedResult = null;
            _previewGenes.Clear();
            _previewGenesScrollPosition = Vector2.zero;
            ResolveSelectedSource(revalidate: false, closeOnSuccess: false);
        }

        private void ContinueWithSelectedSource()
        {
            if (_selectedSource == null)
                return;

            ResolveSelectedSource(revalidate: true, closeOnSuccess: true);
        }

        private void ResolveSelectedSource(bool revalidate, bool closeOnSuccess)
        {
            XenogermPlanSourceEntry source = _selectedSource;

            if (source == null)
                return;

            int resolveVersion = ++_resolveVersion;
            var callbackInvoked = false;
            _isResolving = true;

            _provider.Resolve(
                source,
                revalidate,
                result =>
                {
                    callbackInvoked = true;

                    if (resolveVersion != _resolveVersion || !ReferenceEquals(source, _selectedSource))
                        return;

                    _isResolving = false;
                    _selectedResult = result ??
                                      XenogermPlanSourceResolveResult.Failed(
                                          XenogermPlanSourceFailure.InvalidSourceData);
                    RefreshPreviewGenes();

                    if (!closeOnSuccess || !_selectedResult.IsSuccess)
                        return;

                    _onSourceSelected?.Invoke(_selectedResult.Selection);
                    Close();
                });

            if (!callbackInvoked && resolveVersion == _resolveVersion && ReferenceEquals(source, _selectedSource))
                _isResolving = false;
        }

        private void RefreshPreviewGenes()
        {
            _previewGenes = _selectedResult?.IsSuccess == true
                ? XenogermPlannerPresentation.GetSortedGenes(_selectedResult.Selection.DesiredGenes)
                : new List<GeneDef>();
            _previewGenesScrollPosition = Vector2.zero;
        }

        private void RefreshSources(object preferredStableKey)
        {
            _resolveVersion++;
            _isResolving = false;
            _selectedResult = null;
            _previewGenes.Clear();

            _provider.Refresh();
            RefreshSourceProjection();

            _selectedSource = FindSourceByStableKey(preferredStableKey);
            _presentationLanguageKey = LanguageDatabase.activeLanguage;

            if (_selectedSource != null)
                SelectSource(_selectedSource);
        }

        private void RefreshSourceProjection()
        {
            _sourceRows = XenogermPlanSourceListProjection.Build(_provider.Groups, _collapsedGroupKeys);
            _sourceLayoutCache.Invalidate();
        }

        private XenogermPlanSourceEntry FindSourceByStableKey(object stableKey)
        {
            if (stableKey == null)
                return null;

            foreach (XenogermPlanSourceGroup group in _provider.Groups)
            {
                foreach (XenogermPlanSourceEntry source in group.Sources)
                {
                    if (Equals(source.StableKey, stableKey))
                        return source;
                }
            }

            return null;
        }

        private bool HasAnySources()
        {
            foreach (XenogermPlanSourceGroup group in _provider.Groups)
            {
                if (group.Sources.Count > 0)
                    return true;
            }

            return false;
        }

        private void RefreshLanguageDependentPresentationIfNeeded()
        {
            object languageKey = LanguageDatabase.activeLanguage;

            if (Equals(_presentationLanguageKey, languageKey))
                return;

            object preferredStableKey = _selectedSource?.StableKey;
            RefreshSources(preferredStableKey);
        }

        private static string GetSourceMetadata(XenogermPlanSourceEntry source)
        {
            if (source.MetadataArguments.Count == 0)
                return source.MetadataKey.Translate().ToString();

            var arguments = new NamedArgument[source.MetadataArguments.Count];

            for (var index = 0; index < arguments.Length; index++)
                arguments[index] = source.MetadataArguments[index];

            return source.MetadataKey.Translate(arguments).ToString();
        }

        private static string GetFailureMessage(XenogermPlanSourceFailure failure)
        {
            switch (failure)
            {
                case XenogermPlanSourceFailure.SourceUnavailable:
                    return "XenogermPlanner.PlanSource.Errors.SourceUnavailable".Translate().ToString();

                case XenogermPlanSourceFailure.EmptySource:
                    return "XenogermPlanner.PlanSource.Errors.EmptySource".Translate().ToString();

                case XenogermPlanSourceFailure.LoadFailed:
                    return "XenogermPlanner.PlanSource.Errors.LoadFailed".Translate().ToString();

                case XenogermPlanSourceFailure.InvalidSourceData:
                default:
                    return "XenogermPlanner.PlanSource.Errors.InvalidSource".Translate().ToString();
            }
        }

        private static void DrawCenteredMessage(Rect rect, string text)
        {
            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = RimWorldUiStyle.Colors.MutedText;
                Widgets.Label(rect, text);
            }
        }

        private static void DrawWarningText(Rect rect, string text)
        {
            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = RimWorldUiStyle.Colors.Warning;
                Widgets.Label(rect, text);
            }
        }
    }
}