using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version of ConvInstruction for type conversions
    /// Handles all standard IL conv types
    /// </summary>
    [Instruction(
        Code.Conv_I,        // Convert to native int
        Code.Conv_I1,       // Convert to int8 (sbyte)
        Code.Conv_I2,       // Convert to int16 (short)
        Code.Conv_I4,       // Convert to int32 (int)
        Code.Conv_I8,       // Convert to int64 (long)
        Code.Conv_U,        // Convert to native uint
        Code.Conv_U1,       // Convert to uint8 (byte)
        Code.Conv_U2,       // Convert to uint16 (ushort)
        Code.Conv_U4,       // Convert to uint32 (uint)
        Code.Conv_U8,       // Convert to uint64 (ulong)
        Code.Conv_R4,       // Convert to float32 (float)
        Code.Conv_R8,       // Convert to float64 (double)
        Code.Conv_R_Un,     // Convert unsigned to floating point
        // Overflow checking versions
        Code.Conv_Ovf_I,    // Convert to native int with overflow detection
        Code.Conv_Ovf_I1,   // Convert to int8 with overflow detection
        Code.Conv_Ovf_I2,   // Convert to int16 with overflow detection
        Code.Conv_Ovf_I4,   // Convert to int32 with overflow detection
        Code.Conv_Ovf_I8,   // Convert to int64 with overflow detection
        Code.Conv_Ovf_U,    // Convert to native uint with overflow detection
        Code.Conv_Ovf_U1,   // Convert to uint8 with overflow detection
        Code.Conv_Ovf_U2,   // Convert to uint16 with overflow detection
        Code.Conv_Ovf_U4,   // Convert to uint32 with overflow detection
        Code.Conv_Ovf_U8,   // Convert to uint64 with overflow detection
        // Overflow checking unsigned versions
        Code.Conv_Ovf_I_Un, // Convert unsigned to native int with overflow detection
        Code.Conv_Ovf_I1_Un,// Convert unsigned to int8 with overflow detection
        Code.Conv_Ovf_I2_Un,// Convert unsigned to int16 with overflow detection
        Code.Conv_Ovf_I4_Un,// Convert unsigned to int32 with overflow detection
        Code.Conv_Ovf_I8_Un,// Convert unsigned to int64 with overflow detection
        Code.Conv_Ovf_U_Un, // Convert unsigned to native uint with overflow detection
        Code.Conv_Ovf_U1_Un,// Convert unsigned to uint8 with overflow detection
        Code.Conv_Ovf_U2_Un,// Convert unsigned to uint16 with overflow detection
        Code.Conv_Ovf_U4_Un,// Convert unsigned to uint32 with overflow detection
        Code.Conv_Ovf_U8_Un // Convert unsigned to uint64 with overflow detection
    )]
    public class AstConvInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var expression = ExpressionStack.Pop();

            // Determine the target type based on the opcode
            var targetType = GetTargetType(Instruction.OpCode.Code);

            // Create a cast expression
            var castExpression = new CastExpression(expression, targetType);

            // Push the cast result onto the expression stack
            ExpressionStack.Push(castExpression);

            // Conv instructions don't produce statements, just transform the stack
            return new NopStatement();
        }

        private AstType GetTargetType(Code opCode)
        {
            return opCode switch
            {
                // Native int/uint (treated as int32/uint32 for OpenCL compatibility)
                Code.Conv_I or Code.Conv_Ovf_I or Code.Conv_Ovf_I_Un => PrimitiveAstType.Int32,
                Code.Conv_U or Code.Conv_Ovf_U or Code.Conv_Ovf_U_Un => PrimitiveAstType.UInt32,

                // Signed integers
                Code.Conv_I1 or Code.Conv_Ovf_I1 or Code.Conv_Ovf_I1_Un => PrimitiveAstType.Int8,
                Code.Conv_I2 or Code.Conv_Ovf_I2 or Code.Conv_Ovf_I2_Un => PrimitiveAstType.Int16,
                Code.Conv_I4 or Code.Conv_Ovf_I4 or Code.Conv_Ovf_I4_Un => PrimitiveAstType.Int32,
                Code.Conv_I8 or Code.Conv_Ovf_I8 or Code.Conv_Ovf_I8_Un => PrimitiveAstType.Int64,

                // Unsigned integers
                Code.Conv_U1 or Code.Conv_Ovf_U1 or Code.Conv_Ovf_U1_Un => PrimitiveAstType.UInt8,
                Code.Conv_U2 or Code.Conv_Ovf_U2 or Code.Conv_Ovf_U2_Un => PrimitiveAstType.UInt16,
                Code.Conv_U4 or Code.Conv_Ovf_U4 or Code.Conv_Ovf_U4_Un => PrimitiveAstType.UInt32,
                Code.Conv_U8 or Code.Conv_Ovf_U8 or Code.Conv_Ovf_U8_Un => PrimitiveAstType.UInt64,

                // Floating point
                Code.Conv_R4 => PrimitiveAstType.Float32,
                Code.Conv_R8 => PrimitiveAstType.Float64,
                Code.Conv_R_Un => PrimitiveAstType.Float32, // Unsigned to float conversion

                _ => throw new System.NotSupportedException($"Conversion opcode {opCode} not supported")
            };
        }
    }
}
