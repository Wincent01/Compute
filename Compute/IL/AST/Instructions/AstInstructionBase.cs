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

        public Stack<IExpression> ExpressionStack => Context.ExpressionStack;

        public HashSet<Type> TypeDependencies => Context.TypeDependencies;

        public HashSet<MethodDefinition> MethodDependencies => Context.MethodDependencies;
        
        public Dictionary<int, IExpression> Variables => Context.Variables;

        /// <summary>
        /// Compiles this instruction and returns any resulting statement.
        /// May push expressions onto the ExpressionStack or add statements to PrefixStatements.
        /// </summary>
        /// <returns>A statement if this instruction produces one, otherwise null</returns>
        public abstract IStatement CompileToAst();
    }
}