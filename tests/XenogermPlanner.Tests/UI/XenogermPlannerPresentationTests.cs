using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Escarval.RimWorld.UI;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Assemblers;
using XenogermPlanner.Donors;
using XenogermPlanner.Plans;
using XenogermPlanner.Templates;
using XenogermPlanner.Tests.Donors;
using XenogermPlanner.Tests.Genes;
using XenogermPlanner.Trade;
using XenogermPlanner.UI;

namespace XenogermPlanner.Tests.UI
{
    [TestFixture]
    public sealed class XenogermPlannerPresentationTests
    {
        [Test]
        public void GetReadinessDiagnosticTranslationKey_NoActiveMapUsesUnavailableDiagnostic()
        {
            var result = PlanReadinessResult.CreateUnavailable(PlanReadinessUnavailableReason.NoActiveMap);

            string translationKey = XenogermPlannerPresentation.GetReadinessDiagnosticTranslationKey(result);

            Assert.That(translationKey, Is.EqualTo("XenogermPlanner.Planner.ReadinessNoActiveMap"));
        }

        [Test]
        public void GetReadinessDiagnosticTranslationKey_EmptyTargetUsesEmptyTargetDiagnostic()
        {
            var result = PlanReadinessResult.CreateEmptyTarget();

            string translationKey = XenogermPlannerPresentation.GetReadinessDiagnosticTranslationKey(result);

            Assert.That(translationKey, Is.EqualTo("XenogermPlanner.Planner.ReadinessEmptyTarget"));
        }

        [Test]
        public void GetReadinessDiagnosticTranslationKey_ExactPayloadConflictUsesConflictDiagnostic()
        {
            var result = PlanReadinessResult.CreateNotReady([], [], true);

            string translationKey = XenogermPlannerPresentation.GetReadinessDiagnosticTranslationKey(result);

            Assert.That(translationKey, Is.EqualTo("XenogermPlanner.Planner.ReadinessExactPayloadConflict"));
        }

        [Test]
        public void GetReadinessDiagnosticTranslationKey_ReadyHasNoDiagnostic()
        {
            var result = PlanReadinessResult.CreateReady([]);

            string translationKey = XenogermPlannerPresentation.GetReadinessDiagnosticTranslationKey(result);

            Assert.That(translationKey, Is.Null);
        }

        [Test]
        public void GetReadinessDiagnosticTranslationKey_OrdinaryNotReadyHasNoDiagnostic()
        {
            var missingGene = new GeneDef();

            var result = PlanReadinessResult.CreateNotReady([], [missingGene], false);

            string translationKey = XenogermPlannerPresentation.GetReadinessDiagnosticTranslationKey(result);

            Assert.That(translationKey, Is.Null);
        }

        [Test]
        public void GetAssemblerScopeDiagnosticTranslationKey_EmptyTargetUsesReadinessDiagnostic()
        {
            var result = PlanReadinessResult.CreateEmptyTarget();

            string translationKey = XenogermPlannerPresentation.GetAssemblerScopeDiagnosticTranslationKey(result);

            Assert.That(translationKey, Is.EqualTo("XenogermPlanner.Planner.ReadinessEmptyTarget"));
        }

        [Test]
        public void GetAssemblerScopeDiagnosticTranslationKey_ExactPayloadConflictUsesAssemblerDiagnostic()
        {
            var result = PlanReadinessResult.CreateNotReady([], [], true);

            string translationKey = XenogermPlannerPresentation.GetAssemblerScopeDiagnosticTranslationKey(result);

            Assert.That(translationKey, Is.EqualTo("XenogermPlanner.Planner.AssemblerScopeExactPayloadConflict"));
        }

        [TestCase(PlanReadinessStatus.Ready)]
        [TestCase(PlanReadinessStatus.NotReady)]
        public void GetAssemblerScopeDiagnosticTranslationKey_OrdinaryStatusHasNoDiagnostic(PlanReadinessStatus status)
        {
            PlanReadinessResult result = CreateResult(status);

            string translationKey = XenogermPlannerPresentation.GetAssemblerScopeDiagnosticTranslationKey(result);

            Assert.That(translationKey, Is.Null);
        }

        [TestCase(PlanReadinessStatus.Ready, true)]
        [TestCase(PlanReadinessStatus.NotReady, true)]
        [TestCase(PlanReadinessStatus.Degraded, true)]
        [TestCase(PlanReadinessStatus.EmptyTarget, false)]
        [TestCase(PlanReadinessStatus.Unavailable, false)]
        public void ShouldShowReadinessGeneDiagnostics_UsesStatusPresentationPolicy(
            PlanReadinessStatus status,
            bool expected)
        {
            PlanReadinessResult result = CreateResult(status);

            bool shouldShow = XenogermPlannerPresentation.ShouldShowReadinessGeneDiagnostics(result);

            Assert.That(shouldShow, Is.EqualTo(expected));
        }

        [Test]
        public void GetGeneCoverageStateTranslationKey_MapsSemanticStates()
        {
            GeneDef availableGene = CreateGene("Available", "Available");
            GeneDef conflictGene = CreateGene("Conflict", "Conflict");
            GeneDef additionalGene = CreateGene("Additional", "Additional");
            GeneDef missingGene = CreateGene("Missing", "Missing");
            PlanGenepackCompositionDiagnostic availableComposition = CreateComposition(availableGene, 1);
            PlanGenepackCompositionDiagnostic conflictComposition = CreateExactPayloadConflictComposition(
                conflictGene,
                additionalGene,
                1);
            var availableDiagnostic = new PlanGeneCoverageDiagnostic(
                availableGene,
                PlanGeneCoverageState.Available,
                [availableComposition]);
            var conflictDiagnostic = new PlanGeneCoverageDiagnostic(
                conflictGene,
                PlanGeneCoverageState.ExactPayloadConflict,
                [conflictComposition]);
            var missingDiagnostic = new PlanGeneCoverageDiagnostic(missingGene, PlanGeneCoverageState.Missing, []);

            Assert.That(
                XenogermPlannerPresentation.GetGeneCoverageStateTranslationKey(availableDiagnostic),
                Is.EqualTo("XenogermPlanner.GeneCoverageState.Covered"));
            Assert.That(
                XenogermPlannerPresentation.GetGeneCoverageStateTranslationKey(conflictDiagnostic),
                Is.EqualTo("XenogermPlanner.GeneCoverageState.ExactPayloadConflict"));
            Assert.That(
                XenogermPlannerPresentation.GetGeneCoverageStateTranslationKey(missingDiagnostic),
                Is.EqualTo("XenogermPlanner.GeneCoverageState.Missing"));
        }

        [Test]
        public void GetGeneCoverageStateTooltipTranslationKey_MapsSemanticStates()
        {
            GeneDef availableGene = CreateGene("Available", "Available");
            GeneDef conflictGene = CreateGene("Conflict", "Conflict");
            GeneDef additionalGene = CreateGene("Additional", "Additional");
            GeneDef missingGene = CreateGene("Missing", "Missing");
            PlanGenepackCompositionDiagnostic availableComposition = CreateComposition(availableGene, 1);
            PlanGenepackCompositionDiagnostic conflictComposition = CreateExactPayloadConflictComposition(
                conflictGene,
                additionalGene,
                1);
            var availableDiagnostic = new PlanGeneCoverageDiagnostic(
                availableGene,
                PlanGeneCoverageState.Available,
                [availableComposition]);
            var conflictDiagnostic = new PlanGeneCoverageDiagnostic(
                conflictGene,
                PlanGeneCoverageState.ExactPayloadConflict,
                [conflictComposition]);
            var missingDiagnostic = new PlanGeneCoverageDiagnostic(missingGene, PlanGeneCoverageState.Missing, []);

            Assert.That(
                XenogermPlannerPresentation.GetGeneCoverageStateTooltipTranslationKey(availableDiagnostic),
                Is.EqualTo("XenogermPlanner.GeneCoverageState.AvailableTooltip"));
            Assert.That(
                XenogermPlannerPresentation.GetGeneCoverageStateTooltipTranslationKey(conflictDiagnostic),
                Is.EqualTo("XenogermPlanner.GeneCoverageState.ExactPayloadConflictTooltip"));
            Assert.That(
                XenogermPlannerPresentation.GetGeneCoverageStateTooltipTranslationKey(missingDiagnostic),
                Is.EqualTo("XenogermPlanner.GeneCoverageState.MissingTooltip"));
        }

