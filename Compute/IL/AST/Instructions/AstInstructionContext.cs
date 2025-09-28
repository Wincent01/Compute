using System;
using System.Collections.Generic;
using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Compute.IL.Compiler;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// New AST-based instruction base class for future instruction implementations
    /// </summary>
    public class AstInstructionContext
    {
        public required MethodDefinition Definition { get; set; }

        public required MethodBody Body { get; set; }

        public required Stack<IExpression> ExpressionStack { get; set; }

        public required Dictionary<int, IExpression> Variables { get; set; }

        public required List<IStatement> Statements { get; set; }

        public required HashSet<Type> TypeDependencies { get; set; }

        public required HashSet<MethodDefinition> MethodDependencies { get; set; }
    }
}
