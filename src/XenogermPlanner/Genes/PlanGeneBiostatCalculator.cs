using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace XenogermPlanner.Genes
{
    internal static class PlanGeneBiostatCalculator
    {
        internal static PlanGeneBiostats CalculateRaw(IEnumerable<GeneDef> genes)
        {
            return SumBiostats(CopyAndValidateGenes(genes, nameof(genes)));
        }

        internal static PlanGeneBiostats CalculateEffective(IEnumerable<GeneDef> genes)
        {
            return CalculateEffective(genes, GetNonOverriddenGenes);
        }

        internal static PlanGeneBiostats CalculateEffective(
            IEnumerable<GeneDef> genes,
            Func<IEnumerable<GeneDef>, IEnumerable<GeneDef>> getNonOverriddenGenes)
        {
            if (getNonOverriddenGenes == null)
                throw new ArgumentNullException(nameof(getNonOverriddenGenes));

            List<GeneDef> validatedGenes = CopyAndValidateGenes(genes, nameof(genes));
            IEnumerable<GeneDef> projectedGenes = getNonOverriddenGenes(validatedGenes);

            if (projectedGenes == null)
            {
                throw new InvalidOperationException("The non-overridden gene projection returned no collection.");
            }

            var complexity = 0;
            var metabolism = 0;
            var architeCapsules = 0;
            var seenGenes = new HashSet<GeneDef>();

            foreach (GeneDef gene in projectedGenes)
            {
                if (gene == null)
                {
                    throw new InvalidOperationException("The non-overridden gene projection contained a null gene.");
                }

                if (seenGenes.Add(gene))
                {
                    complexity += gene.biostatCpx;
                    metabolism += gene.biostatMet;
                    architeCapsules += gene.biostatArc;
                }
            }

            return new PlanGeneBiostats(complexity, metabolism, architeCapsules);
        }

        private static List<GeneDef> CopyAndValidateGenes(IEnumerable<GeneDef> genes, string parameterName)
        {
            if (genes == null)
                throw new ArgumentNullException(parameterName);

            var copiedGenes = new List<GeneDef>();

            foreach (GeneDef gene in genes)
            {
                if (gene == null)
                    throw new ArgumentException("Gene collection cannot contain null values.", parameterName);

                copiedGenes.Add(gene);
            }

            return copiedGenes;
        }

        private static PlanGeneBiostats SumBiostats(IEnumerable<GeneDef> genes)
        {
            var complexity = 0;
            var metabolism = 0;
            var architeCapsules = 0;

            foreach (GeneDef gene in genes)
            {
                complexity += gene.biostatCpx;
                metabolism += gene.biostatMet;
                architeCapsules += gene.biostatArc;
            }

            return new PlanGeneBiostats(complexity, metabolism, architeCapsules);
        }

        private static IEnumerable<GeneDef> GetNonOverriddenGenes(IEnumerable<GeneDef> genes)
        {
            var genesWithType = new List<GeneDefWithType>();

            foreach (GeneDef gene in genes)
                genesWithType.Add(new GeneDefWithType(gene, true));

            return genesWithType.NonOverriddenGenes();
        }
    }
}