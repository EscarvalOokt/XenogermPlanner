using System;
using Escarval.RimWorld.UI;
using RimWorld;
using UnityEngine;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;
using XenogermPlanner.Templates;

namespace XenogermPlanner.UI
{
    internal sealed class XenogermTemplateGenerationDialog : Window
    {
        private const float DialogWidth = 440f;
        private const float DialogHeight = 150f;
        private const float PanelPadding = RimWorldUiStyle.Metrics.PanelPadding;
        private const float TitleHeight = RimWorldUiStyle.Metrics.HeaderHeight;
        private const float StatusGap = RimWorldUiStyle.Metrics.SectionGap;
        private readonly PlanGenepackInventoryGameComponent _inventoryComponent;

        private readonly XenogermPlan _plan;
        private bool _generationStarted;

        private bool _hasRenderedFrame;
        private int _renderedFrame;

        internal XenogermTemplateGenerationDialog(
            XenogermPlan plan,
            PlanGenepackInventoryGameComponent inventoryComponent)
        {
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
            _inventoryComponent = inventoryComponent ?? throw new ArgumentNullException(nameof(inventoryComponent));

            doCloseX = false;
            closeOnAccept = false;
            closeOnCancel = false;
            closeOnClickedOutside = false;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(DialogWidth, DialogHeight);

        public override void DoWindowContents(Rect inRect)
        {
            using (ImGuiStateScope.Capture())
            {
                DrawProgress(inRect);
                TryStartGenerationAfterRenderedFrame();
            }
        }

        private static void DrawProgress(Rect rect)
        {
            RimWorldUiWidgets.DrawPanel(rect);
            Rect contentRect = rect.ContractedBy(PanelPadding);
            var titleRect = new Rect(contentRect.x, contentRect.y, contentRect.width, TitleHeight);
            var statusRect = new Rect(
                contentRect.x,
                titleRect.yMax + StatusGap,
                contentRect.width,
                Mathf.Max(0f, contentRect.yMax - titleRect.yMax - StatusGap));

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = RimWorldUiStyle.Colors.Accent;
            Widgets.Label(titleRect, "XenogermPlanner.Template.Generation.Title".Translate());

            int dotCount = 1 + Mathf.FloorToInt(Time.realtimeSinceStartup * 2f) % 3;
            string status = "XenogermPlanner.Template.Generation.InProgress".Translate().ToString() +
                            new string('.', dotCount);

            Text.Font = GameFont.Small;
            GUI.color = RimWorldUiStyle.Colors.PrimaryText;
            Widgets.Label(statusRect, status);
        }

        private void TryStartGenerationAfterRenderedFrame()
        {
            if (_generationStarted)
                return;

            if (!_hasRenderedFrame)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    _hasRenderedFrame = true;
                    _renderedFrame = Time.frameCount;
                }

                return;
            }

            if (Time.frameCount <= _renderedFrame)
                return;

            _generationStarted = true;
            GenerateTemplateCandidates();
        }

        private void GenerateTemplateCandidates()
        {
            _inventoryComponent.Invalidate();
            PlanGenepackInventorySnapshot inventorySnapshot = _inventoryComponent.Snapshot;
            PlanReadinessResult readinessResult = PlanReadinessAnalyzer.Analyze(_plan, inventorySnapshot);

            if (!readinessResult.IsReady)
            {
                string disabledTranslationKey =
                    XenogermPlannerPresentation.GetTemplateCreationDisabledTranslationKey(readinessResult);

                Close(doCloseSound: false);
                Messages.Message(disabledTranslationKey.Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            PlanXenogermTemplateCandidateSearchResult searchResult =
                PlanXenogermTemplateCandidateSearcher.Search(_plan, readinessResult, inventorySnapshot);

            if (!searchResult.HasCandidate)
            {
                Close(doCloseSound: false);
                Messages.Message(
                    "XenogermPlanner.Template.NoCandidate".Translate(),
                    MessageTypeDefOf.RejectInput,
                    historical: false);
                return;
            }

            Close(doCloseSound: false);
            Find.WindowStack.Add(new XenogermTemplateCreationDialog(_plan, _inventoryComponent, searchResult));
        }
    }
}