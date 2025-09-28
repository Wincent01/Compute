using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Compute.IL.Utility;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version for loading local variables
    /// </summary>
    [Instruction(Code.Ldfld)]
    public class AstLdfldInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var fieldReference = (FieldReference)Instruction.Operand;

            var fieldDef = fieldReference.Resolve();
            if (fieldDef == null)
                throw new System.Exception($"Field {fieldReference.FullName} could not be resolved");

            var fieldType = TypeHelper.Find(fieldDef.FieldType.Resolve());

            if (fieldType == null)
                throw new System.Exception($"Field type {fieldDef.FieldType.FullName} could not be resolved");

            var instance = ExpressionStack.Pop();

            var fieldAccess = new FieldAccessExpression(instance, fieldReference.Name, AstType.FromClrType(fieldType));

            ExpressionStack.Push(fieldAccess);
            
            return new NopStatement(); // Loading fields doesn't produce a statement
        }
    }
}