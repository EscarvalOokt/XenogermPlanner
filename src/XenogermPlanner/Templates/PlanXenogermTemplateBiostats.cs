namespace XenogermPlanner.Templates
{
    internal sealed class PlanXenogermTemplateBiostats
    {
        internal PlanXenogermTemplateBiostats(int complexity, int metabolism, int architeCapsules)
        {
            Complexity = complexity;
            Metabolism = metabolism;
            ArchiteCapsules = architeCapsules;
        }

        internal int Complexity { get; }
        internal int Metabolism { get; }
        internal int ArchiteCapsules { get; }
    }
}