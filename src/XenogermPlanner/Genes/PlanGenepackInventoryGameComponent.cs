using System;
using Verse;

namespace XenogermPlanner.Genes
{
    public sealed class PlanGenepackInventoryGameComponent : GameComponent
    {
        internal const int FallbackRefreshIntervalTicks = 600;

        private readonly Func<Map> _getCurrentMap;
        private readonly Func<int> _getCurrentTick;
        private readonly Func<Map, PlanGenepackInventorySnapshot> _scanMap;
        private bool _invalidated;
        private int _nextFallbackRefreshTick;

        private PlanGenepackInventorySnapshot _snapshot;
        private Map _snapshotMap;

        // ReSharper disable once UnusedParameter.Local
        public PlanGenepackInventoryGameComponent(Game _) : this(
            () => Find.CurrentMap,
            GetCurrentGameTick,
            PlanGenepackInventoryScanner.Scan)
        {
        }

        internal PlanGenepackInventoryGameComponent(
            Func<Map> getCurrentMap,
            Func<int> getCurrentTick,
            Func<Map, PlanGenepackInventorySnapshot> scanMap)
        {
            _getCurrentMap = getCurrentMap ?? throw new ArgumentNullException(nameof(getCurrentMap));
            _getCurrentTick = getCurrentTick ?? throw new ArgumentNullException(nameof(getCurrentTick));
            _scanMap = scanMap ?? throw new ArgumentNullException(nameof(scanMap));

            _snapshot = PlanGenepackInventorySnapshot.Unavailable;
            _snapshotMap = null;
            _invalidated = true;
            _nextFallbackRefreshTick = 0;
        }

        public PlanGenepackInventorySnapshot Snapshot
        {
            get
            {
                EnsureFreshSnapshot();
                return _snapshot;
            }
        }

        public void Invalidate()
        {
            _invalidated = true;
        }

        public override void GameComponentTick()
        {
            EnsureActiveMapState();

            if (_invalidated || IsFallbackRefreshDue())
                RefreshSnapshot();
        }

        private static int GetCurrentGameTick()
        {
            return Find.TickManager?.TicksGame ?? 0;
        }

        private void EnsureFreshSnapshot()
        {
            EnsureActiveMapState();

            if (_invalidated || IsFallbackRefreshDue())
                RefreshSnapshot();
        }

        private void EnsureActiveMapState()
        {
            Map currentMap = _getCurrentMap();

            if (!ReferenceEquals(_snapshotMap, currentMap))
            {
                _invalidated = true;
            }
        }

        private bool IsFallbackRefreshDue()
        {
            return _getCurrentTick() >= _nextFallbackRefreshTick;
        }

        private void RefreshSnapshot()
        {
            Map currentMap = _getCurrentMap();
            PlanGenepackInventorySnapshot refreshedSnapshot = _scanMap(currentMap) ??
                                                              throw new InvalidOperationException(
                                                                  "Inventory scanner returned a null snapshot.");

            _snapshot = refreshedSnapshot;
            _snapshotMap = currentMap;
            _invalidated = false;
            _nextFallbackRefreshTick = _getCurrentTick() + FallbackRefreshIntervalTicks;
        }
    }
}