using System;
using System.Collections.Generic;
using System.Linq;
using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Compute.IL.Utility;
using Mono.Cecil;

namespace Compute.IL.AST.Transforms
{
    /// <summary>
    /// Recognizes LocalMemory allocation markers and rewrites them into __local array declarations.
    /// Also lowers LocalArray2D/LocalArray3D indexer calls into flat array accesses.
    /// </summary>
    public class LocalMemoryTransform : IAstTransform
    {
        public BlockStatement Transform(BlockStatement body, AstTransformContext context)
        {
            var localAllocations = CollectLocalAllocations(body);

            if (localAllocations.Count == 0)
                return body;

            var newStatements = new List<IStatement>(body.Statements.Count);
            var rewriter = new LocalIndexerRewriter(localAllocations);

            foreach (var statement in body.Statements)
            {
                if (IsAllocationAssignment(statement, out var assignedName) && localAllocations.ContainsKey(assignedName))
                {
                    continue;
                }

                if (statement is VariableDeclarationStatement varDecl &&
                    localAllocations.TryGetValue(varDecl.Name, out var info))
                {
                    newStatements.Add(new VariableDeclarationStatement(info.ElementType, varDecl.Name, null)
                    {
                        AddressSpace = AddressSpace.Local,
                        ArraySize = info.Size
                    });
                    continue;
                }

                newStatements.Add(rewriter.Rewrite(statement));
            }

            return new BlockStatement(newStatements);
        }

        private static Dictionary<string, LocalAllocationInfo> CollectLocalAllocations(BlockStatement body)
        {
            var localAllocations = new Dictionary<string, LocalAllocationInfo>();

            foreach (var statement in body.Statements)
            {
                if (!IsAllocationAssignment(statement, out var targetName, out var call))
                    continue;

                var signature = ExtractAllocationSignature(call);
                if (signature == null)
                    continue;

                var elementType = ResolveElementType(call);
                if (elementType == null)
                    continue;

                localAllocations[targetName] = new LocalAllocationInfo
                {
                    VariableName = targetName,
                    ElementType = elementType,
                    Size = signature.Size,
                    DimensionLiterals = signature.DimensionLiterals
                };
            }

            return localAllocations;
        }

        private static bool IsAllocationAssignment(IStatement statement, out string targetName)
        {
            targetName = string.Empty;
            return IsAllocationAssignment(statement, out targetName, out _);
        }

        private static bool IsAllocationAssignment(IStatement statement, out string targetName, out FunctionCallExpression call)
        {
            targetName = string.Empty;
            call = null!;

            if (statement is not AssignmentStatement assignment)
                return false;

            if (assignment.Target is not IdentifierExpression target)
                return false;

            if (assignment.Value is not FunctionCallExpression fn)
                return false;

            if (!IsLocalMemoryAllocate(fn))
                return false;

            targetName = target.Name;
            call = fn;
            return true;
        }

        private static bool IsLocalMemoryAllocate(FunctionCallExpression call)
        {
            if (!(call.Method.Name is "Allocate" or "Allocate2D" or "Allocate3D"))
                return false;

            var declaringName = call.Method.DeclaringType?.Name;
            if (declaringName == "LocalMemory")
                return true;

            return call.Method.FullName.Contains("LocalMemory::", StringComparison.Ordinal);
        }

        private static LocalAllocationSignature? ExtractAllocationSignature(FunctionCallExpression call)
        {
            var dimensions = call.Method.Name switch
            {
                "Allocate2D" => 2,
                "Allocate3D" => 3,
                _ => call.Arguments.Count
            };

            if (dimensions <= 0 || call.Arguments.Count < dimensions)
                return null;

            var literals = new int[dimensions];
            for (var i = 0; i < dimensions; i++)
            {
                var literal = TryReadIntLiteral(call.Arguments[i]);
                if (!literal.HasValue)
                    return null;
                literals[i] = literal.Value;
            }

            var total = 1;
            for (var i = 0; i < literals.Length; i++)
                total *= literals[i];

            return new LocalAllocationSignature(total, literals);
        }

        private static AstType? ResolveElementType(FunctionCallExpression call)
        {
            if (call.Method is GenericInstanceMethod genericMethod && genericMethod.GenericArguments.Count > 0)
            {
                var genericArgType = TypeHelper.Find(genericMethod.GenericArguments[0].FullName) ??
                                     TypeHelper.Find(genericMethod.GenericArguments[0].FullName.Replace('/', '+'));

                if (genericArgType != null)
                    return AstType.FromClrType(genericArgType);
            }

            if (call.Type is ArrayAstType arrayType)
                return arrayType.ElementType;

            if (call.Type is PointerAstType pointerType)
                return pointerType.ElementType;

            if (call.Type is StructAstType structType && structType.ClrType != null && structType.ClrType.IsGenericType)
            {
                var genericDef = structType.ClrType.GetGenericTypeDefinition();
                if (genericDef == typeof(LocalArray2D<>) || genericDef == typeof(LocalArray3D<>))
                {
                    var elementClr = structType.ClrType.GetGenericArguments()[0];
                    return AstType.FromClrType(elementClr);
                }
            }

            return null;
        }

        private static int? TryReadIntLiteral(IExpression expression)
        {
            return expression switch
            {
                LiteralExpression { Value: int intVal } => intVal,
                LiteralExpression { Value: uint uintVal } => (int)uintVal,
                _ => null
            };
        }

