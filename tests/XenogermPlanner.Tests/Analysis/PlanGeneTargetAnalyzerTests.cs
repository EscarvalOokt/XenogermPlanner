using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Tests.Plans;

namespace XenogermPlanner.Tests.Analysis
{
    [TestFixture]
    public sealed class PlanGeneTargetAnalyzerTests
    {
        [Test]
        public void Analyze_OrdinaryConflictReportsUnambiguousWinner()
        {
            GeneDef winner = PlanTestData.CreateGene("A");
            GeneDef loser = PlanTestData.CreateGene("B");

            PlanGeneTargetAnalysisResult result = Analyze(
                [loser, winner],
                (_, _) => true,
                (left, _) => ReferenceEquals(left, winner),
                _ => false);

            Assert.That(result.Conflicts, Has.Count.EqualTo(1));
            Assert.That(result.RandomChoiceGroups, Is.Empty);
            Assert.That(result.Conflicts[0].Kind, Is.EqualTo(PlanGeneConflictKind.Ordinary));
            Assert.That(result.Conflicts[0].OverridingGene, Is.SameAs(winner));
            Assert.That(result.Conflicts[0].OverriddenGene, Is.SameAs(loser));
        }

        [Test]
        public void Analyze_TwoRandomChosenGenesProduceOneGroup()
        {
            GeneDef first = PlanTestData.CreateGene("A");
            GeneDef second = PlanTestData.CreateGene("B");

            PlanGeneTargetAnalysisResult result = Analyze(
                [first, second],
                (_, _) => true,
                (left, _) => ReferenceEquals(left, first),
                _ => true);

            Assert.That(result.Conflicts, Is.Empty);
            Assert.That(result.RandomChoiceGroups, Has.Count.EqualTo(1));
            Assert.That(result.RandomChoiceGroups[0].Genes, Is.EqualTo([first, second]));
        }

        [Test]
        public void Analyze_CompleteRandomConflictGraphProducesOneGroup()
        {
            GeneDef first = PlanTestData.CreateGene("A");
            GeneDef second = PlanTestData.CreateGene("B");
            GeneDef third = PlanTestData.CreateGene("C");

            PlanGeneTargetAnalysisResult result = Analyze(
                [third, first, second],
                (left, right) => !ReferenceEquals(left, right),
                (_, _) => false,
                _ => true);

            Assert.That(result.Conflicts, Is.Empty);
            Assert.That(result.RandomChoiceGroups, Has.Count.EqualTo(1));
            Assert.That(result.RandomChoiceGroups[0].Genes, Is.EqualTo([first, second, third]));
        }

        [Test]
        public void Analyze_ConnectedRandomConflictGraphProducesOneGroup()
        {
            GeneDef first = PlanTestData.CreateGene("A");
            GeneDef second = PlanTestData.CreateGene("B");
            GeneDef third = PlanTestData.CreateGene("C");

            bool Conflicts(GeneDef left, GeneDef right)
            {
                return ReferenceEquals(left, first) && ReferenceEquals(right, second) ||
                       ReferenceEquals(left, second) && ReferenceEquals(right, first) ||
                       ReferenceEquals(left, second) && ReferenceEquals(right, third) ||
                       ReferenceEquals(left, third) && ReferenceEquals(right, second);
            }

            PlanGeneTargetAnalysisResult result = Analyze(
                [third, first, second],
                Conflicts,
                (_, _) => false,
                _ => true);

            Assert.That(result.RandomChoiceGroups, Has.Count.EqualTo(1));
            Assert.That(result.RandomChoiceGroups[0].Genes, Is.EqualTo([first, second, third]));
        }

