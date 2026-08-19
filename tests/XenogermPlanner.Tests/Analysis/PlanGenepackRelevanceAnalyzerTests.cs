using System;
using System.Collections.Generic;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Analysis
{
    [TestFixture]
    public sealed class PlanGenepackRelevanceAnalyzerTests
    {
        [Test]
        public void Evaluate_CoverageMissingGene_ReturnsPlanAndAllowsAdditionalGenes()
        {
            GeneDef missingGene = CreateGene("MissingGene");
            GeneDef additionalGene = CreateGene("AdditionalGene");
            XenogermPlan plan = CreatePlan("coverage-plan", "Coverage Plan", PlanReadinessMode.Coverage, missingGene);
            PlanReadinessResult readiness = CreateNotReadyResult(plan, (missingGene, PlanGeneCoverageState.Missing));
            PlanGenepackRelevanceAnalyzer analyzer = CreateAnalyzer(plan, readiness);

            IReadOnlyList<XenogermPlan> matches = analyzer.Evaluate(new[] { missingGene, additionalGene });

            AssertSingleMatch(matches, plan);
        }

        [Test]
        public void Evaluate_CoverageAvailableTargetGene_DoesNotReturnPlan()
        {
            GeneDef availableGene = CreateGene("AvailableGene");
            GeneDef missingGene = CreateGene("MissingGene");
            XenogermPlan plan = CreatePlan(
                "coverage-plan",
                "Coverage Plan",
                PlanReadinessMode.Coverage,
                availableGene,
                missingGene);
            PlanReadinessResult readiness = CreateNotReadyResult(
                plan,
                (availableGene, PlanGeneCoverageState.Available),
                (missingGene, PlanGeneCoverageState.Missing));
            PlanGenepackRelevanceAnalyzer analyzer = CreateAnalyzer(plan, readiness);

            IReadOnlyList<XenogermPlan> matches = analyzer.Evaluate(new[] { availableGene });

            Assert.That(matches, Is.Empty);
        }

        [Test]
        public void Evaluate_CoveragePrerequisiteOnlyGene_DoesNotReturnPlan()
        {
            GeneDef targetGene = CreateGene("TargetGene");
            GeneDef prerequisiteGene = CreateGene("PrerequisiteGene");
            XenogermPlan plan = CreatePlan("coverage-plan", "Coverage Plan", PlanReadinessMode.Coverage, targetGene);
            PlanReadinessResult readiness = CreateNotReadyResult(plan, (targetGene, PlanGeneCoverageState.Missing));
            PlanGenepackRelevanceAnalyzer analyzer = CreateAnalyzer(plan, readiness);

            IReadOnlyList<XenogermPlan> matches = analyzer.Evaluate(new[] { prerequisiteGene });

            Assert.That(matches, Is.Empty);
        }

        [Test]
        public void Evaluate_ExactPayloadMissingGene_ReturnsPlan()
        {
            GeneDef missingGene = CreateGene("MissingGene");
            XenogermPlan plan = CreatePlan("exact-plan", "Exact Plan", PlanReadinessMode.ExactPayload, missingGene);
            PlanReadinessResult readiness = CreateNotReadyResult(plan, (missingGene, PlanGeneCoverageState.Missing));
            PlanGenepackRelevanceAnalyzer analyzer = CreateAnalyzer(plan, readiness);

            IReadOnlyList<XenogermPlan> matches = analyzer.Evaluate(new[] { missingGene });

            AssertSingleMatch(matches, plan);
        }

        [Test]
        public void Evaluate_ExactPayloadConflictGene_ReturnsPlan()
        {
            GeneDef conflictGene = CreateGene("ConflictGene");
            XenogermPlan plan = CreatePlan("exact-plan", "Exact Plan", PlanReadinessMode.ExactPayload, conflictGene);
            PlanReadinessResult readiness = CreateNotReadyResult(
                plan,
                (conflictGene, PlanGeneCoverageState.ExactPayloadConflict));
            PlanGenepackRelevanceAnalyzer analyzer = CreateAnalyzer(plan, readiness);

            IReadOnlyList<XenogermPlan> matches = analyzer.Evaluate(new[] { conflictGene });

            AssertSingleMatch(matches, plan);
        }

        [Test]
        public void Evaluate_ExactPayloadOfferedGeneOutsideTarget_DoesNotReturnPlan()
        {
            GeneDef missingGene = CreateGene("MissingGene");
            GeneDef outsideGene = CreateGene("OutsideGene");
            XenogermPlan plan = CreatePlan("exact-plan", "Exact Plan", PlanReadinessMode.ExactPayload, missingGene);
            PlanReadinessResult readiness = CreateNotReadyResult(plan, (missingGene, PlanGeneCoverageState.Missing));
            PlanGenepackRelevanceAnalyzer analyzer = CreateAnalyzer(plan, readiness);

            IReadOnlyList<XenogermPlan> matches = analyzer.Evaluate(new[] { missingGene, outsideGene });

            Assert.That(matches, Is.Empty);
        }

        [Test]
        public void Evaluate_ExactPayloadOnlyAvailableTargetGenes_DoesNotReturnPlan()
        {
            GeneDef availableGene = CreateGene("AvailableGene");
            GeneDef missingGene = CreateGene("MissingGene");
            XenogermPlan plan = CreatePlan(
                "exact-plan",
                "Exact Plan",
                PlanReadinessMode.ExactPayload,
                availableGene,
                missingGene);
            PlanReadinessResult readiness = CreateNotReadyResult(
                plan,
                (availableGene, PlanGeneCoverageState.Available),
                (missingGene, PlanGeneCoverageState.Missing));
            PlanGenepackRelevanceAnalyzer analyzer = CreateAnalyzer(plan, readiness);

            IReadOnlyList<XenogermPlan> matches = analyzer.Evaluate(new[] { availableGene });

            Assert.That(matches, Is.Empty);
        }

        [TestCase(PlanReadinessStatus.Ready)]
        [TestCase(PlanReadinessStatus.EmptyTarget)]
        [TestCase(PlanReadinessStatus.Degraded)]
        [TestCase(PlanReadinessStatus.Unavailable)]
        public void Evaluate_NonNotReadyState_DoesNotReturnPlan(PlanReadinessStatus status)
        {
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("plan-id", "Plan Name", PlanReadinessMode.Coverage, gene);
            PlanGenepackRelevanceAnalyzer analyzer = CreateAnalyzer(plan, CreateResult(status, gene));

            IReadOnlyList<XenogermPlan> matches = analyzer.Evaluate(new[] { gene });

            Assert.That(matches, Is.Empty);
        }

        [Test]
        public void Evaluate_NotReadyWithoutRelevantDiagnostics_DoesNotReturnPlan()
        {
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("plan-id", "Plan Name", PlanReadinessMode.Coverage, gene);
            var readiness = PlanReadinessResult.CreateNotReady(new[] { gene }, Array.Empty<GeneDef>(), false);
            PlanGenepackRelevanceAnalyzer analyzer = CreateAnalyzer(plan, readiness);

            IReadOnlyList<XenogermPlan> matches = analyzer.Evaluate(new[] { gene });

            Assert.That(matches, Is.Empty);
        }

        [Test]
        public void Constructor_AnalyzesEachPlanOnceRegardlessOfEvaluatedCompositionCount()
        {
            GeneDef firstGene = CreateGene("FirstGene");
            GeneDef secondGene = CreateGene("SecondGene");
            XenogermPlan firstPlan = CreatePlan("first-plan", "First Plan", PlanReadinessMode.Coverage, firstGene);
            XenogermPlan secondPlan = CreatePlan("second-plan", "Second Plan", PlanReadinessMode.Coverage, secondGene);
            var callCount = 0;
            var readinessByPlan = new Dictionary<XenogermPlan, PlanReadinessResult>
            {
                [firstPlan] = CreateNotReadyResult(firstPlan, (firstGene, PlanGeneCoverageState.Missing)),
                [secondPlan] = CreateNotReadyResult(secondPlan, (secondGene, PlanGeneCoverageState.Missing))
            };

            var analyzer = new PlanGenepackRelevanceAnalyzer(
                new[] { firstPlan, secondPlan },
                PlanGenepackInventorySnapshot.CreateAvailable(Array.Empty<Genepack>()),
                (plan, _) =>
                {
                    callCount++;
                    return readinessByPlan[plan];
                });

            analyzer.Evaluate(new[] { firstGene });
            analyzer.Evaluate(new[] { secondGene });
            analyzer.Evaluate(new[] { firstGene, secondGene });

            Assert.That(callCount, Is.EqualTo(2));
        }

        [Test]
        public void Evaluate_ReturnsPlanReferencesInDeterministicOrder()
        {
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan lastByName = CreatePlan("plan-c", "Zulu", PlanReadinessMode.Coverage, gene);
            XenogermPlan secondById = CreatePlan("plan-b", "alpha", PlanReadinessMode.Coverage, gene);
            XenogermPlan firstById = CreatePlan("plan-a", "Alpha", PlanReadinessMode.Coverage, gene);
            var readinessByPlan = new Dictionary<XenogermPlan, PlanReadinessResult>
            {
                [lastByName] = CreateNotReadyResult(lastByName, (gene, PlanGeneCoverageState.Missing)),
                [secondById] = CreateNotReadyResult(secondById, (gene, PlanGeneCoverageState.Missing)),
                [firstById] = CreateNotReadyResult(firstById, (gene, PlanGeneCoverageState.Missing))
            };
            var analyzer = new PlanGenepackRelevanceAnalyzer(
                new[] { lastByName, secondById, firstById },
                PlanGenepackInventorySnapshot.CreateAvailable(Array.Empty<Genepack>()),
                (plan, _) => readinessByPlan[plan]);

            IReadOnlyList<XenogermPlan> matches = analyzer.Evaluate(new[] { gene });

            Assert.That(matches, Is.EqualTo(new[] { firstById, secondById, lastByName }));
        }

        [Test]
        public void Evaluate_RepeatedCallsPreserveDeterministicOrder()
        {
            GeneDef firstGene = CreateGene("FirstGene");
            GeneDef secondGene = CreateGene("SecondGene");
            XenogermPlan lastFirstGenePlan = CreatePlan("plan-c", "Zulu A", PlanReadinessMode.Coverage, firstGene);
            XenogermPlan firstFirstGenePlan = CreatePlan("plan-a", "Alpha A", PlanReadinessMode.Coverage, firstGene);
            XenogermPlan lastSecondGenePlan = CreatePlan("plan-d", "Zulu B", PlanReadinessMode.Coverage, secondGene);
            XenogermPlan firstSecondGenePlan = CreatePlan("plan-b", "Beta B", PlanReadinessMode.Coverage, secondGene);
            var readinessByPlan = new Dictionary<XenogermPlan, PlanReadinessResult>
            {
                [lastFirstGenePlan] = CreateNotReadyResult(
                    lastFirstGenePlan,
                    (firstGene, PlanGeneCoverageState.Missing)),
                [firstFirstGenePlan] = CreateNotReadyResult(
                    firstFirstGenePlan,
                    (firstGene, PlanGeneCoverageState.Missing)),
                [lastSecondGenePlan] = CreateNotReadyResult(
                    lastSecondGenePlan,
                    (secondGene, PlanGeneCoverageState.Missing)),
                [firstSecondGenePlan] = CreateNotReadyResult(
                    firstSecondGenePlan,
                    (secondGene, PlanGeneCoverageState.Missing))
            };
            var analyzer = new PlanGenepackRelevanceAnalyzer(
                new[] { lastSecondGenePlan, lastFirstGenePlan, firstSecondGenePlan, firstFirstGenePlan },
                PlanGenepackInventorySnapshot.CreateAvailable(Array.Empty<Genepack>()),
                (plan, _) => readinessByPlan[plan]);

            IReadOnlyList<XenogermPlan> firstMatches = analyzer.Evaluate(new[] { firstGene });
            IReadOnlyList<XenogermPlan> secondMatches = analyzer.Evaluate(new[] { secondGene });
            IReadOnlyList<XenogermPlan> thirdMatches = analyzer.Evaluate(new[] { firstGene });

            Assert.That(firstMatches, Is.EqualTo(new[] { firstFirstGenePlan, lastFirstGenePlan }));
            Assert.That(secondMatches, Is.EqualTo(new[] { firstSecondGenePlan, lastSecondGenePlan }));
            Assert.That(thirdMatches, Is.EqualTo(new[] { firstFirstGenePlan, lastFirstGenePlan }));
            Assert.That(firstMatches[0], Is.SameAs(firstFirstGenePlan));
            Assert.That(firstMatches[1], Is.SameAs(lastFirstGenePlan));
            Assert.That(secondMatches[0], Is.SameAs(firstSecondGenePlan));
            Assert.That(secondMatches[1], Is.SameAs(lastSecondGenePlan));
            Assert.That(thirdMatches[0], Is.SameAs(firstFirstGenePlan));
            Assert.That(thirdMatches[1], Is.SameAs(lastFirstGenePlan));
        }

        [Test]
        public void Evaluate_EquivalentOfferedSetsInDifferentOrder_ReturnEquivalentResults()
        {
            GeneDef firstGene = CreateGene("FirstGene");
            GeneDef secondGene = CreateGene("SecondGene");
            XenogermPlan plan = CreatePlan("plan-id", "Plan Name", PlanReadinessMode.Coverage, firstGene, secondGene);
            PlanReadinessResult readiness = CreateNotReadyResult(
                plan,
                (firstGene, PlanGeneCoverageState.Missing),
                (secondGene, PlanGeneCoverageState.Missing));
            PlanGenepackRelevanceAnalyzer analyzer = CreateAnalyzer(plan, readiness);

            IReadOnlyList<XenogermPlan> firstMatches = analyzer.Evaluate(new[] { firstGene, secondGene });
            IReadOnlyList<XenogermPlan> secondMatches = analyzer.Evaluate(new[] { secondGene, firstGene });

            Assert.That(secondMatches, Is.EqualTo(firstMatches));
        }

        [Test]
        public void Evaluate_DuplicateOfferedGeneDoesNotChangeResult()
        {
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("plan-id", "Plan Name", PlanReadinessMode.Coverage, gene);
            PlanGenepackRelevanceAnalyzer analyzer = CreateAnalyzer(
                plan,
                CreateNotReadyResult(plan, (gene, PlanGeneCoverageState.Missing)));

            IReadOnlyList<XenogermPlan> matches = analyzer.Evaluate(new[] { gene, gene });

            AssertSingleMatch(matches, plan);
        }

        [Test]
        public void Evaluate_DoesNotMutatePlanOwnedState()
        {
            GeneDef gene = CreateGene("GeneA");
            var plan = new XenogermPlan(
                "plan-id",
                "  Plan Name  ",
                new[] { gene },
                Array.Empty<string>(),
                PlanReadinessMode.Coverage,
                readinessNotificationsEnabled: false,
                hasReadinessNotificationBaseline: true,
                lastReadinessNotificationStateWasReady: true);
            string originalName = plan.Name;
            string originalId = plan.Id;
            PlanReadinessMode originalMode = plan.ReadinessMode;
            bool originalNotificationsEnabled = plan.ReadinessNotificationsEnabled;
            bool originalHasBaseline = plan.HasReadinessNotificationBaseline;
            bool originalLastStateWasReady = plan.LastReadinessNotificationStateWasReady;
            GeneDef[] originalGenes = new List<GeneDef>(plan.DesiredGenes).ToArray();
            PlanGenepackRelevanceAnalyzer analyzer = CreateAnalyzer(
                plan,
                CreateNotReadyResult(plan, (gene, PlanGeneCoverageState.Missing)));

            analyzer.Evaluate(new[] { gene });

            Assert.That(plan.Id, Is.EqualTo(originalId));
            Assert.That(plan.Name, Is.EqualTo(originalName));
            Assert.That(plan.ReadinessMode, Is.EqualTo(originalMode));
            Assert.That(plan.ReadinessNotificationsEnabled, Is.EqualTo(originalNotificationsEnabled));
            Assert.That(plan.HasReadinessNotificationBaseline, Is.EqualTo(originalHasBaseline));
            Assert.That(plan.LastReadinessNotificationStateWasReady, Is.EqualTo(originalLastStateWasReady));
            Assert.That(plan.DesiredGenes, Is.EquivalentTo(originalGenes));
        }

        [Test]
        public void Evaluate_NullEmptyOrNullContainingCompositionThrows()
        {
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("plan-id", "Plan Name", PlanReadinessMode.Coverage, gene);
            PlanGenepackRelevanceAnalyzer analyzer = CreateAnalyzer(
                plan,
                CreateNotReadyResult(plan, (gene, PlanGeneCoverageState.Missing)));

            Assert.Throws<ArgumentNullException>((Action)(() => analyzer.Evaluate(null)));
            Assert.Throws<ArgumentException>((Action)(() => analyzer.Evaluate(Array.Empty<GeneDef>())));
            Assert.Throws<ArgumentException>((Action)(() => analyzer.Evaluate(new GeneDef[] { null })));
        }

        private static PlanGenepackRelevanceAnalyzer CreateAnalyzer(XenogermPlan plan, PlanReadinessResult readiness)
        {
            return new PlanGenepackRelevanceAnalyzer(
                new[] { plan },
                PlanGenepackInventorySnapshot.CreateAvailable(Array.Empty<Genepack>()),
                (_, __) => readiness);
        }

        private static XenogermPlan CreatePlan(
            string id,
            string name,
            PlanReadinessMode readinessMode,
            params GeneDef[] desiredGenes)
        {
            return new XenogermPlan(id, name, desiredGenes, Array.Empty<string>(), readinessMode);
        }

        private static PlanReadinessResult CreateNotReadyResult(
            XenogermPlan plan,
            params (GeneDef Gene, PlanGeneCoverageState State)[] diagnostics)
        {
            var coveredGenes = new List<GeneDef>();
            var missingGenes = new List<GeneDef>();
            var geneDiagnostics = new List<PlanGeneCoverageDiagnostic>();

            foreach ((GeneDef gene, PlanGeneCoverageState state) in diagnostics)
            {
                PlanGeneCoverageDiagnostic diagnostic = CreateDiagnostic(gene, state);
                geneDiagnostics.Add(diagnostic);

                if (diagnostic.IsCovered)
                    coveredGenes.Add(gene);
                else
                    missingGenes.Add(gene);
            }

            return PlanReadinessResult.CreateNotReady(
                coveredGenes,
                missingGenes,
                plan.ReadinessMode == PlanReadinessMode.ExactPayload && missingGenes.Count == 0,
                geneDiagnostics);
        }

        private static PlanGeneCoverageDiagnostic CreateDiagnostic(GeneDef gene, PlanGeneCoverageState state)
        {
            switch (state)
            {
                case PlanGeneCoverageState.Missing:
                    return new PlanGeneCoverageDiagnostic(
                        gene,
                        state,
                        Array.Empty<PlanGenepackCompositionDiagnostic>());

                case PlanGeneCoverageState.Available:
                    return new PlanGeneCoverageDiagnostic(
                        gene,
                        state,
                        new[]
                        {
                            new PlanGenepackCompositionDiagnostic(new[] { gene }, 1, true, Array.Empty<GeneDef>())
                        });

                case PlanGeneCoverageState.ExactPayloadConflict:
                    GeneDef additionalGene = CreateGene(gene.defName + "_Additional");

                    return new PlanGeneCoverageDiagnostic(
                        gene,
                        state,
                        new[]
                        {
                            new PlanGenepackCompositionDiagnostic(
                                new[] { gene, additionalGene },
                                1,
                                false,
                                new[] { additionalGene })
                        });

                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown diagnostic state.");
            }
        }

        private static PlanReadinessResult CreateResult(PlanReadinessStatus status, GeneDef gene)
        {
            switch (status)
            {
                case PlanReadinessStatus.Ready:
                    return PlanReadinessResult.CreateReady(new[] { gene });

                case PlanReadinessStatus.EmptyTarget:
                    return PlanReadinessResult.CreateEmptyTarget();

                case PlanReadinessStatus.Degraded:
                    return PlanReadinessResult.CreateDegraded(Array.Empty<GeneDef>(), new[] { gene });

                case PlanReadinessStatus.Unavailable:
                    return PlanReadinessResult.CreateUnavailable(PlanReadinessUnavailableReason.NoActiveMap);

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported test status.");
            }
        }

        private static GeneDef CreateGene(string defName)
        {
            return new GeneDef { defName = defName };
        }

        private static void AssertSingleMatch(IReadOnlyList<XenogermPlan> matches, XenogermPlan expectedPlan)
        {
            Assert.That(matches, Has.Count.EqualTo(1));
            Assert.That(matches[0], Is.SameAs(expectedPlan));
        }
    }
}