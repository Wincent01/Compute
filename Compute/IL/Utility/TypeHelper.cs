using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Compute.IL.Utility
{
    public static class TypeHelper
    {
        public static Type? Find(string name)
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

        public static Type? Find(TypeDefinition typeDefinition)
        {
            if (typeDefinition == null) return null;

            return Find(typeDefinition.FullName);
        }

        public static MethodDefinition? FindMethodDefinition(MethodBase methodBase)
        {
            if (methodBase.DeclaringType == null) return null;

            var type = methodBase.DeclaringType;

            if (type == null) return null;

            var assembly = AssemblyDefinition.ReadAssembly(type.Assembly.Location);
            var typeDef = assembly.MainModule.GetType(type.FullName);

            if (typeDef == null) return null;

            foreach (var method in typeDef.Methods)
            {
                if (methodBase.Name == method.Name && methodBase.GetParameters().Length == method.Parameters.Count)
                {
                    return method;
                }
            }

            return null;
        }

        public static MethodBase? FindMethod(MethodDefinition methodDefinition)
        {
            if (methodDefinition.DeclaringType == null) return null;

            var type = Find(methodDefinition.DeclaringType.FullName);

            if (type == null) return null;


            if (!methodDefinition.IsConstructor)
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (method.Name == methodDefinition.Name && method.GetParameters().Length == methodDefinition.Parameters.Count)
                    {
                        return method;
                    }
                }
            }
            else
            {
                // It's a constructor
                foreach (var method in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (method.GetParameters().Length == methodDefinition.Parameters.Count)
                    {
                        return method;
                    }
                }
            }

            return null;
        }
    }
}