using System.Collections.Generic;

namespace Compute.IL.AST.Statements
{
    /// <summary>
    /// Represents an expression used as a statement (e.g., function call)
    /// </summary>
    public class ExpressionStatement : StatementBase
    {
        public IExpression Expression { get; }

        public override IEnumerable<IAstNode> Children => new IAstNode[] { Expression };

        public ExpressionStatement(IExpression expression)
        {
            Expression = expression;
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
            return $"ExpressionStmt({Expression})";
        }
    }
}