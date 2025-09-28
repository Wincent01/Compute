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
    [Instruction(Code.Ldflda)]
    public class AstLdfldaInstruction : AstInstructionBase
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

            var declearingType = TypeHelper.Find(fieldReference.DeclaringType.Resolve());

            if (declearingType == null)
                throw new System.Exception($"Declaring type {fieldReference.DeclaringType.FullName} could not be resolved");

            var field = declearingType.GetField(fieldReference.Name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);

            if (field == null)
                throw new System.Exception($"Field {fieldReference.Name} not found in type {declearingType.FullName}");

            TypeDependencies.Add(fieldType);

            var aliasAttributes = field.GetCustomAttributes(typeof(AliasAttribute), false);

            var instance = ExpressionStack.Pop();

            var astType = AstType.FromClrType(fieldType);

            var fieldAccess = new FieldAccessExpression(instance, aliasAttributes.Length > 0 ? (aliasAttributes[0] as AliasAttribute)!.Alias : fieldReference.Name, astType);

            ExpressionStack.Push(new AddressOfExpression(fieldAccess, new PointerAstType(astType)));

            return new NopStatement(); // Loading fields doesn't produce a statement
        }
    }
}