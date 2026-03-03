namespace Compute.IL
{
    /// <summary>
    /// Ergonomic thread/group helpers for kernel code.
    /// These methods are compile-time markers and are lowered to OpenCL built-ins.
    /// </summary>
    public static class KernelThread
    {
        public static class Global
        {
            public static int X => BuiltIn.GetGlobalId(0);
            public static int Y => BuiltIn.GetGlobalId(1);
            public static int Z => BuiltIn.GetGlobalId(2);
        }

        public static class Local
        {
            public static int X => BuiltIn.GetLocalId(0);
            public static int Y => BuiltIn.GetLocalId(1);
            public static int Z => BuiltIn.GetLocalId(2);
        }

        public static class Group
        {
            public static int X => BuiltIn.GetGroupId(0);
            public static int Y => BuiltIn.GetGroupId(1);
            public static int Z => BuiltIn.GetGroupId(2);
        }

        public static class GlobalSize
        {
            public static int X => BuiltIn.GetGlobalSize(0);
            public static int Y => BuiltIn.GetGlobalSize(1);
            public static int Z => BuiltIn.GetGlobalSize(2);
        }

        public static class LocalSize
        {
            public static int X => BuiltIn.GetLocalSize(0);
            public static int Y => BuiltIn.GetLocalSize(1);
            public static int Z => BuiltIn.GetLocalSize(2);
        }

        public static class GroupCount
        {
            public static int X => BuiltIn.GetNumGroups(0);
            public static int Y => BuiltIn.GetNumGroups(1);
            public static int Z => BuiltIn.GetNumGroups(2);
        }

        public static int GlobalX() => BuiltIn.GetGlobalId(0);
        public static int GlobalY() => BuiltIn.GetGlobalId(1);
        public static int GlobalZ() => BuiltIn.GetGlobalId(2);

        public static int LocalX() => BuiltIn.GetLocalId(0);
        public static int LocalY() => BuiltIn.GetLocalId(1);
        public static int LocalZ() => BuiltIn.GetLocalId(2);

        public static int GroupX() => BuiltIn.GetGroupId(0);
        public static int GroupY() => BuiltIn.GetGroupId(1);
        public static int GroupZ() => BuiltIn.GetGroupId(2);

        public static int GlobalSizeX() => BuiltIn.GetGlobalSize(0);
        public static int GlobalSizeY() => BuiltIn.GetGlobalSize(1);
        public static int GlobalSizeZ() => BuiltIn.GetGlobalSize(2);

        public static int LocalSizeX() => BuiltIn.GetLocalSize(0);
        public static int LocalSizeY() => BuiltIn.GetLocalSize(1);
        public static int LocalSizeZ() => BuiltIn.GetLocalSize(2);

        public static int GroupCountX() => BuiltIn.GetNumGroups(0);
        public static int GroupCountY() => BuiltIn.GetNumGroups(1);
        public static int GroupCountZ() => BuiltIn.GetNumGroups(2);
    }

    /// <summary>
    /// Ergonomic synchronization helpers for kernel code.
    /// These methods are compile-time markers and are lowered to OpenCL barrier flags.
    /// </summary>
    public static class Sync
    {
        public const int LocalFence = 1;
        public const int GlobalFence = 2;
        public const int AllFences = LocalFence | GlobalFence;

        public static void Local() => BuiltIn.Barrier(LocalFence);

        public static void Global() => BuiltIn.Barrier(GlobalFence);

        public static void All() => BuiltIn.Barrier(AllFences);
    }
}