        [Test]
        public void GetGeneCoverageStateColor_UsesSharedSemanticColors()
        {
            Assert.That(
                XenogermPlannerWidgets.GetGeneCoverageStateColor(PlanGeneCoverageState.Available),
                Is.EqualTo(RimWorldUiStyle.Colors.PrimaryText));
            Assert.That(
                XenogermPlannerWidgets.GetGeneCoverageStateColor(PlanGeneCoverageState.ExactPayloadConflict),
                Is.EqualTo(RimWorldUiStyle.Colors.Negative));
            Assert.That(
                XenogermPlannerWidgets.GetGeneCoverageStateColor(PlanGeneCoverageState.Missing),
                Is.EqualTo(RimWorldUiStyle.Colors.Warning));
        }

        [Test]
        public void GetGeneConflictTranslationKey_UsesOrdinaryWinnerPolicy()
        {
            GeneDef winner = CreateGene("Winner", "Winner");
            GeneDef loser = CreateGene("Loser", "Loser");
            var diagnostic = new PlanGeneConflictDiagnostic(
                winner,
                loser,
                PlanGeneConflictKind.Ordinary,
                winner,
                loser);

            string translationKey = XenogermPlannerPresentation.GetGeneConflictTranslationKey(diagnostic);

            Assert.That(translationKey, Is.EqualTo("XenogermPlanner.GeneDiagnostics.Conflict.OrdinaryWinner"));
        }

        [Test]
        public void GetGeneConflictTranslationKey_UsesOrdinaryFallbackWithoutWinner()
        {
            GeneDef first = CreateGene("First", "First");
            GeneDef second = CreateGene("Second", "Second");
            var diagnostic = new PlanGeneConflictDiagnostic(first, second, PlanGeneConflictKind.Ordinary, null, null);

            string translationKey = XenogermPlannerPresentation.GetGeneConflictTranslationKey(diagnostic);

            Assert.That(translationKey, Is.EqualTo("XenogermPlanner.GeneDiagnostics.Conflict.Ordinary"));
        }

        [TestCase(PlanGeneConflictKind.RandomChosen, "XenogermPlanner.GeneDiagnostics.Conflict.RandomChosen")]
        [TestCase(PlanGeneConflictKind.Mixed, "XenogermPlanner.GeneDiagnostics.Conflict.Mixed")]
        public void GetGeneConflictTranslationKey_UsesConflictKind(
            PlanGeneConflictKind kind,
            string expectedTranslationKey)
        {
            var diagnostic = new PlanGeneConflictDiagnostic(
                CreateGene("First", "First"),
                CreateGene("Second", "Second"),
                kind,
                null,
                null);

            Assert.That(
                XenogermPlannerPresentation.GetGeneConflictTranslationKey(diagnostic),
                Is.EqualTo(expectedTranslationKey));
        }

        [Test]
        public void GetGeneRandomChoiceGroupTranslationKey_UsesGroupKey()
        {
            var diagnostic = new PlanGeneRandomChoiceGroupDiagnostic(
            [
                CreateGene("First", "First"),
                CreateGene("Second", "Second")
            ]);

            Assert.That(
                XenogermPlannerPresentation.GetGeneRandomChoiceGroupTranslationKey(diagnostic),
                Is.EqualTo("XenogermPlanner.GeneDiagnostics.Conflict.RandomChosenGroup"));
        }

        [Test]
        public void GetGeneEffectsTabTranslationKey_UsesTabKey()
        {
            Assert.That(
                XenogermPlannerPresentation.GetGeneEffectsTabTranslationKey(),
                Is.EqualTo("XenogermPlanner.Planner.Tab.GeneEffects"));
        }

        [TestCase(PlanReadinessMode.Coverage, "XenogermPlanner.GeneDiagnostics.Prerequisite.MissingCoverage")]
        [TestCase(PlanReadinessMode.ExactPayload, "XenogermPlanner.GeneDiagnostics.Prerequisite.MissingExactPayload")]
        public void GetGenePrerequisiteTranslationKey_UsesReadinessMode(
            PlanReadinessMode readinessMode,
            string expectedTranslationKey)
        {
            Assert.That(
                XenogermPlannerPresentation.GetGenePrerequisiteTranslationKey(readinessMode),
                Is.EqualTo(expectedTranslationKey));
        }

        [Test]
        public void GetAssemblerBlockerTranslationKey_UsesMissingPrerequisiteKey()
        {
            Assert.That(
                XenogermPlannerPresentation.GetAssemblerBlockerTranslationKey(
                    PlanAssemblerBlockerReason.MissingPrerequisite),
                Is.EqualTo("XenogermPlanner.AssemblerBlocker.MissingPrerequisite"));
        }

        [Test]
        public void GetAssemblerBlockerTranslationKey_UsesMetabolismTooLowKey()
        {
            Assert.That(
                XenogermPlannerPresentation.GetAssemblerBlockerTranslationKey(
                    PlanAssemblerBlockerReason.MetabolismTooLow),
                Is.EqualTo("XenogermPlanner.AssemblerBlocker.MetabolismTooLow"));
        }

        [Test]
        public void SearchIncomplete_HasItsOwnStatusLabelAndWarningColor()
        {
            Assert.That(
                XenogermPlannerPresentation.GetAssemblerReadinessStatusTranslationKey(
                    PlanAssemblerReadinessStatus.SearchIncomplete),
                Is.EqualTo("XenogermPlanner.AssemblerReadinessStatus.SearchIncomplete"));
            Assert.That(
                XenogermPlannerWidgets.GetAssemblerReadinessStatusColor(PlanAssemblerReadinessStatus.SearchIncomplete),
                Is.EqualTo(RimWorldUiStyle.Colors.Warning));
        }

        [Test]
        public void GetSortedGeneDiagnostics_UsesPresentationOrderDeterministically()
        {
            GeneDef alpha = CreateGene("Z", "Alpha");
            GeneDef beta = CreateGene("A", "Beta");
            GeneDef gamma = CreateGene("B", "Gamma");
            var laterConflict = new PlanGeneConflictDiagnostic(beta, gamma, PlanGeneConflictKind.Mixed, null, null);
            var earlierConflict = new PlanGeneConflictDiagnostic(
                alpha,
                gamma,
                PlanGeneConflictKind.RandomChosen,
                null,
                null);
            var laterGroup = new PlanGeneRandomChoiceGroupDiagnostic([beta, gamma]);
            var earlierGroup = new PlanGeneRandomChoiceGroupDiagnostic([alpha, gamma]);
            var laterPrerequisite = new PlanGenePrerequisiteDiagnostic(gamma, beta);
            var earlierPrerequisite = new PlanGenePrerequisiteDiagnostic(alpha, beta);

            Assert.That(
                XenogermPlannerPresentation.GetSortedGeneConflictDiagnostics([laterConflict, earlierConflict]),
                Is.EqualTo([earlierConflict, laterConflict]));
            Assert.That(
                XenogermPlannerPresentation.GetSortedGeneRandomChoiceGroupDiagnostics([laterGroup, earlierGroup]),
                Is.EqualTo([earlierGroup, laterGroup]));
            Assert.That(
                XenogermPlannerPresentation.GetSortedGenePrerequisiteDiagnostics(
                [
                    laterPrerequisite, earlierPrerequisite
                ]),
                Is.EqualTo([earlierPrerequisite, laterPrerequisite]));
        }

