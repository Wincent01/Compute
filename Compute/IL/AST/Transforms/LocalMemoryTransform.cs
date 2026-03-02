using System.Collections.Generic;
using System.Linq;
using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;

namespace Compute.IL.AST.Transforms
{
    /// <summary>
    /// Recognizes <c>LocalMemory.Allocate&lt;T&gt;(size)</c> calls in the AST and transforms
    /// them into <c>__local T name[size]</c> variable declarations.
    /// 
    /// This transform looks for the pattern:
    ///   AssignmentStatement(IdentifierExpression("localN"), FunctionCallExpression("Allocate", [sizeExpr]))
    /// where the called method is on the LocalMemory class, and:
    ///   1. Modifies the corresponding VariableDeclarationStatement to have AddressSpace.Local and the array size
    ///   2. Removes the assignment statement (the allocation is now part of the declaration)
    /// </summary>
    public class LocalMemoryTransform : IAstTransform
    {
        public BlockStatement Transform(BlockStatement body, AstTransformContext context)
        {
            // Phase 1: Find all LocalMemory.Allocate assignments and collect the mappings
            var localAllocations = new Dictionary<string, LocalAllocationInfo>();

            foreach (var statement in body.Statements)
            {
                if (statement is not AssignmentStatement assignment)
                    continue;

                if (assignment.Target is not IdentifierExpression target)
                    continue;

                if (assignment.Value is not FunctionCallExpression call)
                    continue;

                if (!IsLocalMemoryAllocate(call))
                    continue;

                // Extract the size from the first argument
                var sizeArg = call.Arguments.FirstOrDefault();
                int? size = sizeArg switch
                {
                    LiteralExpression { Value: int intVal } => intVal,
                    LiteralExpression { Value: uint uintVal } => (int)uintVal,
                    _ => null
                };

                if (size == null)
                    continue; // Can't determine size — skip (will fail at OpenCL compile time)

                // Determine the element type from the generic method return type
                // The call returns T[] (ArrayAstType), so the element type is inside it
                var elementType = call.Type switch
                {
                    ArrayAstType arrayType => arrayType.ElementType,
                    PointerAstType pointerType => pointerType.ElementType,
                    _ => null
                };

                if (elementType == null)
                    continue;

                localAllocations[target.Name] = new LocalAllocationInfo
                {
                    VariableName = target.Name,
                    ElementType = elementType,
                    Size = size.Value
                };
            }

            if (localAllocations.Count == 0)
                return body; // Nothing to transform

            // Phase 2: Build a new statement list
            var newStatements = new List<IStatement>(body.Statements.Count);

            foreach (var statement in body.Statements)
            {
                // Skip the assignment statements that we're inlining into declarations
                if (statement is AssignmentStatement assignment &&
                    assignment.Target is IdentifierExpression assignTarget &&
                    assignment.Value is FunctionCallExpression assignCall &&
                    IsLocalMemoryAllocate(assignCall) &&
                    localAllocations.ContainsKey(assignTarget.Name))
                {
                    continue; // Remove this assignment — it's now part of the declaration
                }

                // Modify variable declarations for local memory
                if (statement is VariableDeclarationStatement varDecl &&
                    localAllocations.TryGetValue(varDecl.Name, out var info))
                {
                    // Replace with a __local array declaration
                    var newDecl = new VariableDeclarationStatement(info.ElementType, varDecl.Name, null)
                    {
                        AddressSpace = AddressSpace.Local,
                        ArraySize = info.Size
                    };
                    newStatements.Add(newDecl);
                    continue;
                }

                newStatements.Add(statement);
            }

            return new BlockStatement(newStatements);
        }

        private static bool IsLocalMemoryAllocate(FunctionCallExpression call)
        {
            var method = call.Method;
            return method.DeclaringType?.Name == "LocalMemory" && method.Name == "Allocate";
        }

        private class LocalAllocationInfo
        {
            public required string VariableName { get; init; }
            public required AstType ElementType { get; init; }
            public required int Size { get; init; }
        }
    }
}
