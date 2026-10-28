using System;
using System.Linq;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Assemblers;
using XenogermPlanner.Plans;
using Pack = XenogermPlanner.Tests.Assemblers.PlanAssemblerReadinessTestData.PackFixture;

namespace XenogermPlanner.Tests.Assemblers
{
    [TestFixture]
    public sealed class PlanAssemblerSearchBudgetTests
    {
        [Test]
        public void Budget_IsExhaustedOnlyWhenAnAdditionalStateIsRefused()
        {
            var budget = new PlanAssemblerSearchBudget(1);
            Assert.That(budget.TryVisitState(), Is.True);
            Assert.That(budget.VisitedStates, Is.EqualTo(1));
            Assert.That(budget.IsExhausted, Is.False);
            Assert.That(budget.TryVisitState(), Is.False);
            Assert.That(budget.IsExhausted, Is.True);
            Assert.That(budget.VisitedStates, Is.EqualTo(1));
            Assert.That(budget.TryVisitState(), Is.False);
            Assert.That(budget.VisitedStates, Is.EqualTo(1));
        }

        [Test]
        public void Budget_DefaultAndAppendedEnumValuesAreStable()
        {
            Assert.That(new PlanAssemblerSearchBudget().MaxVisitedStates, Is.EqualTo(10000));
            Assert.That((int)PlanAssemblerReadinessStatus.SearchIncomplete, Is.EqualTo(5));
            Assert.That((int)PlanAssemblerBlockerReason.MetabolismTooLow, Is.EqualTo(6));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new PlanAssemblerSearchBudget(0));
        }

        [Test]
        public void Analyze_BudgetBeforeFirstCandidateReturnsHonestIncomplete()
        {
            GeneDef target = Gene("A_Target", 0);
            var budget = new PlanAssemblerSearchBudget(1);
            PlanAssemblerReadinessResult result = Analyze(
                PlanReadinessMode.Coverage,
                [target],
                budget,
                PackOf("target", target));

            AssertIncomplete(result);
            Assert.That(budget.VisitedStates, Is.EqualTo(1));
        }

