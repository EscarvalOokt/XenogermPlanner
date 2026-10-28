using System.Collections.Generic;
using NUnit.Framework;
using XenogermPlanner.UI;

namespace XenogermPlanner.Tests.UI
{
    [TestFixture]
    public sealed class XenogermPlanSourceListProjectionTests
    {
        [Test]
        public void Build_ExpandedCollapsibleGroupIncludesHeaderAndSources()
        {
            XenogermPlanSourceGroup group = CreateGroup("premade", true, "A", "B");

            List<XenogermPlanSourceListRow> rows = XenogermPlanSourceListProjection.Build(
                [group],
                new HashSet<string>());

            Assert.That(rows.Count, Is.EqualTo(3));
            Assert.That(rows[0].Kind, Is.EqualTo(XenogermPlanSourceListRowKind.Group));
            Assert.That(rows[0].Group, Is.SameAs(group));
            Assert.That(rows[0].IsGroupExpanded, Is.True);
            Assert.That(rows[1].Kind, Is.EqualTo(XenogermPlanSourceListRowKind.Source));
            Assert.That(rows[1].Source.DisplayName, Is.EqualTo("A"));
            Assert.That(rows[2].Source.DisplayName, Is.EqualTo("B"));
        }

        [Test]
        public void Build_CollapsedGroupIncludesOnlyHeader()
        {
            XenogermPlanSourceGroup group = CreateGroup("saved", true, "A", "B");

            List<XenogermPlanSourceListRow> rows = XenogermPlanSourceListProjection.Build(
                [group],
                new HashSet<string> { "saved" });

            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].Kind, Is.EqualTo(XenogermPlanSourceListRowKind.Group));
            Assert.That(rows[0].IsGroupExpanded, Is.False);
        }

        [Test]
        public void Build_CollapsingOneGroupDoesNotAffectAnotherGroup()
        {
            XenogermPlanSourceGroup first = CreateGroup("first", true, "A");
            XenogermPlanSourceGroup second = CreateGroup("second", true, "B");

            List<XenogermPlanSourceListRow> rows = XenogermPlanSourceListProjection.Build(
                [first, second],
                new HashSet<string> { "first" });

            Assert.That(rows.Count, Is.EqualTo(3));
            Assert.That(rows[0].Group, Is.SameAs(first));
            Assert.That(rows[0].IsGroupExpanded, Is.False);
            Assert.That(rows[1].Group, Is.SameAs(second));
            Assert.That(rows[1].IsGroupExpanded, Is.True);
            Assert.That(rows[2].Source.DisplayName, Is.EqualTo("B"));
        }

        [Test]
        public void Build_NonCollapsibleGroupDoesNotEmitHeader()
        {
            XenogermPlanSourceGroup group = CreateGroup("templates", false, "A", "B");

            List<XenogermPlanSourceListRow> rows = XenogermPlanSourceListProjection.Build(
                [group],
                new HashSet<string>());

            Assert.That(rows.Count, Is.EqualTo(2));
            Assert.That(rows[0].Kind, Is.EqualTo(XenogermPlanSourceListRowKind.Source));
            Assert.That(rows[0].Source.DisplayName, Is.EqualTo("A"));
            Assert.That(rows[1].Source.DisplayName, Is.EqualTo("B"));
        }

        [Test]
        public void Build_PreservesGroupAndSourceOrder()
        {
            XenogermPlanSourceGroup first = CreateGroup("first", true, "B", "A");
            XenogermPlanSourceGroup second = CreateGroup("second", true, "D", "C");

            List<XenogermPlanSourceListRow> rows = XenogermPlanSourceListProjection.Build(
                [first, second],
                new HashSet<string>());

            Assert.That(rows[1].Source.DisplayName, Is.EqualTo("B"));
            Assert.That(rows[2].Source.DisplayName, Is.EqualTo("A"));
            Assert.That(rows[4].Source.DisplayName, Is.EqualTo("D"));
            Assert.That(rows[5].Source.DisplayName, Is.EqualTo("C"));
        }

        private static XenogermPlanSourceGroup CreateGroup(string key, bool isCollapsible, params string[] names)
        {
            var sources = new List<XenogermPlanSourceEntry>();

            foreach (string name in names)
            {
                sources.Add(
                    new XenogermPlanSourceEntry(
                        stableKey: name,
                        displayName: name,
                        metadataKey: "Metadata",
                        sourceToken: new object()));
            }

            return new XenogermPlanSourceGroup(key, isCollapsible ? "GroupLabel" : null, isCollapsible, sources);
        }
    }
}