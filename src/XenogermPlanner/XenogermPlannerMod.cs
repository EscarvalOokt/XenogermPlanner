using RimWorld;
using Verse;

namespace XenogermPlanner
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class XenogermPlannerMod : Mod
    {
        internal const string LogPrefix = "[XenogermPlanner]";

        public XenogermPlannerMod(ModContentPack content) : base(content)
        {
            if (!ModsConfig.BiotechActive)
            {
                Log.Error($"{LogPrefix} Biotech is required. Xenogerm Planner was not initialized.");
                return;
            }

            Log.Message($"{LogPrefix} Loaded for RimWorld {VersionControl.CurrentVersionStringWithRev}.");
        }
    }
}