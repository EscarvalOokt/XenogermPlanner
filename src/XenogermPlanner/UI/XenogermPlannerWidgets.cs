using System;
using System.Collections.Generic;
using Escarval.RimWorld.UI;
using RimWorld;
using UnityEngine;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Assemblers;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;
using XenogermPlanner.Templates;

namespace XenogermPlanner.UI
{
    [StaticConstructorOnStartup]
    internal static class XenogermPlannerWidgets
    {
        internal const float GenepackTargetIconSize = XenogermPlannerStyle.Metrics.GenepackTargetSize;
        internal const float GenepackTargetIconGap = XenogermPlannerStyle.Metrics.GenepackTargetGap;

        private const float GeneIconSize = XenogermPlannerStyle.Metrics.GeneIconSize;
        private const float CollapsibleSectionIconSize = 18f;
        private const float GeneIconGap = XenogermPlannerStyle.Metrics.GeneIconGap;
        private const float ReadinessOptionGap = 24f;
        private const float ReadinessRadioPadding = 30f;
        private const float GenepackIconSize = 24f;
        private const float GenepackStatusBarHeight = 2f;
        private const float DiagnosticPadding = 8f;
        private const float DiagnosticTitleHeight = 26f;
        private const float DiagnosticLineGap = 4f;
        private const float DiagnosticMinLineHeight = 24f;
        private const float DiagnosticIconGap = 4f;
        private const float DiagnosticTextGap = 8f;
        private const float DiagnosticGroupMessageGap = 6f;
        private const float DiagnosticPanelWidthFraction = 0.36f;
        private const float DiagnosticPanelMaxWidth = 340f;
        private const float BiostatIconSize = 18f;
        private const float BiostatLabelGap = 6f;
        private const float BiostatValueGap = 6f;
        private const float BiostatCompactEntryGap = 6f;
        private const float PlanEditorBiostatMetabolismWidthFactor = 1.45f;
        private const float BiostatMinimumValueWidth = 24f;
        private const float BiostatSummaryRowHeight = 22f;
        internal const float LabeledBiostatSummaryHeight = BiostatSummaryRowHeight * 3f;
        internal const float CompactBiostatSummaryWidth = 156f;

        internal static bool DrawCollapsibleSectionRow(
            Rect rect,
            string label,
            bool expanded,
            int rowIndex,
            bool enabled = true)
        {
            if (label == null)
                throw new ArgumentNullException(nameof(label));

            using (ImGuiStateScope.Capture())
            {
                RimWorldUiWidgets.DrawSelectableRowBackground(
                    rect,
                    rowIndex,
                    selected: false,
                    hovered: Mouse.IsOver(rect),
                    drawAccent: false);

                var iconRect = new Rect(
                    rect.x,
                    rect.y + (rect.height - CollapsibleSectionIconSize) * 0.5f,
                    CollapsibleSectionIconSize,
                    CollapsibleSectionIconSize);

                var labelRect = new Rect(
                    iconRect.xMax + RimWorldUiStyle.Metrics.SmallGap,
                    rect.y,
                    Mathf.Max(0f, rect.xMax - iconRect.xMax - RimWorldUiStyle.Metrics.SmallGap),
                    rect.height);

                RimWorldUiWidgets.DrawIcon(iconRect, expanded ? TexButton.Collapse : TexButton.Reveal);

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, label);

                return enabled && Event.current.button == 0 && Widgets.ButtonInvisible(rect);
            }
        }

        internal static Color GetReadinessStatusColor(PlanReadinessStatus status)
        {
            switch (status)
            {
                case PlanReadinessStatus.Ready:
                    return RimWorldUiStyle.Colors.Positive;

                case PlanReadinessStatus.NotReady:
                    return RimWorldUiStyle.Colors.Negative;

                case PlanReadinessStatus.Degraded:
                    return RimWorldUiStyle.Colors.Warning;

                case PlanReadinessStatus.EmptyTarget:
                case PlanReadinessStatus.Unavailable:
                default:
                    return RimWorldUiStyle.Colors.Neutral;
            }
        }

        internal static Color GetAssemblerReadinessStatusColor(PlanAssemblerReadinessStatus status)
        {
            switch (status)
            {
                case PlanAssemblerReadinessStatus.Ready:
                    return RimWorldUiStyle.Colors.Positive;

                case PlanAssemblerReadinessStatus.NotReady:
                    return RimWorldUiStyle.Colors.Negative;

                case PlanAssemblerReadinessStatus.Blocked:
                case PlanAssemblerReadinessStatus.Degraded:
                case PlanAssemblerReadinessStatus.SearchIncomplete:
                    return RimWorldUiStyle.Colors.Warning;

                case PlanAssemblerReadinessStatus.EmptyTarget:
                default:
                    return RimWorldUiStyle.Colors.Neutral;
            }
        }

        internal static Color GetGeneCoverageStateColor(PlanGeneCoverageState state)
        {
            switch (state)
            {
                case PlanGeneCoverageState.Available:
                    return RimWorldUiStyle.Colors.PrimaryText;

                case PlanGeneCoverageState.ExactPayloadConflict:
                    return RimWorldUiStyle.Colors.Negative;

                case PlanGeneCoverageState.Missing:
                    return RimWorldUiStyle.Colors.Warning;

                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported gene coverage state.");
            }
        }

        internal static void DrawGeneLabel(Rect rect, GeneDef gene)
        {
            DrawGeneLabel(
                rect,
                gene,
                XenogermPlannerPresentation.GetGeneDisplayName(gene),
                GameFont.Small,
                GeneIconSize,
                GeneIconGap);
        }

