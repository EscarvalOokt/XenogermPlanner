using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace XenogermPlanner.Api
{
    public sealed class GenepackRelevanceRequest
    {
        private readonly ReadOnlyCollection<string> _geneDefNames;

        public GenepackRelevanceRequest(IEnumerable<string> geneDefNames)
        {
            if (geneDefNames == null)
                throw new ArgumentNullException(nameof(geneDefNames));

            _geneDefNames = new List<string>(geneDefNames).AsReadOnly();
        }

        public IReadOnlyList<string> GeneDefNames => _geneDefNames;
    }
}