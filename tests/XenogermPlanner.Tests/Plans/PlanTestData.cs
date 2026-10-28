using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace XenogermPlanner.Tests.Plans
{
    internal static class PlanTestData
    {
        internal static GeneDef CreateGene(string defName)
        {
            if (string.IsNullOrWhiteSpace(defName))
                throw new ArgumentException("Gene def name cannot be null, empty or whitespace.", nameof(defName));

            return new GeneDef
            {
                defName = defName
            };
        }

        internal static GeneSet CreateGeneSet(params GeneDef[] genes)
        {
            if (genes == null)
                throw new ArgumentNullException(nameof(genes));

            var geneSet = new GeneSet();

            foreach (GeneDef gene in genes)
            {
                if (gene == null)
                    throw new ArgumentNullException(nameof(genes));

                geneSet.AddGene(gene);
            }

            return geneSet;
        }

        internal static CustomXenogerm CreateCustomXenogerm(string name, params GeneSet[] geneSets)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (geneSets == null)
                throw new ArgumentNullException(nameof(geneSets));

            foreach (GeneSet geneSet in geneSets)
            {
                if (geneSet == null)
                    throw new ArgumentNullException(nameof(geneSets));
            }

            return new CustomXenogerm
            {
                name = name,
                genesets = [..geneSets]
            };
        }

        internal static Func<string, GeneDef> CreateResolver(params GeneDef[] genes)
        {
            if (genes == null)
                throw new ArgumentNullException(nameof(genes));

            var genesByDefName = new Dictionary<string, GeneDef>(StringComparer.Ordinal);

            foreach (GeneDef gene in genes)
            {
                if (gene == null)
                    throw new ArgumentNullException(nameof(genes));

                if (string.IsNullOrWhiteSpace(gene.defName))
                {
                    throw new ArgumentException("Gene def name cannot be null, empty or whitespace.", nameof(genes));
                }

                genesByDefName.Add(gene.defName, gene);
            }

            return geneDefName =>
            {
                genesByDefName.TryGetValue(geneDefName, out GeneDef gene);

                return gene;
            };
        }
    }
}