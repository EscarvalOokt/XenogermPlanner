using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Escarval.RimWorld.UI;
using RimWorld;
using UnityEngine;
using Verse;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;
using XenogermPlanner.Templates;

namespace XenogermPlanner.UI
{
    internal sealed class XenogermTemplateCreationDialog : Window
    {
        private const float DialogWidth = 1000f;
        private const float DialogHeight = 720f;
        private const float HeaderHeight = 34f;
        private const float NameFieldWidth = 360f;
        private const float IconSize = 32f;
        private const float FieldControlGap = RimWorldUiStyle.Metrics.SectionGap;
        private const float FieldSectionGap = 18f;
        private const float RowGap = RimWorldUiStyle.Metrics.SmallGap;
        private const float SectionGap = RimWorldUiStyle.Metrics.SectionGap;
        private const float FooterHeight = 40f;
        private const float ButtonWidth = 140f;
        private const float CandidateListWidth = 320f;
        private const float CandidateMinRowHeight = 54f;
        private const float CandidateRowPadding = RimWorldUiStyle.Metrics.ControlGap;
        private const float CandidateLabelHeight = 24f;
        private const float CandidateSummaryGap = 2f;
        private const float PreviewPadding = RimWorldUiStyle.Metrics.PanelPadding;
        private const float PreviewHeaderHeight = RimWorldUiStyle.Metrics.HeaderHeight;
        private const float PreviewBiostatWidth = 250f;
        private const float PreviewBiostatGap = RimWorldUiStyle.Metrics.SectionGap;
        private const float CompositionHeaderHeight = RimWorldUiStyle.Metrics.HeaderHeight;
        private const float CompositionHeaderBiostatGap = 8f;
        private const float CompositionContentGap = RimWorldUiStyle.Metrics.SmallGap;
        private const float GeneRowHeight = RimWorldUiStyle.Metrics.CompactRowHeight;
        private const float CompositionGap = RimWorldUiStyle.Metrics.SectionGap;
        private const float AdditionalGenePanelPadding = RimWorldUiStyle.Metrics.ControlGap;
        private const float AdditionalGeneHeaderHeight = RimWorldUiStyle.Metrics.HeaderHeight;
        private const float AdditionalGeneIconGap = RimWorldUiStyle.Metrics.SmallGap;
        private const int MaxAdditionalGeneIconRows = 2;
        private const float SearchLimitedWarningPadding = RimWorldUiStyle.Metrics.ControlGap;
        private const int MaxNameLength = 40;

        private static readonly Regex _validNameSymbolRegex = new Regex("^[\\p{L}0-9 '\\-]*$");

        private readonly Dictionary<string, string> _additionalGeneTooltips =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private readonly Dictionary<string, PlanXenogermTemplateBiostats> _candidateBiostats =
            new Dictionary<string, PlanXenogermTemplateBiostats>(StringComparer.Ordinal);

        private readonly VariableHeightScrollListLayoutCache _candidateListLayoutCache =
            new VariableHeightScrollListLayoutCache();

        private readonly Dictionary<string, PlanXenogermTemplateBiostats> _compositionBiostats =
            new Dictionary<string, PlanXenogermTemplateBiostats>(StringComparer.Ordinal);

        private readonly PlanGenepackInventoryGameComponent _inventoryComponent;

        private readonly XenogermPlan _plan;

        private readonly VariableHeightScrollListLayoutCache _previewLayoutCache =
            new VariableHeightScrollListLayoutCache();

        private readonly PlanXenogermTemplateCandidateSearchResult _searchResult;
        private Vector2 _candidateListScrollPosition;

        private bool _customize;
        private XenotypeIconDef _iconDef;
        private object _presentationLanguageKey;

        private XenogermTemplatePresentationProjection _presentationProjection;
        private Vector2 _previewScrollPosition;
        private int _selectedCandidateIndex;
        private string _templateName;

