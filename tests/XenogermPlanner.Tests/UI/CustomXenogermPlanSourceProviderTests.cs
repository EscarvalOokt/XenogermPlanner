using System.Collections.Generic;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Tests.Plans;
using XenogermPlanner.UI;

namespace XenogermPlanner.Tests.UI
{
    [TestFixture]
    public sealed class CustomXenogermPlanSourceProviderTests
    {
        [Test]
        public void Refresh_ValidTemplateCreatesResolvedSourceEntry()
        {
            CustomXenogerm source = PlanTestData.CreateCustomXenogerm(
                "Template",
                PlanTestData.CreateGeneSet(PlanTestData.CreateGene("GeneA")));
            var runtimeSources = new List<CustomXenogerm> { source };
            var provider = new CustomXenogermPlanSourceProvider(() => runtimeSources);

            XenogermPlanSourceGroup group = provider.Groups[0];
            XenogermPlanSourceEntry entry = group.Sources[0];

            Assert.That(group.IsCollapsible, Is.False);
            Assert.That(entry.DisplayName, Is.EqualTo("Template"));
            Assert.That(entry.InitialResult, Is.Not.Null);
            Assert.That(entry.InitialResult.IsSuccess, Is.True);
            Assert.That(entry.InitialResult.Selection.DesiredGenes.Count, Is.EqualTo(1));
        }

        [Test]
        public void Refresh_InvalidTemplateDoesNotPreventValidTemplateFromBeingListed()
        {
            var invalid = new CustomXenogerm
            {
                name = "Invalid",
                genesets = new List<GeneSet>()
            };
            CustomXenogerm valid = PlanTestData.CreateCustomXenogerm(
                "Valid",
                PlanTestData.CreateGeneSet(PlanTestData.CreateGene("GeneA")));
            var runtimeSources = new List<CustomXenogerm> { invalid, valid };
            var provider = new CustomXenogermPlanSourceProvider(() => runtimeSources);

            Assert.That(provider.Groups[0].Sources.Count, Is.EqualTo(2));
            Assert.That(provider.Groups[0].Sources[0].DisplayName, Is.EqualTo("Invalid"));
            Assert.That(provider.Groups[0].Sources[0].IsKnownInvalid, Is.True);
            Assert.That(provider.Groups[0].Sources[1].DisplayName, Is.EqualTo("Valid"));
            Assert.That(provider.Groups[0].Sources[1].IsKnownInvalid, Is.False);
        }

        [Test]
        public void Resolve_RevalidatesRuntimeSourceAndRejectsRemovedTemplate()
        {
            CustomXenogerm source = PlanTestData.CreateCustomXenogerm(
                "Template",
                PlanTestData.CreateGeneSet(PlanTestData.CreateGene("GeneA")));
            var runtimeSources = new List<CustomXenogerm> { source };
            var provider = new CustomXenogermPlanSourceProvider(() => runtimeSources);
            XenogermPlanSourceEntry entry = provider.Groups[0].Sources[0];

            runtimeSources.Clear();

            XenogermPlanSourceResolveResult result = Resolve(provider, entry, revalidate: true);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Failure, Is.EqualTo(XenogermPlanSourceFailure.SourceUnavailable));
        }

        [Test]
        public void Resolve_ReturnsIndependentFlattenedGeneSnapshot()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            GeneSet sourceGeneSet = PlanTestData.CreateGeneSet(geneA);
            CustomXenogerm source = PlanTestData.CreateCustomXenogerm("Template", sourceGeneSet);
            var runtimeSources = new List<CustomXenogerm> { source };
            var provider = new CustomXenogermPlanSourceProvider(() => runtimeSources);
            XenogermPlanSourceEntry entry = provider.Groups[0].Sources[0];

            XenogermPlanSourceResolveResult result = Resolve(provider, entry, revalidate: true);
            sourceGeneSet.AddGene(geneB);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Selection.Name, Is.EqualTo("Template"));
            Assert.That(result.Selection.DesiredGenes, Is.EquivalentTo([geneA]));
        }

        private static XenogermPlanSourceResolveResult Resolve(
            IXenogermPlanSourceProvider provider,
            XenogermPlanSourceEntry entry,
            bool revalidate)
        {
            XenogermPlanSourceResolveResult result = null;
            provider.Resolve(entry, revalidate, resolved => result = resolved);
            return result;
        }
    }
}