        [Test]
        public void Analyze_IndependentRandomConflictGraphsProduceSeparateGroups()
        {
            GeneDef first = PlanTestData.CreateGene("A");
            GeneDef second = PlanTestData.CreateGene("B");
            GeneDef third = PlanTestData.CreateGene("C");
            GeneDef fourth = PlanTestData.CreateGene("D");

            bool Conflicts(GeneDef left, GeneDef right)
            {
                return IsPair(left, right, first, second) || IsPair(left, right, third, fourth);
            }

            PlanGeneTargetAnalysisResult result = Analyze(
                [fourth, second, third, first],
                Conflicts,
                (_, _) => false,
                _ => true);

            Assert.That(result.RandomChoiceGroups, Has.Count.EqualTo(2));
            Assert.That(result.RandomChoiceGroups[0].Genes, Is.EqualTo([first, second]));
            Assert.That(result.RandomChoiceGroups[1].Genes, Is.EqualTo([third, fourth]));
        }

        [Test]
        public void Analyze_MixedConflictDoesNotMergeOrdinaryGeneIntoRandomGroup()
        {
            GeneDef ordinary = PlanTestData.CreateGene("A");
            GeneDef randomFirst = PlanTestData.CreateGene("B");
            GeneDef randomSecond = PlanTestData.CreateGene("C");

            bool Conflicts(GeneDef left, GeneDef right)
            {
                return IsPair(left, right, ordinary, randomFirst) || IsPair(left, right, randomFirst, randomSecond);
            }

            PlanGeneTargetAnalysisResult result = Analyze(
                [randomSecond, ordinary, randomFirst],
                Conflicts,
                (_, _) => false,
                gene => !ReferenceEquals(gene, ordinary));

            Assert.That(result.Conflicts, Has.Count.EqualTo(1));
            Assert.That(result.Conflicts[0].Kind, Is.EqualTo(PlanGeneConflictKind.Mixed));
            Assert.That(result.Conflicts[0].FirstGene, Is.SameAs(ordinary));
            Assert.That(result.Conflicts[0].SecondGene, Is.SameAs(randomFirst));
            Assert.That(result.RandomChoiceGroups, Has.Count.EqualTo(1));
            Assert.That(result.RandomChoiceGroups[0].Genes, Is.EqualTo([randomFirst, randomSecond]));
        }

        [Test]
        public void Analyze_MixedConflictDoesNotPredictWinner()
        {
            GeneDef ordinary = PlanTestData.CreateGene("A");
            GeneDef random = PlanTestData.CreateGene("B");

            PlanGeneTargetAnalysisResult result = Analyze(
                [ordinary, random],
                (_, _) => true,
                (left, _) => ReferenceEquals(left, ordinary),
                gene => ReferenceEquals(gene, random));

            Assert.That(result.Conflicts[0].Kind, Is.EqualTo(PlanGeneConflictKind.Mixed));
            Assert.That(result.Conflicts[0].HasPredictedWinner, Is.False);
        }

