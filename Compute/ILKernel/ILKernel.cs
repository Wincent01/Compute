using System.Collections.Generic;
using System.Reflection;

namespace Compute.ILKernel
{
    public class ILKernel
    {
        public static List<Assembly> Assemblies { get; } = new List<Assembly>();
        
        public string Source { get; }
        
        private ILKernel(string source)
        {
            Source = source;
        }

        public static ILKernel Compile(MethodInfo method)
        {
            var source = IlKernelCompiler.CompileKernel(method);

            return new ILKernel(source);
        }

        [Alias("get_global_id")]
        public static int GetGlobalId(int dimension)
        {
            return default;
        }
    }
}