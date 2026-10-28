using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RimWorld;
using XenogermPlanner.Tests.Plans;
using XenogermPlanner.UI;

namespace XenogermPlanner.Tests.UI
{
    [TestFixture]
    public sealed class XenotypePlanSourceProviderTests
    {
        [SetUp]
        public void SetUp()
        {
            _temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                "XenogermPlannerTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_temporaryDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_temporaryDirectory))
                Directory.Delete(_temporaryDirectory, recursive: true);
        }

        private string _temporaryDirectory;

        [Test]
        public void Refresh_SeparatesPremadeAndSavedSources()
        {
            XenotypeDef premade = CreatePremade("Premade", displayPriority: 1f);
            FileInfo saved = CreateSavedFile("Saved.xtp", "saved");
            XenotypePlanSourceProvider provider = CreateProvider([premade], [saved], out _, out _);

            Assert.That(provider.Groups.Count, Is.EqualTo(2));
            Assert.That(provider.Groups[0].Key, Is.EqualTo("premade-xenotypes"));
            Assert.That(provider.Groups[0].IsCollapsible, Is.True);
            Assert.That(provider.Groups[0].Sources.Single().DisplayName, Is.EqualTo("Premade"));
            Assert.That(provider.Groups[1].Key, Is.EqualTo("saved-xenotypes"));
            Assert.That(provider.Groups[1].IsCollapsible, Is.True);
            Assert.That(provider.Groups[1].Sources.Single().DisplayName, Is.EqualTo("Saved"));
        }

        [Test]
        public void Refresh_PremadeSourcesPreserveDisplayPriorityOrdering()
        {
            XenotypeDef low = CreatePremade("Low", displayPriority: 1f);
            XenotypeDef high = CreatePremade("High", displayPriority: 5f);
            XenotypePlanSourceProvider provider = CreateProvider([low, high], [], out _, out _);

            Assert.That(provider.Groups[0].Sources[0].DisplayName, Is.EqualTo("High"));
            Assert.That(provider.Groups[0].Sources[1].DisplayName, Is.EqualTo("Low"));
        }

        [Test]
        public void Refresh_DoesNotLoadSavedXenotypeFiles()
        {
            FileInfo saved = CreateSavedFile("Saved.xtp", "saved");
            XenotypePlanSourceProvider provider = CreateProvider(
                [],
                [saved],
                out Func<int> getVersionCheckCount,
                out Func<int> getLoadCount);

            provider.Refresh();

            Assert.That(getVersionCheckCount(), Is.Zero);
            Assert.That(getLoadCount(), Is.Zero);
        }

        [Test]
        public void Resolve_SavedSourceLoadsThroughNativeBoundaryAndCachesUnchangedFile()
        {
            FileInfo saved = CreateSavedFile("Saved.xtp", "saved");
            XenotypePlanSourceProvider provider = CreateProvider(
                [],
                [saved],
                out Func<int> getVersionCheckCount,
                out Func<int> getLoadCount);
            XenogermPlanSourceEntry entry = provider.Groups[1].Sources[0];

            XenogermPlanSourceResolveResult first = Resolve(provider, entry, revalidate: false);
            XenogermPlanSourceResolveResult second = Resolve(provider, entry, revalidate: true);

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(second.IsSuccess, Is.True);
            Assert.That(first.Selection.Name, Is.EqualTo("Saved custom"));
            Assert.That(first.Selection.DesiredGenes.Count, Is.EqualTo(1));
            Assert.That(getVersionCheckCount(), Is.EqualTo(1));
            Assert.That(getLoadCount(), Is.EqualTo(1));
        }

