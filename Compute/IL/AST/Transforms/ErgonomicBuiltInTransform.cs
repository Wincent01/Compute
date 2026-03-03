using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Compute.IL.Utility;
using Mono.Cecil;

namespace Compute.IL.AST.Transforms
{
    /// <summary>
    /// Lowers ergonomic helper calls (KernelThread/Sync) to canonical BuiltIn OpenCL aliases.
    /// </summary>
    public class ErgonomicBuiltInTransform : IAstTransform
    {
        private static readonly MethodReference BarrierMethod = ResolveMethod(typeof(BuiltIn), nameof(BuiltIn.Barrier), typeof(int));
        private static readonly MethodReference GetGlobalIdMethod = ResolveMethod(typeof(BuiltIn), nameof(BuiltIn.GetGlobalId), typeof(int));
        private static readonly MethodReference GetLocalIdMethod = ResolveMethod(typeof(BuiltIn), nameof(BuiltIn.GetLocalId), typeof(int));
        private static readonly MethodReference GetGroupIdMethod = ResolveMethod(typeof(BuiltIn), nameof(BuiltIn.GetGroupId), typeof(int));
        private static readonly MethodReference GetGlobalSizeMethod = ResolveMethod(typeof(BuiltIn), nameof(BuiltIn.GetGlobalSize), typeof(int));
        private static readonly MethodReference GetLocalSizeMethod = ResolveMethod(typeof(BuiltIn), nameof(BuiltIn.GetLocalSize), typeof(int));
        private static readonly MethodReference GetNumGroupsMethod = ResolveMethod(typeof(BuiltIn), nameof(BuiltIn.GetNumGroups), typeof(int));

        private static readonly IReadOnlyDictionary<string, (MethodReference method, int dimension)> ThreadMethodMap =
            new Dictionary<string, (MethodReference method, int dimension)>
            {
                [nameof(KernelThread.GlobalX)] = (GetGlobalIdMethod, 0),
                [nameof(KernelThread.GlobalY)] = (GetGlobalIdMethod, 1),
                [nameof(KernelThread.GlobalZ)] = (GetGlobalIdMethod, 2),
                [nameof(KernelThread.LocalX)] = (GetLocalIdMethod, 0),
                [nameof(KernelThread.LocalY)] = (GetLocalIdMethod, 1),
                [nameof(KernelThread.LocalZ)] = (GetLocalIdMethod, 2),
                [nameof(KernelThread.GroupX)] = (GetGroupIdMethod, 0),
                [nameof(KernelThread.GroupY)] = (GetGroupIdMethod, 1),
                [nameof(KernelThread.GroupZ)] = (GetGroupIdMethod, 2),
                [nameof(KernelThread.GlobalSizeX)] = (GetGlobalSizeMethod, 0),
                [nameof(KernelThread.GlobalSizeY)] = (GetGlobalSizeMethod, 1),
                [nameof(KernelThread.GlobalSizeZ)] = (GetGlobalSizeMethod, 2),
                [nameof(KernelThread.LocalSizeX)] = (GetLocalSizeMethod, 0),
                [nameof(KernelThread.LocalSizeY)] = (GetLocalSizeMethod, 1),
                [nameof(KernelThread.LocalSizeZ)] = (GetLocalSizeMethod, 2),
                [nameof(KernelThread.GroupCountX)] = (GetNumGroupsMethod, 0),
                [nameof(KernelThread.GroupCountY)] = (GetNumGroupsMethod, 1),
                [nameof(KernelThread.GroupCountZ)] = (GetNumGroupsMethod, 2),
            };

        private static readonly IReadOnlyDictionary<Type, MethodReference> PropertyDeclaringTypeToBuiltIn =
            new Dictionary<Type, MethodReference>
            {
                [typeof(KernelThread.Global)] = GetGlobalIdMethod,
                [typeof(KernelThread.Local)] = GetLocalIdMethod,
                [typeof(KernelThread.Group)] = GetGroupIdMethod,
                [typeof(KernelThread.GlobalSize)] = GetGlobalSizeMethod,
                [typeof(KernelThread.LocalSize)] = GetLocalSizeMethod,
                [typeof(KernelThread.GroupCount)] = GetNumGroupsMethod,
            };

        private static readonly IReadOnlyDictionary<string, int> AxisByPropertyName =
            new Dictionary<string, int>
            {
                ["get_X"] = 0,
                ["get_Y"] = 1,
                ["get_Z"] = 2,
            };

        public BlockStatement Transform(BlockStatement body, AstTransformContext context)
        {
            var rewriter = new ErgonomicBuiltInRewriter();
            return (BlockStatement)rewriter.Rewrite(body);
        }

        private static MethodReference ResolveMethod(Type declaringType, string methodName, params Type[] parameterTypes)
        {
            var method = declaringType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, parameterTypes);
            if (method == null)
            {
                throw new InvalidOperationException($"Failed to resolve method {declaringType.FullName}.{methodName}.");
            }

            var definition = TypeHelper.FindMethodDefinition(method);
            if (definition == null)
            {
                throw new InvalidOperationException($"Failed to resolve Cecil definition for method {declaringType.FullName}.{methodName}.");
            }

            return definition;
        }

        private class ErgonomicBuiltInRewriter : AstRewriter
        {
            protected override IExpression RewriteFunctionCall(FunctionCallExpression node)
            {
                var methodDefinition = node.Method.Resolve();
                var methodBase = methodDefinition == null ? null : TypeHelper.FindMethod(methodDefinition) as MethodInfo;

                if (methodBase?.DeclaringType == typeof(Sync))
                {
                    var fenceFlags = methodBase.Name switch
                    {
                        nameof(Sync.Local) => Sync.LocalFence,
                        nameof(Sync.Global) => Sync.GlobalFence,
                        nameof(Sync.All) => Sync.AllFences,
                        _ => -1
                    };

                    if (fenceFlags >= 0)
                    {
                        return new FunctionCallExpression(
                            BarrierMethod,
                            [LiteralExpression.Int32(fenceFlags)],
                            PrimitiveAstType.Void
                        );
                    }
                }

                if (methodBase?.DeclaringType == typeof(KernelThread) &&
                    ThreadMethodMap.TryGetValue(methodBase.Name, out var mapping))
                {
                    return new FunctionCallExpression(
                        mapping.method,
                        [LiteralExpression.Int32(mapping.dimension)],
                        PrimitiveAstType.Int32
                    );
                }

                if (methodBase != null &&
                    PropertyDeclaringTypeToBuiltIn.TryGetValue(methodBase.DeclaringType!, out var builtInMethod) &&
                    AxisByPropertyName.TryGetValue(methodBase.Name, out var axis))
                {
                    return new FunctionCallExpression(
                        builtInMethod,
                        [LiteralExpression.Int32(axis)],
                        PrimitiveAstType.Int32
                    );
                }

                return base.RewriteFunctionCall(node);
            }
        }
    }
}