        private static bool TryGetTargetIdentifier(IExpression expression, out IdentifierExpression identifier)
        {
            if (expression is IdentifierExpression direct)
            {
                identifier = direct;
                return true;
            }

            if (expression is AddressOfExpression { Expression: IdentifierExpression id })
            {
                identifier = id;
                return true;
            }

            identifier = null!;
            return false;
        }

        private static IExpression BuildLinearIndex(IReadOnlyList<IExpression> indices, IReadOnlyList<int> dims)
        {
            if (indices.Count == 1)
                return indices[0];

            if (indices.Count == 2)
            {
                var width = LiteralExpression.Int32(dims[1]);
                return Add(Mul(indices[0], width), indices[1]);
            }

            var height = LiteralExpression.Int32(dims[1]);
            var width3 = LiteralExpression.Int32(dims[2]);
            var zMulHeight = Mul(indices[0], height);
            var zPlusY = Add(zMulHeight, indices[1]);
            var zyxBase = Mul(zPlusY, width3);
            return Add(zyxBase, indices[2]);
        }

        private static BinaryExpression Mul(IExpression left, IExpression right)
            => new(left, BinaryOperatorType.Multiply, right, PrimitiveAstType.Int32);

        private static BinaryExpression Add(IExpression left, IExpression right)
            => new(left, BinaryOperatorType.Add, right, PrimitiveAstType.Int32);

        private sealed class LocalIndexerRewriter : AstRewriter
        {
            private readonly IReadOnlyDictionary<string, LocalAllocationInfo> _localAllocations;

            public LocalIndexerRewriter(IReadOnlyDictionary<string, LocalAllocationInfo> localAllocations)
            {
                _localAllocations = localAllocations;
            }

            protected override IStatement RewriteExpressionStatement(ExpressionStatement node)
            {
                if (node.Expression is FunctionCallExpression call &&
                    IsIndexerSetter(call, out var info, out var target, out var indices, out var value))
                {
                    var nonNullInfo = info!;
                    var nonNullTarget = target!;
                    var nonNullValue = value!;
                    var rewrittenIndices = indices.Select(Rewrite).ToArray();
                    var linearIndex = BuildLinearIndex(rewrittenIndices, nonNullInfo.DimensionLiterals);
                    var targetAccess = new ArrayAccessExpression(nonNullTarget, linearIndex, nonNullInfo.ElementType);
                    return new AssignmentStatement(targetAccess, Rewrite(nonNullValue));
                }

                return base.RewriteExpressionStatement(node);
            }

            protected override IExpression RewriteFunctionCall(FunctionCallExpression node)
            {
                if (IsIndexerGetter(node, out var info, out var target, out var indices))
                {
                    var nonNullInfo = info!;
                    var nonNullTarget = target!;
                    var rewrittenIndices = indices.Select(Rewrite).ToArray();
                    var linearIndex = BuildLinearIndex(rewrittenIndices, nonNullInfo.DimensionLiterals);
                    return new ArrayAccessExpression(nonNullTarget, linearIndex, nonNullInfo.ElementType);
                }

                return base.RewriteFunctionCall(node);
            }

            private bool IsIndexerGetter(FunctionCallExpression call, out LocalAllocationInfo? info, out IdentifierExpression? target, out IReadOnlyList<IExpression> indices)
            {
                info = null!;
                target = null!;
                indices = [];

                if (call.Method.Name != "get_Item" || call.Arguments.Count < 2)
                    return false;

                if (!TryGetTargetIdentifier(call.Arguments[0], out target))
                    return false;

                if (!_localAllocations.TryGetValue(target.Name, out var resolvedInfo))
                    return false;

                info = resolvedInfo;

                if (call.Arguments.Count != info.DimensionLiterals.Count + 1)
                    return false;

                indices = call.Arguments.Skip(1).ToArray();
                return true;
            }

            private bool IsIndexerSetter(FunctionCallExpression call, out LocalAllocationInfo? info, out IdentifierExpression? target, out IReadOnlyList<IExpression> indices, out IExpression? value)
            {
                info = null!;
                target = null!;
                indices = [];
                value = null!;

                if (call.Method.Name != "set_Item" || call.Arguments.Count < 3)
                    return false;

                if (!TryGetTargetIdentifier(call.Arguments[0], out target))
                    return false;

                if (!_localAllocations.TryGetValue(target.Name, out var resolvedInfo))
                    return false;

                info = resolvedInfo;

                if (call.Arguments.Count != info.DimensionLiterals.Count + 2)
                    return false;

                indices = call.Arguments.Skip(1).Take(info.DimensionLiterals.Count).ToArray();
                value = call.Arguments[^1];
                return true;
            }
        }

        private sealed class LocalAllocationInfo
        {
            public required string VariableName { get; init; }
            public required AstType ElementType { get; init; }
            public required int Size { get; init; }
            public required IReadOnlyList<int> DimensionLiterals { get; init; }
        }

        private sealed class LocalAllocationSignature
        {
            public int Size { get; }
            public IReadOnlyList<int> DimensionLiterals { get; }

            public LocalAllocationSignature(int size, IReadOnlyList<int> dimensionLiterals)
            {
                Size = size;
                DimensionLiterals = dimensionLiterals;
            }
        }
    }
}
