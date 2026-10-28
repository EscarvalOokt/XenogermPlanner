using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace XenogermPlanner.Genes
{
    internal static class GenepackCompositionUtility
    {
        internal static IReadOnlyCollection<GeneDef> GetGenes(Genepack genepack)
        {
            if (genepack == null)
                throw new ArgumentNullException(nameof(genepack));

            GeneSet geneSet = genepack.GeneSet ??
                              throw new InvalidOperationException("Genepack does not have a gene set.");

            List<GeneDef> genes = geneSet.GenesListForReading;

            if (genes == null)
                throw new InvalidOperationException("Genepack gene collection is unavailable.");

            return CopyDistinctGenes(genes, nameof(genepack));
        }

        internal static bool TryCopyDistinctGenes(IEnumerable<GeneDef> genes, out HashSet<GeneDef> distinctGenes)
        {
            distinctGenes = new HashSet<GeneDef>();

            if (genes == null)
                return false;

            foreach (GeneDef gene in genes)
            {
                if (gene == null)
                {
                    distinctGenes.Clear();
                    return false;
                }

                distinctGenes.Add(gene);
            }

            return true;
        }

        internal static HashSet<GeneDef> CopyDistinctGenes(IEnumerable<GeneDef> genes, string parameterName)
        {
            if (genes == null)
                throw new ArgumentNullException(parameterName);

            var copiedGenes = new HashSet<GeneDef>();

            foreach (GeneDef gene in genes)
            {
                if (gene == null)
                    throw new ArgumentException("Gene collection cannot contain null values.", parameterName);

                copiedGenes.Add(gene);
            }

            return copiedGenes;
        }

        internal static string CreateCompositionKey(IEnumerable<GeneDef> genes)
        {
            HashSet<GeneDef> distinctGenes = CopyDistinctGenes(genes, nameof(genes));

            return string.Join(
                "\u001f",
                distinctGenes.Select(gene => gene.defName ?? string.Empty)
                    .OrderBy(defName => defName, StringComparer.Ordinal));
        }

        internal static bool CompositionsMatch(IEnumerable<GeneDef> leftGenes, IEnumerable<GeneDef> rightGenes)
        {
            HashSet<GeneDef> left = CopyDistinctGenes(leftGenes, nameof(leftGenes));
            HashSet<GeneDef> right = CopyDistinctGenes(rightGenes, nameof(rightGenes));

            return left.SetEquals(right);
        }

        internal static bool TryCompositionsMatch(IEnumerable<GeneDef> leftGenes, IEnumerable<GeneDef> rightGenes)
        {
            return TryCopyDistinctGenes(leftGenes, out HashSet<GeneDef> left) &&
                   TryCopyDistinctGenes(rightGenes, out HashSet<GeneDef> right) &&
                   left.SetEquals(right);
        }

        internal static string GetStablePhysicalKey(Genepack genepack)
        {
            if (genepack == null)
                throw new ArgumentNullException(nameof(genepack));

            return genepack.ThingID ?? string.Empty;
        }

        internal static int ComparePhysicalGenepacks(Genepack left, Genepack right)
        {
            if (ReferenceEquals(left, right))
                return 0;

            if (left == null)
                return 1;

            if (right == null)
                return -1;

            return StringComparer.Ordinal.Compare(GetStablePhysicalKey(left), GetStablePhysicalKey(right));
        }
    }
}