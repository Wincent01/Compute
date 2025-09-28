using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Compute.IL.Utility;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST instruction for method calls
    /// </summary>
    [Instruction(Code.Call, Code.Callvirt)]
    public class AstCallInstruction : AstInstructionBase
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

            var returnType = TypeHelper.Find(methodRef.ReturnType.FullName);

            // Determine return type
            var returnAstType = methodRef.ReturnType.FullName == "System.Void"
                ? PrimitiveAstType.Void
                : AstType.FromClrType(returnType ?? typeof(int));

            var callExpr = new FunctionCallExpression(methodRef, arguments, returnAstType);

            ExpressionStack.Push(callExpr);

            if (returnType != null) {
                TypeDependencies.Add(returnType);
            }

            MethodDependencies.Add(methodRef.Resolve());

            return new NopStatement();
        }
    }
}