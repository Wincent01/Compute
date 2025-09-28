using System;
using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Compute.IL.Utility;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST instruction for loading arguments (ldarg)
    /// </summary>
    [Instruction(Code.Ldarg_0, Code.Ldarg_1, Code.Ldarg_2, Code.Ldarg_3, Code.Ldarg, Code.Ldarg_S)]
    public class AstLdargInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var argIndex = GetArgumentIndex();
            var argName = GetArgument(argIndex);
            var argType = GetArgumentType(argIndex);
            
            var identifierExpr = new IdentifierExpression(argName, argType);
            ExpressionStack.Push(identifierExpr);
            
            return new NopStatement();
        }
        
        private int GetArgumentIndex()
        {
            return Instruction.OpCode.Code switch
            {
                Code.Ldarg_0 => 0,
                Code.Ldarg_1 => 1,
                Code.Ldarg_2 => 2,
                Code.Ldarg_3 => 3,
                Code.Ldarg => (int)Instruction.Operand,
                Code.Ldarg_S => ((Mono.Cecil.ParameterDefinition)Instruction.Operand).Index,
                _ => 0
            };
        }

        private AstType GetArgumentType(int index)
        {
            if (index < Definition.Parameters.Count)
            {
                var param = Definition.Parameters[index];

                var clrType = TypeHelper.Find(param.ParameterType.Resolve());

                if (clrType == null)
                    throw new InvalidOperationException($"Unable to resolve type for parameter {param.Name} in method {Definition.FullName}");

                var type = AstType.FromClrType(clrType);

                if (type.IsStruct)
                {
                    return new PointerAstType(type);
                }

                return type;
            }
            
            throw new InvalidOperationException($"Invalid argument index {index} for method {Definition.FullName}");
        }
    }
}