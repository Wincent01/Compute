using System.Collections.Generic;

namespace Compute.IL.AST.Statements
{
    /// <summary>
    /// Represents a return statement
    /// </summary>
    public class ReturnStatement : StatementBase
    {
        public IExpression? Value { get; }

        public override IEnumerable<IAstNode> Children => 
            Value != null ? [Value] : System.Array.Empty<IAstNode>();

        public ReturnStatement(IExpression? value)
        {
            Value = value;
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
            return Value != null ? $"Return({Value})" : "Return()";
        }
    }
}