        [Test]
        public void Analyze_BasePhaseDoesNotGetASecondBudgetForHelpers()
        {
            GeneDef first = Gene("A_First", 0);
            GeneDef second = Gene("B_Second", 0);
            var budget = new PlanAssemblerSearchBudget(2);
            PlanAssemblerReadinessResult result = Analyze(
                PlanReadinessMode.Coverage,
                [first, second],
                budget,
                PackOf("first", first),
                PackOf("second", second),
                PackOf("helper", Gene("Helper", 1)));

            AssertIncomplete(result);
            Assert.That(budget.VisitedStates, Is.EqualTo(2));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void Analyze_ExhaustedHelperSearchDoesNotExposeTheBlockedBaseCandidate(int limit)
        {
            GeneDef target = Gene("A_Target", -6);
            var budget = new PlanAssemblerSearchBudget(limit);
            PlanAssemblerReadinessResult result = Analyze(
                PlanReadinessMode.Coverage,
                [target],
                budget,
                PackOf("target", target),
                PackOf("helper", Gene("B_Helper", 1)));

            AssertIncomplete(result);
            Assert.That(budget.IsExhausted, Is.True);
            Assert.That(budget.VisitedStates, Is.EqualTo(limit));
        }

        [Test]
        public void Analyze_ReadyOnLastAllowedBaseStateWins()
        {
            GeneDef target = Gene("A_Target", -5);
            var budget = new PlanAssemblerSearchBudget(2);
            PlanAssemblerReadinessResult result = Analyze(
                PlanReadinessMode.ExactPayload,
                [target],
                budget,
                PackOf("target", target));

            Assert.That(result.IsReady, Is.True);
            Assert.That(budget.VisitedStates, Is.EqualTo(2));
            Assert.That(budget.IsExhausted, Is.False);
        }

        [Test]
        public void Analyze_ReadyOnLastAllowedHelperStateWins()
        {
            GeneDef target = Gene("A_Target", -6);
            var budget = new PlanAssemblerSearchBudget(5);
            PlanAssemblerReadinessResult result = Analyze(
                PlanReadinessMode.Coverage,
                [target],
                budget,
                PackOf("target", target),
                PackOf("helper", Gene("B_Helper", 1)));

            Assert.That(result.IsReady, Is.True);
            Assert.That(result.ActualMetabolism, Is.EqualTo(-5));
            Assert.That(budget.VisitedStates, Is.EqualTo(5));
            Assert.That(budget.IsExhausted, Is.False);
        }

        [Test]
        public void Analyze_FullyCompletedBaseAtLimitIsBlockedNotIncomplete()
        {
            GeneDef target = Gene("A_Target", -6);
            var budget = new PlanAssemblerSearchBudget(2);
            PlanAssemblerReadinessResult result = Analyze(
                PlanReadinessMode.ExactPayload,
                [target],
                budget,
                PackOf("target", target));

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Blocked));
            Assert.That(budget.VisitedStates, Is.EqualTo(2));
            Assert.That(budget.IsExhausted, Is.False);
        }

        [TestCase(5, PlanAssemblerReadinessStatus.SearchIncomplete)]
        [TestCase(6, PlanAssemblerReadinessStatus.Blocked)]
        public void Analyze_CompletedHelperBoundaryRequiresNoUnvisitedState(
            int limit,
            PlanAssemblerReadinessStatus expected)
        {
            GeneDef target = Gene("A_Target", -6);
            var budget = new PlanAssemblerSearchBudget(limit);
            PlanAssemblerReadinessResult result = Analyze(
                PlanReadinessMode.Coverage,
                [target],
                budget,
                PackOf("target", target),
                PackOf("helper", Gene("B_Helper", 0)));

            Assert.That(result.Status, Is.EqualTo(expected));
            Assert.That(budget.VisitedStates, Is.EqualTo(limit));
            Assert.That(budget.IsExhausted, Is.EqualTo(expected == PlanAssemblerReadinessStatus.SearchIncomplete));
        }

        [Test]
        public void Analyze_ReadyDoesNotRequireEnumeratingOtherCandidates()
        {
            GeneDef target = Gene("A_Target", 0);
            var budget = new PlanAssemblerSearchBudget(2);
            PlanAssemblerReadinessResult result = Analyze(
                PlanReadinessMode.Coverage,
                [target],
                budget,
                PackOf("target", target),
                PackOf("alternative", target, Gene("Z_Extra", 1)));

            Assert.That(result.IsReady, Is.True);
            Assert.That(budget.VisitedStates, Is.EqualTo(2));
        }

        [Test]
        public void Analyze_DefaultBudgetBoundsHelperHeavyBlockedSearch()
        {
            GeneDef target = Gene("A_Target", -6);
            Pack[] packs = Enumerable.Range(0, 20)
                .Select(index => PackOf("helper-" + index, Gene("Helper" + index.ToString("D2"), 0)))
                .Prepend(PackOf("target", target))
                .ToArray();
            var budget = new PlanAssemblerSearchBudget();

            PlanAssemblerReadinessResult result = Analyze(PlanReadinessMode.Coverage, [target], budget, packs);

            AssertIncomplete(result);
            Assert.That(budget.VisitedStates, Is.EqualTo(10000));
        }

        [Test]
        public void Analyze_EquivalentPhysicalCopiesDoNotConsumeExtraStates()
        {
            GeneDef target = Gene("A_Target", -6);
            GeneDef helper = Gene("B_Helper", 1);
            Pack targetPack = PackOf("target", target);
            Pack[] packs = Enumerable.Range(0, 30)
                .Select(index => PackOf("helper-" + index.ToString("D2"), helper))
                .Prepend(targetPack)
                .ToArray();
            var budget = new PlanAssemblerSearchBudget(5);

            PlanAssemblerReadinessResult result = Analyze(PlanReadinessMode.Coverage, [target], budget, packs);

            Assert.That(result.IsReady, Is.True);
            Assert.That(budget.VisitedStates, Is.EqualTo(5));
            Assert.That(result.CandidatePackCount, Is.EqualTo(2));
        }

        private static void AssertIncomplete(PlanAssemblerReadinessResult result)
        {
            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.SearchIncomplete));
            Assert.That(result.GeneScopeResult.Status, Is.EqualTo(PlanReadinessStatus.Ready));
            Assert.That(result.IsReady, Is.False);
            Assert.That(result.CandidateGenepacks, Is.Empty);
            Assert.That(result.BlockerReasons, Is.Empty);
            Assert.That(result.MissingPrerequisites, Is.Empty);
            Assert.That(result.ActualMetabolism, Is.Zero);
            Assert.That(result.MinimumMetabolism, Is.EqualTo(-5));
            Assert.That(result.RequiredComplexity, Is.Zero);
            Assert.That(result.RequiredArchiteCapsules, Is.Zero);
        }

        private static GeneDef Gene(string name, int metabolism)
        {
            return PlanAssemblerReadinessTestData.CreateGene(name, metabolism: metabolism);
        }

        private static Pack PackOf(string name, params GeneDef[] genes)
        {
            return PlanAssemblerReadinessTestData.CreatePack(name, true, genes);
        }

        private static PlanAssemblerReadinessResult Analyze(
            PlanReadinessMode mode,
            GeneDef[] target,
            PlanAssemblerSearchBudget budget,
            params Pack[] packs)
        {
            return PlanAssemblerReadinessTestData.Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(mode, target),
                PlanAssemblerReadinessTestData.CreateLiveState(true, 100, 100, true, packs),
                genes => genes,
                budget,
                packs);
        }
    }
}