        internal XenogermTemplateCreationDialog(
            XenogermPlan plan,
            PlanGenepackInventoryGameComponent inventoryComponent,
            PlanXenogermTemplateCandidateSearchResult searchResult)
        {
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
            _inventoryComponent = inventoryComponent ?? throw new ArgumentNullException(nameof(inventoryComponent));
            _searchResult = searchResult ?? throw new ArgumentNullException(nameof(searchResult));

            if (!searchResult.HasCandidate)
                throw new ArgumentException(
                    "Template creation dialog requires at least one candidate.",
                    nameof(searchResult));

            _templateName = plan.Name ?? string.Empty;
            _iconDef = XenotypeIconDefOf.Basic;
            _customize = false;
            _selectedCandidateIndex = 0;
            RefreshPresentationProjection();

            doCloseX = true;
            closeOnAccept = false;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(DialogWidth, DialogHeight);

        private XenogermTemplateCandidatePresentation SelectedCandidatePresentation =>
            _selectedCandidateIndex >= 0 &&
            _presentationProjection != null &&
            _selectedCandidateIndex < _presentationProjection.Candidates.Count
                ? _presentationProjection.Candidates[_selectedCandidateIndex]
                : null;

        public override void DoWindowContents(Rect inRect)
        {
            using (ImGuiStateScope.Capture())
            {
                RefreshLanguageDependentPresentationIfNeeded();

                Rect contentRect = inRect;
                var titleRect = new Rect(contentRect.x, contentRect.y, contentRect.width, HeaderHeight);
                DrawTitle(titleRect);

                float y = titleRect.yMax + RowGap;
                y = DrawNameAndIcon(new Rect(contentRect.x, y, contentRect.width, IconSize));
                y += RowGap;
                y = DrawModeSelector(new Rect(contentRect.x, y, contentRect.width, 30f));
                y += SectionGap;

                if (!_searchResult.IsComplete)
                {
                    y = DrawSearchLimitedWarning(contentRect.x, y, contentRect.width);
                    y += SectionGap;
                }

                var footerRect = new Rect(
                    contentRect.x,
                    contentRect.yMax - FooterHeight,
                    contentRect.width,
                    FooterHeight);
                var bodyRect = new Rect(
                    contentRect.x,
                    y,
                    contentRect.width,
                    Mathf.Max(0f, footerRect.y - y - SectionGap));

                DrawBody(bodyRect);
                DrawFooter(footerRect);
            }
        }

        public override void OnAcceptKeyPressed()
        {
            TrySave();
            Event.current?.Use();
        }

        private static void DrawTitle(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rect, "XenogermPlanner.Template.Title".Translate());
        }

        private float DrawNameAndIcon(Rect rect)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            var nameLabel = "XenogermPlanner.Template.Name".Translate().ToString();
            var iconLabel = "XenogermPlanner.Template.Icon".Translate().ToString();
            float nameLabelWidth = Text.CalcSize(nameLabel).x;
            float iconLabelWidth = Text.CalcSize(iconLabel).x;

            var nameLabelRect = new Rect(rect.x, rect.y, nameLabelWidth, rect.height);
            Widgets.Label(nameLabelRect, nameLabel);

            var nameRect = new Rect(
                nameLabelRect.xMax + FieldControlGap,
                rect.y + (rect.height - Text.LineHeight) * 0.5f,
                NameFieldWidth,
                Text.LineHeight);
            _templateName = Widgets.TextField(
                nameRect,
                _templateName ?? string.Empty,
                MaxNameLength,
                _validNameSymbolRegex);

            var iconLabelRect = new Rect(nameRect.xMax + FieldSectionGap, rect.y, iconLabelWidth, rect.height);
            Widgets.Label(iconLabelRect, iconLabel);

            var iconRect = new Rect(
                iconLabelRect.xMax + FieldControlGap,
                rect.y + (rect.height - IconSize) * 0.5f,
                IconSize,
                IconSize);
            Widgets.DrawBoxSolid(iconRect, RimWorldUiStyle.Colors.NestedPanelBackground);

