using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Plans
{
    [TestFixture]
    public sealed class XenogermPlanTransferCodecTests
    {
        private const string Marker = "XENOGERM_PLANNER_PLAN";

        private static readonly Encoding _strictUtf8 = new UTF8Encoding(false, true);

        private static IEnumerable<string> MalformedPayloads
        {
            get
            {
                yield return null;
                yield return string.Empty;
                yield return " ";
                yield return "WRONG_MARKER\nversion=1\nname=UGxhbg==\nreadinessMode=Coverage";
                yield return Marker + "\nversion=abc\nname=UGxhbg==\nreadinessMode=Coverage";
                yield return Marker + "\nversion=1\nreadinessMode=Coverage";
                yield return Marker + "\nversion=1\nname=" + Encode(string.Empty) + "\nreadinessMode=Coverage";
                yield return Marker + "\nversion=1\nname=" + Encode("   ") + "\nreadinessMode=Coverage";
                yield return Marker + "\nversion=1\nname=UGxhbg==";
                yield return Marker + "\nversion=1\nname=%%%\nreadinessMode=Coverage";
                yield return Marker + "\nversion=1\nname=/w==\nreadinessMode=Coverage";
                yield return Marker + "\nversion=1\nname=UGxhbg==\nname=UGxhbiAy\nreadinessMode=Coverage";
                yield return Marker + "\nversion=1\nname=UGxhbg==\nreadinessMode=Coverage\nreadinessMode=ExactPayload";
                yield return Marker + "\nversion=1\nname=UGxhbg==\nreadinessMode=Unknown";
                yield return Marker + "\nversion=1\nname=UGxhbg==\nreadinessMode=Coverage\ngene=IA==";
                yield return Marker + "\nversion=1\nname=UGxhbg==\nreadinessMode=Coverage\ngene=%%%";
                yield return Marker + "\nversion=1\nname=UGxhbg==\nreadinessMode=Coverage\nunknown=value";
                yield return Marker + "\nversion=1\nname=UGxhbg==\nreadinessMode=Coverage\n\ngene=R2VuZUE=";
            }
        }

        [Test]
        public void RoundTrip_PreservesPopulatedPlanAndCreatesNewIdentity()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            var source = new XenogermPlan("Plan", [geneB, geneA], PlanReadinessMode.ExactPayload);

            string payload = XenogermPlanTransferCodec.Serialize(source);
            bool deserialized = XenogermPlanTransferCodec.TryDeserialize(
                payload.Replace("\n", "\r\n"),
                PlanTestData.CreateResolver(geneA, geneB),
                out XenogermPlan pasted,
                out XenogermPlanTransferFailure failure);

            Assert.That(deserialized, Is.True);
            Assert.That(failure, Is.EqualTo(XenogermPlanTransferFailure.None));
            Assert.That(pasted.Id, Is.Not.EqualTo(source.Id));
            Assert.That(pasted.Name, Is.EqualTo(source.Name));
            Assert.That(pasted.DesiredGenes, Is.EquivalentTo([geneA, geneB]));
            Assert.That(pasted.UnresolvedDesiredGeneDefNames, Is.Empty);
            Assert.That(pasted.ReadinessMode, Is.EqualTo(PlanReadinessMode.ExactPayload));
        }

        [Test]
        public void Serialize_DoesNotTransferSourceIdentityAndRepeatedPasteCreatesDistinctIds()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            var source = new XenogermPlan("Plan", [geneA], PlanReadinessMode.Coverage);

            string payload = XenogermPlanTransferCodec.Serialize(source);
            bool firstResult = XenogermPlanTransferCodec.TryDeserialize(
                payload,
                PlanTestData.CreateResolver(geneA),
                out XenogermPlan firstPaste,
                out XenogermPlanTransferFailure firstFailure);
            bool secondResult = XenogermPlanTransferCodec.TryDeserialize(
                payload,
                PlanTestData.CreateResolver(geneA),
                out XenogermPlan secondPaste,
                out XenogermPlanTransferFailure secondFailure);

            Assert.That(payload, Does.Not.Contain(source.Id));
            Assert.That(firstResult, Is.True);
            Assert.That(secondResult, Is.True);
            Assert.That(firstFailure, Is.EqualTo(XenogermPlanTransferFailure.None));
            Assert.That(secondFailure, Is.EqualTo(XenogermPlanTransferFailure.None));
            Assert.That(firstPaste.Id, Is.Not.EqualTo(source.Id));
            Assert.That(secondPaste.Id, Is.Not.EqualTo(source.Id));
            Assert.That(firstPaste.Id, Is.Not.EqualTo(secondPaste.Id));
        }

        [Test]
        public void RoundTrip_PreservesConflictingResolvedAndUnavailableRequirements()
        {
            GeneDef first = PlanTestData.CreateGene("First");
            GeneDef second = PlanTestData.CreateGene("Second");
            first.exclusionTags = ["Conflict"];
            second.exclusionTags = ["Conflict"];
            var source = new XenogermPlan(
                "source-id",
                "Plan",
                [first, second],
                ["UnavailableConflict"],
                PlanReadinessMode.Coverage);

            bool deserialized = XenogermPlanTransferCodec.TryDeserialize(
                XenogermPlanTransferCodec.Serialize(source),
                PlanTestData.CreateResolver(first, second),
                out XenogermPlan pasted,
                out XenogermPlanTransferFailure failure);

            Assert.That(deserialized, Is.True);
            Assert.That(failure, Is.EqualTo(XenogermPlanTransferFailure.None));
            Assert.That(pasted.Id, Is.Not.EqualTo(source.Id));
            Assert.That(pasted.DesiredGenes, Is.EquivalentTo([first, second]));
            Assert.That(pasted.UnresolvedDesiredGeneDefNames, Is.EquivalentTo(["UnavailableConflict"]));
        }

        [Test]
        public void RoundTrip_PreservesEmptyPlan()
        {
            var source = new XenogermPlan("Empty", [], PlanReadinessMode.Coverage);

            bool deserialized = XenogermPlanTransferCodec.TryDeserialize(
                XenogermPlanTransferCodec.Serialize(source),
                PlanTestData.CreateResolver(),
                out XenogermPlan pasted,
                out XenogermPlanTransferFailure failure);

            Assert.That(deserialized, Is.True);
            Assert.That(failure, Is.EqualTo(XenogermPlanTransferFailure.None));
            Assert.That(pasted.Name, Is.EqualTo("Empty"));
            Assert.That(pasted.DesiredGenes, Is.Empty);
            Assert.That(pasted.UnresolvedDesiredGeneDefNames, Is.Empty);
            Assert.That(pasted.IsDegraded, Is.False);
        }

        [Test]
        public void RoundTrip_PreservesResolvedAndUnresolvedRequirements()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            var source = new XenogermPlan(
                "source-id",
                "Degraded",
                [geneA],
                ["MissingGene"],
                PlanReadinessMode.ExactPayload);

            bool deserialized = XenogermPlanTransferCodec.TryDeserialize(
                XenogermPlanTransferCodec.Serialize(source),
                PlanTestData.CreateResolver(geneA),
                out XenogermPlan pasted,
                out XenogermPlanTransferFailure failure);

            Assert.That(deserialized, Is.True);
            Assert.That(failure, Is.EqualTo(XenogermPlanTransferFailure.None));
            Assert.That(pasted.DesiredGenes, Is.EquivalentTo([geneA]));
            Assert.That(pasted.UnresolvedDesiredGeneDefNames, Is.EquivalentTo(["MissingGene"]));
            Assert.That(pasted.IsDegraded, Is.True);
            Assert.That(pasted.ReadinessMode, Is.EqualTo(PlanReadinessMode.ExactPayload));
        }

        [Test]
        public void Deserialize_PreservesResolvedSourceGeneAsUnresolvedWhenDestinationDefinitionIsMissing()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            var source = new XenogermPlan("Plan", [geneA], PlanReadinessMode.Coverage);

            bool deserialized = XenogermPlanTransferCodec.TryDeserialize(
                XenogermPlanTransferCodec.Serialize(source),
                PlanTestData.CreateResolver(),
                out XenogermPlan pasted,
                out XenogermPlanTransferFailure failure);

            Assert.That(deserialized, Is.True);
            Assert.That(failure, Is.EqualTo(XenogermPlanTransferFailure.None));
            Assert.That(pasted.DesiredGenes, Is.Empty);
            Assert.That(pasted.UnresolvedDesiredGeneDefNames, Is.EquivalentTo(["GeneA"]));
            Assert.That(pasted.IsDegraded, Is.True);
        }

        [Test]
        public void Deserialize_ResolvesPreviouslyUnresolvedRequirementWhenDestinationDefinitionExists()
        {
            GeneDef restoredGene = PlanTestData.CreateGene("RestoredGene");
            var source = new XenogermPlan("source-id", "Plan", [], ["RestoredGene"], PlanReadinessMode.Coverage);

            bool deserialized = XenogermPlanTransferCodec.TryDeserialize(
                XenogermPlanTransferCodec.Serialize(source),
                PlanTestData.CreateResolver(restoredGene),
                out XenogermPlan pasted,
                out XenogermPlanTransferFailure failure);

            Assert.That(deserialized, Is.True);
            Assert.That(failure, Is.EqualTo(XenogermPlanTransferFailure.None));
            Assert.That(pasted.DesiredGenes, Is.EquivalentTo([restoredGene]));
            Assert.That(pasted.UnresolvedDesiredGeneDefNames, Is.Empty);
            Assert.That(pasted.IsDegraded, Is.False);
        }

        [Test]
        public void Deserialize_NormalizesDuplicateGeneEntries()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            string payload = CreatePayload("Plan", "Coverage", "GeneA", "GeneA", "GeneA");

            bool deserialized = XenogermPlanTransferCodec.TryDeserialize(
                payload,
                PlanTestData.CreateResolver(geneA),
                out XenogermPlan pasted,
                out XenogermPlanTransferFailure failure);

            Assert.That(deserialized, Is.True);
            Assert.That(failure, Is.EqualTo(XenogermPlanTransferFailure.None));
            Assert.That(pasted.DesiredGenes, Is.EquivalentTo([geneA]));
            Assert.That(pasted.DesiredGenes.Count, Is.EqualTo(1));
        }

        [Test]
        public void Serialize_ProducesDeterministicOrdinalGeneOrder()
        {
            GeneDef geneUpper = PlanTestData.CreateGene("GeneA");
            GeneDef geneLower = PlanTestData.CreateGene("geneB");
            GeneDef geneMiddle = PlanTestData.CreateGene("GeneC");
            var first = new XenogermPlan("Plan", [geneLower, geneMiddle, geneUpper], PlanReadinessMode.Coverage);
            var second = new XenogermPlan("Plan", [geneUpper, geneLower, geneMiddle], PlanReadinessMode.Coverage);

            string firstPayload = XenogermPlanTransferCodec.Serialize(first);
            string secondPayload = XenogermPlanTransferCodec.Serialize(second);
            string[] geneLines = firstPayload.Split('\n')
                .Where(line => line.StartsWith("gene=", StringComparison.Ordinal))
                .ToArray();

            Assert.That(secondPayload, Is.EqualTo(firstPayload));
            Assert.That(
                geneLines,
                Is.EqualTo(
                [
                    "gene=" + Encode("GeneA"),
                    "gene=" + Encode("GeneC"),
                    "gene=" + Encode("geneB")
                ]));
        }

        [TestCase("План\n第二行")]
        public void RoundTrip_PreservesEncodedName(string name)
        {
            var source = new XenogermPlan(name, [], PlanReadinessMode.ExactPayload);

            bool deserialized = XenogermPlanTransferCodec.TryDeserialize(
                XenogermPlanTransferCodec.Serialize(source),
                PlanTestData.CreateResolver(),
                out XenogermPlan pasted,
                out XenogermPlanTransferFailure failure);

            Assert.That(deserialized, Is.True);
            Assert.That(failure, Is.EqualTo(XenogermPlanTransferFailure.None));
            Assert.That(pasted.Name, Is.EqualTo(name));
        }

        [Test]
        public void Deserialize_TrimsEncodedName()
        {
            string payload = CreatePayload("  Plan  ", "Coverage");

            bool deserialized = XenogermPlanTransferCodec.TryDeserialize(
                payload,
                PlanTestData.CreateResolver(),
                out XenogermPlan plan,
                out XenogermPlanTransferFailure failure);

            Assert.That(deserialized, Is.True);
            Assert.That(failure, Is.EqualTo(XenogermPlanTransferFailure.None));
            Assert.That(plan.Name, Is.EqualTo("Plan"));
        }

        [Test]
        public void RepeatedPaste_IsAllocatedUniquelyByDestination()
        {
            string payload = CreatePayload("Plan", "Coverage");
            var component = new XenogermPlanGameComponent(null);

            XenogermPlanTransferCodec.TryDeserialize(
                payload,
                PlanTestData.CreateResolver(),
                out XenogermPlan first,
                out _);
            XenogermPlanTransferCodec.TryDeserialize(
                payload,
                PlanTestData.CreateResolver(),
                out XenogermPlan second,
                out _);

            component.AddPlanWithAllocatedName(first);
            component.AddPlanWithAllocatedName(second);

            Assert.That(first.Id, Is.Not.EqualTo(second.Id));
            Assert.That(component.Plans.Select(plan => plan.Name), Is.EqualTo(["Plan", "Plan 2"]));
        }

        [TestCaseSource(nameof(MalformedPayloads))]
        public void Deserialize_RejectsMalformedPayload(string payload)
        {
            bool deserialized = XenogermPlanTransferCodec.TryDeserialize(
                payload,
                PlanTestData.CreateResolver(),
                out XenogermPlan plan,
                out XenogermPlanTransferFailure failure);

            Assert.That(deserialized, Is.False);
            Assert.That(plan, Is.Null);
            Assert.That(failure, Is.EqualTo(XenogermPlanTransferFailure.MalformedPayload));
        }

        [Test]
        public void Deserialize_RejectsUnsupportedVersionSeparately()
        {
            string payload = Marker + "\nversion=2\nname=" + Encode("Plan") + "\nreadinessMode=Coverage";

            bool deserialized = XenogermPlanTransferCodec.TryDeserialize(
                payload,
                PlanTestData.CreateResolver(),
                out XenogermPlan plan,
                out XenogermPlanTransferFailure failure);

            Assert.That(deserialized, Is.False);
            Assert.That(plan, Is.Null);
            Assert.That(failure, Is.EqualTo(XenogermPlanTransferFailure.UnsupportedVersion));
        }

        [Test]
        public void DeserializeFailure_DoesNotMutateSourcePlan()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            var source = new XenogermPlan("Source", [geneA], PlanReadinessMode.ExactPayload);
            string sourceId = source.Id;

            bool deserialized = XenogermPlanTransferCodec.TryDeserialize(
                "invalid",
                PlanTestData.CreateResolver(geneA),
                out XenogermPlan plan,
                out XenogermPlanTransferFailure failure);

            Assert.That(deserialized, Is.False);
            Assert.That(plan, Is.Null);
            Assert.That(failure, Is.EqualTo(XenogermPlanTransferFailure.MalformedPayload));
            Assert.That(source.Id, Is.EqualTo(sourceId));
            Assert.That(source.Name, Is.EqualTo("Source"));
            Assert.That(source.DesiredGenes, Is.EquivalentTo([geneA]));
            Assert.That(source.ReadinessMode, Is.EqualTo(PlanReadinessMode.ExactPayload));
        }


        [Test]
        public void RoundTrip_DoesNotTransferNotificationPreferenceOrTransitionBaseline()
        {
            var source = new XenogermPlan("Plan", [], PlanReadinessMode.Coverage, readinessNotificationsEnabled: false);
            source.UpdateReadinessNotificationState(true);

            bool deserialized = XenogermPlanTransferCodec.TryDeserialize(
                XenogermPlanTransferCodec.Serialize(source),
                PlanTestData.CreateResolver(),
                out XenogermPlan pasted,
                out XenogermPlanTransferFailure failure);

            Assert.That(deserialized, Is.True);
            Assert.That(failure, Is.EqualTo(XenogermPlanTransferFailure.None));
            Assert.That(pasted.Id, Is.Not.EqualTo(source.Id));
            Assert.That(pasted.ReadinessNotificationsEnabled, Is.True);
            Assert.That(pasted.HasReadinessNotificationBaseline, Is.False);
            Assert.That(pasted.LastReadinessNotificationStateWasReady, Is.False);
        }

        private static string CreatePayload(string name, string readinessMode, params string[] geneDefNames)
        {
            var lines = new List<string>
            {
                Marker,
                "version=1",
                "name=" + Encode(name),
                "readinessMode=" + readinessMode
            };

            foreach (string geneDefName in geneDefNames)
                lines.Add("gene=" + Encode(geneDefName));

            return string.Join("\n", lines);
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(_strictUtf8.GetBytes(value));
        }
    }
}