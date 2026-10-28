using System;

namespace XenogermPlanner.Templates
{
    internal sealed class PlanXenogermTemplateSaveResult
    {
        private PlanXenogermTemplateSaveResult(PlanXenogermTemplateSaveFailure failure, string vanillaRejectionReason)
        {
            if (failure == PlanXenogermTemplateSaveFailure.None && !string.IsNullOrEmpty(vanillaRejectionReason))
            {
                throw new ArgumentException(
                    "Successful template save result cannot contain a rejection reason.",
                    nameof(vanillaRejectionReason));
            }

            if (failure != PlanXenogermTemplateSaveFailure.VanillaRejected &&
                !string.IsNullOrEmpty(vanillaRejectionReason))
            {
                throw new ArgumentException(
                    "Only a vanilla rejection can contain a vanilla rejection reason.",
                    nameof(vanillaRejectionReason));
            }

            Failure = failure;
            VanillaRejectionReason = vanillaRejectionReason;
        }

        internal bool Succeeded => Failure == PlanXenogermTemplateSaveFailure.None;
        internal PlanXenogermTemplateSaveFailure Failure { get; }
        internal string VanillaRejectionReason { get; }

        internal static PlanXenogermTemplateSaveResult Success()
        {
            return new PlanXenogermTemplateSaveResult(PlanXenogermTemplateSaveFailure.None, null);
        }

        internal static PlanXenogermTemplateSaveResult Failed(PlanXenogermTemplateSaveFailure failure)
        {
            if (failure == PlanXenogermTemplateSaveFailure.None ||
                failure == PlanXenogermTemplateSaveFailure.VanillaRejected)
            {
                throw new ArgumentOutOfRangeException(nameof(failure), failure, "Unsupported local failure reason.");
            }

            return new PlanXenogermTemplateSaveResult(failure, null);
        }

        internal static PlanXenogermTemplateSaveResult VanillaRejected(string reason)
        {
            return new PlanXenogermTemplateSaveResult(
                PlanXenogermTemplateSaveFailure.VanillaRejected,
                reason ?? string.Empty);
        }
    }
}