        [Test]
        public void GetSortedGenes_OrdersByDisplayNameAndUsesDefNameTieBreak()
        {
            GeneDef secondTie = CreateGene("GeneB", "same");
            GeneDef firstByName = CreateGene("GeneC", "Alpha");
            GeneDef firstTie = CreateGene("GeneA", "Same");

            List<GeneDef> sortedGenes = XenogermPlannerPresentation.GetSortedGenes(
            [
                secondTie,
                firstByName,
                firstTie
            ]);

            Assert.That(
                sortedGenes,
                Is.EqualTo(
                [
                    firstByName,
                    firstTie,
                    secondTie
                ]));
        }

        [Test]
        public void GetSortedGenepackGeneGroups_SeparatesArchiteGenesWithoutChangingInput()
        {
            GeneDef architeGene = CreateGene("Archite", "Archite");
            architeGene.biostatArc = 1;
            GeneDef nonArchiteGene = CreateGene("Ordinary", "Ordinary");
            var genes = new List<GeneDef>
            {
                architeGene,
                nonArchiteGene
            };

            XenogermPlannerPresentation.GetSortedGenepackGeneGroups(
                genes,
                out List<GeneDef> nonArchiteGenes,
                out List<GeneDef> architeGenes);

            Assert.That(nonArchiteGenes, Is.EqualTo([nonArchiteGene]));
            Assert.That(architeGenes, Is.EqualTo([architeGene]));
            Assert.That(genes, Is.EqualTo([architeGene, nonArchiteGene]));
        }

        [Test]
        public void GetSortedGenepackGeneGroups_SortsEachGroupDeterministically()
        {
            GeneDef nonArchiteSecondTie = CreateGene("NonArchiteB", "same");
            GeneDef nonArchiteFirstByName = CreateGene("NonArchiteC", "Alpha");
            GeneDef nonArchiteFirstTie = CreateGene("NonArchiteA", "Same");
            GeneDef architeSecondTie = CreateGene("ArchiteB", "same");
            architeSecondTie.biostatArc = 1;
            GeneDef architeFirstByName = CreateGene("ArchiteC", "Alpha");
            architeFirstByName.biostatArc = 2;
            GeneDef architeFirstTie = CreateGene("ArchiteA", "Same");
            architeFirstTie.biostatArc = 1;

            XenogermPlannerPresentation.GetSortedGenepackGeneGroups(
                [
                    architeSecondTie,
                    nonArchiteSecondTie,
                    architeFirstByName,
                    nonArchiteFirstByName,
                    architeFirstTie,
                    nonArchiteFirstTie
                ],
                out List<GeneDef> nonArchiteGenes,
                out List<GeneDef> architeGenes);

            Assert.That(
                nonArchiteGenes,
                Is.EqualTo(
                [
                    nonArchiteFirstByName,
                    nonArchiteFirstTie,
                    nonArchiteSecondTie
                ]));

            Assert.That(
                architeGenes,
                Is.EqualTo(
                [
                    architeFirstByName,
                    architeFirstTie,
                    architeSecondTie
                ]));
        }

        [Test]
        public void GetSortedGenepackCompositions_OrdersByGenePresentationAndPhysicalPackCount()
        {
            GeneDef alphaGene = CreateGene("GeneAlpha", "Alpha");
            GeneDef betaGene = CreateGene("GeneBeta", "Beta");
            var betaTwoCopies = new PlanGenepackCompositionDiagnostic([betaGene], 2, true, []);
            var alphaThreeCopies = new PlanGenepackCompositionDiagnostic([alphaGene], 3, true, []);
            var betaOneCopy = new PlanGenepackCompositionDiagnostic([betaGene], 1, true, []);

            List<PlanGenepackCompositionDiagnostic> sortedCompositions =
                XenogermPlannerPresentation.GetSortedGenepackCompositions(
                [
                    betaTwoCopies,
                    alphaThreeCopies,
                    betaOneCopy
                ]);

            Assert.That(
                sortedCompositions,
                Is.EqualTo(
                [
                    alphaThreeCopies,
                    betaOneCopy,
                    betaTwoCopies
                ]));
        }

        [Test]
        public void GeneCoverageSortState_DefaultAndToggleFollowHeaderPolicy()
        {
            GeneCoverageSortState state = GeneCoverageSortState.Default;

            Assert.That(state.Column, Is.EqualTo(GeneCoverageSortColumn.Gene));
            Assert.That(state.Descending, Is.False);

            state = state.Toggle(GeneCoverageSortColumn.Availability);

            Assert.That(state.Column, Is.EqualTo(GeneCoverageSortColumn.Availability));
            Assert.That(state.Descending, Is.False);

            state = state.Toggle(GeneCoverageSortColumn.Availability);

            Assert.That(state.Column, Is.EqualTo(GeneCoverageSortColumn.Availability));
            Assert.That(state.Descending, Is.True);

            state = state.Toggle(GeneCoverageSortColumn.Gene);

            Assert.That(state.Column, Is.EqualTo(GeneCoverageSortColumn.Gene));
            Assert.That(state.Descending, Is.False);

            state = state.Toggle(GeneCoverageSortColumn.PotentialDonorCount);

            Assert.That(state.Column, Is.EqualTo(GeneCoverageSortColumn.PotentialDonorCount));
            Assert.That(state.Descending, Is.False);

            state = state.Toggle(GeneCoverageSortColumn.PotentialDonorCount);

            Assert.That(state.Column, Is.EqualTo(GeneCoverageSortColumn.PotentialDonorCount));
            Assert.That(state.Descending, Is.True);
        }

        [Test]
        public void GetSortedGeneCoverageRows_GeneSortCombinesResolvedAndUnresolvedRowsDeterministically()
        {
            GeneDef secondTieGene = CreateGene("GeneB", "same");
            GeneDef firstTieGene = CreateGene("GeneA", "Same");
            var secondTieDiagnostic = new PlanGeneCoverageDiagnostic(secondTieGene, PlanGeneCoverageState.Missing, []);
            var firstTieDiagnostic = new PlanGeneCoverageDiagnostic(firstTieGene, PlanGeneCoverageState.Missing, []);
            var diagnostics = new List<PlanGeneCoverageDiagnostic>
            {
                secondTieDiagnostic,
                firstTieDiagnostic
            };
            var unresolvedGeneDefNames = new List<string>
            {
                "AlphaUnavailable"
            };

            List<GeneCoverageTableRow> ascendingRows = XenogermPlannerPresentation.GetSortedGeneCoverageRows(
                diagnostics,
                unresolvedGeneDefNames,
                EmptyGenepackLookup(),
                GeneCoverageSortState.Default);

            Assert.That(ascendingRows[0].UnresolvedGeneDefName, Is.EqualTo("AlphaUnavailable"));
            Assert.That(ascendingRows[1].Diagnostic, Is.SameAs(firstTieDiagnostic));
            Assert.That(ascendingRows[2].Diagnostic, Is.SameAs(secondTieDiagnostic));

            var descendingState = new GeneCoverageSortState(GeneCoverageSortColumn.Gene, descending: true);

            List<GeneCoverageTableRow> descendingRows = XenogermPlannerPresentation.GetSortedGeneCoverageRows(
                diagnostics,
                unresolvedGeneDefNames,
                EmptyGenepackLookup(),
                descendingState);

            Assert.That(descendingRows[0].Diagnostic, Is.SameAs(secondTieDiagnostic));
            Assert.That(descendingRows[1].Diagnostic, Is.SameAs(firstTieDiagnostic));
            Assert.That(descendingRows[2].UnresolvedGeneDefName, Is.EqualTo("AlphaUnavailable"));
            Assert.That(diagnostics, Is.EqualTo([secondTieDiagnostic, firstTieDiagnostic]));
            Assert.That(unresolvedGeneDefNames, Is.EqualTo(["AlphaUnavailable"]));
        }

