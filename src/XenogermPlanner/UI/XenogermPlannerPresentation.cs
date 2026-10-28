using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Assemblers;
using XenogermPlanner.Donors;
using XenogermPlanner.Plans;
using XenogermPlanner.Templates;
using XenogermPlanner.Trade;

namespace XenogermPlanner.UI
{
    internal static class XenogermPlannerPresentation
    {
        internal static string GetPlanDisplayName(XenogermPlan plan)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (string.IsNullOrEmpty(plan.Name))
            {
                return "XenogermPlanner.Planner.UnnamedPlan".Translate().ToString();
            }

            return plan.Name;
        }

        internal static List<XenogermPlan> GetFilteredPlans(IEnumerable<XenogermPlan> plans, string searchText)
        {
            if (plans == null)
                throw new ArgumentNullException(nameof(plans));

            string query = (searchText ?? string.Empty).Trim();
            var filteredPlans = new List<XenogermPlan>();

            foreach (XenogermPlan plan in plans)
            {
                if (plan == null)
                    continue;

                if (query.Length == 0 ||
                    GetPlanDisplayName(plan).IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    filteredPlans.Add(plan);
                }
            }

            return filteredPlans;
        }

        internal static string GetReadinessReadyNotificationMessage(XenogermPlan plan)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            return GetReadinessReadyNotificationTranslationKey().Translate(GetPlanDisplayName(plan)).ToString();
        }

        internal static string GetReadinessReadyNotificationTranslationKey()
        {
            return "XenogermPlanner.Notifications.PlanReady";
        }

        internal static string GetTraderAdvisoryNotificationMessage(
            PlanTraderAdvisoryNotification notification,
            string nativeTraderName)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            string traderName = GetTraderAdvisoryDisplayName(notification.Source.Kind, nativeTraderName);
            List<XenogermPlan> affectedPlans = GetTraderAdvisoryAffectedPlans(notification);
            var planNames = new List<string>(affectedPlans.Count);

            foreach (XenogermPlan plan in affectedPlans)
                planNames.Add(GetPlanDisplayName(plan));

            return GetTraderAdvisoryNotificationTranslationKey()
                .Translate(traderName, notification.OfferCount, string.Join(", ", planNames))
                .ToString();
        }

        internal static string GetTraderAdvisoryNotificationTranslationKey()
        {
            return "XenogermPlanner.Notifications.TraderRelevantOffers";
        }

        internal static string GetTraderAdvisoryDisplayName(PlanTraderAdvisorySourceKind kind, string nativeTraderName)
        {
            if (!string.IsNullOrWhiteSpace(nativeTraderName))
                return nativeTraderName;

            return GetTraderAdvisorySourceFallbackTranslationKey(kind).Translate().ToString();
        }

        internal static string GetTraderAdvisorySourceFallbackTranslationKey(PlanTraderAdvisorySourceKind kind)
        {
            switch (kind)
            {
                case PlanTraderAdvisorySourceKind.Orbital:
                    return "XenogermPlanner.Notifications.OrbitalTrader";

                case PlanTraderAdvisorySourceKind.Caravan:
                    return "XenogermPlanner.Notifications.VisitingTrader";

                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown trader advisory source kind.");
            }
        }

        internal static List<XenogermPlan> GetTraderAdvisoryAffectedPlans(PlanTraderAdvisoryNotification notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            var plansById = new Dictionary<string, XenogermPlan>(StringComparer.Ordinal);

            foreach (PlanTraderAdvisoryNotificationOffer offer in notification.Offers)
            {
                foreach (XenogermPlan plan in offer.MatchingPlans)
                {
                    plansById.TryAdd(plan.Id, plan);
                }
            }

            var affectedPlans = new List<XenogermPlan>(plansById.Values);
            affectedPlans.Sort(ComparePlansForTraderAdvisory);
            return affectedPlans;
        }

        internal static string GetAssemblerDisplayName(Building_GeneAssembler assembler)
        {
            if (assembler == null)
                throw new ArgumentNullException(nameof(assembler));

            string label = assembler.LabelCap;

            if (string.IsNullOrWhiteSpace(label))
            {
                label = assembler.def?.defName ?? string.Empty;
            }

            IntVec3 position = assembler.Position;

            return "XenogermPlanner.Planner.AssemblerDisplayName".Translate(label, position.x, position.z).ToString();
        }

        internal static int GetDesiredGeneCount(XenogermPlan plan)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            return plan.DesiredGenes.Count + plan.UnresolvedDesiredGeneDefNames.Count;
        }

        internal static string GetReadinessModeLabel(PlanReadinessMode readinessMode)
        {
            switch (readinessMode)
            {
                case PlanReadinessMode.Coverage:
                    return "XenogermPlanner.ReadinessMode.Coverage".Translate().ToString();

                case PlanReadinessMode.ExactPayload:
                    return "XenogermPlanner.ReadinessMode.ExactPayload".Translate().ToString();

                default:
                    return "XenogermPlanner.ReadinessMode.Unknown".Translate().ToString();
            }
        }

        internal static string GetReadinessModeDescription(PlanReadinessMode readinessMode)
        {
            switch (readinessMode)
            {
                case PlanReadinessMode.Coverage:
                    return "XenogermPlanner.ReadinessMode.CoverageDescription".Translate().ToString();

                case PlanReadinessMode.ExactPayload:
                    return "XenogermPlanner.ReadinessMode.ExactPayloadDescription".Translate().ToString();

                default:
                    return "XenogermPlanner.ReadinessMode.UnknownDescription".Translate().ToString();
            }
        }

        internal static string GetReadinessStatusLabel(PlanReadinessStatus status)
        {
            switch (status)
            {
                case PlanReadinessStatus.Ready:
                    return "XenogermPlanner.ReadinessStatus.Ready".Translate().ToString();

                case PlanReadinessStatus.NotReady:
                    return "XenogermPlanner.ReadinessStatus.NotReady".Translate().ToString();

                case PlanReadinessStatus.EmptyTarget:
                    return "XenogermPlanner.ReadinessStatus.EmptyTarget".Translate().ToString();

                case PlanReadinessStatus.Degraded:
                    return "XenogermPlanner.ReadinessStatus.Degraded".Translate().ToString();

                case PlanReadinessStatus.Unavailable:
                    return "XenogermPlanner.ReadinessStatus.Unavailable".Translate().ToString();

                default:
                    return "XenogermPlanner.ReadinessStatus.Unknown".Translate().ToString();
            }
        }

        internal static string GetTemplateCreationDisabledTranslationKey(PlanReadinessResult result)
        {
            if (result == null)
                return "XenogermPlanner.Template.Disabled.DataUnavailable";

            switch (result.Status)
            {
                case PlanReadinessStatus.Ready:
                    return null;

                case PlanReadinessStatus.Unavailable:
                    return "XenogermPlanner.Template.Disabled.NoActiveMap";

                case PlanReadinessStatus.EmptyTarget:
                    return "XenogermPlanner.Template.Disabled.EmptyTarget";

                case PlanReadinessStatus.Degraded:
                    return "XenogermPlanner.Template.Disabled.Degraded";

                case PlanReadinessStatus.NotReady:
                    return result.HasExactPayloadConflict
                        ? "XenogermPlanner.Template.Disabled.ExactPayloadConflict"
                        : "XenogermPlanner.Template.Disabled.NotReady";

                default:
                    return "XenogermPlanner.Template.Disabled.DataUnavailable";
            }
        }

        internal static string GetTemplateCreationTooltip(PlanReadinessResult result)
        {
            string disabledTranslationKey = GetTemplateCreationDisabledTranslationKey(result);

            return disabledTranslationKey == null
                ? "XenogermPlanner.Template.Create".Translate().ToString() +
                  "\n\n" +
                  "XenogermPlanner.Template.CreateDescription".Translate().ToString()
                : disabledTranslationKey.Translate().ToString();
        }

        internal static string GetTemplateCandidateSummaryTranslationKey()
        {
            return "XenogermPlanner.Template.CandidateSummary";
        }

        internal static string GetTemplateCandidateSummary(PlanXenogermTemplateCandidate candidate)
        {
            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));

            return GetTemplateCandidateSummaryTranslationKey()
                .Translate(candidate.GeneSetCount, candidate.AdditionalGenes.Count, candidate.TotalGeneOccurrences)
                .ToString();
        }

        internal static string GetTemplateCandidateLabel(int candidateIndex, bool automatic)
        {
            if (candidateIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(candidateIndex),
                    candidateIndex,
                    "Candidate index cannot be negative.");
            }

            string translationKey = GetTemplateCandidateLabelTranslationKey(automatic);

            return automatic
                ? translationKey.Translate().ToString()
                : translationKey.Translate(candidateIndex + 1).ToString();
        }

        internal static string GetTemplateCandidateLabelTranslationKey(bool automatic)
        {
            return automatic
                ? "XenogermPlanner.Template.AutomaticCandidate"
                : "XenogermPlanner.Template.AlternativeCandidate";
        }

        internal static string FormatMetabolism(int metabolism)
        {
            return metabolism > 0 ? "+" + metabolism : metabolism.ToString();
        }

        private static float GetHungerRateFactor(int metabolism)
        {
            return GeneTuning.MetabolismToFoodConsumptionFactorCurve.Evaluate(metabolism);
        }

        internal static string FormatHungerRateFactor(float hungerRateFactor)
        {
            return "x" + hungerRateFactor.ToStringPercent();
        }

        internal static string GetHungerRateSummary(int metabolism)
        {
            return "HungerRate".Translate().ToString() + " " + FormatHungerRateFactor(GetHungerRateFactor(metabolism));
        }

        internal static string GetTemplateSaveFailureTranslationKey(PlanXenogermTemplateSaveFailure failure)
        {
            switch (failure)
            {
                case PlanXenogermTemplateSaveFailure.InventoryUnavailable:
                    return "XenogermPlanner.Template.SaveFailure.InventoryUnavailable";

                case PlanXenogermTemplateSaveFailure.CandidateInvalidForPlan:
                    return "XenogermPlanner.Template.SaveFailure.CandidateInvalidForPlan";

                case PlanXenogermTemplateSaveFailure.CompositionUnavailable:
                    return "XenogermPlanner.Template.SaveFailure.CompositionUnavailable";

                case PlanXenogermTemplateSaveFailure.VanillaRejected:
                    return "XenogermPlanner.Template.SaveFailure.VanillaRejected";

                case PlanXenogermTemplateSaveFailure.None:
                default:
                    return "XenogermPlanner.Template.SaveFailure.Unknown";
            }
        }

        internal static string GetAssemblerReadinessStatusLabel(PlanAssemblerReadinessStatus status)
        {
            return GetAssemblerReadinessStatusTranslationKey(status).Translate().ToString();
        }

        internal static string GetAssemblerReadinessStatusTranslationKey(PlanAssemblerReadinessStatus status)
        {
            switch (status)
            {
                case PlanAssemblerReadinessStatus.Ready:
                    return "XenogermPlanner.ReadinessStatus.Ready";

                case PlanAssemblerReadinessStatus.NotReady:
                    return "XenogermPlanner.ReadinessStatus.NotReady";

                case PlanAssemblerReadinessStatus.Blocked:
                    return "XenogermPlanner.AssemblerReadinessStatus.Blocked";

                case PlanAssemblerReadinessStatus.EmptyTarget:
                    return "XenogermPlanner.ReadinessStatus.EmptyTarget";

                case PlanAssemblerReadinessStatus.Degraded:
                    return "XenogermPlanner.ReadinessStatus.Degraded";

                case PlanAssemblerReadinessStatus.SearchIncomplete:
                    return "XenogermPlanner.AssemblerReadinessStatus.SearchIncomplete";

                default:
                    return "XenogermPlanner.AssemblerReadinessStatus.Unknown";
            }
        }

        internal static string GetAssemblerBlockerMessage(
            PlanAssemblerReadinessResult result,
            PlanAssemblerBlockerReason blockerReason)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            string translationKey = GetAssemblerBlockerTranslationKey(blockerReason);

            switch (blockerReason)
            {
                case PlanAssemblerBlockerReason.MissingPrerequisite:
                    return translationKey.Translate(GetMissingPrerequisiteSummary(result.MissingPrerequisites))
                        .ToString();

                case PlanAssemblerBlockerReason.InsufficientComplexity:
                    return translationKey.Translate(result.RequiredComplexity, result.AvailableComplexity).ToString();

                case PlanAssemblerBlockerReason.InsufficientArchiteCapsules:
                    return translationKey.Translate(result.RequiredArchiteCapsules, result.AvailableArchiteCapsules)
                        .ToString();

                case PlanAssemblerBlockerReason.MetabolismTooLow:
                    return translationKey.Translate(result.ActualMetabolism, result.MinimumMetabolism).ToString();

                default:
                    return translationKey.Translate().ToString();
            }
        }

        internal static string GetAssemblerBlockerTranslationKey(PlanAssemblerBlockerReason blockerReason)
        {
            switch (blockerReason)
            {
                case PlanAssemblerBlockerReason.MissingPrerequisite:
                    return "XenogermPlanner.AssemblerBlocker.MissingPrerequisite";

                case PlanAssemblerBlockerReason.UsedGeneBankUnpowered:
                    return "XenogermPlanner.AssemblerBlocker.UsedGeneBankUnpowered";

                case PlanAssemblerBlockerReason.AssemblerUnpowered:
                    return "XenogermPlanner.AssemblerBlocker.AssemblerUnpowered";

                case PlanAssemblerBlockerReason.InsufficientComplexity:
                    return "XenogermPlanner.AssemblerBlocker.InsufficientComplexity";

                case PlanAssemblerBlockerReason.ArchogeneticsResearchMissing:
                    return "XenogermPlanner.AssemblerBlocker.ArchogeneticsResearchMissing";

                case PlanAssemblerBlockerReason.InsufficientArchiteCapsules:
                    return "XenogermPlanner.AssemblerBlocker.InsufficientArchiteCapsules";

                case PlanAssemblerBlockerReason.MetabolismTooLow:
                    return "XenogermPlanner.AssemblerBlocker.MetabolismTooLow";

                default:
                    return "XenogermPlanner.AssemblerBlocker.Unknown";
            }
        }

        internal static string GetGeneConflictTranslationKey(PlanGeneConflictDiagnostic diagnostic)
        {
            if (diagnostic == null)
                throw new ArgumentNullException(nameof(diagnostic));

            switch (diagnostic.Kind)
            {
                case PlanGeneConflictKind.Ordinary:
                    return diagnostic.HasPredictedWinner
                        ? "XenogermPlanner.GeneDiagnostics.Conflict.OrdinaryWinner"
                        : "XenogermPlanner.GeneDiagnostics.Conflict.Ordinary";

                case PlanGeneConflictKind.RandomChosen:
                    return "XenogermPlanner.GeneDiagnostics.Conflict.RandomChosen";

                case PlanGeneConflictKind.Mixed:
                    return "XenogermPlanner.GeneDiagnostics.Conflict.Mixed";

                default:
                    return "XenogermPlanner.GeneDiagnostics.Conflict.Unknown";
            }
        }

        internal static string GetGeneConflictMessage(PlanGeneConflictDiagnostic diagnostic)
        {
            string translationKey = GetGeneConflictTranslationKey(diagnostic);

            if (diagnostic.Kind == PlanGeneConflictKind.Ordinary && diagnostic.HasPredictedWinner)
            {
                return translationKey.Translate(
                        GetGeneDisplayName(diagnostic.OverridingGene),
                        GetGeneDisplayName(diagnostic.OverriddenGene))
                    .ToString();
            }

            return translationKey.Translate(
                    GetGeneDisplayName(diagnostic.FirstGene),
                    GetGeneDisplayName(diagnostic.SecondGene))
                .ToString();
        }

        internal static string GetGeneRandomChoiceGroupTranslationKey(PlanGeneRandomChoiceGroupDiagnostic diagnostic)
        {
            if (diagnostic == null)
                throw new ArgumentNullException(nameof(diagnostic));

            return "XenogermPlanner.GeneDiagnostics.Conflict.RandomChosenGroup";
        }

        internal static string GetGeneRandomChoiceGroupMessage(PlanGeneRandomChoiceGroupDiagnostic diagnostic)
        {
            return GetGeneRandomChoiceGroupTranslationKey(diagnostic).Translate().ToString();
        }

        internal static string GetGeneEffectsTabTranslationKey()
        {
            return "XenogermPlanner.Planner.Tab.GeneEffects";
        }

        internal static string GetGeneEffectsTabLabel(int diagnosticCount)
        {
            if (diagnosticCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(diagnosticCount),
                    diagnosticCount,
                    "Diagnostic count cannot be negative.");
            }

            return GetGeneEffectsTabTranslationKey().Translate(diagnosticCount).ToString();
        }

        internal static string GetGenePrerequisiteTranslationKey(PlanReadinessMode readinessMode)
        {
            switch (readinessMode)
            {
                case PlanReadinessMode.Coverage:
                    return "XenogermPlanner.GeneDiagnostics.Prerequisite.MissingCoverage";

                case PlanReadinessMode.ExactPayload:
                    return "XenogermPlanner.GeneDiagnostics.Prerequisite.MissingExactPayload";

                default:
                    return "XenogermPlanner.GeneDiagnostics.Prerequisite.Unknown";
            }
        }

        internal static string GetGenePrerequisiteMessage(
            PlanGenePrerequisiteDiagnostic diagnostic,
            PlanReadinessMode readinessMode)
        {
            if (diagnostic == null)
                throw new ArgumentNullException(nameof(diagnostic));

            return GetGenePrerequisiteTranslationKey(readinessMode)
                .Translate(
                    GetGeneDisplayName(diagnostic.DependentGene),
                    GetGeneDisplayName(diagnostic.PrerequisiteGene))
                .ToString();
        }

        internal static List<PlanGeneConflictDiagnostic> GetSortedGeneConflictDiagnostics(
            IEnumerable<PlanGeneConflictDiagnostic> diagnostics)
        {
            if (diagnostics == null)
                throw new ArgumentNullException(nameof(diagnostics));

            var sorted = new List<PlanGeneConflictDiagnostic>();

            foreach (PlanGeneConflictDiagnostic diagnostic in diagnostics)
            {
                if (diagnostic == null)
                {
                    throw new ArgumentException(
                        "Gene conflict diagnostic collection cannot contain null values.",
                        nameof(diagnostics));
                }

                sorted.Add(diagnostic);
            }

            sorted.Sort(CompareGeneConflicts);
            return sorted;
        }

        internal static List<PlanGeneRandomChoiceGroupDiagnostic> GetSortedGeneRandomChoiceGroupDiagnostics(
            IEnumerable<PlanGeneRandomChoiceGroupDiagnostic> diagnostics)
        {
            if (diagnostics == null)
                throw new ArgumentNullException(nameof(diagnostics));

            var sorted = new List<PlanGeneRandomChoiceGroupDiagnostic>();

            foreach (PlanGeneRandomChoiceGroupDiagnostic diagnostic in diagnostics)
            {
                if (diagnostic == null)
                {
                    throw new ArgumentException(
                        "Random-choice diagnostic collection cannot contain null values.",
                        nameof(diagnostics));
                }

                sorted.Add(diagnostic);
            }

            sorted.Sort(CompareGeneRandomChoiceGroups);
            return sorted;
        }

        internal static List<PlanGenePrerequisiteDiagnostic> GetSortedGenePrerequisiteDiagnostics(
            IEnumerable<PlanGenePrerequisiteDiagnostic> diagnostics)
        {
            if (diagnostics == null)
                throw new ArgumentNullException(nameof(diagnostics));

            var sorted = new List<PlanGenePrerequisiteDiagnostic>();

            foreach (PlanGenePrerequisiteDiagnostic diagnostic in diagnostics)
            {
                if (diagnostic == null)
                {
                    throw new ArgumentException(
                        "Gene prerequisite diagnostic collection cannot contain null values.",
                        nameof(diagnostics));
                }

                sorted.Add(diagnostic);
            }

            sorted.Sort(CompareGenePrerequisites);
            return sorted;
        }

        internal static string GetReadinessDiagnosticMessage(PlanReadinessResult result)
        {
            string translationKey = GetReadinessDiagnosticTranslationKey(result);

            return translationKey?.Translate().ToString();
        }

        internal static string GetReadinessDiagnosticTranslationKey(PlanReadinessResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (result.Status == PlanReadinessStatus.Unavailable &&
                result.UnavailableReason == PlanReadinessUnavailableReason.NoActiveMap)
            {
                return "XenogermPlanner.Planner.ReadinessNoActiveMap";
            }

            if (result.Status == PlanReadinessStatus.EmptyTarget)
                return "XenogermPlanner.Planner.ReadinessEmptyTarget";

            if (result.HasExactPayloadConflict)
            {
                return "XenogermPlanner.Planner.ReadinessExactPayloadConflict";
            }

            return null;
        }

        internal static string GetAssemblerScopeDiagnosticMessage(PlanReadinessResult result)
        {
            string translationKey = GetAssemblerScopeDiagnosticTranslationKey(result);

            return translationKey?.Translate().ToString();
        }

        internal static string GetAssemblerScopeDiagnosticTranslationKey(PlanReadinessResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (result.Status == PlanReadinessStatus.EmptyTarget)
                return "XenogermPlanner.Planner.ReadinessEmptyTarget";

            if (result.HasExactPayloadConflict)
            {
                return "XenogermPlanner.Planner.AssemblerScopeExactPayloadConflict";
            }

            return null;
        }

        internal static bool ShouldShowReadinessGeneDiagnostics(PlanReadinessResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            return result.Status == PlanReadinessStatus.Ready ||
                   result.Status == PlanReadinessStatus.NotReady ||
                   result.Status == PlanReadinessStatus.Degraded;
        }

        internal static string GetGeneCoverageStateLabel(PlanGeneCoverageDiagnostic diagnostic)
        {
            return GetGeneCoverageStateTranslationKey(diagnostic).Translate().ToString();
        }

        internal static string GetGeneCoverageStateTranslationKey(PlanGeneCoverageDiagnostic diagnostic)
        {
            if (diagnostic == null)
                throw new ArgumentNullException(nameof(diagnostic));

            switch (diagnostic.State)
            {
                case PlanGeneCoverageState.Available:
                    return "XenogermPlanner.GeneCoverageState.Covered";

                case PlanGeneCoverageState.ExactPayloadConflict:
                    return "XenogermPlanner.GeneCoverageState.ExactPayloadConflict";

                case PlanGeneCoverageState.Missing:
                    return "XenogermPlanner.GeneCoverageState.Missing";

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(diagnostic),
                        diagnostic.State,
                        "Unsupported gene coverage state.");
            }
        }

        internal static string GetGeneCoverageStateTooltip(PlanGeneCoverageDiagnostic diagnostic)
        {
            return GetGeneCoverageStateTooltipTranslationKey(diagnostic).Translate().ToString();
        }

        internal static string GetGeneCoverageStateTooltipTranslationKey(PlanGeneCoverageDiagnostic diagnostic)
        {
            if (diagnostic == null)
                throw new ArgumentNullException(nameof(diagnostic));

            switch (diagnostic.State)
            {
                case PlanGeneCoverageState.Available:
                    return "XenogermPlanner.GeneCoverageState.AvailableTooltip";

                case PlanGeneCoverageState.ExactPayloadConflict:
                    return "XenogermPlanner.GeneCoverageState.ExactPayloadConflictTooltip";

                case PlanGeneCoverageState.Missing:
                    return "XenogermPlanner.GeneCoverageState.MissingTooltip";

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(diagnostic),
                        diagnostic.State,
                        "Unsupported gene coverage state.");
            }
        }

        internal static string GetGenepackExactCompatibilityMessage(PlanGenepackCompositionDiagnostic composition)
        {
            if (composition == null)
                throw new ArgumentNullException(nameof(composition));

            string translationKey = composition.IsExactPayloadEligible
                ? "XenogermPlanner.Planner.ExactCompatible"
                : "XenogermPlanner.Planner.ExactIncompatible";

            return translationKey.Translate().ToString();
        }

        internal static string GetGeneDisplayName(GeneDef gene)
        {
            if (gene == null)
                throw new ArgumentNullException(nameof(gene));

            var label = gene.LabelCap.ToString();

            if (!string.IsNullOrWhiteSpace(label))
                return label;

            return gene.defName ?? string.Empty;
        }

        internal static string GetPotentialDonorDisplayName(Pawn donor)
        {
            if (donor == null)
                throw new ArgumentNullException(nameof(donor));

            string label = donor.LabelCap;

            if (string.IsNullOrWhiteSpace(label))
                label = donor.def?.label;

            if (string.IsNullOrWhiteSpace(label))
                label = donor.ThingID;

            return string.IsNullOrWhiteSpace(label)
                ? "XenogermPlanner.PotentialDonors.UnnamedPawn".Translate().ToString()
                : label;
        }

        internal static List<Pawn> GetSortedPotentialDonors(IEnumerable<Pawn> donors)
        {
            return GetSortedPotentialDonors(donors, GetPotentialDonorDisplayName, donor => donor.thingIDNumber);
        }

        internal static List<Pawn> GetSortedPotentialDonors(
            IEnumerable<Pawn> donors,
            Func<Pawn, string> getDisplayName,
            Func<Pawn, int> getStableKey)
        {
            if (donors == null)
                throw new ArgumentNullException(nameof(donors));

            if (getDisplayName == null)
                throw new ArgumentNullException(nameof(getDisplayName));

            if (getStableKey == null)
                throw new ArgumentNullException(nameof(getStableKey));

            var sortedDonors = new List<Pawn>();

            foreach (Pawn donor in donors)
            {
                if (donor == null)
                {
                    throw new ArgumentException(
                        "Potential donor collection cannot contain null values.",
                        nameof(donors));
                }

                sortedDonors.Add(donor);
            }

            sortedDonors.Sort((left, right) =>
            {
                int displayNameComparison = StringComparer.CurrentCultureIgnoreCase.Compare(
                    getDisplayName(left) ?? string.Empty,
                    getDisplayName(right) ?? string.Empty);

                return displayNameComparison != 0
                    ? displayNameComparison
                    : getStableKey(left).CompareTo(getStableKey(right));
            });

            return sortedDonors;
        }

        internal static bool TryGetPotentialDonorDiagnostic(
            PlanGeneCoverageDiagnostic coverageDiagnostic,
            PlanPotentialDonorAnalysisResult potentialDonorAnalysis,
            out PlanPotentialDonorGeneDiagnostic potentialDonorDiagnostic)
        {
            if (coverageDiagnostic == null)
                throw new ArgumentNullException(nameof(coverageDiagnostic));

            if (potentialDonorAnalysis == null)
                throw new ArgumentNullException(nameof(potentialDonorAnalysis));

            if (coverageDiagnostic.IsCovered || !potentialDonorAnalysis.IsAvailable)
            {
                potentialDonorDiagnostic = null;
                return false;
            }

            return potentialDonorAnalysis.TryGetDiagnostic(coverageDiagnostic.Gene, out potentialDonorDiagnostic);
        }

        internal static List<GeneDef> GetSortedGenes(IEnumerable<GeneDef> genes)
        {
            List<GeneDef> sortedGenes = CopyGenes(genes);

            sortedGenes.Sort(CompareGenesByDisplayName);

            return sortedGenes;
        }

        internal static void GetSortedGenepackGeneGroups(
            IEnumerable<GeneDef> genes,
            out List<GeneDef> nonArchiteGenes,
            out List<GeneDef> architeGenes)
        {
            List<GeneDef> sortedGenes = GetSortedGenes(genes);

            nonArchiteGenes = new List<GeneDef>();
            architeGenes = new List<GeneDef>();

            foreach (GeneDef gene in sortedGenes)
            {
                if (gene.biostatArc > 0)
                {
                    architeGenes.Add(gene);
                }
                else
                {
                    nonArchiteGenes.Add(gene);
                }
            }
        }

        internal static List<PlanGeneCoverageDiagnostic> GetSortedGeneCoverageDiagnostics(
            IEnumerable<PlanGeneCoverageDiagnostic> diagnostics)
        {
            if (diagnostics == null)
                throw new ArgumentNullException(nameof(diagnostics));

            var sortedDiagnostics = new List<PlanGeneCoverageDiagnostic>();

            foreach (PlanGeneCoverageDiagnostic diagnostic in diagnostics)
            {
                if (diagnostic == null)
                {
                    throw new ArgumentException(
                        "Gene coverage diagnostic collection cannot contain null values.",
                        nameof(diagnostics));
                }

                sortedDiagnostics.Add(diagnostic);
            }

            sortedDiagnostics.Sort((left, right) => CompareGenesByDisplayName(left.Gene, right.Gene));

            return sortedDiagnostics;
        }

        internal static List<GeneCoverageTableRow> GetSortedGeneCoverageRows(
            IEnumerable<PlanGeneCoverageDiagnostic> diagnostics,
            IEnumerable<string> unresolvedGeneDefNames,
            IReadOnlyDictionary<PlanGenepackCompositionDiagnostic, IReadOnlyList<Genepack>> genepacksByComposition,
            GeneCoverageSortState sortState)
        {
            return GetSortedGeneCoverageRows(
                diagnostics,
                unresolvedGeneDefNames,
                genepacksByComposition,
                null,
                sortState);
        }

        internal static List<GeneCoverageTableRow> GetSortedGeneCoverageRows(
            IEnumerable<PlanGeneCoverageDiagnostic> diagnostics,
            IEnumerable<string> unresolvedGeneDefNames,
            IReadOnlyDictionary<PlanGenepackCompositionDiagnostic, IReadOnlyList<Genepack>> genepacksByComposition,
            PlanPotentialDonorAnalysisResult potentialDonorAnalysis,
            GeneCoverageSortState sortState)
        {
            if (diagnostics == null)
                throw new ArgumentNullException(nameof(diagnostics));

            if (unresolvedGeneDefNames == null)
                throw new ArgumentNullException(nameof(unresolvedGeneDefNames));

            if (genepacksByComposition == null)
                throw new ArgumentNullException(nameof(genepacksByComposition));

            var rows = new List<GeneCoverageTableRow>();

            foreach (PlanGeneCoverageDiagnostic diagnostic in diagnostics)
            {
                if (diagnostic == null)
                {
                    throw new ArgumentException(
                        "Gene coverage diagnostic collection cannot contain null values.",
                        nameof(diagnostics));
                }

                string displayName = GetGeneDisplayName(diagnostic.Gene);
                string stableKey = diagnostic.Gene.defName ?? string.Empty;
                int sourceGenepackCount = CountDisplayedGenepacks(diagnostic, genepacksByComposition);
                int? potentialDonorCount = GetPotentialDonorCount(diagnostic, potentialDonorAnalysis);

                rows.Add(
                    GeneCoverageTableRow.CreateResolved(
                        diagnostic,
                        displayName,
                        stableKey,
                        sourceGenepackCount,
                        potentialDonorCount));
            }

            foreach (string unresolvedGeneDefName in unresolvedGeneDefNames)
                rows.Add(GeneCoverageTableRow.CreateUnresolved(unresolvedGeneDefName));

            rows.Sort((left, right) => CompareGeneCoverageRows(left, right, sortState));

            return rows;
        }

        internal static List<PlanGenepackCompositionDiagnostic> GetSortedGenepackCompositions(
            IEnumerable<PlanGenepackCompositionDiagnostic> compositions)
        {
            if (compositions == null)
                throw new ArgumentNullException(nameof(compositions));

            var sortedCompositions = new List<PlanGenepackCompositionDiagnostic>();

            foreach (PlanGenepackCompositionDiagnostic composition in compositions)
            {
                if (composition == null)
                {
                    throw new ArgumentException(
                        "Genepack composition collection cannot contain null values.",
                        nameof(compositions));
                }

                sortedCompositions.Add(composition);
            }

            sortedCompositions.Sort(CompareGenepackCompositions);

            return sortedCompositions;
        }

        internal static List<GeneDef> GetGenesInCatalogOrder(IEnumerable<GeneDef> genes)
        {
            List<GeneDef> sortedGenes = CopyGenes(genes);

            sortedGenes.Sort(CompareGenesByCatalogOrder);

            return sortedGenes;
        }

        private static string GetMissingPrerequisiteSummary(IEnumerable<PlanGenePrerequisiteDiagnostic> diagnostics)
        {
            List<PlanGenePrerequisiteDiagnostic> sorted = GetSortedGenePrerequisiteDiagnostics(diagnostics);
            var relations = new List<string>(sorted.Count);

            foreach (PlanGenePrerequisiteDiagnostic diagnostic in sorted)
            {
                relations.Add(
                    "XenogermPlanner.GeneDiagnostics.Prerequisite.Relation".Translate(
                            GetGeneDisplayName(diagnostic.DependentGene),
                            GetGeneDisplayName(diagnostic.PrerequisiteGene))
                        .ToString());
            }

            return string.Join("; ", relations);
        }

        private static int? GetPotentialDonorCount(
            PlanGeneCoverageDiagnostic diagnostic,
            PlanPotentialDonorAnalysisResult potentialDonorAnalysis)
        {
            if (potentialDonorAnalysis == null)
                return null;

            return TryGetPotentialDonorDiagnostic(
                diagnostic,
                potentialDonorAnalysis,
                out PlanPotentialDonorGeneDiagnostic potentialDonorDiagnostic)
                ? potentialDonorDiagnostic.DonorCount
                : null;
        }

        private static int CountDisplayedGenepacks(
            PlanGeneCoverageDiagnostic diagnostic,
            IReadOnlyDictionary<PlanGenepackCompositionDiagnostic, IReadOnlyList<Genepack>> genepacksByComposition)
        {
            var count = 0;

            foreach (PlanGenepackCompositionDiagnostic composition in diagnostic.SourceGenepackCompositions)
            {
                if (!genepacksByComposition.TryGetValue(composition, out IReadOnlyList<Genepack> genepacks))
                    continue;

                if (genepacks == null)
                {
                    throw new ArgumentException(
                        "Genepack composition lookup cannot contain null values.",
                        nameof(genepacksByComposition));
                }

                count += genepacks.Count;
            }

            return count;
        }

        private static int CompareGeneCoverageRows(
            GeneCoverageTableRow left,
            GeneCoverageTableRow right,
            GeneCoverageSortState sortState)
        {
            switch (sortState.Column)
            {
                case GeneCoverageSortColumn.Gene:
                    return sortState.Descending
                        ? CompareGeneCoverageRowsByGene(right, left)
                        : CompareGeneCoverageRowsByGene(left, right);

                case GeneCoverageSortColumn.Availability:
                    {
                        int comparison = sortState.Descending
                            ? right.AvailabilityState.CompareTo(left.AvailabilityState)
                            : left.AvailabilityState.CompareTo(right.AvailabilityState);

                        return comparison != 0 ? comparison : CompareGeneCoverageRowsByGene(left, right);
                    }

                case GeneCoverageSortColumn.GenepackCount:
                    {
                        int comparison = sortState.Descending
                            ? right.SourceGenepackCount.CompareTo(left.SourceGenepackCount)
                            : left.SourceGenepackCount.CompareTo(right.SourceGenepackCount);

                        return comparison != 0 ? comparison : CompareGeneCoverageRowsByGene(left, right);
                    }

                case GeneCoverageSortColumn.PotentialDonorCount:
                    return CompareGeneCoverageRowsByPotentialDonorCount(left, right, sortState.Descending);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(sortState),
                        sortState.Column,
                        "Unsupported gene coverage sort column.");
            }
        }

        private static int CompareGeneCoverageRowsByPotentialDonorCount(
            GeneCoverageTableRow left,
            GeneCoverageTableRow right,
            bool descending)
        {
            bool leftHasCount = left.PotentialDonorCount.HasValue;
            bool rightHasCount = right.PotentialDonorCount.HasValue;

            if (leftHasCount != rightHasCount)
                return leftHasCount ? -1 : 1;

            if (!leftHasCount)
                return CompareGeneCoverageRowsByGene(left, right);

            int comparison = descending
                ? right.PotentialDonorCount.Value.CompareTo(left.PotentialDonorCount.Value)
                : left.PotentialDonorCount.Value.CompareTo(right.PotentialDonorCount.Value);

            return comparison != 0 ? comparison : CompareGeneCoverageRowsByGene(left, right);
        }

        private static int CompareGeneCoverageRowsByGene(GeneCoverageTableRow left, GeneCoverageTableRow right)
        {
            int displayNameComparison = StringComparer.CurrentCultureIgnoreCase.Compare(
                left.DisplayName,
                right.DisplayName);

            if (displayNameComparison != 0)
                return displayNameComparison;

            return StringComparer.Ordinal.Compare(left.StableKey, right.StableKey);
        }

        private static int CompareGeneConflicts(PlanGeneConflictDiagnostic left, PlanGeneConflictDiagnostic right)
        {
            int comparison = CompareGenesByDisplayName(left.FirstGene, right.FirstGene);

            if (comparison != 0)
                return comparison;

            comparison = CompareGenesByDisplayName(left.SecondGene, right.SecondGene);

            return comparison != 0 ? comparison : left.Kind.CompareTo(right.Kind);
        }

        private static int CompareGeneRandomChoiceGroups(
            PlanGeneRandomChoiceGroupDiagnostic left,
            PlanGeneRandomChoiceGroupDiagnostic right)
        {
            List<GeneDef> leftGenes = GetSortedGenes(left.Genes);
            List<GeneDef> rightGenes = GetSortedGenes(right.Genes);
            int sharedCount = Math.Min(leftGenes.Count, rightGenes.Count);

            for (var index = 0; index < sharedCount; index++)
            {
                int comparison = CompareGenesByDisplayName(leftGenes[index], rightGenes[index]);

                if (comparison != 0)
                    return comparison;
            }

            return leftGenes.Count.CompareTo(rightGenes.Count);
        }

        private static int CompareGenePrerequisites(
            PlanGenePrerequisiteDiagnostic left,
            PlanGenePrerequisiteDiagnostic right)
        {
            int comparison = CompareGenesByDisplayName(left.DependentGene, right.DependentGene);

            return comparison != 0
                ? comparison
                : CompareGenesByDisplayName(left.PrerequisiteGene, right.PrerequisiteGene);
        }

        private static int CompareGenepackCompositions(
            PlanGenepackCompositionDiagnostic left,
            PlanGenepackCompositionDiagnostic right)
        {
            int geneListComparison = StringComparer.CurrentCultureIgnoreCase.Compare(
                GetGenepackCompositionSortKey(left),
                GetGenepackCompositionSortKey(right));

            if (geneListComparison != 0)
                return geneListComparison;

            return left.PhysicalPackCount.CompareTo(right.PhysicalPackCount);
        }

        private static string GetGenepackCompositionSortKey(PlanGenepackCompositionDiagnostic composition)
        {
            return JoinGeneDisplayNames(composition.Genes, "\n");
        }

        private static string JoinGeneDisplayNames(IEnumerable<GeneDef> genes, string separator)
        {
            List<GeneDef> sortedGenes = GetSortedGenes(genes);
            var displayNames = new List<string>(sortedGenes.Count);

            foreach (GeneDef gene in sortedGenes)
                displayNames.Add(GetGeneDisplayName(gene));

            return string.Join(separator, displayNames);
        }

        private static List<GeneDef> CopyGenes(IEnumerable<GeneDef> genes)
        {
            if (genes == null)
                throw new ArgumentNullException(nameof(genes));

            var copiedGenes = new List<GeneDef>();

            foreach (GeneDef gene in genes)
            {
                if (gene == null)
                {
                    throw new ArgumentException("Gene collection cannot contain null values.", nameof(genes));
                }

                copiedGenes.Add(gene);
            }

            return copiedGenes;
        }

        private static int ComparePlansForTraderAdvisory(XenogermPlan left, XenogermPlan right)
        {
            int displayNameComparison = StringComparer.OrdinalIgnoreCase.Compare(
                GetPlanDisplayName(left),
                GetPlanDisplayName(right));

            if (displayNameComparison != 0)
                return displayNameComparison;

            return StringComparer.Ordinal.Compare(left.Id, right.Id);
        }

        private static int CompareGenesByDisplayName(GeneDef left, GeneDef right)
        {
            int displayNameComparison = StringComparer.CurrentCultureIgnoreCase.Compare(
                GetGeneDisplayName(left),
                GetGeneDisplayName(right));

            if (displayNameComparison != 0)
                return displayNameComparison;

            return StringComparer.Ordinal.Compare(left.defName ?? string.Empty, right.defName ?? string.Empty);
        }

        private static int CompareGenesByCatalogOrder(GeneDef left, GeneDef right)
        {
            GeneCategoryDef leftCategory = left.displayCategory;

            GeneCategoryDef rightCategory = right.displayCategory;

            int categoryPriorityComparison =
                rightCategory.displayPriorityInXenotype.CompareTo(leftCategory.displayPriorityInXenotype);

            if (categoryPriorityComparison != 0)
                return categoryPriorityComparison;

            int categoryLabelComparison = StringComparer.CurrentCultureIgnoreCase.Compare(
                leftCategory.label ?? string.Empty,
                rightCategory.label ?? string.Empty);

            if (categoryLabelComparison != 0)
                return categoryLabelComparison;

            int categoryDefNameComparison = StringComparer.Ordinal.Compare(
                leftCategory.defName ?? string.Empty,
                rightCategory.defName ?? string.Empty);

            if (categoryDefNameComparison != 0)
                return categoryDefNameComparison;

            int displayOrderComparison = left.displayOrderInCategory.CompareTo(right.displayOrderInCategory);

            if (displayOrderComparison != 0)
                return displayOrderComparison;

            return StringComparer.Ordinal.Compare(left.defName ?? string.Empty, right.defName ?? string.Empty);
        }
    }
}