            if (Widgets.ButtonImage(iconRect, _iconDef.Icon, XenotypeDef.IconColor))
            {
                Find.WindowStack.Add(
                    new Dialog_SelectXenotypeIcon(
                        _iconDef,
                        selectedIcon => _iconDef = selectedIcon ?? XenotypeIconDefOf.Basic));
            }

            TooltipHandler.TipRegion(
                iconRect,
                "SelectIconDesc".Translate() +
                "\n\n" +
                "ClickToEdit".Translate().Colorize(RimWorldUiStyle.Colors.MutedText));

            return rect.yMax;
        }

        private float DrawModeSelector(Rect rect)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            var automaticLabel = "XenogermPlanner.Template.Mode.Automatic".Translate().ToString();
            var customizeLabel = "XenogermPlanner.Template.Mode.Customize".Translate().ToString();
            float automaticWidth = Text.CalcSize(automaticLabel).x + 30f;
            float customizeWidth = Text.CalcSize(customizeLabel).x + 30f;

            var automaticRect = new Rect(rect.x, rect.y, automaticWidth, rect.height);
            var customizeRect = new Rect(automaticRect.xMax + 24f, rect.y, customizeWidth, rect.height);

            if (Widgets.RadioButtonLabeled(automaticRect, automaticLabel, !_customize))
            {
                _customize = false;
                _selectedCandidateIndex = 0;
                _previewScrollPosition = Vector2.zero;
            }

            bool hasAlternatives = _searchResult.Candidates.Count > 1;

            if (hasAlternatives)
            {
                if (Widgets.RadioButtonLabeled(customizeRect, customizeLabel, _customize))
                    _customize = true;
            }
            else
            {
                using (ImGuiStateScope.Capture())
                {
                    GUI.color = RimWorldUiStyle.Colors.MutedText;
                    Widgets.RadioButtonLabeled(customizeRect, customizeLabel, false);
                }
            }

            string automaticDescriptionKey = _searchResult.IsComplete
                ? "XenogermPlanner.Template.Mode.AutomaticDescription"
                : "XenogermPlanner.Template.Mode.AutomaticDescriptionLimited";
            string customizeDescriptionKey;

            if (hasAlternatives)
                customizeDescriptionKey = "XenogermPlanner.Template.Mode.CustomizeDescription";
            else if (_searchResult.IsComplete)
                customizeDescriptionKey = "XenogermPlanner.Template.NoAlternatives";
            else
                customizeDescriptionKey = "XenogermPlanner.Template.NoDisplayedAlternativesLimited";

            TooltipHandler.TipRegion(automaticRect, automaticDescriptionKey.Translate().ToString());
            TooltipHandler.TipRegion(customizeRect, customizeDescriptionKey.Translate().ToString());

