using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace Compute.IL.Compiler
{
    public static class MethodDefinitionExtensions
    {
        public static MethodInfo FindInfo(this MethodDefinition @this)
        {
            var type = TypeHelper.Find(@this.DeclaringType.FullName);

            MethodInfo info = default;
            
            foreach (var method in type.GetMethods().Where(m => m.Name == @this.Name))
            {
                var param = method.GetParameters();

                var found = true;
                
                for (var index = 0; index < param.Length; index++)
                {
                    var parameterInfo = param[index];

                    if (parameterInfo.ParameterType != @this.Parameters[index].ParameterType.FindType())
                    {
                        found = false;
                    }
                }
                
                if (!found) continue;

                info = method;
                
                break;
            }

            if (info == default)
            {
                throw new Exception($"Failed to find method {@this.Name}");
            }

            return info;
        }
    }
}