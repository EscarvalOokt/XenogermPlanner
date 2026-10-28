using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Assemblers;
using XenogermPlanner.Plans;
using Pack = XenogermPlanner.Tests.Assemblers.PlanAssemblerReadinessTestData.PackFixture;

namespace XenogermPlanner.Tests.Assemblers
{
    [TestFixture]
    public sealed class PlanAssemblerMetabolismTests
    {
        [TestCase(-6, PlanAssemblerReadinessStatus.Blocked)]
        [TestCase(-5, PlanAssemblerReadinessStatus.Ready)]
        [TestCase(6, PlanAssemblerReadinessStatus.Ready)]
        public void Analyze_UsesVanillaLowerBoundWithoutAnUpperBound(
            int metabolism,
            PlanAssemblerReadinessStatus expected)
        {
            GeneDef target = Gene("Target", metabolism);
            Pack pack = PackOf("target", target);
            PlanAssemblerReadinessResult result = Analyze(Plan(PlanReadinessMode.ExactPayload, target), pack);

            Assert.That(result.Status, Is.EqualTo(expected));
            Assert.That(result.ActualMetabolism, Is.EqualTo(metabolism));
            Assert.That(result.MinimumMetabolism, Is.EqualTo(-5));
            Assert.That(
                result.BlockerReasons.Contains(PlanAssemblerBlockerReason.MetabolismTooLow),
                Is.EqualTo(metabolism < -5));
        }

        [Test]
        public void Analyze_UsesDistinctEffectivePhysicalGenesIncludingExtras()
        {
            GeneDef first = Gene("First", 0);
            GeneDef second = Gene("Second", 0);
            GeneDef extra = Gene("Extra", -4);
            Pack one = PackOf("one", first, extra);
            Pack two = PackOf("two", second, extra);

            PlanAssemblerReadinessResult result = Analyze(Plan(PlanReadinessMode.Coverage, first, second), one, two);

            Assert.That(result.IsReady, Is.True);
            Assert.That(result.ActualMetabolism, Is.EqualTo(-4));
        }

        [Test]
        public void Analyze_CoverageUsesCompensatingExtraInTargetPack()
        {
            GeneDef target = Gene("Target", -6);
            GeneDef helper = Gene("Helper", 1);
            PlanAssemblerReadinessResult result = Analyze(
                Plan(PlanReadinessMode.Coverage, target),
                PackOf("pack", target, helper));

            Assert.That(result.IsReady, Is.True);
            Assert.That(result.ActualMetabolism, Is.EqualTo(-5));
        }

        [Test]
        public void Analyze_CoverageCanUseMultipleSeparateHelperPacks()
        {
            GeneDef target = Gene("Target", -8);
            Pack targetPack = PackOf("target", target);
            Pack helperOne = PackOf("one", Gene("HelperOne", 1));
            Pack helperTwo = PackOf("two", Gene("HelperTwo", 2));

            PlanAssemblerReadinessResult result = Analyze(
                Plan(PlanReadinessMode.Coverage, target),
                targetPack,
                helperOne,
                helperTwo);

            Assert.That(result.IsReady, Is.True);
            Assert.That(result.CandidatePackCount, Is.EqualTo(3));
            Assert.That(result.ActualMetabolism, Is.EqualTo(-5));
        }

        [Test]
        public void Analyze_HelperPrerequisiteAndItsMetabolismAreIncluded()
        {
            GeneDef target = Gene("Target", -6);
            GeneDef prerequisite = Gene("Prerequisite", -2);
            GeneDef helper = Gene("Helper", 3, prerequisite: prerequisite);
            Pack[] packs = [PackOf("target", target), PackOf("helper", helper), PackOf("prerequisite", prerequisite)];

            PlanAssemblerReadinessResult result = Analyze(Plan(PlanReadinessMode.Coverage, target), packs);

            Assert.That(result.IsReady, Is.True);
            Assert.That(result.CandidatePackCount, Is.EqualTo(3));
            Assert.That(result.ActualMetabolism, Is.EqualTo(-5));
        }

