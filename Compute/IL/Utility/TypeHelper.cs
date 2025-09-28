using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Compute.IL.Utility
{
    public static class TypeHelper
    {
        private static readonly Dictionary<MethodDefinition, MethodBase> MethodCache = new();
        private static readonly Dictionary<MethodBase, MethodDefinition> MethodDefinitionCache = new();
        private static readonly Dictionary<TypeDefinition, Type> TypeCache = new();

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

            if (TypeCache.TryGetValue(typeDefinition, out var cachedType))
                return cachedType;

            var type = Find(typeDefinition.FullName);

            if (type == null) return null;

            TypeCache[typeDefinition] = type;

            return type;
        }

        public static MethodDefinition? FindMethodDefinition(MethodBase methodBase)
        {
            if (MethodDefinitionCache.TryGetValue(methodBase, out var cachedDefinition))
                return cachedDefinition;

            var methodDef = FindMethodDefinitionInternal(methodBase);

            if (methodDef != null)
            {
                MethodDefinitionCache[methodBase] = methodDef;
                MethodCache[methodDef] = methodBase;
                TypeCache[methodDef.DeclaringType] = methodBase.DeclaringType!;
            }

            return methodDef;
        }

        private static MethodDefinition? FindMethodDefinitionInternal(MethodBase methodBase)
        {
            if (methodBase.DeclaringType == null) return null;

            var type = methodBase.DeclaringType;

            if (type == null) return null;

            var assembly = AssemblyDefinition.ReadAssembly(type.Assembly.Location);
            var typeDef = assembly.MainModule.GetType(type.FullName);

            if (typeDef == null)
            {
                typeDef = assembly.MainModule.GetType(type.DeclaringType?.FullName ?? "");

                if (typeDef == null) return null;

                foreach (var method in typeDef.Methods)
                {
                    if (!method.HasBody)
                        continue;

                    foreach (var instr in method.Body.Instructions)
                    {
                        var methodRef = instr.Operand as MethodReference;

                        if (methodRef == null) continue;

                        // Check if it's constructing System.Action
                        if (methodRef.DeclaringType.Name == methodBase.DeclaringType?.Name)
                        {
                            var methods = methodRef.DeclaringType.Resolve().Methods.ToArray();

                            var selectedMethod = methods.FirstOrDefault(m => m.Name == methodBase.Name);

                            if (selectedMethod != null)
                                return selectedMethod;
                        }
                    }
                }

                return null;
            }

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
            if (MethodCache.TryGetValue(methodDefinition, out var cachedMethod))
                return cachedMethod;

            var method = FindMethodInternal(methodDefinition);

            if (method != null)
            {
                MethodCache[methodDefinition] = method;
                MethodDefinitionCache[method] = methodDefinition;
                TypeCache[methodDefinition.DeclaringType] = method.DeclaringType!;
            }

            return method;
        }

        private static MethodBase? FindMethodInternal(MethodDefinition methodDefinition)
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