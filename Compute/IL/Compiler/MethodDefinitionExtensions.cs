using System;
using System.Linq;
using System.Reflection;
using Compute.IL.Utility;
using Mono.Cecil;

namespace Compute.IL.Compiler
{
    public static class MethodDefinitionExtensions
    {
        public static MethodBase FindInfo(this MethodDefinition @this)
        {
            var type = TypeHelper.Find(@this.DeclaringType.FullName);

            MethodBase info = default;

            MethodBase[] methods = @this.Name == ".ctor" ? type.GetConstructors() : type.GetMethods();

            foreach (var method in methods.Where(m => m.Name == @this.Name))
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