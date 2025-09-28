using System.Collections.Generic;

namespace Compute.IL.AST.Expressions
{
    /// <summary>
    /// Represents a type cast expression
    /// </summary>
    public class CastExpression : ExpressionBase
    {
        public IExpression Expression { get; }
        public AstType TargetType { get; }

        public override IEnumerable<IAstNode> Children => new IAstNode[] { Expression };

        public CastExpression(IExpression expression, AstType targetType) : base(targetType)
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
            return $"Cast(({TargetType}){Expression})";
        }
    }
}