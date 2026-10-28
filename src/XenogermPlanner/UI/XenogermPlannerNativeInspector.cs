using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace XenogermPlanner.UI
{
    internal static class XenogermPlannerNativeInspector
    {
        internal static bool TryOpenContextMenu(Rect rect, GeneDef gene)
        {
            if (gene == null)
                throw new ArgumentNullException(nameof(gene));

            return TryOpenContextMenu(rect, () => OpenInfoCard(gene));
        }

        internal static bool TryOpenContextMenu(Rect rect, Genepack genepack)
        {
            if (!CanInspect(genepack))
                return false;

            return TryOpenContextMenu(
                rect,
                () =>
                {
                    if (CanInspect(genepack))
                    {
                        OpenInfoCard(genepack);
                    }
                });
        }

        private static bool TryOpenContextMenu(Rect rect, Action openInfoCard)
        {
            Event currentEvent = Event.current;

            if (currentEvent == null ||
                currentEvent.type != EventType.MouseDown ||
                currentEvent.button != 1 ||
                !Mouse.IsOver(rect))
            {
                return false;
            }

            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption("XenogermPlanner.Planner.OpenInfoCard".Translate().ToString(), openInfoCard)
            };

            Find.WindowStack.Add(new FloatMenu(options));

            return true;
        }

        private static bool CanInspect(Genepack genepack)
        {
            return genepack is { Destroyed: false };
        }

        private static void OpenInfoCard(GeneDef gene)
        {
            Find.WindowStack.Add(new Dialog_InfoCard(gene));
        }

        private static void OpenInfoCard(Genepack genepack)
        {
            Find.WindowStack.Add(new Dialog_InfoCard(genepack));
        }
    }
}