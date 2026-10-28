using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RimWorld;
using Verse;

namespace XenogermPlanner.Plans
{
    internal enum CustomXenogermPlanImportFailure
    {
        None,
        SourceUnavailable,
        InvalidSourceData,
        EmptySource
    }

    internal sealed class CustomXenogermPlanImportData
    {
        private readonly ReadOnlyCollection<GeneDef> _desiredGenes;

        internal CustomXenogermPlanImportData(string name, IEnumerable<GeneDef> desiredGenes)
        {
            if (desiredGenes == null)
                throw new ArgumentNullException(nameof(desiredGenes));

            var distinctGenes = new HashSet<GeneDef>();

            foreach (GeneDef gene in desiredGenes)
            {
                if (gene == null)
                    throw new ArgumentException(
                        "Desired gene collection cannot contain null values.",
                        nameof(desiredGenes));

                distinctGenes.Add(gene);
            }

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (!XenogermPlanNameAllocator.TryNormalize(name, out string normalizedName))
            {
                throw new ArgumentException("Imported plan name cannot be empty or whitespace.", nameof(name));
            }

            Name = normalizedName;
            _desiredGenes = new List<GeneDef>(distinctGenes).AsReadOnly();
        }

        internal string Name { get; }
        internal IReadOnlyCollection<GeneDef> DesiredGenes => _desiredGenes;
    }

    internal static class CustomXenogermPlanImporter
    {
        internal static bool TryReadSource(
            CustomXenogerm source,
            out CustomXenogermPlanImportData importData,
            out CustomXenogermPlanImportFailure failure)
        {
            importData = null;
            failure = CustomXenogermPlanImportFailure.None;

            if (source == null)
            {
                failure = CustomXenogermPlanImportFailure.SourceUnavailable;

                return false;
            }

            if (!XenogermPlanNameAllocator.TryNormalize(source.name, out string normalizedName) ||
                source.genesets == null)
            {
                failure = CustomXenogermPlanImportFailure.InvalidSourceData;

                return false;
            }

            if (source.genesets.Count == 0)
            {
                failure = CustomXenogermPlanImportFailure.EmptySource;

                return false;
            }

            var desiredGenes = new HashSet<GeneDef>();

            foreach (GeneSet geneSet in source.genesets)
            {
                if (geneSet == null)
                {
                    failure = CustomXenogermPlanImportFailure.InvalidSourceData;

                    return false;
                }

                List<GeneDef> genes = geneSet.GenesListForReading;

                if (genes == null)
                {
                    failure = CustomXenogermPlanImportFailure.InvalidSourceData;

                    return false;
                }

                if (genes.Count == 0)
                {
                    failure = CustomXenogermPlanImportFailure.EmptySource;

                    return false;
                }

                foreach (GeneDef gene in genes)
                {
                    if (gene == null)
                    {
                        failure = CustomXenogermPlanImportFailure.InvalidSourceData;

                        return false;
                    }

                    desiredGenes.Add(gene);
                }
            }

            if (desiredGenes.Count == 0)
            {
                failure = CustomXenogermPlanImportFailure.EmptySource;

                return false;
            }

            importData = new CustomXenogermPlanImportData(normalizedName, desiredGenes);

            return true;
        }
    }
}