using System;
using System.Collections.Generic;
using Escarval.RimWorld.UI;
using RimWorld;
using UnityEngine;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.UI
{
    [StaticConstructorOnStartup]
    public sealed class XenogermPlanEditorDialog : Window
    {
        private const float DialogWidth = 1000f;
        private const float DialogHeight = 700f;
        private const float TitleHeight = 34f;
        private const float FieldHeight = 30f;
        private const float FieldGap = RimWorldUiStyle.Metrics.ControlGap;
        private const float FieldLabelWidth = 150f;
        private const float ReadinessOptionsStartGap = 12f;
        private const float SectionGap = RimWorldUiStyle.Metrics.SectionGap;
        private const float SectionPadding = RimWorldUiStyle.Metrics.PanelPadding;
        private const float SectionTitleHeight = RimWorldUiStyle.Metrics.HeaderHeight;
        private const float SearchRowHeight = RimWorldUiStyle.Metrics.SearchRowHeight;
        private const float CatalogRowHeight = RimWorldUiStyle.Metrics.CompactRowHeight;
        private const float GeneRowHeight = RimWorldUiStyle.Metrics.CompactRowHeight;
        private const float SelectedHeaderGap = 8f;
        private const float SectionHeaderContentGap = RimWorldUiStyle.Metrics.SectionGap;
        private const float FooterHeight = 40f;
        private const float FooterButtonWidth = 120f;
        private const float FooterButtonHeight = 35f;
        private const float FooterButtonGap = 8f;

        private const float BiostatSummaryHeight =
            RimWorldUiStyle.Metrics.TwoLineRowHeight + RimWorldUiStyle.Metrics.SmallGap;

        private const float WarningPadding = RimWorldUiStyle.Metrics.PanelPadding;

        private static readonly ReloadableTexture2D _addAllFilteredIcon =
            new ReloadableTexture2D("UI/Buttons/AddAllFiltered");

        private static readonly ReloadableTexture2D _expandAllCategoriesIcon =
            new ReloadableTexture2D("UI/Buttons/ExpandAll");

        private static readonly ReloadableTexture2D _collapseAllCategoriesIcon =
            new ReloadableTexture2D("UI/Buttons/CollapseAll");

        private static readonly ReloadableTexture2D _clearSelectedGenesIcon =
            new ReloadableTexture2D("UI/Buttons/Delete");

        private readonly XenogermPlanGameComponent _component;
        private readonly XenogermPlan _existingPlan;
        private readonly Action<XenogermPlan> _onSaved;
        private readonly HashSet<GeneDef> _selectedGenes;
        private readonly List<GeneDef> _catalogGenes;
        private readonly Dictionary<GeneCategoryDef, bool> _collapsedCategories;

        private List<GeneDef> _filteredGenes;
        private List<GeneCatalogRow> _catalogRows;
        private List<GeneDef> _sortedSelectedGenes;
        private string _planName;
        private PlanReadinessMode _readinessMode;
        private bool _readinessNotificationsEnabled;
        private string _searchText;
        private Vector2 _catalogScrollPosition;
        private Vector2 _selectedGenesScrollPosition;
        private Vector2 _geneDiagnosticsScrollPosition;

        private readonly VariableHeightScrollListLayoutCache _geneDiagnosticsLayoutCache =
            new VariableHeightScrollListLayoutCache();

        private PlanGeneTargetAnalysisResult _targetAnalysis;
        private PlanGeneBiostats _selectedGeneBiostats;
        private bool _selectedGeneBiostatsPartial;
        private GeneTargetDiagnosticsProjection _targetDiagnosticsProjection;
        private object _presentationLanguageKey;

        private bool IsEditMode => _existingPlan != null;

        private bool IsSearchActive =>
            !string.IsNullOrWhiteSpace(_searchText);

        public override Vector2 InitialSize =>
            new Vector2(DialogWidth, DialogHeight);

        public XenogermPlanEditorDialog(XenogermPlanGameComponent component, Action<XenogermPlan> onSaved) : this(
            component,
            onSaved,
            existingPlan: null,
            XenogermPlanEditorInitialState.CreateEmpty())
        {
        }

        internal XenogermPlanEditorDialog(
            XenogermPlanGameComponent component,
            XenogermPlanEditorInitialState initialState,
            Action<XenogermPlan> onSaved) : this(
            component,
            onSaved,
            existingPlan: null,
            initialState ?? throw new ArgumentNullException(nameof(initialState)))
        {
        }

        public XenogermPlanEditorDialog(
            XenogermPlanGameComponent component,
            XenogermPlan existingPlan,
            Action<XenogermPlan> onSaved) : this(
            component,
            onSaved,
            existingPlan ?? throw new ArgumentNullException(nameof(existingPlan)),
            initialState: null)
        {
        }

        private XenogermPlanEditorDialog(
            XenogermPlanGameComponent component,
            Action<XenogermPlan> onSaved,
            XenogermPlan existingPlan,
            XenogermPlanEditorInitialState initialState)
        {
            _component = component ?? throw new ArgumentNullException(nameof(component));
            _existingPlan = existingPlan;
            _onSaved = onSaved;

            _planName = existingPlan?.Name ?? initialState?.PlanName ?? string.Empty;
            _readinessMode = existingPlan?.ReadinessMode ?? initialState?.ReadinessMode ?? PlanReadinessMode.Coverage;
            _readinessNotificationsEnabled = existingPlan?.ReadinessNotificationsEnabled ??
                                             initialState?.ReadinessNotificationsEnabled ?? true;
            _searchText = string.Empty;

            _selectedGenes = existingPlan != null
                ? new HashSet<GeneDef>(existingPlan.DesiredGenes)
                : new HashSet<GeneDef>(initialState?.DesiredGenes ?? Array.Empty<GeneDef>());

            _catalogGenes =
                XenogermPlannerPresentation.GetGenesInCatalogOrder(DefDatabase<GeneDef>.AllDefsListForReading);

            _collapsedCategories = new Dictionary<GeneCategoryDef, bool>();

            InitializeCollapsedCategories();

            _filteredGenes = new List<GeneDef>(_catalogGenes);
            RefreshCatalogProjection();
            RefreshTargetAnalysis();

            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            forcePause = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            using (ImGuiStateScope.Capture())
            {
                RefreshLanguageDependentPresentationIfNeeded();

                Rect contentRect = inRect;
                float y = contentRect.y;

                DrawTitle(new Rect(contentRect.x, y, contentRect.width, TitleHeight));

                y += TitleHeight;

                DrawNameField(new Rect(contentRect.x, y, contentRect.width, FieldHeight));

                y += FieldHeight + FieldGap;

                DrawReadinessModeField(new Rect(contentRect.x, y, contentRect.width, FieldHeight));

                y += FieldHeight + FieldGap;

                DrawReadinessNotificationsField(new Rect(contentRect.x, y, contentRect.width, FieldHeight));

                y += FieldHeight + FieldGap;

                if (IsEditMode && _existingPlan.IsDegraded)
                {
                    float warningHeight = CalculateDegradedWarningHeight(contentRect.width);

                    DrawDegradedWarning(new Rect(contentRect.x, y, contentRect.width, warningHeight));

                    y += warningHeight + FieldGap;
                }

                DrawBiostatSummary(new Rect(contentRect.x, y, contentRect.width, BiostatSummaryHeight));

                y += BiostatSummaryHeight + FieldGap;

                var footerRect = new Rect(
                    contentRect.x,
                    contentRect.yMax - FooterHeight,
                    contentRect.width,
                    FooterHeight);

                float sectionsHeight = footerRect.y - y - SectionGap;

                var workspaceRect = new Rect(contentRect.x, y, contentRect.width, Mathf.Max(0f, sectionsHeight));

                DrawWorkspace(workspaceRect);
                DrawFooter(footerRect);
            }
        }

        private void DrawWorkspace(Rect rect)
        {
            if (!_targetAnalysis.HasDiagnostics)
            {
                DrawGeneSections(rect);
                return;
            }

            float diagnosticsWidth = XenogermPlannerWidgets.CalculateGeneTargetDiagnosticsPanelWidth(rect.width);
            float geneSectionsWidth = Mathf.Max(0f, rect.width - diagnosticsWidth - SectionGap);

            var geneSectionsRect = new Rect(rect.x, rect.y, geneSectionsWidth, rect.height);
            var diagnosticsRect = new Rect(
                geneSectionsRect.xMax + SectionGap,
                rect.y,
                Mathf.Max(0f, rect.xMax - geneSectionsRect.xMax - SectionGap),
                rect.height);

            DrawGeneSections(geneSectionsRect);

            XenogermPlannerWidgets.DrawGeneTargetDiagnosticsPanel(
                diagnosticsRect,
                _targetDiagnosticsProjection,
                _geneDiagnosticsLayoutCache,
                ref _geneDiagnosticsScrollPosition);
        }

        private void DrawTitle(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;

            Widgets.Label(
                rect,
                IsEditMode
                    ? "XenogermPlanner.PlanEditor.EditTitle".Translate()
                    : "XenogermPlanner.PlanEditor.CreateTitle".Translate());
        }

        private bool CanAddAllFilteredGenes()
        {
            foreach (GeneDef gene in _filteredGenes)
            {
                if (!_selectedGenes.Contains(gene))
                    return true;
            }

            return false;
        }

        private bool CanClearSelectedGenes()
        {
            return _selectedGenes.Count > 0;
        }

        private bool CanExpandAllCategories()
        {
            if (IsSearchActive)
                return false;

            foreach (bool collapsed in _collapsedCategories.Values)
            {
                if (collapsed)
                    return true;
            }

            return false;
        }

        private bool CanCollapseAllCategories()
        {
            if (IsSearchActive)
                return false;

            foreach (bool collapsed in _collapsedCategories.Values)
            {
                if (!collapsed)
                    return true;
            }

            return false;
        }

        private void AddAllFilteredGenes()
        {
            var changed = false;

            foreach (GeneDef gene in _filteredGenes)
                changed |= _selectedGenes.Add(gene);

            if (!changed)
                return;

            _selectedGenesScrollPosition = Vector2.zero;
            RefreshTargetAnalysis();
        }

        private void ClearSelectedGenes()
        {
            if (_selectedGenes.Count == 0)
                return;

            _selectedGenes.Clear();
            _selectedGenesScrollPosition = Vector2.zero;
            RefreshTargetAnalysis();
        }

        private void ExpandAllCategories()
        {
            if (IsSearchActive)
                return;

            SetAllCategoriesCollapsed(false);
        }

        private void CollapseAllCategories()
        {
            if (IsSearchActive)
                return;

            SetAllCategoriesCollapsed(true);
        }

        private void SetAllCategoriesCollapsed(bool collapsed)
        {
            var categories = new List<GeneCategoryDef>(_collapsedCategories.Keys);

            foreach (GeneCategoryDef category in categories)
                _collapsedCategories[category] = collapsed;

            RefreshCatalogProjection();
            _catalogScrollPosition = Vector2.zero;
        }

        private void DrawNameField(Rect rect)
        {
            var labelRect = new Rect(rect.x, rect.y, FieldLabelWidth, rect.height);

            var fieldRect = new Rect(labelRect.xMax, rect.y, rect.width - FieldLabelWidth, rect.height);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            Widgets.Label(labelRect, "XenogermPlanner.PlanEditor.Name".Translate());

            _planName = Widgets.TextField(fieldRect, _planName ?? string.Empty);
        }

        private void DrawReadinessModeField(Rect rect)
        {
            var labelRect = new Rect(rect.x, rect.y, FieldLabelWidth, rect.height);

            var selectorRect = new Rect(
                labelRect.xMax + ReadinessOptionsStartGap,
                rect.y,
                rect.xMax - labelRect.xMax - ReadinessOptionsStartGap,
                rect.height);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            Widgets.Label(labelRect, "XenogermPlanner.PlanEditor.ReadinessMode".Translate());

            PlanReadinessMode previousMode = _readinessMode;
            XenogermPlannerWidgets.DrawReadinessModeSelector(selectorRect, ref _readinessMode);

            if (_readinessMode != previousMode)
                RefreshDiagnosticsProjection();
        }

        private void DrawReadinessNotificationsField(Rect rect)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            Widgets.CheckboxLabeled(
                rect,
                "XenogermPlanner.Notifications.ReadinessEnabled".Translate(),
                ref _readinessNotificationsEnabled);

            TooltipHandler.TipRegion(rect, "XenogermPlanner.Notifications.ReadinessEnabledDescription".Translate());
        }

        private static void DrawDegradedWarning(Rect rect)
        {
            using (ImGuiStateScope.Capture())
            {
                GUI.color = RimWorldUiStyle.Colors.Warning;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;

                Widgets.Label(rect, "XenogermPlanner.PlanEditor.DegradedTargetWarning".Translate());
            }
        }

        private void DrawBiostatSummary(Rect rect)
        {
            XenogermPlannerWidgets.DrawPlanEditorBiostatSummary(
                rect,
                _selectedGeneBiostats,
                _selectedGeneBiostatsPartial);
        }

        private void DrawGeneSections(Rect rect)
        {
            float sectionWidth = (rect.width - SectionGap) * 0.5f;

            var catalogRect = new Rect(rect.x, rect.y, sectionWidth, rect.height);

            var selectedRect = new Rect(
                catalogRect.xMax + SectionGap,
                rect.y,
                rect.width - sectionWidth - SectionGap,
                rect.height);

            DrawGeneCatalog(catalogRect);
            DrawSelectedGenes(selectedRect);
        }

        private void DrawGeneCatalog(Rect rect)
        {
            RimWorldUiWidgets.DrawPanel(rect);

            Rect innerRect = rect.ContractedBy(SectionPadding);

            var headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width, SectionTitleHeight);

            DrawGeneCatalogHeader(headerRect);

            var searchRect = new Rect(innerRect.x, headerRect.yMax, innerRect.width, SearchRowHeight);

            DrawSearchField(searchRect);

            var listRect = new Rect(innerRect.x, searchRect.yMax, innerRect.width, innerRect.yMax - searchRect.yMax);

            List<GeneCatalogRow> catalogRows = _catalogRows;

            if (catalogRows.Count == 0)
            {
                DrawNoSearchResults(listRect);
                return;
            }

            FixedHeightScrollListLayout layout = RimWorldUiWidgets.BeginFixedHeightScrollView(
                listRect,
                ref _catalogScrollPosition,
                catalogRows.Count,
                CatalogRowHeight,
                out float viewWidth);

            for (int index = layout.FirstVisibleIndex; index < layout.LastVisibleIndexExclusive; index++)
            {
                GeneCatalogRow row = catalogRows[index];
                var rowRect = new Rect(0f, index * CatalogRowHeight, viewWidth, CatalogRowHeight);

                if (row.Kind == GeneCatalogRowKind.Category)
                {
                    DrawGeneCategoryRow(rowRect, row.Category, row.IsCategoryExpanded, index);
                }
                else
                {
                    DrawCatalogGeneRow(rowRect, row.Gene, index);
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawGeneCatalogHeader(Rect rect)
        {
            Rect addAllRect = GetSectionHeaderActionRect(rect, 2);
            Rect expandAllRect = GetSectionHeaderActionRect(rect, 1);
            Rect collapseAllRect = GetSectionHeaderActionRect(rect, 0);

            var titleRect = new Rect(
                rect.x,
                rect.y,
                Mathf.Max(0f, addAllRect.x - rect.x - SectionHeaderContentGap),
                rect.height);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;

            Widgets.Label(titleRect, "XenogermPlanner.PlanEditor.GeneCatalog".Translate());

            if (RimWorldUiWidgets.DrawIconButton(
                    addAllRect,
                    _addAllFilteredIcon.Texture,
                    RimWorldUiStyle.Colors.Positive,
                    CanAddAllFilteredGenes(),
                    "XenogermPlanner.PlanEditor.AddAllFiltered".Translate().ToString()))
            {
                AddAllFilteredGenes();
            }

            if (RimWorldUiWidgets.DrawIconButton(
                    expandAllRect,
                    _expandAllCategoriesIcon.Texture,
                    RimWorldUiStyle.Colors.MutedText,
                    CanExpandAllCategories(),
                    "XenogermPlanner.PlanEditor.ExpandAllCategories".Translate().ToString()))
            {
                ExpandAllCategories();
            }

            if (RimWorldUiWidgets.DrawIconButton(
                    collapseAllRect,
                    _collapseAllCategoriesIcon.Texture,
                    RimWorldUiStyle.Colors.MutedText,
                    CanCollapseAllCategories(),
                    "XenogermPlanner.PlanEditor.CollapseAllCategories".Translate().ToString()))
            {
                CollapseAllCategories();
            }
        }

        private static Rect GetSectionHeaderActionRect(Rect rect, int indexFromRight)
        {
            float actionSize = RimWorldUiStyle.Metrics.IconButtonSize;
            float x = rect.xMax - actionSize - indexFromRight * (actionSize + RimWorldUiStyle.Metrics.IconButtonGap);

            return new Rect(x, rect.y + (rect.height - actionSize) * 0.5f, actionSize, actionSize);
        }

        private void DrawSearchField(Rect rect)
        {
            if (!RimWorldUiWidgets.DrawLabeledSearchField(
                    rect,
                    "XenogermPlanner.PlanEditor.Search".Translate().ToString(),
                    ref _searchText))
            {
                return;
            }

            RefreshFilteredGenes();
            _catalogScrollPosition = Vector2.zero;
        }

        private void DrawGeneCategoryRow(Rect rect, GeneCategoryDef category, bool expanded, int rowIndex)
        {
            if (!XenogermPlannerWidgets.DrawCollapsibleSectionRow(
                    rect,
                    category.LabelCap.ToString(),
                    expanded,
                    rowIndex,
                    enabled: !IsSearchActive))
            {
                return;
            }

            _collapsedCategories[category] = expanded;
            RefreshCatalogProjection();
        }

        private void DrawCatalogGeneRow(Rect rect, GeneDef gene, int rowIndex)
        {
            bool selected = _selectedGenes.Contains(gene);

            RimWorldUiWidgets.DrawSelectableRowBackground(rect, rowIndex, selected, Mouse.IsOver(rect));

            Rect contentRect = rect.ContractedBy(4f);

            Text.Font = GameFont.Small;

            XenogermPlannerWidgets.DrawGeneLabel(contentRect, gene);

            XenogermPlannerWidgets.AddGeneTooltip(
                rect,
                gene,
                selected ? "ClickToRemove".Translate().ToString() : "ClickToAdd".Translate().ToString());

            if (XenogermPlannerNativeInspector.TryOpenContextMenu(rect, gene))
                return;

            if (!Widgets.ButtonInvisible(rect))
                return;

            if (!_selectedGenes.Add(gene))
                _selectedGenes.Remove(gene);

            RefreshTargetAnalysis();
        }

        private void DrawSelectedGenes(Rect rect)
        {
            RimWorldUiWidgets.DrawPanel(rect);

            Rect innerRect = rect.ContractedBy(SectionPadding);

            var headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width, SectionTitleHeight);

            DrawSelectedGenesHeader(headerRect);

            var listRect = new Rect(
                innerRect.x,
                headerRect.yMax,
                innerRect.width,
                innerRect.height - SectionTitleHeight);

            if (_selectedGenes.Count == 0)
            {
                DrawNoSelectedGenes(listRect);
                return;
            }

            GeneDef geneToRemove = null;

            FixedHeightScrollListLayout layout = RimWorldUiWidgets.BeginFixedHeightScrollView(
                listRect,
                ref _selectedGenesScrollPosition,
                _sortedSelectedGenes.Count,
                GeneRowHeight,
                out float viewWidth);

            try
            {
                for (int index = layout.FirstVisibleIndex; index < layout.LastVisibleIndexExclusive; index++)
                {
                    GeneDef gene = _sortedSelectedGenes[index];

                    var rowRect = new Rect(0f, index * GeneRowHeight, viewWidth, GeneRowHeight);

                    if (DrawSelectedGeneRow(rowRect, gene, index))
                    {
                        geneToRemove = gene;
                        break;
                    }
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }

            if (geneToRemove != null && _selectedGenes.Remove(geneToRemove))
                RefreshTargetAnalysis();
        }

        private void DrawSelectedGenesHeader(Rect rect)
        {
            Rect clearRect = GetSectionHeaderActionRect(rect, 0);
            var countLabel = "XenogermPlanner.PlanEditor.SelectedGeneCount".Translate(_selectedGenes.Count).ToString();

            Text.Font = GameFont.Small;

            float availableCountWidth = Mathf.Max(0f, clearRect.x - rect.x - SelectedHeaderGap);
            float countWidth = Mathf.Min(availableCountWidth, Text.CalcSize(countLabel).x);

            var countRect = new Rect(clearRect.x - SelectedHeaderGap - countWidth, rect.y, countWidth, rect.height);

            var titleRect = new Rect(
                rect.x,
                rect.y,
                Mathf.Max(0f, countRect.x - rect.x - SelectedHeaderGap),
                rect.height);

            bool previousWordWrap = Text.WordWrap;

            using (ImGuiStateScope.Capture())
            {
                try
                {
                    Text.WordWrap = false;
                    Text.Font = GameFont.Medium;
                    Text.Anchor = TextAnchor.MiddleLeft;

                    Widgets.Label(titleRect, "XenogermPlanner.PlanEditor.SelectedGenes".Translate());

                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleRight;

                    Widgets.Label(countRect, countLabel);

                    if (RimWorldUiWidgets.DrawIconButton(
                            clearRect,
                            _clearSelectedGenesIcon.Texture,
                            RimWorldUiStyle.Colors.Negative,
                            CanClearSelectedGenes(),
                            "XenogermPlanner.PlanEditor.ClearSelectedGenes".Translate().ToString()))
                    {
                        ClearSelectedGenes();
                    }
                }
                finally
                {
                    Text.WordWrap = previousWordWrap;
                }
            }
        }

        private static void DrawNoSelectedGenes(Rect rect)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            Widgets.Label(rect, "XenogermPlanner.PlanEditor.NoGenes".Translate());
        }

        private static void DrawNoSearchResults(Rect rect)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            Widgets.Label(rect, "XenogermPlanner.PlanEditor.NoSearchResults".Translate());
        }

        private bool DrawSelectedGeneRow(Rect rect, GeneDef gene, int rowIndex)
        {
            RimWorldUiWidgets.DrawSelectableRowBackground(
                rect,
                rowIndex,
                selected: false,
                hovered: Mouse.IsOver(rect),
                drawAccent: false);

            Rect contentRect = rect.ContractedBy(4f);

            Text.Font = GameFont.Small;

            XenogermPlannerWidgets.DrawGeneLabel(contentRect, gene);

            XenogermPlannerWidgets.AddGeneTooltip(rect, gene, "ClickToRemove".Translate().ToString());

            if (XenogermPlannerNativeInspector.TryOpenContextMenu(rect, gene))
                return false;

            return Widgets.ButtonInvisible(rect);
        }

        private void DrawFooter(Rect rect)
        {
            var saveButtonRect = new Rect(
                rect.xMax - FooterButtonWidth,
                rect.y + (rect.height - FooterButtonHeight) * 0.5f,
                FooterButtonWidth,
                FooterButtonHeight);

            var cancelButtonRect = new Rect(
                saveButtonRect.x - FooterButtonGap - FooterButtonWidth,
                saveButtonRect.y,
                FooterButtonWidth,
                FooterButtonHeight);

            if (Widgets.ButtonText(cancelButtonRect, "XenogermPlanner.PlanEditor.Cancel".Translate()))
            {
                Close();
            }

            if (Widgets.ButtonText(
                    saveButtonRect,
                    IsEditMode
                        ? "XenogermPlanner.PlanEditor.Save".Translate()
                        : "XenogermPlanner.PlanEditor.Create".Translate()))
            {
                RequestSave();
            }
        }

        private void RequestSave()
        {
            string excludedPlanId = IsEditMode ? _existingPlan.Id : null;

            if (!_component.TryValidatePlanName(
                    _planName,
                    excludedPlanId,
                    out string normalizedPlanName,
                    out XenogermPlanNameValidationFailure failure))
            {
                ShowNameValidationFailure(failure, normalizedPlanName);
                return;
            }

            if (!IsEditMode)
            {
                CreatePlan(normalizedPlanName);
                return;
            }

            bool desiredGenesChanged = !_selectedGenes.SetEquals(_existingPlan.DesiredGenes);

            if (_existingPlan.IsDegraded && desiredGenesChanged)
            {
                var message = "XenogermPlanner.PlanEditor.DegradedTargetChangeConfirmation".Translate().ToString();

                Find.WindowStack.Add(
                    Dialog_MessageBox.CreateConfirmation(message, () => ApplyChanges(normalizedPlanName), true));

                return;
            }

            ApplyChanges(normalizedPlanName);
        }

        private void CreatePlan(string normalizedPlanName)
        {
            var plan = new XenogermPlan(
                normalizedPlanName,
                _selectedGenes,
                _readinessMode,
                _readinessNotificationsEnabled);

            _component.AddPlan(plan);

            CompleteSave(plan);
        }

        private void ApplyChanges(string normalizedPlanName)
        {
            bool desiredGenesChanged = !_selectedGenes.SetEquals(_existingPlan.DesiredGenes);

            if (!string.Equals(_existingPlan.Name, normalizedPlanName, StringComparison.Ordinal))
                _existingPlan.Rename(normalizedPlanName);

            if (desiredGenesChanged)
                _existingPlan.ReplaceDesiredGenes(_selectedGenes);

            if (_existingPlan.ReadinessMode != _readinessMode)
                _existingPlan.ChangeReadinessMode(_readinessMode);

            if (_existingPlan.ReadinessNotificationsEnabled != _readinessNotificationsEnabled)
            {
                _existingPlan.ChangeReadinessNotificationsEnabled(_readinessNotificationsEnabled);
            }

            CompleteSave(_existingPlan);
        }

        private static void ShowNameValidationFailure(
            XenogermPlanNameValidationFailure failure,
            string normalizedPlanName)
        {
            string message;

            switch (failure)
            {
                case XenogermPlanNameValidationFailure.InvalidName:
                    message = "XenogermPlanner.PlanEditor.InvalidName".Translate().ToString();
                    break;

                case XenogermPlanNameValidationFailure.NameConflict:
                    message = "XenogermPlanner.PlanEditor.NameConflict".Translate(normalizedPlanName).ToString();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(failure),
                        failure,
                        "Unsupported name validation failure.");
            }

            Messages.Message(message, MessageTypeDefOf.RejectInput, false);
        }

        private void CompleteSave(XenogermPlan plan)
        {
            _onSaved?.Invoke(plan);
            Close();
        }

        private void RefreshTargetAnalysis()
        {
            _sortedSelectedGenes = XenogermPlannerPresentation.GetSortedGenes(_selectedGenes);
            _targetAnalysis = PlanGeneTargetAnalyzer.Analyze(_selectedGenes);
            _selectedGeneBiostats = PlanGeneBiostatCalculator.CalculateEffective(_selectedGenes);
            _selectedGeneBiostatsPartial = IsEditMode && _existingPlan.IsDegraded &&
                                           _selectedGenes.SetEquals(_existingPlan.DesiredGenes);
            _presentationLanguageKey = LanguageDatabase.activeLanguage;
            RefreshDiagnosticsProjection();
        }

        private void RefreshDiagnosticsProjection()
        {
            _targetDiagnosticsProjection = GeneTargetDiagnosticsProjection.Build(_targetAnalysis, _readinessMode);
            _geneDiagnosticsLayoutCache.Invalidate();

            if (!_targetDiagnosticsProjection.HasDiagnostics)
                _geneDiagnosticsScrollPosition = Vector2.zero;
        }

        private void RefreshLanguageDependentPresentationIfNeeded()
        {
            object languageKey = LanguageDatabase.activeLanguage;

            if (Equals(_presentationLanguageKey, languageKey))
                return;

            _presentationLanguageKey = languageKey;
            _sortedSelectedGenes = XenogermPlannerPresentation.GetSortedGenes(_selectedGenes);
            RefreshDiagnosticsProjection();
        }

        private void InitializeCollapsedCategories()
        {
            foreach (GeneDef gene in _catalogGenes)
            {
                GeneCategoryDef category = gene.displayCategory;

                if (!_collapsedCategories.ContainsKey(category))
                    _collapsedCategories.Add(category, false);
            }
        }

        private void RefreshFilteredGenes()
        {
            if (!IsSearchActive)
            {
                _filteredGenes = new List<GeneDef>(_catalogGenes);
                RefreshCatalogProjection();
                return;
            }

            string query = _searchText.Trim();

            var matchingCategories = new HashSet<GeneCategoryDef>();

            foreach (GeneCategoryDef category in _collapsedCategories.Keys)
            {
                if (CategoryMatchesSearch(category, query))
                {
                    matchingCategories.Add(category);
                }
            }

            var matchingGenes = new List<GeneDef>();

            foreach (GeneDef gene in _catalogGenes)
            {
                if (matchingCategories.Contains(gene.displayCategory) || GeneMatchesSearch(gene, query))
                {
                    matchingGenes.Add(gene);
                }
            }

            _filteredGenes = matchingGenes;
            RefreshCatalogProjection();
        }

        private void RefreshCatalogProjection()
        {
            _catalogRows = GeneCatalogProjection.Build(
                _filteredGenes,
                _collapsedCategories,
                forceExpanded: IsSearchActive);
        }

        private static bool GeneMatchesSearch(GeneDef gene, string query)
        {
            string displayName = XenogermPlannerPresentation.GetGeneDisplayName(gene);

            if (displayName.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0)
            {
                return true;
            }

            string defName = gene.defName ?? string.Empty;

            return defName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool CategoryMatchesSearch(GeneCategoryDef category, string query)
        {
            var displayName = category.LabelCap.ToString();

            return displayName.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private static float CalculateDegradedWarningHeight(float width)
        {
            var warning = "XenogermPlanner.PlanEditor.DegradedTargetWarning".Translate().ToString();

            return GetTextHeight(warning, width, GameFont.Small) + WarningPadding;
        }

        private static float GetTextHeight(string text, float width, GameFont font)
        {
            using (ImGuiStateScope.Capture())
            {
                Text.Font = font;
                return Text.CalcHeight(text, width);
            }
        }
    }
}