        [Test]
        public void GetSortedGeneCoverageRows_AvailabilitySortUsesSemanticStateAndGeneTieBreak()
        {
            GeneDef availableGene = CreateGene("Available", "Zulu");
            GeneDef conflictGene = CreateGene("Conflict", "Gamma");
            GeneDef additionalGene = CreateGene("Additional", "Additional");
            GeneDef secondMissingGene = CreateGene("MissingB", "Beta");
            GeneDef firstMissingGene = CreateGene("MissingA", "Alpha");
            PlanGenepackCompositionDiagnostic availableComposition = CreateComposition(availableGene, 1);
            PlanGenepackCompositionDiagnostic conflictComposition = CreateExactPayloadConflictComposition(
                conflictGene,
                additionalGene,
                1);
            var availableDiagnostic = new PlanGeneCoverageDiagnostic(
                availableGene,
                PlanGeneCoverageState.Available,
                [availableComposition]);
            var conflictDiagnostic = new PlanGeneCoverageDiagnostic(
                conflictGene,
                PlanGeneCoverageState.ExactPayloadConflict,
                [conflictComposition]);
            var secondMissingDiagnostic = new PlanGeneCoverageDiagnostic(
                secondMissingGene,
                PlanGeneCoverageState.Missing,
                []);
            var firstMissingDiagnostic = new PlanGeneCoverageDiagnostic(
                firstMissingGene,
                PlanGeneCoverageState.Missing,
                []);
            var lookup = new Dictionary<PlanGenepackCompositionDiagnostic, IReadOnlyList<Genepack>>
            {
                { availableComposition, CreateGenepacks(1) },
                { conflictComposition, CreateGenepacks(1) }
            };

            List<GeneCoverageTableRow> ascendingRows = XenogermPlannerPresentation.GetSortedGeneCoverageRows(
                [
                    secondMissingDiagnostic,
                    conflictDiagnostic,
                    availableDiagnostic,
                    firstMissingDiagnostic
                ],
                ["Unavailable"],
                lookup,
                new GeneCoverageSortState(GeneCoverageSortColumn.Availability, descending: false));

            Assert.That(ascendingRows[0].Diagnostic, Is.SameAs(availableDiagnostic));
            Assert.That(ascendingRows[1].Diagnostic, Is.SameAs(conflictDiagnostic));
            Assert.That(ascendingRows[2].Diagnostic, Is.SameAs(firstMissingDiagnostic));
            Assert.That(ascendingRows[3].Diagnostic, Is.SameAs(secondMissingDiagnostic));
            Assert.That(ascendingRows[4].AvailabilityState, Is.EqualTo(GeneCoverageAvailabilityState.Unavailable));

            List<GeneCoverageTableRow> descendingRows = XenogermPlannerPresentation.GetSortedGeneCoverageRows(
                [
                    secondMissingDiagnostic,
                    conflictDiagnostic,
                    availableDiagnostic,
                    firstMissingDiagnostic
                ],
                ["Unavailable"],
                lookup,
                new GeneCoverageSortState(GeneCoverageSortColumn.Availability, descending: true));

            Assert.That(descendingRows[0].AvailabilityState, Is.EqualTo(GeneCoverageAvailabilityState.Unavailable));
            Assert.That(descendingRows[1].Diagnostic, Is.SameAs(firstMissingDiagnostic));
            Assert.That(descendingRows[2].Diagnostic, Is.SameAs(secondMissingDiagnostic));
            Assert.That(descendingRows[3].Diagnostic, Is.SameAs(conflictDiagnostic));
            Assert.That(descendingRows[4].Diagnostic, Is.SameAs(availableDiagnostic));
        }

        [Test]
        public void GetSortedGeneCoverageRows_GenepackCountUsesActivePhysicalLookup()
        {
            GeneDef alphaGene = CreateGene("Alpha", "Alpha");
            GeneDef betaGene = CreateGene("Beta", "Beta");
            GeneDef missingGene = CreateGene("Missing", "Missing");
            PlanGenepackCompositionDiagnostic alphaComposition = CreateComposition(alphaGene, 2);
            PlanGenepackCompositionDiagnostic betaComposition = CreateComposition(betaGene, 3);
            var alphaDiagnostic = new PlanGeneCoverageDiagnostic(
                alphaGene,
                PlanGeneCoverageState.Available,
                [alphaComposition]);
            var betaDiagnostic = new PlanGeneCoverageDiagnostic(
                betaGene,
                PlanGeneCoverageState.Available,
                [betaComposition]);
            var missingDiagnostic = new PlanGeneCoverageDiagnostic(missingGene, PlanGeneCoverageState.Missing, []);
            var productLookup = new Dictionary<PlanGenepackCompositionDiagnostic, IReadOnlyList<Genepack>>
            {
                { alphaComposition, CreateGenepacks(2) },
                { betaComposition, CreateGenepacks(1) }
            };
            var assemblerLookup = new Dictionary<PlanGenepackCompositionDiagnostic, IReadOnlyList<Genepack>>
            {
                { alphaComposition, CreateGenepacks(1) },
                { betaComposition, CreateGenepacks(3) }
            };
            var ascendingState = new GeneCoverageSortState(GeneCoverageSortColumn.GenepackCount, descending: false);
            var descendingState = new GeneCoverageSortState(GeneCoverageSortColumn.GenepackCount, descending: true);

            List<GeneCoverageTableRow> ascendingProductRows = XenogermPlannerPresentation.GetSortedGeneCoverageRows(
                [betaDiagnostic, missingDiagnostic, alphaDiagnostic],
                ["Unavailable"],
                productLookup,
                ascendingState);

            Assert.That(ascendingProductRows[0].Diagnostic, Is.SameAs(missingDiagnostic));
            Assert.That(ascendingProductRows[1].UnresolvedGeneDefName, Is.EqualTo("Unavailable"));
            Assert.That(ascendingProductRows[2].Diagnostic, Is.SameAs(betaDiagnostic));
            Assert.That(ascendingProductRows[3].Diagnostic, Is.SameAs(alphaDiagnostic));

            List<GeneCoverageTableRow> productRows = XenogermPlannerPresentation.GetSortedGeneCoverageRows(
                [betaDiagnostic, missingDiagnostic, alphaDiagnostic],
                ["Unavailable"],
                productLookup,
                descendingState);

            Assert.That(productRows[0].Diagnostic, Is.SameAs(alphaDiagnostic));
            Assert.That(productRows[0].SourceGenepackCount, Is.EqualTo(2));
            Assert.That(productRows[1].Diagnostic, Is.SameAs(betaDiagnostic));
            Assert.That(productRows[1].SourceGenepackCount, Is.EqualTo(1));
            Assert.That(productRows[2].Diagnostic, Is.SameAs(missingDiagnostic));
            Assert.That(productRows[3].UnresolvedGeneDefName, Is.EqualTo("Unavailable"));

            List<GeneCoverageTableRow> assemblerRows = XenogermPlannerPresentation.GetSortedGeneCoverageRows(
                [betaDiagnostic, missingDiagnostic, alphaDiagnostic],
                ["Unavailable"],
                assemblerLookup,
                descendingState);

            Assert.That(assemblerRows[0].Diagnostic, Is.SameAs(betaDiagnostic));
            Assert.That(assemblerRows[0].SourceGenepackCount, Is.EqualTo(3));
            Assert.That(assemblerRows[1].Diagnostic, Is.SameAs(alphaDiagnostic));
            Assert.That(assemblerRows[1].SourceGenepackCount, Is.EqualTo(1));
        }

