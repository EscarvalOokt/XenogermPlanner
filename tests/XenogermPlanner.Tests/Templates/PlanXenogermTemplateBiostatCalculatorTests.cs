using System.Collections.Generic;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Plans;
using XenogermPlanner.Templates;

namespace XenogermPlanner.Tests.Templates
{
    [TestFixture]
    public sealed class PlanXenogermTemplateBiostatCalculatorTests
    {
        [Test]
        public void CalculateComposition_AdaptsRawGeneBiostats()
        {
            GeneDef firstGene = CreateGene("First", complexity: 3, metabolism: -2, archites: 1);
            GeneDef secondGene = CreateGene("Second", complexity: 5, metabolism: 4, archites: 2);
            var composition = new PlanXenogermTemplateComposition([firstGene, secondGene], [], physicalPackCount: 1);

            PlanXenogermTemplateBiostats result =
                PlanXenogermTemplateBiostatCalculator.CalculateComposition(composition);

            Assert.That(result.Complexity, Is.EqualTo(8));
            Assert.That(result.Metabolism, Is.EqualTo(2));
            Assert.That(result.ArchiteCapsules, Is.EqualTo(3));
        }

        [Test]
        public void CalculateCandidate_FlattensEveryCompositionBeforeSharedProjection()
        {
            GeneDef geneA = CreateGene("A", complexity: 1, metabolism: -1, archites: 0);
            GeneDef geneB = CreateGene("B", complexity: 2, metabolism: 2, archites: 1);
            GeneDef geneC = CreateGene("C", complexity: 4, metabolism: 3, archites: 2);
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(
                PlanReadinessMode.Coverage,
                geneA,
                geneB,
                geneC);
            PlanXenogermTemplateCandidate candidate = PlanXenogermTemplateTestData.CreateCandidate(
                plan,
                [geneA, geneB],
                [geneC]);
            var capturedGenes = new List<GeneDef>();

            PlanXenogermTemplateBiostats result = PlanXenogermTemplateBiostatCalculator.CalculateCandidate(
                candidate,
                genes =>
                {
                    capturedGenes.AddRange(genes);
                    return capturedGenes;
                });

            Assert.That(capturedGenes, Is.EquivalentTo([geneA, geneB, geneC]));
            Assert.That(result.Complexity, Is.EqualTo(7));
            Assert.That(result.Metabolism, Is.EqualTo(4));
            Assert.That(result.ArchiteCapsules, Is.EqualTo(3));
        }

        [Test]
        public void CalculateCandidate_AdaptsSharedEffectiveBiostats()
        {
            GeneDef retainedGene = CreateGene("Retained", complexity: 4, metabolism: 2, archites: 1);
            GeneDef removedGene = CreateGene("Removed", complexity: 9, metabolism: -5, archites: 3);
            XenogermPlan plan = PlanXenogermTemplateTestData.CreatePlan(
                PlanReadinessMode.Coverage,
                retainedGene,
                removedGene);
            PlanXenogermTemplateCandidate candidate = PlanXenogermTemplateTestData.CreateCandidate(
                plan,
                [retainedGene, removedGene]);

            PlanXenogermTemplateBiostats result = PlanXenogermTemplateBiostatCalculator.CalculateCandidate(
                candidate,
                _ => [retainedGene]);

            Assert.That(result.Complexity, Is.EqualTo(4));
            Assert.That(result.Metabolism, Is.EqualTo(2));
            Assert.That(result.ArchiteCapsules, Is.EqualTo(1));
        }

        private static GeneDef CreateGene(string defName, int complexity, int metabolism, int archites)
        {
            GeneDef gene = PlanXenogermTemplateTestData.CreateGene(defName);
            gene.biostatCpx = complexity;
            gene.biostatMet = metabolism;
            gene.biostatArc = archites;
            return gene;
        }
    }
}