        [Test]
        public void Analyze_HelperWithoutItsPrerequisiteDoesNotProduceReady()
        {
            GeneDef target = Gene("Target", -6);
            GeneDef prerequisite = Gene("Missing", 0);
            GeneDef helper = Gene("Helper", 1, prerequisite: prerequisite);

            PlanAssemblerReadinessResult result = Analyze(
                Plan(PlanReadinessMode.Coverage, target),
                PackOf("target", target),
                PackOf("helper", helper));

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Blocked));
        }

        [Test]
        public void Analyze_HelperPrerequisiteChainIsClosed()
        {
            GeneDef root = Gene("Root", -1);
            GeneDef middle = Gene("Middle", 0, prerequisite: root);
            GeneDef helper = Gene("Helper", 2, prerequisite: middle);
            GeneDef target = Gene("Target", -6);

            PlanAssemblerReadinessResult result = Analyze(
                Plan(PlanReadinessMode.Coverage, target),
                PackOf("target", target),
                PackOf("helper", helper),
                PackOf("middle", middle),
                PackOf("root", root));

            Assert.That(result.IsReady, Is.True);
            Assert.That(result.CandidatePackCount, Is.EqualTo(4));
        }

        [Test]
        public void Analyze_NegativeHelperCanImproveMetabolismByOverridingAGene()
        {
            GeneDef target = Gene("Target", -8);
            GeneDef helper = Gene("Helper", -2);
            Pack[] packs = [PackOf("target", target), PackOf("helper", helper)];

            IEnumerable<GeneDef> Effective(IEnumerable<GeneDef> genes)
            {
                GeneDef[] copied = genes.ToArray();
                return copied.Contains(helper) ? copied.Where(gene => gene != target) : copied;
            }

            PlanAssemblerReadinessResult result = Analyze(
                Plan(PlanReadinessMode.Coverage, target),
                packs,
                effective: Effective);

            Assert.That(result.IsReady, Is.True);
            Assert.That(result.ActualMetabolism, Is.EqualTo(-2));
        }

        [Test]
        public void Analyze_DoesNotStopAtMetabolismThresholdWhileComplexityStillNeedsAnOverride()
        {
            GeneDef target = Gene("A_Target", -6);
            GeneDef expensive = Gene("B_Expensive", 0, complexity: 20);
            GeneDef helper = Gene("C_Helper", 1);
            GeneDef overriding = Gene("D_Override", 0);
            Pack[] packs =
            [
                PackOf("target", target, expensive), PackOf("helper", helper), PackOf("override", overriding)
            ];

            IEnumerable<GeneDef> Effective(IEnumerable<GeneDef> genes)
            {
                GeneDef[] copied = genes.ToArray();
                return copied.Contains(overriding) ? copied.Where(gene => gene != expensive) : copied;
            }

            PlanAssemblerReadinessResult result = Analyze(
                Plan(PlanReadinessMode.Coverage, target, expensive),
                packs,
                effective: Effective,
                maxComplexity: 10);

            Assert.That(result.IsReady, Is.True);
            Assert.That(result.CandidatePackCount, Is.EqualTo(3));
            Assert.That(result.RequiredComplexity, Is.Zero);
        }

        [Test]
        public void Analyze_ExactPayloadDoesNotAdmitExtraHelperGenes()
        {
            GeneDef target = Gene("Target", -6);
            Pack targetPack = PackOf("target", target);
            Pack helper = PackOf("helper", Gene("Helper", 1));

            PlanAssemblerReadinessResult result = Analyze(
                Plan(PlanReadinessMode.ExactPayload, target),
                targetPack,
                helper);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Blocked));
            Assert.That(result.CandidateGenepacks, Is.EqualTo([targetPack.Genepack]));
        }

        [Test]
        public void Analyze_EquivalentHelperCompositionsChoosePoweredPhysicalSource()
        {
            GeneDef target = Gene("Target", -6);
            GeneDef helper = Gene("Helper", 1);
            Pack targetPack = PackOf("target", target);
            Pack unavailable = PlanAssemblerReadinessTestData.CreatePack("a-unpowered", false, helper);
            Pack available = PackOf("z-powered", helper);

            PlanAssemblerReadinessResult result = Analyze(
                Plan(PlanReadinessMode.Coverage, target),
                targetPack,
                unavailable,
                available);

            Assert.That(result.IsReady, Is.True);
            Assert.That(result.CandidateGenepacks.Any(pack => ReferenceEquals(pack, available.Genepack)), Is.True);
            Assert.That(result.CandidateGenepacks.Any(pack => ReferenceEquals(pack, unavailable.Genepack)), Is.False);
        }

        [TestCase(false, 0, 0, true)]
        [TestCase(true, 101, 0, true)]
        [TestCase(true, 0, 1, false)]
        [TestCase(true, 0, 101, true)]
        public void Analyze_HelperMustMeetEveryLiveBlocker(bool power, int complexity, int archite, bool research)
        {
            GeneDef target = Gene("Target", -6);
            GeneDef helper = Gene("Helper", 1, complexity, archite);
            Pack[] packs =
            [
                PackOf("target", target), PlanAssemblerReadinessTestData.CreatePack("helper", power, helper)
            ];

            PlanAssemblerReadinessResult result = Analyze(
                Plan(PlanReadinessMode.Coverage, target),
                packs,
                effective: null,
                research: research);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Blocked));
        }

        [Test]
        public void Analyze_ContinuesPastAnUnusableHelperToAWorkingAlternative()
        {
            GeneDef target = Gene("Target", -6);
            Pack expensive = PackOf("expensive", Gene("A_Expensive", 1, complexity: 101));
            Pack working = PackOf("working", Gene("B_Working", 1));

            PlanAssemblerReadinessResult result = Analyze(
                Plan(PlanReadinessMode.Coverage, target),
                PackOf("target", target),
                expensive,
                working);

            Assert.That(result.IsReady, Is.True);
            Assert.That(result.CandidateGenepacks.Any(pack => ReferenceEquals(pack, working.Genepack)), Is.True);
            Assert.That(result.CandidateGenepacks.Any(pack => ReferenceEquals(pack, expensive.Genepack)), Is.False);
        }

        [Test]
        public void Analyze_SmallExhaustiveOracleAgreesAcrossOverridesPrerequisitesAndLiveState()
        {
            for (var seed = 0; seed < 40; seed++)
            {
                var random = new Random(seed);
                GeneDef first = Gene("TargetA", -4);
                GeneDef second = Gene("TargetB", -3);
                var helpers = new GeneDef[4];
                for (var index = 0; index < helpers.Length; index++)
                    helpers[index] = Gene("Helper" + index, random.Next(-2, 4), random.Next(0, 5), random.Next(0, 2));
                helpers[0].prerequisite = seed % 2 == 0 ? helpers[1] : null;
                helpers[1].prerequisite = seed % 3 == 0 ? helpers[2] : null;

                var packs = new List<Pack> { PackOf("target-a", first), PackOf("target-b", second) };
                for (var index = 0; index < helpers.Length; index++)
                    packs.Add(
                        PlanAssemblerReadinessTestData.CreatePack(
                            "helper-" + index,
                            random.Next(4) != 0,
                            helpers[index]));
                Pack[] inputs = packs.ToArray();
                bool research = seed % 4 != 0;
                bool overrideTarget = seed % 2 == 0;
                Func<IEnumerable<GeneDef>, IEnumerable<GeneDef>> effective = genes =>
                {
                    GeneDef[] copied = genes.ToArray();
                    return overrideTarget && copied.Contains(helpers[3]) ? copied.Where(gene => gene != first) : copied;
                };
                XenogermPlan plan = Plan(PlanReadinessMode.Coverage, first, second);
                bool expected = ExhaustiveReady(inputs, [first, second], effective, 7, 2, research);
                PlanAssemblerReadinessResult result = Analyze(
                    plan,
                    inputs,
                    effective: effective,
                    maxComplexity: 7,
                    capsules: 2,
                    research: research);
                PlanAssemblerReadinessResult reversed = Analyze(
                    plan,
                    inputs.Reverse().ToArray(),
                    effective: effective,
                    maxComplexity: 7,
                    capsules: 2,
                    research: research);

                Assert.That(
                    result.Status,
                    Is.EqualTo(expected ? PlanAssemblerReadinessStatus.Ready : PlanAssemblerReadinessStatus.Blocked),
                    "seed " + seed);
                Assert.That(reversed.Status, Is.EqualTo(result.Status), "seed " + seed);
                Assert.That(reversed.CandidateGenepacks, Is.EqualTo(result.CandidateGenepacks), "seed " + seed);
            }
        }

        private static bool ExhaustiveReady(
            Pack[] packs,
            GeneDef[] target,
            Func<IEnumerable<GeneDef>, IEnumerable<GeneDef>> effective,
            int capacity,
            int capsules,
            bool research)
        {
            for (var mask = 1; mask < (1 << packs.Length); mask++)
            {
                Pack[] selected = packs.Where((_, index) => (mask & (1 << index)) != 0).ToArray();
                GeneDef[] raw = selected.SelectMany(pack => pack.Genes).ToArray();
                var union = new HashSet<GeneDef>(raw);
                if (!target.All(union.Contains) ||
                    selected.Any(pack => !pack.Source.IsFacilityPowered) ||
                    raw.Any(gene => gene.prerequisite != null && !union.Contains(gene.prerequisite)))
                    continue;

                GeneDef[] active = effective(raw).ToArray();
                GeneDef[] distinct = active.Distinct().ToArray();
                int archites = distinct.Sum(gene => gene.biostatArc);
                if (active.Sum(gene => gene.biostatCpx) <= capacity &&
                    distinct.Sum(gene => gene.biostatMet) >= -5 &&
                    archites <= capsules &&
                    (archites == 0 || research))
                    return true;
            }

            return false;
        }

        private static GeneDef Gene(
            string name,
            int metabolism,
            int complexity = 0,
            int archite = 0,
            GeneDef prerequisite = null)
        {
            return PlanAssemblerReadinessTestData.CreateGene(name, complexity, archite, prerequisite, metabolism);
        }

        private static Pack PackOf(string name, params GeneDef[] genes)
        {
            return PlanAssemblerReadinessTestData.CreatePack(name, true, genes);
        }

        private static XenogermPlan Plan(PlanReadinessMode mode, params GeneDef[] genes)
        {
            return PlanAssemblerReadinessTestData.CreatePlan(mode, genes);
        }

        private static PlanAssemblerReadinessResult Analyze(XenogermPlan plan, params Pack[] packs)
        {
            return Analyze(plan, packs, null);
        }

        private static PlanAssemblerReadinessResult Analyze(
            XenogermPlan plan,
            Pack[] packs,
            Func<IEnumerable<GeneDef>, IEnumerable<GeneDef>> effective,
            int maxComplexity = 100,
            int capsules = 100,
            bool research = true)
        {
            return PlanAssemblerReadinessTestData.Analyze(
                plan,
                PlanAssemblerReadinessTestData.CreateLiveState(true, maxComplexity, capsules, research, packs),
                effective ?? (genes => genes),
                packs);
        }
    }
}