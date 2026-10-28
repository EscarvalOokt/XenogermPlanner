using System;
using System.Collections.Generic;
using Escarval.RimWorld.UI;
using RimWorld;
using UnityEngine;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Assemblers;
using XenogermPlanner.Donors;
using XenogermPlanner.Genes;
using XenogermPlanner.Notifications;
using XenogermPlanner.Plans;
using XenogermPlanner.Trade;

namespace XenogermPlanner.UI
{
    [StaticConstructorOnStartup]
    public sealed class XenogermPlannerWindow : MainTabWindow
    {
        private const float WindowWidth = 980f;
        private const float WindowHeight = 660f;
        private const float ColumnGap = RimWorldUiStyle.Metrics.SectionGap;
        private const float LeftColumnWidthFraction = 0.24f;
        private const float LeftColumnMinWidth = 220f;
        private const float LeftColumnMaxWidth = 280f;
        private const float SectionPadding = RimWorldUiStyle.Metrics.PanelPadding;
        private const float SectionTitleHeight = RimWorldUiStyle.Metrics.HeaderHeight;
        private const float HeaderActionContentGap = RimWorldUiStyle.Metrics.SectionGap;
        private const float HeaderActionGroupGap = RimWorldUiStyle.Metrics.LargeGap;
        private const float PlanRowHeight = RimWorldUiStyle.Metrics.CompactRowHeight;
        private const float PlanSearchGap = RimWorldUiStyle.Metrics.SmallGap;
        private const float MetadataLineHeight = 24f;
        private const float AssemblerSelectorGap = 8f;
        private const float AssemblerSelectorHorizontalPadding = 18f;
        private const float AssemblerSelectorMinWidth = 180f;
        private const float AssemblerSelectorMaxWidth = 420f;
        private const float AssemblerClearButtonSize = 26f;
        private const float AssemblerClearButtonGap = 4f;
        private const float AssemblerBlockerIconSize = 20f;
        private const float AssemblerBlockerIconGap = 6f;
        private const float AssemblerDetailsHorizontalPadding = 12f;
        private const float GeneRowHeight = RimWorldUiStyle.Metrics.CompactRowHeight;
        private const float CoverageTableHeaderHeight = RimWorldUiStyle.Metrics.TableHeaderHeight;
        private const float CoverageTableRowMinHeight = RimWorldUiStyle.Metrics.CompactRowHeight;
        private const float CoverageColumnGap = RimWorldUiStyle.Metrics.TableDividerWidth;
        private const float CoverageGeneColumnFraction = 0.32f;
        private const float CoverageStateColumnFraction = 0.14f;
        private const float CoveragePotentialDonorColumnWidth = 116f;
        private const float CoveragePotentialDonorButtonInset = 2f;
        private const float ContentGap = RimWorldUiStyle.Metrics.SectionGap;
        private const float ScrollbarWidth = RimWorldUiStyle.Metrics.ScrollbarWidth;
        private const float DetailsTabHeight = RimWorldUiStyle.Metrics.CompactTabHeight;
        private const float DetailsTabGap = RimWorldUiStyle.Metrics.SmallGap;

        private static readonly ReloadableTexture2D _createPlanIcon = new ReloadableTexture2D("UI/Buttons/Create");

        private static readonly ReloadableTexture2D _pastePlanIcon = new ReloadableTexture2D("UI/Buttons/Paste");

        private static readonly ReloadableTexture2D _refreshInventoryIcon =
            new ReloadableTexture2D("UI/Buttons/Refresh");

        private static readonly ReloadableTexture2D _createTemplateIcon =
            new ReloadableTexture2D("UI/Buttons/CreateTemplate");

        private static readonly ReloadableTexture2D _editPlanIcon = new ReloadableTexture2D("UI/Buttons/Edit");

        private static readonly ReloadableTexture2D _duplicatePlanIcon =
            new ReloadableTexture2D("UI/Buttons/Duplicate");

        private static readonly ReloadableTexture2D _copyPlanIcon = new ReloadableTexture2D("UI/Buttons/Copy");

        private static readonly ReloadableTexture2D _deletePlanIcon = new ReloadableTexture2D("UI/Buttons/Delete");

        private readonly XenogermPlannerWindowAnalysisCache _analysisCache = new XenogermPlannerWindowAnalysisCache();

        private readonly VariableHeightScrollListLayoutCache _assemblerGeneCoverageLayoutCache =
            new VariableHeightScrollListLayoutCache();

        private readonly GeneCoverageTableProjectionCache _assemblerGeneCoverageProjectionCache =
            new GeneCoverageTableProjectionCache();

        private readonly VariableHeightScrollListLayoutCache _geneDiagnosticsLayoutCache =
            new VariableHeightScrollListLayoutCache();

        private readonly VariableHeightScrollListLayoutCache _productGeneCoverageLayoutCache =
            new VariableHeightScrollListLayoutCache();

        private readonly GeneCoverageTableProjectionCache _productGeneCoverageProjectionCache =
            new GeneCoverageTableProjectionCache();

        private bool _assemblerDetailsExpanded;
        private Vector2 _assemblerGeneCoverageScrollPosition;
        private GeneCoverageSortState _assemblerGeneCoverageSortState = GeneCoverageSortState.Default;
        private object _filteredPlanLanguageKey;
        private List<XenogermPlan> _filteredPlanProjection;
        private string _filteredPlanQuery;

        private IReadOnlyList<XenogermPlan> _filteredPlanSource;
        private object _geneDiagnosticsDesiredGenesKey;
        private object _geneDiagnosticsLanguageKey;

        private XenogermPlan _geneDiagnosticsPlan;
        private GeneTargetDiagnosticsProjection _geneDiagnosticsProjection;
        private PlanReadinessMode _geneDiagnosticsReadinessMode;
        private Vector2 _geneDiagnosticsScrollPosition;
        private object _geneDiagnosticsUnresolvedGenesKey;
        private Vector2 _planListScrollPosition;
        private string _planSearchText = string.Empty;
        private Vector2 _productGeneCoverageScrollPosition;
        private GeneCoverageSortState _productGeneCoverageSortState = GeneCoverageSortState.Default;
        private Building_GeneAssembler _selectedAssembler;
        private Map _selectedAssemblerMap;
        private PlanDetailsTab _selectedDetailsTab = PlanDetailsTab.Overview;

        private string _selectedPlanId;

        public override Vector2 RequestedTabSize => new Vector2(WindowWidth, WindowHeight);

        public override void PreOpen()
        {
            base.PreOpen();

            InvalidatePresentationProjections();
            InvalidateVariableHeightLayouts();
            _analysisCache.Invalidate();
            Current.Game?.GetComponent<PlanGenepackInventoryGameComponent>()?.Invalidate();
        }

        public override void DoWindowContents(Rect inRect)
        {
            using (ImGuiStateScope.Capture())
            {
                Rect contentRect = inRect;

                XenogermPlanGameComponent component = Current.Game?.GetComponent<XenogermPlanGameComponent>();
                PlanGenepackInventoryGameComponent inventoryComponent =
                    Current.Game?.GetComponent<PlanGenepackInventoryGameComponent>();

                if (component == null || inventoryComponent == null)
                {
                    DrawUnavailableState(contentRect);
                    return;
                }

                IReadOnlyList<XenogermPlan> plans = component.Plans;
                XenogermPlan selectedPlan = ResolveSelectedPlan(plans);
                PlanGenepackInventorySnapshot inventorySnapshot = inventoryComponent.Snapshot;
                PlanReadinessResult readinessResult = selectedPlan == null
                    ? null
                    : _analysisCache.GetProductReadiness(selectedPlan, inventorySnapshot);
                PlanGeneTargetAnalysisResult targetAnalysis = selectedPlan == null
                    ? null
                    : _analysisCache.GetTargetAnalysis(selectedPlan);
                Map activeMap = Find.CurrentMap;

                float leftColumnWidth = Mathf.Clamp(
                    contentRect.width * LeftColumnWidthFraction,
                    LeftColumnMinWidth,
                    LeftColumnMaxWidth);

                var planListRect = new Rect(contentRect.x, contentRect.y, leftColumnWidth, contentRect.height);

                var planDetailsRect = new Rect(
                    planListRect.xMax + ColumnGap,
                    contentRect.y,
                    contentRect.width - leftColumnWidth - ColumnGap,
                    contentRect.height);

                DrawPlanList(planListRect, component, plans);

                DrawPlanDetails(
                    planDetailsRect,
                    component,
                    inventoryComponent,
                    selectedPlan,
                    readinessResult,
                    targetAnalysis,
                    inventorySnapshot.Genepacks,
                    activeMap);
            }
        }

        private static void DrawUnavailableState(Rect rect)
        {
            RimWorldUiWidgets.DrawPanel(rect);

            Rect messageRect = rect.ContractedBy(SectionPadding);
            DrawCenteredMutedMessage(messageRect, "XenogermPlanner.Planner.DataUnavailable".Translate().ToString());
        }

        private void DrawPlanList(Rect rect, XenogermPlanGameComponent component, IReadOnlyList<XenogermPlan> plans)
        {
            RimWorldUiWidgets.DrawPanel(rect);

            Rect innerRect = rect.ContractedBy(SectionPadding);
            var headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width, SectionTitleHeight);

            DrawPlanListHeader(headerRect, component);

            var searchRect = new Rect(
                innerRect.x,
                headerRect.yMax + PlanSearchGap,
                innerRect.width,
                RimWorldUiStyle.Metrics.SearchRowHeight);

            if (RimWorldUiWidgets.DrawLabeledSearchField(
                    searchRect,
                    "XenogermPlanner.Planner.SearchPlans".Translate().ToString(),
                    ref _planSearchText))
            {
                _planListScrollPosition = Vector2.zero;
            }

            var listRect = new Rect(
                innerRect.x,
                searchRect.yMax + PlanSearchGap,
                innerRect.width,
                Mathf.Max(0f, innerRect.yMax - searchRect.yMax - PlanSearchGap));