            return rect.yMax;
        }

        private static float DrawSearchLimitedWarning(float x, float y, float width)
        {
            var warning = "XenogermPlanner.Template.SearchLimitedWarning".Translate().ToString();
            float contentWidth = Mathf.Max(1f, width - SearchLimitedWarningPadding * 2f);
            float textHeight = CalculateTextHeight(warning, contentWidth, GameFont.Small);
            float warningHeight = textHeight + SearchLimitedWarningPadding * 2f;
            var warningRect = new Rect(x, y, width, warningHeight);
            Rect textRect = warningRect.ContractedBy(SearchLimitedWarningPadding);
            RimWorldUiWidgets.DrawPanelBackground(warningRect, nested: true);

            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = RimWorldUiStyle.Colors.Warning;
                Widgets.Label(textRect, warning);
            }

            return warningRect.yMax;
        }

        private void DrawBody(Rect rect)
        {
            if (_customize)
            {
                var candidateListRect = new Rect(rect.x, rect.y, CandidateListWidth, rect.height);
                var previewRect = new Rect(
                    candidateListRect.xMax + SectionGap,
                    rect.y,
                    Mathf.Max(0f, rect.width - CandidateListWidth - SectionGap),
                    rect.height);

                DrawCandidateList(candidateListRect);
                DrawPreview(previewRect);
                return;
            }

            DrawPreview(rect);
        }

        private void DrawCandidateList(Rect rect)
        {
            RimWorldUiWidgets.DrawPanel(rect);
            Rect contentRect = rect.ContractedBy(PreviewPadding);
            var titleRect = new Rect(contentRect.x, contentRect.y, contentRect.width, PreviewHeaderHeight);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(titleRect, "XenogermPlanner.Template.Alternatives".Translate());

            var listRect = new Rect(
                contentRect.x,
                titleRect.yMax,
                contentRect.width,
                Mathf.Max(0f, contentRect.yMax - titleRect.yMax));
            float viewWidth = Mathf.Max(0f, listRect.width - RimWorldUiStyle.Metrics.ScrollbarWidth);
            VariableHeightScrollListLayout layout = GetCandidateListLayout(viewWidth);

            VariableHeightScrollListVisibleRange visibleRange = RimWorldUiWidgets.BeginVariableHeightScrollView(
                listRect,
                ref _candidateListScrollPosition,
                layout,
                out _);

            for (int index = visibleRange.FirstVisibleIndex; index < visibleRange.LastVisibleIndexExclusive; index++)
            {
                XenogermTemplateCandidatePresentation candidate = _presentationProjection.Candidates[index];
                var rowRect = new Rect(0f, layout.GetRowOffset(index), viewWidth, layout.GetRowHeight(index));
                DrawCandidateRow(rowRect, candidate);
            }

            Widgets.EndScrollView();
        }

        private VariableHeightScrollListLayout GetCandidateListLayout(float viewWidth)
        {
            if (_candidateListLayoutCache.TryGet(
                    _presentationProjection,
                    0,
                    viewWidth,
                    out VariableHeightScrollListLayout layout))
            {
                return layout;
            }

            var rows = new VariableHeightScrollListRowMeasurement[_presentationProjection.Candidates.Count];

            for (var index = 0; index < _presentationProjection.Candidates.Count; index++)
            {
                rows[index] = new VariableHeightScrollListRowMeasurement(
                    CalculateCandidateRowHeight(_presentationProjection.Candidates[index], viewWidth));
            }

            layout = VariableHeightScrollListLayout.Create(viewWidth, rows);
            _candidateListLayoutCache.Store(_presentationProjection, 0, layout);
            return layout;
        }

        private void DrawCandidateRow(Rect rect, XenogermTemplateCandidatePresentation candidate)
        {
            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));

            int index = candidate.Index;
            bool selected = index == _selectedCandidateIndex;

            RimWorldUiWidgets.DrawSelectableRowBackground(rect, index, selected, Mouse.IsOver(rect));

            Rect contentRect = rect.ContractedBy(CandidateRowPadding);
            var labelRect = new Rect(contentRect.x, contentRect.y, contentRect.width, CandidateLabelHeight);
            var summaryRect = new Rect(
                contentRect.x,
                labelRect.yMax + CandidateSummaryGap,
                contentRect.width,
                Mathf.Max(0f, contentRect.yMax - labelRect.yMax - CandidateSummaryGap));

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, candidate.Label);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = RimWorldUiStyle.Colors.MutedText;
            Widgets.Label(summaryRect, candidate.Summary);
            GUI.color = Color.white;

            if (Event.current.button == 0 && Widgets.ButtonInvisible(rect))
            {
                _selectedCandidateIndex = index;
                _previewScrollPosition = Vector2.zero;
            }
        }

        private static float CalculateCandidateRowHeight(XenogermTemplateCandidatePresentation candidate, float width)
        {
            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));

            float contentWidth = Mathf.Max(1f, width - CandidateRowPadding * 2f);
            float summaryHeight = CalculateTextHeight(candidate.Summary, contentWidth, GameFont.Tiny);
            float contentHeight = CandidateLabelHeight + CandidateSummaryGap + summaryHeight;

            return Mathf.Max(CandidateMinRowHeight, contentHeight + CandidateRowPadding * 2f);
        }

        private void DrawPreview(Rect rect)
        {
            RimWorldUiWidgets.DrawPanel(rect);
            Rect contentRect = rect.ContractedBy(PreviewPadding);
            XenogermTemplateCandidatePresentation candidatePresentation = SelectedCandidatePresentation;
            PlanXenogermTemplateCandidate candidate = candidatePresentation?.Candidate;

            if (candidate == null)
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(contentRect, "XenogermPlanner.Template.NoCandidate".Translate());
                return;
            }

            float biostatWidth = Mathf.Min(PreviewBiostatWidth, contentRect.width * 0.45f);
            float headerTextWidth = Mathf.Max(0f, contentRect.width - biostatWidth - PreviewBiostatGap);
            var titleRect = new Rect(contentRect.x, contentRect.y, headerTextWidth, PreviewHeaderHeight);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(titleRect, "XenogermPlanner.Template.Preview".Translate());

            var modeRect = new Rect(contentRect.x, titleRect.yMax, headerTextWidth, 24f);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = RimWorldUiStyle.Colors.MutedText;
            Widgets.Label(
                modeRect,
                "XenogermPlanner.Template.PlanMode".Translate(
                    XenogermPlannerPresentation.GetReadinessModeLabel(_plan.ReadinessMode)));

            var summaryRect = new Rect(contentRect.x, modeRect.yMax, headerTextWidth, 24f);
            Widgets.Label(summaryRect, candidatePresentation.Summary);
            GUI.color = Color.white;

            float headerAreaHeight = summaryRect.yMax - contentRect.y;
            var biostatRect = new Rect(
                contentRect.xMax - biostatWidth,
                contentRect.y + (headerAreaHeight - XenogermPlannerWidgets.LabeledBiostatSummaryHeight) * 0.5f,
                biostatWidth,
                XenogermPlannerWidgets.LabeledBiostatSummaryHeight);
            XenogermPlannerWidgets.DrawLabeledBiostatSummary(biostatRect, GetCandidateBiostats(candidate));

            float additionalGenePanelY = Mathf.Max(summaryRect.yMax, biostatRect.yMax) + RowGap;
            float additionalGenePanelHeight = CalculateAdditionalGenePanelHeight(
                candidatePresentation,
                contentRect.width);
            var additionalGenePanelRect = new Rect(
                contentRect.x,
                additionalGenePanelY,
                contentRect.width,
                additionalGenePanelHeight);
            DrawAdditionalGenePanel(additionalGenePanelRect, candidatePresentation);

            var scrollRect = new Rect(
                contentRect.x,
                additionalGenePanelRect.yMax + RowGap,
                contentRect.width,
                Mathf.Max(0f, contentRect.yMax - additionalGenePanelRect.yMax - RowGap));
            float viewWidth = Mathf.Max(0f, scrollRect.width - RimWorldUiStyle.Metrics.ScrollbarWidth);
            VariableHeightScrollListLayout layout = GetPreviewLayout(candidatePresentation, viewWidth);

            VariableHeightScrollListVisibleRange visibleRange =
                RimWorldUiWidgets.BeginVariableHeightScrollView(scrollRect, ref _previewScrollPosition, layout, out _);

            DrawCompositions(viewWidth, candidatePresentation, layout, visibleRange);
            Widgets.EndScrollView();
        }

        private static float CalculateAdditionalGenePanelHeight(
            XenogermTemplateCandidatePresentation candidate,
            float width)
        {
            float contentWidth = Mathf.Max(1f, width - AdditionalGenePanelPadding * 2f);
            float height = AdditionalGenePanelPadding * 2f + AdditionalGeneHeaderHeight;

            if (candidate.SortedAdditionalGenes.Count == 0)
                return height;

            float iconFlowHeight = XenogermPlannerWidgets.CalculateLimitedGeneIconFlowHeight(
                candidate.SortedAdditionalGenes.Count,
                contentWidth,
                MaxAdditionalGeneIconRows);

            return height + AdditionalGeneIconGap + iconFlowHeight;
        }

        private void DrawAdditionalGenePanel(Rect rect, XenogermTemplateCandidatePresentation candidate)
        {
            RimWorldUiWidgets.DrawPanelBackground(rect, nested: true);
            RimWorldUiWidgets.DrawPanelBorder(rect);

            Rect contentRect = rect.ContractedBy(AdditionalGenePanelPadding);
            var headerRect = new Rect(contentRect.x, contentRect.y, contentRect.width, AdditionalGeneHeaderHeight);

            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;

                if (candidate.SortedAdditionalGenes.Count == 0)
                {
                    GUI.color = RimWorldUiStyle.Colors.MutedText;
                    Widgets.Label(headerRect, "XenogermPlanner.Template.AdditionalGenesNone".Translate());
                    return;
                }

                GUI.color = RimWorldUiStyle.Colors.Warning;
                Widgets.Label(
                    headerRect,
                    "XenogermPlanner.Template.AdditionalGeneCount".Translate(candidate.SortedAdditionalGenes.Count));
            }

            float iconFlowHeight = Mathf.Max(
                0f,
                contentRect.height - AdditionalGeneHeaderHeight - AdditionalGeneIconGap);
            var iconFlowRect = new Rect(
                contentRect.x,
                headerRect.yMax + AdditionalGeneIconGap,
                contentRect.width,
                iconFlowHeight);
            bool hasOverflow = XenogermPlannerWidgets.CalculateGeneIconFlowHeight(
                                   candidate.SortedAdditionalGenes.Count,
                                   iconFlowRect.width) >
                               iconFlowHeight;
            string overflowTooltip = hasOverflow ? GetAdditionalGeneTooltip(candidate) : null;

            XenogermPlannerWidgets.DrawLimitedGeneIconFlow(
                iconFlowRect,
                candidate.SortedAdditionalGenes,
                MaxAdditionalGeneIconRows,
                overflowTooltip);
        }

        private string GetAdditionalGeneTooltip(XenogermTemplateCandidatePresentation candidate)
        {
            string candidateKey = candidate.Candidate.CandidateKey;

            if (_additionalGeneTooltips.TryGetValue(candidateKey, out string tooltip))
                return tooltip;

            var builder = new StringBuilder();
            builder.Append(
                "XenogermPlanner.Template.AdditionalGeneCount".Translate(candidate.SortedAdditionalGenes.Count)
                    .ToString());

            foreach (GeneDef gene in candidate.SortedAdditionalGenes)
            {
                builder.AppendLine();
                builder.Append("• ");
                builder.Append(XenogermPlannerPresentation.GetGeneDisplayName(gene));
            }

            tooltip = builder.ToString();
            _additionalGeneTooltips.Add(candidateKey, tooltip);
            return tooltip;
        }

        private VariableHeightScrollListLayout GetPreviewLayout(
            XenogermTemplateCandidatePresentation candidate,
            float viewWidth)
        {
            if (_previewLayoutCache.TryGet(candidate, 0, viewWidth, out VariableHeightScrollListLayout layout))
            {
                return layout;
            }

            var rows = new VariableHeightScrollListRowMeasurement[candidate.Compositions.Count];

            for (var compositionIndex = 0; compositionIndex < candidate.Compositions.Count; compositionIndex++)
            {
                rows[compositionIndex] = new VariableHeightScrollListRowMeasurement(
                    CalculateCompositionHeight(candidate.Compositions[compositionIndex]),
                    CompositionGap);
            }

            layout = VariableHeightScrollListLayout.Create(viewWidth, rows);
            _previewLayoutCache.Store(candidate, 0, layout);
            return layout;
        }

        private static float CalculateCompositionHeight(XenogermTemplateCompositionPresentation composition)
        {
            if (composition == null)
                throw new ArgumentNullException(nameof(composition));

            float geneRowsHeight = composition.SortedGenes.Count * GeneRowHeight;
            float contentGap = composition.SortedGenes.Count > 0 ? CompositionContentGap : 0f;

            return RimWorldUiStyle.Metrics.PanelPadding * 2f + CompositionHeaderHeight + contentGap + geneRowsHeight;
        }

        private void DrawCompositions(
            float width,
            XenogermTemplateCandidatePresentation candidate,
            VariableHeightScrollListLayout layout,
            VariableHeightScrollListVisibleRange visibleRange)
        {
            int lastVisibleCompositionIndexExclusive = Math.Min(
                visibleRange.LastVisibleIndexExclusive,
                candidate.Compositions.Count);

            for (int compositionIndex = visibleRange.FirstVisibleIndex;
                 compositionIndex < lastVisibleCompositionIndexExclusive;
                 compositionIndex++)
            {
                XenogermTemplateCompositionPresentation compositionPresentation =
                    candidate.Compositions[compositionIndex];
                PlanXenogermTemplateComposition composition = compositionPresentation.Composition;
                var compositionRect = new Rect(
                    0f,
                    layout.GetRowOffset(compositionIndex),
                    width,
                    layout.GetRowHeight(compositionIndex));

                RimWorldUiWidgets.DrawPanelBackground(compositionRect, nested: true);

                Rect contentRect = compositionRect.ContractedBy(RimWorldUiStyle.Metrics.PanelPadding);
                var fullHeaderRect = new Rect(contentRect.x, contentRect.y, contentRect.width, CompositionHeaderHeight);
                float compactBiostatWidth = Mathf.Min(
                    XenogermPlannerWidgets.CompactBiostatSummaryWidth,
                    fullHeaderRect.width * 0.45f);
                var biostatRect = new Rect(
                    fullHeaderRect.xMax - compactBiostatWidth,
                    fullHeaderRect.y,
                    compactBiostatWidth,
                    fullHeaderRect.height);
                var headerRect = new Rect(
                    fullHeaderRect.x,
                    fullHeaderRect.y,
                    Mathf.Max(0f, biostatRect.x - fullHeaderRect.x - CompositionHeaderBiostatGap),
                    fullHeaderRect.height);

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = RimWorldUiStyle.Colors.Accent;
                Widgets.Label(
                    headerRect,
                    "XenogermPlanner.Template.GeneSetHeader".Translate(
                        compositionIndex + 1,
                        composition.PhysicalPackCount));
                GUI.color = Color.white;

                XenogermPlannerWidgets.DrawCompactBiostatSummary(biostatRect, GetCompositionBiostats(composition));

                float geneY = fullHeaderRect.yMax;

                if (compositionPresentation.SortedGenes.Count > 0)
                    geneY += CompositionContentGap;

                for (var geneIndex = 0; geneIndex < compositionPresentation.SortedGenes.Count; geneIndex++)
                {
                    GeneDef gene = compositionPresentation.SortedGenes[geneIndex];
                    var rowRect = new Rect(contentRect.x, geneY, contentRect.width, GeneRowHeight);
                    bool isAdditional = compositionPresentation.IsAdditional(gene);

                    RimWorldUiWidgets.DrawSelectableRowBackground(
                        rowRect,
                        geneIndex,
                        selected: false,
                        hovered: Mouse.IsOver(rowRect),
                        drawAccent: false);

                    if (isAdditional)
                    {
                        const float AdditionalMarkerWidth = 90f;
                        var geneRect = new Rect(
                            rowRect.x,
                            rowRect.y,
                            Mathf.Max(0f, rowRect.width - AdditionalMarkerWidth),
                            rowRect.height);
                        var markerRect = new Rect(geneRect.xMax, rowRect.y, AdditionalMarkerWidth, rowRect.height);

                        using (ImGuiStateScope.Capture())
                        {
                            GUI.color = Color.white;
                            XenogermPlannerWidgets.DrawGeneLabel(geneRect, gene);

                            Text.Font = GameFont.Tiny;
                            Text.Anchor = TextAnchor.MiddleRight;
                            GUI.color = RimWorldUiStyle.Colors.Warning;
                            Widgets.Label(markerRect, "XenogermPlanner.Template.AdditionalGene".Translate());
                        }
                    }
                    else
                    {
                        using (ImGuiStateScope.Capture())
                        {
                            GUI.color = Color.white;
                            XenogermPlannerWidgets.DrawGeneLabel(rowRect, gene);
                        }
                    }

                    XenogermPlannerWidgets.AddGeneTooltip(rowRect, gene);
                    XenogermPlannerNativeInspector.TryOpenContextMenu(rowRect, gene);
                    geneY += GeneRowHeight;
                }

                RimWorldUiWidgets.DrawPanelBorder(compositionRect);
            }
        }

        private PlanXenogermTemplateBiostats GetCandidateBiostats(PlanXenogermTemplateCandidate candidate)
        {
            if (!_candidateBiostats.TryGetValue(candidate.CandidateKey, out PlanXenogermTemplateBiostats biostats))
            {
                biostats = PlanXenogermTemplateBiostatCalculator.CalculateCandidate(candidate);
                _candidateBiostats.Add(candidate.CandidateKey, biostats);
            }

            return biostats;
        }

        private PlanXenogermTemplateBiostats GetCompositionBiostats(PlanXenogermTemplateComposition composition)
        {
            if (!_compositionBiostats.TryGetValue(
                    composition.CompositionKey,
                    out PlanXenogermTemplateBiostats biostats))
            {
                biostats = PlanXenogermTemplateBiostatCalculator.CalculateComposition(composition);
                _compositionBiostats.Add(composition.CompositionKey, biostats);
            }

            return biostats;
        }

        private void RefreshPresentationProjection()
        {
            _presentationProjection = XenogermTemplatePresentationProjection.Build(_searchResult);
            _presentationLanguageKey = LanguageDatabase.activeLanguage;
            _additionalGeneTooltips.Clear();
            _candidateListLayoutCache.Invalidate();
            _previewLayoutCache.Invalidate();
        }

        private void RefreshLanguageDependentPresentationIfNeeded()
        {
            if (!Equals(_presentationLanguageKey, LanguageDatabase.activeLanguage))
                RefreshPresentationProjection();
        }

        private static float CalculateTextHeight(string text, float width, GameFont font)
        {
            using (ImGuiStateScope.Capture())
            {
                Text.Font = font;
                return Text.CalcHeight(text ?? string.Empty, Mathf.Max(1f, width));
            }
        }

        private void DrawFooter(Rect rect)
        {
            var cancelRect = new Rect(rect.x, rect.y, ButtonWidth, rect.height);
            var saveRect = new Rect(rect.xMax - ButtonWidth, rect.y, ButtonWidth, rect.height);

            if (Widgets.ButtonText(cancelRect, "XenogermPlanner.Template.Cancel".Translate()))
                Close();

            if (Widgets.ButtonText(saveRect, "XenogermPlanner.Template.Save".Translate()))
                TrySave();
        }

        private void TrySave()
        {
            XenogermTemplateCandidatePresentation candidatePresentation = SelectedCandidatePresentation;
            PlanXenogermTemplateCandidate candidate = candidatePresentation?.Candidate;

            if (candidate == null)
            {
                Messages.Message(
                    "XenogermPlanner.Template.NoCandidate".Translate(),
                    MessageTypeDefOf.RejectInput,
                    historical: false);
                return;
            }

            _inventoryComponent.Invalidate();
            PlanGenepackInventorySnapshot snapshot = _inventoryComponent.Snapshot;
            PlanXenogermTemplateSaveResult result = PlanXenogermTemplateSaver.Save(
                _plan,
                candidate,
                _templateName,
                _iconDef,
                snapshot);

            if (result.Succeeded)
            {
                Close(doCloseSound: false);
                return;
            }

            string message;

            if (result.Failure == PlanXenogermTemplateSaveFailure.VanillaRejected &&
                !string.IsNullOrEmpty(result.VanillaRejectionReason))
            {
                message = result.VanillaRejectionReason;
            }
            else
            {
                message = XenogermPlannerPresentation.GetTemplateSaveFailureTranslationKey(result.Failure)
                    .Translate()
                    .ToString();
            }

            Messages.Message(message, MessageTypeDefOf.RejectInput, historical: false);
        }
    }
}