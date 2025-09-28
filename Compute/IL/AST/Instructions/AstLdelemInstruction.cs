using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST instruction for array element access (ldelem)
    /// </summary>
    [Instruction(Code.Ldelem_R4, Code.Ldelem_Any, Code.Ldelem_U4, Code.Ldelem_I4)]
    public class AstLdelemInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var index = ExpressionStack.Pop();
            var array = ExpressionStack.Pop();
            
            var elementType = GetElementType(array.Type);
            var arrayAccess = new ArrayAccessExpression(array, index, elementType);
            
            ExpressionStack.Push(arrayAccess);
            
            return new NopStatement();
        }
        
        private AstType GetElementType(AstType arrayType)
        {
            return arrayType switch
            {
                ArrayAstType array => array.ElementType,
                PointerAstType pointer => pointer.ElementType,
                _ => PrimitiveAstType.Float32 // Default fallback
            };
        }
    }
}