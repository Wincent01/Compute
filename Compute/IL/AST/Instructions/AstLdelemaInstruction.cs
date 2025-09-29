using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST instruction for array element access (ldelem)
    /// </summary>
    [Instruction(Code.Ldelema)]
    public class AstLdelemaInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var index = ExpressionStack.Pop();
            var array = ExpressionStack.Pop();
            
            var elementType = GetElementType(array.Type);
            var arrayAccess = new ArrayAccessExpression(array, index, elementType);
            
            ExpressionStack.Push(new AddressOfExpression(arrayAccess, new PointerAstType(elementType)));
            
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