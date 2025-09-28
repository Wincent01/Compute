using System;
using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Compute.IL.Utility;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version of CltInstruction for "compare less than" operations
    /// Handles both signed (Clt) and unsigned (Clt_Un) comparisons
    /// </summary>
    [Instruction(Code.Newobj)]
    public class AstNewobjInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var methodRef = (Mono.Cecil.MethodReference)Instruction.Operand;

            // Collect arguments from stack
            var argumentCount = methodRef.Parameters.Count;
            var arguments = new IExpression[argumentCount];

            for (int i = argumentCount - 1; i >= 0; i--)
            {
                arguments[i] = ExpressionStack.Pop();
            }

            var returnType = TypeHelper.Find(methodRef.DeclaringType.FullName);

            if (returnType == null)
                throw new InvalidOperationException($"Unable to resolve type for method {methodRef.FullName}");

            var returnAstType = AstType.FromClrType(returnType);
            
            var callExpr = new FunctionCallExpression(methodRef, arguments, returnAstType);

            ExpressionStack.Push(callExpr);

            if (returnType != null) {
                TypeDependencies.Add(returnType);
            }

            MethodDependencies.Add(methodRef.Resolve());

            return new NopStatement(); // Duplication doesn't produce a statement, just pushes result onto stack
        }
    }
}