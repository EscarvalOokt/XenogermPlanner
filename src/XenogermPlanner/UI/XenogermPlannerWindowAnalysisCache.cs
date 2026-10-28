using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Assemblers;
using XenogermPlanner.Donors;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.UI
{
    internal sealed class XenogermPlannerWindowAssemblerAnalysis
    {
        internal XenogermPlannerWindowAssemblerAnalysis(
            PlanAssemblerLiveState liveState,
            PlanAssemblerReadinessResult readinessResult)
        {
            LiveState = liveState ?? throw new ArgumentNullException(nameof(liveState));
            ReadinessResult = readinessResult ?? throw new ArgumentNullException(nameof(readinessResult));
        }

        internal PlanAssemblerLiveState LiveState { get; }
        internal PlanAssemblerReadinessResult ReadinessResult { get; }
    }

    internal sealed class XenogermPlannerWindowAnalysisCache
    {
        internal const int LiveStateRefreshIntervalTicks = 30;

        private readonly Func<XenogermPlan, PlanAssemblerLiveState, PlanAssemblerReadinessResult>
            _analyzeAssemblerReadiness;

        private readonly Func<IEnumerable<GeneDef>, PlanPotentialDonorScopeSnapshot, PlanPotentialDonorAnalysisResult>
            _analyzePotentialDonors;

        private readonly Func<XenogermPlan, PlanGenepackInventorySnapshot, PlanReadinessResult>
            _analyzeProductReadiness;

        private readonly Func<IEnumerable<GeneDef>, PlanGeneTargetAnalysisResult> _analyzeTarget;
        private readonly Func<Genepack, IReadOnlyList<GeneDef>> _getAssemblerGenepackGenes;

        private readonly Func<int> _getCurrentTick;

        private readonly Func<Map, IReadOnlyList<Building_GeneAssembler>> _getSelectableAssemblers;
        private readonly Func<Building_GeneAssembler, PlanAssemblerLiveState> _readAssemblerLiveState;
        private readonly Func<Map, PlanPotentialDonorScopeSnapshot> _scanPotentialDonorScope;
        private Building_GeneAssembler _assembler;
        private XenogermPlannerWindowAssemblerAnalysis _assemblerAnalysis;
        private object _assemblerDesiredGenesKey;
        private AssemblerAnalysisInputSnapshot _assemblerInputSnapshot;
        private Map _assemblerMap;

        private XenogermPlan _assemblerPlan;
        private PlanReadinessMode _assemblerReadinessMode;
        private int _assemblerRefreshTick;
        private object _assemblerUnresolvedGenesKey;
        private bool _hasAssemblerRefreshTick;
        private bool _hasPotentialDonorRefreshTick;
        private bool _hasSelectableAssemblerRefreshTick;
        private PlanPotentialDonorAnalysisResult _potentialDonorAnalysisResult;

        private Map _potentialDonorMap;
        private PlanReadinessResult _potentialDonorReadinessResult;
        private int _potentialDonorRefreshTick;
        private object _productDesiredGenesKey;
        private PlanGenepackInventorySnapshot _productInventorySnapshot;

        private XenogermPlan _productPlan;
        private PlanReadinessMode _productReadinessMode;
        private PlanReadinessResult _productReadinessResult;
        private object _productUnresolvedGenesKey;

        private Map _selectableAssemblerMap;
        private int _selectableAssemblerRefreshTick;
        private IReadOnlyList<Building_GeneAssembler> _selectableAssemblers;
        private PlanGeneTargetAnalysisResult _targetAnalysisResult;

        private object _targetDesiredGenesKey;

        internal XenogermPlannerWindowAnalysisCache() : this(
            () => Find.TickManager?.TicksGame ?? 0,
            PlanReadinessAnalyzer.Analyze,
            PlanGeneTargetAnalyzer.Analyze,
            PlanPotentialDonorScopeScanner.Scan,
            PlanPotentialDonorAnalyzer.Analyze,
            PlanAssemblerScopeScanner.GetSelectableAssemblers,
            PlanAssemblerLiveStateReader.Read,
            PlanAssemblerReadinessAnalyzer.Analyze)
        {
        }

        internal XenogermPlannerWindowAnalysisCache(
            Func<int> getCurrentTick,
            Func<XenogermPlan, PlanGenepackInventorySnapshot, PlanReadinessResult> analyzeProductReadiness,
            Func<IEnumerable<GeneDef>, PlanGeneTargetAnalysisResult> analyzeTarget,
            Func<Map, PlanPotentialDonorScopeSnapshot> scanPotentialDonorScope,
            Func<IEnumerable<GeneDef>, PlanPotentialDonorScopeSnapshot, PlanPotentialDonorAnalysisResult>
                analyzePotentialDonors,
            Func<Map, IReadOnlyList<Building_GeneAssembler>> getSelectableAssemblers,
            Func<Building_GeneAssembler, PlanAssemblerLiveState> readAssemblerLiveState,
            Func<XenogermPlan, PlanAssemblerLiveState, PlanAssemblerReadinessResult> analyzeAssemblerReadiness,
            Func<Genepack, IReadOnlyList<GeneDef>> getAssemblerGenepackGenes = null)
        {
            _getCurrentTick = getCurrentTick ?? throw new ArgumentNullException(nameof(getCurrentTick));
            _analyzeProductReadiness = analyzeProductReadiness ??
                                       throw new ArgumentNullException(nameof(analyzeProductReadiness));
            _analyzeTarget = analyzeTarget ?? throw new ArgumentNullException(nameof(analyzeTarget));
            _scanPotentialDonorScope = scanPotentialDonorScope ??
                                       throw new ArgumentNullException(nameof(scanPotentialDonorScope));
            _analyzePotentialDonors = analyzePotentialDonors ??
                                      throw new ArgumentNullException(nameof(analyzePotentialDonors));
            _getSelectableAssemblers = getSelectableAssemblers ??
                                       throw new ArgumentNullException(nameof(getSelectableAssemblers));
            _readAssemblerLiveState = readAssemblerLiveState ??
                                      throw new ArgumentNullException(nameof(readAssemblerLiveState));
            _analyzeAssemblerReadiness = analyzeAssemblerReadiness ??
                                         throw new ArgumentNullException(nameof(analyzeAssemblerReadiness));
            _getAssemblerGenepackGenes = getAssemblerGenepackGenes ??
                                         (genepack => genepack.GeneSet?.GenesListForReading);
        }

        internal PlanReadinessResult GetProductReadiness(
            XenogermPlan plan,
            PlanGenepackInventorySnapshot inventorySnapshot)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (inventorySnapshot == null)
                throw new ArgumentNullException(nameof(inventorySnapshot));

            if (_productReadinessResult != null &&
                ReferenceEquals(_productPlan, plan) &&
                ReferenceEquals(_productDesiredGenesKey, plan.DesiredGenes) &&
                ReferenceEquals(_productUnresolvedGenesKey, plan.UnresolvedDesiredGeneDefNames) &&
                _productReadinessMode == plan.ReadinessMode &&
                ReferenceEquals(_productInventorySnapshot, inventorySnapshot))
            {
                return _productReadinessResult;
            }

            PlanReadinessResult result = _analyzeProductReadiness(plan, inventorySnapshot) ??
                                         throw new InvalidOperationException(
                                             "Product readiness analyzer returned a null result.");

            _productPlan = plan;
            _productDesiredGenesKey = plan.DesiredGenes;
            _productUnresolvedGenesKey = plan.UnresolvedDesiredGeneDefNames;
            _productReadinessMode = plan.ReadinessMode;
            _productInventorySnapshot = inventorySnapshot;
            _productReadinessResult = result;

            return result;
        }

        internal PlanGeneTargetAnalysisResult GetTargetAnalysis(XenogermPlan plan)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (_targetAnalysisResult != null && ReferenceEquals(_targetDesiredGenesKey, plan.DesiredGenes))
                return _targetAnalysisResult;

            PlanGeneTargetAnalysisResult result = _analyzeTarget(plan.DesiredGenes) ??
                                                  throw new InvalidOperationException(
                                                      "Target analyzer returned a null result.");

            _targetDesiredGenesKey = plan.DesiredGenes;
            _targetAnalysisResult = result;

            return result;
        }

        internal PlanPotentialDonorAnalysisResult GetPotentialDonorAnalysis(
            PlanReadinessResult readinessResult,
            Map map)
        {
            if (readinessResult == null)
                throw new ArgumentNullException(nameof(readinessResult));

            if (readinessResult.MissingGenes.Count == 0)
            {
                InvalidatePotentialDonorAnalysis();
                return null;
            }

            int currentTick = _getCurrentTick();

            if (_potentialDonorAnalysisResult != null &&
                ReferenceEquals(_potentialDonorMap, map) &&
                ReferenceEquals(_potentialDonorReadinessResult, readinessResult) &&
                !ShouldRefresh(_hasPotentialDonorRefreshTick, _potentialDonorRefreshTick, currentTick))
            {
                return _potentialDonorAnalysisResult;
            }

            PlanPotentialDonorScopeSnapshot scope = _scanPotentialDonorScope(map) ??
                                                    throw new InvalidOperationException(
                                                        "Potential donor scope scanner returned a null snapshot.");

            PlanPotentialDonorAnalysisResult result = _analyzePotentialDonors(readinessResult.MissingGenes, scope) ??
                                                      throw new InvalidOperationException(
                                                          "Potential donor analyzer returned a null result.");

            _potentialDonorMap = map;
            _potentialDonorReadinessResult = readinessResult;
            _potentialDonorRefreshTick = currentTick;
            _hasPotentialDonorRefreshTick = true;
            _potentialDonorAnalysisResult = result;

            return result;
        }

        internal IReadOnlyList<Building_GeneAssembler> GetSelectableAssemblers(Map map)
        {
            int currentTick = _getCurrentTick();

            if (_selectableAssemblers != null &&
                ReferenceEquals(_selectableAssemblerMap, map) &&
                !ShouldRefresh(_hasSelectableAssemblerRefreshTick, _selectableAssemblerRefreshTick, currentTick))
            {
                return _selectableAssemblers;
            }

            IReadOnlyList<Building_GeneAssembler> assemblers = _getSelectableAssemblers(map) ??
                                                               throw new InvalidOperationException(
                                                                   "Assembler scope scanner returned a null list.");

            _selectableAssemblerMap = map;
            _selectableAssemblerRefreshTick = currentTick;
            _hasSelectableAssemblerRefreshTick = true;
            _selectableAssemblers = assemblers;

            return assemblers;
        }

        internal XenogermPlannerWindowAssemblerAnalysis GetAssemblerAnalysis(
            XenogermPlan plan,
            Building_GeneAssembler assembler,
            Map map)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (assembler == null)
                throw new ArgumentNullException(nameof(assembler));

            int currentTick = _getCurrentTick();

            bool sameRequest = _assemblerAnalysis != null &&
                               ReferenceEquals(_assemblerPlan, plan) &&
                               ReferenceEquals(_assemblerDesiredGenesKey, plan.DesiredGenes) &&
                               ReferenceEquals(_assemblerUnresolvedGenesKey, plan.UnresolvedDesiredGeneDefNames) &&
                               _assemblerReadinessMode == plan.ReadinessMode &&
                               ReferenceEquals(_assembler, assembler) &&
                               ReferenceEquals(_assemblerMap, map);

            if (sameRequest && !ShouldRefresh(_hasAssemblerRefreshTick, _assemblerRefreshTick, currentTick))
            {
                return _assemblerAnalysis;
            }

            PlanAssemblerLiveState liveState = _readAssemblerLiveState(assembler) ??
                                               throw new InvalidOperationException(
                                                   "Assembler live-state reader returned a null result.");

            if (sameRequest && _assemblerInputSnapshot.Matches(liveState, _getAssemblerGenepackGenes))
            {
                _assemblerRefreshTick = currentTick;
                return _assemblerAnalysis;
            }

            var inputSnapshot = new AssemblerAnalysisInputSnapshot(liveState, _getAssemblerGenepackGenes);

            PlanAssemblerReadinessResult readinessResult = _analyzeAssemblerReadiness(plan, liveState) ??
                                                           throw new InvalidOperationException(
                                                               "Assembler readiness analyzer returned a null result.");

            var analysis = new XenogermPlannerWindowAssemblerAnalysis(liveState, readinessResult);

            _assemblerPlan = plan;
            _assemblerDesiredGenesKey = plan.DesiredGenes;
            _assemblerUnresolvedGenesKey = plan.UnresolvedDesiredGeneDefNames;
            _assemblerReadinessMode = plan.ReadinessMode;
            _assembler = assembler;
            _assemblerMap = map;
            _assemblerInputSnapshot = inputSnapshot;
            _assemblerRefreshTick = currentTick;
            _hasAssemblerRefreshTick = true;
            _assemblerAnalysis = analysis;

            return analysis;
        }

        internal void Invalidate()
        {
            _productPlan = null;
            _productDesiredGenesKey = null;
            _productUnresolvedGenesKey = null;
            _productInventorySnapshot = null;
            _productReadinessResult = null;

            _targetDesiredGenesKey = null;
            _targetAnalysisResult = null;

            InvalidatePotentialDonorAnalysis();

            _selectableAssemblerMap = null;
            _hasSelectableAssemblerRefreshTick = false;
            _selectableAssemblers = null;

            InvalidateAssemblerAnalysis();
        }

        internal void InvalidateAssemblerAnalysis()
        {
            _assemblerPlan = null;
            _assemblerDesiredGenesKey = null;
            _assemblerUnresolvedGenesKey = null;
            _assembler = null;
            _assemblerMap = null;
            _assemblerInputSnapshot = null;
            _hasAssemblerRefreshTick = false;
            _assemblerAnalysis = null;
        }

        private void InvalidatePotentialDonorAnalysis()
        {
            _potentialDonorMap = null;
            _potentialDonorReadinessResult = null;
            _hasPotentialDonorRefreshTick = false;
            _potentialDonorAnalysisResult = null;
        }

        private static bool ShouldRefresh(bool hasRefreshTick, int refreshTick, int currentTick)
        {
            return !hasRefreshTick ||
                   currentTick < refreshTick ||
                   currentTick - refreshTick >= LiveStateRefreshIntervalTicks;
        }
    }
}