        internal static void DrawGeneLabel(
            Rect rect,
            GeneDef gene,
            string label,
            GameFont font,
            float iconSize,
            float iconGap)
        {
            if (gene == null)
                throw new ArgumentNullException(nameof(gene));

            if (label == null)
                throw new ArgumentNullException(nameof(label));

            if (iconSize < 0f)
                throw new ArgumentOutOfRangeException(nameof(iconSize), iconSize, "Gene icon size cannot be negative.");

            if (iconGap < 0f)
                throw new ArgumentOutOfRangeException(nameof(iconGap), iconGap, "Gene icon gap cannot be negative.");

            float actualIconSize = Mathf.Min(iconSize, rect.height);

            var iconRect = new Rect(
                rect.x,
                rect.y + (rect.height - actualIconSize) * 0.5f,
                actualIconSize,
                actualIconSize);

            var labelRect = new Rect(
                iconRect.xMax + iconGap,
                rect.y,
                Mathf.Max(0f, rect.xMax - iconRect.xMax - iconGap),
                rect.height);

            Widgets.DefIcon(iconRect, gene, null, 0.9f, null, drawPlaceholder: false, gene.IconColor);

            using (ImGuiStateScope.Capture())
            {
                Text.Font = font;
                Text.Anchor = TextAnchor.MiddleLeft;

                Widgets.Label(labelRect, label);
            }
        }

        internal static void AddGeneTooltip(Rect rect, GeneDef gene, string actionHint = null)
        {
            if (gene == null)
                throw new ArgumentNullException(nameof(gene));

            string tooltip = gene.LabelCap.Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + gene.DescriptionFull;

            if (!string.IsNullOrWhiteSpace(actionHint))
            {
                tooltip = tooltip + "\n\n" + actionHint.Colorize(RimWorldUiStyle.Colors.MutedText);
            }

            tooltip = tooltip +
                      "\n\n" +
                      "XenogermPlanner.Planner.OpenInfoCardHint".Translate()
                          .ToString()
                          .Colorize(RimWorldUiStyle.Colors.MutedText);

            TooltipHandler.TipRegion(rect, tooltip);
        }

        internal static void DrawThingTargetIcon(Rect rect, Thing target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (Mouse.IsOver(rect))
                Widgets.DrawBoxSolid(rect, RimWorldUiStyle.Colors.HoverBackground);

            var iconRect = new Rect(
                rect.x + (rect.width - GenepackIconSize) * 0.5f,
                rect.y + (rect.height - GenepackIconSize) * 0.5f,
                GenepackIconSize,
                GenepackIconSize);

            Widgets.ThingIcon(iconRect, target);

            AddTargetNavigationTooltip(rect, target);
            HandleTargetInteraction(rect, target);
        }

        internal static bool DrawThingTargetRow(Rect rowRect, Rect iconContainerRect, Thing target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            bool isHovered = Mouse.IsOver(rowRect);

            if (isHovered)
                Widgets.DrawBoxSolid(rowRect, RimWorldUiStyle.Colors.HoverBackground);

            var iconRect = new Rect(
                iconContainerRect.x + (iconContainerRect.width - GenepackIconSize) * 0.5f,
                iconContainerRect.y + (iconContainerRect.height - GenepackIconSize) * 0.5f,
                GenepackIconSize,
                GenepackIconSize);

            Widgets.ThingIcon(iconRect, target);

            AddTargetNavigationTooltip(rowRect, target);

            if (Event.current.button == 0 && Widgets.ButtonInvisible(rowRect))
                XenogermPlannerTargetInteraction.TryNavigate(target);

            return isHovered;
        }

        internal static bool DrawGenepackTargetIcon(
            Rect rect,
            Genepack target,
            PlanGenepackCompositionDiagnostic composition,
            PlanReadinessMode readinessMode)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (composition == null)
                throw new ArgumentNullException(nameof(composition));

            if (Mouse.IsOver(rect))
                Widgets.DrawBoxSolid(rect, RimWorldUiStyle.Colors.HoverBackground);

            var iconRect = new Rect(
                rect.x + (rect.width - GenepackIconSize) * 0.5f,
                rect.y + (rect.height - GenepackStatusBarHeight - GenepackIconSize) * 0.5f,
                GenepackIconSize,
                GenepackIconSize);

            Widgets.ThingIcon(iconRect, target);

            var statusBarRect = new Rect(
                rect.x,
                rect.yMax - GenepackStatusBarHeight,
                rect.width,
                GenepackStatusBarHeight);

            Widgets.DrawBoxSolid(statusBarRect, GetGenepackStatusColor(composition, readinessMode));

            bool isHovered = Mouse.IsOver(rect);

            if (isHovered)
            {
                XenogermPlannerTargetInteraction.Highlight(target);
            }

            if (!XenogermPlannerNativeInspector.TryOpenContextMenu(rect, target) && Widgets.ButtonInvisible(rect))
            {
                XenogermPlannerTargetInteraction.TryNavigate(target);
            }

