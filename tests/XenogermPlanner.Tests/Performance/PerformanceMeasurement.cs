using System;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;

namespace XenogermPlanner.Tests.Performance
{
    internal static class PerformanceMeasurement
    {
        private const int WarmUpCount = 10;
        private const int TimingSampleCount = 100;
        private const int AllocationSampleCount = 100;

        private static readonly Func<long> _getAllocatedBytes = CreateAllocatedBytesReader();

        internal static void Measure<T>(
            string scenarioName,
            Func<T> operation,
            Action<T> assertResult,
            params string[] metadataLines)
        {
            if (string.IsNullOrWhiteSpace(scenarioName))
            {
                throw new ArgumentException(
                    "Performance scenario name cannot be null, empty or whitespace.",
                    nameof(scenarioName));
            }

            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (assertResult == null)
                throw new ArgumentNullException(nameof(assertResult));

            if (metadataLines == null)
                throw new ArgumentNullException(nameof(metadataLines));

            foreach (string metadataLine in metadataLines)
            {
                if (metadataLine == null)
                {
                    throw new ArgumentException(
                        "Performance metadata cannot contain null values.",
                        nameof(metadataLines));
                }
            }

            var lastResult = default(T);

            for (var index = 0; index < WarmUpCount; index++)
                lastResult = operation();

            ForceFullCollection();

            var timingSamples = new double[TimingSampleCount];

            for (var index = 0; index < TimingSampleCount; index++)
            {
                long startedAt = Stopwatch.GetTimestamp();

                lastResult = operation();

                long completedAt = Stopwatch.GetTimestamp();

                timingSamples[index] = (completedAt - startedAt) * 1000d / Stopwatch.Frequency;
            }

            Array.Sort(timingSamples);

            double timingMedian = CalculateMedian(timingSamples);
            double timingP95 = CalculatePercentile95(timingSamples);
            double timingMaximum = timingSamples[TimingSampleCount - 1];

            ForceFullCollection();

            var allocationSamples = new long[AllocationSampleCount];

            int gen0CollectionsBefore = GC.CollectionCount(0);
            int gen1CollectionsBefore = GC.CollectionCount(1);
            int gen2CollectionsBefore = GC.CollectionCount(2);

            for (var index = 0; index < AllocationSampleCount; index++)
            {
                long allocatedBytesBefore = _getAllocatedBytes();

                lastResult = operation();

                long allocatedBytesAfter = _getAllocatedBytes();

                allocationSamples[index] = allocatedBytesAfter - allocatedBytesBefore;
            }

            int gen0Collections = GC.CollectionCount(0) - gen0CollectionsBefore;
            int gen1Collections = GC.CollectionCount(1) - gen1CollectionsBefore;
            int gen2Collections = GC.CollectionCount(2) - gen2CollectionsBefore;

            Array.Sort(allocationSamples);

            double allocationMedian = CalculateMedian(allocationSamples);
            long allocationP95 = CalculatePercentile95(allocationSamples);
            long allocationMaximum = allocationSamples[AllocationSampleCount - 1];

            TestContext.Progress.WriteLine(scenarioName);

            foreach (string metadataLine in metadataLines)
                TestContext.Progress.WriteLine(metadataLine);

            TestContext.Progress.WriteLine($"Timing samples: {TimingSampleCount}");
            TestContext.Progress.WriteLine($"Allocation samples: {AllocationSampleCount}");
            TestContext.Progress.WriteLine(string.Empty);

            TestContext.Progress.WriteLine("Timing:");
            TestContext.Progress.WriteLine($"Median: {timingMedian:F3} ms");
            TestContext.Progress.WriteLine($"P95:    {timingP95:F3} ms");
            TestContext.Progress.WriteLine($"Max:    {timingMaximum:F3} ms");
            TestContext.Progress.WriteLine(string.Empty);

            TestContext.Progress.WriteLine("Managed allocation:");
            TestContext.Progress.WriteLine($"Median: {allocationMedian:F0} B");
            TestContext.Progress.WriteLine($"P95:    {allocationP95} B");
            TestContext.Progress.WriteLine($"Max:    {allocationMaximum} B");
            TestContext.Progress.WriteLine(string.Empty);

            TestContext.Progress.WriteLine("GC collections during allocation samples:");
            TestContext.Progress.WriteLine($"Gen 0: {gen0Collections}");
            TestContext.Progress.WriteLine($"Gen 1: {gen1Collections}");
            TestContext.Progress.WriteLine($"Gen 2: {gen2Collections}");

            assertResult(lastResult);
        }

        private static Func<long> CreateAllocatedBytesReader()
        {
            MethodInfo method = typeof(GC).GetMethod(
                "GetAllocatedBytesForCurrentThread",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);

            if (method == null || method.ReturnType != typeof(long))
            {
                throw new NotSupportedException(
                    "The current runtime does not expose GC.GetAllocatedBytesForCurrentThread().");
            }

            return (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), method);
        }

        private static void ForceFullCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private static double CalculateMedian(double[] sortedSamples)
        {
            if (sortedSamples == null)
                throw new ArgumentNullException(nameof(sortedSamples));

            if (sortedSamples.Length == 0)
            {
                throw new ArgumentException(
                    "Median cannot be calculated for an empty sample collection.",
                    nameof(sortedSamples));
            }

            int middleIndex = sortedSamples.Length / 2;

            if (sortedSamples.Length % 2 == 0)
                return (sortedSamples[middleIndex - 1] + sortedSamples[middleIndex]) / 2d;

            return sortedSamples[middleIndex];
        }

        private static double CalculateMedian(long[] sortedSamples)
        {
            if (sortedSamples == null)
                throw new ArgumentNullException(nameof(sortedSamples));

            if (sortedSamples.Length == 0)
            {
                throw new ArgumentException(
                    "Median cannot be calculated for an empty sample collection.",
                    nameof(sortedSamples));
            }

            int middleIndex = sortedSamples.Length / 2;

            if (sortedSamples.Length % 2 == 0)
                return (sortedSamples[middleIndex - 1] + sortedSamples[middleIndex]) / 2d;

            return sortedSamples[middleIndex];
        }

        private static double CalculatePercentile95(double[] sortedSamples)
        {
            if (sortedSamples == null)
                throw new ArgumentNullException(nameof(sortedSamples));

            if (sortedSamples.Length == 0)
            {
                throw new ArgumentException(
                    "Percentile cannot be calculated for an empty sample collection.",
                    nameof(sortedSamples));
            }

            int percentileIndex = (int)Math.Ceiling(sortedSamples.Length * 0.95d) - 1;
            return sortedSamples[percentileIndex];
        }

        private static long CalculatePercentile95(long[] sortedSamples)
        {
            if (sortedSamples == null)
                throw new ArgumentNullException(nameof(sortedSamples));

            if (sortedSamples.Length == 0)
            {
                throw new ArgumentException(
                    "Percentile cannot be calculated for an empty sample collection.",
                    nameof(sortedSamples));
            }

            int percentileIndex = (int)Math.Ceiling(sortedSamples.Length * 0.95d) - 1;
            return sortedSamples[percentileIndex];
        }
    }
}