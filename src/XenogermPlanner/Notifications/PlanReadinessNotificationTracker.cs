using System;
using XenogermPlanner.Analysis;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Notifications
{
    internal static class PlanReadinessNotificationTracker
    {
        internal static bool Update(XenogermPlan plan, PlanReadinessResult readinessResult)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (readinessResult == null)
                throw new ArgumentNullException(nameof(readinessResult));

            if (readinessResult.Status == PlanReadinessStatus.Unavailable)
                return false;

            bool isReady = readinessResult.Status == PlanReadinessStatus.Ready;

            if (!plan.HasReadinessNotificationBaseline)
            {
                plan.UpdateReadinessNotificationState(isReady);
                return false;
            }

            bool shouldAnnounce = plan.ReadinessNotificationsEnabled &&
                                  !plan.LastReadinessNotificationStateWasReady &&
                                  isReady;

            plan.UpdateReadinessNotificationState(isReady);

            return shouldAnnounce;
        }
    }
}