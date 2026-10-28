using System.Collections.Generic;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Plans;
using XenogermPlanner.Tests.Plans;
using XenogermPlanner.UI;

namespace XenogermPlanner.Tests.UI
{
    [TestFixture]
    public sealed class XenogermPlanEditorInitialStateTests
    {
        [Test]
        public void CreateFromSource_UsesNewPlanDefaults()
        {
            GeneDef gene = PlanTestData.CreateGene("GeneA");

            var state = XenogermPlanEditorInitialState.CreateFromSource("Plan", [gene]);

            Assert.That(state.PlanName, Is.EqualTo("Plan"));
            Assert.That(state.ReadinessMode, Is.EqualTo(PlanReadinessMode.Coverage));
            Assert.That(state.ReadinessNotificationsEnabled, Is.True);
            Assert.That(state.DesiredGenes, Is.EquivalentTo([gene]));
        }

        [Test]
        public void CreateFromSource_CopiesAndNormalizesGeneCollection()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            var genes = new List<GeneDef> { geneA, geneA };

            var state = XenogermPlanEditorInitialState.CreateFromSource("Plan", genes);
            genes.Add(geneB);

            Assert.That(state.DesiredGenes, Is.EquivalentTo([geneA]));
        }

        [Test]
        public void CreateEmpty_UsesSameDefaultsWithEmptySelection()
        {
            var state = XenogermPlanEditorInitialState.CreateEmpty();

            Assert.That(state.PlanName, Is.Empty);
            Assert.That(state.DesiredGenes, Is.Empty);
            Assert.That(state.ReadinessMode, Is.EqualTo(PlanReadinessMode.Coverage));
            Assert.That(state.ReadinessNotificationsEnabled, Is.True);
        }
    }
}