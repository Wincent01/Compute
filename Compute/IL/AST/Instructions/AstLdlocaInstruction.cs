using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version for loading local variables
    /// </summary>
    [Instruction(Code.Ldloca, Code.Ldloca_S)]
    public class AstLdlocaInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var index = Instruction.OpCode.Code switch
            {
                Code.Ldloca => ((VariableDefinition)Instruction.Operand).Index,
                Code.Ldloca_S => ((VariableDefinition)Instruction.Operand).Index,
                _ => throw new System.NotSupportedException($"Ldloca opcode {Instruction.OpCode.Code} not supported")
            };
            
            var variable = GetVariable(index);

            if (variable == null)
                throw new System.Exception($"Variable at index {index} not found");

            var addressOfExpression = new AddressOfExpression(variable, new PointerAstType(variable.Type));
            ExpressionStack.Push(addressOfExpression);
            
            return new Statements.NopStatement(); // Loading variable addresses doesn't produce a statement
        }
    }
}