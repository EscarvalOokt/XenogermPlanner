using System.Linq;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Assemblers;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Assemblers
{
    [TestFixture]
    public sealed class PlanAssemblerReadinessAnalyzerTests
    {
        [Test]
        public void Analyze_DegradedPlanReturnsDegraded()
        {
            GeneDef gene = PlanAssemblerReadinessTestData.CreateGene("A");
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("pack", true, gene);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreateDegradedPlan(PlanReadinessMode.Coverage, [gene], "MissingGene"),
                true,
                100,
                100,
                true,
                pack);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Degraded));
            Assert.That(result.CandidateGenepacks, Is.Empty);
        }

        [Test]
        public void Analyze_EmptyTargetReturnsEmptyTarget()
        {
            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage),
                true,
                100,
                100,
                true);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.EmptyTarget));
            Assert.That(result.CandidateGenepacks, Is.Empty);
        }

        [Test]
        public void Analyze_NonReadyGeneScopeReturnsNotReady()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Target");

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                true,
                100,
                100,
                true);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.NotReady));
            Assert.That(result.GeneScopeResult.MissingGenes, Has.Count.EqualTo(1));
        }

        [Test]
        public void Analyze_UsableCandidateReturnsReady()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Target", 2);
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("pack", true, target);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                true,
                6,
                0,
                true,
                pack);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Ready));
            Assert.That(result.BlockerReasons, Is.Empty);
            Assert.That(result.RequiredComplexity, Is.EqualTo(2));
        }

        [Test]
        public void Analyze_UnpoweredAssemblerReturnsBlocked()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Target");
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("pack", true, target);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                false,
                100,
                100,
                true,
                pack);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Blocked));
            Assert.That(result.BlockerReasons, Is.EqualTo([PlanAssemblerBlockerReason.AssemblerUnpowered]));
        }

        [Test]
        public void Analyze_UnpoweredUsedGeneBankReturnsBlocked()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Target");
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("pack", false, target);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                true,
                100,
                100,
                true,
                pack);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Blocked));
            Assert.That(result.BlockerReasons, Does.Contain(PlanAssemblerBlockerReason.UsedGeneBankUnpowered));
        }

        [Test]
        public void Analyze_UnpoweredUnusedGeneBankDoesNotBlockReadiness()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Target");
            GeneDef unrelated = PlanAssemblerReadinessTestData.CreateGene("Other");
            PlanAssemblerReadinessTestData.PackFixture targetPack =
                PlanAssemblerReadinessTestData.CreatePack("target", true, target);
            PlanAssemblerReadinessTestData.PackFixture unrelatedPack =
                PlanAssemblerReadinessTestData.CreatePack("other", false, unrelated);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                true,
                100,
                100,
                true,
                targetPack,
                unrelatedPack);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Ready));
            Assert.That(result.BlockerReasons, Is.Empty);
        }

        [Test]
        public void Analyze_InsufficientLiveComplexityReturnsBlocked()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Target", 7);
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("pack", true, target);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                true,
                6,
                100,
                true,
                pack);

            Assert.That(result.BlockerReasons, Does.Contain(PlanAssemblerBlockerReason.InsufficientComplexity));
            Assert.That(result.RequiredComplexity, Is.EqualTo(7));
            Assert.That(result.AvailableComplexity, Is.EqualTo(6));
        }

        [Test]
        public void Analyze_IncreasedLiveComplexityRemovesComplexityBlocker()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Target", 7);
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("pack", true, target);
            XenogermPlan plan = PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target);

            PlanAssemblerReadinessResult result = Analyze(plan, true, 7, 100, true, pack);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Ready));
        }

        [Test]
        public void Analyze_DuplicateGeneAcrossCandidatePacksCountsComplexityTwice()
        {
            GeneDef duplicate = PlanAssemblerReadinessTestData.CreateGene("A", 2);
            GeneDef geneB = PlanAssemblerReadinessTestData.CreateGene("B");
            GeneDef geneC = PlanAssemblerReadinessTestData.CreateGene("C");
            PlanAssemblerReadinessTestData.PackFixture first =
                PlanAssemblerReadinessTestData.CreatePack("first", true, duplicate, geneB);
            PlanAssemblerReadinessTestData.PackFixture second =
                PlanAssemblerReadinessTestData.CreatePack("second", true, duplicate, geneC);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, duplicate, geneB, geneC),
                true,
                3,
                100,
                true,
                first,
                second);

            Assert.That(result.RequiredComplexity, Is.EqualTo(4));
            Assert.That(result.BlockerReasons, Does.Contain(PlanAssemblerBlockerReason.InsufficientComplexity));
        }

        [Test]
        public void Analyze_ArchiteCandidateRequiresArchogeneticsResearch()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Archite", 0, 1);
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("pack", true, target);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                true,
                100,
                1,
                false,
                pack);

            Assert.That(result.BlockerReasons, Does.Contain(PlanAssemblerBlockerReason.ArchogeneticsResearchMissing));
        }

        [Test]
        public void Analyze_CompletedArchogeneticsRemovesResearchBlocker()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Archite", 0, 1);
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("pack", true, target);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                true,
                100,
                1,
                true,
                pack);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Ready));
        }

        [Test]
        public void Analyze_InsufficientArchiteCapsulesReturnsBlocked()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Archite", 0, 2);
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("pack", true, target);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                true,
                100,
                1,
                true,
                pack);

            Assert.That(result.BlockerReasons, Does.Contain(PlanAssemblerBlockerReason.InsufficientArchiteCapsules));
            Assert.That(result.RequiredArchiteCapsules, Is.EqualTo(2));
            Assert.That(result.AvailableArchiteCapsules, Is.EqualTo(1));
        }

        [Test]
        public void Analyze_SufficientArchiteCapsulesRemovesCapsuleBlocker()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Archite", 0, 2);
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("pack", true, target);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                true,
                100,
                2,
                true,
                pack);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Ready));
        }

        [Test]
        public void Analyze_CoverageAdditionalArchiteGeneAffectsLiveRequirements()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Target");
            GeneDef extraArchite = PlanAssemblerReadinessTestData.CreateGene("ExtraArchite", 0, 1);
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("pack", true, target, extraArchite);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                true,
                100,
                0,
                false,
                pack);

            Assert.That(result.RequiredArchiteCapsules, Is.EqualTo(1));
            Assert.That(result.BlockerReasons, Does.Contain(PlanAssemblerBlockerReason.ArchogeneticsResearchMissing));
            Assert.That(result.BlockerReasons, Does.Contain(PlanAssemblerBlockerReason.InsufficientArchiteCapsules));
        }

        [Test]
        public void Analyze_AlternativePoweredCandidateCanMakeAssemblerReady()
        {
            GeneDef geneA = PlanAssemblerReadinessTestData.CreateGene("A");
            GeneDef geneB = PlanAssemblerReadinessTestData.CreateGene("B");
            PlanAssemblerReadinessTestData.PackFixture blockedPackA =
                PlanAssemblerReadinessTestData.CreatePack("a", false, geneA);
            PlanAssemblerReadinessTestData.PackFixture packB =
                PlanAssemblerReadinessTestData.CreatePack("b", true, geneB);
            PlanAssemblerReadinessTestData.PackFixture poweredCombined =
                PlanAssemblerReadinessTestData.CreatePack("ab", true, geneA, geneB);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB),
                true,
                100,
                100,
                true,
                blockedPackA,
                packB,
                poweredCombined);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Ready));
            Assert.That(result.CandidatePackCount, Is.EqualTo(1));
            Assert.That(PlanAssemblerReadinessTestData.ResultContainsCandidatePack(result, poweredCombined), Is.True);
        }

        [Test]
        public void Analyze_AlternativeLowerComplexityCandidateCanMakeAssemblerReady()
        {
            GeneDef geneA = PlanAssemblerReadinessTestData.CreateGene("A");
            GeneDef geneB = PlanAssemblerReadinessTestData.CreateGene("B");
            GeneDef duplicateExtra = PlanAssemblerReadinessTestData.CreateGene("0X", 5);
            PlanAssemblerReadinessTestData.PackFixture splitA =
                PlanAssemblerReadinessTestData.CreatePack("a-x", true, geneA, duplicateExtra);
            PlanAssemblerReadinessTestData.PackFixture splitB =
                PlanAssemblerReadinessTestData.CreatePack("b-x", true, geneB, duplicateExtra);
            PlanAssemblerReadinessTestData.PackFixture combined =
                PlanAssemblerReadinessTestData.CreatePack("ab-x", true, geneA, geneB, duplicateExtra);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB),
                true,
                6,
                100,
                true,
                splitA,
                splitB,
                combined);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Ready));
            Assert.That(result.RequiredComplexity, Is.EqualTo(5));
            Assert.That(result.CandidatePackCount, Is.EqualTo(1));
            Assert.That(PlanAssemblerReadinessTestData.ResultContainsCandidatePack(result, combined), Is.True);
        }

        [Test]
        public void Analyze_NoUsableCandidateReturnsDeterministicBestBlockedCandidate()
        {
            GeneDef geneA = PlanAssemblerReadinessTestData.CreateGene("A");
            GeneDef geneB = PlanAssemblerReadinessTestData.CreateGene("B");
            GeneDef duplicateExtra = PlanAssemblerReadinessTestData.CreateGene("0X", 5);
            PlanAssemblerReadinessTestData.PackFixture splitA =
                PlanAssemblerReadinessTestData.CreatePack("a-x", true, geneA, duplicateExtra);
            PlanAssemblerReadinessTestData.PackFixture splitB =
                PlanAssemblerReadinessTestData.CreatePack("b-x", true, geneB, duplicateExtra);
            PlanAssemblerReadinessTestData.PackFixture combined =
                PlanAssemblerReadinessTestData.CreatePack("ab-x", true, geneA, geneB, duplicateExtra);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB),
                true,
                4,
                100,
                true,
                splitA,
                splitB,
                combined);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Blocked));
            Assert.That(result.RequiredComplexity, Is.EqualTo(5));
            Assert.That(result.CandidatePackCount, Is.EqualTo(1));
            Assert.That(PlanAssemblerReadinessTestData.ResultContainsCandidatePack(result, combined), Is.True);
        }

        [Test]
        public void Analyze_TargetCoveringCandidateWithoutPrerequisiteReturnsBlocked()
        {
            GeneDef prerequisite = PlanAssemblerReadinessTestData.CreateGene("Prerequisite");
            GeneDef dependent = PlanAssemblerReadinessTestData.CreateGene("Dependent", prerequisite: prerequisite);
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("dependent", true, dependent);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, dependent),
                true,
                100,
                100,
                true,
                pack);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Blocked));
            Assert.That(result.BlockerReasons, Does.Contain(PlanAssemblerBlockerReason.MissingPrerequisite));
            Assert.That(result.MissingPrerequisites, Has.Count.EqualTo(1));
            Assert.That(result.MissingPrerequisites[0].DependentGene, Is.SameAs(dependent));
            Assert.That(result.MissingPrerequisites[0].PrerequisiteGene, Is.SameAs(prerequisite));
        }

        [Test]
        public void Analyze_CoverageUsesSeparatePrerequisitePackAndReturnsReady()
        {
            GeneDef prerequisite = PlanAssemblerReadinessTestData.CreateGene("Prerequisite");
            GeneDef dependent = PlanAssemblerReadinessTestData.CreateGene("Dependent", prerequisite: prerequisite);
            PlanAssemblerReadinessTestData.PackFixture dependentPack =
                PlanAssemblerReadinessTestData.CreatePack("dependent", true, dependent);
            PlanAssemblerReadinessTestData.PackFixture prerequisitePack =
                PlanAssemblerReadinessTestData.CreatePack("prerequisite", true, prerequisite);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, dependent),
                true,
                100,
                100,
                true,
                dependentPack,
                prerequisitePack);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Ready));
            Assert.That(result.MissingPrerequisites, Is.Empty);
            Assert.That(result.CandidatePackCount, Is.EqualTo(2));
        }

        [Test]
        public void Analyze_ExactPayloadWithoutPrerequisiteInTargetReturnsBlocked()
        {
            GeneDef prerequisite = PlanAssemblerReadinessTestData.CreateGene("Prerequisite");
            GeneDef dependent = PlanAssemblerReadinessTestData.CreateGene("Dependent", prerequisite: prerequisite);
            PlanAssemblerReadinessTestData.PackFixture dependentPack =
                PlanAssemblerReadinessTestData.CreatePack("dependent", true, dependent);
            PlanAssemblerReadinessTestData.PackFixture prerequisitePack =
                PlanAssemblerReadinessTestData.CreatePack("prerequisite", true, prerequisite);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, dependent),
                true,
                100,
                100,
                true,
                dependentPack,
                prerequisitePack);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Blocked));
            Assert.That(result.BlockerReasons, Does.Contain(PlanAssemblerBlockerReason.MissingPrerequisite));
            Assert.That(result.CandidatePackCount, Is.EqualTo(1));
        }

        [Test]
        public void Analyze_ExactPayloadWithPrerequisiteInTargetReturnsReady()
        {
            GeneDef prerequisite = PlanAssemblerReadinessTestData.CreateGene("Prerequisite");
            GeneDef dependent = PlanAssemblerReadinessTestData.CreateGene("Dependent", prerequisite: prerequisite);
            PlanAssemblerReadinessTestData.PackFixture dependentPack =
                PlanAssemblerReadinessTestData.CreatePack("dependent", true, dependent);
            PlanAssemblerReadinessTestData.PackFixture prerequisitePack =
                PlanAssemblerReadinessTestData.CreatePack("prerequisite", true, prerequisite);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, dependent, prerequisite),
                true,
                100,
                100,
                true,
                dependentPack,
                prerequisitePack);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Ready));
            Assert.That(result.MissingPrerequisites, Is.Empty);
        }

        [Test]
        public void Analyze_PrerequisiteCompleteBlockedCandidateIsPreferredOverIncompleteFallback()
        {
            GeneDef prerequisite = PlanAssemblerReadinessTestData.CreateGene("Prerequisite");
            GeneDef dependent = PlanAssemblerReadinessTestData.CreateGene("Dependent", prerequisite: prerequisite);
            PlanAssemblerReadinessTestData.PackFixture incomplete =
                PlanAssemblerReadinessTestData.CreatePack("a-incomplete", true, dependent);
            PlanAssemblerReadinessTestData.PackFixture complete =
                PlanAssemblerReadinessTestData.CreatePack("z-complete", false, dependent, prerequisite);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, dependent),
                true,
                100,
                100,
                true,
                incomplete,
                complete);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Blocked));
            Assert.That(result.MissingPrerequisites, Is.Empty);
            Assert.That(
                result.BlockerReasons,
                Is.EqualTo(
                [
                    PlanAssemblerBlockerReason.UsedGeneBankUnpowered
                ]));
            Assert.That(PlanAssemblerReadinessTestData.ResultContainsCandidatePack(result, complete), Is.True);
        }

        [Test]
        public void Analyze_MultipleMissingPrerequisitesAreDistinctAndOrdered()
        {
            GeneDef prerequisiteA = PlanAssemblerReadinessTestData.CreateGene("PrerequisiteA");
            GeneDef prerequisiteB = PlanAssemblerReadinessTestData.CreateGene("PrerequisiteB");
            GeneDef dependentB = PlanAssemblerReadinessTestData.CreateGene("DependentB", prerequisite: prerequisiteB);
            GeneDef dependentA = PlanAssemblerReadinessTestData.CreateGene("DependentA", prerequisite: prerequisiteA);
            PlanAssemblerReadinessTestData.PackFixture packA =
                PlanAssemblerReadinessTestData.CreatePack("a", true, dependentA);
            PlanAssemblerReadinessTestData.PackFixture packB =
                PlanAssemblerReadinessTestData.CreatePack("b", true, dependentB);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, dependentB, dependentA),
                true,
                100,
                100,
                true,
                packB,
                packA);

            Assert.That(
                result.MissingPrerequisites.Select(diagnostic => diagnostic.DependentGene.defName),
                Is.EqualTo(["DependentA", "DependentB"]));
        }

        [Test]
        public void Analyze_MissingPrerequisiteCombinesWithInfrastructureBlockers()
        {
            GeneDef prerequisite = PlanAssemblerReadinessTestData.CreateGene("Prerequisite");
            GeneDef dependent = PlanAssemblerReadinessTestData.CreateGene(
                "Dependent",
                complexity: 10,
                prerequisite: prerequisite);
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("dependent", false, dependent);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, dependent),
                false,
                1,
                100,
                true,
                pack);

            Assert.That(
                result.BlockerReasons,
                Is.EqualTo(
                [
                    PlanAssemblerBlockerReason.MissingPrerequisite,
                    PlanAssemblerBlockerReason.AssemblerUnpowered,
                    PlanAssemblerBlockerReason.UsedGeneBankUnpowered,
                    PlanAssemblerBlockerReason.InsufficientComplexity
                ]));
        }

        [Test]
        public void Analyze_ReadyResultCannotRetainMissingPrerequisites()
        {
            GeneDef prerequisite = PlanAssemblerReadinessTestData.CreateGene("Prerequisite");
            GeneDef dependent = PlanAssemblerReadinessTestData.CreateGene("Dependent", prerequisite: prerequisite);
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("complete", true, dependent, prerequisite);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, dependent),
                true,
                100,
                100,
                true,
                pack);

            Assert.That(result.Status, Is.EqualTo(PlanAssemblerReadinessStatus.Ready));
            Assert.That(result.MissingPrerequisites, Is.Empty);
            Assert.That(result.BlockerReasons, Is.Empty);
        }

        [Test]
        public void Analyze_BlockersAreDistinctAndUseStableOrder()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Archite", 10, 2);
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("pack", false, target);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                false,
                1,
                0,
                false,
                pack);

            Assert.That(
                result.BlockerReasons,
                Is.EqualTo(
                [
                    PlanAssemblerBlockerReason.AssemblerUnpowered,
                    PlanAssemblerBlockerReason.UsedGeneBankUnpowered,
                    PlanAssemblerBlockerReason.InsufficientComplexity,
                    PlanAssemblerBlockerReason.ArchogeneticsResearchMissing,
                    PlanAssemblerBlockerReason.InsufficientArchiteCapsules
                ]));
            Assert.That(result.BlockerReasons.Distinct().Count(), Is.EqualTo(result.BlockerReasons.Count));
        }

        [Test]
        public void Analyze_ResultRetainsExactPhysicalCandidateReferences()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Target");
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("pack", true, target);

            PlanAssemblerReadinessResult result = Analyze(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                true,
                100,
                100,
                true,
                pack);

            Assert.That(result.CandidateGenepacks, Has.Count.EqualTo(1));
            Assert.That(result.CandidateGenepacks[0], Is.SameAs(pack.Genepack));
        }

        [Test]
        public void Analyze_FreshScopeDoesNotReuseDisconnectedPhysicalCandidate()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Target");
            PlanAssemblerReadinessTestData.PackFixture disconnected =
                PlanAssemblerReadinessTestData.CreatePack("old-pack", true, target);
            PlanAssemblerReadinessTestData.PackFixture replacement =
                PlanAssemblerReadinessTestData.CreatePack("new-pack", true, target);
            XenogermPlan plan = PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target);
            PlanAssemblerLiveState liveState =
                PlanAssemblerReadinessTestData.CreateLiveState(true, 100, 100, true, replacement);

            PlanAssemblerReadinessResult result = PlanAssemblerReadinessTestData.Analyze(
                plan,
                liveState,
                disconnected,
                replacement);

            Assert.That(PlanAssemblerReadinessTestData.ResultContainsCandidatePack(result, disconnected), Is.False);
            Assert.That(PlanAssemblerReadinessTestData.ResultContainsCandidatePack(result, replacement), Is.True);
        }

        private static PlanAssemblerReadinessResult Analyze(
            XenogermPlan plan,
            bool assemblerPowerOn,
            int maxComplexity,
            int availableArchiteCapsules,
            bool archogeneticsFinished,
            params PlanAssemblerReadinessTestData.PackFixture[] packs)
        {
            PlanAssemblerLiveState liveState = PlanAssemblerReadinessTestData.CreateLiveState(
                assemblerPowerOn,
                maxComplexity,
                availableArchiteCapsules,
                archogeneticsFinished,
                packs);

            return PlanAssemblerReadinessTestData.Analyze(plan, liveState, packs);
        }
    }
}