using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace XenogermPlanner.UI
{
    internal static class XenogermPlannerTargetInteraction
    {
        internal static void Highlight(Thing target)
        {
            if (!TryGetCurrentMapTarget(target, out GlobalTargetInfo adjustedTarget))
                return;

            TargetHighlighter.Highlight(adjustedTarget);
        }

        internal static void Highlight(IEnumerable<Thing> targets)
        {
            if (targets == null)
                throw new ArgumentNullException(nameof(targets));

            foreach (Thing target in targets)
                Highlight(target);
        }

        internal static bool CanNavigate(Thing target)
        {
            return TryGetCurrentMapTarget(target, out GlobalTargetInfo adjustedTarget) &&
                   CameraJumper.CanJump(adjustedTarget);
        }

        internal static bool TryNavigate(Thing target)
        {
            if (!TryGetCurrentMapTarget(target, out GlobalTargetInfo adjustedTarget) ||
                !CameraJumper.CanJump(adjustedTarget))
            {
                return false;
            }

            CameraJumper.TryJump(adjustedTarget);

            return true;
        }

        private static bool TryGetCurrentMapTarget(Thing target, out GlobalTargetInfo adjustedTarget)
        {
            adjustedTarget = GlobalTargetInfo.Invalid;

            if (target == null || target.Destroyed)
                return false;

            Map currentMap = Find.CurrentMap;

            if (currentMap == null)
                return false;

            adjustedTarget = CameraJumper.GetAdjustedTarget(target);

            return adjustedTarget is { IsValid: true, IsMapTarget: true } &&
                   ReferenceEquals(adjustedTarget.Map, currentMap);
        }
    }
}