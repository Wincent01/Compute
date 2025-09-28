using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version of LdindInstruction for loading indirect values from memory
    /// </summary>
    [Instruction(Code.Ldind_I1, Code.Ldind_U1, Code.Ldind_I2, Code.Ldind_U2, Code.Ldind_I4, Code.Ldind_U4, 
                 Code.Ldind_I8, Code.Ldind_I, Code.Ldind_R4, Code.Ldind_R8, Code.Ldind_Ref)]
    public class AstLdindInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var address = ExpressionStack.Pop();
            
            // Determine the type to load based on the opcode
            AstType typeToLoad = Instruction.OpCode.Code switch
            {
                Code.Ldind_I1 => PrimitiveAstType.Int8,
                Code.Ldind_U1 => PrimitiveAstType.UInt8,
                Code.Ldind_I2 => PrimitiveAstType.Int16,
                Code.Ldind_U2 => PrimitiveAstType.UInt16,
                Code.Ldind_I4 => PrimitiveAstType.Int32,
                Code.Ldind_U4 => PrimitiveAstType.UInt32,
                Code.Ldind_I8 => PrimitiveAstType.Int64,
                Code.Ldind_I => PrimitiveAstType.Int64, // Assuming 64-bit for IntPtr
                Code.Ldind_R4 => PrimitiveAstType.Float32,
                Code.Ldind_R8 => PrimitiveAstType.Float64,
                Code.Ldind_Ref => new PointerAstType(PrimitiveAstType.Void), // Using void* for reference types
                _ => throw new System.NotSupportedException($"Ldind opcode {Instruction.OpCode.Code} not supported")
            };

            if (address is AddressOfExpression addrOf)
            {
                address = addrOf.Expression;

                ExpressionStack.Push(new CastExpression(address, typeToLoad));
                
                return new NopStatement(); // Loading indirect values doesn't produce a statement
            }

            var dereferenceExpr = new DereferenceExpression(address, typeToLoad);

            ExpressionStack.Push(new CastExpression(dereferenceExpr, typeToLoad));

            return new NopStatement(); // Loading indirect values doesn't produce a statement
        }
    }
}