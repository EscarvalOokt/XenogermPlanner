using System;
using System.Globalization;
using NUnit.Framework;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Plans
{
    [TestFixture]
    public sealed class XenogermPlanNameAllocatorTests
    {
        [Test]
        public void TryValidate_TrimsOuterWhitespaceAndPreservesCasing()
        {
            bool valid = XenogermPlanNameAllocator.TryValidate(
                [],
                "  My Plan  ",
                null,
                out string normalizedName,
                out XenogermPlanNameValidationFailure failure);

            Assert.That(valid, Is.True);
            Assert.That(normalizedName, Is.EqualTo("My Plan"));
            Assert.That(failure, Is.EqualTo(XenogermPlanNameValidationFailure.None));
        }

        [Test]
        public void TryValidate_PreservesInternalWhitespaceAndUnicode()
        {
            bool valid = XenogermPlanNameAllocator.TryValidate(
                [],
                "  План  Ω  ",
                null,
                out string normalizedName,
                out XenogermPlanNameValidationFailure failure);

            Assert.That(valid, Is.True);
            Assert.That(normalizedName, Is.EqualTo("План  Ω"));
            Assert.That(failure, Is.EqualTo(XenogermPlanNameValidationFailure.None));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void TryValidate_InvalidName_ReturnsInvalidName(string requestedName)
        {
            bool valid = XenogermPlanNameAllocator.TryValidate(
                [],
                requestedName,
                null,
                out string normalizedName,
                out XenogermPlanNameValidationFailure failure);

            Assert.That(valid, Is.False);
            Assert.That(normalizedName, Is.Null);
            Assert.That(failure, Is.EqualTo(XenogermPlanNameValidationFailure.InvalidName));
        }

        [Test]
        public void TryValidate_ExistingNameIgnoringCase_ReturnsConflict()
        {
            XenogermPlan[] plans = [CreatePlan("plan-a", "Alpha")];

            bool valid = XenogermPlanNameAllocator.TryValidate(
                plans,
                "alpha",
                null,
                out string normalizedName,
                out XenogermPlanNameValidationFailure failure);

            Assert.That(valid, Is.False);
            Assert.That(normalizedName, Is.EqualTo("alpha"));
            Assert.That(failure, Is.EqualTo(XenogermPlanNameValidationFailure.NameConflict));
        }

        [Test]
        public void TryValidate_TrimsExistingNamesBeforeComparison()
        {
            XenogermPlan[] plans = [CreatePlan("plan-a", "  Alpha  ")];

            bool valid = XenogermPlanNameAllocator.TryValidate(
                plans,
                "Alpha",
                null,
                out _,
                out XenogermPlanNameValidationFailure failure);

            Assert.That(valid, Is.False);
            Assert.That(failure, Is.EqualTo(XenogermPlanNameValidationFailure.NameConflict));
        }

        [Test]
        public void TryValidate_DifferentInternalWhitespace_RemainsAvailable()
        {
            XenogermPlan[] plans = [CreatePlan("plan-a", "Plan A")];

            bool valid = XenogermPlanNameAllocator.TryValidate(
                plans,
                "Plan  A",
                null,
                out string normalizedName,
                out XenogermPlanNameValidationFailure failure);

            Assert.That(valid, Is.True);
            Assert.That(normalizedName, Is.EqualTo("Plan  A"));
            Assert.That(failure, Is.EqualTo(XenogermPlanNameValidationFailure.None));
        }

        [Test]
        public void TryValidate_UsesOrdinalIgnoreCaseInsteadOfCurrentCulture()
        {
            CultureInfo previousCulture = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                XenogermPlan[] plans = [CreatePlan("plan-a", "I")];

                bool valid = XenogermPlanNameAllocator.TryValidate(
                    plans,
                    "i",
                    null,
                    out _,
                    out XenogermPlanNameValidationFailure failure);

                Assert.That(valid, Is.False);
                Assert.That(failure, Is.EqualTo(XenogermPlanNameValidationFailure.NameConflict));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        [Test]
        public void TryValidate_ExcludedStableId_AllowsOwnName()
        {
            XenogermPlan plan = CreatePlan("plan-a", "Alpha");

            bool valid = XenogermPlanNameAllocator.TryValidate(
                [plan],
                "alpha",
                plan.Id,
                out string normalizedName,
                out XenogermPlanNameValidationFailure failure);

            Assert.That(valid, Is.True);
            Assert.That(normalizedName, Is.EqualTo("alpha"));
            Assert.That(failure, Is.EqualTo(XenogermPlanNameValidationFailure.None));
        }

        [Test]
        public void TryValidate_ExcludedStableId_DoesNotHideAnotherConflict()
        {
            XenogermPlan firstPlan = CreatePlan("plan-a", "Alpha");
            XenogermPlan secondPlan = CreatePlan("plan-b", "alpha");

            bool valid = XenogermPlanNameAllocator.TryValidate(
                [firstPlan, secondPlan],
                "Alpha",
                firstPlan.Id,
                out _,
                out XenogermPlanNameValidationFailure failure);

            Assert.That(valid, Is.False);
            Assert.That(failure, Is.EqualTo(XenogermPlanNameValidationFailure.NameConflict));
        }

        [Test]
        public void TryValidate_NullPlans_Throws()
        {
            Assert.That(
                (Action)(() => XenogermPlanNameAllocator.TryValidate(null, "Plan", null, out _, out _)),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Allocate_FreeName_ReturnsTrimmedPreferredName()
        {
            string allocated = XenogermPlanNameAllocator.Allocate([], "  My Plan  ");

            Assert.That(allocated, Is.EqualTo("My Plan"));
        }

        [Test]
        public void Allocate_OccupiedBaseName_UsesSuffixTwo()
        {
            XenogermPlan[] plans = [CreatePlan("plan-a", "Plan")];

            string allocated = XenogermPlanNameAllocator.Allocate(plans, "Plan");

            Assert.That(allocated, Is.EqualTo("Plan 2"));
        }

        [Test]
        public void Allocate_OccupiedSequence_UsesFirstAvailableSuffix()
        {
            XenogermPlan[] plans =
            [
                CreatePlan("plan-a", "Plan"),
                CreatePlan("plan-b", "Plan 2"),
                CreatePlan("plan-c", "Plan 3")
            ];

            string allocated = XenogermPlanNameAllocator.Allocate(plans, "Plan");

            Assert.That(allocated, Is.EqualTo("Plan 4"));
        }

        [Test]
        public void Allocate_CaseInsensitiveSequence_PreservesPreferredCasing()
        {
            XenogermPlan[] plans =
            [
                CreatePlan("plan-a", "plan"),
                CreatePlan("plan-b", "PLAN 2"),
                CreatePlan("plan-c", "pLaN 3")
            ];

            string allocated = XenogermPlanNameAllocator.Allocate(plans, "Plan");

            Assert.That(allocated, Is.EqualTo("Plan 4"));
        }

        [Test]
        public void Allocate_PreferredNameWithNumericSuffix_ContinuesSequence()
        {
            XenogermPlan[] plans =
            [
                CreatePlan("plan-a", "Plan 2"),
                CreatePlan("plan-b", "Plan 3")
            ];

            string allocated = XenogermPlanNameAllocator.Allocate(plans, "Plan 2");

            Assert.That(allocated, Is.EqualTo("Plan 4"));
        }

        [Test]
        public void Allocate_PreferredCopyNameWithNumericSuffix_ContinuesSequence()
        {
            XenogermPlan[] plans =
            [
                CreatePlan("plan-a", "Plan copy 2"),
                CreatePlan("plan-b", "Plan copy 3")
            ];

            string allocated = XenogermPlanNameAllocator.Allocate(plans, "Plan copy 2");

            Assert.That(allocated, Is.EqualTo("Plan copy 4"));
        }

        [Test]
        public void Allocate_InternalWhitespaceAndUnicodeBase_ArePreserved()
        {
            XenogermPlan[] plans = [CreatePlan("plan-a", "План  Ω")];

            string allocated = XenogermPlanNameAllocator.Allocate(plans, "  План  Ω  ");

            Assert.That(allocated, Is.EqualTo("План  Ω 2"));
        }

        [Test]
        public void Allocate_ExcludedStableId_IgnoresOwnName()
        {
            XenogermPlan plan = CreatePlan("plan-a", "Plan");

            string allocated = XenogermPlanNameAllocator.Allocate([plan], " Plan ", plan.Id);

            Assert.That(allocated, Is.EqualTo("Plan"));
        }

        [Test]
        public void Allocate_SameInputs_ReturnSameResult()
        {
            XenogermPlan[] plans =
            [
                CreatePlan("plan-a", "Plan"),
                CreatePlan("plan-b", "Plan 2")
            ];

            string first = XenogermPlanNameAllocator.Allocate(plans, "Plan");
            string second = XenogermPlanNameAllocator.Allocate(plans, "Plan");

            Assert.That(first, Is.EqualTo("Plan 3"));
            Assert.That(second, Is.EqualTo(first));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Allocate_InvalidPreferredName_Throws(string preferredName)
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(
                (Action)(() => XenogermPlanNameAllocator.Allocate([], preferredName)));

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.ParamName, Is.EqualTo("preferredName"));
        }

        [Test]
        public void Allocate_NullPlans_Throws()
        {
            Assert.That(
                (Action)(() => XenogermPlanNameAllocator.Allocate(null, "Plan")),
                Throws.TypeOf<ArgumentNullException>());
        }

        private static XenogermPlan CreatePlan(string id, string name)
        {
            return new XenogermPlan(id, name, [], [], PlanReadinessMode.Coverage);
        }
    }
}