using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version of StindInstruction for storing indirect values to memory
    /// </summary>
    [Instruction(Code.Stind_I1, Code.Stind_I2, Code.Stind_I4, Code.Stind_I8, Code.Stind_R4, Code.Stind_R8, Code.Stind_Ref)]
    public class AstStindInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var value = ExpressionStack.Pop();
            var address = ExpressionStack.Pop();

            // Determine the type to store based on the opcode
            AstType typeToStore = Instruction.OpCode.Code switch
            {
                Code.Stind_I1 => PrimitiveAstType.Int8,
                Code.Stind_I2 => PrimitiveAstType.Int16,
                Code.Stind_I4 => PrimitiveAstType.Int32,
                Code.Stind_I8 => PrimitiveAstType.Int64,
                Code.Stind_R4 => PrimitiveAstType.Float32,
                Code.Stind_R8 => PrimitiveAstType.Float64,
                Code.Stind_Ref => new PointerAstType(PrimitiveAstType.Void), // Using void* for reference types
                _ => throw new System.NotSupportedException($"Stind opcode {Instruction.OpCode.Code} not supported")
            };

            if (address is AddressOfExpression addrOf)
            {
                address = addrOf.Expression;

                return new AssignmentStatement(address, value);
            }

            var dereferenceExpr = new DereferenceExpression(address, typeToStore);

            var castValue = new CastExpression(value, new PointerAstType(typeToStore));

            var assignment = new AssignmentStatement(dereferenceExpr, castValue);
            
            return assignment;
        }
    }
}