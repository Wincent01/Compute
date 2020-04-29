using System;

namespace Compute.IL
{
    public static class CLFunctions
    {
        [Alias("get_global_id")]
        public static int GetGlobalId(int dimension)
        {
            return default;
        }

        [Alias("sqrt")]
        public static float Sqrt(float value)
        {
            return MathF.Sqrt(value);
        }
    }
}