            if (plans.Count == 0)
            {
                DrawEmptyPlanList(listRect);
                return;
            }

            IReadOnlyList<XenogermPlan> filteredPlans = GetFilteredPlanProjection(plans);

            if (filteredPlans.Count == 0)
            {
                DrawNoPlanSearchResults(listRect);
                return;
            }

            FixedHeightScrollListLayout layout = RimWorldUiWidgets.BeginFixedHeightScrollView(
                listRect,
                ref _planListScrollPosition,
                filteredPlans.Count,
                PlanRowHeight,
                out float viewWidth);

            for (int index = layout.FirstVisibleIndex; index < layout.LastVisibleIndexExclusive; index++)
            {
                XenogermPlan plan = filteredPlans[index];
                var rowRect = new Rect(0f, index * PlanRowHeight, viewWidth, PlanRowHeight);

                DrawPlanRow(rowRect, plan, string.Equals(_selectedPlanId, plan.Id, StringComparison.Ordinal), index);
            }

            Widgets.EndScrollView();
        }

        private void DrawPlanListHeader(Rect rect, XenogermPlanGameComponent component)
        {
            float actionRight = rect.xMax;
            Rect pasteRect = GetHeaderActionRect(rect, ref actionRight, 0f);
            Rect createRect = GetHeaderActionRect(rect, ref actionRight, RimWorldUiStyle.Metrics.IconButtonGap);

            var titleRect = new Rect(
                rect.x,
                rect.y,
                Mathf.Max(0f, createRect.x - rect.x - HeaderActionContentGap),
                rect.height);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;

            Widgets.Label(titleRect, "XenogermPlanner.Planner.PlansTitle".Translate());

            if (RimWorldUiWidgets.DrawIconButton(
                    createRect,
                    _createPlanIcon.Texture,
                    RimWorldUiStyle.Colors.Positive,
                    true,
                    "XenogermPlanner.Planner.CreatePlan".Translate().ToString()))
            {
                ShowCreatePlanMenu(component);
            }

            if (RimWorldUiWidgets.DrawIconButton(
                    pasteRect,
                    _pastePlanIcon.Texture,
                    RimWorldUiStyle.Colors.PrimaryText,
                    true,
                    "XenogermPlanner.Planner.PastePlan".Translate().ToString()))
            {
                PastePlanFromClipboard(component);
            }
        }

        private static Rect GetHeaderActionRect(Rect rect, ref float rightEdge, float gapToRight)
        {
            rightEdge -= gapToRight;

            float actionSize = Mathf.Min(RimWorldUiStyle.Metrics.IconButtonSize, rect.height);
            rightEdge -= actionSize;

            return new Rect(rightEdge, rect.y + (rect.height - actionSize) * 0.5f, actionSize, actionSize);
        }

        private static void DrawEmptyPlanList(Rect rect)
        {
            DrawCenteredMutedMessage(rect, "XenogermPlanner.Planner.EmptyPlans".Translate().ToString());
        }

        private static void DrawNoPlanSearchResults(Rect rect)
        {
            DrawCenteredMutedMessage(rect, "XenogermPlanner.Planner.NoSearchResults".Translate().ToString());
        }

        private void DrawPlanRow(Rect rect, XenogermPlan plan, bool selected, int rowIndex)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            RimWorldUiWidgets.DrawSelectableRowBackground(rect, rowIndex, selected, Mouse.IsOver(rect));

            var contentRect = new Rect(
                rect.x + RimWorldUiStyle.Metrics.ControlGap,
                rect.y,
                Mathf.Max(0f, rect.width - RimWorldUiStyle.Metrics.ControlGap * 2f),
                rect.height);
            string displayName = XenogermPlannerPresentation.GetPlanDisplayName(plan);
            var countLabel = $"({XenogermPlannerPresentation.GetDesiredGeneCount(plan)})";

            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleRight;

                float countWidth = Mathf.Min(
                    contentRect.width * 0.34f,
                    Text.CalcSize(countLabel).x + RimWorldUiStyle.Metrics.ControlGap);

                var countRect = new Rect(contentRect.xMax - countWidth, contentRect.y, countWidth, contentRect.height);

                var nameRect = new Rect(
                    contentRect.x,
                    contentRect.y,
                    Mathf.Max(0f, countRect.x - contentRect.x - RimWorldUiStyle.Metrics.ControlGap),
                    contentRect.height);

