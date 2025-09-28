using System.Collections.Generic;

namespace Compute.IL.AST.Expressions
{
    /// <summary>
    /// Represents a dereference expression
    /// </summary>
    public class DereferenceExpression : ExpressionBase
    {
        public IExpression Expression { get; }
        public AstType TargetType { get; }

        public override IEnumerable<IAstNode> Children => new IAstNode[] { Expression };

        public DereferenceExpression(IExpression expression, AstType targetType) : base(targetType)
        {
            Expression = expression;
            TargetType = targetType;
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
            return $"Dereference(({TargetType}){Expression})";
        }
    }
}