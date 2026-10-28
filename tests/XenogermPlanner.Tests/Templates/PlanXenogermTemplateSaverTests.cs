using System.Collections.Generic;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Plans;
using XenogermPlanner.Templates;

namespace XenogermPlanner.Tests.Templates
{
    [TestFixture]
    public sealed class PlanXenogermTemplateSaverTests
    {
        [Test]
        public void Save_ResolvesSingleExactComposition()
        {
            GeneDef target = PlanXenogermTemplateTestData.CreateGene("Target");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.ExactPayload, target);
            PlanXenogermTemplateCandidate candidate = PlanXenogermTemplateTestData.CreateCandidate(plan, [target]);
            PlanXenogermTemplateTestData.PackFixture pack = PlanXenogermTemplateTestData.CreatePack("pack", target);
            List<Genepack> captured = null;

            PlanXenogermTemplateSaveResult result = PlanXenogermTemplateTestData.Save(
                plan,
                candidate,
                inventoryAvailable: true,
                [pack],
                (_, _, selected) =>
                {
                    captured = selected;
                    return true;
                });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(captured, Has.Count.EqualTo(1));
            Assert.That(captured[0], Is.SameAs(pack.Genepack));
        }

        [Test]
        public void Save_PassesCompositionsInStableCandidateOrder()
        {
            GeneDef geneA = PlanXenogermTemplateTestData.CreateGene("A");
            GeneDef geneB = PlanXenogermTemplateTestData.CreateGene("B");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.ExactPayload, geneA, geneB);
            PlanXenogermTemplateCandidate candidate = PlanXenogermTemplateTestData.CreateCandidate(
                plan,
                [geneB],
                [geneA]);
            PlanXenogermTemplateTestData.PackFixture packB = PlanXenogermTemplateTestData.CreatePack("b", geneB);
            PlanXenogermTemplateTestData.PackFixture packA = PlanXenogermTemplateTestData.CreatePack("a", geneA);
            List<Genepack> captured = null;

            PlanXenogermTemplateTestData.Save(
                plan,
                candidate,
                inventoryAvailable: true,
                [packB, packA],
                (_, _, selected) =>
                {
                    captured = selected;
                    return true;
                });

