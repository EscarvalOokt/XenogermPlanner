using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RimWorld;
using Verse;

namespace XenogermPlanner.Plans
{
    internal enum XenotypePlanCreationSourceFailure
    {
        None,
        SourceUnavailable,
        InvalidSourceData,
        EmptySource
    }

    internal sealed class XenotypePlanCreationSourceData
    {
        private readonly ReadOnlyCollection<GeneDef> _desiredGenes;

        internal XenotypePlanCreationSourceData(string name, IEnumerable<GeneDef> desiredGenes)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (desiredGenes == null)
                throw new ArgumentNullException(nameof(desiredGenes));

            if (!XenogermPlanNameAllocator.TryNormalize(name, out string normalizedName))
                throw new ArgumentException("Xenotype source name cannot be empty or whitespace.", nameof(name));

            var distinctGenes = new HashSet<GeneDef>();

            foreach (GeneDef gene in desiredGenes)
            {
                if (gene == null)
                    throw new ArgumentException(
                        "Xenotype gene collection cannot contain null values.",
                        nameof(desiredGenes));

                distinctGenes.Add(gene);
            }

            if (distinctGenes.Count == 0)
                throw new ArgumentException("Xenotype gene collection cannot be empty.", nameof(desiredGenes));

            Name = normalizedName;
            _desiredGenes = new List<GeneDef>(distinctGenes).AsReadOnly();
        }

        internal string Name { get; }
        internal IReadOnlyCollection<GeneDef> DesiredGenes => _desiredGenes;
    }

    internal static class XenotypePlanCreationSourceReader
    {
        internal static bool TryReadSource(
            XenotypeDef source,
            out XenotypePlanCreationSourceData sourceData,
            out XenotypePlanCreationSourceFailure failure)
        {
            sourceData = null;
            failure = XenotypePlanCreationSourceFailure.None;

            if (source == null)
            {
                failure = XenotypePlanCreationSourceFailure.SourceUnavailable;
                return false;
            }

            var sourceName = source.LabelCap.ToString();

            if (!XenogermPlanNameAllocator.TryNormalize(sourceName, out _) || source.genes == null)
            {
                failure = XenotypePlanCreationSourceFailure.InvalidSourceData;
                return false;
            }

            return TryCreateSourceData(sourceName, source.genes, out sourceData, out failure);
        }

        internal static bool TryReadSource(
            CustomXenotype source,
            out XenotypePlanCreationSourceData sourceData,
            out XenotypePlanCreationSourceFailure failure)
        {
            sourceData = null;
            failure = XenotypePlanCreationSourceFailure.None;

            if (source == null)
            {
                failure = XenotypePlanCreationSourceFailure.SourceUnavailable;
                return false;
            }

            if (!XenogermPlanNameAllocator.TryNormalize(source.name, out _) || source.genes == null)
            {
                failure = XenotypePlanCreationSourceFailure.InvalidSourceData;
                return false;
            }

            return TryCreateSourceData(source.name, source.genes, out sourceData, out failure);
        }

        private static bool TryCreateSourceData(
            string sourceName,
            IEnumerable<GeneDef> genes,
            out XenotypePlanCreationSourceData sourceData,
            out XenotypePlanCreationSourceFailure failure)
        {
            sourceData = null;
            failure = XenotypePlanCreationSourceFailure.None;

            var distinctGenes = new HashSet<GeneDef>();

            foreach (GeneDef gene in genes)
            {
                if (gene == null)
                {
                    failure = XenotypePlanCreationSourceFailure.InvalidSourceData;
                    return false;
                }

                distinctGenes.Add(gene);
            }

            if (distinctGenes.Count == 0)
            {
                failure = XenotypePlanCreationSourceFailure.EmptySource;
                return false;
            }

            sourceData = new XenotypePlanCreationSourceData(sourceName, distinctGenes);
            return true;
        }
    }
}