        [Test]
        public void GetSortedGeneCoverageRows_PotentialDonorCountSortsApplicableRowsBeforePlaceholders()
        {
            GeneDef zeroGene = CreateGene("Zero", "Zero");
            GeneDef oneGene = CreateGene("One", "One");
            GeneDef threeGene = CreateGene("Three", "Three");
            GeneDef coveredGene = CreateGene("Covered", "Covered");
            var zeroDiagnostic = new PlanGeneCoverageDiagnostic(zeroGene, PlanGeneCoverageState.Missing, []);
            var oneDiagnostic = new PlanGeneCoverageDiagnostic(oneGene, PlanGeneCoverageState.Missing, []);
            var threeDiagnostic = new PlanGeneCoverageDiagnostic(threeGene, PlanGeneCoverageState.Missing, []);
            PlanGenepackCompositionDiagnostic coveredComposition = CreateComposition(coveredGene, 1);
            var coveredDiagnostic = new PlanGeneCoverageDiagnostic(
                coveredGene,
                PlanGeneCoverageState.Available,
                [coveredComposition]);
            var donorAnalysis = PlanPotentialDonorAnalysisResult.CreateAvailable(
            [
                new PlanPotentialDonorGeneDiagnostic(zeroGene, []),
                new PlanPotentialDonorGeneDiagnostic(oneGene, [PlanPotentialDonorTestData.CreatePawn()]),
                new PlanPotentialDonorGeneDiagnostic(
                    threeGene,
                    [
                        PlanPotentialDonorTestData.CreatePawn(),
                        PlanPotentialDonorTestData.CreatePawn(),
                        PlanPotentialDonorTestData.CreatePawn()
                    ])
            ]);
            var lookup = new Dictionary<PlanGenepackCompositionDiagnostic, IReadOnlyList<Genepack>>
            {
                { coveredComposition, CreateGenepacks(1) }
            };

            List<GeneCoverageTableRow> ascendingRows = XenogermPlannerPresentation.GetSortedGeneCoverageRows(
                [coveredDiagnostic, threeDiagnostic, zeroDiagnostic, oneDiagnostic],
                ["Unavailable"],
                lookup,
                donorAnalysis,
                new GeneCoverageSortState(GeneCoverageSortColumn.PotentialDonorCount, descending: false));

            Assert.That(ascendingRows[0].Diagnostic, Is.SameAs(zeroDiagnostic));
            Assert.That(ascendingRows[0].PotentialDonorCount, Is.EqualTo(0));
            Assert.That(ascendingRows[1].Diagnostic, Is.SameAs(oneDiagnostic));
            Assert.That(ascendingRows[1].PotentialDonorCount, Is.EqualTo(1));
            Assert.That(ascendingRows[2].Diagnostic, Is.SameAs(threeDiagnostic));
            Assert.That(ascendingRows[2].PotentialDonorCount, Is.EqualTo(3));
            Assert.That(ascendingRows[3].Diagnostic, Is.SameAs(coveredDiagnostic));
            Assert.That(ascendingRows[3].PotentialDonorCount, Is.Null);
            Assert.That(ascendingRows[4].UnresolvedGeneDefName, Is.EqualTo("Unavailable"));
            Assert.That(ascendingRows[4].PotentialDonorCount, Is.Null);

            List<GeneCoverageTableRow> descendingRows = XenogermPlannerPresentation.GetSortedGeneCoverageRows(
                [coveredDiagnostic, threeDiagnostic, zeroDiagnostic, oneDiagnostic],
                ["Unavailable"],
                lookup,
                donorAnalysis,
                new GeneCoverageSortState(GeneCoverageSortColumn.PotentialDonorCount, descending: true));

            Assert.That(descendingRows[0].Diagnostic, Is.SameAs(threeDiagnostic));
            Assert.That(descendingRows[1].Diagnostic, Is.SameAs(oneDiagnostic));
            Assert.That(descendingRows[2].Diagnostic, Is.SameAs(zeroDiagnostic));
            Assert.That(descendingRows[3].Diagnostic, Is.SameAs(coveredDiagnostic));
            Assert.That(descendingRows[4].UnresolvedGeneDefName, Is.EqualTo("Unavailable"));
        }