            Assert.That(captured[0], Is.SameAs(packA.Genepack));
            Assert.That(captured[1], Is.SameAs(packB.Genepack));
        }

        [Test]
        public void Save_SelectsLowestPhysicalKeyAmongEquivalentPacks()
        {
            GeneDef target = PlanXenogermTemplateTestData.CreateGene("Target");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, target);
            PlanXenogermTemplateCandidate candidate = PlanXenogermTemplateTestData.CreateCandidate(plan, [target]);
            PlanXenogermTemplateTestData.PackFixture later = PlanXenogermTemplateTestData.CreatePack("z-pack", target);
            PlanXenogermTemplateTestData.PackFixture earlier =
                PlanXenogermTemplateTestData.CreatePack("a-pack", target);
            List<Genepack> captured = null;

            PlanXenogermTemplateTestData.Save(
                plan,
                candidate,
                inventoryAvailable: true,
                [later, earlier],
                (_, _, selected) =>
                {
                    captured = selected;
                    return true;
                });

            Assert.That(captured[0], Is.SameAs(earlier.Genepack));
        }

        [Test]
        public void Save_PhysicalSelectionIsIndependentOfInventoryOrder()
        {
            GeneDef target = PlanXenogermTemplateTestData.CreateGene("Target");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, target);
            PlanXenogermTemplateCandidate candidate = PlanXenogermTemplateTestData.CreateCandidate(plan, [target]);
            PlanXenogermTemplateTestData.PackFixture first = PlanXenogermTemplateTestData.CreatePack("a", target);
            PlanXenogermTemplateTestData.PackFixture second = PlanXenogermTemplateTestData.CreatePack("b", target);
            Genepack selectedFromFirstOrder = null;
            Genepack selectedFromSecondOrder = null;

            PlanXenogermTemplateTestData.Save(
                plan,
                candidate,
                true,
                [second, first],
                (_, _, selected) =>
                {
                    selectedFromFirstOrder = selected[0];
                    return true;
                });
            PlanXenogermTemplateTestData.Save(
                plan,
                candidate,
                true,
                [first, second],
                (_, _, selected) =>
                {
                    selectedFromSecondOrder = selected[0];
                    return true;
                });

            Assert.That(selectedFromFirstOrder, Is.SameAs(first.Genepack));
            Assert.That(selectedFromSecondOrder, Is.SameAs(first.Genepack));
        }

        [Test]
        public void Save_UsesEquivalentReplacementWhenOriginalPhysicalPackIsGone()
        {
            GeneDef target = PlanXenogermTemplateTestData.CreateGene("Target");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, target);
            PlanXenogermTemplateCandidate candidate = PlanXenogermTemplateTestData.CreateCandidate(plan, [target]);
            PlanXenogermTemplateTestData.PackFixture replacement =
                PlanXenogermTemplateTestData.CreatePack("replacement", target);
            Genepack selected = null;

            PlanXenogermTemplateSaveResult result = PlanXenogermTemplateTestData.Save(
                plan,
                candidate,
                true,
                [replacement],
                (_, _, packs) =>
                {
                    selected = packs[0];
                    return true;
                });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(selected, Is.SameAs(replacement.Genepack));
        }

        [Test]
        public void Save_ReturnsCompositionUnavailableWithoutCallingVanillaHelper()
        {
            GeneDef target = PlanXenogermTemplateTestData.CreateGene("Target");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, target);
            PlanXenogermTemplateCandidate candidate = PlanXenogermTemplateTestData.CreateCandidate(plan, [target]);
            var called = false;

            PlanXenogermTemplateSaveResult result = PlanXenogermTemplateTestData.Save(
                plan,
                candidate,
                true,
                [],
                (_, _, _) =>
                {
                    called = true;
                    return true;
                });

            Assert.That(result.Failure, Is.EqualTo(PlanXenogermTemplateSaveFailure.CompositionUnavailable));
            Assert.That(called, Is.False);
        }

        [Test]
        public void Save_RejectsExactCandidateWithAdditionalGene()
        {
            GeneDef target = PlanXenogermTemplateTestData.CreateGene("Target");
            GeneDef extra = PlanXenogermTemplateTestData.CreateGene("Extra");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.ExactPayload, target);
            PlanXenogermTemplateCandidate candidate =
                PlanXenogermTemplateTestData.CreateCandidate(plan, [target, extra]);
            var called = false;

            PlanXenogermTemplateSaveResult result = PlanXenogermTemplateTestData.Save(
                plan,
                candidate,
                true,
                [PlanXenogermTemplateTestData.CreatePack("pack", target, extra)],
                (_, _, _) =>
                {
                    called = true;
                    return true;
                });

            Assert.That(result.Failure, Is.EqualTo(PlanXenogermTemplateSaveFailure.CandidateInvalidForPlan));
            Assert.That(called, Is.False);
        }

        [Test]
        public void Save_RejectsCoverageCandidateMissingTargetGene()
        {
            GeneDef geneA = PlanXenogermTemplateTestData.CreateGene("A");
            GeneDef geneB = PlanXenogermTemplateTestData.CreateGene("B");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, geneA, geneB);
            PlanXenogermTemplateCandidate candidate = PlanXenogermTemplateTestData.CreateCandidate(plan, [geneA]);

            PlanXenogermTemplateSaveResult result = PlanXenogermTemplateTestData.Save(
                plan,
                candidate,
                true,
                [PlanXenogermTemplateTestData.CreatePack("a", geneA)],
                (_, _, _) => true);

            Assert.That(result.Failure, Is.EqualTo(PlanXenogermTemplateSaveFailure.CandidateInvalidForPlan));
        }

        [Test]
        public void Save_RejectsCandidateAfterPlanTargetChanges()
        {
            GeneDef original = PlanXenogermTemplateTestData.CreateGene("Original");
            GeneDef replacement = PlanXenogermTemplateTestData.CreateGene("Replacement");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, original);
            PlanXenogermTemplateCandidate candidate = PlanXenogermTemplateTestData.CreateCandidate(plan, [original]);
            plan.ReplaceDesiredGenes([replacement]);

            PlanXenogermTemplateSaveResult result = PlanXenogermTemplateTestData.Save(
                plan,
                candidate,
                true,
                [PlanXenogermTemplateTestData.CreatePack("original", original)],
                (_, _, _) => true);

            Assert.That(result.Failure, Is.EqualTo(PlanXenogermTemplateSaveFailure.CandidateInvalidForPlan));
        }

        [Test]
        public void Save_ReturnsInventoryUnavailableBeforeResolvingPacks()
        {
            GeneDef target = PlanXenogermTemplateTestData.CreateGene("Target");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, target);
            PlanXenogermTemplateCandidate candidate = PlanXenogermTemplateTestData.CreateCandidate(plan, [target]);

            PlanXenogermTemplateSaveResult result = PlanXenogermTemplateTestData.Save(
                plan,
                candidate,
                inventoryAvailable: false,
                [PlanXenogermTemplateTestData.CreatePack("pack", target)],
                (_, _, _) => true);

            Assert.That(result.Failure, Is.EqualTo(PlanXenogermTemplateSaveFailure.InventoryUnavailable));
        }

        [Test]
        public void Save_PassesTemplateNameAndIconToVanillaHelper()
        {
            GeneDef target = PlanXenogermTemplateTestData.CreateGene("Target");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, target);
            PlanXenogermTemplateCandidate candidate = PlanXenogermTemplateTestData.CreateCandidate(plan, [target]);
            var icon = new XenotypeIconDef();
            string capturedName = null;
            XenotypeIconDef capturedIcon = null;

            PlanXenogermTemplateSaveResult result = PlanXenogermTemplateTestData.Save(
                plan,
                candidate,
                "Expected template",
                icon,
                true,
                [PlanXenogermTemplateTestData.CreatePack("pack", target)],
                (name, receivedIcon, _) =>
                {
                    capturedName = name;
                    capturedIcon = receivedIcon;
                    return true;
                });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(capturedName, Is.EqualTo("Expected template"));
            Assert.That(capturedIcon, Is.SameAs(icon));
        }

        [Test]
        public void Save_MapsVanillaRejection()
        {
            GeneDef target = PlanXenogermTemplateTestData.CreateGene("Target");
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, target);
            PlanXenogermTemplateCandidate candidate = PlanXenogermTemplateTestData.CreateCandidate(plan, [target]);

            PlanXenogermTemplateSaveResult result = PlanXenogermTemplateTestData.Save(
                plan,
                candidate,
                true,
                [PlanXenogermTemplateTestData.CreatePack("pack", target)],
                (_, _, _) => "Rejected by vanilla");

            Assert.That(result.Failure, Is.EqualTo(PlanXenogermTemplateSaveFailure.VanillaRejected));
            Assert.That(result.VanillaRejectionReason, Is.EqualTo("Rejected by vanilla"));
        }
    }
}