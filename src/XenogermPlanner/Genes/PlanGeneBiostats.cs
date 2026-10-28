namespace XenogermPlanner.Genes
{
    internal sealed class PlanGeneBiostats
    {
        internal PlanGeneBiostats(int complexity, int metabolism, int architeCapsules)
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