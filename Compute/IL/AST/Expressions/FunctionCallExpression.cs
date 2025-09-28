using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Compute.IL.AST.Expressions
{
    /// <summary>
    /// Represents a function call expression
    /// </summary>
    public class FunctionCallExpression : ExpressionBase
    {
        public MethodReference Method { get; }

        public IReadOnlyList<IExpression> Arguments { get; }

        public override IEnumerable<IAstNode> Children => Arguments.Cast<IAstNode>();

        public FunctionCallExpression(MethodReference method, IReadOnlyList<IExpression> arguments, AstType returnType) 
            : base(returnType)
        {
            Method = method;
            Arguments = arguments;
        }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"FunctionCall({Method.FullName}({string.Join(", ", Arguments)}))";
        }
    }
}