            return isHovered;
        }

        internal static void DrawGenepackCompositionTooltip(
            Rect bounds,
            PlanGenepackCompositionDiagnostic composition,
            PlanReadinessMode readinessMode)
        {
            if (composition == null)
                throw new ArgumentNullException(nameof(composition));

            XenogermPlannerPresentation.GetSortedGenepackGeneGroups(
                composition.Genes,
                out List<GeneDef> nonArchiteGenes,
                out List<GeneDef> architeGenes);

            bool hasNonArchiteGenes = nonArchiteGenes.Count > 0;
            bool hasArchiteGenes = architeGenes.Count > 0;
            bool hasBothGeneGroups = hasNonArchiteGenes && hasArchiteGenes;

            float tooltipPadding = RimWorldUiStyle.Metrics.TooltipPadding;
            float groupGap = XenogermPlannerStyle.Metrics.TooltipGroupGap;
            float verticalGroupGap = XenogermPlannerStyle.Metrics.TooltipGroupVerticalGap;
            float tooltipGap = RimWorldUiStyle.Metrics.TooltipGap;
            float availableTooltipWidth = Mathf.Max(1f, bounds.width);
            float minimumTwoColumnWidth = tooltipPadding * 2f +
                                          XenogermPlannerStyle.Metrics.TooltipMinimumGroupColumnWidth * 2f +
                                          groupGap;
            bool useTwoColumns = hasBothGeneGroups && availableTooltipWidth >= minimumTwoColumnWidth;
            float preferredTooltipWidth = useTwoColumns
                ? XenogermPlannerStyle.Metrics.TooltipWideWidth
                : RimWorldUiStyle.Metrics.TooltipWidth;
            float tooltipWidth = Mathf.Min(preferredTooltipWidth, availableTooltipWidth);
            float contentWidth = Mathf.Max(1f, tooltipWidth - tooltipPadding * 2f);

            float nonArchiteGroupHeight = hasNonArchiteGenes
                ? CalculateTooltipGeneGroupHeight(nonArchiteGenes.Count)
                : 0f;
            float architeGroupHeight = hasArchiteGenes ? CalculateTooltipGeneGroupHeight(architeGenes.Count) : 0f;
            float geneGroupsHeight;

            if (useTwoColumns)
            {
                geneGroupsHeight = Mathf.Max(nonArchiteGroupHeight, architeGroupHeight);
            }
            else
            {
                geneGroupsHeight = nonArchiteGroupHeight + architeGroupHeight;

                if (hasBothGeneGroups)
                    geneGroupsHeight += verticalGroupGap;
            }

            float compatibilityHeight = readinessMode == PlanReadinessMode.ExactPayload
                ? CalculateTextHeight(
                    XenogermPlannerPresentation.GetGenepackExactCompatibilityMessage(composition),
                    contentWidth,
                    GameFont.Small)
                : 0f;

            var navigationHint = "XenogermPlanner.Planner.TargetNavigationHint".Translate().ToString();
            var inspectionHint = "XenogermPlanner.Planner.OpenInfoCardHint".Translate().ToString();
            string interactionHint = navigationHint + "\n" + inspectionHint;
            float interactionHintHeight = CalculateTextHeight(interactionHint, contentWidth, GameFont.Tiny);
            float tooltipHeight = tooltipPadding +
                                  RimWorldUiStyle.Metrics.TooltipTitleHeight +
                                  tooltipGap +
                                  RimWorldUiStyle.Metrics.TooltipLineHeight +
                                  tooltipGap +
                                  geneGroupsHeight;

            if (compatibilityHeight > 0f)
                tooltipHeight += tooltipGap + compatibilityHeight;

            tooltipHeight += tooltipGap + interactionHintHeight + tooltipPadding;

            Rect tooltipRect = GetTooltipRect(bounds, tooltipWidth, tooltipHeight);
            using (ImGuiStateScope.Capture())
            {
                RimWorldUiWidgets.DrawTooltipPanel(tooltipRect);

                Rect contentRect = tooltipRect.ContractedBy(tooltipPadding);
                float y = contentRect.y;

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = RimWorldUiStyle.Colors.TooltipTitle;

                Widgets.Label(
                    new Rect(contentRect.x, y, contentRect.width, RimWorldUiStyle.Metrics.TooltipTitleHeight),
                    "XenogermPlanner.Planner.GenepackTooltipTitle".Translate());

                y += RimWorldUiStyle.Metrics.TooltipTitleHeight + tooltipGap;

                GUI.color = RimWorldUiStyle.Colors.PrimaryText;

                Widgets.Label(
                    new Rect(contentRect.x, y, contentRect.width, RimWorldUiStyle.Metrics.TooltipLineHeight),
                    "XenogermPlanner.Planner.StoredGenepackCount".Translate(composition.PhysicalPackCount));

                y += RimWorldUiStyle.Metrics.TooltipLineHeight + tooltipGap;

                if (useTwoColumns)
                {
                    float columnWidth = (contentRect.width - groupGap) * 0.5f;

                    DrawTooltipGeneGroup(
                        new Rect(contentRect.x, y, columnWidth, nonArchiteGroupHeight),
                        "XenogermPlanner.Planner.GenepackNonArchiteGenes".Translate().ToString(),
                        nonArchiteGenes,
                        composition,
                        readinessMode);

                    DrawTooltipGeneGroup(
                        new Rect(contentRect.x + columnWidth + groupGap, y, columnWidth, architeGroupHeight),
                        "XenogermPlanner.Planner.GenepackArchiteGenes".Translate().ToString(),
                        architeGenes,
                        composition,
                        readinessMode);
                }
                else
                {
                    if (hasNonArchiteGenes)
                    {
                        DrawTooltipGeneGroup(
                            new Rect(contentRect.x, y, contentRect.width, nonArchiteGroupHeight),
                            "XenogermPlanner.Planner.GenepackNonArchiteGenes".Translate().ToString(),
                            nonArchiteGenes,
                            composition,
                            readinessMode);

                        y += nonArchiteGroupHeight;
                    }

                    if (hasBothGeneGroups)
                        y += verticalGroupGap;

                    if (hasArchiteGenes)
                    {
                        DrawTooltipGeneGroup(
                            new Rect(contentRect.x, y, contentRect.width, architeGroupHeight),
                            "XenogermPlanner.Planner.GenepackArchiteGenes".Translate().ToString(),
                            architeGenes,
                            composition,
                            readinessMode);
                    }
                }

                if (useTwoColumns)
                    y += geneGroupsHeight;
                else if (hasArchiteGenes)
                    y += architeGroupHeight;

                if (compatibilityHeight > 0f)
                {
                    y += tooltipGap;
                    GUI.color = composition.IsExactPayloadEligible
                        ? RimWorldUiStyle.Colors.Positive
                        : RimWorldUiStyle.Colors.Negative;
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;

                    Widgets.Label(
                        new Rect(contentRect.x, y, contentRect.width, compatibilityHeight),
                        XenogermPlannerPresentation.GetGenepackExactCompatibilityMessage(composition));

                    y += compatibilityHeight;
                }

                y += tooltipGap;
                GUI.color = RimWorldUiStyle.Colors.MutedText;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperLeft;

                Widgets.Label(new Rect(contentRect.x, y, contentRect.width, interactionHintHeight), interactionHint);
            }
        }

        internal static float CalculateGeneTargetDiagnosticsPanelWidth(float availableWidth)
        {
            if (availableWidth <= 0f)
                return 0f;

            return Mathf.Min(DiagnosticPanelMaxWidth, availableWidth * DiagnosticPanelWidthFraction);
        }

        internal static void DrawGeneTargetDiagnosticsPanel(
            Rect rect,
            GeneTargetDiagnosticsProjection projection,
            VariableHeightScrollListLayoutCache layoutCache,
            ref Vector2 scrollPosition)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));

            if (layoutCache == null)
                throw new ArgumentNullException(nameof(layoutCache));

            if (!projection.HasDiagnostics || rect.width <= 0f || rect.height <= 0f)
                return;

            RimWorldUiWidgets.DrawPanel(rect);

            Rect contentRect = rect.ContractedBy(DiagnosticPadding);
            var titleRect = new Rect(contentRect.x, contentRect.y, contentRect.width, DiagnosticTitleHeight);

            var scrollRect = new Rect(
                contentRect.x,
                titleRect.yMax,
                contentRect.width,
                Mathf.Max(0f, contentRect.yMax - titleRect.yMax));

            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = ColoredText.TipSectionTitleColor;

                Widgets.Label(titleRect, "XenogermPlanner.GeneDiagnostics.Title".Translate());

                if (scrollRect.height <= 0f)
                    return;

                float viewWidth = Mathf.Max(0f, scrollRect.width - RimWorldUiStyle.Metrics.ScrollbarWidth);

                if (!layoutCache.TryGet(projection, 0, viewWidth, out VariableHeightScrollListLayout layout))
                {
                    layout = BuildDiagnosticLayout(viewWidth, projection);
                    layoutCache.Store(projection, 0, layout);
                }

                VariableHeightScrollListVisibleRange visibleRange = RimWorldUiWidgets.BeginVariableHeightScrollView(
                    scrollRect,
                    ref scrollPosition,
                    layout,
                    out _);

                DrawDiagnosticRows(viewWidth, projection, layout, visibleRange);

                Widgets.EndScrollView();
            }
        }

        internal static void DrawReadinessModeSelector(Rect rect, ref PlanReadinessMode readinessMode)
        {
            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;

                string coverageLabel = XenogermPlannerPresentation.GetReadinessModeLabel(PlanReadinessMode.Coverage);

                string exactPayloadLabel =
                    XenogermPlannerPresentation.GetReadinessModeLabel(PlanReadinessMode.ExactPayload);

                float coverageWidth = Text.CalcSize(coverageLabel).x + ReadinessRadioPadding;

                float exactPayloadWidth = Text.CalcSize(exactPayloadLabel).x + ReadinessRadioPadding;

                var coverageRect = new Rect(rect.x, rect.y, coverageWidth, rect.height);

                var exactPayloadRect = new Rect(
                    coverageRect.xMax + ReadinessOptionGap,
                    rect.y,
                    exactPayloadWidth,
                    rect.height);

                if (Widgets.RadioButtonLabeled(
                        coverageRect,
                        coverageLabel,
                        readinessMode == PlanReadinessMode.Coverage))
                {
                    readinessMode = PlanReadinessMode.Coverage;
                }

                TooltipHandler.TipRegion(
                    coverageRect,
                    XenogermPlannerPresentation.GetReadinessModeDescription(PlanReadinessMode.Coverage));

                if (Widgets.RadioButtonLabeled(
                        exactPayloadRect,
                        exactPayloadLabel,
                        readinessMode == PlanReadinessMode.ExactPayload))
                {
                    readinessMode = PlanReadinessMode.ExactPayload;
                }

                TooltipHandler.TipRegion(
                    exactPayloadRect,
                    XenogermPlannerPresentation.GetReadinessModeDescription(PlanReadinessMode.ExactPayload));
            }
        }

        private static VariableHeightScrollListLayout BuildDiagnosticLayout(
            float viewWidth,
            GeneTargetDiagnosticsProjection projection)
        {
            var rows = new VariableHeightScrollListRowMeasurement[projection.Rows.Count];

            for (var index = 0; index < projection.Rows.Count; index++)
            {
                GeneTargetDiagnosticPresentationRow row = projection.Rows[index];
                float height = row.Kind == GeneTargetDiagnosticPresentationRowKind.RandomChoiceGroup
                    ? CalculateRandomChoiceGroupRowHeight(row.Genes.Count, row.Message, viewWidth)
                    : CalculateDiagnosticRowHeight(row.Message, viewWidth);

                rows[index] = new VariableHeightScrollListRowMeasurement(
                    height,
                    index + 1 < projection.Rows.Count ? DiagnosticLineGap : 0f);
            }

            return VariableHeightScrollListLayout.Create(viewWidth, rows);
        }

        private static void DrawDiagnosticRows(
            float viewWidth,
            GeneTargetDiagnosticsProjection projection,
            VariableHeightScrollListLayout layout,
            VariableHeightScrollListVisibleRange visibleRange)
        {
            for (int rowIndex = visibleRange.FirstVisibleIndex;
                 rowIndex < visibleRange.LastVisibleIndexExclusive;
                 rowIndex++)
            {
                GeneTargetDiagnosticPresentationRow row = projection.Rows[rowIndex];
                var rowRect = new Rect(0f, layout.GetRowOffset(rowIndex), viewWidth, layout.GetRowHeight(rowIndex));

                switch (row.Kind)
                {
                    case GeneTargetDiagnosticPresentationRowKind.Conflict:
                        DrawGeneDiagnosticRow(
                            rowRect,
                            row.FirstGene,
                            row.SecondGene,
                            row.Message,
                            GetConflictDiagnosticColor(row.ConflictDiagnostic));
                        break;

                    case GeneTargetDiagnosticPresentationRowKind.RandomChoiceGroup:
                        DrawRandomChoiceGroupRow(rowRect, row.Genes, row.Message, RimWorldUiStyle.Colors.Warning);
                        break;

                    case GeneTargetDiagnosticPresentationRowKind.Prerequisite:
                        DrawGeneDiagnosticRow(
                            rowRect,
                            row.FirstGene,
                            row.SecondGene,
                            row.Message,
                            RimWorldUiStyle.Colors.Warning);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(row.Kind),
                            row.Kind,
                            "Unsupported diagnostic row.");
                }
            }
        }

        private static float CalculateDiagnosticRowHeight(string message, float viewWidth)
        {
            float textWidth = GetDiagnosticTextWidth(viewWidth);
            float textHeight = CalculateTextHeight(message, textWidth, GameFont.Small);

            return Mathf.Max(DiagnosticMinLineHeight, GeneIconSize, textHeight);
        }

        private static float CalculateRandomChoiceGroupRowHeight(int geneCount, string message, float viewWidth)
        {
            float iconFlowHeight = CalculateGeneIconFlowHeight(geneCount, viewWidth);
            float textHeight = CalculateTextHeight(message, Mathf.Max(1f, viewWidth), GameFont.Small);

            return Mathf.Max(DiagnosticMinLineHeight, iconFlowHeight + DiagnosticGroupMessageGap + textHeight);
        }

        private static void DrawGeneDiagnosticRow(
            Rect rect,
            GeneDef firstGene,
            GeneDef secondGene,
            string message,
            Color messageColor)
        {
            if (firstGene == null)
                throw new ArgumentNullException(nameof(firstGene));

            if (secondGene == null)
                throw new ArgumentNullException(nameof(secondGene));

            float iconY = rect.y + (rect.height - GeneIconSize) * 0.5f;
            var firstIconRect = new Rect(rect.x, iconY, GeneIconSize, GeneIconSize);
            var secondIconRect = new Rect(firstIconRect.xMax + DiagnosticIconGap, iconY, GeneIconSize, GeneIconSize);
            var messageRect = new Rect(
                secondIconRect.xMax + DiagnosticTextGap,
                rect.y,
                Mathf.Max(0f, rect.xMax - secondIconRect.xMax - DiagnosticTextGap),
                rect.height);

            DrawGeneIcon(firstIconRect, firstGene);
            DrawGeneIcon(secondIconRect, secondGene);

            GUI.color = messageColor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            Widgets.Label(messageRect, message);
        }

        private static void DrawRandomChoiceGroupRow(
            Rect rect,
            IReadOnlyList<GeneDef> sortedGenes,
            string message,
            Color messageColor)
        {
            if (sortedGenes == null)
                throw new ArgumentNullException(nameof(sortedGenes));

            float iconFlowHeight = CalculateGeneIconFlowHeight(sortedGenes.Count, rect.width);
            var iconFlowRect = new Rect(rect.x, rect.y, rect.width, iconFlowHeight);
            var messageRect = new Rect(
                rect.x,
                iconFlowRect.yMax + DiagnosticGroupMessageGap,
                rect.width,
                Mathf.Max(0f, rect.yMax - iconFlowRect.yMax - DiagnosticGroupMessageGap));

            DrawGeneIconFlow(iconFlowRect, sortedGenes);

            GUI.color = messageColor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            Widgets.Label(messageRect, message);
        }

        internal static void DrawPlanEditorBiostatSummary(Rect rect, PlanGeneBiostats biostats, bool isPartial)
        {
            if (biostats == null)
                throw new ArgumentNullException(nameof(biostats));

            bool previousWordWrap = Text.WordWrap;

            using (ImGuiStateScope.Capture())
            {
                try
                {
                    RimWorldUiWidgets.DrawPanel(rect);

                    float horizontalPadding = RimWorldUiStyle.Metrics.PanelPadding;
                    float verticalPadding = RimWorldUiStyle.Metrics.SmallGap;
                    var innerRect = new Rect(
                        rect.x + horizontalPadding,
                        rect.y + verticalPadding,
                        Mathf.Max(0f, rect.width - horizontalPadding * 2f),
                        Mathf.Max(0f, rect.height - verticalPadding * 2f));

                    float gap = RimWorldUiStyle.Metrics.SectionGap;
                    float availableWidth = Mathf.Max(0f, innerRect.width - gap * 2f);
                    float standardWidth = availableWidth / (2f + PlanEditorBiostatMetabolismWidthFactor);
                    float metabolismWidth = standardWidth * PlanEditorBiostatMetabolismWidthFactor;

                    var complexityRect = new Rect(innerRect.x, innerRect.y, standardWidth, innerRect.height);
                    var metabolismRect = new Rect(
                        complexityRect.xMax + gap,
                        innerRect.y,
                        metabolismWidth,
                        innerRect.height);
                    var architeRect = new Rect(
                        metabolismRect.xMax + gap,
                        innerRect.y,
                        Mathf.Max(0f, innerRect.xMax - metabolismRect.xMax - gap),
                        innerRect.height);

                    string partialSuffix = isPartial
                        ? "XenogermPlanner.PlanEditor.Biostats.PartialSuffix".Translate().ToString()
                        : null;
                    string partialTooltip = isPartial
                        ? "XenogermPlanner.PlanEditor.Biostats.PartialTooltip".Translate().ToString()
                        : null;

                    DrawPlanEditorBiostatEntry(
                        complexityRect,
                        GeneUtility.GCXTex.Texture,
                        GeneUtility.GCXColor,
                        "XenogermPlanner.Template.Biostats.Complexity".Translate().ToString(),
                        biostats.Complexity.ToString(),
                        null,
                        "XenogermPlanner.PlanEditor.Biostats.ComplexityTooltip".Translate().ToString(),
                        partialSuffix,
                        partialTooltip);

                    DrawPlanEditorBiostatEntry(
                        metabolismRect,
                        GeneUtility.METTex.Texture,
                        GeneUtility.METColor,
                        "XenogermPlanner.Template.Biostats.MetabolicEfficiency".Translate().ToString(),
                        XenogermPlannerPresentation.FormatMetabolism(biostats.Metabolism),
                        XenogermPlannerPresentation.GetHungerRateSummary(biostats.Metabolism),
                        "XenogermPlanner.PlanEditor.Biostats.MetabolicEfficiencyTooltip".Translate().ToString(),
                        partialSuffix,
                        partialTooltip);

                    DrawPlanEditorBiostatEntry(
                        architeRect,
                        GeneUtility.ARCTex.Texture,
                        GeneUtility.ARCColor,
                        "XenogermPlanner.Template.Biostats.ArchiteCapsules".Translate().ToString(),
                        biostats.ArchiteCapsules.ToString(),
                        null,
                        "XenogermPlanner.Template.Biostats.ArchiteCapsulesTooltip".Translate().ToString(),
                        partialSuffix,
                        partialTooltip);

                    float dividerOffset = gap * 0.5f;
                    RimWorldUiWidgets.DrawTableDivider(
                        new Rect(
                            complexityRect.xMax + dividerOffset,
                            innerRect.y,
                            RimWorldUiStyle.Metrics.TableDividerWidth,
                            innerRect.height));
                    RimWorldUiWidgets.DrawTableDivider(
                        new Rect(
                            metabolismRect.xMax + dividerOffset,
                            innerRect.y,
                            RimWorldUiStyle.Metrics.TableDividerWidth,
                            innerRect.height));
                }
                finally
                {
                    Text.WordWrap = previousWordWrap;
                }
            }
        }

        private static void DrawPlanEditorBiostatEntry(
            Rect rect,
            Texture2D texture,
            Color color,
            string label,
            string value,
            string secondaryText,
            string tooltip,
            string partialSuffix,
            string partialTooltip)
        {
            float iconSize = Mathf.Min(BiostatIconSize, rect.height);
            var iconRect = new Rect(rect.x, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize);
            var textRect = new Rect(
                iconRect.xMax + BiostatLabelGap,
                rect.y,
                Mathf.Max(0f, rect.xMax - iconRect.xMax - BiostatLabelGap),
                rect.height);
            float lineHeight = textRect.height * 0.5f;
            var labelRect = new Rect(textRect.x, textRect.y, textRect.width, lineHeight);
            var valueRect = new Rect(textRect.x, labelRect.yMax, textRect.width, textRect.yMax - labelRect.yMax);

            DrawBiostatIcon(iconRect, texture, color);

            Text.WordWrap = false;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = RimWorldUiStyle.Colors.PrimaryText;
            Widgets.Label(labelRect, label);

            string valueLine = (value ?? string.Empty).Colorize(color);

            if (!string.IsNullOrEmpty(secondaryText))
                valueLine += "  " + secondaryText;

            if (!string.IsNullOrEmpty(partialSuffix))
                valueLine += "  " + partialSuffix.Colorize(RimWorldUiStyle.Colors.Warning);

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(valueRect, valueLine);

            string completeTooltip = tooltip ?? string.Empty;

            if (!string.IsNullOrEmpty(partialTooltip))
            {
                completeTooltip += "\n\n" + partialTooltip.Colorize(RimWorldUiStyle.Colors.Warning);
            }

            TooltipHandler.TipRegion(rect, completeTooltip);
        }

        internal static void DrawLabeledBiostatSummary(Rect rect, PlanXenogermTemplateBiostats biostats)
        {
            if (biostats == null)
                throw new ArgumentNullException(nameof(biostats));

            using (ImGuiStateScope.Capture())
            {
                float rowHeight = rect.height / 3f;

                DrawLabeledBiostatEntry(
                    new Rect(rect.x, rect.y, rect.width, rowHeight),
                    GeneUtility.GCXTex.Texture,
                    GeneUtility.GCXColor,
                    "XenogermPlanner.Template.Biostats.Complexity".Translate().ToString(),
                    biostats.Complexity.ToString(),
                    "XenogermPlanner.Template.Biostats.ComplexityTooltip".Translate().ToString());

                DrawLabeledBiostatEntry(
                    new Rect(rect.x, rect.y + rowHeight, rect.width, rowHeight),
                    GeneUtility.METTex.Texture,
                    GeneUtility.METColor,
                    "XenogermPlanner.Template.Biostats.MetabolicEfficiency".Translate().ToString(),
                    XenogermPlannerPresentation.FormatMetabolism(biostats.Metabolism),
                    "XenogermPlanner.Template.Biostats.MetabolicEfficiencyTooltip".Translate().ToString());

                DrawLabeledBiostatEntry(
                    new Rect(rect.x, rect.y + rowHeight * 2f, rect.width, rowHeight),
                    GeneUtility.ARCTex.Texture,
                    GeneUtility.ARCColor,
                    "XenogermPlanner.Template.Biostats.ArchiteCapsules".Translate().ToString(),
                    biostats.ArchiteCapsules.ToString(),
                    "XenogermPlanner.Template.Biostats.ArchiteCapsulesTooltip".Translate().ToString());
            }
        }

        internal static void DrawCompactBiostatSummary(Rect rect, PlanXenogermTemplateBiostats biostats)
        {
            if (biostats == null)
                throw new ArgumentNullException(nameof(biostats));

            using (ImGuiStateScope.Capture())
            {
                float entryWidth = Mathf.Max(0f, (rect.width - BiostatCompactEntryGap * 2f) / 3f);
                var geneSetNote = "XenogermPlanner.Template.Biostats.GeneSetTooltip".Translate().ToString();

                DrawCompactBiostatEntry(
                    new Rect(rect.x, rect.y, entryWidth, rect.height),
                    GeneUtility.GCXTex.Texture,
                    GeneUtility.GCXColor,
                    biostats.Complexity.ToString(),
                    BuildBiostatTooltip(
                        "XenogermPlanner.Template.Biostats.ComplexityTooltip".Translate().ToString(),
                        geneSetNote));

                DrawCompactBiostatEntry(
                    new Rect(rect.x + entryWidth + BiostatCompactEntryGap, rect.y, entryWidth, rect.height),
                    GeneUtility.METTex.Texture,
                    GeneUtility.METColor,
                    XenogermPlannerPresentation.FormatMetabolism(biostats.Metabolism),
                    BuildBiostatTooltip(
                        "XenogermPlanner.Template.Biostats.MetabolicEfficiencyTooltip".Translate().ToString(),
                        geneSetNote));

                DrawCompactBiostatEntry(
                    new Rect(rect.x + (entryWidth + BiostatCompactEntryGap) * 2f, rect.y, entryWidth, rect.height),
                    GeneUtility.ARCTex.Texture,
                    GeneUtility.ARCColor,
                    biostats.ArchiteCapsules.ToString(),
                    BuildBiostatTooltip(
                        "XenogermPlanner.Template.Biostats.ArchiteCapsulesTooltip".Translate().ToString(),
                        geneSetNote));
            }
        }

        private static void DrawLabeledBiostatEntry(
            Rect rect,
            Texture2D texture,
            Color color,
            string label,
            string value,
            string tooltip)
        {
            float iconSize = Mathf.Min(BiostatIconSize, rect.height);
            var iconRect = new Rect(rect.x, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize);

            Text.Font = GameFont.Tiny;
            float measuredValueWidth = Text.CalcSize(value ?? string.Empty).x + 4f;
            float valueWidth = Mathf.Max(BiostatMinimumValueWidth, measuredValueWidth);
            var valueRect = new Rect(rect.xMax - valueWidth, rect.y, valueWidth, rect.height);
            var labelRect = new Rect(
                iconRect.xMax + BiostatLabelGap,
                rect.y,
                Mathf.Max(0f, valueRect.x - iconRect.xMax - BiostatLabelGap - BiostatValueGap),
                rect.height);

            DrawBiostatIcon(iconRect, texture, color);

            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = Color.white;
            Widgets.Label(labelRect, label);

            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = color;
            Widgets.Label(valueRect, value);
            GUI.color = Color.white;

            TooltipHandler.TipRegion(rect, tooltip);
        }

        private static void DrawCompactBiostatEntry(
            Rect rect,
            Texture2D texture,
            Color color,
            string value,
            string tooltip)
        {
            float iconSize = Mathf.Min(BiostatIconSize, rect.height);
            var iconRect = new Rect(rect.x, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize);
            var valueRect = new Rect(
                iconRect.xMax + BiostatValueGap,
                rect.y,
                Mathf.Max(0f, rect.xMax - iconRect.xMax - BiostatValueGap),
                rect.height);

            DrawBiostatIcon(iconRect, texture, color);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = color;
            Widgets.Label(valueRect, value);
            GUI.color = Color.white;

            TooltipHandler.TipRegion(rect, tooltip);
        }

        private static void DrawBiostatIcon(Rect rect, Texture2D texture, Color color)
        {
            using (ImGuiStateScope.Capture())
            {
                GUI.color = color;
                GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
            }
        }

        private static string BuildBiostatTooltip(string tooltip, string suffix)
        {
            if (string.IsNullOrEmpty(suffix))
                return tooltip ?? string.Empty;

            return (tooltip ?? string.Empty) + "\n\n" + suffix.Colorize(RimWorldUiStyle.Colors.MutedText);
        }

        internal static float CalculateGeneIconFlowHeight(int geneCount, float width)
        {
            if (geneCount <= 0)
                return 0f;

            int iconsPerRow = CalculateGeneIconsPerRow(width);
            int rowCount = Mathf.CeilToInt(geneCount / (float)iconsPerRow);

            return rowCount * GeneIconSize + Mathf.Max(0, rowCount - 1) * DiagnosticIconGap;
        }

        private static void DrawGeneIconFlow(Rect rect, IReadOnlyList<GeneDef> genes)
        {
            if (genes == null)
                throw new ArgumentNullException(nameof(genes));

            int iconsPerRow = CalculateGeneIconsPerRow(rect.width);

            for (var index = 0; index < genes.Count; index++)
            {
                int row = index / iconsPerRow;
                int column = index % iconsPerRow;
                var iconRect = new Rect(
                    rect.x + column * (GeneIconSize + DiagnosticIconGap),
                    rect.y + row * (GeneIconSize + DiagnosticIconGap),
                    GeneIconSize,
                    GeneIconSize);

                DrawGeneIcon(iconRect, genes[index]);
            }
        }

        internal static float CalculateLimitedGeneIconFlowHeight(int geneCount, float width, int maxRows)
        {
            if (maxRows <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxRows));

            float maximumHeight = maxRows * GeneIconSize + Mathf.Max(0, maxRows - 1) * DiagnosticIconGap;

            return Mathf.Min(CalculateGeneIconFlowHeight(geneCount, width), maximumHeight);
        }

        internal static void DrawLimitedGeneIconFlow(
            Rect rect,
            IReadOnlyList<GeneDef> genes,
            int maxRows,
            string overflowTooltip)
        {
            if (genes == null)
                throw new ArgumentNullException(nameof(genes));

            if (maxRows <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxRows));

            int iconsPerRow = CalculateGeneIconsPerRow(rect.width);
            int capacity = iconsPerRow * maxRows;
            bool hasOverflow = genes.Count > capacity;
            int displayedGeneCount = hasOverflow ? Mathf.Max(0, capacity - 1) : genes.Count;

            for (var index = 0; index < displayedGeneCount; index++)
            {
                int row = index / iconsPerRow;
                int column = index % iconsPerRow;
                var iconRect = new Rect(
                    rect.x + column * (GeneIconSize + DiagnosticIconGap),
                    rect.y + row * (GeneIconSize + DiagnosticIconGap),
                    GeneIconSize,
                    GeneIconSize);

                DrawGeneIcon(iconRect, genes[index]);
            }

            if (!hasOverflow)
                return;

            int hiddenGeneCount = genes.Count - displayedGeneCount;
            int overflowRow = displayedGeneCount / iconsPerRow;
            int overflowColumn = displayedGeneCount % iconsPerRow;
            var overflowRect = new Rect(
                rect.x + overflowColumn * (GeneIconSize + DiagnosticIconGap),
                rect.y + overflowRow * (GeneIconSize + DiagnosticIconGap),
                GeneIconSize,
                GeneIconSize);
            using (ImGuiStateScope.Capture())
            {
                Widgets.DrawBoxSolid(
                    overflowRect,
                    Mouse.IsOver(overflowRect)
                        ? RimWorldUiStyle.Colors.HoverBackground
                        : RimWorldUiStyle.Colors.NestedPanelBackground);
                RimWorldUiWidgets.DrawPanelBorder(overflowRect);

                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = RimWorldUiStyle.Colors.PrimaryText;
                Widgets.Label(overflowRect, "+" + hiddenGeneCount);

                if (!string.IsNullOrEmpty(overflowTooltip))
                    TooltipHandler.TipRegion(overflowRect, overflowTooltip);
            }
        }

        private static int CalculateGeneIconsPerRow(float width)
        {
            return Mathf.Max(1, Mathf.FloorToInt((width + DiagnosticIconGap) / (GeneIconSize + DiagnosticIconGap)));
        }

        private static void DrawGeneIcon(Rect rect, GeneDef gene)
        {
            using (ImGuiStateScope.Capture())
            {
                GUI.color = Color.white;
                Widgets.DefIcon(rect, gene, null, 0.9f, null, drawPlaceholder: false, gene.IconColor);
            }

            AddGeneTooltip(rect, gene);
            XenogermPlannerNativeInspector.TryOpenContextMenu(rect, gene);
        }

        private static float GetDiagnosticTextWidth(float viewWidth)
        {
            float iconPairWidth = GeneIconSize * 2f + DiagnosticIconGap;

            return Mathf.Max(1f, viewWidth - iconPairWidth - DiagnosticTextGap);
        }

        private static Color GetConflictDiagnosticColor(PlanGeneConflictDiagnostic conflict)
        {
            return conflict.Kind == PlanGeneConflictKind.Ordinary && conflict.HasPredictedWinner
                ? RimWorldUiStyle.Colors.MutedText
                : RimWorldUiStyle.Colors.Warning;
        }

        private static void HandleTargetInteraction(Rect rect, Thing target)
        {
            bool isHovered = Mouse.IsOver(rect);

            if (isHovered)
            {
                XenogermPlannerTargetInteraction.Highlight(target);
            }

            if (Widgets.ButtonInvisible(rect))
            {
                XenogermPlannerTargetInteraction.TryNavigate(target);
            }
        }

        private static void AddTargetNavigationTooltip(Rect rect, Thing target)
        {
            string tooltip = target.LabelCap.Colorize(ColoredText.TipSectionTitleColor) +
                             "\n\n" +
                             "XenogermPlanner.Planner.TargetNavigationHint".Translate()
                                 .ToString()
                                 .Colorize(RimWorldUiStyle.Colors.MutedText);

            TooltipHandler.TipRegion(rect, tooltip);
        }

        private static float CalculateTooltipGeneGroupHeight(int geneCount)
        {
            return XenogermPlannerStyle.Metrics.TooltipGroupHeaderHeight +
                   geneCount * XenogermPlannerStyle.Metrics.TooltipRowHeight;
        }

        private static void DrawTooltipGeneGroup(
            Rect rect,
            string header,
            IReadOnlyList<GeneDef> genes,
            PlanGenepackCompositionDiagnostic composition,
            PlanReadinessMode readinessMode)
        {
            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = RimWorldUiStyle.Colors.MutedText;

                Widgets.Label(
                    new Rect(rect.x, rect.y, rect.width, XenogermPlannerStyle.Metrics.TooltipGroupHeaderHeight),
                    header);

                float y = rect.y + XenogermPlannerStyle.Metrics.TooltipGroupHeaderHeight;

                Text.Font = GameFont.Small;
                GUI.color = RimWorldUiStyle.Colors.PrimaryText;

                foreach (GeneDef gene in genes)
                {
                    DrawTooltipGeneRow(
                        new Rect(rect.x, y, rect.width, XenogermPlannerStyle.Metrics.TooltipRowHeight),
                        gene,
                        composition,
                        readinessMode);

                    y += XenogermPlannerStyle.Metrics.TooltipRowHeight;
                }
            }
        }

        private static void DrawTooltipGeneRow(
            Rect rect,
            GeneDef gene,
            PlanGenepackCompositionDiagnostic composition,
            PlanReadinessMode readinessMode)
        {
            float iconSize = Mathf.Min(GeneIconSize, rect.height);

            var iconRect = new Rect(rect.x, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize);

            var labelRect = new Rect(
                iconRect.xMax + GeneIconGap,
                rect.y,
                Mathf.Max(0f, rect.xMax - iconRect.xMax - GeneIconGap),
                rect.height);

            using (ImGuiStateScope.Capture())
            {
                Widgets.DefIcon(iconRect, gene, null, 0.9f, null, drawPlaceholder: false, gene.IconColor);

                GUI.color = GetTooltipGeneLabelColor(gene, composition, readinessMode);
                Text.Anchor = TextAnchor.MiddleLeft;

                Widgets.Label(labelRect, XenogermPlannerPresentation.GetGeneDisplayName(gene));
            }
        }

        private static Color GetTooltipGeneLabelColor(
            GeneDef gene,
            PlanGenepackCompositionDiagnostic composition,
            PlanReadinessMode readinessMode)
        {
            bool isAdditionalGene = ContainsGene(composition.AdditionalGenes, gene);

            if (!isAdditionalGene)
                return RimWorldUiStyle.Colors.Positive;

            return readinessMode == PlanReadinessMode.ExactPayload
                ? RimWorldUiStyle.Colors.Negative
                : RimWorldUiStyle.Colors.MutedText;
        }

        private static Color GetGenepackStatusColor(
            PlanGenepackCompositionDiagnostic composition,
            PlanReadinessMode readinessMode)
        {
            if (readinessMode != PlanReadinessMode.ExactPayload)
                return RimWorldUiStyle.Colors.Neutral;

            return composition.IsExactPayloadEligible
                ? RimWorldUiStyle.Colors.Positive
                : RimWorldUiStyle.Colors.Negative;
        }

        private static bool ContainsGene(IEnumerable<GeneDef> genes, GeneDef expectedGene)
        {
            foreach (GeneDef gene in genes)
            {
                if (gene == expectedGene)
                    return true;
            }

            return false;
        }

        private static Rect GetTooltipRect(Rect bounds, float width, float height)
        {
            Vector2 mousePosition = Event.current.mousePosition;

            float x = mousePosition.x + RimWorldUiStyle.Metrics.TooltipMouseOffset;

            float y = mousePosition.y + RimWorldUiStyle.Metrics.TooltipMouseOffset;

            if (x + width > bounds.xMax)
            {
                x = mousePosition.x - width - RimWorldUiStyle.Metrics.TooltipMouseOffset;
            }

            if (y + height > bounds.yMax)
            {
                y = mousePosition.y - height - RimWorldUiStyle.Metrics.TooltipMouseOffset;
            }

            x = Mathf.Clamp(x, bounds.x, Mathf.Max(bounds.x, bounds.xMax - width));

            y = Mathf.Clamp(y, bounds.y, Mathf.Max(bounds.y, bounds.yMax - height));

            return new Rect(x, y, width, height);
        }

        private static float CalculateTextHeight(string text, float width, GameFont font)
        {
            using (ImGuiStateScope.Capture())
            {
                Text.Font = font;
                return Text.CalcHeight(text, width);
            }
        }
    }
}