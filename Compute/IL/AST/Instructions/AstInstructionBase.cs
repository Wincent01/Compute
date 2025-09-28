using System;
using System.Collections.Generic;
using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Compute.IL.Utility;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// New AST-based instruction base class for future instruction implementations
    /// </summary>
    public abstract class AstInstructionBase
    {
        public required Instruction Instruction { get; set; }

        public required AstInstructionContext Context { get; set; }

        public MethodDefinition Definition => Context.Definition;

        public MethodBody Body => Context.Body;

        public Stack<IExpression> ExpressionStack => Context.ExpressionStack;

        public HashSet<Type> TypeDependencies => Context.TypeDependencies;

        public HashSet<MethodDefinition> MethodDependencies => Context.MethodDependencies;

        /// <summary>
        /// Compiles this instruction and returns any resulting statement.
        /// May push expressions onto the ExpressionStack or add statements to PrefixStatements.
        /// </summary>
        /// <returns>A statement if this instruction produces one, otherwise null</returns>
        public abstract IStatement CompileToAst();

        protected string GetArgument(int index)
        {
            if (Body.Method.HasThis)
            {
                index--;
                if (index < 0) return "this";
            }

            return Body.Method.Parameters[index].Name;
        }

        protected IExpression GetVariable(int index)
        {
            // Create a new variable reference if not found
            var varType = TypeHelper.Find(Body.Variables[index].VariableType.FullName);
            var astType = AstType.FromClrType(varType ?? typeof(int));
            var identifier = new IdentifierExpression($"local{index}", astType);
            
            return identifier;
        }
    }
}