                GUI.color = RimWorldUiStyle.Colors.PrimaryText;
                RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                    nameRect,
                    displayName,
                    GameFont.Small,
                    TextAnchor.MiddleLeft);

                GUI.color = plan.IsDegraded ? RimWorldUiStyle.Colors.Warning : RimWorldUiStyle.Colors.MutedText;
                Widgets.Label(countRect, countLabel);

                TooltipHandler.TipRegion(
                    countRect,
                    "XenogermPlanner.Planner.DesiredGeneCount".Translate(
                            XenogermPlannerPresentation.GetDesiredGeneCount(plan))
                        .ToString());

                if (plan.IsDegraded)
                {
                    TooltipHandler.TipRegion(rect, "XenogermPlanner.Planner.DegradedWarning".Translate().ToString());
                }
            }

            if (Event.current.button == 0 && Widgets.ButtonInvisible(rect))
                SelectPlan(plan.Id);
        }

        private void DrawPlanDetails(
            Rect rect,
            XenogermPlanGameComponent component,
            PlanGenepackInventoryGameComponent inventoryComponent,
            XenogermPlan selectedPlan,
            PlanReadinessResult readinessResult,
            PlanGeneTargetAnalysisResult targetAnalysis,
            IReadOnlyList<Genepack> productGenepacks,
            Map activeMap)
        {
            RimWorldUiWidgets.DrawPanel(rect);

            Rect innerRect = rect.ContractedBy(SectionPadding);

            var headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width, SectionTitleHeight);

            DrawPlanDetailsHeader(headerRect, component, inventoryComponent, selectedPlan, readinessResult);

            var detailsRect = new Rect(
                innerRect.x,
                headerRect.yMax,
                innerRect.width,
                innerRect.height - SectionTitleHeight);

            if (selectedPlan == null)
            {
                DrawNoSelectedPlan(detailsRect);
                return;
            }

            if (readinessResult == null)
                throw new ArgumentNullException(nameof(readinessResult));

            if (targetAnalysis == null)
                throw new ArgumentNullException(nameof(targetAnalysis));

            if (productGenepacks == null)
                throw new ArgumentNullException(nameof(productGenepacks));

            float y = DrawReadinessSummary(detailsRect, detailsRect.y, selectedPlan, readinessResult);
            y += ContentGap;

            var tabsRect = new Rect(
                detailsRect.x,
                y,
                detailsRect.width,
                Mathf.Min(DetailsTabHeight, Mathf.Max(0f, detailsRect.yMax - y)));

            DrawDetailsTabs(tabsRect, targetAnalysis);

            var tabContentRect = new Rect(
                detailsRect.x,
                tabsRect.yMax + ContentGap,
                detailsRect.width,
                Mathf.Max(0f, detailsRect.yMax - tabsRect.yMax - ContentGap));

            switch (_selectedDetailsTab)
            {
                case PlanDetailsTab.Overview:
                    PlanPotentialDonorAnalysisResult potentialDonorAnalysis = readinessResult.MissingGenes.Count == 0
                        ? null
                        : _analysisCache.GetPotentialDonorAnalysis(readinessResult, activeMap);

                    DrawOverviewTab(
                        tabContentRect,
                        selectedPlan,
                        readinessResult,
                        productGenepacks,
                        potentialDonorAnalysis);
                    break;

                case PlanDetailsTab.Assembler:
                    IReadOnlyList<Building_GeneAssembler> selectableAssemblers =
                        _analysisCache.GetSelectableAssemblers(activeMap);
                    Building_GeneAssembler currentAssembler = ResolveSelectedAssembler(activeMap, selectableAssemblers);
                    XenogermPlannerWindowAssemblerAnalysis assemblerAnalysis = currentAssembler == null
                        ? null
                        : _analysisCache.GetAssemblerAnalysis(selectedPlan, currentAssembler, activeMap);

                    DrawAssemblerTab(
                        tabContentRect,
                        selectedPlan,
                        selectableAssemblers,
                        currentAssembler,
                        assemblerAnalysis?.ReadinessResult,
                        assemblerAnalysis?.LiveState.Scope.VisibleGenepacks);
                    break;

                case PlanDetailsTab.GeneEffects:
                    DrawGeneEffectsTab(tabContentRect, selectedPlan, targetAnalysis);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(_selectedDetailsTab),
                        _selectedDetailsTab,
                        "Unsupported plan details tab.");
            }
        }

        private void DrawDetailsTabs(Rect rect, PlanGeneTargetAnalysisResult targetAnalysis)
        {
            if (targetAnalysis == null)
                throw new ArgumentNullException(nameof(targetAnalysis));

            if (rect.width <= 0f || rect.height <= 0f)
                return;

            string[] labels =
            [
                "XenogermPlanner.Planner.Tab.Overview".Translate().ToString(),
                "XenogermPlanner.Planner.Tab.Assembler".Translate().ToString(),
                XenogermPlannerPresentation.GetGeneEffectsTabLabel(targetAnalysis.DiagnosticCount)
            ];

            PlanDetailsTab[] tabs =
            [
                PlanDetailsTab.Overview,
                PlanDetailsTab.Assembler,
                PlanDetailsTab.GeneEffects
            ];

            float x = rect.x;

            for (var index = 0; index < tabs.Length; index++)
            {
                float desiredWidth = RimWorldUiWidgets.CalculateCompactTabWidth(labels[index]);
                float remainingWidth = Mathf.Max(0f, rect.xMax - x);
                float tabWidth = Mathf.Min(desiredWidth, remainingWidth);

                if (tabWidth <= 0f)
                    break;

                var tabRect = new Rect(x, rect.y, tabWidth, rect.height);

                if (RimWorldUiWidgets.DrawCompactTab(tabRect, labels[index], _selectedDetailsTab == tabs[index]))
                {
                    _selectedDetailsTab = tabs[index];
                }

                x = tabRect.xMax + DetailsTabGap;
            }
        }

        private void DrawOverviewTab(
            Rect rect,
            XenogermPlan selectedPlan,
            PlanReadinessResult readinessResult,
            IReadOnlyList<Genepack> sourceGenepacks,
            PlanPotentialDonorAnalysisResult potentialDonorAnalysis)
        {
            float y = rect.y;
            string readinessDiagnostic = XenogermPlannerPresentation.GetReadinessDiagnosticMessage(readinessResult);

            if (readinessDiagnostic != null)
            {
                string displayedDiagnostic = readinessResult.HasExactPayloadConflict
                    ? "• " + readinessDiagnostic
                    : readinessDiagnostic;

                float diagnosticHeight = GetTextHeight(displayedDiagnostic, rect.width, GameFont.Small);

                using (ImGuiStateScope.Capture())
                {
                    GUI.color = readinessResult.HasExactPayloadConflict
                        ? RimWorldUiStyle.Colors.Warning
                        : RimWorldUiStyle.Colors.MutedText;
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;

                    Widgets.Label(new Rect(rect.x, y, rect.width, diagnosticHeight), displayedDiagnostic);
                }

                y += diagnosticHeight + ContentGap;
            }

            if (selectedPlan.IsDegraded)
            {
                var degradedWarning = "XenogermPlanner.Planner.DegradedWarning".Translate().ToString();
                float warningHeight = GetTextHeight(degradedWarning, rect.width, GameFont.Small);

                using (ImGuiStateScope.Capture())
                {
                    GUI.color = RimWorldUiStyle.Colors.Warning;
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;

                    Widgets.Label(new Rect(rect.x, y, rect.width, warningHeight), degradedWarning);
                }

                y += warningHeight + ContentGap;
            }

            if (!XenogermPlannerPresentation.ShouldShowReadinessGeneDiagnostics(readinessResult) &&
                !selectedPlan.IsDegraded)
            {
                return;
            }

            var tableRect = new Rect(rect.x, y, rect.width, Mathf.Max(0f, rect.yMax - y));
            PlanGenepackCompositionDiagnostic hoveredComposition = null;
            PlanReadinessMode hoveredCompositionMode = selectedPlan.ReadinessMode;

            DrawGeneCoverageTable(
                tableRect,
                selectedPlan,
                selectedPlan.ReadinessMode,
                readinessResult,
                sourceGenepacks,
                potentialDonorAnalysis,
                _productGeneCoverageProjectionCache,
                _productGeneCoverageLayoutCache,
                ref _productGeneCoverageScrollPosition,
                ref _productGeneCoverageSortState,
                ref hoveredComposition,
                ref hoveredCompositionMode);

            if (hoveredComposition != null)
            {
                XenogermPlannerWidgets.DrawGenepackCompositionTooltip(rect, hoveredComposition, hoveredCompositionMode);
            }
        }

        private void DrawAssemblerTab(
            Rect rect,
            XenogermPlan selectedPlan,
            IReadOnlyList<Building_GeneAssembler> selectableAssemblers,
            Building_GeneAssembler currentAssembler,
            PlanAssemblerReadinessResult assemblerReadinessResult,
            IReadOnlyList<Genepack> sourceGenepacks)
        {
            float y = DrawAssemblerScope(
                rect,
                rect.y,
                selectableAssemblers,
                currentAssembler,
                assemblerReadinessResult);

            if (currentAssembler == null || assemblerReadinessResult == null)
                return;

            PlanReadinessResult scopeResult = assemblerReadinessResult.GeneScopeResult;

            if (!XenogermPlannerPresentation.ShouldShowReadinessGeneDiagnostics(scopeResult) &&
                !selectedPlan.IsDegraded)
            {
                return;
            }

            y += ContentGap;

            var tableRect = new Rect(rect.x, y, rect.width, Mathf.Max(0f, rect.yMax - y));
            PlanGenepackCompositionDiagnostic hoveredComposition = null;
            PlanReadinessMode hoveredCompositionMode = selectedPlan.ReadinessMode;

            DrawGeneCoverageTable(
                tableRect,
                selectedPlan,
                selectedPlan.ReadinessMode,
                scopeResult,
                sourceGenepacks,
                null,
                _assemblerGeneCoverageProjectionCache,
                _assemblerGeneCoverageLayoutCache,
                ref _assemblerGeneCoverageScrollPosition,
                ref _assemblerGeneCoverageSortState,
                ref hoveredComposition,
                ref hoveredCompositionMode);

            if (hoveredComposition != null)
            {
                XenogermPlannerWidgets.DrawGenepackCompositionTooltip(rect, hoveredComposition, hoveredCompositionMode);
            }
        }

        private void DrawGeneEffectsTab(
            Rect rect,
            XenogermPlan selectedPlan,
            PlanGeneTargetAnalysisResult targetAnalysis)
        {
            if (targetAnalysis.HasDiagnostics)
            {
                GeneTargetDiagnosticsProjection projection = GetGeneDiagnosticsProjection(selectedPlan, targetAnalysis);

                XenogermPlannerWidgets.DrawGeneTargetDiagnosticsPanel(
                    rect,
                    projection,
                    _geneDiagnosticsLayoutCache,
                    ref _geneDiagnosticsScrollPosition);
                return;
            }

            RimWorldUiWidgets.DrawPanel(rect, nested: true);

            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = RimWorldUiStyle.Colors.MutedText;

                Widgets.Label(rect.ContractedBy(SectionPadding), "XenogermPlanner.GeneDiagnostics.Empty".Translate());
            }
        }

        private float DrawAssemblerScope(
            Rect rect,
            float y,
            IReadOnlyList<Building_GeneAssembler> selectableAssemblers,
            Building_GeneAssembler currentAssembler,
            PlanAssemblerReadinessResult readinessResult)
        {
            if (selectableAssemblers == null)
            {
                throw new ArgumentNullException(nameof(selectableAssemblers));
            }

            if (currentAssembler != null && readinessResult == null)
            {
                throw new ArgumentException(
                    "Selected assembler readiness data is incomplete.",
                    nameof(currentAssembler));
            }

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;

            Widgets.Label(
                new Rect(rect.x, y, rect.width, SectionTitleHeight),
                "XenogermPlanner.Planner.AssemblerScopeTitle".Translate());

            y += SectionTitleHeight;

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            var summaryRowRect = new Rect(rect.x, y, rect.width, MetadataLineHeight);

            GetSummaryColumnRects(summaryRowRect, out Rect selectorColumnRect, out Rect statusColumnRect);

            var selectorCaption = "XenogermPlanner.Planner.AssemblerSelector".Translate().ToString();

            float selectorLabelWidth = Mathf.Min(
                Text.CalcSize(selectorCaption).x + AssemblerSelectorGap,
                selectorColumnRect.width * 0.4f);

            var selectorLabelRect = new Rect(
                selectorColumnRect.x,
                selectorColumnRect.y,
                selectorLabelWidth,
                selectorColumnRect.height);

            Widgets.Label(selectorLabelRect, selectorCaption);

            float selectorStartX = selectorLabelRect.xMax + AssemblerSelectorGap;
            float selectorEndX = selectorColumnRect.xMax;

            var locateLabel = "XenogermPlanner.Planner.Locate".Translate().ToString();

            float locateButtonWidth = currentAssembler == null
                ? 0f
                : Mathf.Max(
                    AssemblerClearButtonSize,
                    Text.CalcSize(locateLabel).x + AssemblerSelectorHorizontalPadding);

            float auxiliaryButtonWidth = currentAssembler == null
                ? 0f
                : AssemblerClearButtonGap + locateButtonWidth + AssemblerClearButtonGap + AssemblerClearButtonSize;

            float availableButtonWidth = Mathf.Max(0f, selectorEndX - selectorStartX - auxiliaryButtonWidth);

            if (selectableAssemblers.Count > 0)
            {
                string selectorLabel = currentAssembler == null
                    ? "XenogermPlanner.Planner.SelectAssembler".Translate().ToString()
                    : XenogermPlannerPresentation.GetAssemblerDisplayName(currentAssembler);

                float desiredButtonWidth = Mathf.Clamp(
                    Text.CalcSize(selectorLabel).x + AssemblerSelectorHorizontalPadding * 2f,
                    AssemblerSelectorMinWidth,
                    AssemblerSelectorMaxWidth);

                var selectorButtonRect = new Rect(
                    selectorStartX,
                    y,
                    Mathf.Min(desiredButtonWidth, availableButtonWidth),
                    MetadataLineHeight);

                if (currentAssembler != null)
                {
                    if (Mouse.IsOver(selectorButtonRect))
                    {
                        XenogermPlannerTargetInteraction.Highlight(currentAssembler);
                    }

                    TooltipHandler.TipRegion(selectorButtonRect, selectorLabel);
                }

                if (Widgets.ButtonText(selectorButtonRect, selectorLabel))
                {
                    OpenAssemblerSelector(selectableAssemblers);
                }

                if (currentAssembler != null)
                {
                    var locateButtonRect = new Rect(
                        selectorButtonRect.xMax + AssemblerClearButtonGap,
                        y,
                        locateButtonWidth,
                        MetadataLineHeight);

                    if (Mouse.IsOver(locateButtonRect))
                    {
                        XenogermPlannerTargetInteraction.Highlight(currentAssembler);
                    }

                    bool canLocate = XenogermPlannerTargetInteraction.CanNavigate(currentAssembler);

                    if (Widgets.ButtonText(locateButtonRect, locateLabel, true, true, canLocate))
                    {
                        XenogermPlannerTargetInteraction.TryNavigate(currentAssembler);
                    }

                    TooltipHandler.TipRegion(
                        locateButtonRect,
                        "XenogermPlanner.Planner.TargetNavigationHint".Translate());

                    var clearButtonRect = new Rect(
                        locateButtonRect.xMax + AssemblerClearButtonGap,
                        y,
                        AssemblerClearButtonSize,
                        MetadataLineHeight);

                    if (Widgets.ButtonText(clearButtonRect, "×"))
                    {
                        SelectAssembler(null);
                    }

                    TooltipHandler.TipRegion(
                        clearButtonRect,
                        "XenogermPlanner.Planner.ClearAssemblerSelection".Translate());
                }
            }
            else
            {
                using (ImGuiStateScope.Capture())
                {
                    GUI.color = RimWorldUiStyle.Colors.MutedText;

                    Widgets.Label(
                        new Rect(
                            selectorStartX,
                            y,
                            Mathf.Max(0f, selectorColumnRect.xMax - selectorStartX),
                            MetadataLineHeight),
                        "—");
                }
            }

            if (readinessResult != null)
            {
                string statusLabel =
                    XenogermPlannerPresentation.GetAssemblerReadinessStatusLabel(readinessResult.Status);

                var coloredStatusText = "XenogermPlanner.Planner.AssemblerReadiness".Translate(
                        statusLabel.Colorize(
                            XenogermPlannerWidgets.GetAssemblerReadinessStatusColor(readinessResult.Status)))
                    .ToString();

                Widgets.Label(statusColumnRect, coloredStatusText);
            }

            y += MetadataLineHeight;

            if (selectableAssemblers.Count == 0)
            {
                return DrawAssemblerScopeMessage(
                    rect,
                    y,
                    "XenogermPlanner.Planner.NoAssemblers".Translate().ToString(),
                    RimWorldUiStyle.Colors.MutedText);
            }

            if (currentAssembler == null)
            {
                return DrawAssemblerScopeMessage(
                    rect,
                    y,
                    "XenogermPlanner.Planner.NoSelectedAssembler".Translate().ToString(),
                    RimWorldUiStyle.Colors.MutedText);
            }

            PlanReadinessResult scopeResult = readinessResult.GeneScopeResult;

            bool hasPrimaryDiagnostics = scopeResult.HasExactPayloadConflict ||
                                         readinessResult.BlockerReasons.Count > 0 ||
                                         readinessResult.Status == PlanAssemblerReadinessStatus.SearchIncomplete;

            if (hasPrimaryDiagnostics)
                y += ContentGap * 0.5f;

            if (readinessResult.Status == PlanAssemblerReadinessStatus.SearchIncomplete)
            {
                y = DrawAssemblerScopeMessage(
                    rect,
                    y,
                    "XenogermPlanner.Planner.AssemblerSearchIncomplete".Translate().ToString(),
                    RimWorldUiStyle.Colors.Warning);
            }

            if (scopeResult.HasExactPayloadConflict)
            {
                string diagnostic = XenogermPlannerPresentation.GetAssemblerScopeDiagnosticMessage(scopeResult);

                if (diagnostic != null)
                {
                    y = DrawAssemblerScopeMessage(rect, y, "• " + diagnostic, RimWorldUiStyle.Colors.Warning);
                }
            }

            foreach (PlanAssemblerBlockerReason blockerReason in readinessResult.BlockerReasons)
            {
                string blockerMessage = XenogermPlannerPresentation.GetAssemblerBlockerMessage(
                    readinessResult,
                    blockerReason);

                Texture2D blockerIcon = null;
                Color? blockerIconColor = null;

                switch (blockerReason)
                {
                    case PlanAssemblerBlockerReason.InsufficientComplexity:
                        blockerIcon = GeneUtility.GCXTex.Texture;
                        blockerIconColor = GeneUtility.GCXColor;
                        break;

                    case PlanAssemblerBlockerReason.InsufficientArchiteCapsules:
                        blockerIcon = GeneUtility.ARCTex.Texture;
                        blockerIconColor = GeneUtility.ARCColor;
                        break;

                    case PlanAssemblerBlockerReason.MetabolismTooLow:
                        blockerIcon = GeneUtility.METTex.Texture;
                        blockerIconColor = GeneUtility.METColor;
                        break;
                }

                if (blockerIcon == null)
                {
                    blockerMessage = "• " + blockerMessage;
                }

                y = DrawAssemblerScopeMessage(
                    rect,
                    y,
                    blockerMessage,
                    RimWorldUiStyle.Colors.Warning,
                    blockerIcon,
                    blockerIconColor);
            }

            if (_assemblerDetailsExpanded)
            {
                y += ContentGap * 0.5f;

                y = DrawAssemblerDetails(rect, y, readinessResult);
            }

            y += ContentGap * 0.25f;

            var detailsLabel =
                (_assemblerDetailsExpanded
                    ? "XenogermPlanner.Planner.AssemblerDetailsHide"
                    : "XenogermPlanner.Planner.AssemblerDetailsShow").Translate()
                .ToString();

            float detailsButtonWidth = Text.CalcSize(detailsLabel).x + AssemblerDetailsHorizontalPadding * 2f;

            var detailsButtonRect = new Rect(rect.xMax - detailsButtonWidth, y, detailsButtonWidth, MetadataLineHeight);

            if (Widgets.ButtonText(detailsButtonRect, detailsLabel))
            {
                _assemblerDetailsExpanded = !_assemblerDetailsExpanded;
            }

            return y + MetadataLineHeight;
        }

        private static float DrawAssemblerDetails(Rect rect, float y, PlanAssemblerReadinessResult readinessResult)
        {
            PlanReadinessResult scopeResult = readinessResult.GeneScopeResult;

            float halfWidth = rect.width * 0.5f;

            string scopeStatusLabel = XenogermPlannerPresentation.GetReadinessStatusLabel(scopeResult.Status);

            Widgets.Label(
                new Rect(rect.x, y, halfWidth, MetadataLineHeight),
                "XenogermPlanner.Planner.AssemblerGeneScope".Translate(
                    scopeStatusLabel.Colorize(XenogermPlannerWidgets.GetReadinessStatusColor(scopeResult.Status))));

            Widgets.Label(
                new Rect(rect.x + halfWidth, y, rect.width - halfWidth, MetadataLineHeight),
                "XenogermPlanner.Planner.AssemblerVisibleGenepacks".Translate(readinessResult.VisibleGenepackCount));

            y += MetadataLineHeight;

            Widgets.Label(
                new Rect(rect.x, y, halfWidth, MetadataLineHeight),
                "XenogermPlanner.Planner.AssemblerScopeCovered".Translate(scopeResult.CoveredGenes.Count));

            Widgets.Label(
                new Rect(rect.x + halfWidth, y, rect.width - halfWidth, MetadataLineHeight),
                "XenogermPlanner.Planner.AssemblerScopeMissing".Translate(scopeResult.MissingGenes.Count));

            return y + MetadataLineHeight;
        }

        private static float DrawAssemblerScopeMessage(
            Rect rect,
            float y,
            string message,
            Color color,
            Texture2D icon = null,
            Color? iconColor = null)
        {
            float textWidth = icon == null
                ? rect.width
                : Mathf.Max(1f, rect.width - AssemblerBlockerIconSize - AssemblerBlockerIconGap);

            float messageHeight = GetTextHeight(message, textWidth, GameFont.Small);
            float lineHeight = icon == null ? messageHeight : Mathf.Max(AssemblerBlockerIconSize, messageHeight);

            using (ImGuiStateScope.Capture())
            {
                float textX = rect.x;

                if (icon != null)
                {
                    var iconRect = new Rect(
                        rect.x,
                        y + (lineHeight - AssemblerBlockerIconSize) * 0.5f,
                        AssemblerBlockerIconSize,
                        AssemblerBlockerIconSize);

                    GUI.color = iconColor ?? Color.white;
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);

                    textX = iconRect.xMax + AssemblerBlockerIconGap;
                }

                GUI.color = color;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;

                Widgets.Label(new Rect(textX, y, textWidth, lineHeight), message);
            }

            return y + lineHeight;
        }

        private void OpenAssemblerSelector(IReadOnlyList<Building_GeneAssembler> selectableAssemblers)
        {
            if (selectableAssemblers == null)
            {
                throw new ArgumentNullException(nameof(selectableAssemblers));
            }

            var sortedAssemblers = new List<Building_GeneAssembler>(selectableAssemblers);

            sortedAssemblers.Sort((left, right) => StringComparer.CurrentCultureIgnoreCase.Compare(
                XenogermPlannerPresentation.GetAssemblerDisplayName(left),
                XenogermPlannerPresentation.GetAssemblerDisplayName(right)));

            var options = new List<FloatMenuOption>(sortedAssemblers.Count);

            foreach (Building_GeneAssembler assembler in sortedAssemblers)
            {
                Building_GeneAssembler optionAssembler = assembler;

                options.Add(
                    new FloatMenuOption(
                        XenogermPlannerPresentation.GetAssemblerDisplayName(optionAssembler),
                        () =>
                        {
                            SelectAssembler(optionAssembler);
                            XenogermPlannerTargetInteraction.TryNavigate(optionAssembler);
                        },
                        MenuOptionPriority.Default,
                        _ => XenogermPlannerTargetInteraction.Highlight(optionAssembler)));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void DrawPlanDetailsHeader(
            Rect rect,
            XenogermPlanGameComponent component,
            PlanGenepackInventoryGameComponent inventoryComponent,
            XenogermPlan selectedPlan,
            PlanReadinessResult readinessResult)
        {
            if (selectedPlan == null)
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleLeft;

                Widgets.Label(rect, "XenogermPlanner.Planner.EmptyDetailsTitle".Translate());
                return;
            }

            float actionRight = rect.xMax;
            Rect deleteRect = GetHeaderActionRect(rect, ref actionRight, 0f);
            Rect copyRect = GetHeaderActionRect(rect, ref actionRight, RimWorldUiStyle.Metrics.IconButtonGap);
            Rect duplicateRect = GetHeaderActionRect(rect, ref actionRight, RimWorldUiStyle.Metrics.IconButtonGap);
            Rect editRect = GetHeaderActionRect(rect, ref actionRight, RimWorldUiStyle.Metrics.IconButtonGap);
            Rect templateRect = GetHeaderActionRect(rect, ref actionRight, RimWorldUiStyle.Metrics.IconButtonGap);
            Rect refreshRect = GetHeaderActionRect(rect, ref actionRight, HeaderActionGroupGap);

            var titleRect = new Rect(
                rect.x,
                rect.y,
                Mathf.Max(0f, refreshRect.x - rect.x - HeaderActionContentGap),
                rect.height);

            var detailsTitle = "XenogermPlanner.Planner.DetailsTitle".Translate(
                    XenogermPlannerPresentation.GetPlanDisplayName(selectedPlan))
                .ToString();

            RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                titleRect,
                detailsTitle,
                GameFont.Medium,
                TextAnchor.MiddleLeft);

            bool canRefresh = Find.CurrentMap != null;

            if (RimWorldUiWidgets.DrawIconButton(
                    refreshRect,
                    _refreshInventoryIcon.Texture,
                    RimWorldUiStyle.Colors.Accent,
                    canRefresh,
                    "XenogermPlanner.Planner.RefreshDescription".Translate().ToString()))
            {
                inventoryComponent.Invalidate();
                _analysisCache.InvalidateAssemblerAnalysis();
                _productGeneCoverageLayoutCache.Invalidate();
                _assemblerGeneCoverageLayoutCache.Invalidate();
                _ = inventoryComponent.Snapshot;
            }

            bool canCreateTemplate = readinessResult?.IsReady == true;

            if (RimWorldUiWidgets.DrawIconButton(
                    templateRect,
                    _createTemplateIcon.Texture,
                    RimWorldUiStyle.Colors.Positive,
                    canCreateTemplate,
                    XenogermPlannerPresentation.GetTemplateCreationTooltip(readinessResult)))
            {
                OpenTemplateCreationDialog(selectedPlan, inventoryComponent);
            }

            if (RimWorldUiWidgets.DrawIconButton(
                    editRect,
                    _editPlanIcon.Texture,
                    RimWorldUiStyle.Colors.PrimaryText,
                    true,
                    "XenogermPlanner.Planner.EditPlan".Translate().ToString()))
            {
                OpenEditPlanEditor(component, selectedPlan);
            }

            if (RimWorldUiWidgets.DrawIconButton(
                    duplicateRect,
                    _duplicatePlanIcon.Texture,
                    RimWorldUiStyle.Colors.PrimaryText,
                    true,
                    "XenogermPlanner.Planner.DuplicatePlan".Translate().ToString()))
            {
                DuplicatePlan(component, selectedPlan);
            }

            if (RimWorldUiWidgets.DrawIconButton(
                    copyRect,
                    _copyPlanIcon.Texture,
                    RimWorldUiStyle.Colors.PrimaryText,
                    true,
                    "XenogermPlanner.Planner.CopyPlan".Translate().ToString()))
            {
                CopyPlanToClipboard(selectedPlan);
            }

            if (RimWorldUiWidgets.DrawIconButton(
                    deleteRect,
                    _deletePlanIcon.Texture,
                    RimWorldUiStyle.Colors.Negative,
                    true,
                    "XenogermPlanner.Planner.DeletePlan".Translate().ToString()))
            {
                ConfirmDeletePlan(component, selectedPlan);
            }
        }

        private static void OpenTemplateCreationDialog(
            XenogermPlan plan,
            PlanGenepackInventoryGameComponent inventoryComponent)
        {
            Find.WindowStack.Add(new XenogermTemplateGenerationDialog(plan, inventoryComponent));
        }

        private static void DrawCenteredMutedMessage(Rect rect, string message)
        {
            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = RimWorldUiStyle.Colors.MutedText;
                Widgets.Label(rect, message);
            }
        }

        private static void DrawNoSelectedPlan(Rect rect)
        {
            DrawCenteredMutedMessage(rect, "XenogermPlanner.Planner.NoSelectedPlan".Translate().ToString());
        }

        private static void GetSummaryColumnRects(Rect rect, out Rect leftRect, out Rect rightRect)
        {
            float leftWidth = rect.width * 0.6f;

            leftRect = new Rect(rect.x, rect.y, leftWidth, rect.height);
            rightRect = new Rect(rect.x + leftWidth, rect.y, rect.width - leftWidth, rect.height);
        }

        private static float DrawReadinessSummary(
            Rect rect,
            float y,
            XenogermPlan plan,
            PlanReadinessResult readinessResult)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            var summaryRowRect = new Rect(rect.x, y, rect.width, MetadataLineHeight);

            GetSummaryColumnRects(summaryRowRect, out Rect modeRect, out Rect statusRect);

            Widgets.Label(
                modeRect,
                "XenogermPlanner.Planner.SummaryMode".Translate(
                    XenogermPlannerPresentation.GetReadinessModeLabel(plan.ReadinessMode)));

            string readinessStatusLabel = XenogermPlannerPresentation.GetReadinessStatusLabel(readinessResult.Status);

            Widgets.Label(
                statusRect,
                "XenogermPlanner.Planner.SummaryReadiness".Translate(
                    readinessStatusLabel.Colorize(
                        XenogermPlannerWidgets.GetReadinessStatusColor(readinessResult.Status))));

            y += MetadataLineHeight;

            string coveredCount = readinessResult.Status == PlanReadinessStatus.Unavailable
                ? "—"
                : readinessResult.CoveredGenes.Count.ToString();

            string missingCount = readinessResult.Status == PlanReadinessStatus.Unavailable
                ? "—"
                : readinessResult.MissingGenes.Count.ToString();

            string genesSummary = plan.IsDegraded
                ? "XenogermPlanner.Planner.SummaryGenesDegraded".Translate(
                        XenogermPlannerPresentation.GetDesiredGeneCount(plan),
                        coveredCount,
                        missingCount,
                        plan.UnresolvedDesiredGeneDefNames.Count)
                    .ToString()
                : "XenogermPlanner.Planner.SummaryGenes".Translate(
                        XenogermPlannerPresentation.GetDesiredGeneCount(plan),
                        coveredCount,
                        missingCount)
                    .ToString();

            Widgets.Label(new Rect(rect.x, y, rect.width, MetadataLineHeight), genesSummary);

            return y + MetadataLineHeight;
        }

        private static void DrawGeneCoverageTable(
            Rect rect,
            XenogermPlan plan,
            PlanReadinessMode readinessMode,
            PlanReadinessResult readinessResult,
            IReadOnlyList<Genepack> sourceGenepacks,
            PlanPotentialDonorAnalysisResult potentialDonorAnalysis,
            GeneCoverageTableProjectionCache projectionCache,
            VariableHeightScrollListLayoutCache layoutCache,
            ref Vector2 scrollPosition,
            ref GeneCoverageSortState sortState,
            ref PlanGenepackCompositionDiagnostic hoveredComposition,
            ref PlanReadinessMode hoveredCompositionMode)
        {
            if (projectionCache == null)
                throw new ArgumentNullException(nameof(projectionCache));

            if (layoutCache == null)
                throw new ArgumentNullException(nameof(layoutCache));

            if (sourceGenepacks == null)
                throw new ArgumentNullException(nameof(sourceGenepacks));

            if (rect.width <= 0f || rect.height <= 0f)
                return;

            bool showPotentialDonors = potentialDonorAnalysis != null;

            if (!showPotentialDonors && sortState.Column == GeneCoverageSortColumn.PotentialDonorCount)
            {
                sortState = GeneCoverageSortState.Default;
                scrollPosition = Vector2.zero;
                projectionCache.Invalidate();
                layoutCache.Invalidate();
            }

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;

            var titleRect = new Rect(rect.x, rect.y, rect.width, Mathf.Min(SectionTitleHeight, rect.height));

            Widgets.Label(titleRect, "XenogermPlanner.Planner.GeneCoverage".Translate());

            float headerY = titleRect.yMax;

            if (headerY >= rect.yMax)
                return;

            float tableContentWidth = Mathf.Max(0f, rect.width - ScrollbarWidth);

            var headerRect = new Rect(
                rect.x,
                headerY,
                tableContentWidth,
                Mathf.Min(CoverageTableHeaderHeight, rect.yMax - headerY));

            GetCoverageColumnRects(
                headerRect,
                showPotentialDonors,
                out Rect geneHeaderRect,
                out Rect stateHeaderRect,
                out Rect sourcesHeaderRect,
                out Rect potentialDonorsHeaderRect);

            var sortChanged = false;

            if (RimWorldUiWidgets.DrawSortableTableHeader(
                    geneHeaderRect,
                    "XenogermPlanner.Planner.GeneColumn".Translate().ToString(),
                    sortState.Column == GeneCoverageSortColumn.Gene,
                    sortState.Descending ? SortDirection.Descending : SortDirection.Ascending,
                    "ClickToSortByThisColumn".Translate().ToString()))
            {
                sortState = sortState.Toggle(GeneCoverageSortColumn.Gene);
                sortChanged = true;
            }

            if (RimWorldUiWidgets.DrawSortableTableHeader(
                    stateHeaderRect,
                    "XenogermPlanner.Planner.StateColumn".Translate().ToString(),
                    sortState.Column == GeneCoverageSortColumn.Availability,
                    sortState.Descending ? SortDirection.Descending : SortDirection.Ascending,
                    "ClickToSortByThisColumn".Translate().ToString()))
            {
                sortState = sortState.Toggle(GeneCoverageSortColumn.Availability);
                sortChanged = true;
            }

            if (RimWorldUiWidgets.DrawSortableTableHeader(
                    sourcesHeaderRect,
                    "XenogermPlanner.Planner.AvailableGenepacksColumn".Translate().ToString(),
                    sortState.Column == GeneCoverageSortColumn.GenepackCount,
                    sortState.Descending ? SortDirection.Descending : SortDirection.Ascending,
                    "ClickToSortByThisColumn".Translate().ToString()))
            {
                sortState = sortState.Toggle(GeneCoverageSortColumn.GenepackCount);
                sortChanged = true;
            }

            if (showPotentialDonors &&
                RimWorldUiWidgets.DrawSortableTableHeader(
                    potentialDonorsHeaderRect,
                    "XenogermPlanner.Planner.PotentialDonorsColumn".Translate().ToString(),
                    sortState.Column == GeneCoverageSortColumn.PotentialDonorCount,
                    sortState.Descending ? SortDirection.Descending : SortDirection.Ascending,
                    "ClickToSortByThisColumn".Translate().ToString()))
            {
                sortState = sortState.Toggle(GeneCoverageSortColumn.PotentialDonorCount);
                sortChanged = true;
            }

            DrawCoverageHeaderDividers(
                headerRect,
                showPotentialDonors,
                geneHeaderRect,
                stateHeaderRect,
                sourcesHeaderRect,
                potentialDonorsHeaderRect);

            if (sortChanged)
            {
                scrollPosition = Vector2.zero;
                projectionCache.Invalidate();
                layoutCache.Invalidate();
            }

            var bodyRect = new Rect(rect.x, headerRect.yMax, rect.width, Mathf.Max(0f, rect.yMax - headerRect.yMax));

            if (bodyRect.width <= 0f || bodyRect.height <= 0f)
                return;

            GeneCoverageTableProjection projection = projectionCache.GetOrBuild(
                plan,
                readinessResult,
                sourceGenepacks,
                potentialDonorAnalysis,
                sortState,
                LanguageDatabase.activeLanguage);

            float viewWidth = tableContentWidth;
            VariableHeightScrollListLayout layout = GetGeneCoverageLayout(projection, viewWidth, layoutCache);

            VariableHeightScrollListVisibleRange visibleRange = RimWorldUiWidgets.BeginVariableHeightScrollView(
                bodyRect,
                ref scrollPosition,
                layout,
                out Rect viewRect);

            DrawGeneCoverageRows(
                viewRect,
                readinessMode,
                readinessResult,
                projection,
                potentialDonorAnalysis,
                layout,
                visibleRange,
                ref hoveredComposition,
                ref hoveredCompositionMode);

            Widgets.EndScrollView();
        }

        private static VariableHeightScrollListLayout GetGeneCoverageLayout(
            GeneCoverageTableProjection projection,
            float viewWidth,
            VariableHeightScrollListLayoutCache layoutCache)
        {
            int variant = projection.ShowPotentialDonors ? 1 : 0;

            if (layoutCache.TryGet(projection, variant, viewWidth, out VariableHeightScrollListLayout layout))
                return layout;

            VariableHeightScrollListRowMeasurement[] measurements = projection.Rows.Count == 0
                ? [new VariableHeightScrollListRowMeasurement(GeneRowHeight)]
                : new VariableHeightScrollListRowMeasurement[projection.Rows.Count];

            for (var index = 0; index < projection.Rows.Count; index++)
            {
                measurements[index] = new VariableHeightScrollListRowMeasurement(
                    CalculateGeneCoverageRowHeight(viewWidth, projection.Rows[index], projection.ShowPotentialDonors));
            }

            layout = VariableHeightScrollListLayout.Create(viewWidth, measurements);
            layoutCache.Store(projection, variant, layout);
            return layout;
        }

        private static void DrawGeneCoverageRows(
            Rect rect,
            PlanReadinessMode readinessMode,
            PlanReadinessResult readinessResult,
            GeneCoverageTableProjection projection,
            PlanPotentialDonorAnalysisResult potentialDonorAnalysis,
            VariableHeightScrollListLayout layout,
            VariableHeightScrollListVisibleRange visibleRange,
            ref PlanGenepackCompositionDiagnostic hoveredComposition,
            ref PlanReadinessMode hoveredCompositionMode)
        {
            if (projection.Rows.Count == 0)
            {
                if (!visibleRange.Contains(0))
                    return;

                using (ImGuiStateScope.Capture())
                {
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    GUI.color = RimWorldUiStyle.Colors.MutedText;

                    Widgets.Label(
                        new Rect(rect.x, rect.y, rect.width, layout.GetRowHeight(0)),
                        "XenogermPlanner.Planner.NoResolvedGeneCoverage".Translate());
                }

                return;
            }

            for (int rowIndex = visibleRange.FirstVisibleIndex;
                 rowIndex < visibleRange.LastVisibleIndexExclusive;
                 rowIndex++)
            {
                GeneCoverageTablePresentationRow presentationRow = projection.Rows[rowIndex];
                GeneCoverageTableRow row = presentationRow.Row;
                var rowRect = new Rect(
                    rect.x,
                    rect.y + layout.GetRowOffset(rowIndex),
                    rect.width,
                    layout.GetRowHeight(rowIndex));

                RimWorldUiWidgets.DrawSelectableRowBackground(
                    rowRect,
                    rowIndex,
                    selected: false,
                    hovered: Mouse.IsOver(rowRect),
                    drawAccent: false);

                GetCoverageColumnRects(
                    rowRect,
                    projection.ShowPotentialDonors,
                    out Rect geneRect,
                    out Rect stateRect,
                    out Rect sourcesRect,
                    out Rect potentialDonorsRect);

                if (row.IsResolved)
                {
                    PlanGeneCoverageDiagnostic diagnostic = row.Diagnostic;

                    XenogermPlannerWidgets.DrawGeneLabel(geneRect, diagnostic.Gene);
                    XenogermPlannerWidgets.AddGeneTooltip(geneRect, diagnostic.Gene);
                    XenogermPlannerNativeInspector.TryOpenContextMenu(geneRect, diagnostic.Gene);

                    string stateLabel = XenogermPlannerPresentation.GetGeneCoverageStateLabel(diagnostic);
                    string stateTooltip = XenogermPlannerPresentation.GetGeneCoverageStateTooltip(diagnostic);

                    using (ImGuiStateScope.Capture())
                    {
                        GUI.color = XenogermPlannerWidgets.GetGeneCoverageStateColor(diagnostic.State);

                        RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                            stateRect,
                            stateLabel,
                            GameFont.Small,
                            TextAnchor.MiddleLeft,
                            stateTooltip);
                    }

                    if (diagnostic.IsCovered)
                    {
                        bool showExactPayloadEligibility = readinessMode == PlanReadinessMode.ExactPayload &&
                                                           readinessResult.Status != PlanReadinessStatus.Degraded;

                        DrawGenepackTargetIcons(
                            sourcesRect,
                            presentationRow.SourceGroups,
                            showExactPayloadEligibility ? PlanReadinessMode.ExactPayload : PlanReadinessMode.Coverage,
                            ref hoveredComposition,
                            ref hoveredCompositionMode);
                    }
                    else
                    {
                        using (ImGuiStateScope.Capture())
                        {
                            GUI.color = RimWorldUiStyle.Colors.MutedText;
                            Widgets.Label(sourcesRect, "XenogermPlanner.Planner.NoSourceGenepacks".Translate());
                        }
                    }

                    if (projection.ShowPotentialDonors)
                        DrawPotentialDonorCell(potentialDonorsRect, diagnostic, potentialDonorAnalysis);
                }
                else
                {
                    using (ImGuiStateScope.Capture())
                    {
                        GUI.color = RimWorldUiStyle.Colors.Warning;
                        Text.Font = GameFont.Small;
                        Text.Anchor = TextAnchor.MiddleLeft;

                        Widgets.Label(geneRect, row.UnresolvedGeneDefName);
                        Widgets.Label(stateRect, "XenogermPlanner.GeneCoverageState.Unavailable".Translate());

                        GUI.color = RimWorldUiStyle.Colors.MutedText;
                        Widgets.Label(sourcesRect, "XenogermPlanner.Planner.NoSourceGenepacks".Translate());
                    }

                    if (projection.ShowPotentialDonors)
                        DrawPotentialDonorPlaceholder(potentialDonorsRect);
                }
            }
        }

        private static void DrawPotentialDonorCell(
            Rect rect,
            PlanGeneCoverageDiagnostic coverageDiagnostic,
            PlanPotentialDonorAnalysisResult potentialDonorAnalysis)
        {
            if (coverageDiagnostic == null)
                throw new ArgumentNullException(nameof(coverageDiagnostic));

            if (potentialDonorAnalysis == null)
                throw new ArgumentNullException(nameof(potentialDonorAnalysis));

            if (coverageDiagnostic.IsCovered)
            {
                DrawPotentialDonorPlaceholder(rect);
                return;
            }

            if (!potentialDonorAnalysis.IsAvailable)
            {
                DrawPotentialDonorPlaceholder(
                    rect,
                    "XenogermPlanner.Planner.PotentialDonorsUnavailable".Translate().ToString());
                return;
            }

            if (!XenogermPlannerPresentation.TryGetPotentialDonorDiagnostic(
                    coverageDiagnostic,
                    potentialDonorAnalysis,
                    out PlanPotentialDonorGeneDiagnostic donorDiagnostic))
            {
                DrawPotentialDonorPlaceholder(rect);
                return;
            }

            var tooltip = "XenogermPlanner.Planner.PotentialDonorCountTooltip".Translate(
                    donorDiagnostic.DonorCount,
                    XenogermPlannerPresentation.GetGeneDisplayName(donorDiagnostic.Gene))
                .ToString();

            TooltipHandler.TipRegion(rect, tooltip);

            if (!donorDiagnostic.HasDonors)
            {
                DrawPotentialDonorCount(rect, donorDiagnostic.DonorCount, RimWorldUiStyle.Colors.MutedText);
                return;
            }

            Rect buttonRect = rect.ContractedBy(CoveragePotentialDonorButtonInset);

            if (Widgets.ButtonText(buttonRect, donorDiagnostic.DonorCount.ToString()))
                Find.WindowStack.Add(new PotentialDonorDetailsDialog(donorDiagnostic.Gene));
        }

        private static void DrawPotentialDonorCount(Rect rect, int count, Color color)
        {
            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = color;

                Widgets.Label(rect, count.ToString());
            }
        }

        private static void DrawPotentialDonorPlaceholder(Rect rect, string tooltip = null)
        {
            if (!string.IsNullOrWhiteSpace(tooltip))
                TooltipHandler.TipRegion(rect, tooltip);

            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = RimWorldUiStyle.Colors.MutedText;

                Widgets.Label(rect, "—");
            }
        }

        private static void DrawGenepackTargetIcons(
            Rect rect,
            IReadOnlyList<GeneCoverageTableSourceGroup> sourceGroups,
            PlanReadinessMode readinessMode,
            ref PlanGenepackCompositionDiagnostic hoveredComposition,
            ref PlanReadinessMode hoveredCompositionMode)
        {
            if (sourceGroups == null)
                throw new ArgumentNullException(nameof(sourceGroups));

            float x = rect.x;
            float y = rect.y + 2f;

            foreach (GeneCoverageTableSourceGroup sourceGroup in sourceGroups)
            {
                foreach (Genepack genepack in sourceGroup.Genepacks)
                {
                    if (x > rect.x && x + XenogermPlannerWidgets.GenepackTargetIconSize > rect.xMax)
                    {
                        x = rect.x;
                        y += XenogermPlannerWidgets.GenepackTargetIconSize +
                             XenogermPlannerWidgets.GenepackTargetIconGap;
                    }

                    var iconRect = new Rect(
                        x,
                        y,
                        XenogermPlannerWidgets.GenepackTargetIconSize,
                        XenogermPlannerWidgets.GenepackTargetIconSize);

                    if (XenogermPlannerWidgets.DrawGenepackTargetIcon(
                            iconRect,
                            genepack,
                            sourceGroup.Composition,
                            readinessMode))
                    {
                        hoveredComposition = sourceGroup.Composition;
                        hoveredCompositionMode = readinessMode;
                    }

                    x += XenogermPlannerWidgets.GenepackTargetIconSize + XenogermPlannerWidgets.GenepackTargetIconGap;
                }
            }
        }

        private static void DrawCoverageHeaderDividers(
            Rect headerRect,
            bool showPotentialDonors,
            Rect geneRect,
            Rect stateRect,
            Rect sourcesRect,
            Rect potentialDonorsRect)
        {
            DrawCoverageHeaderDivider(headerRect, geneRect.xMax, stateRect.x);
            DrawCoverageHeaderDivider(headerRect, stateRect.xMax, sourcesRect.x);

            if (showPotentialDonors)
                DrawCoverageHeaderDivider(headerRect, sourcesRect.xMax, potentialDonorsRect.x);
        }

        private static void DrawCoverageHeaderDivider(Rect headerRect, float left, float right)
        {
            float width = Mathf.Max(0f, right - left);

            RimWorldUiWidgets.DrawTableDivider(new Rect(left, headerRect.y, width, headerRect.height));
        }

        private static void GetCoverageColumnRects(
            Rect rect,
            bool showPotentialDonors,
            out Rect geneRect,
            out Rect stateRect,
            out Rect sourcesRect,
            out Rect potentialDonorsRect)
        {
            float potentialDonorWidth = showPotentialDonors
                ? Mathf.Min(CoveragePotentialDonorColumnWidth, rect.width * 0.24f)
                : 0f;

            float primaryColumnsWidth = showPotentialDonors
                ? Mathf.Max(0f, rect.width - potentialDonorWidth - CoverageColumnGap)
                : rect.width;

            float geneWidth = primaryColumnsWidth * CoverageGeneColumnFraction;
            float stateWidth = primaryColumnsWidth * CoverageStateColumnFraction;
            float primaryColumnsRight = rect.x + primaryColumnsWidth;

            geneRect = new Rect(rect.x, rect.y, geneWidth, rect.height);

            stateRect = new Rect(geneRect.xMax + CoverageColumnGap, rect.y, stateWidth, rect.height);

            sourcesRect = new Rect(
                stateRect.xMax + CoverageColumnGap,
                rect.y,
                Mathf.Max(0f, primaryColumnsRight - stateRect.xMax - CoverageColumnGap),
                rect.height);

            potentialDonorsRect = showPotentialDonors
                ? new Rect(primaryColumnsRight + CoverageColumnGap, rect.y, potentialDonorWidth, rect.height)
                : new Rect(rect.xMax, rect.y, 0f, rect.height);
        }

        private static void CopyPlanToClipboard(XenogermPlan plan)
        {
            GUIUtility.systemCopyBuffer = XenogermPlanTransferCodec.Serialize(plan);

            Messages.Message("XenogermPlanner.Planner.PlanCopied".Translate(), MessageTypeDefOf.PositiveEvent, false);
        }

        private void PastePlanFromClipboard(XenogermPlanGameComponent component)
        {
            bool deserialized = XenogermPlanTransferCodec.TryDeserialize(
                GUIUtility.systemCopyBuffer,
                DefDatabase<GeneDef>.GetNamedSilentFail,
                out XenogermPlan plan,
                out XenogermPlanTransferFailure failure);

            if (!deserialized)
            {
                string messageKey = failure == XenogermPlanTransferFailure.UnsupportedVersion
                    ? "XenogermPlanner.Planner.UnsupportedClipboardPlanVersion"
                    : "XenogermPlanner.Planner.InvalidClipboardPlan";

                Messages.Message(messageKey.Translate(), MessageTypeDefOf.RejectInput, false);

                return;
            }

            if (plan.IsDegraded)
            {
                ConfirmPasteDegradedPlan(component, plan);

                return;
            }

            AddPastedPlan(component, plan);
        }

        private void ConfirmPasteDegradedPlan(XenogermPlanGameComponent component, XenogermPlan plan)
        {
            var message = "XenogermPlanner.Planner.PasteDegradedPlanConfirmation".Translate().ToString();

            Find.WindowStack.Add(
                Dialog_MessageBox.CreateConfirmation(message, () => AddPastedPlan(component, plan), true));
        }

        private void AddPastedPlan(XenogermPlanGameComponent component, XenogermPlan plan)
        {
            component.AddPlanWithAllocatedName(plan);
            HandlePlanSaved(plan);
        }

        private void ShowCreatePlanMenu(XenogermPlanGameComponent component)
        {
            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption(
                    "XenogermPlanner.PlanCreation.FromScratch".Translate().ToString(),
                    () => OpenCreatePlanEditor(component)),
                new FloatMenuOption(
                    "XenogermPlanner.PlanCreation.FromXenogermTemplate".Translate().ToString(),
                    () => OpenXenogermTemplateSourceDialog(component)),
                new FloatMenuOption(
                    "XenogermPlanner.PlanCreation.FromXenotype".Translate().ToString(),
                    () => OpenXenotypeSourceDialog(component))
            };

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void OpenXenogermTemplateSourceDialog(XenogermPlanGameComponent component)
        {
            Find.WindowStack.Add(
                new XenogermPlanSourceDialog(
                    new CustomXenogermPlanSourceProvider(),
                    selection => OpenCreatePlanEditorFromSource(component, selection)));
        }

        private void OpenXenotypeSourceDialog(XenogermPlanGameComponent component)
        {
            Find.WindowStack.Add(
                new XenogermPlanSourceDialog(
                    new XenotypePlanSourceProvider(),
                    selection => OpenCreatePlanEditorFromSource(component, selection)));
        }

        private void OpenCreatePlanEditorFromSource(
            XenogermPlanGameComponent component,
            XenogermPlanSourceSelection selection)
        {
            if (selection == null)
                return;

            string initialPlanName = component.AllocateUniquePlanName(selection.Name);

            var initialState = XenogermPlanEditorInitialState.CreateFromSource(initialPlanName, selection.DesiredGenes);

            OpenCreatePlanEditor(component, initialState);
        }

        private void OpenCreatePlanEditor(XenogermPlanGameComponent component)
        {
            Find.WindowStack.Add(new XenogermPlanEditorDialog(component, HandlePlanSaved));
        }

        private void OpenCreatePlanEditor(
            XenogermPlanGameComponent component,
            XenogermPlanEditorInitialState initialState)
        {
            Find.WindowStack.Add(new XenogermPlanEditorDialog(component, initialState, HandlePlanSaved));
        }

        private void OpenEditPlanEditor(XenogermPlanGameComponent component, XenogermPlan plan)
        {
            Find.WindowStack.Add(new XenogermPlanEditorDialog(component, plan, HandlePlanSaved));
        }

        private void HandlePlanSaved(XenogermPlan plan)
        {
            Current.Game?.GetComponent<PlanReadinessNotificationGameComponent>()?.Invalidate();
            Current.Game?.GetComponent<PlanTraderAdvisoryGameComponent>()?.Invalidate();
            _analysisCache.Invalidate();
            InvalidatePresentationProjections();
            InvalidateVariableHeightLayouts();

            SelectPlan(plan.Id);
            _productGeneCoverageScrollPosition = Vector2.zero;
            _assemblerGeneCoverageScrollPosition = Vector2.zero;
            _geneDiagnosticsScrollPosition = Vector2.zero;
        }

        private void DuplicatePlan(XenogermPlanGameComponent component, XenogermPlan plan)
        {
            XenogermPlan duplicate = plan.CreateDuplicate();

            string sourceDisplayName = XenogermPlannerPresentation.GetPlanDisplayName(plan);

            duplicate.Rename("XenogermPlanner.Planner.DuplicatePlanName".Translate(sourceDisplayName).ToString());

            component.AddPlanWithAllocatedName(duplicate);
            HandlePlanSaved(duplicate);
        }

        private void ConfirmDeletePlan(XenogermPlanGameComponent component, XenogermPlan plan)
        {
            string displayName = XenogermPlannerPresentation.GetPlanDisplayName(plan);

            string message = "XenogermPlanner.Planner.DeletePlanConfirmation".Translate(displayName).ToString() +
                             "\n\n" +
                             "XenogermPlanner.Planner.DeletePlanWarning".Translate().ToString();

            string planId = plan.Id;

            Find.WindowStack.Add(
                Dialog_MessageBox.CreateConfirmation(message, () => DeletePlan(component, planId), true));
        }

        private void DeletePlan(XenogermPlanGameComponent component, string planId)
        {
            component.RemovePlan(planId);
            Current.Game?.GetComponent<PlanTraderAdvisoryGameComponent>()?.Invalidate();
            InvalidatePresentationProjections();
            InvalidateVariableHeightLayouts();

            if (string.Equals(_selectedPlanId, planId, StringComparison.Ordinal))
            {
                SelectPlan(null);
            }

            ResolveSelectedPlan(component.Plans);
        }

        private void SelectAssembler(Building_GeneAssembler assembler)
        {
            if (ReferenceEquals(_selectedAssembler, assembler))
            {
                return;
            }

            _selectedAssembler = assembler;
            _assemblerDetailsExpanded = false;
            _assemblerGeneCoverageProjectionCache.Invalidate();
            _assemblerGeneCoverageLayoutCache.Invalidate();
            _assemblerGeneCoverageScrollPosition = Vector2.zero;
        }

        private Building_GeneAssembler ResolveSelectedAssembler(
            Map activeMap,
            IReadOnlyList<Building_GeneAssembler> selectableAssemblers)
        {
            if (selectableAssemblers == null)
            {
                throw new ArgumentNullException(nameof(selectableAssemblers));
            }

            if (!ReferenceEquals(_selectedAssemblerMap, activeMap))
            {
                _selectedAssemblerMap = activeMap;
                _selectedAssembler = null;
                _assemblerDetailsExpanded = false;
                _productGeneCoverageProjectionCache.Invalidate();
                _assemblerGeneCoverageProjectionCache.Invalidate();
                _productGeneCoverageLayoutCache.Invalidate();
                _assemblerGeneCoverageLayoutCache.Invalidate();
                _assemblerGeneCoverageScrollPosition = Vector2.zero;
            }

            if (_selectedAssembler == null)
                return null;

            foreach (Building_GeneAssembler assembler in selectableAssemblers)
            {
                if (ReferenceEquals(assembler, _selectedAssembler))
                {
                    return _selectedAssembler;
                }
            }

            SelectAssembler(null);

            return null;
        }

        private XenogermPlan ResolveSelectedPlan(IReadOnlyList<XenogermPlan> plans)
        {
            if (plans == null || plans.Count == 0)
            {
                SelectPlan(null);
                return null;
            }

            if (!string.IsNullOrEmpty(_selectedPlanId))
            {
                foreach (XenogermPlan plan in plans)
                {
                    if (string.Equals(plan.Id, _selectedPlanId, StringComparison.Ordinal))
                    {
                        return plan;
                    }
                }
            }

            XenogermPlan firstPlan = plans[0];

            SelectPlan(firstPlan.Id);

            return firstPlan;
        }

        private void SelectPlan(string planId)
        {
            if (string.Equals(_selectedPlanId, planId, StringComparison.Ordinal))
            {
                return;
            }

            _selectedPlanId = planId;
            _productGeneCoverageProjectionCache.Invalidate();
            _assemblerGeneCoverageProjectionCache.Invalidate();
            InvalidateGeneDiagnosticsProjection();
            InvalidateVariableHeightLayouts();
            _productGeneCoverageScrollPosition = Vector2.zero;
            _assemblerGeneCoverageScrollPosition = Vector2.zero;
            _geneDiagnosticsScrollPosition = Vector2.zero;
        }

        private IReadOnlyList<XenogermPlan> GetFilteredPlanProjection(IReadOnlyList<XenogermPlan> plans)
        {
            if (plans == null)
                throw new ArgumentNullException(nameof(plans));

            string normalizedQuery = (_planSearchText ?? string.Empty).Trim();
            object languageKey = LanguageDatabase.activeLanguage;

            if (_filteredPlanProjection != null &&
                ReferenceEquals(_filteredPlanSource, plans) &&
                string.Equals(_filteredPlanQuery, normalizedQuery, StringComparison.Ordinal) &&
                Equals(_filteredPlanLanguageKey, languageKey))
            {
                return _filteredPlanProjection;
            }

            _filteredPlanSource = plans;
            _filteredPlanQuery = normalizedQuery;
            _filteredPlanLanguageKey = languageKey;
            _filteredPlanProjection = XenogermPlannerPresentation.GetFilteredPlans(plans, normalizedQuery);
            return _filteredPlanProjection;
        }

        private GeneTargetDiagnosticsProjection GetGeneDiagnosticsProjection(
            XenogermPlan plan,
            PlanGeneTargetAnalysisResult targetAnalysis)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (targetAnalysis == null)
                throw new ArgumentNullException(nameof(targetAnalysis));

            object languageKey = LanguageDatabase.activeLanguage;

            if (_geneDiagnosticsProjection != null &&
                ReferenceEquals(_geneDiagnosticsPlan, plan) &&
                ReferenceEquals(_geneDiagnosticsDesiredGenesKey, plan.DesiredGenes) &&
                ReferenceEquals(_geneDiagnosticsUnresolvedGenesKey, plan.UnresolvedDesiredGeneDefNames) &&
                _geneDiagnosticsReadinessMode == plan.ReadinessMode &&
                Equals(_geneDiagnosticsLanguageKey, languageKey))
            {
                return _geneDiagnosticsProjection;
            }

            _geneDiagnosticsPlan = plan;
            _geneDiagnosticsDesiredGenesKey = plan.DesiredGenes;
            _geneDiagnosticsUnresolvedGenesKey = plan.UnresolvedDesiredGeneDefNames;
            _geneDiagnosticsReadinessMode = plan.ReadinessMode;
            _geneDiagnosticsLanguageKey = languageKey;
            _geneDiagnosticsProjection = GeneTargetDiagnosticsProjection.Build(targetAnalysis, plan.ReadinessMode);
            _geneDiagnosticsLayoutCache.Invalidate();
            return _geneDiagnosticsProjection;
        }

        private void InvalidatePresentationProjections()
        {
            InvalidateFilteredPlanProjection();
            _productGeneCoverageProjectionCache.Invalidate();
            _assemblerGeneCoverageProjectionCache.Invalidate();
            InvalidateGeneDiagnosticsProjection();
        }

        private void InvalidateFilteredPlanProjection()
        {
            _filteredPlanSource = null;
            _filteredPlanQuery = null;
            _filteredPlanLanguageKey = null;
            _filteredPlanProjection = null;
        }

        private void InvalidateGeneDiagnosticsProjection()
        {
            _geneDiagnosticsPlan = null;
            _geneDiagnosticsDesiredGenesKey = null;
            _geneDiagnosticsUnresolvedGenesKey = null;
            _geneDiagnosticsLanguageKey = null;
            _geneDiagnosticsProjection = null;
            _geneDiagnosticsLayoutCache.Invalidate();
        }

        private void InvalidateVariableHeightLayouts()
        {
            _productGeneCoverageLayoutCache.Invalidate();
            _assemblerGeneCoverageLayoutCache.Invalidate();
            _geneDiagnosticsLayoutCache.Invalidate();
        }

        private static float CalculateGeneCoverageRowHeight(
            float tableWidth,
            GeneCoverageTablePresentationRow presentationRow,
            bool showPotentialDonors)
        {
            if (presentationRow == null)
                throw new ArgumentNullException(nameof(presentationRow));

            if (!presentationRow.Row.IsResolved || !presentationRow.Row.Diagnostic.IsCovered)
                return CoverageTableRowMinHeight;

            GetCoverageColumnRects(
                new Rect(0f, 0f, tableWidth, CoverageTableRowMinHeight),
                showPotentialDonors,
                out _,
                out _,
                out Rect sourcesRect,
                out _);

            float iconsHeight = CalculateGenepackTargetIconsHeight(sourcesRect.width, presentationRow.SourceGroups);

            return Mathf.Max(CoverageTableRowMinHeight, iconsHeight + 4f);
        }

        private static float CalculateGenepackTargetIconsHeight(
            float width,
            IReadOnlyList<GeneCoverageTableSourceGroup> sourceGroups)
        {
            if (sourceGroups == null)
                throw new ArgumentNullException(nameof(sourceGroups));

            var iconCount = 0;

            foreach (GeneCoverageTableSourceGroup sourceGroup in sourceGroups)
                iconCount += sourceGroup.Genepacks.Count;

            if (iconCount == 0)
                return 0f;

            var x = 0f;
            var rowCount = 1;

            for (var index = 0; index < iconCount; index++)
            {
                if (x > 0f && x + XenogermPlannerWidgets.GenepackTargetIconSize > width)
                {
                    x = 0f;
                    rowCount++;
                }

                x += XenogermPlannerWidgets.GenepackTargetIconSize + XenogermPlannerWidgets.GenepackTargetIconGap;
            }

            return rowCount * XenogermPlannerWidgets.GenepackTargetIconSize +
                   (rowCount - 1) * XenogermPlannerWidgets.GenepackTargetIconGap;
        }


        private static float GetTextHeight(string text, float width, GameFont font)
        {
            using (ImGuiStateScope.Capture())
            {
                Text.Font = font;
                return Text.CalcHeight(text, width);
            }
        }

        private enum PlanDetailsTab
        {
            Overview,
            Assembler,
            GeneEffects
        }
    }
}