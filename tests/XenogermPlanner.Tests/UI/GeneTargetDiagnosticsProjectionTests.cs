using System;
using System.Collections.Generic;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Plans;
using XenogermPlanner.Tests.Plans;
using XenogermPlanner.UI;

namespace XenogermPlanner.Tests.UI
{
    [TestFixture]
    public sealed class GeneTargetDiagnosticsProjectionTests
    {
        [Test]
        public void Build_ProducesPreparedRowsInSegmentOrder()
        {
            GeneDef conflictA = PlanTestData.CreateGene("ConflictA");
            GeneDef conflictB = PlanTestData.CreateGene("ConflictB");
            GeneDef randomA = PlanTestData.CreateGene("RandomA");
            GeneDef randomB = PlanTestData.CreateGene("RandomB");
            GeneDef dependent = PlanTestData.CreateGene("Dependent");
            GeneDef prerequisite = PlanTestData.CreateGene("Prerequisite");
            var conflict = new PlanGeneConflictDiagnostic(
                conflictA,
                conflictB,
                PlanGeneConflictKind.Ordinary,
                conflictA,
                conflictB);
            var randomGroup = new PlanGeneRandomChoiceGroupDiagnostic([randomA, randomB]);
            var prerequisiteDiagnostic = new PlanGenePrerequisiteDiagnostic(dependent, prerequisite);
            var analysis = new PlanGeneTargetAnalysisResult([conflict], [randomGroup], [prerequisiteDiagnostic]);

            GeneTargetDiagnosticsProjection projection = Build(
                analysis,
                PlanReadinessMode.Coverage,
                genes =>
                {
                    var sorted = new List<GeneDef>(genes);
                    sorted.Reverse();
                    return sorted;
                });

            Assert.That(projection.Rows, Has.Count.EqualTo(3));

            GeneTargetDiagnosticPresentationRow conflictRow = projection.Rows[0];
            Assert.That(conflictRow.Kind, Is.EqualTo(GeneTargetDiagnosticPresentationRowKind.Conflict));
            Assert.That(conflictRow.ConflictDiagnostic, Is.SameAs(conflict));
            Assert.That(conflictRow.FirstGene, Is.SameAs(conflictA));
            Assert.That(conflictRow.SecondGene, Is.SameAs(conflictB));
            Assert.That(conflictRow.Message, Is.EqualTo("conflict:ConflictA"));

            GeneTargetDiagnosticPresentationRow randomRow = projection.Rows[1];
            Assert.That(randomRow.Kind, Is.EqualTo(GeneTargetDiagnosticPresentationRowKind.RandomChoiceGroup));
            Assert.That(randomRow.RandomChoiceGroupDiagnostic, Is.SameAs(randomGroup));
            Assert.That(randomRow.Genes, Is.EqualTo([randomB, randomA]));
            Assert.That(randomRow.Message, Is.EqualTo("random:RandomA"));

            GeneTargetDiagnosticPresentationRow prerequisiteRow = projection.Rows[2];
            Assert.That(prerequisiteRow.Kind, Is.EqualTo(GeneTargetDiagnosticPresentationRowKind.Prerequisite));
            Assert.That(prerequisiteRow.PrerequisiteDiagnostic, Is.SameAs(prerequisiteDiagnostic));
            Assert.That(prerequisiteRow.FirstGene, Is.SameAs(dependent));
            Assert.That(prerequisiteRow.SecondGene, Is.SameAs(prerequisite));
            Assert.That(prerequisiteRow.Message, Is.EqualTo("prerequisite:Coverage"));
        }

        [Test]
        public void Build_ReadinessModeCanChangePreparedPrerequisiteMessage()
        {
            GeneDef dependent = PlanTestData.CreateGene("Dependent");
            GeneDef prerequisite = PlanTestData.CreateGene("Prerequisite");
            var analysis = new PlanGeneTargetAnalysisResult(
                [],
                [],
                [new PlanGenePrerequisiteDiagnostic(dependent, prerequisite)]);

            GeneTargetDiagnosticsProjection coverage = Build(analysis, PlanReadinessMode.Coverage, genes => [..genes]);
            GeneTargetDiagnosticsProjection exact = Build(analysis, PlanReadinessMode.ExactPayload, genes => [..genes]);

            Assert.That(coverage.Rows[0].Message, Is.EqualTo("prerequisite:Coverage"));
            Assert.That(exact.Rows[0].Message, Is.EqualTo("prerequisite:ExactPayload"));
        }

        [Test]
        public void Build_CopiesSortedRandomGenesIntoStableSnapshot()
        {
            GeneDef first = PlanTestData.CreateGene("First");
            GeneDef second = PlanTestData.CreateGene("Second");
            var randomGroup = new PlanGeneRandomChoiceGroupDiagnostic([first, second]);
            var analysis = new PlanGeneTargetAnalysisResult([], [randomGroup], []);
            var sortedGenes = new List<GeneDef> { second, first };

            GeneTargetDiagnosticsProjection projection = Build(analysis, PlanReadinessMode.Coverage, _ => sortedGenes);

            sortedGenes.Clear();

            Assert.That(projection.Rows[0].Genes, Is.EqualTo([second, first]));
        }

        [Test]
        public void Build_EmptyAnalysis_ReturnsEmptyProjection()
        {
            var analysis = new PlanGeneTargetAnalysisResult([], [], []);

            GeneTargetDiagnosticsProjection projection = Build(
                analysis,
                PlanReadinessMode.Coverage,
                genes => [..genes]);

            Assert.That(projection.HasDiagnostics, Is.False);
            Assert.That(projection.Rows, Is.Empty);
        }

        private static GeneTargetDiagnosticsProjection Build(
            PlanGeneTargetAnalysisResult analysis,
            PlanReadinessMode readinessMode,
            Func<IEnumerable<GeneDef>, List<GeneDef>> sortGenes)
        {
            return GeneTargetDiagnosticsProjection.Build(
                analysis,
                readinessMode,
                diagnostics => [..diagnostics],
                diagnostics => [..diagnostics],
                diagnostics => [..diagnostics],
                sortGenes,
                diagnostic => "conflict:" + diagnostic.FirstGene.defName,
                diagnostic => "random:" + diagnostic.Genes[0].defName,
                _ => "prerequisite:" + readinessMode);
        }
    }
}