        [Test]
        public void Analyze_AsymmetricConflictPredicateProducesOneUnorderedDiagnostic()
        {
            GeneDef first = PlanTestData.CreateGene("A");
            GeneDef second = PlanTestData.CreateGene("B");

            PlanGeneTargetAnalysisResult result = Analyze(
                [second, first],
                (left, right) => ReferenceEquals(left, second) && ReferenceEquals(right, first),
                (_, _) => false,
                _ => false);

            Assert.That(result.Conflicts, Has.Count.EqualTo(1));
            Assert.That(result.Conflicts[0].FirstGene, Is.SameAs(first));
            Assert.That(result.Conflicts[0].SecondGene, Is.SameAs(second));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Analyze_AmbiguousOrdinaryOverrideDoesNotPredictWinner(bool bothOverride)
        {
            GeneDef first = PlanTestData.CreateGene("A");
            GeneDef second = PlanTestData.CreateGene("B");

            PlanGeneTargetAnalysisResult result = Analyze(
                [first, second],
                (_, _) => true,
                (_, _) => bothOverride,
                _ => false);

            Assert.That(result.Conflicts[0].HasPredictedWinner, Is.False);
        }

        [Test]
        public void Analyze_MissingPrerequisiteReportsDependentRelation()
        {
            GeneDef prerequisite = PlanTestData.CreateGene("Prerequisite");
            GeneDef dependent = PlanTestData.CreateGene("Dependent");

            PlanGeneTargetAnalysisResult result = Analyze(
                [dependent],
                (_, _) => false,
                (_, _) => false,
                _ => false,
                gene => ReferenceEquals(gene, dependent) ? prerequisite : null);

            Assert.That(result.MissingPrerequisites, Has.Count.EqualTo(1));
            Assert.That(result.MissingPrerequisites[0].DependentGene, Is.SameAs(dependent));
            Assert.That(result.MissingPrerequisites[0].PrerequisiteGene, Is.SameAs(prerequisite));
        }

        [Test]
        public void Analyze_PresentPrerequisiteProducesNoMissingDiagnostic()
        {
            GeneDef prerequisite = PlanTestData.CreateGene("Prerequisite");
            GeneDef dependent = PlanTestData.CreateGene("Dependent");

            PlanGeneTargetAnalysisResult result = Analyze(
                [dependent, prerequisite],
                (_, _) => false,
                (_, _) => false,
                _ => false,
                gene => ReferenceEquals(gene, dependent) ? prerequisite : null);

            Assert.That(result.MissingPrerequisites, Is.Empty);
        }

        [Test]
        public void Analyze_NormalizesDuplicateInputWithoutMutatingCollection()
        {
            GeneDef first = PlanTestData.CreateGene("A");
            GeneDef second = PlanTestData.CreateGene("B");
            var input = new List<GeneDef> { second, first, first };

            PlanGeneTargetAnalysisResult result = Analyze(input, (_, _) => true, (_, _) => false, _ => true);

            Assert.That(result.RandomChoiceGroups, Has.Count.EqualTo(1));
            Assert.That(result.RandomChoiceGroups[0].Genes, Is.EqualTo([first, second]));
            Assert.That(input, Is.EqualTo([second, first, first]));
        }

        [Test]
        public void Analyze_OrdersConflictsGroupsAndPrerequisitesByDefName()
        {
            GeneDef geneA = PlanTestData.CreateGene("A");
            GeneDef geneB = PlanTestData.CreateGene("B");
            GeneDef geneC = PlanTestData.CreateGene("C");
            GeneDef geneD = PlanTestData.CreateGene("D");
            GeneDef prerequisiteA = PlanTestData.CreateGene("P1");
            GeneDef prerequisiteC = PlanTestData.CreateGene("P2");

            bool Conflicts(GeneDef left, GeneDef right)
            {
                return IsPair(left, right, geneA, geneB) || IsPair(left, right, geneC, geneD);
            }

            PlanGeneTargetAnalysisResult result = Analyze(
                [geneD, geneC, geneB, geneA],
                Conflicts,
                (_, _) => false,
                _ => true,
                gene => ReferenceEquals(gene, geneA) ? prerequisiteA :
                    ReferenceEquals(gene, geneC) ? prerequisiteC : null);

            Assert.That(
                result.RandomChoiceGroups.Select(group => string.Join(
                    string.Empty,
                    group.Genes.Select(gene => gene.defName))),
                Is.EqualTo(["AB", "CD"]));
            Assert.That(
                result.MissingPrerequisites.Select(diagnostic => diagnostic.DependentGene.defName),
                Is.EqualTo(["A", "C"]));
        }

        [Test]
        public void Analyze_InputOrderDoesNotChangeRandomGroups()
        {
            GeneDef first = PlanTestData.CreateGene("A");
            GeneDef second = PlanTestData.CreateGene("B");
            GeneDef third = PlanTestData.CreateGene("C");

            PlanGeneTargetAnalysisResult firstResult = Analyze(
                [first, second, third],
                (left, right) => !ReferenceEquals(left, right),
                (_, _) => false,
                _ => true);
            PlanGeneTargetAnalysisResult secondResult = Analyze(
                [third, first, second],
                (left, right) => !ReferenceEquals(left, right),
                (_, _) => false,
                _ => true);

            Assert.That(firstResult.RandomChoiceGroups, Has.Count.EqualTo(1));
            Assert.That(secondResult.RandomChoiceGroups, Has.Count.EqualTo(1));
            Assert.That(secondResult.RandomChoiceGroups[0].Genes, Is.EqualTo(firstResult.RandomChoiceGroups[0].Genes));
        }

        [Test]
        public void AnalysisResult_DiagnosticCountIncludesAllDiagnosticKinds()
        {
            GeneDef first = PlanTestData.CreateGene("A");
            GeneDef second = PlanTestData.CreateGene("B");
            GeneDef third = PlanTestData.CreateGene("C");
            GeneDef fourth = PlanTestData.CreateGene("D");
            var conflict = new PlanGeneConflictDiagnostic(first, second, PlanGeneConflictKind.Ordinary, first, second);
            var group = new PlanGeneRandomChoiceGroupDiagnostic([second, third]);
            var prerequisite = new PlanGenePrerequisiteDiagnostic(fourth, first);
            var result = new PlanGeneTargetAnalysisResult([conflict], [group], [prerequisite]);

            Assert.That(result.DiagnosticCount, Is.EqualTo(3));
            Assert.That(result.HasConflicts, Is.True);
            Assert.That(result.HasDiagnostics, Is.True);
        }

        [Test]
        public void RandomChoiceGroup_RequiresAtLeastTwoDistinctGenes()
        {
            GeneDef gene = PlanTestData.CreateGene("A");

            Assert.That(
                (Action)(() => _ = new PlanGeneRandomChoiceGroupDiagnostic([gene, gene])),
                Throws.ArgumentException);
        }

        [Test]
        public void RandomChoiceGroup_RejectsNullGene()
        {
            GeneDef gene = PlanTestData.CreateGene("A");

            Assert.That(
                (Action)(() => _ = new PlanGeneRandomChoiceGroupDiagnostic([gene, null])),
                Throws.ArgumentException);
        }

        [Test]
        public void RandomChoiceGroup_CopiesAndOrdersInput()
        {
            GeneDef first = PlanTestData.CreateGene("A");
            GeneDef second = PlanTestData.CreateGene("B");
            var input = new List<GeneDef> { second, first };
            var diagnostic = new PlanGeneRandomChoiceGroupDiagnostic(input);

            input.Clear();

            Assert.That(diagnostic.Genes, Is.EqualTo([first, second]));
        }

        [Test]
        public void AnalysisResult_RejectsDuplicateRandomChoiceGroups()
        {
            GeneDef first = PlanTestData.CreateGene("A");
            GeneDef second = PlanTestData.CreateGene("B");
            var firstGroup = new PlanGeneRandomChoiceGroupDiagnostic([first, second]);
            var duplicateGroup = new PlanGeneRandomChoiceGroupDiagnostic([second, first]);

            Assert.That(
                (Action)(() => _ = new PlanGeneTargetAnalysisResult([], [firstGroup, duplicateGroup], [])),
                Throws.ArgumentException);
        }

        [Test]
        public void Analyze_NullGeneFailsAtAnalysisBoundary()
        {
            void Action() => PlanGeneTargetAnalyzer.Analyze([null]);

            Assert.That((Action)Action, Throws.ArgumentException);
        }

        private static bool IsPair(GeneDef left, GeneDef right, GeneDef first, GeneDef second)
        {
            return ReferenceEquals(left, first) && ReferenceEquals(right, second) ||
                   ReferenceEquals(left, second) && ReferenceEquals(right, first);
        }

        private static PlanGeneTargetAnalysisResult Analyze(
            IEnumerable<GeneDef> genes,
            Func<GeneDef, GeneDef, bool> conflictsWith,
            Func<GeneDef, GeneDef, bool> overrides,
            Func<GeneDef, bool> isRandomChosen,
            Func<GeneDef, GeneDef> getPrerequisite = null)
        {
            return PlanGeneTargetAnalyzer.Analyze(
                genes,
                conflictsWith,
                overrides,
                isRandomChosen,
                getPrerequisite ?? (_ => null));
        }
    }
}