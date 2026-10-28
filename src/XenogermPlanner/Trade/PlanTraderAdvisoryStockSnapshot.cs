using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RimWorld;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Trade
{
    internal sealed class PlanTraderAdvisoryStockSnapshot
    {
        private readonly ReadOnlyCollection<PlanTraderAdvisorySourceSnapshot> _sources;

        private PlanTraderAdvisoryStockSnapshot(bool isAvailable, IEnumerable<PlanTraderAdvisorySourceSnapshot> sources)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            var copiedSources = new List<PlanTraderAdvisorySourceSnapshot>();
            var distinctSources = new HashSet<ITrader>(ReferenceEqualityComparer<ITrader>.Instance);

            foreach (PlanTraderAdvisorySourceSnapshot source in sources)
            {
                if (source == null)
                {
                    throw new ArgumentException(
                        "Trader advisory source collection cannot contain null values.",
                        nameof(sources));
                }

                if (distinctSources.Add(source.Source))
                    copiedSources.Add(source);
            }

            if (!isAvailable && copiedSources.Count > 0)
            {
                throw new ArgumentException(
                    "Unavailable trader advisory stock snapshot cannot contain sources.",
                    nameof(sources));
            }

            IsAvailable = isAvailable;
            _sources = copiedSources.AsReadOnly();
        }

        internal bool IsAvailable { get; }
        internal IReadOnlyList<PlanTraderAdvisorySourceSnapshot> Sources => _sources;

        internal static PlanTraderAdvisoryStockSnapshot Unavailable { get; } = new PlanTraderAdvisoryStockSnapshot(
            false,
            Array.Empty<PlanTraderAdvisorySourceSnapshot>());

        internal static PlanTraderAdvisoryStockSnapshot CreateAvailable(
            IEnumerable<PlanTraderAdvisorySourceSnapshot> sources)
        {
            return new PlanTraderAdvisoryStockSnapshot(true, sources);
        }
    }
}