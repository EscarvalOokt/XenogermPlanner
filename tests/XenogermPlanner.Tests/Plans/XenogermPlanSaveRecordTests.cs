using System.Linq;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Plans
{
    [TestFixture]
    public sealed class XenogermPlanSaveRecordTests
    {
        [Test]
        public void RoundTrip_PreservesEmptyPlan()
        {
            var sourcePlan = new XenogermPlan("Empty", [], PlanReadinessMode.Coverage);
            var saveRecord = XenogermPlanSaveRecord.FromPlan(sourcePlan);

            bool restored = saveRecord.TryCreatePlan(
                PlanTestData.CreateResolver(),
                out XenogermPlan restoredPlan,
                out string failureReason);

            Assert.That(restored, Is.True, failureReason);
            Assert.That(restoredPlan, Is.Not.Null);
            Assert.That(restoredPlan.Id, Is.EqualTo(sourcePlan.Id));
            Assert.That(restoredPlan.Name, Is.EqualTo(sourcePlan.Name));
            Assert.That(restoredPlan.DesiredGenes, Is.Empty);
            Assert.That(restoredPlan.UnresolvedDesiredGeneDefNames, Is.Empty);
            Assert.That(restoredPlan.IsDegraded, Is.False);
            Assert.That(restoredPlan.ReadinessMode, Is.EqualTo(PlanReadinessMode.Coverage));
        }

        [TestCase(PlanReadinessMode.Coverage)]
        [TestCase(PlanReadinessMode.ExactPayload)]
        public void RoundTrip_PreservesPopulatedPlan(PlanReadinessMode readinessMode)
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            var sourcePlan = new XenogermPlan("Plan", [geneA, geneB], readinessMode);
            var saveRecord = XenogermPlanSaveRecord.FromPlan(sourcePlan);

            bool restored = saveRecord.TryCreatePlan(
                PlanTestData.CreateResolver(geneA, geneB),
                out XenogermPlan restoredPlan,
                out string failureReason);

            Assert.That(restored, Is.True, failureReason);
            Assert.That(restoredPlan.Id, Is.EqualTo(sourcePlan.Id));
            Assert.That(restoredPlan.Name, Is.EqualTo(sourcePlan.Name));
            Assert.That(restoredPlan.DesiredGenes.Select(gene => gene.defName), Is.EquivalentTo(["GeneA", "GeneB"]));
            Assert.That(restoredPlan.IsDegraded, Is.False);
            Assert.That(restoredPlan.ReadinessMode, Is.EqualTo(readinessMode));
        }

        [Test]
        public void TryCreatePlan_NormalizesDuplicatePersistedGeneNames()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            var saveRecord = new XenogermPlanSaveRecord(
                "stable-id",
                "Plan",
                ["GeneA", "GeneA", "GeneB"],
                (int)PlanReadinessMode.Coverage);

            bool restored = saveRecord.TryCreatePlan(
                PlanTestData.CreateResolver(geneA, geneB),
                out XenogermPlan plan,
                out string failureReason);

            Assert.That(restored, Is.True, failureReason);
            Assert.That(plan.DesiredGenes.Select(gene => gene.defName), Is.EquivalentTo(["GeneA", "GeneB"]));
            Assert.That(plan.DesiredGenes.Count, Is.EqualTo(2));
        }

        [Test]
        public void TryCreatePlan_PreservesMissingGeneAsDegradedRequirement()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            var saveRecord = new XenogermPlanSaveRecord(
                "stable-id",
                "Plan",
                ["GeneA", "MissingGene"],
                (int)PlanReadinessMode.Coverage);

            bool restored = saveRecord.TryCreatePlan(
                PlanTestData.CreateResolver(geneA),
                out XenogermPlan plan,
                out string failureReason);

            Assert.That(restored, Is.True, failureReason);
            Assert.That(plan.DesiredGenes.Select(gene => gene.defName), Is.EquivalentTo(["GeneA"]));
            Assert.That(plan.UnresolvedDesiredGeneDefNames, Is.EquivalentTo(["MissingGene"]));
            Assert.That(plan.IsDegraded, Is.True);
        }

        [Test]
        public void RoundTrip_PreservesUnresolvedRequirementAcrossDegradedResave()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            var sourceRecord = new XenogermPlanSaveRecord(
                "stable-id",
                "Plan",
                ["GeneA", "MissingGene"],
                (int)PlanReadinessMode.Coverage);

            bool firstRestore = sourceRecord.TryCreatePlan(
                PlanTestData.CreateResolver(geneA),
                out XenogermPlan degradedPlan,
                out string firstFailureReason);
            var resavedRecord = XenogermPlanSaveRecord.FromPlan(degradedPlan);
            bool secondRestore = resavedRecord.TryCreatePlan(
                PlanTestData.CreateResolver(geneA),
                out XenogermPlan restoredDegradedPlan,
                out string secondFailureReason);

            Assert.That(firstRestore, Is.True, firstFailureReason);
            Assert.That(secondRestore, Is.True, secondFailureReason);
            Assert.That(restoredDegradedPlan.IsDegraded, Is.True);
            Assert.That(restoredDegradedPlan.UnresolvedDesiredGeneDefNames, Is.EquivalentTo(["MissingGene"]));
        }

        [Test]
        public void TryCreatePlan_ResolvesPreviouslyMissingGeneWhenDefinitionReturns()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef restoredGene = PlanTestData.CreateGene("MissingGene");
            var sourceRecord = new XenogermPlanSaveRecord(
                "stable-id",
                "Plan",
                ["GeneA", "MissingGene"],
                (int)PlanReadinessMode.ExactPayload);

            bool degradedRestore = sourceRecord.TryCreatePlan(
                PlanTestData.CreateResolver(geneA),
                out XenogermPlan degradedPlan,
                out string degradedFailureReason);
            var resavedRecord = XenogermPlanSaveRecord.FromPlan(degradedPlan);
            bool restored = resavedRecord.TryCreatePlan(
                PlanTestData.CreateResolver(geneA, restoredGene),
                out XenogermPlan restoredPlan,
                out string failureReason);

            Assert.That(degradedRestore, Is.True, degradedFailureReason);
            Assert.That(restored, Is.True, failureReason);
            Assert.That(
                restoredPlan.DesiredGenes.Select(gene => gene.defName),
                Is.EquivalentTo(["GeneA", "MissingGene"]));
            Assert.That(restoredPlan.UnresolvedDesiredGeneDefNames, Is.Empty);
            Assert.That(restoredPlan.IsDegraded, Is.False);
            Assert.That(restoredPlan.ReadinessMode, Is.EqualTo(PlanReadinessMode.ExactPayload));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void TryCreatePlan_RejectsInvalidId(string id)
        {
            XenogermPlanSaveRecord saveRecord = CreateValidRecord(id: id);

            AssertInvalidRecord(saveRecord);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void TryCreatePlan_RejectsInvalidName(string name)
        {
            XenogermPlanSaveRecord saveRecord = CreateValidRecord(name: name);

            AssertInvalidRecord(saveRecord);
        }

        [Test]
        public void TryCreatePlan_TrimsPersistedName()
        {
            XenogermPlanSaveRecord saveRecord = CreateValidRecord(name: "  Plan  ");

            bool restored = saveRecord.TryCreatePlan(
                PlanTestData.CreateResolver(),
                out XenogermPlan plan,
                out string failureReason);

            Assert.That(restored, Is.True, failureReason);
            Assert.That(plan.Name, Is.EqualTo("Plan"));
        }

        [Test]
        public void TryCreatePlan_RejectsMissingDesiredGeneCollection()
        {
            var saveRecord = new XenogermPlanSaveRecord("stable-id", "Plan", null, (int)PlanReadinessMode.Coverage);

            AssertInvalidRecord(saveRecord);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void TryCreatePlan_RejectsInvalidDesiredGeneName(string geneDefName)
        {
            XenogermPlanSaveRecord saveRecord = CreateValidRecord(desiredGeneDefNames: [geneDefName]);

            AssertInvalidRecord(saveRecord);
        }

        [Test]
        public void TryCreatePlan_RejectsUnknownReadinessMode()
        {
            XenogermPlanSaveRecord saveRecord = CreateValidRecord(readinessMode: 999);

            AssertInvalidRecord(saveRecord);
        }

        private static XenogermPlanSaveRecord CreateValidRecord(
            string id = "stable-id",
            string name = "Plan",
            string[] desiredGeneDefNames = null,
            int readinessMode = (int)PlanReadinessMode.Coverage)
        {
            return new XenogermPlanSaveRecord(id, name, desiredGeneDefNames ?? [], readinessMode);
        }

        private static void AssertInvalidRecord(XenogermPlanSaveRecord saveRecord)
        {
            bool restored = saveRecord.TryCreatePlan(
                PlanTestData.CreateResolver(),
                out XenogermPlan plan,
                out string failureReason);

            Assert.That(restored, Is.False);
            Assert.That(plan, Is.Null);
            Assert.That(failureReason, Is.Not.Null.And.Not.Empty);
        }


        [Test]
        public void RoundTrip_PreservesDisabledNotificationSettingAndReadyBaseline()
        {
            var source = new XenogermPlan("Plan", [], PlanReadinessMode.Coverage, readinessNotificationsEnabled: false);
            source.UpdateReadinessNotificationState(true);

            var record = XenogermPlanSaveRecord.FromPlan(source);
            bool restored = record.TryCreatePlan(
                PlanTestData.CreateResolver(),
                out XenogermPlan restoredPlan,
                out string failureReason);

            Assert.That(restored, Is.True, failureReason);
            Assert.That(restoredPlan.ReadinessNotificationsEnabled, Is.False);
            Assert.That(restoredPlan.HasReadinessNotificationBaseline, Is.True);
            Assert.That(restoredPlan.LastReadinessNotificationStateWasReady, Is.True);
        }

        [Test]
        public void RoundTrip_PreservesEnabledNotificationSettingAndNonReadyBaseline()
        {
            var source = new XenogermPlan("Plan", [], PlanReadinessMode.Coverage);
            source.UpdateReadinessNotificationState(false);

            var record = XenogermPlanSaveRecord.FromPlan(source);
            bool restored = record.TryCreatePlan(
                PlanTestData.CreateResolver(),
                out XenogermPlan restoredPlan,
                out string failureReason);

            Assert.That(restored, Is.True, failureReason);
            Assert.That(restoredPlan.ReadinessNotificationsEnabled, Is.True);
            Assert.That(restoredPlan.HasReadinessNotificationBaseline, Is.True);
            Assert.That(restoredPlan.LastReadinessNotificationStateWasReady, Is.False);
        }

        [Test]
        public void LegacyRecordDefaultsToEnabledNotificationsWithoutBaseline()
        {
            var record = new XenogermPlanSaveRecord("stable-id", "Plan", [], (int)PlanReadinessMode.Coverage);

            bool restored = record.TryCreatePlan(
                PlanTestData.CreateResolver(),
                out XenogermPlan restoredPlan,
                out string failureReason);

            Assert.That(restored, Is.True, failureReason);
            Assert.That(restoredPlan.ReadinessNotificationsEnabled, Is.True);
            Assert.That(restoredPlan.HasReadinessNotificationBaseline, Is.False);
            Assert.That(restoredPlan.LastReadinessNotificationStateWasReady, Is.False);
        }

        [Test]
        public void RehydrationIgnoresReadyFlagWhenBaselineIsMissing()
        {
            var record = new XenogermPlanSaveRecord(
                "stable-id",
                "Plan",
                [],
                (int)PlanReadinessMode.Coverage,
                readinessNotificationsEnabled: true,
                hasReadinessNotificationBaseline: false,
                lastReadinessNotificationStateWasReady: true);

            bool restored = record.TryCreatePlan(
                PlanTestData.CreateResolver(),
                out XenogermPlan restoredPlan,
                out string failureReason);

            Assert.That(restored, Is.True, failureReason);
            Assert.That(restoredPlan.HasReadinessNotificationBaseline, Is.False);
            Assert.That(restoredPlan.LastReadinessNotificationStateWasReady, Is.False);
        }
    }
}