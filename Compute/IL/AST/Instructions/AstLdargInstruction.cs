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

            /*if (argIndex == 0 && Definition.HasThis)
            {
                // This is the 'this' argument for instance methods
                var method = TypeHelper.FindMethod(Definition);

                if (method == null)
                    throw new InvalidOperationException($"Unable to resolve method for {Definition.FullName}");

                var thisType = method.DeclaringType;

                if (thisType == null)
                    throw new InvalidOperationException($"Unable to resolve declaring type for method {Definition.FullName}");

                var astType = AstType.FromClrType(thisType);

                var argument = new IdentifierExpression("this", IdentifierType.Parameter, 0, new PointerAstType(astType));

                ExpressionStack.Push(argument);

                return new NopStatement();
            }
            else*/
            {
                var argument = Context.Arguments[argIndex];

                ExpressionStack.Push(argument);

                return new NopStatement();
            }
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
            var method = TypeHelper.FindMethod(Definition);

            if (method == null)
                throw new InvalidOperationException($"Unable to resolve method for {Definition.FullName}");

            var parameters = method.GetParameters();

            if (Definition.HasThis)
            {
                if (index == 0)
                    return new PointerAstType(AstType.FromClrType(method.DeclaringType!));

                index -= 1;
            }

            if (index < Definition.Parameters.Count)
                {
                    var param = Definition.Parameters[index];

                    var clrType = TypeHelper.Find(param.ParameterType.Resolve());

                    if (clrType == null)
                        throw new InvalidOperationException($"Unable to resolve type for parameter {param.Name} in method {Definition.FullName}");

                    var type = AstType.FromClrType(clrType);

                    var isByValue = parameters[index].GetCustomAttributes(typeof(ByValueAttribute), false).Length > 0;

                    if (type.IsStruct && !type.IsPrimitive && !isByValue)
                    {
                        return new PointerAstType(type);
                    }

                    return type;
                }
            
            throw new InvalidOperationException($"Invalid argument index {index} for method {Definition.FullName}");
        }
    }
}