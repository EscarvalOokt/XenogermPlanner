using System;
using Escarval.RimWorld.UI;
using UnityEngine;
using Verse;
using XenogermPlanner.Donors;

namespace XenogermPlanner.UI
{
    public sealed class PotentialDonorDetailsDialog : Window
    {
        private const float DialogWidth = 560f;
        private const float DialogHeight = 540f;
        private const float TitleHeight = 34f;
        private const float TitleGeneIconSize = 30f;
        private const float TitleGeneIconGap = 8f;
        private const float ContentGap = RimWorldUiStyle.Metrics.SectionGap;
        private const float SectionPadding = RimWorldUiStyle.Metrics.PanelPadding;
        private const float DonorRowHeight = 40f;
        private const float DonorTargetSize = 32f;
        private const float DonorTargetGap = RimWorldUiStyle.Metrics.ControlGap;
        private const float InitialScreenMargin = 35f;

        private readonly PotentialDonorPresentationProjectionCache _donorProjectionCache =
            new PotentialDonorPresentationProjectionCache();

        private readonly GeneDef _gene;

        private Vector2 _scrollPosition;

        public PotentialDonorDetailsDialog(GeneDef gene)
        {
            _gene = gene ?? throw new ArgumentNullException(nameof(gene));

            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            forcePause = true;
            draggable = true;
        }

        public override Vector2 InitialSize => new Vector2(DialogWidth, DialogHeight);

        protected override void SetInitialSizeAndPosition()
        {
            Vector2 initialSize = InitialSize;
            float maximumX = Mathf.Max(0f, Verse.UI.screenWidth - initialSize.x);
            float maximumY = Mathf.Max(0f, Verse.UI.screenHeight - initialSize.y);
            float x = Mathf.Clamp(maximumX - InitialScreenMargin, 0f, maximumX);
            float y = Mathf.Clamp(maximumY * 0.5f, 0f, maximumY);

            windowRect = new Rect(x, y, initialSize.x, initialSize.y);
        }

        public override void DoWindowContents(Rect inRect)
        {
            using (ImGuiStateScope.Capture())
            {
                Rect contentRect = inRect;
                var titleRect = new Rect(contentRect.x, contentRect.y, contentRect.width, TitleHeight);
                DrawTitle(titleRect);

                var description = "XenogermPlanner.PotentialDonors.Description".Translate().ToString();
                float descriptionHeight = GetTextHeight(description, contentRect.width, GameFont.Small);

                var descriptionRect = new Rect(contentRect.x, titleRect.yMax, contentRect.width, descriptionHeight);

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = RimWorldUiStyle.Colors.MutedText;

                Widgets.Label(descriptionRect, description);


                var listRect = new Rect(
                    contentRect.x,
                    descriptionRect.yMax + ContentGap,
                    contentRect.width,
                    Mathf.Max(0f, contentRect.yMax - descriptionRect.yMax - ContentGap));

                DrawDonorList(listRect);
            }
        }

        private void DrawTitle(Rect rect)
        {
            var title = "XenogermPlanner.PotentialDonors.Title".Translate(
                    XenogermPlannerPresentation.GetGeneDisplayName(_gene))
                .ToString();

            XenogermPlannerWidgets.DrawGeneLabel(
                rect,
                _gene,
                title,
                GameFont.Medium,
                TitleGeneIconSize,
                TitleGeneIconGap);
        }

        private void DrawDonorList(Rect rect)
        {
            RimWorldUiWidgets.DrawPanel(rect);

            Rect innerRect = rect.ContractedBy(SectionPadding);
            PlanPotentialDonorAnalysisResult analysis = PlanPotentialDonorAnalyzer.Analyze(
                [_gene],
                PlanPotentialDonorScopeScanner.Scan(Find.CurrentMap));

            if (!analysis.IsAvailable)
            {
                DrawCenteredMessage(innerRect, "XenogermPlanner.PotentialDonors.Unavailable".Translate().ToString());
                return;
            }

            if (!analysis.TryGetDiagnostic(_gene, out PlanPotentialDonorGeneDiagnostic diagnostic) ||
                !diagnostic.HasDonors)
            {
                DrawCenteredMessage(innerRect, "XenogermPlanner.PotentialDonors.Empty".Translate().ToString());
                return;
            }

            PotentialDonorPresentationProjection projection = _donorProjectionCache.GetOrBuild(
                diagnostic.Donors,
                LanguageDatabase.activeLanguage);
            Pawn hoveredDonor = null;

            FixedHeightScrollListLayout layout = RimWorldUiWidgets.BeginFixedHeightScrollView(
                innerRect,
                ref _scrollPosition,
                projection.Rows.Count,
                DonorRowHeight,
                out float viewWidth);

            for (int index = layout.FirstVisibleIndex; index < layout.LastVisibleIndexExclusive; index++)
            {
                var rowRect = new Rect(0f, index * DonorRowHeight, viewWidth, DonorRowHeight);

                PotentialDonorPresentationRow row = projection.Rows[index];

                if (DrawDonorRow(rowRect, row, index))
                    hoveredDonor = row.Donor;
            }

            Widgets.EndScrollView();

            if (hoveredDonor != null)
                XenogermPlannerTargetInteraction.Highlight(hoveredDonor);
        }

        private static bool DrawDonorRow(Rect rect, PotentialDonorPresentationRow row, int rowIndex)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            Pawn donor = row.Donor;

            RimWorldUiWidgets.DrawSelectableRowBackground(
                rect,
                rowIndex,
                selected: false,
                hovered: Mouse.IsOver(rect),
                drawAccent: false);

            Rect contentRect = rect.ContractedBy(RimWorldUiStyle.Metrics.SmallGap);
            var targetRect = new Rect(contentRect.x, contentRect.y, DonorTargetSize, contentRect.height);
            var labelRect = new Rect(
                targetRect.xMax + DonorTargetGap,
                contentRect.y,
                Mathf.Max(0f, contentRect.xMax - targetRect.xMax - DonorTargetGap),
                contentRect.height);

            bool isHovered = XenogermPlannerWidgets.DrawThingTargetRow(rect, targetRect, donor);

            RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                labelRect,
                row.DisplayName,
                GameFont.Small,
                TextAnchor.MiddleLeft);

            return isHovered;
        }

        private static void DrawCenteredMessage(Rect rect, string message)
        {
            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = RimWorldUiStyle.Colors.MutedText;

                Widgets.Label(rect, message);
            }
        }

        private static float GetTextHeight(string text, float width, GameFont font)
        {
            using (ImGuiStateScope.Capture())
            {
                Text.Font = font;
                return Text.CalcHeight(text ?? string.Empty, width);
            }
        }
    }
}