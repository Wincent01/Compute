using Silk.NET.OpenCL;

namespace Compute
{
    internal static class Bindings
    {
        public static CL OpenCl { get; } = CL.GetApi();
    }
}