        [Test]
        public void GetSortedGeneCoverageRows_MissingLookupCompositionDoesNotIncreaseDisplayedCount()
        {
            GeneDef gene = CreateGene("Gene", "Gene");
            PlanGenepackCompositionDiagnostic composition = CreateComposition(gene, 3);
            var diagnostic = new PlanGeneCoverageDiagnostic(gene, PlanGeneCoverageState.Available, [composition]);

            List<GeneCoverageTableRow> rows = XenogermPlannerPresentation.GetSortedGeneCoverageRows(
                [diagnostic],
                [],
                EmptyGenepackLookup(),
                new GeneCoverageSortState(GeneCoverageSortColumn.GenepackCount, descending: false));

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0].SourceGenepackCount, Is.Zero);
        }

        [Test]
        public void GetSortedGeneCoverageRows_RepeatedCallsAreDeterministicAndEmptyInputReturnsEmptyList()
        {
            GeneDef betaGene = CreateGene("Beta", "Beta");
            GeneDef alphaGene = CreateGene("Alpha", "Alpha");
            var betaDiagnostic = new PlanGeneCoverageDiagnostic(betaGene, PlanGeneCoverageState.Missing, []);
            var alphaDiagnostic = new PlanGeneCoverageDiagnostic(alphaGene, PlanGeneCoverageState.Missing, []);
            PlanGeneCoverageDiagnostic[] diagnostics = [betaDiagnostic, alphaDiagnostic];

            List<GeneCoverageTableRow> first = XenogermPlannerPresentation.GetSortedGeneCoverageRows(
                diagnostics,
                [],
                EmptyGenepackLookup(),
                GeneCoverageSortState.Default);
            List<GeneCoverageTableRow> second = XenogermPlannerPresentation.GetSortedGeneCoverageRows(
                diagnostics,
                [],
                EmptyGenepackLookup(),
                GeneCoverageSortState.Default);
            List<GeneCoverageTableRow> empty = XenogermPlannerPresentation.GetSortedGeneCoverageRows(
                [],
                [],
                EmptyGenepackLookup(),
                GeneCoverageSortState.Default);

            Assert.That(first[0].Diagnostic, Is.SameAs(alphaDiagnostic));
            Assert.That(first[1].Diagnostic, Is.SameAs(betaDiagnostic));
            Assert.That(second[0].Diagnostic, Is.SameAs(alphaDiagnostic));
            Assert.That(second[1].Diagnostic, Is.SameAs(betaDiagnostic));
            Assert.That(empty, Is.Empty);
        }

        [Test]
        public void GetSortedPotentialDonors_OrdersByDisplayName()
        {
            Pawn beta = PlanPotentialDonorTestData.CreatePawn();
            Pawn alpha = PlanPotentialDonorTestData.CreatePawn();

            List<Pawn> sorted = XenogermPlannerPresentation.GetSortedPotentialDonors(
                [beta, alpha],
                donor => ReferenceEquals(donor, alpha) ? "Alpha" : "Beta",
                donor => ReferenceEquals(donor, alpha) ? 2 : 1);

            Assert.That(sorted, Is.EqualTo([alpha, beta]));
        }

        [Test]
        public void GetSortedPotentialDonors_UsesStableKeyAsDisplayNameTieBreaker()
        {
            Pawn later = PlanPotentialDonorTestData.CreatePawn();
            Pawn earlier = PlanPotentialDonorTestData.CreatePawn();

            List<Pawn> sorted = XenogermPlannerPresentation.GetSortedPotentialDonors(
                [later, earlier],
                _ => "Same name",
                donor => ReferenceEquals(donor, earlier) ? 10 : 20);

            Assert.That(sorted, Is.EqualTo([earlier, later]));
        }

        [Test]
        public void GetSortedPotentialDonors_DoesNotMutateInput()
        {
            Pawn first = PlanPotentialDonorTestData.CreatePawn();
            Pawn second = PlanPotentialDonorTestData.CreatePawn();
            var input = new List<Pawn> { first, second };

            List<Pawn> sorted = XenogermPlannerPresentation.GetSortedPotentialDonors(
                input,
                donor => ReferenceEquals(donor, first) ? "Zulu" : "Alpha",
                _ => 0);

            Assert.That(input, Is.EqualTo([first, second]));
            Assert.That(sorted, Is.EqualTo([second, first]));
        }

        [Test]
        public void GetSortedPotentialDonors_NullDonorThrows()
        {
            Assert.Throws<ArgumentException>(
                (Action)(() => XenogermPlannerPresentation.GetSortedPotentialDonors(
                    [null],
                    _ => string.Empty,
                    _ => 0)));
        }

        [Test]
        public void TryGetPotentialDonorDiagnostic_ReturnsMissingGeneDiagnostic()
        {
            GeneDef gene = CreateGene("Missing", "Missing");
            Pawn donor = PlanPotentialDonorTestData.CreatePawn();
            var coverageDiagnostic = new PlanGeneCoverageDiagnostic(gene, PlanGeneCoverageState.Missing, []);
            var donorDiagnostic = new PlanPotentialDonorGeneDiagnostic(gene, [donor]);
            var analysis = PlanPotentialDonorAnalysisResult.CreateAvailable([donorDiagnostic]);

            bool found = XenogermPlannerPresentation.TryGetPotentialDonorDiagnostic(
                coverageDiagnostic,
                analysis,
                out PlanPotentialDonorGeneDiagnostic resolved);

            Assert.That(found, Is.True);
            Assert.That(resolved, Is.SameAs(donorDiagnostic));
        }

        [Test]
        public void TryGetPotentialDonorDiagnostic_DoesNotReturnForCoveredGene()
        {
            GeneDef gene = CreateGene("Covered", "Covered");
            PlanGenepackCompositionDiagnostic composition = CreateComposition(gene, 1);
            var coverageDiagnostic = new PlanGeneCoverageDiagnostic(
                gene,
                PlanGeneCoverageState.Available,
                [composition]);
            var donorDiagnostic = new PlanPotentialDonorGeneDiagnostic(gene, [PlanPotentialDonorTestData.CreatePawn()]);
            var analysis = PlanPotentialDonorAnalysisResult.CreateAvailable([donorDiagnostic]);

            bool found = XenogermPlannerPresentation.TryGetPotentialDonorDiagnostic(
                coverageDiagnostic,
                analysis,
                out PlanPotentialDonorGeneDiagnostic resolved);

            Assert.That(found, Is.False);
            Assert.That(resolved, Is.Null);
        }

        [Test]
        public void TryGetPotentialDonorDiagnostic_DoesNotReturnUnavailableAnalysis()
        {
            GeneDef gene = CreateGene("Missing", "Missing");
            var coverageDiagnostic = new PlanGeneCoverageDiagnostic(gene, PlanGeneCoverageState.Missing, []);

            bool found = XenogermPlannerPresentation.TryGetPotentialDonorDiagnostic(
                coverageDiagnostic,
                PlanPotentialDonorAnalysisResult.Unavailable,
                out PlanPotentialDonorGeneDiagnostic resolved);

            Assert.That(found, Is.False);
            Assert.That(resolved, Is.Null);
        }

        [Test]
        public void TryGetPotentialDonorDiagnostic_DoesNotReturnDifferentGene()
        {
            GeneDef requestedGene = CreateGene("Requested", "Requested");
            GeneDef otherGene = CreateGene("Other", "Other");
            var coverageDiagnostic = new PlanGeneCoverageDiagnostic(requestedGene, PlanGeneCoverageState.Missing, []);
            var donorDiagnostic = new PlanPotentialDonorGeneDiagnostic(
                otherGene,
                [PlanPotentialDonorTestData.CreatePawn()]);
            var analysis = PlanPotentialDonorAnalysisResult.CreateAvailable([donorDiagnostic]);

            bool found = XenogermPlannerPresentation.TryGetPotentialDonorDiagnostic(
                coverageDiagnostic,
                analysis,
                out PlanPotentialDonorGeneDiagnostic resolved);

            Assert.That(found, Is.False);
            Assert.That(resolved, Is.Null);
        }

        [Test]
        public void GetGenesInCatalogOrder_UsesCategoryAndGeneDisplayOrder()
        {
            GeneCategoryDef highPriorityCategory = CreateCategory("HighPriority", "Zeta", 10);
            GeneCategoryDef categoryA = CreateCategory("CategoryA", "Alpha", 5);
            GeneCategoryDef categoryB = CreateCategory("CategoryB", "alpha", 5);
            GeneCategoryDef lowPriorityCategory = CreateCategory("LowPriority", "Aardvark", 0);
            GeneDef highPriorityGene = CreateCatalogGene("GeneHigh", highPriorityCategory, 100);
            GeneDef categoryATieFirst = CreateCatalogGene("GeneA", categoryA, 1);
            GeneDef categoryATieSecond = CreateCatalogGene("GeneC", categoryA, 1);
            GeneDef categoryALater = CreateCatalogGene("GeneB", categoryA, 2);
            GeneDef categoryBGene = CreateCatalogGene("GeneCategoryB", categoryB, 0);
            GeneDef lowPriorityGene = CreateCatalogGene("GeneLow", lowPriorityCategory, 0);

            List<GeneDef> sortedGenes = XenogermPlannerPresentation.GetGenesInCatalogOrder(
            [
                lowPriorityGene,
                categoryBGene,
                categoryALater,
                categoryATieSecond,
                highPriorityGene,
                categoryATieFirst
            ]);

            Assert.That(
                sortedGenes,
                Is.EqualTo(
                [
                    highPriorityGene,
                    categoryATieFirst,
                    categoryATieSecond,
                    categoryALater,
                    categoryBGene,
                    lowPriorityGene
                ]));
        }

        [TestCase(4, "+4")]
        [TestCase(0, "0")]
        [TestCase(-4, "-4")]
        public void FormatMetabolism_UsesSignedPositiveValues(int metabolism, string expected)
        {
            Assert.That(XenogermPlannerPresentation.FormatMetabolism(metabolism), Is.EqualTo(expected));
        }

        [TestCase(2f, "x200%")]
        [TestCase(1f, "x100%")]
        [TestCase(0.75f, "x75%")]
        public void FormatHungerRateFactor_UsesVanillaStylePercentage(float factor, string expected)
        {
            Assert.That(XenogermPlannerPresentation.FormatHungerRateFactor(factor), Is.EqualTo(expected));
        }

        [TestCase(PlanReadinessStatus.Unavailable, "XenogermPlanner.Template.Disabled.NoActiveMap")]
        [TestCase(PlanReadinessStatus.EmptyTarget, "XenogermPlanner.Template.Disabled.EmptyTarget")]
        [TestCase(PlanReadinessStatus.Degraded, "XenogermPlanner.Template.Disabled.Degraded")]
        [TestCase(PlanReadinessStatus.NotReady, "XenogermPlanner.Template.Disabled.NotReady")]
        public void GetTemplateCreationDisabledTranslationKey_MapsNonReadyStatus(
            PlanReadinessStatus status,
            string expectedTranslationKey)
        {
            PlanReadinessResult result = CreateResult(status);

            Assert.That(
                XenogermPlannerPresentation.GetTemplateCreationDisabledTranslationKey(result),
                Is.EqualTo(expectedTranslationKey));
        }

        [Test]
        public void GetTemplateCreationDisabledTranslationKey_ReturnsNullForReadyPlan()
        {
            Assert.That(
                XenogermPlannerPresentation.GetTemplateCreationDisabledTranslationKey(
                    PlanReadinessResult.CreateReady([])),
                Is.Null);
        }

        [Test]
        public void GetTemplateCreationDisabledTranslationKey_UsesExactConflictReason()
        {
            var result = PlanReadinessResult.CreateNotReady([], [], hasExactPayloadConflict: true);

            Assert.That(
                XenogermPlannerPresentation.GetTemplateCreationDisabledTranslationKey(result),
                Is.EqualTo("XenogermPlanner.Template.Disabled.ExactPayloadConflict"));
        }

        [Test]
        public void GetTemplateCandidateSummaryTranslationKey_ReturnsTemplateSummaryKey()
        {
            Assert.That(
                XenogermPlannerPresentation.GetTemplateCandidateSummaryTranslationKey(),
                Is.EqualTo("XenogermPlanner.Template.CandidateSummary"));
        }

        [TestCase(true, "XenogermPlanner.Template.AutomaticCandidate")]
        [TestCase(false, "XenogermPlanner.Template.AlternativeCandidate")]
        public void GetTemplateCandidateLabelTranslationKey_MapsCandidateKind(
            bool automatic,
            string expectedTranslationKey)
        {
            Assert.That(
                XenogermPlannerPresentation.GetTemplateCandidateLabelTranslationKey(automatic),
                Is.EqualTo(expectedTranslationKey));
        }

        [Test]
        public void GetTemplateSaveFailureTranslationKey_MapsFailure()
        {
            Assert.That(
                XenogermPlannerPresentation.GetTemplateSaveFailureTranslationKey(
                    PlanXenogermTemplateSaveFailure.InventoryUnavailable),
                Is.EqualTo("XenogermPlanner.Template.SaveFailure.InventoryUnavailable"));

            Assert.That(
                XenogermPlannerPresentation.GetTemplateSaveFailureTranslationKey(
                    PlanXenogermTemplateSaveFailure.CandidateInvalidForPlan),
                Is.EqualTo("XenogermPlanner.Template.SaveFailure.CandidateInvalidForPlan"));

            Assert.That(
                XenogermPlannerPresentation.GetTemplateSaveFailureTranslationKey(
                    PlanXenogermTemplateSaveFailure.CompositionUnavailable),
                Is.EqualTo("XenogermPlanner.Template.SaveFailure.CompositionUnavailable"));

            Assert.That(
                XenogermPlannerPresentation.GetTemplateSaveFailureTranslationKey(
                    PlanXenogermTemplateSaveFailure.VanillaRejected),
                Is.EqualTo("XenogermPlanner.Template.SaveFailure.VanillaRejected"));
        }


        [Test]
        public void GetReadinessReadyNotificationTranslationKey_ReturnsNotificationKey()
        {
            Assert.That(
                XenogermPlannerPresentation.GetReadinessReadyNotificationTranslationKey(),
                Is.EqualTo("XenogermPlanner.Notifications.PlanReady"));
        }

        [Test]
        public void GetTraderAdvisoryNotificationTranslationKey_ReturnsNotificationKey()
        {
            Assert.That(
                XenogermPlannerPresentation.GetTraderAdvisoryNotificationTranslationKey(),
                Is.EqualTo("XenogermPlanner.Notifications.TraderRelevantOffers"));
        }

        [Test]
        public void GetTraderAdvisorySourceFallbackTranslationKey_OrbitalUsesOrbitalTraderKey()
        {
            Assert.That(
                XenogermPlannerPresentation.GetTraderAdvisorySourceFallbackTranslationKey(
                    PlanTraderAdvisorySourceKind.Orbital),
                Is.EqualTo("XenogermPlanner.Notifications.OrbitalTrader"));
        }

        [Test]
        public void GetTraderAdvisorySourceFallbackTranslationKey_CaravanUsesVisitingTraderKey()
        {
            Assert.That(
                XenogermPlannerPresentation.GetTraderAdvisorySourceFallbackTranslationKey(
                    PlanTraderAdvisorySourceKind.Caravan),
                Is.EqualTo("XenogermPlanner.Notifications.VisitingTrader"));
        }

        [Test]
        public void GetTraderAdvisoryDisplayName_UsesCurrentNativeNameWhenAvailable()
        {
            string displayName = XenogermPlannerPresentation.GetTraderAdvisoryDisplayName(
                PlanTraderAdvisorySourceKind.Orbital,
                "Current trader name");

            Assert.That(displayName, Is.EqualTo("Current trader name"));
        }

        [Test]
        public void GetTraderAdvisoryAffectedPlans_DeduplicatesByStableIdAndOrdersByNameThenId()
        {
            GeneDef gene = CreateGene("Gene", "Gene");
            XenogermPlan alphaSecond = CreatePlanWithId("id-b", "alpha", gene);
            XenogermPlan alphaFirst = CreatePlanWithId("id-a", "Alpha", gene);
            XenogermPlan beta = CreatePlanWithId("id-c", "Beta", gene);
            PlanTraderAdvisoryOfferSnapshot firstOffer = CreateTraderOffer(gene);
            PlanTraderAdvisoryOfferSnapshot secondOffer = CreateTraderOffer(gene);
            PlanTraderAdvisoryNotification notification = CreateTraderNotification(
                new PlanTraderAdvisoryNotificationOffer(firstOffer, [alphaSecond, alphaFirst]),
                new PlanTraderAdvisoryNotificationOffer(secondOffer, [alphaSecond, beta]));

            List<XenogermPlan> affectedPlans = XenogermPlannerPresentation.GetTraderAdvisoryAffectedPlans(notification);

            Assert.That(affectedPlans, Is.EqualTo([alphaFirst, alphaSecond, beta]));
            Assert.That(notification.OfferCount, Is.EqualTo(2));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void GetFilteredPlans_EmptyQueryReturnsAllPlansInOriginalOrder(string query)
        {
            var first = new XenogermPlan("First", [], PlanReadinessMode.Coverage);
            var second = new XenogermPlan("Second", [], PlanReadinessMode.Coverage);
            XenogermPlan[] plans = [first, second];

            List<XenogermPlan> filtered = XenogermPlannerPresentation.GetFilteredPlans(plans, query);

            Assert.That(filtered, Is.EqualTo(plans));
        }

        [Test]
        public void GetFilteredPlans_PartialQueryMatchesDisplayNameIgnoringCaseAndOuterWhitespace()
        {
            var matching = new XenogermPlan("Underslave research", [], PlanReadinessMode.Coverage);
            var other = new XenogermPlan("Worker", [], PlanReadinessMode.Coverage);

            List<XenogermPlan> filtered = XenogermPlannerPresentation.GetFilteredPlans([other, matching], "  SLAVE  ");

            Assert.That(filtered, Is.EqualTo([matching]));
        }

        [Test]
        public void GetFilteredPlans_MultipleMatchesPreserveOrderAndDuplicateIdentities()
        {
            var first = new XenogermPlan("Alpha", [], PlanReadinessMode.Coverage);
            var second = new XenogermPlan("Alpha", [], PlanReadinessMode.ExactPayload);
            var third = new XenogermPlan("Alphabet", [], PlanReadinessMode.Coverage);

            List<XenogermPlan> filtered = XenogermPlannerPresentation.GetFilteredPlans([first, second, third], "alpha");

            Assert.That(filtered, Is.EqualTo([first, second, third]));
            Assert.That(filtered[0].Id, Is.Not.EqualTo(filtered[1].Id));
        }

        [Test]
        public void GetFilteredPlans_NoMatchReturnsEmptyList()
        {
            var plan = new XenogermPlan("Alpha", [], PlanReadinessMode.Coverage);

            List<XenogermPlan> filtered = XenogermPlannerPresentation.GetFilteredPlans([plan], "Beta");

            Assert.That(filtered, Is.Empty);
        }

        [Test]
        public void GetFilteredPlans_DoesNotChangePlanIdentityOrState()
        {
            GeneDef gene = CreateGene("Gene", "Gene");
            var plan = new XenogermPlan(
                "stable-id",
                "Tracked plan",
                [gene],
                [],
                PlanReadinessMode.ExactPayload,
                readinessNotificationsEnabled: false,
                hasReadinessNotificationBaseline: true,
                lastReadinessNotificationStateWasReady: true);

            List<XenogermPlan> filtered = XenogermPlannerPresentation.GetFilteredPlans([plan], "tracked");

            Assert.That(filtered, Is.EqualTo([plan]));
            Assert.That(plan.Id, Is.EqualTo("stable-id"));
            Assert.That(plan.Name, Is.EqualTo("Tracked plan"));
            Assert.That(plan.ReadinessMode, Is.EqualTo(PlanReadinessMode.ExactPayload));
            Assert.That(plan.ReadinessNotificationsEnabled, Is.False);
            Assert.That(plan.HasReadinessNotificationBaseline, Is.True);
            Assert.That(plan.LastReadinessNotificationStateWasReady, Is.True);
            Assert.That(plan.DesiredGenes, Is.EquivalentTo([gene]));
        }

        [Test]
        public void GetPlanDisplayName_NamedPlanReturnsCurrentNameWithoutChangingState()
        {
            var plan = new XenogermPlan("Named plan", [], PlanReadinessMode.Coverage);
            string id = plan.Id;

            string displayName = XenogermPlannerPresentation.GetPlanDisplayName(plan);

            Assert.That(displayName, Is.EqualTo("Named plan"));
            Assert.That(plan.Id, Is.EqualTo(id));
            Assert.That(plan.Name, Is.EqualTo("Named plan"));
            Assert.That(plan.HasReadinessNotificationBaseline, Is.False);
        }

        private static PlanTraderAdvisoryNotification CreateTraderNotification(
            params PlanTraderAdvisoryNotificationOffer[] notificationOffers)
        {
            var sourceOffers = new List<PlanTraderAdvisoryOfferSnapshot>(notificationOffers.Length);

            foreach (PlanTraderAdvisoryNotificationOffer notificationOffer in notificationOffers)
                sourceOffers.Add(notificationOffer.Offer);

            var source = new PlanTraderAdvisorySourceSnapshot(
                CreateUninitialized<TradeShip>(),
                PlanTraderAdvisorySourceKind.Orbital,
                null,
                sourceOffers);

            return new PlanTraderAdvisoryNotification(source, notificationOffers);
        }

        private static PlanTraderAdvisoryOfferSnapshot CreateTraderOffer(params GeneDef[] genes)
        {
            return new PlanTraderAdvisoryOfferSnapshot(CreateUninitialized<Genepack>(), genes);
        }

        private static XenogermPlan CreatePlanWithId(string id, string name, params GeneDef[] genes)
        {
            return new XenogermPlan(id, name, genes, [], PlanReadinessMode.Coverage);
        }

        private static T CreateUninitialized<T>() where T : class
        {
            return (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        }

        private static PlanGenepackCompositionDiagnostic CreateExactPayloadConflictComposition(
            GeneDef gene,
            GeneDef additionalGene,
            int physicalPackCount)
        {
            return new PlanGenepackCompositionDiagnostic(
                [gene, additionalGene],
                physicalPackCount,
                isExactPayloadEligible: false,
                additionalGenes: [additionalGene]);
        }

        private static PlanGenepackCompositionDiagnostic CreateComposition(GeneDef gene, int physicalPackCount)
        {
            return new PlanGenepackCompositionDiagnostic([gene], physicalPackCount, true, []);
        }

        private static IReadOnlyList<Genepack> CreateGenepacks(int count)
        {
            var genepacks = new List<Genepack>(count);

            for (var index = 0; index < count; index++)
                genepacks.Add(GenepackInventoryTestData.CreateGenepack());

            return genepacks.AsReadOnly();
        }

        private static IReadOnlyDictionary<PlanGenepackCompositionDiagnostic, IReadOnlyList<Genepack>>
            EmptyGenepackLookup()
        {
            return new Dictionary<PlanGenepackCompositionDiagnostic, IReadOnlyList<Genepack>>();
        }

        private static GeneDef CreateGene(string defName, string label)
        {
            return new GeneDef
            {
                defName = defName,
                label = label
            };
        }

        private static GeneCategoryDef CreateCategory(string defName, string label, int displayPriorityInXenotype)
        {
            return new GeneCategoryDef
            {
                defName = defName,
                label = label,
                displayPriorityInXenotype = displayPriorityInXenotype
            };
        }

        private static GeneDef CreateCatalogGene(
            string defName,
            GeneCategoryDef displayCategory,
            int displayOrderInCategory)
        {
            return new GeneDef
            {
                defName = defName,
                displayCategory = displayCategory,
                displayOrderInCategory = displayOrderInCategory
            };
        }

        private static PlanReadinessResult CreateResult(PlanReadinessStatus status)
        {
            switch (status)
            {
                case PlanReadinessStatus.Ready:
                    return PlanReadinessResult.CreateReady([]);

                case PlanReadinessStatus.NotReady:
                    return PlanReadinessResult.CreateNotReady([], [], false);

                case PlanReadinessStatus.EmptyTarget:
                    return PlanReadinessResult.CreateEmptyTarget();

                case PlanReadinessStatus.Degraded:
                    return PlanReadinessResult.CreateDegraded([], []);

                case PlanReadinessStatus.Unavailable:
                    return PlanReadinessResult.CreateUnavailable(PlanReadinessUnavailableReason.NoActiveMap);

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported readiness status.");
            }
        }
    }
}