using System;

namespace Compute.IL.Compiler
{
    public static class TypeHelper
    {
        public static Type Find(string name)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                if (assembly == default) continue;

                var type = assembly.GetType(name);
                
                if (type == default) continue;

                return type;
            }

            return default;
        }
    }
}