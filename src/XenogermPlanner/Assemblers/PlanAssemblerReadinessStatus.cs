namespace XenogermPlanner.Assemblers
{
    public enum PlanAssemblerReadinessStatus
    {
        Ready,
        NotReady,
        Blocked,
        EmptyTarget,
        Degraded,
        SearchIncomplete = 5
    }
}