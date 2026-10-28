using System.Collections.Generic;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Genes;
using XenogermPlanner.Tests.Donors;
using XenogermPlanner.UI;

namespace XenogermPlanner.Tests.UI
{
    [TestFixture]
    public sealed class PotentialDonorPresentationProjectionTests
    {
        [Test]
        public void GetOrBuild_SortsPreparedRowsAndPreservesExactPawnReferences()
        {
            Pawn first = PlanPotentialDonorTestData.CreatePawn();
            Pawn second = PlanPotentialDonorTestData.CreatePawn();
            Pawn third = PlanPotentialDonorTestData.CreatePawn();
            var names = new Dictionary<Pawn, string>(ReferenceEqualityComparer<Pawn>.Instance)
            {
                { first, "beta" },
                { second, "Alpha" },
                { third, "alpha" }
            };
            var keys = new Dictionary<Pawn, int>(ReferenceEqualityComparer<Pawn>.Instance)
            {
                { first, 3 },
                { second, 2 },
                { third, 1 }
            };
            var cache = new PotentialDonorPresentationProjectionCache();

            PotentialDonorPresentationProjection projection = cache.GetOrBuild(
                [first, second, third],
                "English",
                pawn => names[pawn],
                pawn => keys[pawn]);

            Assert.That(projection.Rows, Has.Count.EqualTo(3));
            Assert.That(projection.Rows[0].Donor, Is.SameAs(third));
            Assert.That(projection.Rows[0].DisplayName, Is.EqualTo("alpha"));
            Assert.That(projection.Rows[1].Donor, Is.SameAs(second));
            Assert.That(projection.Rows[2].Donor, Is.SameAs(first));
        }

        [Test]
        public void GetOrBuild_ReusesProjectionOnlyWhileInputsRemainCompatible()
        {
            Pawn donor = PlanPotentialDonorTestData.CreatePawn();
            var cache = new PotentialDonorPresentationProjectionCache();

            PotentialDonorPresentationProjection first = cache.GetOrBuild([donor], "English", _ => "Donor", _ => 10);
            PotentialDonorPresentationProjection second = cache.GetOrBuild([donor], "English", _ => "Donor", _ => 10);
            PotentialDonorPresentationProjection renamed = cache.GetOrBuild(
                [donor],
                "English",
                _ => "Renamed",
                _ => 10);
            PotentialDonorPresentationProjection otherLanguage = cache.GetOrBuild(
                [donor],
                "Other",
                _ => "Renamed",
                _ => 10);

            Assert.That(second, Is.SameAs(first));
            Assert.That(renamed, Is.Not.SameAs(first));
            Assert.That(renamed.Rows[0].DisplayName, Is.EqualTo("Renamed"));
            Assert.That(otherLanguage, Is.Not.SameAs(renamed));
        }

        [Test]
        public void GetOrBuild_InvalidatesForMembershipAndExactReferenceChanges()
        {
            Pawn first = PlanPotentialDonorTestData.CreatePawn();
            Pawn second = PlanPotentialDonorTestData.CreatePawn();
            Pawn replacement = PlanPotentialDonorTestData.CreatePawn();
            var cache = new PotentialDonorPresentationProjectionCache();

            PotentialDonorPresentationProjection initial = cache.GetOrBuild([first], "English", _ => "Donor", _ => 1);
            PotentialDonorPresentationProjection added = cache.GetOrBuild(
                [first, second],
                "English",
                _ => "Donor",
                pawn => ReferenceEquals(pawn, first) ? 1 : 2);
            PotentialDonorPresentationProjection replaced = cache.GetOrBuild(
                [replacement],
                "English",
                _ => "Donor",
                _ => 1);

            Assert.That(added, Is.Not.SameAs(initial));
            Assert.That(replaced, Is.Not.SameAs(added));
            Assert.That(replaced.Rows[0].Donor, Is.SameAs(replacement));
        }

        [Test]
        public void GetOrBuild_CopiesRowsFromMutableInputCollection()
        {
            Pawn donor = PlanPotentialDonorTestData.CreatePawn();
            var donors = new List<Pawn> { donor };
            var cache = new PotentialDonorPresentationProjectionCache();

            PotentialDonorPresentationProjection projection = cache.GetOrBuild(donors, "English", _ => "Donor", _ => 1);

            donors.Clear();

            Assert.That(projection.Rows, Has.Count.EqualTo(1));
            Assert.That(projection.Rows[0].Donor, Is.SameAs(donor));
        }

        [Test]
        public void Invalidate_ForcesProjectionRebuild()
        {
            Pawn donor = PlanPotentialDonorTestData.CreatePawn();
            var cache = new PotentialDonorPresentationProjectionCache();
            PotentialDonorPresentationProjection first = cache.GetOrBuild([donor], "English", _ => "Donor", _ => 1);

            cache.Invalidate();

            PotentialDonorPresentationProjection rebuilt = cache.GetOrBuild([donor], "English", _ => "Donor", _ => 1);

            Assert.That(rebuilt, Is.Not.SameAs(first));
        }
    }
}