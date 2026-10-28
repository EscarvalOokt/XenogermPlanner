using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Assemblers;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Assemblers
{
    [TestFixture]
    public sealed class PlanAssemblerCandidateSearcherTests
    {
        [Test]
        public void Search_CoverageCandidateCoversAllTargetGenes()
        {
            GeneDef geneA = PlanAssemblerReadinessTestData.CreateGene("A");
            GeneDef geneB = PlanAssemblerReadinessTestData.CreateGene("B");
            PlanAssemblerReadinessTestData.PackFixture packA =
                PlanAssemblerReadinessTestData.CreatePack("pack-a", true, geneA);
            PlanAssemblerReadinessTestData.PackFixture packB =
                PlanAssemblerReadinessTestData.CreatePack("pack-b", true, geneB);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB),
                packA,
                packB);

            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(PlanAssemblerReadinessTestData.CandidateContains(candidates[0], packA), Is.True);
            Assert.That(PlanAssemblerReadinessTestData.CandidateContains(candidates[0], packB), Is.True);
        }

        [Test]
        public void Search_CoverageAllowsAdditionalGenes()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Target");
            GeneDef extra = PlanAssemblerReadinessTestData.CreateGene("Extra");
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("pack", true, target, extra);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                pack);

            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(candidates[0].Sources, Has.Count.EqualTo(1));
            Assert.That(candidates[0].Sources[0].Genepack, Is.SameAs(pack.Genepack));
        }

        [Test]
        public void Search_IgnoresPackWithoutDesiredGeneContribution()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Target");
            GeneDef unrelated = PlanAssemblerReadinessTestData.CreateGene("Other");
            PlanAssemblerReadinessTestData.PackFixture targetPack =
                PlanAssemblerReadinessTestData.CreatePack("target-pack", true, target);
            PlanAssemblerReadinessTestData.PackFixture unrelatedPack =
                PlanAssemblerReadinessTestData.CreatePack("other-pack", true, unrelated);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                unrelatedPack,
                targetPack);

            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(candidates[0].Sources, Has.Count.EqualTo(1));
            Assert.That(candidates[0].Sources[0].Genepack, Is.SameAs(targetPack.Genepack));
        }

        [Test]
        public void Search_ExactPayloadExcludesPackWithAdditionalGene()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Target");
            GeneDef extra = PlanAssemblerReadinessTestData.CreateGene("Extra");
            PlanAssemblerReadinessTestData.PackFixture exactPack =
                PlanAssemblerReadinessTestData.CreatePack("exact-pack", true, target);
            PlanAssemblerReadinessTestData.PackFixture extraPack =
                PlanAssemblerReadinessTestData.CreatePack("extra-pack", true, target, extra);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, target),
                extraPack,
                exactPack);

            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(candidates[0].Sources, Has.Count.EqualTo(1));
            Assert.That(candidates[0].Sources[0].Genepack, Is.SameAs(exactPack.Genepack));
        }

        [Test]
        public void Search_EmitsOnlyTargetIrredundantCandidates()
        {
            GeneDef geneA = PlanAssemblerReadinessTestData.CreateGene("A");
            GeneDef geneB = PlanAssemblerReadinessTestData.CreateGene("B");
            PlanAssemblerReadinessTestData.PackFixture packA =
                PlanAssemblerReadinessTestData.CreatePack("pack-a", true, geneA);
            PlanAssemblerReadinessTestData.PackFixture packAb =
                PlanAssemblerReadinessTestData.CreatePack("pack-ab", true, geneA, geneB);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB),
                packA,
                packAb);

            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(candidates[0].Sources, Has.Count.EqualTo(1));
            Assert.That(candidates[0].Sources[0].Genepack, Is.SameAs(packAb.Genepack));
        }

        [Test]
        public void Search_EquivalentCompositionsDoNotCreateDuplicateBranches()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Target");
            PlanAssemblerReadinessTestData.PackFixture first =
                PlanAssemblerReadinessTestData.CreatePack("first", true, target);
            PlanAssemblerReadinessTestData.PackFixture second =
                PlanAssemblerReadinessTestData.CreatePack("second", true, target);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                first,
                second);

            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(candidates[0].Sources, Has.Count.EqualTo(1));
        }

        [Test]
        public void Search_EquivalentCompositionPrefersPoweredPhysicalPack()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Target");
            PlanAssemblerReadinessTestData.PackFixture unpowered =
                PlanAssemblerReadinessTestData.CreatePack("a-unpowered", false, target);
            PlanAssemblerReadinessTestData.PackFixture powered =
                PlanAssemblerReadinessTestData.CreatePack("z-powered", true, target);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                unpowered,
                powered);

            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(candidates[0].Sources[0].Genepack, Is.SameAs(powered.Genepack));
        }

        [Test]
        public void Search_EquivalentPoweredPacksUseStablePhysicalTieBreak()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Target");
            PlanAssemblerReadinessTestData.PackFixture later =
                PlanAssemblerReadinessTestData.CreatePack("z-pack", true, target);
            PlanAssemblerReadinessTestData.PackFixture earlier =
                PlanAssemblerReadinessTestData.CreatePack("a-pack", true, target);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                later,
                earlier);

            Assert.That(candidates[0].Sources[0].Genepack, Is.SameAs(earlier.Genepack));
        }

        [Test]
        public void Search_EnumeratesMultipleValidConcreteCombinations()
        {
            GeneDef geneA = PlanAssemblerReadinessTestData.CreateGene("A");
            GeneDef geneB = PlanAssemblerReadinessTestData.CreateGene("B");
            PlanAssemblerReadinessTestData.PackFixture packA =
                PlanAssemblerReadinessTestData.CreatePack("a", true, geneA);
            PlanAssemblerReadinessTestData.PackFixture packB =
                PlanAssemblerReadinessTestData.CreatePack("b", true, geneB);
            PlanAssemblerReadinessTestData.PackFixture packAb =
                PlanAssemblerReadinessTestData.CreatePack("ab", true, geneA, geneB);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB),
                packA,
                packB,
                packAb);

            Assert.That(candidates, Has.Count.EqualTo(2));
            Assert.That(candidates.Any(candidate => candidate.Sources.Count == 1), Is.True);
            Assert.That(candidates.Any(candidate => candidate.Sources.Count == 2), Is.True);
        }

        [Test]
        public void Search_PrerequisiteInSamePackProducesCompleteCandidate()
        {
            GeneDef prerequisite = PlanAssemblerReadinessTestData.CreateGene("Prerequisite");
            GeneDef dependent = PlanAssemblerReadinessTestData.CreateGene("Dependent", prerequisite: prerequisite);
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("pack", true, dependent, prerequisite);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, dependent),
                pack);

            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(candidates[0].IsPrerequisiteComplete, Is.True);
            Assert.That(candidates[0].Sources, Has.Count.EqualTo(1));
        }

        [Test]
        public void Search_CoverageAddsSeparatePrerequisiteOnlyPack()
        {
            GeneDef prerequisite = PlanAssemblerReadinessTestData.CreateGene("Prerequisite");
            GeneDef dependent = PlanAssemblerReadinessTestData.CreateGene("Dependent", prerequisite: prerequisite);
            PlanAssemblerReadinessTestData.PackFixture dependentPack =
                PlanAssemblerReadinessTestData.CreatePack("dependent", true, dependent);
            PlanAssemblerReadinessTestData.PackFixture prerequisitePack =
                PlanAssemblerReadinessTestData.CreatePack("prerequisite", true, prerequisite);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, dependent),
                dependentPack,
                prerequisitePack);

            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(candidates[0].IsPrerequisiteComplete, Is.True);
            Assert.That(PlanAssemblerReadinessTestData.CandidateContains(candidates[0], dependentPack), Is.True);
            Assert.That(PlanAssemblerReadinessTestData.CandidateContains(candidates[0], prerequisitePack), Is.True);
        }

        [Test]
        public void Search_ExactPayloadDoesNotAddPrerequisiteOutsideTarget()
        {
            GeneDef prerequisite = PlanAssemblerReadinessTestData.CreateGene("Prerequisite");
            GeneDef dependent = PlanAssemblerReadinessTestData.CreateGene("Dependent", prerequisite: prerequisite);
            PlanAssemblerReadinessTestData.PackFixture dependentPack =
                PlanAssemblerReadinessTestData.CreatePack("dependent", true, dependent);
            PlanAssemblerReadinessTestData.PackFixture prerequisitePack =
                PlanAssemblerReadinessTestData.CreatePack("prerequisite", true, prerequisite);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, dependent),
                dependentPack,
                prerequisitePack);

            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(candidates[0].IsPrerequisiteComplete, Is.False);
            Assert.That(candidates[0].Sources, Has.Count.EqualTo(1));
            Assert.That(candidates[0].MissingPrerequisites, Has.Count.EqualTo(1));
            Assert.That(candidates[0].MissingPrerequisites[0].PrerequisiteGene, Is.SameAs(prerequisite));
        }

        [Test]
        public void Search_ExactPayloadCompletesPrerequisiteIncludedInTarget()
        {
            GeneDef prerequisite = PlanAssemblerReadinessTestData.CreateGene("Prerequisite");
            GeneDef dependent = PlanAssemblerReadinessTestData.CreateGene("Dependent", prerequisite: prerequisite);
            PlanAssemblerReadinessTestData.PackFixture dependentPack =
                PlanAssemblerReadinessTestData.CreatePack("dependent", true, dependent);
            PlanAssemblerReadinessTestData.PackFixture prerequisitePack =
                PlanAssemblerReadinessTestData.CreatePack("prerequisite", true, prerequisite);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, dependent, prerequisite),
                dependentPack,
                prerequisitePack);

            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(candidates[0].IsPrerequisiteComplete, Is.True);
            Assert.That(candidates[0].Sources, Has.Count.EqualTo(2));
        }

        [Test]
        public void Search_CoverageChecksPrerequisiteOfAdditionalSelectedGene()
        {
            GeneDef prerequisite = PlanAssemblerReadinessTestData.CreateGene("Prerequisite");
            GeneDef additionalDependent = PlanAssemblerReadinessTestData.CreateGene(
                "AdditionalDependent",
                prerequisite: prerequisite);
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Target");
            PlanAssemblerReadinessTestData.PackFixture targetPack =
                PlanAssemblerReadinessTestData.CreatePack("target", true, target, additionalDependent);
            PlanAssemblerReadinessTestData.PackFixture prerequisitePack =
                PlanAssemblerReadinessTestData.CreatePack("prerequisite", true, prerequisite);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                targetPack,
                prerequisitePack);

            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(candidates[0].IsPrerequisiteComplete, Is.True);
            Assert.That(PlanAssemblerReadinessTestData.CandidateContains(candidates[0], prerequisitePack), Is.True);
        }

        [Test]
        public void Search_ResolvesRecursivePrerequisiteClosure()
        {
            GeneDef root = PlanAssemblerReadinessTestData.CreateGene("Root");
            GeneDef middle = PlanAssemblerReadinessTestData.CreateGene("Middle", prerequisite: root);
            GeneDef dependent = PlanAssemblerReadinessTestData.CreateGene("Dependent", prerequisite: middle);
            PlanAssemblerReadinessTestData.PackFixture dependentPack =
                PlanAssemblerReadinessTestData.CreatePack("dependent", true, dependent);
            PlanAssemblerReadinessTestData.PackFixture middlePack =
                PlanAssemblerReadinessTestData.CreatePack("middle", true, middle);
            PlanAssemblerReadinessTestData.PackFixture rootPack =
                PlanAssemblerReadinessTestData.CreatePack("root", true, root);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, dependent),
                dependentPack,
                middlePack,
                rootPack);

            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(candidates[0].IsPrerequisiteComplete, Is.True);
            Assert.That(candidates[0].Sources, Has.Count.EqualTo(3));
        }

        [Test]
        public void Search_CyclicPrerequisitesTerminateWithCompleteCandidate()
        {
            GeneDef geneA = PlanAssemblerReadinessTestData.CreateGene("A");
            GeneDef geneB = PlanAssemblerReadinessTestData.CreateGene("B");
            geneA.prerequisite = geneB;
            geneB.prerequisite = geneA;
            PlanAssemblerReadinessTestData.PackFixture packA =
                PlanAssemblerReadinessTestData.CreatePack("a", true, geneA);
            PlanAssemblerReadinessTestData.PackFixture packB =
                PlanAssemblerReadinessTestData.CreatePack("b", true, geneB);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA),
                packA,
                packB);

            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(candidates[0].IsPrerequisiteComplete, Is.True);
            Assert.That(candidates[0].Sources, Has.Count.EqualTo(2));
        }

        [Test]
        public void Search_MissingPrerequisiteProducesDeterministicFallbackCandidate()
        {
            GeneDef prerequisite = PlanAssemblerReadinessTestData.CreateGene("Prerequisite");
            GeneDef dependent = PlanAssemblerReadinessTestData.CreateGene("Dependent", prerequisite: prerequisite);
            PlanAssemblerReadinessTestData.PackFixture dependentPack =
                PlanAssemblerReadinessTestData.CreatePack("dependent", true, dependent);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, dependent),
                dependentPack);

            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(candidates[0].IsPrerequisiteComplete, Is.False);
            Assert.That(candidates[0].MissingPrerequisites, Has.Count.EqualTo(1));
            Assert.That(candidates[0].MissingPrerequisites[0].DependentGene, Is.SameAs(dependent));
        }

        [Test]
        public void Search_InputOrderDoesNotChangeDeterministicCandidates()
        {
            GeneDef geneA = PlanAssemblerReadinessTestData.CreateGene("A");
            GeneDef geneB = PlanAssemblerReadinessTestData.CreateGene("B");
            PlanAssemblerReadinessTestData.PackFixture packA =
                PlanAssemblerReadinessTestData.CreatePack("a", true, geneA);
            PlanAssemblerReadinessTestData.PackFixture packB =
                PlanAssemblerReadinessTestData.CreatePack("b", true, geneB);
            PlanAssemblerReadinessTestData.PackFixture packAb =
                PlanAssemblerReadinessTestData.CreatePack("ab", true, geneA, geneB);
            XenogermPlan plan = PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB);

            IReadOnlyList<PlanAssemblerCandidate> first = PlanAssemblerReadinessTestData.Search(
                plan,
                packA,
                packB,
                packAb);
            IReadOnlyList<PlanAssemblerCandidate> second =
                PlanAssemblerReadinessTestData.Search(plan, packAb, packB, packA);

            Assert.That(
                CreateCandidateKeys(first, packA, packB, packAb),
                Is.EqualTo(CreateCandidateKeys(second, packA, packB, packAb)));
        }

        [Test]
        public void Search_EmptyTargetReturnsNoCandidates()
        {
            GeneDef gene = PlanAssemblerReadinessTestData.CreateGene("A");
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("pack", true, gene);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage),
                pack);

            Assert.That(candidates, Is.Empty);
        }

        [Test]
        public void Search_MissingTargetGeneReturnsNoCandidates()
        {
            GeneDef target = PlanAssemblerReadinessTestData.CreateGene("Target");
            GeneDef other = PlanAssemblerReadinessTestData.CreateGene("Other");
            PlanAssemblerReadinessTestData.PackFixture pack =
                PlanAssemblerReadinessTestData.CreatePack("pack", true, other);

            IReadOnlyList<PlanAssemblerCandidate> candidates = PlanAssemblerReadinessTestData.Search(
                PlanAssemblerReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, target),
                pack);

            Assert.That(candidates, Is.Empty);
        }

        private static string[] CreateCandidateKeys(
            IEnumerable<PlanAssemblerCandidate> candidates,
            params PlanAssemblerReadinessTestData.PackFixture[] packs)
        {
            var keysByPack = packs.ToDictionary(
                pack => pack.Genepack,
                pack => pack.PhysicalKey,
                ReferenceEqualityComparer<Genepack>.Instance);

            return candidates.Select(candidate => string.Join(
                    "+",
                    candidate.Sources.Select(source => keysByPack[source.Genepack])))
                .ToArray();
        }
    }
}