        [Test]
        public void Resolve_SavedSourceSupportsDeferredVersionCheckCallback()
        {
            FileInfo saved = CreateSavedFile("Saved.xtp", "saved");
            Action deferredLoad = null;
            var provider = new XenotypePlanSourceProvider(
                Array.Empty<XenotypeDef>,
                () => [saved],
                (_, loadAction) => deferredLoad = loadAction,
                (_, out source) =>
                {
                    source = new CustomXenotype
                    {
                        name = "Saved custom",
                        genes = [PlanTestData.CreateGene("SavedGene")]
                    };
                    return true;
                });
            XenogermPlanSourceEntry entry = provider.Groups[1].Sources[0];
            XenogermPlanSourceResolveResult result = null;

            provider.Resolve(entry, false, resolved => result = resolved);

            Assert.That(result, Is.Null);
            Assert.That(deferredLoad, Is.Not.Null);

            deferredLoad();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void Resolve_SavedSourceReloadsWhenFileMetadataChanges()
        {
            FileInfo saved = CreateSavedFile("Saved.xtp", "saved");
            XenotypePlanSourceProvider provider = CreateProvider(
                [],
                [saved],
                out Func<int> getVersionCheckCount,
                out Func<int> getLoadCount);
            XenogermPlanSourceEntry entry = provider.Groups[1].Sources[0];

            Assert.That(Resolve(provider, entry, revalidate: false).IsSuccess, Is.True);

            File.AppendAllText(saved.FullName, "changed");
            saved.Refresh();

            Assert.That(Resolve(provider, entry, revalidate: true).IsSuccess, Is.True);
            Assert.That(getVersionCheckCount(), Is.EqualTo(2));
            Assert.That(getLoadCount(), Is.EqualTo(2));
        }

        [Test]
        public void Resolve_SavedSourceReportsLoadFailureWithoutRemovingSourceEntry()
        {
            FileInfo saved = CreateSavedFile("Saved.xtp", "saved");
            var provider = new XenotypePlanSourceProvider(
                Array.Empty<XenotypeDef>,
                () => [saved],
                (_, loadAction) => loadAction(),
                TryLoadSavedXenotype);
            XenogermPlanSourceEntry entry = provider.Groups[1].Sources[0];

            XenogermPlanSourceResolveResult result = Resolve(provider, entry, revalidate: false);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Failure, Is.EqualTo(XenogermPlanSourceFailure.LoadFailed));
            Assert.That(provider.Groups[1].Sources.Count, Is.EqualTo(1));
        }

        [Test]
        public void Resolve_SavedSourceRejectsDeletedFile()
        {
            FileInfo saved = CreateSavedFile("Saved.xtp", "saved");
            XenotypePlanSourceProvider provider = CreateProvider([], [saved], out _, out _);
            XenogermPlanSourceEntry entry = provider.Groups[1].Sources[0];

            File.Delete(saved.FullName);

            XenogermPlanSourceResolveResult result = Resolve(provider, entry, revalidate: true);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Failure, Is.EqualTo(XenogermPlanSourceFailure.SourceUnavailable));
        }

        [Test]
        public void Resolve_PremadeSourceRejectsSourceRemovedAfterEnumeration()
        {
            XenotypeDef premade = CreatePremade("Premade", displayPriority: 1f);
            var premadeSources = new List<XenotypeDef> { premade };
            var provider = new XenotypePlanSourceProvider(
                () => premadeSources,
                Array.Empty<FileInfo>,
                (_, loadAction) => loadAction(),
                TryLoadSavedXenotype);
            XenogermPlanSourceEntry entry = provider.Groups[0].Sources[0];

            premadeSources.Clear();

            XenogermPlanSourceResolveResult result = Resolve(provider, entry, revalidate: true);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Failure, Is.EqualTo(XenogermPlanSourceFailure.SourceUnavailable));
        }

        private XenotypePlanSourceProvider CreateProvider(
            IEnumerable<XenotypeDef> premadeSources,
            IEnumerable<FileInfo> savedSources,
            out Func<int> getVersionCheckCount,
            out Func<int> getLoadCount)
        {
            var versionCheckCount = 0;
            var loadCount = 0;

            var provider = new XenotypePlanSourceProvider(
                () => premadeSources,
                () => savedSources,
                (_, loadAction) =>
                {
                    versionCheckCount++;
                    loadAction();
                },
                (_, out source) =>
                {
                    loadCount++;
                    source = new CustomXenotype
                    {
                        name = "Saved custom",
                        genes = [PlanTestData.CreateGene("SavedGene")]
                    };
                    return true;
                });

            getVersionCheckCount = () => versionCheckCount;
            getLoadCount = () => loadCount;
            return provider;
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

        private static XenotypeDef CreatePremade(string name, float displayPriority)
        {
            return new XenotypeDef
            {
                defName = name + "Def",
                label = name,
                displayPriority = displayPriority,
                genes = [PlanTestData.CreateGene(name + "Gene")]
            };
        }

        private FileInfo CreateSavedFile(string name, string contents)
        {
            string path = Path.Combine(_temporaryDirectory, name);
            File.WriteAllText(path, contents);
            return new FileInfo(path);
        }

        private static bool TryLoadSavedXenotype(string path, out CustomXenotype source)
        {
            source = null;
            return false;
        }
    }
}