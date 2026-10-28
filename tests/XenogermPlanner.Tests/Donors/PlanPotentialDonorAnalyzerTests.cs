using System;
using System.Collections.Generic;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Donors;

namespace XenogermPlanner.Tests.Donors
{
    [TestFixture]
    public sealed class PlanPotentialDonorAnalyzerTests
    {
        [Test]
        public void Analyze_GeneAbsentFromPawnProducesNoDonor()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested");
            GeneDef otherGene = PlanPotentialDonorTestData.CreateGene("Other");
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, otherGene));

            AssertDiagnostic(result, requestedGene, []);
        }

        [Test]
        public void Analyze_DirectlySelectableGeneProducesDonor()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested");
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, requestedGene));

            AssertDiagnostic(result, requestedGene, [pawn]);
        }

        [Test]
        public void Analyze_ReturnsEverySuitableExactPawnReference()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested");
            Pawn firstPawn = PlanPotentialDonorTestData.CreatePawn();
            Pawn secondPawn = PlanPotentialDonorTestData.CreatePawn();
            Dictionary<Pawn, IEnumerable<GeneDef>> genesByPawn = PlanPotentialDonorTestData.CreatePawnGeneMap();
            genesByPawn.Add(firstPawn, [requestedGene]);
            genesByPawn.Add(secondPawn, [requestedGene]);

            PlanPotentialDonorAnalysisResult result = Analyze([requestedGene], [firstPawn, secondPawn], genesByPawn);

            AssertDiagnostic(result, requestedGene, [firstPawn, secondPawn]);
        }

        [Test]
        public void Analyze_RequestedGeneWithNoDonorsStillProducesDiagnostic()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested");

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [],
                PlanPotentialDonorTestData.CreatePawnGeneMap());

            Assert.That(result.GeneDiagnostics, Has.Count.EqualTo(1));
            AssertDiagnostic(result, requestedGene, []);
        }

        [Test]
        public void Analyze_NormalizesDuplicateRequestedGenesWithoutMutatingInput()
        {
            GeneDef firstGene = PlanPotentialDonorTestData.CreateGene("A");
            GeneDef secondGene = PlanPotentialDonorTestData.CreateGene("B");
            var requestedGenes = new List<GeneDef> { secondGene, firstGene, firstGene };

            PlanPotentialDonorAnalysisResult result = Analyze(
                requestedGenes,
                [],
                PlanPotentialDonorTestData.CreatePawnGeneMap());

            Assert.That(result.GeneDiagnostics, Has.Count.EqualTo(2));
            Assert.That(result.GeneDiagnostics[0].Gene, Is.SameAs(firstGene));
            Assert.That(result.GeneDiagnostics[1].Gene, Is.SameAs(secondGene));
            Assert.That(requestedGenes, Is.EqualTo([secondGene, firstGene, firstGene]));
        }

        [Test]
        public void Analyze_DuplicatePawnGeneInstancesDoNotChangeResult()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested");
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, requestedGene, requestedGene));

            AssertDiagnostic(result, requestedGene, [pawn]);
        }

        [Test]
        public void Analyze_NullPawnGeneCollectionIsTreatedAsEmpty()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested");
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();
            Dictionary<Pawn, IEnumerable<GeneDef>> genesByPawn = PlanPotentialDonorTestData.CreatePawnGeneMap();
            genesByPawn.Add(pawn, null);

            PlanPotentialDonorAnalysisResult result = Analyze([requestedGene], [pawn], genesByPawn);

            AssertDiagnostic(result, requestedGene, []);
        }

        [Test]
        public void Analyze_NullPawnGeneEntriesAreIgnored()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested");
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, null, requestedGene));

            AssertDiagnostic(result, requestedGene, [pawn]);
        }

        [Test]
        public void Analyze_ArchiteTargetProducesNoDonor()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested", archites: 1);
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, requestedGene));

            AssertDiagnostic(result, requestedGene, []);
        }

        [Test]
        public void Analyze_ArchiteHelperCannotEnableTarget()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested", metabolism: 6);
            GeneDef architeHelper = PlanPotentialDonorTestData.CreateGene("ArchiteHelper", metabolism: -1, archites: 1);
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, requestedGene, architeHelper));

            AssertDiagnostic(result, requestedGene, []);
        }

        [Test]
        public void Analyze_MelaninTargetProducesNoDonor()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested", isMelanin: true);
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, requestedGene));

            AssertDiagnostic(result, requestedGene, []);
        }

        [Test]
        public void Analyze_MelaninHelperCannotEnableTarget()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested", metabolism: 6);
            GeneDef melaninHelper = PlanPotentialDonorTestData.CreateGene(
                "MelaninHelper",
                metabolism: -1,
                isMelanin: true);
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, requestedGene, melaninHelper));

            AssertDiagnostic(result, requestedGene, []);
        }

        [Test]
        public void Analyze_PositiveSixTargetWithoutCompensationProducesNoDonor()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested", metabolism: 6);
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, requestedGene));

            AssertDiagnostic(result, requestedGene, []);
        }

        [Test]
        public void Analyze_PositiveSixTargetAfterNegativeHelperProducesDonor()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested", metabolism: 6);
            GeneDef helperGene = PlanPotentialDonorTestData.CreateGene("Helper", metabolism: -1);
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, requestedGene, helperGene));

            AssertDiagnostic(result, requestedGene, [pawn]);
        }

        [Test]
        public void Analyze_NegativeSixTargetWithoutCompensationProducesNoDonor()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested", metabolism: -6);
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, requestedGene));

            AssertDiagnostic(result, requestedGene, []);
        }

        [Test]
        public void Analyze_NegativeSixTargetAfterPositiveHelperProducesDonor()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested", metabolism: -6);
            GeneDef helperGene = PlanPotentialDonorTestData.CreateGene("Helper", metabolism: 1);
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, requestedGene, helperGene));

            AssertDiagnostic(result, requestedGene, [pawn]);
        }

        [Test]
        public void Analyze_FinalValidSumWithNoValidIntermediateOrderProducesNoDonor()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested", metabolism: 6);
            GeneDef positiveHelper = PlanPotentialDonorTestData.CreateGene("Positive", metabolism: 6);
            GeneDef negativeHelper = PlanPotentialDonorTestData.CreateGene("Negative", metabolism: -7);
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, requestedGene, positiveHelper, negativeHelper));

            AssertDiagnostic(result, requestedGene, []);
        }

        [Test]
        public void Analyze_BacktracksToAlternativeValidHelperOrder()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested", metabolism: 6);
            GeneDef firstTriedHelper = PlanPotentialDonorTestData.CreateGene("A_Positive", metabolism: 5);
            GeneDef validHelper = PlanPotentialDonorTestData.CreateGene("Z_Negative", metabolism: -5);
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, requestedGene, firstTriedHelper, validHelper));

            AssertDiagnostic(result, requestedGene, [pawn]);
        }

        [Test]
        public void Analyze_SequenceRequiringMoreThanFourGenesProducesNoDonor()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested", metabolism: 9);
            GeneDef firstHelper = PlanPotentialDonorTestData.CreateGene("H1", metabolism: -1);
            GeneDef secondHelper = PlanPotentialDonorTestData.CreateGene("H2", metabolism: -1);
            GeneDef thirdHelper = PlanPotentialDonorTestData.CreateGene("H3", metabolism: -1);
            GeneDef fourthHelper = PlanPotentialDonorTestData.CreateGene("H4", metabolism: -1);
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(
                    pawn,
                    requestedGene,
                    firstHelper,
                    secondHelper,
                    thirdHelper,
                    fourthHelper));

            AssertDiagnostic(result, requestedGene, []);
        }

        [Test]
        public void Analyze_TargetCanParticipateAtSecondSelectionStep()
        {
            AssertTargetStep(6, [-1]);
        }

        [Test]
        public void Analyze_TargetCanParticipateAtThirdSelectionStep()
        {
            AssertTargetStep(7, [-1, -1]);
        }

        [Test]
        public void Analyze_TargetCanParticipateAtFourthSelectionStep()
        {
            AssertTargetStep(8, [-1, -1, -1]);
        }

        [Test]
        public void Analyze_PassOnDirectlyFalseDoesNotBlockParticipation()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested", passOnDirectly: false);
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, requestedGene));

            AssertDiagnostic(result, requestedGene, [pawn]);
        }

        [Test]
        public void Analyze_CanGenerateInGeneSetFalseDoesNotBlockParticipation()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested", canGenerateInGeneSet: false);
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, requestedGene));

            AssertDiagnostic(result, requestedGene, [pawn]);
        }

        [Test]
        public void Analyze_MissingPrerequisiteDoesNotBlockParticipation()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested");
            requestedGene.prerequisite = PlanPotentialDonorTestData.CreateGene("Prerequisite");
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, requestedGene));

            AssertDiagnostic(result, requestedGene, [pawn]);
        }

        [Test]
        public void Analyze_ConflictingGeneDoesNotBlockParticipation()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested");
            GeneDef conflictingGene = PlanPotentialDonorTestData.CreateGene("Conflicting");
            requestedGene.exclusionTags = ["Conflict"];
            conflictingGene.exclusionTags = ["Conflict"];
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, requestedGene, conflictingGene));

            AssertDiagnostic(result, requestedGene, [pawn]);
        }

        [Test]
        public void Analyze_DoesNotApplyGeneLayerFilteringToSuppliedGenes()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested");
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();
            var readerCalls = 0;

            PlanPotentialDonorAnalysisResult result = PlanPotentialDonorAnalyzer.Analyze(
                [requestedGene],
                PlanPotentialDonorTestData.CreateScope(pawn),
                candidate =>
                {
                    Assert.That(candidate, Is.SameAs(pawn));
                    readerCalls++;
                    return [requestedGene];
                });

            Assert.That(readerCalls, Is.EqualTo(1));
            AssertDiagnostic(result, requestedGene, [pawn]);
        }

        [Test]
        public void Analyze_DoesNotApplyActiveOrOverriddenFilteringToSuppliedGenes()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested");
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();

            PlanPotentialDonorAnalysisResult result = PlanPotentialDonorAnalyzer.Analyze(
                [requestedGene],
                PlanPotentialDonorTestData.CreateScope(pawn),
                _ => [requestedGene]);

            AssertDiagnostic(result, requestedGene, [pawn]);
        }

        [Test]
        public void Analyze_UnavailableScopeReturnsUnavailableResultWithoutReadingPawns()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested");
            var readerCalled = false;

            PlanPotentialDonorAnalysisResult result = PlanPotentialDonorAnalyzer.Analyze(
                [requestedGene],
                PlanPotentialDonorScopeSnapshot.Unavailable,
                _ =>
                {
                    readerCalled = true;
                    return [requestedGene];
                });

            Assert.That(result.IsAvailable, Is.False);
            Assert.That(result.GeneDiagnostics, Is.Empty);
            Assert.That(readerCalled, Is.False);
        }

        [Test]
        public void Analyze_AvailableEmptyScopeReturnsAvailableZeroDonorDiagnostics()
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested");

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [],
                PlanPotentialDonorTestData.CreatePawnGeneMap());

            Assert.That(result.IsAvailable, Is.True);
            AssertDiagnostic(result, requestedGene, []);
        }

        [Test]
        public void Analyze_OrdersDiagnosticsByGeneDefName()
        {
            GeneDef geneA = PlanPotentialDonorTestData.CreateGene("A");
            GeneDef geneB = PlanPotentialDonorTestData.CreateGene("B");
            GeneDef geneC = PlanPotentialDonorTestData.CreateGene("C");

            PlanPotentialDonorAnalysisResult result = Analyze(
                [geneC, geneA, geneB],
                [],
                PlanPotentialDonorTestData.CreatePawnGeneMap());

            Assert.That(result.GeneDiagnostics[0].Gene, Is.SameAs(geneA));
            Assert.That(result.GeneDiagnostics[1].Gene, Is.SameAs(geneB));
            Assert.That(result.GeneDiagnostics[2].Gene, Is.SameAs(geneC));
        }

        [Test]
        public void Diagnostic_CopiesAndDeduplicatesDonorCollection()
        {
            GeneDef gene = PlanPotentialDonorTestData.CreateGene("Gene");
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();
            var donors = new List<Pawn> { pawn, pawn };

            var diagnostic = new PlanPotentialDonorGeneDiagnostic(gene, donors);
            donors.Clear();

            Assert.That(diagnostic.Gene, Is.SameAs(gene));
            Assert.That(diagnostic.DonorCount, Is.EqualTo(1));
            Assert.That(diagnostic.HasDonors, Is.True);
            Assert.That(diagnostic.Donors[0], Is.SameAs(pawn));
        }

        [Test]
        public void Result_CopiesDiagnosticsAndSupportsGeneLookup()
        {
            GeneDef gene = PlanPotentialDonorTestData.CreateGene("Gene");
            var diagnostic = new PlanPotentialDonorGeneDiagnostic(gene, []);
            var diagnostics = new List<PlanPotentialDonorGeneDiagnostic> { diagnostic };

            var result = PlanPotentialDonorAnalysisResult.CreateAvailable(diagnostics);
            diagnostics.Clear();

            bool found = result.TryGetDiagnostic(gene, out PlanPotentialDonorGeneDiagnostic resolved);

            Assert.That(result.GeneDiagnostics, Has.Count.EqualTo(1));
            Assert.That(found, Is.True);
            Assert.That(resolved, Is.SameAs(diagnostic));
        }

        [Test]
        public void Result_RejectsDuplicateGeneDiagnostics()
        {
            GeneDef gene = PlanPotentialDonorTestData.CreateGene("Gene");
            var first = new PlanPotentialDonorGeneDiagnostic(gene, []);
            var second = new PlanPotentialDonorGeneDiagnostic(gene, []);

            Assert.Throws<ArgumentException>(
                (Action)(() => PlanPotentialDonorAnalysisResult.CreateAvailable([first, second])));
        }

        [Test]
        public void Analyze_NullRequestedGeneCollectionThrows()
        {
            Assert.Throws<ArgumentNullException>(
                (Action)(() => PlanPotentialDonorAnalyzer.Analyze(
                    null,
                    PlanPotentialDonorTestData.CreateScope(),
                    _ => [])));
        }

        [Test]
        public void Analyze_NullRequestedGeneThrows()
        {
            Assert.Throws<ArgumentException>(
                (Action)(() => PlanPotentialDonorAnalyzer.Analyze(
                    [null],
                    PlanPotentialDonorTestData.CreateScope(),
                    _ => [])));
        }

        [Test]
        public void Analyze_NullScopeThrows()
        {
            Assert.Throws<ArgumentNullException>((Action)(() => PlanPotentialDonorAnalyzer.Analyze([], null, _ => [])));
        }

        [Test]
        public void Analyze_NullPawnGeneReaderThrows()
        {
            Assert.Throws<ArgumentNullException>(
                (Action)(() => PlanPotentialDonorAnalyzer.Analyze([], PlanPotentialDonorTestData.CreateScope(), null)));
        }

        [Test]
        public void CanParticipateInExtraction_NullTargetThrows()
        {
            Assert.Throws<ArgumentNullException>(
                (Action)(() => PlanPotentialDonorAnalyzer.CanParticipateInExtraction(null, [])));
        }

        [Test]
        public void Diagnostic_NullGeneThrows()
        {
            Assert.Throws<ArgumentNullException>(
                (Action)(() => { _ = new PlanPotentialDonorGeneDiagnostic(null, []); }));
        }

        [Test]
        public void Diagnostic_NullDonorCollectionThrows()
        {
            GeneDef gene = PlanPotentialDonorTestData.CreateGene("Gene");

            Assert.Throws<ArgumentNullException>(
                (Action)(() => { _ = new PlanPotentialDonorGeneDiagnostic(gene, null); }));
        }

        [Test]
        public void Diagnostic_NullDonorThrows()
        {
            GeneDef gene = PlanPotentialDonorTestData.CreateGene("Gene");

            Assert.Throws<ArgumentException>(
                (Action)(() => { _ = new PlanPotentialDonorGeneDiagnostic(gene, [null]); }));
        }

        [Test]
        public void Result_NullDiagnosticCollectionThrows()
        {
            Assert.Throws<ArgumentNullException>(
                (Action)(() => PlanPotentialDonorAnalysisResult.CreateAvailable(null)));
        }

        [Test]
        public void Result_NullDiagnosticThrows()
        {
            Assert.Throws<ArgumentException>((Action)(() => PlanPotentialDonorAnalysisResult.CreateAvailable([null])));
        }

        [Test]
        public void Result_TryGetDiagnosticNullGeneThrows()
        {
            var result = PlanPotentialDonorAnalysisResult.CreateAvailable([]);

            Assert.Throws<ArgumentNullException>((Action)(() => result.TryGetDiagnostic(null, out _)));
        }

        private static PlanPotentialDonorAnalysisResult Analyze(
            IEnumerable<GeneDef> requestedGenes,
            IEnumerable<Pawn> pawns,
            IReadOnlyDictionary<Pawn, IEnumerable<GeneDef>> genesByPawn)
        {
            return PlanPotentialDonorTestData.Analyze(
                requestedGenes,
                PlanPotentialDonorScopeSnapshot.CreateAvailable(pawns),
                genesByPawn);
        }

        private static void AssertTargetStep(int targetMetabolism, IReadOnlyList<int> helperMetabolisms)
        {
            GeneDef requestedGene = PlanPotentialDonorTestData.CreateGene("Requested", metabolism: targetMetabolism);
            Pawn pawn = PlanPotentialDonorTestData.CreatePawn();
            var pawnGenes = new List<GeneDef> { requestedGene };

            for (var index = 0; index < helperMetabolisms.Count; index++)
            {
                pawnGenes.Add(
                    PlanPotentialDonorTestData.CreateGene("Helper" + index, metabolism: helperMetabolisms[index]));
            }

            PlanPotentialDonorAnalysisResult result = Analyze(
                [requestedGene],
                [pawn],
                PlanPotentialDonorTestData.CreatePawnGeneMap(pawn, pawnGenes.ToArray()));

            AssertDiagnostic(result, requestedGene, [pawn]);
        }

        private static void AssertDiagnostic(
            PlanPotentialDonorAnalysisResult result,
            GeneDef gene,
            IReadOnlyList<Pawn> expectedDonors)
        {
            Assert.That(result.IsAvailable, Is.True);
            Assert.That(result.TryGetDiagnostic(gene, out PlanPotentialDonorGeneDiagnostic diagnostic), Is.True);
            Assert.That(diagnostic.Gene, Is.SameAs(gene));
            Assert.That(diagnostic.DonorCount, Is.EqualTo(expectedDonors.Count));
            Assert.That(diagnostic.HasDonors, Is.EqualTo(expectedDonors.Count > 0));
            Assert.That(diagnostic.Donors, Is.EqualTo(expectedDonors));
        }
    }
}